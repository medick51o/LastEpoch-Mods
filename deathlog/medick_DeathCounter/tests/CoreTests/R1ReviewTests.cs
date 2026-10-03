using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static DeathRecord ReportedFireDeath(float res, float damage, float overkill, string secondary = null)
    {
        var d = new DeathRecord { Character = "Review", MaxHealth = 1800, ZoneLevel = 100, UtcTime = DateTime.UtcNow,
            Defenses = new() { ["Res.Fire"] = res, ["MaxHealth"] = 1800 }, DefenseSnapshotAgeSeconds = 0.5f };
        new DeathDetails { PrimaryElement = "Fire", SecondaryElement = secondary, Damage = damage, Overkill = overkill, Killer = "Ash Wraith" }.Apply(d);
        return d;
    }

    static void Test_CauseTitle_BlankTimelineFieldsFallBackToAttacker()
    {
        var hit = new HitEvent { Time = 10, Amount = 900, Source = "Bone Golem", MaxHealth = 1000, HealthBefore = 900 };
        var d = DeathAnalyzer.Analyze(new List<HitEvent> { hit }, 10, new DeathContext { Character = "Review", MaxHealth = 1000 });
        Eq("", d.KillerAbility); Eq("", d.KillingAilment);
        Eq("Killed by Bone Golem", d.CauseTitle());
        True(!d.CauseTitleNamesAttack(), "attacker is already in the headline");
        Eq("Not recorded", d.KillingAilmentLabel());

        var tick = new HitEvent { Time = 10, Amount = 300, Source = "Brute", Ailment = "Ignite", IsDot = true, MaxHealth = 1000, HealthBefore = 300 };
        var dot = DeathAnalyzer.Analyze(new List<HitEvent> { tick }, 10, new DeathContext { Character = "Review", MaxHealth = 1000 });
        Eq("Ignite", dot.CauseTitle());
        True(dot.CauseTitleNamesAttack(), "attacker gets its own line under an ailment headline");
        Eq("Cause not recorded", new DeathRecord { KillerAbility = " ", Killer = "" }.CauseTitle());
        Eq("Spear Beam", new DeathRecord { KillerAbility = "Spear Beam", Killer = "Harbinger of Pride" }.CauseTitle());
    }

    static void Test_CauseTitle_IsNotWrittenToHistoryJson()
    {
        string json = JsonSerializer.Serialize(new DeathRecord { Character = "Review", Killer = "Bone Golem" });
        True(!json.Contains("CauseTitle") && !json.Contains("KillingAilmentLabel"), json);
    }

    static void Test_ExactHit_ResistanceCardPreviewsReportedHit()
    {
        // 41% fire at area level 100: effective -34%, taken 1.34x. Capping
        // gives 1.00x, so 2,000 becomes about 1,490, below the 1,700 left.
        var body = Advisor.Suggest(ReportedFireDeath(41, 2000, 300)).Single(t => t.Key == "res_Fire").Body;
        True(body.Contains("For this exact reported hit: capping fire resistance would have reduced this hit from about 2,000 to about 1,490"), body);
        True(body.Contains("would likely have survived, assuming the reported damage was the whole hit after your defenses, including ward"), body);

        body = Advisor.Suggest(ReportedFireDeath(41, 2000, 1000)).Single(t => t.Key == "res_Fire").Body;
        True(body.Contains("still above the roughly 1,000 you had left, so it would probably still have killed you"), body);
    }

    static void Test_ExactHit_NoPreviewWithoutSingleTypeReportOrInCurrentMode()
    {
        True(!Advisor.Suggest(ReportedFireDeath(41, 2000, 300, "Cold")).Single(t => t.Key == "res_Fire").Body.Contains("exact reported hit"), "mixed damage cannot be split");
        True(!Advisor.Suggest(ReportedFireDeath(41, 2000, 2000)).Single(t => t.Key == "res_Fire").Body.Contains("exact reported hit"), "overkill equal to damage leaves no health budget");
        True(!Advisor.Suggest(ReportedFireDeath(41, 2000, 0)).Single(t => t.Key == "res_Fire").Body.Contains("exact reported hit"), "missing overkill is not a full health budget");
        var textOnly = new DeathRecord { Character = "Review", KillingElement = "Fire", KillingBlow = 2000, ZoneLevel = 100, Defenses = new() { ["Res.Fire"] = 41 } };
        new DeathDetails { Text = "Unparsed translated report", TextOnly = true }.Apply(textOnly);
        True(!Advisor.Suggest(textOnly).Single(t => t.Key == "res_Fire").Body.Contains("exact reported hit"), "text-only report keeps timeline units out");
        var d = ReportedFireDeath(41, 2000, 300);
        True(!AdviceRules.Evaluate(d, null, current: true).Single(t => t.Key == "res_Fire").Body.Contains("exact reported hit"), "current stats never replay the old hit");
        var timeline = new DeathRecord { Character = "Review", KillingElement = "Fire", KillingBlow = 2000, OverkillDamage = 300, ZoneLevel = 100,
            Defenses = new() { ["Res.Fire"] = 41 } };
        True(!Advisor.Suggest(timeline).Single(t => t.Key == "res_Fire").Body.Contains("exact reported hit"), "health-loss timeline numbers are not report units");
    }

    static void Test_ExactHit_CurrentAssessmentDoesNotReplayOldHit()
    {
        // Current gear differs in more than one resistance, so Assess my
        // current gear keeps the typed ratio only (v0.1.9 rule).
        var d = ReportedFireDeath(41, 2000, 300);
        var stats = new Dictionary<string, float> { ["Res.Fire"] = 75, ["MaxHealth"] = 1800 };
        var line = CurrentAssessment.Build(d, "Review", stats).Changes.Single(c => c.StartsWith("Fire resistance:"));
        True(line.Contains("25% less fire damage") && !line.Contains("exact reported hit") && !line.Contains("2,000") && !line.Contains("survived"), line);
    }

    static void Test_ExactHit_BufferCardStatesReportedShortfall()
    {
        var d = ReportedFireDeath(75, 2400, 400);
        var ehp = Advisor.Suggest(d, max: int.MaxValue).Single(t => t.Key == "ehp").Body;
        True(ehp.Contains("you were about 400 health and ward short of surviving this exact hit"), ehp);
        True(ehp.Contains("No guaranteed survival amount"), "keeps the existing limit");
    }

    static void Test_BossSources_AttributeGuidePagesInPlainText()
    {
        var defilement = BossCatalog.All.Single(p => p.Id == "harbinger-defilement");
        var lines = BossFieldNotes.SourceLines(defilement);
        True(lines.Any(l => l.Contains("maxroll.gg/last-epoch/resources/harbinger-of-defilement-boss-guide") && l.Contains("(guide updated 2026-09-25)")), string.Join(" | ", lines));
        foreach (var boss in BossCatalog.All)
        {
            var all = BossFieldNotes.SourceLines(boss);
            Eq(all.Count, all.Distinct(StringComparer.OrdinalIgnoreCase).Count(), boss.Id + " distinct");
            True(all.All(l => !l.Contains("https://") && !l.Contains("--") && l.Contains(": ")), boss.Id);
            True(BossGuideMoves.For(boss.Id).Count == 0 || all.Count > 0, boss.Id + " attack facts have a source line");
        }
        Eq(0, BossFieldNotes.SourceLines(null).Count);
    }
}
