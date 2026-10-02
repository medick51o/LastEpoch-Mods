using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static DeathRecord HeavyFire(float res = 75) => new()
    {
        Character = "Test", UtcTime = new DateTime(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc),
        MaxHealth = 1000, KillingElement = "Fire", Kind = DeathKind.OneShot,
        Hits = 1, WindowDamage = 950, KillingBlow = 950, KillingCrit = false,
        Defenses = new() { ["Res.Fire"] = res, ["MaxHealth"] = 1000, ["AreaLevel"] = 100 }, DefenseSnapshotAgeSeconds = 0.5f,
    };
    static void Test_Review_ResistanceGapOutranksCritAndSuppressesPool()
    {
        var d = HeavyFire(41); d.KillingCrit = true; d.Defenses["CritAvoidance"] = 20;
        var actions = Advisor.Show(d);
        Eq("res_Fire", actions[0].Key);
        True(actions.Any(t => t.Key == "crit") && actions.All(t => t.Key != "ehp"), "fix measured gaps before changing the pool");
    }
    static void Test_Review_LargeHealthLossWithCapSupportsPoolButNotWardDefault()
    {
        var d = HeavyFire();
        var tip = Advisor.Show(d).Single(t => t.Key == "ehp");
        True(tip.Body.Contains("950") && tip.Body.Contains("resistance gaps are closed"), tip.Body);
        True(!tip.Body.Contains("ward", StringComparison.OrdinalIgnoreCase), "no ward gear suggestion without ward evidence");
        d.Defenses["Ward"] = 400;
        tip = Advisor.Show(d).Single(t => t.Key == "ehp");
        True(tip.Body.Contains("400") && tip.Body.Contains("does not prove sustain") && tip.Body.Contains("If your build already maintains ward"), tip.Body);
        d.Defenses["Ward"] = 0;
        True(!Advisor.Show(d).Single(t => t.Key == "ehp").Body.Contains("Ward was"), "a zero ward reading is not a ward build");
    }
    static void Test_Review_MixedHitNeedsBothResistancesKnownAndCapped()
    {
        var d = HeavyFire(); d.SecondaryKillingElement = "Cold";
        True(!Advisor.Show(d).Any(t => t.Key == "ehp"), "unknown second resistance blocks pool pivot");
        d.Defenses["Res.Cold"] = 40;
        Eq("res_Cold", Advisor.Show(d)[0].Key);
        True(!Advisor.Show(d).Any(t => t.Key == "ehp"), "measured second gap blocks pool pivot");
        d.Defenses["Res.Cold"] = 75;
        True(Advisor.Show(d).Any(t => t.Key == "ehp"), "both gaps closed and large loss supports pivot");
    }
    static void Test_Review_UnverifiedReportNeedsActualOverkillForPoolReview()
    {
        var d = AdviceDeath("Fire", 75); d.KillingBlow = 950;
        True(!Advisor.Show(d).Any(t => t.Key == "ehp"), "a merely large report is not measured health loss");
        d.KillingBlow = 2000;
        True(!Advisor.Show(d).Any(t => t.Key == "ehp"), "large report without overkill still not enough");
        d.OverkillDamage = 500;
        var tip = Advisor.Show(d).Single(t => t.Key == "ehp");
        True(tip.Body.Contains("not been verified") && tip.Body.Contains("No guaranteed"), tip.Body);
        d.KillingAilment = "Ignite";
        True(!Advisor.Show(d).Any(t => t.Key == "ehp"), "DoT tick never becomes a heavy hit pool suggestion");
    }
    static void Test_Review_AttritionIsRecoveryNotAutomaticPoolGrowth()
    {
        var d = HeavyFire(); d.Kind = DeathKind.Attrition; d.Hits = 10; d.WindowSeconds = 10;
        True(Advisor.Show(d).Any(t => t.Key == "sustain"), "sustained health loss points to recovery");
        True(!Advisor.Show(d).Any(t => t.Key == "ehp"), "same total loss spread over time is not a heavy burst");
    }
    static void Test_Current_ClosingResistanceGapUpdatesActions()
    {
        var d = HeavyFire(41);
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["MaxHealth"] = 1400 });
        True(result.Available, result.Message);
        True(result.Changes.Any(s => s.Contains("41% → 75%")), "show change");
        True(result.Actions.All(t => t.Key != "res_Fire"), "do not demand already repaired resistance");
        var tip = result.Actions.Single(t => t.Key == "ehp");
        True(tip.Body.Contains("Current max health is 1,400") && tip.Body.Contains("before adding still more health"), tip.Body);
        True(!tip.Body.Contains("survived"), "larger pool is not proof of survival");
    }
    static void Test_Current_CurrentResistanceRegressionBecomesFirstAction()
    {
        var d = HeavyFire();
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 30, ["MaxHealth"] = 1400 });
        Eq("res_Fire", result.Actions[0].Key);
        True(result.Actions[0].Title.Contains("you have 30%") && result.Actions[0].Body.Contains("You are 45 points"), "current tense and actual gap");
        True(result.Actions.All(t => t.Key != "ehp"), "close present resistance gap first");
    }
    static void Test_Current_UsesOriginalAreaNotTownLevel()
    {
        var result = CurrentAssessment.Build(HeavyFire(40), "Test", new Dictionary<string, float> { ["Res.Fire"] = 40, ["AreaLevel"] = 1 });
        True(result.Message.Contains("original area level 100"), result.Message);
        True(result.Actions.Single(t => t.Key == "res_Fire").Body.Contains("area level 100"), "model the original encounter");
        var d = HeavyFire(40); d.Defenses.Remove("AreaLevel");
        result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 40, ["AreaLevel"] = 1 });
        True(result.Message.Contains("area level is unknown"), result.Message);
        True(!result.Actions.Single(t => t.Key == "res_Fire").Body.Contains("less fire damage"), "no numeric model from unrelated current zone");
    }
    static void Test_Current_MissingCurrentStatsNeverReuseOldDefense()
    {
        var result = CurrentAssessment.Build(HeavyFire(), "Test", new Dictionary<string, float> { ["MaxHealth"] = 1200 });
        True(result.Available, result.Message);
        True(result.Changes.Any(s => s.Contains("75% → unknown now")), "unknown stays unknown");
        Eq("res_Fire", result.Actions[0].Key);
        True(result.Actions[0].Title.Contains("Check"), "missing current value is a check, not an invented gap");
        True(!result.Actions.Any(t => t.Key == "ehp"), "old capped value cannot justify current pool advice");
    }
    static void Test_Current_RejectsOtherCharacterAndUnavailableRead()
    {
        True(!CurrentAssessment.Build(HeavyFire(), "Alt", new Dictionary<string, float> { ["Res.Fire"] = 75 }).Available, "no other character's gear");
        True(!CurrentAssessment.Build(HeavyFire(), "Test", null).Available, "null read");
        True(!CurrentAssessment.Build(HeavyFire(), "Test", new Dictionary<string, float> { ["AreaLevel"] = 100 }).Available, "area alone is not a stat assessment");
        True(!CurrentAssessment.Build(HeavyFire(), "Test", new Dictionary<string, float> { ["MaxHealth"] = float.NaN }).Available, "invalid read");
    }
    static void Test_Current_LeavesHistoryAndCallerStatsUnchanged()
    {
        var d = HeavyFire(41); d.PlayContext = SeasonFive(); d.AilmentsOnYou.Add("Shock");
        var stats = new Dictionary<string, float> { ["Res.Fire"] = 75, ["MaxHealth"] = 1400, ["AreaLevel"] = 1 };
        string before = JsonSerializer.Serialize(d), statsBefore = JsonSerializer.Serialize(stats);
        var result = CurrentAssessment.Build(d, "Test", stats, new[] { d });
        Eq(before, JsonSerializer.Serialize(d)); Eq(statsBefore, JsonSerializer.Serialize(stats));
        stats["Res.Fire"] = 20;
        True(result.Changes.Any(s => s.Contains("41% → 75%")), "result owns click-time snapshot");
        Eq("Season 5", d.PlayContextLabel());
    }
    static void Test_Current_UpdatedCritProtectionDoesNotCreateFalseConflict()
    {
        var d = HeavyFire(); d.KillingCrit = true; d.Defenses["CritAvoidance"] = 0;
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["CritAvoidance"] = 100 });
        True(!result.Actions.Any(t => t.Group == "crit"), "a former crit is not a conflict with newly acquired avoidance");
        d.AilmentsOnYou.Add("Critical Vulnerability");
        result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["CritAvoidance"] = 100 });
        True(result.Actions.Any(t => t.Group == "crit" && t.Body.Contains("Critical Vulnerability")), "old vulnerability can recur; warn conditionally");
        result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["ReducedBonusCritDamage"] = 100 });
        True(!result.Actions.Any(t => t.Group == "crit"), "bonus protection can close that specific gap");
    }
    static void Test_Current_OldDebuffsCannotBecomeCurrentStackCounts()
    {
        var d = HeavyFire(); d.AilmentsOnYou.Add("Marked for Death");
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["ResUncapped.Fire"] = 90 });
        True(result.Message.Contains("old debuffs may return"), result.Message);
        var tip = result.Actions.Single(t => t.Key == "res_Fire");
        True(tip.Body.Contains("Current uncapped resistance is 90%") && tip.Body.Contains("exact lost resistance were not captured"), tip.Body);
        True(!result.Actions.Any(t => t.Key == "ehp"), "active-debuff headroom question comes before pool");
    }
    static void Test_Current_UnknownCauseDoesNotInventGearAdvice()
    {
        var d = new DeathRecord { Character = "Test", Kind = DeathKind.Unknown };
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 20, ["Ward"] = 10000, ["MaxHealth"] = 1000 });
        Eq(1, result.Actions.Count); Eq("unknown", result.Actions[0].Key);
        True(!result.Actions[0].Body.Contains("ward", StringComparison.OrdinalIgnoreCase), "huge current ward does not identify an unknown cause");
    }
    static void Test_Current_CardsStayDistinctAndAtMostThree()
    {
        var d = HeavyFire(40); d.SecondaryKillingElement = "Cold"; d.KillingCrit = true;
        d.AilmentsOnYou.AddRange(new[] { "Freeze", "Stun", "Marked for Death" });
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 40, ["Res.Cold"] = 20, ["CritAvoidance"] = 0, ["MaxHealth"] = 1000 });
        Eq(3, result.Actions.Count); Eq(3, result.Actions.Select(t => t.Group).Distinct().Count());
        True(result.Actions.Take(2).All(t => t.Group.StartsWith("res:")), "measured gaps first");
    }
    static void Test_Current_ResistanceChangeModelsOnlyTypedRatio()
    {
        var d = HeavyFire(41); d.KillingBlow = 90000; d.OverkillDamage = 50000; d.DetailSource = "game death report";
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["MaxHealth"] = 1500 });
        string change = result.Changes.Single(s => s.StartsWith("Fire resistance"));
        True(change.Contains("25% less fire damage") && change.Contains("other modifiers unchanged"), change);
        True(!change.Contains("survived") && !change.Contains("90,000") && !change.Contains("50,000"), "no invented fatal damage or survival calculation");
        result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 20 });
        True(result.Changes.Single(s => s.StartsWith("Fire resistance")).Contains("more fire damage"), "show regression as increased damage, not a negative benefit");
    }
    static void Test_Current_StaleOriginalAreaCannotEnableNumericComparison()
    {
        var d = HeavyFire(41); d.DefenseSnapshotAgeSeconds = 10;
        var result = CurrentAssessment.Build(d, "Test", new Dictionary<string, float> { ["Res.Fire"] = 75, ["AreaLevel"] = 100 });
        True(result.Message.Contains("original area level is unknown"), result.Message);
        True(result.Changes.Single(s => s.StartsWith("Fire resistance")).Contains("unknown at death"), "stale resistance is not a reliable comparison");
        True(result.Changes.All(s => !s.Contains("Models")), "current area cannot replace stale original evidence");
    }
    static void Test_Current_InvalidPoolReadDoesNotLookLikeLostGear()
    {
        var result = CurrentAssessment.Build(HeavyFire(), "Test", new Dictionary<string, float> { ["MaxHealth"] = -100, ["Ward"] = -1 });
        True(!result.Available, "invalid pool reads remain unavailable");
    }
    static void Test_Review_FreezeDoesNotTurnIntoDefaultWardAdvice()
    {
        var d = new DeathRecord { AilmentsOnYou = new() { "Freeze" } };
        var tip = Advisor.Show(d).Single(t => t.Key == "cc_freeze");
        True(tip.Body.Contains("freeze protection") && !tip.Body.Contains("ward", StringComparison.OrdinalIgnoreCase), tip.Body);
    }
}
