using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_BossCard_ShortMoveNameDoesNotBorrowLongerMoveTip()
    {
        // Defilement and Hatred list "Slam" and "Echo Slam". The Echo Slam
        // tip used to appear on a plain Slam death by substring match.
        foreach (var killer in new[] { "Harbinger of Defilement", "Harbinger of Hatred" })
        {
            var lines = BossFieldNotes.ForDeath(new DeathRecord { Killer = killer, KillerAbility = "Slam", KillingElement = "Physical", IsBossFight = true }, out var boss, out var move);
            Eq("Slam", move?.Name);
            True(!lines.Any(l => l.Contains("Echo Slam")), killer + ": " + string.Join(" | ", lines));
            var echo = BossFieldNotes.ForDeath(new DeathRecord { Killer = killer, KillerAbility = "Echo Slam", IsBossFight = true }, out _, out var echoMove);
            Eq("Echo Slam", echoMove?.Name);
            True(echo.Any(l => l.Contains("Echo Slam's overlap")), killer + " keeps its own tip: " + string.Join(" | ", echo));
        }
        // A tip that also names the short move on its own still applies.
        var ash = BossFieldNotes.ForDeath(new DeathRecord { Killer = "Harbinger of Ash", KillerAbility = "Slam", IsBossFight = true }, out var ashBoss, out _);
        if (ashBoss != null) True(ash.Any(l => l.Contains("trade a slam")), string.Join(" | ", ash));
    }

    static void Test_Toast_NamesOneSupportedStepOnly()
    {
        Eq("Cap fire resistance: you had 41%", Advisor.ToastStep(ReportedFireDeath(41, 2000, 300)));
        // Already capped and nothing else measured: no step is better than a weak one.
        Eq(null, Advisor.ToastStep(new DeathRecord { Character = "Review", Killer = "Ash Wraith" }));
        Eq(null, Advisor.ToastStep(null));
        var capped = ReportedFireDeath(75, 500, 100);
        string step = Advisor.ToastStep(capped);
        True(step == null || !step.Contains("already capped"), step ?? "");
    }

    static void Test_Toast_StepNeverComesFromLowWeightCheck()
    {
        var all = new[] { ReportedFireDeath(41, 2000, 300), ReportedFireDeath(75, 2400, 400), new DeathRecord { Character = "R", Killer = "X", KillingCrit = true } };
        foreach (var d in all)
        {
            string step = Advisor.ToastStep(d);
            if (step == null) continue;
            var match = Advisor.Show(d).First();
            Eq(match.Title, step);
            True(match.Weight >= Advisor.ToastMinWeight, step);
            True(!step.Contains("--"), step);
        }
    }

    static void Test_CounterResets_RoundTripsAfterFlushedWrite()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dc-resets-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(dir, "resets.json");
        try
        {
            var resets = new CounterResets(path);
            True(resets.Reset("Hero", 12), "reset saved");
            True(!File.Exists(path + ".tmp"), "temp file renamed");
            var reloaded = new CounterResets(path);
            Eq(3, reloaded.Count("Hero", 15));
            Eq(15, reloaded.Count("Other", 15));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
}
