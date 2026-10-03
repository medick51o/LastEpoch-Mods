using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_CatalogPort_ActualResearchDocumentRejectsKnownCollisionsAndStaysProposed()
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "boss-harbinger.proposed.json"));
        var errors = BossCatalogSchema.ValidateJson(text);
        var collisions = new[] { "Lagon, God of Storms", "Majasa", "Admiral Harton", "Aberroth" };
        Eq(collisions.Length, errors.Count, string.Join("; ", errors));
        foreach (string name in collisions) True(errors.Contains(name + " alias is duplicated"), "known unresolved alias: " + name);
        var document = JsonSerializer.Deserialize<CatalogDocument>(text, BossCatalogSchema.Json);
        foreach (string name in collisions) Eq(null, BossCatalogSchema.FindExact(document, name));
        Eq(53, document.Encounters.Count);
        Eq(1, document.Encounters.Count(e => e.DamageCoverage.Status == "known"));
        Eq("julra", document.Encounters.Single(e => e.DamageCoverage.Status == "known").Id);
        Eq(5, BossCatalog.Starter.Count);
        True(!BossCatalog.Starter.Any(e => e.Id == "harbinger-chaos"), "historical starter projection stays frozen");
    }
    static void Test_CatalogPort_DuplicateIdsReturnErrorsWithoutThrowing()
    {
        var one = SchemaEncounter("same-id", "One", "One");
        var two = SchemaEncounter("same-id", "Two", "Two");
        var document = SchemaDoc(one, two);
        True(BossCatalogSchema.Validate(document).Any(e => e.Contains("duplicated")), "semantic validation reports duplicate IDs");
        True(BossCatalogSchema.ValidateJson(JsonSerializer.Serialize(document, BossCatalogSchema.Json)).Any(e => e.Contains("duplicated")), "JSON validation reports duplicate IDs");
    }
    static void Test_CatalogPort_AmbiguousExactAliasCannotPickFirstEncounter()
    {
        var document = SchemaDoc(SchemaEncounter("campaign-lagon", "Campaign Lagon", "Lagon"), SchemaEncounter("monolith-lagon", "Monolith Lagon", "lagon"));
        Eq(null, BossCatalogSchema.FindExact(document, " LAGON "));
        Eq("confirmed-unidentified", BossCatalogSchema.Classify(document, new DeathRecord { Killer = "Lagon", IsBossFight = true }).Kind);
        Eq("unknown", BossCatalogSchema.Classify(document, new DeathRecord { Killer = "Lagon" }).Kind);
    }
    static void Test_CatalogPort_ForumHostCannotPromoteUnverifiedSourceToDeveloper()
    {
        var profile = new BossProfile("community-example", "Example", new[] { "Example" }, "Unverified mechanics", "Unknown damage",
            Array.Empty<Element>(), Array.Empty<string>(),
            new BossSource("Community discussion", "https://forum.lastepoch.com/t/community-unverified/1"),
            new BossSource("Spoofed host", "https://forum.lastepoch.com.example.org/t/last-epoch-patch-1-2-1-notes/76245"));
        var projected = BossCatalogSchema.Project(profile);
        True(projected.Claims.Where(c => c.Source != null).All(c => c.EvidenceClass == "unknown"), "only inspected developer pages can be promoted");
    }
}
