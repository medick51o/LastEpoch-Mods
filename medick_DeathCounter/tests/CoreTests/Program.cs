using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using medick_DeathCounter.Core;

// Tiny no-dependency test runner: every static void Test_* runs, any
// exception is a failure. Exit code = number of failures.
static class Program
{
    static int _fail;

    static int Main()
    {
        var tests = typeof(Program).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(m => m.Name.StartsWith("Test_")).OrderBy(m => m.Name);
        int n = 0;
        foreach (var t in tests)
        {
            n++;
            try { t.Invoke(null, null); Console.WriteLine($"  pass  {t.Name}"); }
            catch (Exception ex) { _fail++; Console.WriteLine($"  FAIL  {t.Name}: {(ex.InnerException ?? ex).Message}"); }
        }
        Console.WriteLine(_fail == 0 ? $"{n} tests, all passed" : $"{n} tests, {_fail} FAILED");
        return _fail;
    }

    static void Eq<T>(T want, T got, string what = "")
    {
        if (!EqualityComparer<T>.Default.Equals(want, got))
            throw new Exception($"{what} expected <{want}> got <{got}>");
    }
    static void True(bool c, string what) { if (!c) throw new Exception(what); }

    static HitEvent Hit(double t, float amt, string src, Element? el = null, string ailment = null, bool dot = false, bool? crit = null)
    {
        var h = new HitEvent { Time = t, Amount = amt, Source = src, Ailment = ailment, IsDot = dot, Crit = crit, MaxHealth = 1000f };
        if (el.HasValue) { h.ByElement = new float[Elements.Count]; h.ByElement[(int)el.Value] = amt; }
        return h;
    }

    static DeathContext Ctx(float maxHp = 1000f) => new() { Character = "Medick", Level = 80, Zone = "Monolith", MaxHealth = maxHp };

    // ── Ailments ─────────────────────────────────────────────
    static void Test_AilmentFind_MatchesGameishNames()
    {
        Eq("Bleed",  Ailments.Find("Ailment_Bleed")?.Name);
        Eq("Ignite", Ailments.Find("IgniteAilment(Clone)")?.Name);
        Eq("Armor Shred", Ailments.Find("ArmourShred")?.Name);
        Eq("Time Rot", Ailments.Find("TimeRot")?.Name);
        Eq("Freeze", Ailments.Find("Frozen")?.Name);
    }

    static void Test_AilmentFind_IgnoresStatNames()
    {
        Eq(null, Ailments.Find("PoisonResistance")?.Name);
        Eq(null, Ailments.Find("IgniteChance")?.Name);
        Eq(null, Ailments.Find("")?.Name);
        Eq("Resistance Shred", Ailments.Find("FireResistanceShred")?.Name);
    }

    // The game's AilmentID enum (docs/RESEARCH-game-api.md) names shreds like
    // PoisonResShred / ArmourShred: they must not read as the DoT of that element.
    static void Test_AilmentFind_GameAilmentIdNames()
    {
        Eq("Resistance Shred", Ailments.Find("PoisonResShred")?.Name);
        Eq("Resistance Shred", Ailments.Find("FireResShred")?.Name);
        Eq("Armor Shred", Ailments.Find("ArmourShred")?.Name);
        Eq("Abyssal Decay", Ailments.Find("StackingAbyssalDecay")?.Name);
        Eq("Shock", Ailments.Find("Shock")?.Name);
        Eq("Time Rot", Ailments.Find("TimeRot")?.Name);
    }

    // ── Analyzer ─────────────────────────────────────────────
    static void Test_OneShot_BigFireHit()
    {
        var hits = new List<HitEvent> { Hit(9.0, 50, "Skeleton", Element.Physical), Hit(10.0, 900, "Lagon", Element.Fire, crit: true) };
        var r = DeathAnalyzer.Analyze(hits, 10.0, Ctx());
        Eq(DeathKind.OneShot, r.Kind, "kind");
        Eq("Lagon", r.Killer, "killer");
        Eq("Fire", r.KillingElement, "element");
        Eq(900f, r.KillingBlow, "blow");
        Eq(true, r.KillingCrit, "crit");
    }

    static void Test_Burst_ManyHitsFast()
    {
        var hits = new List<HitEvent>();
        for (int i = 0; i < 6; i++) hits.Add(Hit(10.0 + i * 0.3, 150, i % 2 == 0 ? "Void Cultist" : "Abomination", Element.Void));
        var r = DeathAnalyzer.Analyze(hits, 11.5, Ctx());
        Eq(DeathKind.Burst, r.Kind, "kind");
        Eq(2, r.TopSources.Count, "sources");
        True(r.DamageByElement[(int)Element.Void] > 899f, "void total");
    }

