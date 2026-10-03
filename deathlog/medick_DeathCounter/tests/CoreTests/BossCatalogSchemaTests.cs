using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static EncounterRecord SchemaEncounter(string id, string name, string alias) => new()
    {
        Id = id,
        DisplayName = name,
        Aliases = new List<AliasRecord> { new() { Text = alias, Locale = LocaleField.Unknown() } },
        Parent = ParentRef.Unknown(),
        Variants = CatalogList<VariantRecord>.Unknown(),
        Locations = CatalogList<LocationRecord>.Unknown(),
        Abilities = CatalogList<AbilityRecord>.Unknown(),
        Ailments = CatalogList<AilmentRecord>.Unknown(),
        BrowseGroups = CatalogList<string>.Unknown(),
        DamageCoverage = DamageCoverage.Unknown(),
        Tips = new List<TipRecord>(),
        Claims = new List<ClaimRecord>
        {
            new() { Id = id + ".note", Statement = "Synthetic fixture.", EvidenceClass = "unknown", Confidence = ConfidenceField.Unknown() }
        }
    };

    static CatalogDocument SchemaDoc(params EncounterRecord[] encounters) => new()
    {
        SchemaVersion = BossCatalogSchema.Version,
        Reviewed = "Synthetic fixture for schema tests.",
        Encounters = encounters.ToList()
    };

    static EncounterRecord CompleteFire()
    {
        var encounter = SchemaEncounter("synth-complete", "Synthetic Complete", "Synthetic Complete");
        encounter.Parent = ParentRef.Absent();
        encounter.Variants = CatalogList<VariantRecord>.Known(new List<VariantRecord>());
        encounter.Abilities = CatalogList<AbilityRecord>.Known(new List<AbilityRecord>
        {
            new() { Id = "synth-slam", DisplayName = "Synthetic Slam" }
        });
        encounter.Ailments = CatalogList<AilmentRecord>.Known(new List<AilmentRecord> { new() { Name = "Synthetic Mark" } });
        encounter.DamageCoverage = new DamageCoverage
        {
            Status = "known",
            Scope = "encounter",
            Elements = new List<string> { "Fire" },
            EvidenceClass = "datamined-reference",
            Confidence = ConfidenceField.Known("high"),
            PerAbility = CatalogList<DamageComponent>.Known(new List<DamageComponent>
            {
                new()
                {
                    AbilityId = "synth-slam", Element = "Fire", Delivery = "hit",
                    EvidenceClass = "datamined-reference", Confidence = ConfidenceField.Known("high")
                }
            })
        };
        return encounter;
    }

    static void SchemaOk(CatalogDocument doc, string what)
    {
        var errors = BossCatalogSchema.ValidateJson(JsonSerializer.Serialize(doc, BossCatalogSchema.Json));
        True(errors.Count == 0, what + ": " + string.Join("; ", errors));
    }

    static void Test_Catalog_BaselineProjectsFiveProfilesWithExplicitUnknowns()
    {
        var doc = BossCatalogSchema.ProjectBaseline();
        SchemaOk(doc, "baseline");
        Eq(BossCatalogSchema.Version, doc.SchemaVersion);
        Eq(BossCatalog.Reviewed, doc.Reviewed);
        Eq("lagon,emperor-corpses,heorot,julra,harbinger-hatred", string.Join(",", doc.Encounters.Select(e => e.Id)));
        foreach (var profile in BossCatalog.Starter)
        {
            var encounter = doc.Encounters.Single(e => e.Id == profile.Id);
            Eq(profile.Name, encounter.DisplayName);
            Eq(string.Join("|", profile.Aliases), string.Join("|", encounter.Aliases.Select(a => a.Text)));
            Eq("unknown", encounter.Parent.Status);
            Eq(null, encounter.Parent.EncounterId);
            Eq("unknown", encounter.Variants.Status);
            Eq(null, encounter.Variants.Items);
            Eq("unknown", encounter.Locations.Status);
            Eq("unknown", encounter.Abilities.Status);
            Eq("unknown", encounter.Ailments.Status);
            Eq("unknown", encounter.BrowseGroups.Status);
            Eq(null, encounter.BrowseGroups.Items);
            Eq(profile.Mechanics, encounter.Claims.Single(c => c.Id == profile.Id + ".mechanics").Statement);
            Eq(profile.DamageEvidence, encounter.Claims.Single(c => c.Id == profile.Id + ".damage-evidence").Statement);
            Eq(profile.Tips.Count, encounter.Tips.Count);
            for (int i = 0; i < profile.Tips.Count; i++)
            {
                Eq(profile.Tips[i], encounter.Tips[i].Text);
                Eq("tactical-inference", encounter.Tips[i].EvidenceClass);
                Eq("unknown", encounter.Tips[i].Confidence.Status);
                Eq(null, encounter.Tips[i].Confidence.Level);
            }
            foreach (var alias in encounter.Aliases)
            {
                Eq("unknown", alias.Locale.Status);
                Eq(null, alias.Locale.Tag);
            }
        }
        var lagon = doc.Encounters.Single(e => e.Id == "lagon");
        Eq("unknown", lagon.DamageCoverage.Status);
        Eq(null, lagon.DamageCoverage.Elements);
        Eq(null, lagon.DamageCoverage.Scope);
        Eq("unknown", lagon.DamageCoverage.EvidenceClass);
        Eq("developer-documentation", lagon.Claims.Single(c => c.Id == "lagon.source.1").EvidenceClass);
        True(lagon.Abilities.Status == "unknown", "named attacks in prose are not ability rows");
        var julra = doc.Encounters.Single(e => e.Id == "julra");
        Eq("known", julra.DamageCoverage.Status);
        Eq("encounter", julra.DamageCoverage.Scope);
        Eq("Void,Cold,Lightning", string.Join(",", julra.DamageCoverage.Elements));
        Eq("unknown", julra.DamageCoverage.PerAbility.Status);
        Eq(null, julra.DamageCoverage.PerAbility.Items);
        Eq("datamined-reference", julra.DamageCoverage.EvidenceClass);
        Eq("unknown", julra.DamageCoverage.Confidence.Status);
        Eq("datamined-reference", julra.Claims.Single(c => c.Source != null && c.Source.Url.Contains("tunklab.com")).EvidenceClass);
        Eq("unknown", julra.Claims.Single(c => c.Source != null && c.Source.Url.Contains("lastepochtools.com")).EvidenceClass);
        Eq("unknown", doc.Encounters.Single(e => e.Id == "harbinger-hatred").BrowseGroups.Status);
    }

    static void Test_Catalog_BaselineJsonKeepsNullDistinctFromUnknown()
    {
        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(BossCatalogSchema.ProjectBaseline(), BossCatalogSchema.Json));
        var encounters = parsed.RootElement.GetProperty("encounters").EnumerateArray().ToList();
        var lagon = encounters.Single(e => e.GetProperty("id").GetString() == "lagon");
        Eq("unknown", lagon.GetProperty("parent").GetProperty("status").GetString());
        Eq(JsonValueKind.Null, lagon.GetProperty("parent").GetProperty("encounterId").ValueKind);
        Eq(JsonValueKind.Null, lagon.GetProperty("damageCoverage").GetProperty("elements").ValueKind);
        Eq(JsonValueKind.Null, lagon.GetProperty("abilities").GetProperty("items").ValueKind);
        Eq(JsonValueKind.Null, lagon.GetProperty("browseGroups").GetProperty("items").ValueKind);
        var julra = encounters.Single(e => e.GetProperty("id").GetString() == "julra");
        Eq("known", julra.GetProperty("damageCoverage").GetProperty("status").GetString());
        Eq(JsonValueKind.Null, julra.GetProperty("damageCoverage").GetProperty("perAbility").GetProperty("items").ValueKind);
        Eq("unknown", julra.GetProperty("aliases")[1].GetProperty("locale").GetProperty("status").GetString());
        Eq(JsonValueKind.Null, julra.GetProperty("aliases")[1].GetProperty("locale").GetProperty("tag").ValueKind);
        Eq(JsonValueKind.Null, julra.GetProperty("claims")[0].GetProperty("source").ValueKind);
    }

    static void Test_Catalog_ExactAliasesMatchAndPartialsDoNot()
    {
        var doc = BossCatalogSchema.ProjectBaseline();
        foreach (var alias in new[] { "Lagon", "Emperor of Corpses", "Heorot", "Chronomancer Julra", "Julra", "Harbinger of Hatred" })
        {
            var flagged = new DeathRecord { Killer = alias, IsBossFight = true };
            var link = BossCatalogSchema.Classify(doc, flagged);
            Eq("identified", link.Kind);
            Eq(BossCatalogSchema.FindExact(doc, alias).Id, link.EncounterId);
            var offline = new DeathRecord { Killer = alias };
            var guide = BossCatalogSchema.Classify(doc, offline);
            Eq("unconfirmed-guide", guide.Kind);
            Eq(null, BossCatalog.Match(offline));
            Eq(BossCatalogSchema.FindExact(doc, alias).Id, guide.EncounterId);
            Eq(null, offline.IsBossFight);
            var denied = new DeathRecord { Killer = alias, IsBossFight = false };
            Eq("not-boss", BossCatalogSchema.Classify(doc, denied).Kind);
            Eq(null, BossCatalogSchema.DeathLinkCaption(denied));
            Eq(null, BossCatalog.AttackerGuide(denied));
        }
        Eq("lagon", BossCatalogSchema.FindExact(doc, "  lagon  ").Id);
        Eq("julra", BossCatalogSchema.FindExact(doc, "CHRONOMANCER JULRA").Id);
        Eq("harbinger-hatred", BossCatalogSchema.FindExact(doc, "harbinger of hatred").Id);
        foreach (var name in new[] { "Lagon's Tentacle", "Lagon Clone", "Emperor's Remains", "Emperor", "Chronomancer", "Harbinger", "Hatred", "Rahyeh", "Void Rahyeh", "Harbinger of Hatred's Void Rahyeh", "Julra's Echo" })
            Eq(null, BossCatalogSchema.FindExact(doc, name));
        Eq(null, BossCatalogSchema.FindExact(doc, "  "));
        Eq(null, BossCatalogSchema.FindExact(doc, null));
    }

    static void Test_Catalog_AddsProxiesVariantsAndAbilityNamesDoNotMatch()
    {
        var parent = SchemaEncounter("synth-parent", "Synthetic Parent", "Synthetic Parent");
        var variant = SchemaEncounter("synth-variant", "Synthetic Variant", "Synthetic Variant");
        variant.Parent = ParentRef.Known("synth-parent");
        variant.Variants = CatalogList<VariantRecord>.Known(new List<VariantRecord>
        {
            new() { Id = "synth-empowered", DisplayName = "Synthetic Empowered", Relationship = "empowered" },
            new() { Id = "synth-alternate", DisplayName = "Synthetic Alternate", Relationship = "alternate" }
        });
        var add = SchemaEncounter("synth-add", "Synthetic Add", "Synthetic Add");
        add.Parent = ParentRef.Known("synth-parent");
        var named = SchemaEncounter("synth-shown", "Shown Name", "Other Alias");
        var doc = SchemaDoc(parent, variant, add, named);
        SchemaOk(doc, "synthetic family");
        Eq("synth-parent", BossCatalogSchema.FindExact(doc, "Synthetic Parent").Id);
        Eq("synth-variant", BossCatalogSchema.FindExact(doc, "Synthetic Variant").Id);
        Eq("synth-add", BossCatalogSchema.FindExact(doc, "Synthetic Add").Id);
        Eq(null, BossCatalogSchema.FindExact(doc, "Synthetic Empowered"));
        Eq(null, BossCatalogSchema.FindExact(doc, "Synthetic Alternate"));
        Eq(null, BossCatalogSchema.FindExact(doc, "Shown Name"));
        Eq("synth-shown", BossCatalogSchema.FindExact(doc, "Other Alias").Id);
        foreach (var name in new[] { "Synthetic Parent Add", "Synthetic Parent Proxy", "Synthetic Variant Clone" })
            Eq(null, BossCatalogSchema.FindExact(doc, name));
        var byAbility = new DeathRecord { IsBossFight = true, KillerAbility = "Lightning Blast", Zone = "Temporal Sanctum", IrregularSource = "Heorot", RawSceneId = "Boss_Arena_55" };
        Eq("confirmed-unidentified", BossCatalogSchema.Classify(BossCatalogSchema.ProjectBaseline(), byAbility).Kind);
        Eq(null, BossCatalogSchema.Classify(doc, byAbility).EncounterId);
        Eq(0, BossCatalogSchema.ParentElementsAppliedToFollowUp(parent, variant).Count);
        Eq("unknown", variant.DamageCoverage.Status);
    }

    static void Test_Catalog_ManualBrowseDoesNotRelabelDeath()
    {
        var doc = BossCatalogSchema.ProjectBaseline();
        var death = new DeathRecord { Id = "death-1", Killer = "Lagon", IsBossFight = true, KillingElement = "Cold", Zone = "Frozen scene label" };
        string before = JsonSerializer.Serialize(death);
        True(BossCatalogSchema.TryBrowse("harbinger", "heorot", death, out var selected), "browse");
        Eq("heorot", selected);
        Eq(before, JsonSerializer.Serialize(death));
        Eq("identified", BossCatalogSchema.Classify(doc, death).Kind);
        Eq("lagon", BossCatalogSchema.Classify(doc, death).EncounterId);
        Eq("Browse boss tips", BossCatalogSchema.DeathLinkCaption(death)); // historical alias does not guess a current variant
        Eq(false, BossCatalogSchema.TryBrowse("all", "lagon", death, out _));
        Eq(false, BossCatalogSchema.TryBrowse("boss", "  ", death, out _));
        Eq(before, JsonSerializer.Serialize(death));
        var confirmed = new DeathRecord { IsBossFight = true, Killer = "Thrall" };
        Eq("Browse boss tips", BossCatalogSchema.DeathLinkCaption(confirmed));
        Eq("confirmed-unidentified", BossCatalogSchema.Classify(doc, confirmed).Kind);
    }

    static void Test_Catalog_CoverageCannotManufactureDeathDamage()
    {
        var doc = BossCatalogSchema.ProjectBaseline();
        var julra = doc.Encounters.Single(e => e.Id == "julra");
        var death = new DeathRecord { Killer = "Julra", IsBossFight = true };
        string before = JsonSerializer.Serialize(death);
        Eq(false, BossCatalogSchema.TryApplyCoverageToDeath(death, julra));
        Eq(before, JsonSerializer.Serialize(death));
        Eq(null, death.KillingElement);
        Eq(0f, death.KillingBlow);
        Eq(0f, death.DamageByElement.Sum());
        Eq(0, BossCatalogSchema.AdviceElements(death, julra).Count);
        Eq("Void,Cold,Lightning", string.Join(",", julra.DamageCoverage.Elements));
    }

    static void Test_Catalog_MixedAndUnknownTypingStayOnTheDeath()
    {
        var julra = BossCatalogSchema.ProjectBaseline().Encounters.Single(e => e.Id == "julra");
        var mixed = new DeathRecord { Killer = "Julra", IsBossFight = true, KillingElement = "Fire", SecondaryKillingElement = "Physical" };
        var elements = BossCatalogSchema.AdviceElements(mixed, julra);
        Eq(2, elements.Count);
        Eq(Element.Fire, elements[0]);
        Eq(Element.Physical, elements[1]);
        True(!elements.Contains(Element.Void) && !elements.Contains(Element.Cold) && !elements.Contains(Element.Lightning), "catalog coverage stays off the death");
        var unknown = new DeathRecord { Killer = "Julra", IsBossFight = true, KillingElement = "not-an-element" };
        Eq(0, BossCatalogSchema.AdviceElements(unknown, julra).Count);
        Eq("not-an-element", unknown.KillingElement);
    }

    static void Test_Catalog_ParentCapDoesNotClearUnknownFollowUp()
    {
        var recorded = CompleteFire();
        var followUp = SchemaEncounter("synth-harbinger", "Synthetic Harbinger", "Synthetic Harbinger");
        followUp.Parent = ParentRef.Known(recorded.Id);
        SchemaOk(SchemaDoc(recorded, followUp), "pair");
        Eq(true, BossCatalogSchema.Prepare(recorded, null, new[] { Element.Fire }).ResistanceCoverageComplete);
        var open = BossCatalogSchema.Prepare(recorded, followUp, new[] { Element.Fire, Element.Void });
        Eq(false, open.ResistanceCoverageComplete);
        True(open.OpenThreats.Contains("follow-up: unknown additional threats"), string.Join(" | ", open.OpenThreats));
        True(open.OpenThreats.All(t => !t.StartsWith("recorded encounter:")), string.Join(" | ", open.OpenThreats));
        followUp.DamageCoverage = new DamageCoverage
        {
            Status = "known", Scope = "encounter", Elements = new List<string> { "Void" },
            PerAbility = CatalogList<DamageComponent>.Unknown(), EvidenceClass = "datamined-reference", Confidence = ConfidenceField.Unknown()
        };
        var extra = BossCatalogSchema.Prepare(recorded, followUp, new[] { Element.Fire, Element.Void });
        True(extra.OpenThreats.Contains("follow-up: Void is not closed by the parent death"), string.Join(" | ", extra.OpenThreats));
        Eq(false, extra.ResistanceCoverageComplete);
        Eq(0, BossCatalogSchema.ParentElementsAppliedToFollowUp(recorded, followUp).Count);
        foreach (var name in new[] { "Void Rahyeh", "Ice Spike", "Lightning Blast", "Moon Blast", "Soul Bomb" })
            Eq(false, BossCatalogSchema.AbilityNameEstablishesDamage(name, "Void"));
    }

    static void Test_Catalog_GradeAOnParentElementDoesNotPrepareFollowUp()
    {
        var death = AdviceDeath("Fire", 41);
        death.Killer = "Synthetic Parent";
        death.IsBossFight = true;
        death.KillingCrit = false;
        string before = JsonSerializer.Serialize(death);
        var result = CurrentAssessment.Build(death, "Test", NewGear());
        Eq("A", result.Rating.Grade);
        var followUp = SchemaEncounter("synth-harbinger", "Synthetic Harbinger", "Synthetic Harbinger");
        var report = BossCatalogSchema.Prepare(CompleteFire(), followUp, new[] { Element.Fire });
        Eq(false, report.ResistanceCoverageComplete);
        True(report.OpenThreats.Contains("follow-up: unknown additional threats"), string.Join(" | ", report.OpenThreats));
        Eq(before, JsonSerializer.Serialize(death));
        Eq("Fire", death.KillingElement);
        Eq(950f, death.KillingBlow);
    }

    static void Test_Catalog_EditsDoNotRewriteSavedReassessments()
    {
        InAssessmentDirectory(dir =>
        {
            var death = AdviceDeath("Fire", 41);
            death.Killer = "Julra";
            death.IsBossFight = true;
            death.KillingCrit = false;
            var updates = new ReassessmentLog(dir);
            updates.Load();
            var saved = SaveUpdate(updates, death, NewGear());
            string frozen = JsonSerializer.Serialize(saved);
            var doc = BossCatalogSchema.ProjectBaseline();
            doc.Encounters.Single(e => e.Id == "julra").Claims.Single(c => c.Id == "julra.mechanics").Statement = "Rewritten claim that must not land in the saved update.";
            doc.Encounters.Single(e => e.Id == "julra").DamageCoverage.Elements.Add("Fire");
            True(BossCatalogSchema.TryBrowse("dungeon", "lagon", death, out _), "browse");
            Eq(false, BossCatalogSchema.TryApplyCoverageToDeath(death, doc.Encounters.Single(e => e.Id == "julra")));
            Eq(frozen, JsonSerializer.Serialize(saved));
            var again = new ReassessmentLog(dir);
            again.Load();
            Eq(frozen, JsonSerializer.Serialize(again.All.Single()));
            Eq("Julra", again.All[0].OriginalCapture.Killer);
            Eq(true, again.All[0].OriginalCapture.IsBossFight);
            Eq("Fire", again.All[0].OriginalCapture.KillingElement);
            True(again.All[0].Assessment.Actions.All(a => a.Body == null || !a.Body.Contains("Rewritten")), "saved actions stay frozen");
        });
    }

    static void Test_Catalog_SourceLookupDoesNotCreateThreeResistanceCards()
    {
        var death = new DeathRecord
        {
            Killer = "Julra", IsBossFight = true, Character = "Test", Kind = DeathKind.Reported,
            DetailSource = "game death report", MaxHealth = 1000, DefenseSnapshotAgeSeconds = 0.5f,
            Defenses = new() { ["MaxHealth"] = 1000 }
        };
        string before = string.Join(",", Advisor.Show(death).Select(t => t.Key));
        var julra = BossCatalogSchema.ProjectBaseline().Encounters.Single(e => e.Id == "julra");
        Eq("identified", BossCatalogSchema.Classify(BossCatalogSchema.ProjectBaseline(), death).Kind);
        Eq(false, BossCatalogSchema.TryApplyCoverageToDeath(death, julra));
        Eq(0, BossCatalogSchema.AdviceElements(death, julra).Count);
        Eq(before, string.Join(",", Advisor.Show(death).Select(t => t.Key)));
        True(Advisor.Show(death).All(t => t.Key != "res_Void" && t.Key != "res_Cold" && t.Key != "res_Lightning"), before);

        var poisoned = new DeathRecord
        {
            Kind = DeathKind.Reported, DetailSource = "game death report", MaxHealth = 1000, Hits = 1, WindowDamage = 300,
            DefenseSnapshotAgeSeconds = 0.5f, Defenses = new() { ["MaxHealth"] = 1000 },
            DamageByElement = new float[Elements.Count]
        };
        poisoned.DamageByElement[(int)Element.Void] = 100;
        poisoned.DamageByElement[(int)Element.Cold] = 100;
        poisoned.DamageByElement[(int)Element.Lightning] = 100;
        var cards = Advisor.Show(poisoned);
        Eq(3, cards.Count);
        True(cards.All(t => t.Group.StartsWith("res:")) && cards.Select(t => t.Group).Distinct().Count() == 3, "three catalog elements would fill the card limit");
        Eq(before, string.Join(",", Advisor.Show(death).Select(t => t.Key)));
    }

    static void Test_Catalog_MissingZoneSceneAndOldRecordsStayUnidentified()
    {
        var doc = BossCatalogSchema.ProjectBaseline();
        var scene = new DeathRecord { IsBossFight = true, RawSceneId = "Boss_Arena_55", Killer = "Lagon's Tentacle" };
        var link = BossCatalogSchema.Classify(doc, scene);
        Eq("confirmed-unidentified", link.Kind);
        Eq(null, link.EncounterId);
        True(scene.LocationLabel().Contains("Boss_Arena_55"), scene.LocationLabel());
        Eq("Zone unavailable", new DeathRecord().LocationLabel());
        var old = JsonSerializer.Deserialize<DeathRecord>("{\"Zone\":\"Old area\",\"Killer\":\"Julra\"}");
        Eq(null, old.IsBossFight);
        Eq(null, old.RawSceneId);
        Eq("unconfirmed-guide", BossCatalogSchema.Classify(doc, old).Kind);
        Eq("julra", BossCatalogSchema.Classify(doc, old).EncounterId);
        Eq("Old area", old.LocationLabel());
        True(BossCatalogSchema.TryBrowse("boss", "lagon", old, out var selected), "old record browse");
        Eq("lagon", selected);
        Eq("Julra", old.Killer);
        Eq(null, old.IsBossFight);
        Eq("Boss encounter unknown", BossCatalog.EncounterLabel(old));
    }

    static void Test_Catalog_LateReportDoesNotBackfillZoneOrCatalogDamage()
    {
        var death = new DeathRecord { Character = "Medick" };
        new DeathDetails { Killer = "Julra", PrimaryElement = "Lightning", Damage = 50, IsBossFight = true }.Apply(death);
        var julra = BossCatalogSchema.ProjectBaseline().Encounters.Single(e => e.Id == "julra");
        Eq(false, BossCatalogSchema.TryApplyCoverageToDeath(death, julra));
        Eq("Julra", death.Killer);
        Eq("Lightning", death.KillingElement);
        Eq(50f, death.KillingBlow);
        Eq(null, death.Zone);
        Eq(null, death.RawSceneId);
        Eq(0f, death.DamageByElement.Sum());
        Eq("identified", BossCatalogSchema.Classify(BossCatalogSchema.ProjectBaseline(), death).Kind);
        Eq("julra", BossCatalogSchema.Classify(BossCatalogSchema.ProjectBaseline(), death).EncounterId);
    }

    static void Test_Catalog_ValidationRejectsCollisionsPlaceholdersAndDoubleDashes()
    {
        var one = SchemaEncounter("synth-one", "Synthetic One", "Synthetic One");
        var two = SchemaEncounter("synth-two", "Synthetic Two", "Synthetic One");
        True(BossCatalogSchema.Validate(SchemaDoc(one, two)).Any(e => e.Contains("alias is duplicated")), "duplicate alias");
        one.Parent = ParentRef.Known("synth-one");
        True(BossCatalogSchema.Validate(SchemaDoc(one)).Any(e => e.Contains("cannot parent itself")), "self parent");
        one.Parent = ParentRef.Known("missing-boss");
        True(BossCatalogSchema.Validate(SchemaDoc(one)).Any(e => e.Contains("parent was not found")), "missing parent");
        var dashed = SchemaEncounter("synth-dash", "Synthetic Dash", "Synthetic Dash");
        dashed.Tips.Add(new TipRecord { Text = "Move -- now", EvidenceClass = "tactical-inference", Confidence = ConfidenceField.Unknown() });
        True(BossCatalogSchema.Validate(SchemaDoc(dashed)).Any(e => e.Contains("double dash")), "double dash");
        var todo = SchemaEncounter("synth-todo", "TODO boss", "Synthetic Todo");
        True(BossCatalogSchema.Validate(SchemaDoc(todo)).Any(e => e.Contains("placeholder")), "placeholder");
        var version = SchemaDoc(SchemaEncounter("synth-version", "Synthetic Version", "Synthetic Version"));
        version.SchemaVersion = "0.0.1";
        True(BossCatalogSchema.Validate(version).Any(e => e.Contains("schemaVersion")), "version");
        var inferred = CompleteFire();
        inferred.DamageCoverage.PerAbility.Items[0].EvidenceClass = "tactical-inference";
        True(BossCatalogSchema.Validate(SchemaDoc(inferred)).Any(e => e.Contains("needs source evidence")), "inference is not a damage table");
        var leaked = SchemaEncounter("synth-leak", "Synthetic Leak", "Synthetic Leak");
        leaked.DamageCoverage.Elements = new List<string> { "Fire" };
        True(BossCatalogSchema.Validate(SchemaDoc(leaked)).Any(e => e.Contains("elements and scope null")), "unknown coverage stays null");
        string json = JsonSerializer.Serialize(SchemaDoc(SchemaEncounter("synth-extra", "Synthetic Extra", "Synthetic Extra")), BossCatalogSchema.Json);
        SchemaOk(JsonSerializer.Deserialize<CatalogDocument>(json, BossCatalogSchema.Json), "round trip");
        string dirty = json.Replace("\"id\": \"synth-extra\"", "\"id\": \"synth-extra\",\n      \"extra\": true");
        True(BossCatalogSchema.ValidateJson(dirty).Any(e => e.Contains("extra")), "unsupported property");
    }
}
