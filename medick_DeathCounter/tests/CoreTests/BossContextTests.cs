using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_Boss_ExactNameRequiresExplicitGameFlag()
    {
        var d = new DeathRecord { Killer = "Lagon", Zone = "Lagon boss arena" };
        Eq(null, BossCatalog.Match(d));
        d.IsBossFight = false; Eq(null, BossCatalog.Match(d));
        d.IsBossFight = true; Eq("lagon", BossCatalog.Match(d).Id);
        d.Killer = "  lagon  "; Eq("lagon", BossCatalog.Match(d).Id);
    }
    static void Test_Boss_AddsProxiesAndVariantsDoNotMatchBySubstring()
    {
        foreach (var name in new[] { "Lagon's Tentacle", "Lagon Clone", "Emperor's Remains", "Void Shade of Rahyeh", "Harbinger of Hatred's Void Rahyeh" })
            Eq(null, BossCatalog.Match(new DeathRecord { IsBossFight = true, Killer = name }));
        Eq(null, BossCatalog.Match(new DeathRecord { IsBossFight = true, KillerAbility = "Lagon Lightning Blast", Zone = "Lagon" }));
    }
    static void Test_Boss_OfflineAttackerGuideDoesNotBecomeBossClassification()
    {
        var d = new DeathRecord { Killer = "Lagon" };
        Eq("lagon", BossCatalog.AttackerGuide(d).Id); Eq(null, BossCatalog.Match(d));
        Eq("Boss encounter unknown", BossCatalog.EncounterLabel(d)); Eq(null, d.IsBossFight);
        d.IsBossFight = false; Eq(null, BossCatalog.AttackerGuide(d));
    }
    static void Test_Boss_ManualCatalogCannotMutateSavedDeath()
    {
        var d = new DeathRecord { Killer = "Thrall", IsBossFight = true, KillingElement = "Fire" };
        string original = JsonSerializer.Serialize(d);
        foreach (var boss in BossCatalog.All) { _ = boss.Tips; _ = boss.DamageTypes; }
        _ = BossCatalog.EncounterLabel(d); _ = BossCatalog.ObservedTypes(d);
        Eq(original, JsonSerializer.Serialize(d)); Eq(null, BossCatalog.Match(d));
        True(BossCatalog.EncounterLabel(d).Contains("not identified"), "unknown actor retains confirmed encounter without invented boss");
    }
    static void Test_Boss_CatalogTypesNeverFillMissingDeathTypes()
    {
        var d = new DeathRecord { Killer = "Julra", IsBossFight = true };
        True(BossCatalog.Match(d).DamageTypes.Contains(Element.Void), "published Julra coverage available");
        Eq(0, BossCatalog.ObservedTypes(d).Count); Eq(null, d.KillingElement);
        d.KillingElement = "Fire"; d.SecondaryKillingElement = "Cold";
        Eq(2, BossCatalog.ObservedTypes(d).Count); Eq(Element.Fire, BossCatalog.ObservedTypes(d)[0]);
        d.SecondaryKillingElement = "Fire"; Eq(1, BossCatalog.ObservedTypes(d).Count);
    }
    static void Test_Boss_NetworkFlagSurvivesSameBlowLocalReport()
    {
        var network = new DeathDetails { Killer = "Lagon", PrimaryElement = "Lightning", Damage = 1234, Crit = false, IsBossFight = true };
        var local = new DeathDetails { Killer = "Lagon", PrimaryElement = "Lightning", Damage = 1234, Crit = false };
        local.PreserveBossFlagFrom(network); Eq(true, local.IsBossFight);
        var d = new DeathRecord(); local.Apply(d); Eq("lagon", BossCatalog.Match(d).Id);
    }
    static void Test_Boss_FlagDoesNotBleedAcrossDifferentBlowsOrOverrideExplicitFalse()
    {
        var network = new DeathDetails { Killer = "Lagon", Damage = 1234, IsBossFight = true };
        var other = new DeathDetails { Killer = "Thrall", Damage = 1234 };
        other.PreserveBossFlagFrom(network); Eq(null, other.IsBossFight);
        other = new DeathDetails { Killer = "Lagon", Damage = 1235 }; other.PreserveBossFlagFrom(network); Eq(null, other.IsBossFight);
        other = new DeathDetails { Killer = "Lagon", Damage = 1234, IsBossFight = false }; other.PreserveBossFlagFrom(network); Eq(false, other.IsBossFight);
    }
    static void Test_Zone_RawSceneAndLevelFreezeBeforeRespawnAndRoundTrip()
    {
        var context = Ctx(); context.Zone = "Cleaned"; context.RawSceneId = "Boss_Arena_55"; context.ZoneLevel = 100;
        var pending = new PendingDeath(null, 10, context, null, null, 10);
        context.RawSceneId = "Town_1"; context.ZoneLevel = 1;
        var d = JsonSerializer.Deserialize<DeathRecord>(JsonSerializer.Serialize(pending.Record));
        Eq("Boss_Arena_55", d.RawSceneId); Eq(100, d.ZoneLevel);
        True(d.ToLogLine().Contains("Boss_Arena_55") && d.LocationLabel().Contains("100"), "exact scene and area in log");
        Eq(null, BossCatalog.Match(d));
    }
    static void Test_Zone_OldRecordsRemainReadableAndUnknownFlagsStayUnknown()
    {
        var d = JsonSerializer.Deserialize<DeathRecord>("{\"Zone\":\"Old area\"}");
        Eq("Old area", d.LocationLabel()); Eq("Boss encounter unknown", BossCatalog.EncounterLabel(d));
        Eq("Zone unavailable", new DeathRecord().LocationLabel());
        var context = Ctx(); context.ZoneLevel = -5; Eq(null, DeathAnalyzer.Analyze(null, 1, context).ZoneLevel);
    }
}