    static void Test_Dot_PoisonTicks()
    {
        var hits = new List<HitEvent>();
        for (int i = 0; i < 20; i++) hits.Add(Hit(5.0 + i * 0.25, 40, "Spider", null, "Poison", dot: true));
        hits.Add(Hit(9.9, 30, "Spider", Element.Physical));
        var r = DeathAnalyzer.Analyze(hits, 10.0, Ctx());
        Eq(DeathKind.DamageOverTime, r.Kind, "kind");
        True(r.AilmentsOnYou.Contains("Poison"), "poison listed");
        True(r.DamageByElement[(int)Element.Poison] > r.DamageByElement[(int)Element.Physical], "poison dominates");
    }

    static void Test_Attrition_SlowGrind()
    {
        var hits = new List<HitEvent>();
        for (int i = 0; i < 10; i++) hits.Add(Hit(5.0 + i * 0.5, 60, "Wraith", Element.Necrotic));
        var r = DeathAnalyzer.Analyze(hits, 9.6, Ctx());
        Eq(DeathKind.Attrition, r.Kind, "kind");
    }

    static void Test_KillingDotTick_NamesAilment()
    {
        var hits = new List<HitEvent> { Hit(9.0, 200, "Bandit", Element.Physical), Hit(9.8, 25, "Bandit", null, "Bleed", dot: true) };
        var r = DeathAnalyzer.Analyze(hits, 9.8, Ctx());
        Eq("Bleed", r.KillingAilment);
        Eq("Bandit's Bleed", r.KillerLine());
    }

    static void Test_NoHits_IsUnknown()
    {
        var r = DeathAnalyzer.Analyze(new List<HitEvent>(), 10, Ctx(), new[] { "Frozen" });
        Eq(DeathKind.Unknown, r.Kind);
        Eq("Freeze", r.AilmentsOnYou.Single());
    }

    static void Test_OldHitsOutsideWindowIgnored()
    {
        var hits = new List<HitEvent> { Hit(1.0, 999, "Old Boss", Element.Cold), Hit(9.9, 100, "Rat", Element.Physical) };
        var r = DeathAnalyzer.Analyze(hits, 10.0, Ctx());
        Eq("Rat", r.Killer);
        Eq(0f, r.DamageByElement[(int)Element.Cold], "cold excluded");
    }

    static void Test_UnknownMaxHp_StillClassifies()
    {
        var hits = new List<HitEvent> { new HitEvent { Time = 10, Amount = 500, Source = "Boss" } };
        var r = DeathAnalyzer.Analyze(hits, 10, new DeathContext());
        Eq(DeathKind.OneShot, r.Kind);
        Eq("Unknown Hero", r.Character);
    }

    // ── Buffer ───────────────────────────────────────────────
    static void Test_HitBuffer_PrunesByTime()
    {
        var b = new HitBuffer(5);
        b.Add(Hit(0, 1, "a")); b.Add(Hit(3, 1, "b")); b.Add(Hit(7, 1, "c"));
        Eq(2, b.Count, "pruned");
        Eq("c", b.Since(6).Single().Source);
    }

    static void Test_HitBuffer_Capped()
    {
        var b = new HitBuffer(1000);
        for (int i = 0; i < HitBuffer.Capacity + 50; i++) b.Add(Hit(i * 0.001, 1, "x"));
        Eq(HitBuffer.Capacity, b.Count);
    }

    // ── Advisor ──────────────────────────────────────────────
    static void Test_Advice_FireOneShotCrit()
    {
        var hits = new List<HitEvent> { Hit(10.0, 900, "Lagon", Element.Fire, crit: true) };
        var tips = Advisor.Suggest(DeathAnalyzer.Analyze(hits, 10.0, Ctx()));
        Eq("crit", tips[0].Key, "crit first");
        True(tips.Any(t => t.Key == "res_Fire"), "fire res");
        True(tips.Any(t => t.Key == "ehp"), "ehp");
        True(tips.Count <= Advisor.MaxTips, "capped");
    }

    static void Test_Advice_BleedMentionsArmorDoesNotHelp()
    {
        var hits = new List<HitEvent>();
        for (int i = 0; i < 20; i++) hits.Add(Hit(6 + i * 0.2, 60, "Bandit", null, "Bleed", dot: true));
        var tips = Advisor.Suggest(DeathAnalyzer.Analyze(hits, 10, Ctx()));
        True(tips.Any(t => t.Key == "dot_Bleed" && t.Body.Contains("Armor does not")), "bleed tip");
        True(!tips.Any(t => t.Key == "armor"), "no plain 'stack armor' tip for a bleed death");
    }

