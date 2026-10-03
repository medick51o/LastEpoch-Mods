using System;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_ReferenceFacts_EmbeddedCatalogLoadsActualRevisionTwoWithOnlyAliasDiagnostics()
    {
        Eq(54, BossCatalog.All.Count);
        var document = BossCatalog.Research;
        var errors = BossCatalogSchema.ValidateJson(JsonSerializer.Serialize(document, BossCatalogSchema.Json));
        Eq(4, errors.Count, string.Join("; ", errors));
        True(errors.All(e => e.EndsWith(" alias is duplicated", StringComparison.Ordinal)), "no unreviewed schema/semantic defects accepted");
        True(BossCatalog.All.Any(p => p.Id == "vision-of-the-observer"), "new pinnacle is separate from campaign observer");
        Eq(document.Encounters.Sum(e => e.Claims.Count), document.Encounters.SelectMany(e => e.Claims).Select(c => c.Id).Distinct().Count());
    }
    static void Test_ReferenceFacts_AllTenTimelineHarbingersHaveCommunityMovesAndTwoOrThreeDistinctTips()
    {
        foreach (string suffix in new[] { "defilement", "pride", "hatred", "war", "chaos", "treason", "destruction", "fear", "tyranny", "ash" })
        {
            var profile = BossCatalog.All.Single(p => p.Id == "harbinger-" + suffix);
            var encounter = profile.Encounter;
            Eq("community-guide", encounter.DamageCoverage.EvidenceClass);
            True(encounter.Abilities.Items.Count > 0, suffix + " moves");
            True(profile.Tips.Count >= 2 && profile.Tips.Count <= 3, suffix + " concise plan");
            Eq(profile.Tips.Count, profile.Tips.Distinct(StringComparer.Ordinal).Count());
        }
        var covered = BossCatalog.All.Where(p => p.Encounter.DamageCoverage.EvidenceClass == "community-guide"
            && p.Sources.Any(s => s.Url.StartsWith("https://maxroll.gg/", StringComparison.Ordinal))).ToArray();
        Eq(32, covered.Length);
        True(covered.All(p => p.Tips.Count >= (p.Id == "lagons-tentacle" ? 1 : 2) && p.Tips.Count <= 3), "single-attack tentacle needs one tip; full boss guides have two or three");
        True(covered.SelectMany(p => p.Encounter.DamageCoverage.PerAbility.Items).All(c => c.EvidenceClass == "community-guide"), "guide components never masquerade as datamined captures");
    }
    static void Test_ReferenceFacts_UncoveredHarbingersAndMorditasStayUnknown()
    {
        foreach (string id in new[] { "harbinger-regret", "harbinger-denial", "harbinger-cruelty", "harbinger-brutality", "god-of-bloodshed", "morditas", "yrun-champion-of-morditas", "site-of-carnage" })
        {
            var profile = BossCatalog.All.Single(p => p.Id == id);
            Eq(0, profile.DamageTypes.Count, id);
            Eq("unknown", profile.Encounter.DamageCoverage.Status, id);
            Eq(0, BossGuideMoves.For(id).Count, id);
        }
    }
    static void Test_ReferenceFacts_AmbiguousActorsDoNotPickCampaignPhaseOrInvisibleAdd()
    {
        foreach (string alias in new[] { "Lagon, God of Storms", "Majasa", "Admiral Harton", "Aberroth" })
        {
            var death = new DeathRecord { Killer = alias, IsBossFight = true };
            Eq(null, BossCatalog.Match(death), alias);
            Eq("confirmed-unidentified", BossCatalogSchema.Classify(BossCatalog.Research, death).Kind, alias);
            death.IsBossFight = null; Eq(null, BossCatalog.AttackerGuide(death), alias);
        }
        Eq("herald-of-oblivion", BossCatalog.Match(new DeathRecord { Killer = "Uber Aberroth", IsBossFight = true }).Id);
    }
    static void Test_ReferenceFacts_FlagsAndGuideCaptionsRetainOnlineOfflineDistinction()
    {
        var death = new DeathRecord { Killer = "Harbinger of Defilement" };
        True(BossCatalogSchema.DeathLinkCaption(death).StartsWith("Attacker guide: "), "offline guide is not proof");
        death.IsBossFight = true;
        True(BossCatalogSchema.DeathLinkCaption(death).StartsWith("Boss tips: "), "explicit true plus exact name");
        death.IsBossFight = false;
        Eq(null, BossCatalogSchema.DeathLinkCaption(death));
    }
    static void Test_ReferenceFacts_ConflictsSurviveWithoutUnioningGuideAndPreviewTypes()
    {
        var encounters = BossCatalog.Research.Encounters;
        Eq(45, encounters.SelectMany(e => e.Claims).Count(c => c.Id.Contains(".conflict.maxroll-extract.", StringComparison.Ordinal)));
        var defilement = encounters.Single(e => e.Id == "harbinger-defilement");
        True(defilement.Claims.Any(c => c.Id.EndsWith(".tail-flare", StringComparison.Ordinal) && c.Id.Contains(".conflict.")), "Tail Flare disagreement is visible evidence");
        var move = BossGuideMoves.For(defilement.Id).Single(m => m.Name == "Tail Flare");
        Eq("Fire", string.Join(",", move.Elements.Select(Elements.Name)), "guide typing preserved, preview Physical is not silently merged");
    }
    static void Test_ReferenceFacts_CommunityEvidenceCannotClearEncounterPreparation()
    {
        var encounter = CompleteFire();
        encounter.DamageCoverage.EvidenceClass = "community-guide";
        foreach (var component in encounter.DamageCoverage.PerAbility.Items) component.EvidenceClass = "community-guide";
        SchemaOk(SchemaDoc(encounter), "community coverage has its own evidence class");
        var report = BossCatalogSchema.Prepare(encounter, null, new[] { Element.Fire });
        Eq(false, report.ResistanceCoverageComplete);
        True(report.OpenThreats.Any(t => t.Contains("community guide")), "source uncertainty cannot become a cleared fight");
    }
    static void Test_ReferenceFacts_ManualDataCannotChangeOriginalDeathOrSavedAssessment()
    {
        InAssessmentDirectory(dir =>
        {
            var death = AdviceDeath("Fire", 41); death.Killer = "Harbinger of Defilement";
            var log = new ReassessmentLog(dir); log.Load();
            var saved = SaveUpdate(log, death, NewGear());
            string original = JsonSerializer.Serialize(death), update = JsonSerializer.Serialize(saved);
            foreach (var profile in BossCatalog.All)
            {
                var data = profile.Encounter;
                data.DamageCoverage.Elements?.Clear(); data.Tips.Clear(); data.Claims.Clear();
                _ = BossGuideMoves.For(profile.Id);
            }
            _ = BossCatalog.Match(death); _ = BossCatalog.ObservedTypes(death);
            Eq(original, JsonSerializer.Serialize(death)); Eq(update, JsonSerializer.Serialize(saved));
            Eq(Element.Fire, BossCatalog.ObservedTypes(death).Single());
            True(BossCatalog.All.Single(p => p.Id == "harbinger-defilement").Tips.Count > 0, "profile DTO mutations cannot alter catalog state");
        });
    }
    static void Test_ReferenceFacts_ReadableMoveDetailsAreEmbeddedAndStayGuideOnly()
    {
        Eq(null, BossGuideMoves.LoadError);
        var moves = BossCatalog.All.SelectMany(p => BossGuideMoves.For(p.Id)).ToArray();
        Eq(302, moves.Length); Eq(32, moves.Select(m => m.EncounterId).Distinct().Count());
        var combo = BossGuideMoves.For("harbinger-defilement").Single(m => m.Name == "Void Combo Strike");
        Eq("Physical", string.Join(",", combo.Elements.Select(Elements.Name)), "Void in move name does not type it");
        Eq("unknown", combo.Delivery); Eq("community-guide", combo.EvidenceClass);
        True(BossCatalog.All.Single(p => p.Id == combo.EncounterId).Tips.Any(t => t.Contains("Echo Slam")), "authored fight plan is separate from categorical attack facts");
        True(moves.All(m => m.SourceUrl.StartsWith("https://maxroll.gg/", StringComparison.Ordinal)), "per-move source preserved");
    }
    static void Test_ReferenceFacts_InvalidOrMissingEmbeddedCatalogFallsBackWithoutLosingStarter()
    {
        var bad = BossResearchCatalog.LoadJson("{broken", BossCatalog.Starter);
        Eq(5, bad.Profiles.Count); True(bad.Diagnostics.Any(), "parse fault diagnosed");
        var missing = BossResearchCatalog.LoadEmbedded(typeof(BossCatalog).Assembly, "absent.json", BossCatalog.Starter);
        Eq(5, missing.Profiles.Count); True(missing.Diagnostics.Any(), "missing resource diagnosed");
        var document = BossCatalog.Research;
        document.Encounters[1].Id = document.Encounters[0].Id;
        var duplicate = BossResearchCatalog.LoadJson(JsonSerializer.Serialize(document, BossCatalogSchema.Json), BossCatalog.Starter);
        Eq(5, duplicate.Profiles.Count); True(duplicate.Diagnostics.Any(c => c.Contains("validation")), "invalid identities cannot enter runtime");
    }
    static void Test_ReferenceFacts_PlayerCopyAndSourceClaimsDoNotUseDoubleDashesOrFalseAuthority()
    {
        foreach (var profile in BossCatalog.All)
        {
            True(!profile.Name.Contains("--") && !profile.Mechanics.Contains("--") && !profile.DamageEvidence.Contains("--"), profile.Id);
            True(profile.Tips.All(t => !t.Contains("--")), profile.Id + " tips");
            foreach (var claim in profile.Encounter.Claims.Where(c => c.Id.Contains(".maxroll.", StringComparison.Ordinal)))
                True(claim.EvidenceClass != "datamined-reference" && claim.EvidenceClass != "developer-documentation" && claim.EvidenceClass != "captured-game", claim.Id);
        }
    }

    static void Test_FieldNotes_DistributedResourcesContainFactsWithoutImportedGuideProse()
    {
        var assembly = typeof(BossCatalog).Assembly;
        using var catalogStream = assembly.GetManifestResourceStream(BossResearchCatalog.ResourceName);
        using var catalogReader = new System.IO.StreamReader(catalogStream);
        string text = catalogReader.ReadToEnd();
        True(!text.Contains("Quote:") && !text.Contains("Maxroll strategy:"), "no copied quotations or strategy paragraphs in embedded catalog");
        var document = BossCatalog.Research;
        True(document.Encounters.SelectMany(e => e.Claims).All(c => !c.Id.Contains(".strategy.")), "guide strategy claims are removed");
        True(document.Encounters.SelectMany(e => e.Ailments.Items ?? new System.Collections.Generic.List<AilmentRecord>()).All(a => a.Effect == null), "imported ailment prose is absent");
        using var movesStream = assembly.GetManifestResourceStream("medick_DeathCounter.BossGuideMoves.json");
        using var movesReader = new System.IO.StreamReader(movesStream);
        using var moves = JsonDocument.Parse(movesReader.ReadToEnd());
        foreach (var move in moves.RootElement.EnumerateArray())
            foreach (string field in new[] { "tell", "avoid", "note", "quote", "strategy" })
                True(!move.TryGetProperty(field, out _), "imported prose field not distributed: " + field);
    }

    static void Test_FieldNotes_QuickPlansAreShortAuthoredActionsWithQuietReferences()
    {
        foreach (var boss in BossCatalog.All)
        {
            True(!boss.DamageEvidence.Contains("Maxroll"), "no source branding in preparation text");
            True(boss.Tips.Count <= 3, "bounded fight plan");
            foreach (string tip in boss.Tips)
            {
                True(tip.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 28, "one short action per reminder");
                True(!tip.Contains("Maxroll") && !tip.Contains("one shots almost every build"), "no publisher narration or universal survival claims");
            }
            True(BossFieldNotes.Preparation(boss).Count <= 3, "bounded preparation notes");
        }
        var defilement = BossCatalog.All.Single(p => p.Id == "harbinger-defilement");
        True(defilement.Sources.Any(s => s.Url.StartsWith("https://maxroll.gg/")), "factual references retained, not disguised");
        True(defilement.Tips.Any(t => t.Contains("movement cooldown")), "practical player decision, separate from attack table");
    }

    static void Test_FieldNotes_UnknownProfilesDoNotInventResistanceOrWardGoals()
    {
        var notes = BossFieldNotes.Preparation(BossCatalog.All.Single(p => p.Id == "morditas"));
        Eq(1, notes.Count);
        True(notes[0].Contains("not mapped"), "unknown coverage is explicit");
        True(!notes.Any(t => t.Contains("75%") || t.Contains("ward", StringComparison.OrdinalIgnoreCase)), "no guessed goals");
    }

    static void Test_FieldNotes_ShredAndCurseChangePreparationWithoutPromisingSafety()
    {
        var cold = BossFieldNotes.Preparation(BossCatalog.All.Single(p => p.Id == "heorot-monolith"));
        True(cold.Any(t => t.Contains("uncapped Cold resistance")), "resistance buffer is relevant to typed shred");
        var voidNotes = BossFieldNotes.Preparation(BossCatalog.All.Single(p => p.Id == "julra"));
        True(voidNotes.Any(t => t.Contains("uncapped Void resistance")), "Void shred receives a typed response");
        var curse = BossFieldNotes.Preparation(BossCatalog.All.Single(p => p.Id == "aberroth"));
        True(curse.Any(t => t.Contains("Curse of Aberroth") && t.Contains("buffer")), "curse can reopen sheet cap");
        True(!curse.Any(t => t.Contains("cleanse for Curse")), "uncleansable curse is never assigned a cleanse");
        True(curse.All(t => !t.Contains("safe") && !t.Contains("survive")), "preparation is not a fight grade");
    }

    static void Test_FieldNotes_DoTDamageDoesNotReceiveArmorOrCritAsItsCounter()
    {
        var notes = BossFieldNotes.Preparation(BossCatalog.All.Single(p => p.Id == "stone-titans-heart"));
        True(notes.Any(t => t.Contains("Poison") && t.Contains("recovery") && t.Contains("cleanse")), "reported poison changes preparation");
        True(notes.Any(t => t.Contains("Armor and crit protection do not stop")), "hit defenses are not sold as DoT protection");
    }

    static void Test_FieldNotes_ShadeVariantsDoNotBecomeOneCombinedFight()
    {
        var notes = BossFieldNotes.Preparation(BossCatalog.All.Single(p => p.Id == "shade-of-orobyss"));
        True(notes[0].Contains("Void resistance first") && notes[0].Contains("possible variants"), "variant union is not asserted as one encounter");
    }
}
