using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static DeathRecord AdviceDeath(string element = "Fire", float resistance = 41) => new()
    {
        Character = "Test", UtcTime = new DateTime(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc),
        Kind = DeathKind.Reported, DetailSource = "game death report", KillingElement = element,
        KillingBlow = 950, MaxHealth = 1000, Defenses = new() { ["Res." + element] = resistance },
    };

    static void Test_Rules_UnknownCritCannotOverwriteKnownFlag()
    {
        var d = AdviceDeath(); d.KillingCrit = true;
        new DeathDetails { Damage = 950 }.Apply(d);
        Eq(true, d.KillingCrit);
        new DeathDetails { Crit = false }.Apply(d);
        Eq(false, d.KillingCrit); // an explicit noncrit is different from missing
        var empty = new DeathRecord(); new DeathDetails().Apply(empty);
        Eq(null, empty.KillingCrit);
    }
    static void Test_Rules_ReportContextSurvivesRoundTripAndMissingFallback()
    {
        var d = AdviceDeath();
        new DeathDetails { IsBossFight = true, IrregularSource = "UnmappedSource" }.Apply(d);
        new DeathDetails().Apply(d);
        var restored = JsonSerializer.Deserialize<DeathRecord>(JsonSerializer.Serialize(d));
        Eq(true, restored.IsBossFight); Eq("UnmappedSource", restored.IrregularSource);
        True(!Advisor.Show(d).Any(t => t.Body.Contains("ground effect")), "unmapped source is not a hazard classification");
    }
    static void Test_Rules_SecondaryTypeGetsItsOwnMeasuredGap()
    {
        var d = AdviceDeath(); d.SecondaryKillingElement = "Cold"; d.Defenses["Res.Cold"] = 30;
        var tips = Advisor.Show(d);
        True(tips.Any(t => t.Key == "res_Fire") && tips.Any(t => t.Key == "res_Cold"), "both lethal types considered");
        True(tips.All(t => !t.Body.Contains("475")), "never split a mixed hit evenly");
    }
    static void Test_Rules_KillingAilmentSuppliesTypeWithoutInventedTimeline()
    {
        var d = AdviceDeath(); d.KillingElement = null; d.KillingAilment = "Ignite";
        var tips = Advisor.Show(d);
        True(tips.Any(t => t.Key == "res_Fire"), "known ailment type used");
        True(tips.Any(t => t.Key == "dot_Ignite"), "recovery also supported");
        Eq(0, d.Hits); Eq(0f, d.WindowDamage);
    }
    static void Test_Rules_ResistanceMathDoesNotUseCharacterLevel()
    {
        var d = AdviceDeath(); d.Level = 93;
        string missing = Advisor.Show(d).First(t => t.Key == "res_Fire").Body;
        True(!missing.Contains("less fire damage"), "no numeric ratio without area level");
        d.Defenses["AreaLevel"] = 100;
        string known = Advisor.Show(d).First(t => t.Key == "res_Fire").Body;
        True(known.Contains("25% less fire damage") && known.Contains("area level 100"), known);
        // The comparable-source ratio is never a survival claim. Only the
        // separate exact-hit preview may judge survival, and only with its
        // stated assumption about the report units.
        string ratioPart = known.Split("For this exact reported hit:")[0];
        True(!ratioPart.Contains("survived") && !ratioPart.Contains("saved"), "ratio is not a survival claim");
        if (known.Contains("would likely have survived"))
            True(known.Contains("assuming the reported damage was the whole hit after your defenses, including ward"), "survival preview states its assumption");
    }
    static void Test_Rules_ShockAndResistanceGapShareOneAction()
    {
        var d = AdviceDeath("Lightning", 50); d.AilmentsOnYou.Add("Shock"); d.Defenses["AreaLevel"] = 100;
        var tips = Advisor.Suggest(d, max: 100);
        var res = tips.Single(t => t.Group == "res:Lightning");
        True(res.Body.Contains("Shock") && res.Body.Contains("25 point"), res.Body);
        True(!tips.Any(t => t.Key == "cc_shock"), "no duplicate shock resistance card");
        True(res.Body.Contains("20% less lightning damage"), "snapshot already includes shred; do not apply it a second time");
    }
    static void Test_Rules_OvercapNeedsRecordedDebuff()
    {
        var d = AdviceDeath("Lightning", 75);
        True(!Advisor.Show(d).Any(t => t.Group == "res:Lightning"), "capped resistance is not an action");
        d.AilmentsOnYou.Add("Shock");
        var res = Advisor.Show(d).Single(t => t.Group == "res:Lightning");
        True(res.Body.Contains("uncapped resistance can absorb") && res.Body.Contains("does not offset area-level penetration"), res.Body);
    }
    static void Test_Rules_TypedShredRetainsElementAndIsNotDot()
    {
        foreach (Element e in Enum.GetValues<Element>())
        {
            string n = Elements.Name(e);
            var a = Ailments.Find(n + "ResShred");
            Eq(n + " Resistance Shred", a?.Name); Eq(e, a.Element); True(!a.IsDot, "shred is not a DoT");
            Eq(n + " Resistance Shred", Ailments.Find("Shred " + n + " Resistance")?.Name);
        }
        Eq(null, Ailments.Find("FireResistanceShredChance"));
    }
    static void Test_Rules_TypedShredOnlyChangesMatchingResistance()
    {
        var d = AdviceDeath("Cold", 75); d.AilmentsOnYou.Add("Fire Resistance Shred");
        True(!Advisor.Show(d).Any(t => t.Group == "res:Cold"), "fire shred is not cold shred");
        d.AilmentsOnYou.Add("Cold Resistance Shred");
        var res = Advisor.Show(d).Single(t => t.Group == "res:Cold");
        True(res.Body.Contains("2 points") && res.Body.Contains("up to 10 stacks"), res.Body);
    }
    static void Test_Rules_MarkedForDeathMergesIntoMeasuredGap()
    {
        var d = AdviceDeath(); d.AilmentsOnYou.Add("Marked for Death");
        var tips = Advisor.Suggest(d, max: 100);
        var res = tips.Single(t => t.Group == "res:Fire");
        True(res.Body.Contains("Marked for Death") && res.Body.Contains("25 points"), res.Body);
        True(!tips.Any(t => t.Key == "marked"), "no repeated resistance prescription");
    }
    static void Test_Rules_GenericShredDoesNotInventElement()
    {
        var d = new DeathRecord { AilmentsOnYou = new() { "Resistance Shred" } };
        var tip = Advisor.Show(d).Single();
        Eq("shred_res", tip.Key); True(tip.Body.Contains("element") && tip.Body.Contains("not preserved"), tip.Body);
        True(!tip.Body.Contains("fire"), "no fabricated type");
    }
    static void Test_Rules_CritReductionCapDoesNotDemandAvoidance()
    {
        var d = AdviceDeath(); d.KillingCrit = true; d.Defenses["ReducedBonusCritDamage"] = 100;
        True(!Advisor.Show(d).Any(t => t.Group == "crit"), "protected crit flag is not an avoidance gap");
        True(Advisor.Suggest(d, max: 100).Single(t => t.Key == "crit_check").IsNotice, "explanation is a fact, not another gear action");
    }
    static void Test_Rules_CritBonusMathUsesRecordedProtection()
    {
        var d = AdviceDeath(); d.KillingCrit = true; d.Defenses["ReducedBonusCritDamage"] = 50;
        var tip = Advisor.Show(d).Single(t => t.Key == "crit");
        Eq("crit", tip.Key); True(tip.Body.Contains("33%") && tip.Body.Contains("50%"), tip.Body);
        True(!tip.Body.Contains("survived"), "ratio does not promise survival");
    }
    static void Test_Rules_CritVulnerabilityCannotInventBonusProtectionGap()
    {
        var d = AdviceDeath(); d.KillingCrit = true; d.Defenses["ReducedBonusCritDamage"] = 100;
        d.AilmentsOnYou.Add("Critical Vulnerability");
        True(!Advisor.Show(d).Any(t => t.Group == "crit"), "vulnerability does not override recorded full bonus protection");
    }
    static void Test_Rules_CritVulnerabilityAvoidsDuplicateCritCards()
    {
        var d = AdviceDeath(); d.KillingCrit = true; d.Defenses["CritAvoidance"] = 100;
        d.AilmentsOnYou.Add("Critical Vulnerability");
        var tip = Advisor.Show(d).Single(t => t.Group == "crit");
        True(tip.Title.Contains("Critical Vulnerability"), tip.Title);
        True(!Advisor.Confidence(d).Contains("conflicts with snapshot avoidance"), "recorded vulnerability gives relevant context");
    }
    static void Test_Rules_CritOnDotIsConflictNotCritGearAdvice()
    {
        var d = AdviceDeath(); d.KillingAilment = "Ignite"; d.KillingCrit = true;
        True(!Advisor.Show(d).Any(t => t.Group == "crit"), "DoTs cannot crit");
        True(Advisor.Confidence(d).StartsWith("Low:") && Advisor.Confidence(d).Contains("conflicts with damage over time"), "name contradictory evidence");
    }
    static void Test_Rules_RecoveryIsOneCardAcrossMultipleAilments()
    {
        var d = AdviceDeath("Poison", 75); d.KillingAilment = "Poison";
        d.AilmentsOnYou.AddRange(new[] { "Ignite", "Poison", "Damned", "Damned" });
        var tips = Advisor.Suggest(d, max: 100);
        var recovery = tips.Single(t => t.Group == "recovery");
        True(recovery.Body.Contains("Damned cuts your health regen") && recovery.Body.Contains("leech or potions"), recovery.Body);
        True(!recovery.Body.Contains("bring it to 75%"), "recovery does not repeat the resistance action");
        True(!tips.Any(t => t.Key == "armor" || t.Key == "avoid" || t.Key == "ehp"), "do not treat a DoT tick as a hit");
    }
    static void Test_Rules_ChillIsDifferentFromFreeze()
    {
        var d = AdviceDeath("Cold", 75); d.AilmentsOnYou.Add("Chill");
        var tip = Advisor.Show(d).Single(t => t.Group == "control");
        Eq("cc_chill", tip.Key); True(!tip.Body.Contains("lower freeze chance") && !tip.Body.Contains("More max health"), "health does not prevent chill");
        d.AilmentsOnYou.Add("Freeze");
        Eq("cc_freeze", Advisor.Show(d).Single(t => t.Group == "control").Key);
    }
    static void Test_Rules_UnknownAvoidanceIsDifferentFromZero()
    {
        var d = AdviceDeath();
        True(!Advisor.Suggest(d, max: 100).Any(t => t.Key == "avoid"), "no missing-as-zero claim");
        d.Defenses["Dodge"] = 0; d.Defenses["Block"] = 0;
        True(Advisor.Suggest(d, max: 100).Any(t => t.Key == "avoid"), "both recorded zero support a layer check");
    }
    static void Test_Rules_EnduranceCapAndWardUncertainty()
    {
        var d = AdviceDeath(); d.KillingAilment = "Ignite"; d.Defenses["Endurance"] = 60;
        True(!Advisor.Suggest(d, max: 100).Any(t => t.Key == "endurance"), "do not increase a capped reduction");
        d.Defenses["Endurance"] = 20;
        var tip = Advisor.Suggest(d, max: 100).Single(t => t.Key == "endurance");
        True(tip.Body.Contains("does not protect ward") && tip.Body.Contains("below that threshold"), tip.Body);
    }
    static void Test_Rules_BurstQuotesSeparateBlockChanceAndEffectiveness()
    {
        var d = AdviceDeath(); d.Kind = DeathKind.Burst; d.Hits = 4; d.WindowDamage = 900;
        d.Defenses["AreaLevel"] = 100; d.Defenses["Dodge"] = 1000;
        d.Defenses["Block"] = 50; d.Defenses["BlockEffectiveness"] = 325;
        var tip = Advisor.Suggest(d, max: 100).Single(t => t.Key == "avoid");
        True(tip.Body.Contains("29% dodge") && tip.Body.Contains("50%") && tip.Body.Contains("22% reduction"), tip.Body);
    }
    static void Test_Rules_StaleSnapshotsCannotPrescribeGaps()
    {
        var d = AdviceDeath(); d.DefenseSnapshotAgeSeconds = 3;
        var tip = Advisor.Show(d).Single(t => t.Key == "res_Fire");
        True(!tip.Body.Contains("34 point") && tip.Body.Contains("not captured"), "stale stats not used as death-time evidence");
        True(Advisor.Confidence(d).Contains("stale defense snapshot"), "timing limitation visible");
    }
    static void Test_Rules_HistoryExcludesOtherCharactersAndFutureDeaths()
    {
        var d = AdviceDeath(); d.Killer = "Heorot";
        var history = Enumerable.Range(0, 3).Select(i => new DeathRecord { Killer = "Heorot", Character = "Other", UtcTime = d.UtcTime.AddMinutes(-i) }).ToList();
        history.AddRange(Enumerable.Range(0, 3).Select(i => new DeathRecord { Killer = "Heorot", Character = "Test", UtcTime = d.UtcTime.AddMinutes(1 + i) }));
        True(!Advisor.Suggest(d, history, 100).Any(t => t.Group == "pattern"), "history stays on this character at this death");
    }
    static void Test_Rules_HistoryStopsAtTwentyAndChoosesOneBehavior()
    {
        var d = AdviceDeath(); d.Killer = "Old boss";
        var history = Enumerable.Range(0, 30).Select(i => new DeathRecord { Character = "Test", Killer = i < 20 ? "New boss" : "Old boss", UtcTime = d.UtcTime.AddMinutes(-i), Number = 30 - i }).ToList();
        True(!Advisor.Suggest(d, history, 100).Any(t => t.Key == "nemesis"), "old deaths outside last twenty excluded");
        d.Killer = "New boss"; d.KillerAbility = "Nova"; d.IsBossFight = true;
        foreach (var r in history.Take(3)) r.KillerAbility = "Nova";
        var behavior = Advisor.Suggest(d, history, 100).Single(t => t.Group == "pattern");
        Eq("repeat_ability", behavior.Key); True(behavior.Body.Contains("3 of your last 20"), behavior.Body);
    }
    static void Test_Rules_RepeatedElementIsContextNotExtraCard()
    {
        var d = AdviceDeath();
        var history = Enumerable.Range(0, 4).Select(i => { var r = AdviceDeath(); r.UtcTime = d.UtcTime.AddMinutes(-i); return r; }).ToList();
        var tips = Advisor.Suggest(d, history, 100);
        True(tips.Single(t => t.Group == "res:Fire").Body.Contains("4 of your last 4 deaths"), "measured repeated gap folded into resistance");
        Eq(1, tips.Count(t => t.Group == "res:Fire"));
    }
    static void Test_Rules_ConfidenceNeedsFreshRelevantStats()
    {
        var d = AdviceDeath(); d.Hits = 4; d.KillingCrit = false; d.DefenseSnapshotAgeSeconds = 0.5f;
        True(Advisor.Confidence(d).StartsWith("High:"), Advisor.Confidence(d));
        d.SecondaryKillingElement = "Cold";
        True(Advisor.Confidence(d).StartsWith("Medium:") && Advisor.Confidence(d).Contains("partial"), "missing secondary defense lowers coverage");
        d.Defenses["Res.Cold"] = 75; d.KillingCrit = true; d.Defenses["CritAvoidance"] = 100;
        True(Advisor.Confidence(d).StartsWith("Low:") && Advisor.Confidence(d).Contains("conflicts with snapshot avoidance"), "conflict overrides coverage");
    }
    static void Test_Rules_PatternsNeverPromoteCappedNotices()
    {
        var records = Enumerable.Range(0, 4).Select(_ => AdviceDeath("Fire", 75)).ToList();
        var p = DeathPatterns.Build(records, maxPriorities: 100);
        True(p.Priorities.All(t => t.Advice.Key != "res_Fire" && t.Advice.Key != "none"), "no capped resistance or fallback build priority");
    }
    static void Test_Rules_CardsHaveUniqueGroupsAndStableOrder()
    {
        var d = AdviceDeath(); d.KillingCrit = true; d.SecondaryKillingElement = "Cold"; d.Defenses["Res.Cold"] = 40;
        d.AilmentsOnYou.AddRange(new[] { "Freeze", "Chill", "Marked for Death", "Stun" });
        var first = Advisor.Show(d); var second = Advisor.Show(d);
        Eq(3, first.Count); Eq(first.Count, first.Select(t => t.Group).Distinct().Count());
        Eq(string.Join(",", first.Select(t => t.Key)), string.Join(",", second.Select(t => t.Key)));
        Eq("res_Cold", first[0].Key); True(first.All(t => !t.IsNotice), "measured resistance gaps come first and facts never take action slots");
    }
    static void Test_Rules_NewSnapshotValuesKeepTheirUnits()
    {
        var snapshot = DefenseSnapshot.Normalize(new Dictionary<string, float>
        {
            ["Res.Fire"] = 0.41f, ["ReducedBonusCritDamage"] = 0.5f,
            ["StunAvoidance"] = 1000, ["BlockEffectiveness"] = 325, ["AreaLevel"] = 100,
        });
        Eq(41f, snapshot["Res.Fire"]); Eq(50f, snapshot["ReducedBonusCritDamage"]);
        Eq(325f, snapshot["BlockEffectiveness"]); Eq(100f, snapshot["AreaLevel"]); Eq(1000f, snapshot["StunAvoidance"]);
    }
    static void Test_Rules_LegacyElementCaseAndNullMixAreSafe()
    {
        var d = AdviceDeath(); d.KillingElement = "fire"; d.DamageByElement = null;
        var tip = Advisor.Show(d).Single(t => t.Key == "res_Fire");
        True(tip.Body.Contains("Fire damage") && !tip.Body.Contains("NaN"), tip.Body);
        d.Hits = 1; d.KillingCrit = false; d.DefenseSnapshotAgeSeconds = 0.5f;
        True(Advisor.Confidence(d).StartsWith("High:"), "case-normalized relevant defense lookup");
    }
}