    static void Test_Advice_PhysicalHitSaysArmor()
    {
        var hits = new List<HitEvent> { Hit(10, 800, "Brute", Element.Physical) };
        var tips = Advisor.Suggest(DeathAnalyzer.Analyze(hits, 10, Ctx()));
        True(tips.Any(t => t.Key == "armor"), "armor tip");
    }

    static void Test_Advice_FreezeFromAilmentTap()
    {
        var hits = new List<HitEvent> { Hit(9, 300, "Frost Wraith", Element.Cold), Hit(9.5, 300, "Frost Wraith", Element.Cold), Hit(10, 300, "Frost Wraith", Element.Cold) };
        var tips = Advisor.Suggest(DeathAnalyzer.Analyze(hits, 10, Ctx(), new[] { "Freeze" }));
        True(tips.Any(t => t.Key == "cc_freeze"), "freeze tip");
        True(tips.Any(t => t.Key == "res_Cold"), "cold res tip");
    }

    static void Test_Advice_Nemesis()
    {
        var hist = Enumerable.Range(0, 3).Select(_ => new DeathRecord { Killer = "Heorot" }).ToList();
        var d = new DeathRecord { Killer = "Heorot", Kind = DeathKind.Attrition, WindowSeconds = 5 };
        var tips = Advisor.Suggest(d, hist);
        True(tips.Any(t => t.Key == "nemesis" && t.Title.Contains("3 times")), "nemesis");
    }

    static void Test_Advice_UnknownDeathExplainsItself()
    {
        var tips = Advisor.Suggest(DeathAnalyzer.Analyze(null, 10, Ctx()));
        True(tips.Any(t => t.Key == "unknown"), "unknown tip");
    }

    // Research 2026-10-01: Shock lowers lightning res + raises stun chance;
    // it is not a general "take more damage" debuff. Catches a revert to the old text.
    static void Test_Advice_ShockIsLightningResShred()
    {
        var d = new DeathRecord { Kind = DeathKind.Burst, MaxHealth = 1000, WindowDamage = 900, WindowSeconds = 5, AilmentsOnYou = { "Shock" } };
        var tip = Advisor.Suggest(d).Single(t => t.Key == "cc_shock");
        True(tip.Body.Contains("lightning resistance"), "names lightning res");
        True(!tip.Body.Contains("take more damage"), "no general damage-taken claim");
        True(!Ailments.ByName("Shock").Effect.Contains("more damage"), "ailment effect text");
    }

    // Damned cuts health regen, so its tip must steer to leech, not regen alone.
    static void Test_Advice_DamnedMentionsRegenCut()
    {
        var hits = new List<HitEvent>();
        for (int i = 0; i < 20; i++) hits.Add(Hit(6 + i * 0.2, 60, "Lich", null, "Damned", dot: true));
        var tips = Advisor.Suggest(DeathAnalyzer.Analyze(hits, 10, Ctx()));
        True(tips.Any(t => t.Key == "dot_Damned" && t.Body.Contains("cuts your health regen")), "damned regen rider");
    }

    // ── Log ──────────────────────────────────────────────────
    static void Test_Log_RoundTripAndNumbering()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dc_test_" + Guid.NewGuid().ToString("N"));
        try
        {
            var log = new DeathLog(dir);
            log.Load();
            var r1 = DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 900, "Lagon", Element.Fire) }, 10, Ctx());
            var r2 = DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 900, "Rat", Element.Physical) }, 10, Ctx());
            var other = DeathAnalyzer.Analyze(null, 10, new DeathContext { Character = "Alt" });
            True(log.Append(r1) && log.Append(r2) && log.Append(other), "append ok");
            Eq(2, r2.Number, "second death numbered 2");
            Eq(1, other.Number, "per-character numbering");

            File.AppendAllText(log.JsonPath, "{not json\n");   // a torn line must not kill the history
            var warned = new List<string>();
            var again = new DeathLog(dir, warned.Add);
            again.Load();
            Eq(3, again.All.Count, "reloaded");
            Eq(1, warned.Count, "one warning for the bad line");
            Eq("Rat", again.LastFor("Medick").Killer);
            Eq(DeathKind.OneShot, again.LastFor("Medick").Kind, "enum survives json");
            Eq(2, again.CountFor("Medick"));
            Eq(3, File.ReadAllLines(again.TextPath).Length, "txt lines");
            True(File.ReadAllLines(again.TextPath)[0].Contains("killed by Lagon"), "readable line");
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }
}
