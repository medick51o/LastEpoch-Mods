using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static void WithHistory(Action<string> test)
    {
        string dir = Path.Combine(Path.GetTempPath(), "death-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try { test(dir); }
        finally { Directory.Delete(dir, true); }
    }
    static DeathRecord HistoryRecord() => new() { Character = "History test", UtcTime = DateTime.UtcNow };

    static void Test_History_TornTailCannotSwallowNextDeath()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir);
            True(log.Append(HistoryRecord()), "first death saved");
            File.AppendAllText(log.JsonPath, "{partial");
            var resumed = new DeathLog(dir); resumed.Load();
            True(resumed.Append(HistoryRecord()), "second death saved");
            var loaded = new DeathLog(dir); loaded.Load();
            Eq(2, loaded.All.Count);
            True(File.ReadAllLines(log.JsonPath).Contains("{partial"), "torn tail retained as its own line");
            resumed.All[1].Killer = "late attacker";
            True(resumed.SaveUpdated() && resumed.SaveUpdated(), "consecutive enrichment saves");
            True(File.ReadAllLines(log.JsonPath).Contains("{partial"), "opaque data survives rewrites");
        });
    }
    static void Test_History_CompleteUnterminatedRecordSurvivesAppend()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir); log.Append(HistoryRecord());
            File.WriteAllText(log.JsonPath, File.ReadAllText(log.JsonPath).TrimEnd());
            var resumed = new DeathLog(dir);
            True(resumed.Append(HistoryRecord()), "automatic load before append");
            var loaded = new DeathLog(dir); loaded.Load();
            Eq(2, loaded.All.Count); Eq(2, loaded.All[1].Number);
        });
    }
    static void Test_History_UnknownFieldsAndFutureRowsSurviveEnrichment()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir);
            const string original = "{\"Character\":\"History test\",\"Number\":8,\"FutureField\":{\"Keep\":17},\"PlayContext\":{\"FutureRealm\":\"keep me\"}}";
            const string future = "{\"Schema\":999,\"Character\":\"Future\",\"Number\":42,\"Secret\":true}";
            File.WriteAllText(log.JsonPath, original + "\n" + future + "\nnull\n{bad\n");
            byte[] initial = File.ReadAllBytes(log.JsonPath);
            log.Load(); Eq(1, log.All.Count);
            log.All[0].Killer = "new genuine detail";
            True(log.SaveUpdated(), "enrichment saved");
            using var row = JsonDocument.Parse(File.ReadAllLines(log.JsonPath)[0]);
            Eq(17, row.RootElement.GetProperty("FutureField").GetProperty("Keep").GetInt32());
            Eq("keep me", row.RootElement.GetProperty("PlayContext").GetProperty("FutureRealm").GetString());
            True(File.ReadAllLines(log.JsonPath).Contains(future), "future schema line retained exactly");
            log.All[0].KillingElement = "Cold";
            True(log.SaveUpdated(), "second enrichment saved");
            True(initial.SequenceEqual(File.ReadAllBytes(log.JsonPath + ".recovery")), "original recovery copy never overwritten");
            True(log.Append(HistoryRecord()), "append after skipped rows"); Eq(9, log.All[1].Number);
        });
    }
    static void Test_History_ReadFailureBlocksWritesAndKeepsSessionDeaths()
    {
        WithHistory(dir =>
        {
            var seed = new DeathLog(dir); seed.Append(HistoryRecord());
            byte[] original = File.ReadAllBytes(seed.JsonPath);
            var log = new DeathLog(dir);
            using (var locked = new FileStream(seed.JsonPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                log.Load();
                True(!log.Append(HistoryRecord()), "do not write after incomplete read");
            }
            True(!log.SaveUpdated(), "unlock alone does not authorize incomplete overwrite");
            Eq(1, log.UnsavedCount); Eq(0, log.SavedCountFor("History test"));
            True(log.PersistenceWarning.Contains("blocked"), "visible read failure");
            True(original.SequenceEqual(File.ReadAllBytes(seed.JsonPath)), "existing history unchanged");
        });
    }
    static void Test_History_FailedReloadRetainsExistingView()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir); log.Append(HistoryRecord());
            var record = log.All[0];
            using (var locked = new FileStream(log.JsonPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) log.Load();
            True(ReferenceEquals(record, log.All[0]), "failed load is transactional");
            True(!log.SaveUpdated(), "failed reload blocks rewrite");
        });
    }
    static void Test_History_FailedPrimaryWriteRetriesWithoutDuplicates()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir); log.Append(HistoryRecord());
            byte[] original = File.ReadAllBytes(log.JsonPath);
            Directory.CreateDirectory(log.JsonPath + ".tmp");
            True(!log.Append(HistoryRecord()), "write failure returned");
            Eq(1, log.UnsavedCount);
            True(original.SequenceEqual(File.ReadAllBytes(log.JsonPath)), "primary intact");
            Directory.Delete(log.JsonPath + ".tmp");
            True(log.SaveUpdated(), "retry saves pending death");
            True(log.SaveUpdated(), "repeated retry is idempotent");
            Eq(0, log.UnsavedCount); Eq(null, log.PersistenceWarning);
            var loaded = new DeathLog(dir); loaded.Load(); Eq(2, loaded.All.Count);
        });
    }
    static void Test_History_TextFailureDoesNotReportPrimaryAsLost()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir);
            Directory.CreateDirectory(log.TextPath + ".tmp");
            True(log.Append(HistoryRecord()), "authoritative JSON saved");
            Eq(0, log.UnsavedCount); Eq(1, log.SavedCountFor("History test"));
            True(log.PersistenceWarning.Contains("text copy"), "separate text warning");
            Directory.Delete(log.TextPath + ".tmp");
            True(log.SaveUpdated(), "mirror retry"); Eq(null, log.PersistenceWarning);
            Eq(1, File.ReadAllLines(log.TextPath).Length);
        });
    }
    static void Test_History_ExternalChangeCannotBeOverwritten()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir); log.Append(HistoryRecord());
            File.AppendAllText(log.JsonPath, "external line\n");
            byte[] changed = File.ReadAllBytes(log.JsonPath);
            True(!log.Append(HistoryRecord()), "external edits block overwrite");
            True(changed.SequenceEqual(File.ReadAllBytes(log.JsonPath)), "external changes preserved");
            Eq(1, log.UnsavedCount);
        });
    }
    static void Test_History_InvalidUtf8BlocksDestructiveRewrite()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir);
            byte[] invalid = { 0xff, 0xfe, 0xff };
            File.WriteAllBytes(log.JsonPath, invalid);
            log.Load();
            True(!log.Append(HistoryRecord()), "invalid encoding cannot be rewritten");
            True(invalid.SequenceEqual(File.ReadAllBytes(log.JsonPath)), "original bytes retained");
        });
    }
    static void Test_History_FailedReplacementKeepsPrimaryAndPendingRecord()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir); log.Append(HistoryRecord());
            byte[] original = File.ReadAllBytes(log.JsonPath);
            // An open reader blocks replacement only on Windows. POSIX rename
            // succeeds under a reader, so block the backup step there instead:
            // either way the temp file is written and the swap never happens.
            if (OperatingSystem.IsWindows())
            {
                using (var locked = new FileStream(log.JsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    True(!log.Append(HistoryRecord()), "replacement blocked by open reader");
            }
            else
            {
                Directory.CreateDirectory(log.JsonPath + ".bak");
                True(!log.Append(HistoryRecord()), "replacement blocked by unwritable backup");
                Directory.Delete(log.JsonPath + ".bak");
            }
            True(original.SequenceEqual(File.ReadAllBytes(log.JsonPath)), "failed replacement preserves source");
            Eq(1, log.UnsavedCount);
            True(log.SaveUpdated(), "retry after reader closes");
            var loaded = new DeathLog(dir); loaded.Load(); Eq(2, loaded.All.Count);
        });
    }
    static void Test_History_RecoveryExportIncludesUnsavedFactsWithoutOverwriting()
    {
        WithHistory(dir =>
        {
            var log = new DeathLog(dir); log.Append(HistoryRecord());
            byte[] original = File.ReadAllBytes(log.JsonPath);
            Directory.CreateDirectory(log.JsonPath + ".tmp");
            var pending = HistoryRecord(); pending.KillerAbility = "Unsaved ability";
            pending.Defenses = new() { ["Res.Void"] = 2 };
            True(!log.Append(pending), "pending record");
            string export = Path.Combine(dir, "recovery-export.jsonl");
            log.ExportCharacter("History test", export);
            using var record = JsonDocument.Parse(File.ReadAllLines(export)[1]);
            Eq("Unsaved ability", record.RootElement.GetProperty("KillerAbility").GetString());
            Eq(2f, record.RootElement.GetProperty("Defenses").GetProperty("Res.Void").GetSingle());
            bool refused = false;
            try { log.ExportCharacter("History test", log.JsonPath); } catch (IOException) { refused = true; }
            True(refused, "export cannot overwrite original");
            True(original.SequenceEqual(File.ReadAllBytes(log.JsonPath)), "export does not alter primary");
            Eq(1, log.UnsavedCount);
        });
    }

    static void Test_History_MalformedArraysAndNullListsLoadSafely()
    {
        WithHistory(dir =>
        {
            Directory.CreateDirectory(dir);
            string line = "{\"Character\":\"History test\",\"Number\":1,\"DamageByElement\":[5,6],\"DotByElement\":null,\"AilmentsOnYou\":null,\"TopSources\":null,\"Hits\":2,\"WindowDamage\":11}";
            File.WriteAllText(Path.Combine(dir, DeathLog.JsonFile), line + Environment.NewLine);
            var log = new DeathLog(dir); log.Load();
            var d = log.All.Single();
            Eq(null, d.DamageByElement);
            Eq(0, d.AilmentsOnYou.Count);
            Eq(0, d.TopSources.Count);
            True(d.ToLogLine().Contains("History test"), "text mirror line builds");
            True(Advisor.Show(d, log.All) != null, "advice runs on a short damage array");
            True(log.Append(HistoryRecord()), "a malformed record does not block the next save");
            Eq(line, File.ReadAllLines(log.JsonPath)[0]);
        });
    }
    static void Test_History_NullAilmentListDoesNotBreakTextMirror()
    {
        var d = new DeathRecord { Character = "History test", AilmentsOnYou = null, UtcTime = DateTime.UtcNow };
        True(d.ToLogLine().Contains("death #0"), "null ailment list is tolerated");
    }
}

