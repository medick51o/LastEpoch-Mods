using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static void InAssessmentDirectory(Action<string> test)
    {
        string dir = Path.Combine(Path.GetTempPath(), "dc_updates_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { test(dir); }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
    static Dictionary<string, float> NewGear(float fire = 75, float hp = 1400) => new() { ["Res.Fire"] = fire, ["MaxHealth"] = hp, ["Ward"] = 0, ["AreaLevel"] = 1 };
    static ReassessmentUpdate SaveUpdate(ReassessmentLog log, DeathRecord death, Dictionary<string, float> stats, ReassessmentUpdate prior = null)
    {
        var result = CurrentAssessment.Build(death, death.Character, stats);
        True(log.Save(death, death.Character, stats, result, prior, out var saved), "update saved");
        return saved;
    }
    static void Test_Updates_AppendKeepsOriginalFilesAndCounterByteForByte()
    {
        InAssessmentDirectory(dir =>
        {
            var deaths = new DeathLog(dir); var d = HeavyFire(41); True(deaths.Append(d), "death saved");
            string json = File.ReadAllText(deaths.JsonPath), text = File.ReadAllText(deaths.TextPath), capture = JsonSerializer.Serialize(d);
            var updates = new ReassessmentLog(dir); updates.Load();
            var first = SaveUpdate(updates, d, NewGear());
            var second = SaveUpdate(updates, d, NewGear(75, 1800), first);
            Eq(2, updates.For(d).Count()); Eq(1, deaths.CountFor(d.Character));
            Eq(json, File.ReadAllText(deaths.JsonPath)); Eq(text, File.ReadAllText(deaths.TextPath)); Eq(capture, JsonSerializer.Serialize(d));
            True(first.Id != second.Id && first.Id != d.Id, "new update identity, not a new death");
            Eq(first.Id, second.PreviousUpdateId);
            True(second.SincePrevious.Any(s => s.Contains("Max health: 1400 → 1800")), "compare the prior update too");
        });
    }
    static void Test_Updates_StatsAdviceRatingAndOriginalAreFrozen()
    {
        InAssessmentDirectory(dir =>
        {
            var d = HeavyFire(41); d.PlayContext = SeasonFive();
            var stats = NewGear(); var result = CurrentAssessment.Build(d, d.Character, stats);
            var updates = new ReassessmentLog(dir); updates.Load();
            True(updates.Save(d, d.Character, stats, result, null, out var update), "saved");
            string before = JsonSerializer.Serialize(update);
            stats["MaxHealth"] = 2; d.PlayContext.Realm = PlayRealm.Legacy; d.Defenses["Res.Fire"] = 0;
            result.Rating.Grade = "Z"; result.Changes.Clear(); result.Actions[0].Body = "changed";
            Eq(before, JsonSerializer.Serialize(update)); Eq("Season 5", update.OriginalCapture.PlayContextLabel());
            var again = new ReassessmentLog(dir); again.Load();
            Eq(before, JsonSerializer.Serialize(again.All.Single()));
            True(again.All.Single().Assessment.Actions.All(a => a.ToAdvice().Key != null && a.ToAdvice().Body != "changed"), "saved action fields survive JSON");
        });
    }
    static void Test_Updates_DeleteOnePreservesChildrenOriginalAndOtherUpdates()
    {
        InAssessmentDirectory(dir =>
        {
            var deaths = new DeathLog(dir); var d = HeavyFire(41); deaths.Append(d);
            string original = File.ReadAllText(deaths.JsonPath);
            var updates = new ReassessmentLog(dir); updates.Load();
            var first = SaveUpdate(updates, d, NewGear()); var child = SaveUpdate(updates, d, NewGear(75, 1800), first);
            True(updates.Delete(first.Id), "delete first");
            var again = new ReassessmentLog(dir); again.Load();
            Eq(1, again.For(d).Count()); Eq(child.Id, again.For(d).Single().Id);
            Eq(first.Id, again.For(d).Single().PreviousUpdateId); // informative link; child owns its full snapshot
            Eq(original, File.ReadAllText(deaths.JsonPath)); Eq(1, deaths.CountFor(d.Character));
            True(!File.ReadAllText(Path.Combine(dir, ReassessmentLog.FileName)).Contains("\"Id\":\"" + first.Id + "\""), "removed update content is deleted from the active file");
        });
    }
    static void Test_Updates_DeleteAllIsScopedToOneDeath()
    {
        InAssessmentDirectory(dir =>
        {
            var deaths = new DeathLog(dir); var a = HeavyFire(); var b = HeavyFire(); deaths.Append(a); deaths.Append(b);
            True(a.Id != b.Id, "unique death identities even with same timestamp");
            var updates = new ReassessmentLog(dir); updates.Load();
            SaveUpdate(updates, a, NewGear()); SaveUpdate(updates, a, NewGear(75, 1500)); var keep = SaveUpdate(updates, b, NewGear());
            string original = File.ReadAllText(deaths.JsonPath);
            True(updates.DeleteFor(a), "clear a updates");
            var again = new ReassessmentLog(dir); again.Load();
            Eq(0, again.For(a).Count()); Eq(keep.Id, again.For(b).Single().Id);
            Eq(original, File.ReadAllText(deaths.JsonPath)); Eq(2, deaths.CountFor(a.Character));
        });
    }
    static void Test_Updates_LegacyKeySurvivesLateDetailsAndReload()
    {
        InAssessmentDirectory(dir =>
        {
            var d = HeavyFire(41); d.Number = 17; // old JSON without Id
            string key = ReassessmentLog.KeyFor(d);
            var updates = new ReassessmentLog(dir); updates.Load(); var first = SaveUpdate(updates, d, NewGear());
            new DeathDetails { Killer = "Newly resolved killer", PrimaryElement = "Cold", Damage = 2200 }.Apply(d);
            Eq(key, ReassessmentLog.KeyFor(d));
            var again = new ReassessmentLog(dir); again.Load();
            var fromDisk = JsonSerializer.Deserialize<DeathRecord>(JsonSerializer.Serialize(d));
            Eq(first.Id, again.For(fromDisk).Single().Id);
            Eq("Fire", again.For(fromDisk).Single().OriginalCapture.KillingElement);
            Eq(null, d.Id); // linking old history must not backfill or rewrite it
        });
    }
    static void Test_Updates_CorruptTailAndFutureSchemaAreRetainedOnRewrite()
    {
        InAssessmentDirectory(dir =>
        {
            var d = HeavyFire(41); var updates = new ReassessmentLog(dir); updates.Load(); SaveUpdate(updates, d, NewGear());
            string path = Path.Combine(dir, ReassessmentLog.FileName);
            const string future = "{\"Schema\":99,\"future\":true}";
            File.AppendAllText(path, future + "\n{torn tail");
            var warnings = new List<string>(); var again = new ReassessmentLog(dir, warnings.Add); again.Load();
            Eq(1, warnings.Count); Eq(1, again.All.Count);
            SaveUpdate(again, d, NewGear(75, 1500));
            var restored = new ReassessmentLog(dir); restored.Load(); Eq(2, restored.All.Count);
            True(File.ReadAllText(path).Contains(future) && File.ReadAllText(path).Contains("{torn tail"), "unknown data is retained without swallowing the new update");
        });
    }
    static void Test_Updates_DuplicateLinesCannotResurrectDeletedUpdate()
    {
        InAssessmentDirectory(dir =>
        {
            var d = HeavyFire(); var log = new ReassessmentLog(dir); log.Load(); var update = SaveUpdate(log, d, NewGear());
            string path = Path.Combine(dir, ReassessmentLog.FileName), duplicate = File.ReadAllText(path);
            File.AppendAllText(path, duplicate + "{\"Schema\":99,\"unrelated\":true}\n");
            var again = new ReassessmentLog(dir); again.Load(); Eq(1, again.All.Count);
            True(again.Delete(update.Id), "delete duplicate identity");
            var restored = new ReassessmentLog(dir); restored.Load(); Eq(0, restored.All.Count);
            True(File.ReadAllText(path).Contains("unrelated"), "unrelated future schema retained");
        });
    }
    static void Test_Updates_SaveFailureDoesNotPretendItWasSaved()
    {
        InAssessmentDirectory(dir =>
        {
            string notDirectory = Path.Combine(dir, "blocked"); File.WriteAllText(notDirectory, "keep");
            var log = new ReassessmentLog(notDirectory); log.Load(); var d = HeavyFire(); var stats = NewGear();
            True(!log.Save(d, d.Character, stats, CurrentAssessment.Build(d, d.Character, stats), null, out var saved), "failed disk write reported");
            Eq(null, saved); Eq(0, log.All.Count); Eq("keep", File.ReadAllText(notDirectory));
        });
    }
    static void Test_Updates_DeleteFailureKeepsExistingEntriesAndDisk()
    {
        InAssessmentDirectory(dir =>
        {
            var log = new ReassessmentLog(dir); log.Load(); var d = HeavyFire(); var update = SaveUpdate(log, d, NewGear());
            string path = Path.Combine(dir, ReassessmentLog.FileName), before = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp");
            True(!log.Delete(update.Id), "delete failed");
            Eq(1, log.All.Count); Eq(before, File.ReadAllText(path));
        });
    }
    static void Test_Updates_RejectWrongParentAndUnavailableAssessment()
    {
        InAssessmentDirectory(dir =>
        {
            var log = new ReassessmentLog(dir); log.Load(); var a = HeavyFire(); var b = HeavyFire(); b.Character = "Alt";
            var prior = SaveUpdate(log, a, NewGear()); var stats = NewGear();
            True(!log.Save(b, b.Character, stats, CurrentAssessment.Build(b, b.Character, stats), prior, out _), "update cannot link to another death");
            True(!log.Save(a, "Alt", stats, CurrentAssessment.Build(a, "Alt", stats), null, out _), "cannot save another character's stats");
            Eq(1, log.All.Count);
        });
    }
    static void Test_Updates_NoArtificialHistoryLimitOrCounterImpact()
    {
        InAssessmentDirectory(dir =>
        {
            var deaths = new DeathLog(dir); var d = HeavyFire(41); deaths.Append(d);
            var log = new ReassessmentLog(dir); log.Load(); ReassessmentUpdate prior = null;
            for (int i = 0; i < 25; i++) prior = SaveUpdate(log, d, NewGear(75, 1400 + i), prior);
            var again = new ReassessmentLog(dir); again.Load(); Eq(25, again.For(d).Count());
            Eq(25, again.All.Select(u => u.Id).Distinct().Count()); Eq(1, deaths.CountFor(d.Character));
        });
    }
    static void Test_Grade_KnownGapAndRegressionExplainRepeatRisk()
    {
        var d = HeavyFire(41);
        var same = CurrentAssessment.Build(d, "Test", NewGear(41)); Eq("C", same.Rating.Grade);
        True(same.Rating.Reason.Contains("fire resistance remains below 75%"), same.Rating.Reason);
        var worse = CurrentAssessment.Build(d, "Test", NewGear(20)); Eq("D", worse.Rating.Grade);
        True(worse.Rating.Verdict.Contains("Still vulnerable"), worse.Rating.Verdict);
    }
    static void Test_Grade_HugeOverkillCannotBecomeSafeFromCapsOrWardPeak()
    {
        var d = AdviceDeath("Fire", 41); d.KillingCrit = false; d.KillingBlow = 90000; d.OverkillDamage = 50000;
        var stats = NewGear(); stats["Ward"] = 50000;
        var result = CurrentAssessment.Build(d, "Test", stats);
        Eq("B", result.Rating.Grade); True(result.Rating.Verdict.Contains("unresolved"), result.Rating.Verdict);
        True(result.Rating.Scope.Contains("not a whole-fight survival probability"), result.Rating.Scope);
        True(!result.Rating.Reason.Contains("survived") && !result.Rating.Reason.Contains("100%"), "large ward peak cannot manufacture survival odds");
    }
    static void Test_Grade_MissingRelevantStatsRemainUnrated()
    {
        var result = CurrentAssessment.Build(HeavyFire(), "Test", new Dictionary<string, float> { ["MaxHealth"] = 2000 });
        Eq("?", result.Rating.Grade);
        var d = HeavyFire(); d.KillingCrit = true;
        result = CurrentAssessment.Build(d, "Test", NewGear()); Eq("?", result.Rating.Grade);
        result = CurrentAssessment.Build(new DeathRecord { Character = "Test" }, "Test", NewGear()); Eq("?", result.Rating.Grade);
    }
    static void Test_Grade_ARequiresImprovedMeasuredChecksWithoutMajorKnownRisk()
    {
        var d = AdviceDeath("Fire", 41); d.KillingCrit = false;
        var result = CurrentAssessment.Build(d, "Test", NewGear()); Eq("A", result.Rating.Grade);
        True(result.Rating.Verdict.Contains("Measured"), result.Rating.Verdict);
        d = AdviceDeath("Fire", 75); d.KillingCrit = false;
        result = CurrentAssessment.Build(d, "Test", NewGear(75, 1000)); Eq("B", result.Rating.Grade);
        var stats = NewGear(75, 1000); stats["Ward"] = 100000;
        Eq("B", CurrentAssessment.Build(d, "Test", stats).Rating.Grade); // ward peak alone earns no progress grade
    }
    static void Test_Grade_MixedTypesAndRecurringDebuffsPreventFalseClearance()
    {
        var d = AdviceDeath("Fire", 41); d.KillingCrit = false; d.SecondaryKillingElement = "Cold";
        var stats = NewGear(); Eq("?", CurrentAssessment.Build(d, "Test", stats).Rating.Grade);
        stats["Res.Cold"] = 40; Eq("C", CurrentAssessment.Build(d, "Test", stats).Rating.Grade);
        stats["Res.Cold"] = 75; d.AilmentsOnYou.Add("Marked for Death");
        Eq("B", CurrentAssessment.Build(d, "Test", stats).Rating.Grade);
    }
}
