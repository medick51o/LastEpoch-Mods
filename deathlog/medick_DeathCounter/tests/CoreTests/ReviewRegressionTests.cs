using System;
using System.Linq;
using medick_DeathCounter.Core;

static partial class Program
{
    static DeathRecord MixedResistanceDeath()
    {
        var death = AdviceDeath("Void", 2);
        foreach (var element in new[] { Element.Fire, Element.Cold, Element.Lightning })
        {
            death.DamageByElement[(int)element] = 100;
            death.Defenses["Res." + Elements.Name(element)] = 74;
        }
        return death;
    }
    static void Test_Review_KillingGapSurvivesThreeActionLimit()
    {
        var death = MixedResistanceDeath();
        var actions = Advisor.Show(death);
        Eq(3, actions.Count);
        Eq("res_Void", actions[0].Key);
        Eq(3, actions.Select(a => a.Group).Distinct().Count());
        var assessment = CurrentAssessment.Build(death, death.Character, death.Defenses);
        Eq("res_Void", assessment.Actions[0].Key);
    }
    static void Test_Review_ResistanceGapsSortBySeverityWithinCausePriority()
    {
        var death = MixedResistanceDeath();
        death.Defenses["Res.Fire"] = 10;
        death.Defenses["Res.Cold"] = 20;
        death.Defenses["Res.Lightning"] = 30;
        death.Defenses["Res.Void"] = 74;
        Eq("res_Void,res_Fire,res_Cold", string.Join(",", Advisor.Show(death).Select(a => a.Key)));
        death.SecondaryKillingElement = "Cold";
        Eq("res_Cold,res_Void,res_Fire", string.Join(",", Advisor.Show(death).Select(a => a.Key)));
    }
    static void Test_Review_CappedOrUnknownKillingResistanceIsNotInventedGap()
    {
        var death = MixedResistanceDeath();
        death.Defenses["Res.Void"] = 75;
        True(!Advisor.Show(death).Any(a => a.Key == "res_Void"), "capped notice is not a gear action");
        death.Defenses.Remove("Res.Void");
        var unknown = Advisor.Suggest(death, max: 100).Single(a => a.Key == "res_Void");
        Eq("Check void resistance", unknown.Title);
        True(!unknown.Body.Contains("short of"), "unknown remains unknown");
    }
    static void Test_Review_UnnamedDotCannotGetArmorOrCritAdvice()
    {
        var death = AdviceDeath("Physical", 75);
        death.Kind = DeathKind.DamageOverTime; death.KillingCrit = true;
        death.Defenses["Armor"] = 100; death.Defenses["CritAvoidance"] = 0;
        death.AilmentsOnYou.Add("Armor Shred");
        foreach (bool timeline in new[] { false, true })
        {
            if (timeline) { death.Hits = 10; death.WindowDamage = 1000; death.DotDamage = 1000; }
            var actions = Advisor.Show(death);
            True(actions.All(a => a.Group != "armor" && a.Group != "crit" && a.Group != "avoidance" && a.Group != "pool"), "DoT ticks are not hits");
            var recovery = actions.Single(a => a.Group == "recovery");
            True(timeline || !recovery.Body.Contains("half the recorded"), "do not invent a measured timeline");
        }
    }
    static void Test_Review_MixedTimelineKeepsSupportedCritAdvice()
    {
        var death = AdviceDeath("Fire", 75);
        death.Kind = DeathKind.DamageOverTime; death.KillingCrit = true;
        death.Hits = 5; death.WindowDamage = 1000; death.DotDamage = 600;
        death.Defenses["CritAvoidance"] = 0;
        True(Advisor.Show(death).Any(a => a.Group == "crit"), "measured non-DoT component and explicit crit survive majority-DoT classification");
        death.KillingAilment = "Ignite";
        True(!Advisor.Show(death).Any(a => a.Group == "crit"), "explicit DoT killing ailment still conflicts with crit");
    }
    static void Test_Review_PhysicalHitAdviceStillWorks()
    {
        var death = AdviceDeath("Physical", 75);
        death.Kind = DeathKind.Burst; death.Hits = 4; death.WindowDamage = 500; death.DotDamage = 100;
        death.DamageByElement[(int)Element.Physical] = 500;
        death.DotByElement[(int)Element.Physical] = 100;
        True(Advisor.Suggest(death, max: 100).Any(a => a.Key == "armor"), "mixed hit-dominated timeline retains hit mitigation");
    }
    static void Test_Review_SavedResistanceCardsUseFrozenCauseAndArea()
    {
        InAssessmentDirectory(dir =>
        {
            var death = HeavyFire(41); death.ZoneLevel = 70;
            var updates = new ReassessmentLog(dir); updates.Load();
            var saved = SaveUpdate(updates, death, NewGear(60));
            var before = ResistanceReview.Build(saved.CurrentStats, saved.OriginalCapture, saved.OriginalCapture.Defenses);
            new DeathDetails { PrimaryElement = "Void", Damage = 2000 }.Apply(death);
            death.ZoneLevel = 100; death.Defenses["Res.Fire"] = 1;
            var reloaded = new ReassessmentLog(dir); reloaded.Load(); saved = reloaded.All.Single();
            var after = ResistanceReview.Build(saved.CurrentStats, saved.OriginalCapture, saved.OriginalCapture.Defenses);
            True(after.Single(r => r.Element == Element.Fire).KillingType, "saved update retains original fire relevance");
            True(!after.Single(r => r.Element == Element.Void).Relevant, "later void cause stays out of saved update");
            Eq(41f, after.Single(r => r.Element == Element.Fire).Previous.Value);
            Eq(before.Single(r => r.Element == Element.Fire).ExtraDamageAtArea, after.Single(r => r.Element == Element.Fire).ExtraDamageAtArea);
        });
    }
}
