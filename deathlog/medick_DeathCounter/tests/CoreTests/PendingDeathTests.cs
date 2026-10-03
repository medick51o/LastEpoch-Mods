using System;
using System.Collections.Generic;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_Pending_LateCounterNeverUsesTownAsDeathSnapshot()
    {
        var town = Ctx(); town.Zone = "Town"; town.RawSceneId = "Town_1"; town.ZoneLevel = 1;
        town.PlayContext = SeasonFive(); town.MaxHealth = 5000; town.GameDeathInfo = "Previous death text";
        var pending = new PendingDeath(new[] { Hit(10, 1, "Unrelated", Element.Fire) }, 10, town,
            new[] { "Shock" }, new() { ["Res.Fire"] = 75 }, 10, liveContext: false);
        Eq("Medick", pending.Record.Character); Eq("", pending.Record.Zone); Eq(null, pending.Record.RawSceneId);
        Eq(null, pending.Record.ZoneLevel); Eq(null, pending.Record.PlayContext); Eq(null, pending.Record.Defenses);
        Eq(0, pending.Record.Hits); Eq(0f, pending.Record.MaxHealth); Eq("", pending.Record.GameDeathInfo);
        Eq(0, pending.Record.AilmentsOnYou.Count);
    }
    static void Test_Pending_FreezesOriginalZoneCharacterSeasonAndHitsBeforeUnload()
    {
        var context = Ctx(); context.PlayContext = SeasonFive(); context.Zone = "Original boss arena"; context.Hardcore = true;
        var hits = new List<HitEvent> { Hit(10, 900, "Lagon", Element.Lightning) };
        var defenses = new Dictionary<string, float> { ["Res.Lightning"] = 75, ["MaxHealth"] = 1000 };
        var ailments = new List<string> { "Shock" };
        var pending = new PendingDeath(hits, 10, context, ailments, defenses, 9.5);
        context.Character = "Other"; context.Zone = "Town after respawn"; context.PlayContext.Realm = PlayRealm.Legacy; context.Hardcore = false;
        hits[0].Source = "Wrong"; hits[0].ByElement[0] = 500; hits.Clear(); defenses["Res.Lightning"] = 0; ailments.Clear();
        Eq("Medick", pending.Record.Character); Eq("Original boss arena", pending.Record.Zone); Eq("Season 5", pending.Record.PlayContextLabel());
        Eq(true, pending.Record.Hardcore); Eq("Lagon", pending.Record.Killer); Eq(75f, pending.Record.Defenses["Res.Lightning"]);
        Eq(0.5f, pending.Record.DefenseSnapshotAgeSeconds); True(pending.Record.AilmentsOnYou.Contains("Shock"), "frozen ailment context");
    }
    static void Test_Pending_RejectsStaleOrFutureDefenseSnapshot()
    {
        var stats = new Dictionary<string, float> { ["Res.Fire"] = 75 };
        Eq(null, new PendingDeath(null, 10, Ctx(), null, stats, 5).Record.Defenses);
        Eq(null, new PendingDeath(null, 10, Ctx(), null, stats, 11).Record.Defenses);
        Eq(null, new PendingDeath(null, 10, Ctx(), null, stats, double.NaN).Record.Defenses);
    }
    static void Test_Report_PendingStillAcceptsMatchingLocalReportWithoutPlayerObject()
    {
        Eq(ReportTarget.Pending, DeathReportRouting.Choose(10.5, null, 10, "Hero", null, null, null));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(10.5, "Other", 10, "Hero", null, null, false));
        Eq(ReportTarget.Pending, DeathReportRouting.Choose(10.5, "Hero", 10, "Hero", null, null, true)); // already verified death can respawn before report
    }
    static void Test_Report_LateAfterRespawnEnrichesPreviousNotNextDeath()
    {
        Eq(ReportTarget.Recent, DeathReportRouting.Choose(12, "Hero", null, null, 10, "Hero", true, false));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(12, "Other", null, null, 10, "Hero", true, false));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(12, "Hero", null, null, 10, "Hero", false, false)); // could be a new death report
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(12, "Hero", 11.5, "Hero", 10, "Hero", false)); // overlaps two deaths, no sequence ID
        Eq(ReportTarget.Pending, DeathReportRouting.Choose(18, "Hero", 17.5, "Hero", 10, "Hero", false));
    }
    static void Test_Report_MissingPlayerMatchesRecentVerifiedDeathWithinWindowOnly()
    {
        Eq(ReportTarget.Recent, DeathReportRouting.Choose(12, null, null, null, 10, "Hero", null));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(16, null, null, null, 10, "Hero", null));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(9, null, null, null, 10, "Hero", null));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(double.NaN, null, null, null, 10, "Hero", null));
    }
    static void Test_Report_AliveStaleReportCannotBeSavedForFutureDeath()
    {
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(20, "Hero", null, null, null, null, true));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(20, "Hero", null, null, 10, "Hero", true));
        Eq(ReportTarget.Reject, DeathReportRouting.Choose(20, "Hero", null, null, null, null, null));
        Eq(ReportTarget.AwaitDeath, DeathReportRouting.Choose(20, "Hero", null, null, null, null, false));
    }
}
