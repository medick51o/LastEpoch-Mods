using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static PlayContext SeasonFive() => PlayContext.FromGameCycle(7, "Fugu", "Season 5", true, true);

    static void Test_Realm_UsesGameLabelInsteadOfInternalNumber()
    {
        var context = SeasonFive();
        Eq(PlayRealm.Seasonal, context.Realm);
        Eq("Season 5", context.Label());
        Eq(7, context.CycleId);
        Eq("Fugu", context.CycleCode);
        Eq(true, context.Offline);
        True(!context.Label().Contains("7") && !context.Label().Contains("Fugu"), "internal IDs are not public season numbers");
    }

    static void Test_Realm_RawSeasonCannotProveCurrentRealm()
    {
        var context = PlayContext.FromGameCycle(7, "Fugu", "Season 5", false, true);
        Eq(PlayRealm.Unknown, context.Realm);
        Eq(null, context.SeasonName);
        Eq("Season / Legacy unknown", context.Label());
        Eq("Fugu", context.CycleCode); // preserved for diagnostics, not presented as a current season
    }

    static void Test_Realm_LegacyDoesNotNeedSeasonNumberOrReadyService()
    {
        var legacy = PlayContext.FromGameCycle(99, "Legacy", "Season 5", false, false);
        Eq(PlayRealm.Legacy, legacy.Realm);
        Eq("Legacy", legacy.Label());
        Eq(null, legacy.SeasonName);
        Eq(false, legacy.Offline);
        var numericOnly = PlayContext.FromGameCycle(1, null, null, false, null);
        Eq(PlayRealm.Unknown, numericOnly.Realm); // even today's Legacy numeric value cannot establish its meaning alone
    }

    static void Test_Realm_FutureSeasonNeedsNoHardcodedMapping()
    {
        var future = PlayContext.FromGameCycle(12, "Tarpon", "A Future Adventure", true, null);
        Eq(PlayRealm.Seasonal, future.Realm);
        Eq("Season: A Future Adventure", future.Label());
        Eq(null, future.Offline);
        Eq("Season (name unavailable)", PlayContext.FromGameCycle(12, "Tarpon", null, true, null).Label());
        Eq("Beta", PlayContext.FromGameCycle(0, "Beta", "Some label", true, true).Label());
    }

    static void Test_Realm_LabelSanitizesLocalizationWithoutInferringSeason()
    {
        var context = PlayContext.FromGameCycle(7, "Fugu", "<color=red>Season 5</color>\n  Offline ", true, true);
        Eq("Season 5 Offline", context.Label());
        Eq("Season (name unavailable)", PlayContext.FromGameCycle(7, "Fugu", "<b> </b>", true, true).Label());
        True(PlayContext.FromGameCycle(7, "Fugu", new string('x', 400), true, true).SeasonName.Length == 160, "bound unexpected label length");
    }

    static void Test_Realm_AnalyzerFreezesContextBeforeMigration()
    {
        var ctx = Ctx(); ctx.PlayContext = SeasonFive();
        var death = DeathAnalyzer.Analyze(null, 10, ctx);
        ctx.PlayContext.Realm = PlayRealm.Legacy;
        ctx.PlayContext.SeasonName = null;
        Eq("Season 5", death.PlayContextLabel());
        Eq(PlayRealm.Seasonal, death.PlayContext.Realm);
        True(!ReferenceEquals(ctx.PlayContext, death.PlayContext), "record owns a separate snapshot");
    }

    static void Test_Realm_LateDeathDetailsCannotRelabelHistory()
    {
        var ctx = Ctx(); ctx.PlayContext = SeasonFive();
        var death = DeathAnalyzer.Analyze(null, 10, ctx);
        new DeathDetails { Killer = "Lagon", Damage = 900, PrimaryElement = "Lightning", IsBossFight = true }.Apply(death);
        Eq("Season 5", death.PlayContextLabel());
        True(death.ToLogLine().Contains("Season 5"), "human-readable log contains death-time season");
        Eq("Season 5", JsonSerializer.Deserialize<DeathRecord>(JsonSerializer.Serialize(death)).PlayContextLabel());
    }

    static void Test_Realm_OldRecordsRemainUnknown()
    {
        var death = JsonSerializer.Deserialize<DeathRecord>("{\"Character\":\"Medick\",\"Number\":1}");
        Eq(null, death.PlayContext);
        Eq("Season / Legacy unknown", death.PlayContextLabel());
        Eq("Season / Legacy unknown", DeathAnalyzer.Analyze(null, 10, Ctx()).PlayContextLabel());
    }

    static void Test_Realm_SeasonToLegacyKeepsOneCounterAndHistoricalContext()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dc_realm_" + Guid.NewGuid().ToString("N"));
        try
        {
            var log = new DeathLog(dir);
            var ctx = Ctx(); ctx.PlayContext = SeasonFive();
            var seasonal = DeathAnalyzer.Analyze(null, 10, ctx);
            True(log.Append(seasonal), "season death saved");
            ctx.PlayContext = PlayContext.FromGameCycle(1, "Legacy", null, true, true);
            var legacy = DeathAnalyzer.Analyze(null, 20, ctx);
            True(log.Append(legacy), "legacy death saved");
            Eq(2, legacy.Number);
            new DeathDetails { Killer = "Rat", Damage = 20 }.Apply(seasonal);
            True(log.SaveUpdated(), "late report rewrite saved");
            var restored = new DeathLog(dir); restored.Load();
            var deaths = restored.For("Medick").ToList();
            Eq(2, restored.CountFor("Medick"));
            Eq("Season 5", deaths[0].PlayContextLabel());
            Eq("Legacy", deaths[1].PlayContextLabel());
            True(File.ReadAllLines(restored.TextPath)[0].Contains("Season 5"), "rewrite retains season in text");
            True(File.ReadAllLines(restored.TextPath)[1].Contains("Legacy"), "text shows new Legacy death");
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
}
