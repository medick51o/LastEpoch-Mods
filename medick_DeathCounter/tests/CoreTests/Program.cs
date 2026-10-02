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

    static void Test_GameReport_BleedDoesNotRecommendArmorForItsDamage()
    {
        var record = new DeathRecord();
        new DeathDetails { Ailment = "Bleed", PrimaryElement = "Physical", Damage = 100 }.Apply(record);
        True(!Advisor.Suggest(record).Any(t => t.Key == "armor"), "armor does not reduce bleed");
    }

    static void Test_GameReport_EnrichesWithoutInventingTimeline()
    {
        var record = new DeathRecord { Character = "Medick", Kind = DeathKind.Unknown };
        var details = new DeathDetails { Killer = "Void Horror", Ability = "Void Nova", Ailment = "Time Rot", PrimaryElement = "Void", SecondaryElement = "Physical", Damage = 1234, Overkill = 100, Crit = true };
        details.Apply(record);
        details.Apply(record); // shared death text + network fallback can report twice
        Eq("Void Horror", record.Killer);
        Eq("Void Nova", record.KillerAbility);
        Eq("Void", record.KillingElement);
        Eq("Physical", record.SecondaryKillingElement);
        Eq(1234f, record.KillingBlow);
        Eq(true, record.KillingCrit);
        Eq(DeathKind.Reported, record.Kind);
        Eq(0, record.Hits);
        Eq(0f, record.WindowDamage);
        Eq(0f, record.DamageByElement.Sum());
        Eq(1, record.AilmentsOnYou.Count);
        True(!Advisor.Suggest(record).Any(t => t.Key == "unknown"), "reported cause should offer relevant advice");
    }

    static void Test_GameReport_PreservesObservedHitClassification()
    {
        var record = new DeathRecord { Kind = DeathKind.Burst, Hits = 4, WindowDamage = 1400, Killer = "old source" };
        new DeathDetails { Killer = "reported source", PrimaryElement = "Fire", Damage = 500, Crit = false }.Apply(record);
        Eq(DeathKind.Burst, record.Kind);
        Eq(4, record.Hits);
        Eq(1400f, record.WindowDamage);
        Eq("reported source", record.Killer);
        Eq(false, record.KillingCrit);
    }

    static void Test_LateGameReport_PersistsSameRecordAndCount()
    {
        string dir = Path.Combine(Path.GetTempPath(), "terrible-death-test-" + Guid.NewGuid());
        try
        {
            var log = new DeathLog(dir);
            var record = new DeathRecord { Character = "Medick", UtcTime = DateTime.UtcNow };
            log.Append(record);
            new DeathDetails { Killer = "Wengari", Damage = 900, PrimaryElement = "Cold" }.Apply(record);
            True(log.SaveUpdated(), "details saved");
            var loaded = new DeathLog(dir);
            loaded.Load();
            Eq(1, loaded.CountFor("Medick"));
            Eq(1, loaded.All[0].Number);
            Eq("Wengari", loaded.All[0].Killer);
            Eq(900f, loaded.All[0].KillingBlow);
            True(File.ReadAllText(loaded.TextPath).Contains("Wengari"), "human log updated too");
            True(File.Exists(loaded.JsonPath + ".bak"), "previous log retained");
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    static void Test_CounterReset_PersistsPerCharacterAndKeepsHistory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "terrible-death-test-" + Guid.NewGuid());
        try
        {
            var log = new DeathLog(dir);
            log.Append(new DeathRecord { Character = "Medick" });
            log.Append(new DeathRecord { Character = "Medick" });
            string path = Path.Combine(dir, "counter-resets.json");
            var resets = new CounterResets(path);
            True(resets.Reset("Medick", log.CountFor("Medick")), "reset saved");
            Eq(0, resets.Count("Medick", log.CountFor("Medick")));
            Eq(7, resets.Count("Other", 7));
            log.Append(new DeathRecord { Character = "Medick" });
            resets = new CounterResets(path);
            Eq(1, resets.Count("Medick", log.CountFor("Medick")));
            Eq(3, log.CountFor("Medick"));
            Eq(3, log.All.Last().Number);
            Eq(0, resets.Count("Medick", 0));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

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

    // Review 2026-10-01 #11: AilmentID names that only CONTAIN a keyword.
    static void Test_AilmentFind_ExactIdBeforeSubstring()
    {
        Eq(null, Ailments.Find("ShrineStun")?.Name);        // a shrine buff, not a stun on you
        Eq("Stun", Ailments.Find("Stun")?.Name);
        Eq("Chill", Ailments.Find("Chill")?.Name);
    }

    // Review 2026-10-01 #4: the game's own Deaths counter must not double count
    // a death a hook already recorded, however late the counter moves.
    static void Test_Ledger_CounterLagsLongAfterHook()
    {
        var l = new DeathCountLedger();
        l.Observe(5, 0);                 // baseline
        l.Recorded(10);                  // hook recorded a death at t=10
        Eq(0, l.Observe(6, 200), "counter moved 190 s later: same death, nothing new");
    }

    static void Test_Ledger_UnexplainedIncreaseRecords()
    {
        var l = new DeathCountLedger();
        l.Observe(5, 0);
        Eq(1, l.Observe(6, 30), "no hook saw it: one new death");
        Eq(0, l.Observe(6, 31), "no change");
    }

    static void Test_Ledger_FirstReadIsBaseline_AndCreditsExpire()
    {
        var l = new DeathCountLedger();
        l.Recorded(0);                   // hook fired but the counter never moved
        Eq(0, l.Observe(9, 1), "first read is only a baseline");
        Eq(1, l.Observe(10, DeathCountLedger.CreditSeconds + 5), "stale credit does not swallow a later real death");
    }

    static void Test_Ledger_ResetOnCharacterChange()
    {
        var l = new DeathCountLedger();
        l.Observe(5, 0); l.Recorded(1);
        l.Reset();
        Eq(0, l.Observe(40, 2), "new character baseline");
        Eq(1, l.Observe(41, 3), "credit from the old character is gone");
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

    // ── Defences snapshot (your stats at death) ─────────────
    static DeathRecord FireOneShot(Dictionary<string, float> def)
    {
        var r = DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 900, "Lagon", Element.Fire, crit: true) }, 10, Ctx());
        r.Defenses = def;
        return r;
    }

    static void Test_Advice_UsesYourResistance()
    {
        var tip = Advisor.Suggest(FireOneShot(new() { ["Res.Fire"] = 41f })).Single(t => t.Key == "res_Fire");
        True(tip.Title.Contains("41%"), "title shows your value: " + tip.Title);
        True(tip.Body.Contains("34"), "body shows the gap to 75: " + tip.Body);
    }

    static void Test_Advice_CappedResistanceSaysSo()
    {
        var tips = Advisor.Suggest(FireOneShot(new() { ["Res.Fire"] = 75f }));
        var tip = tips.Single(t => t.Key == "res_Fire");
        True(tip.Title.Contains("capped"), "capped: " + tip.Title);
        True(tips.IndexOf(tip) > 0, "a capped resistance is not the top fix");
    }

    static void Test_Advice_UsesYourCritAvoidance()
    {
        var tip = Advisor.Suggest(FireOneShot(new() { ["CritAvoidance"] = 60f })).Single(t => t.Key == "crit");
        True(tip.Body.Contains("60%"), "crit body shows your value: " + tip.Body);
    }

    static void Test_Advice_NoSnapshotKeepsGenericText()
    {
        var tip = Advisor.Suggest(FireOneShot(null)).Single(t => t.Key == "res_Fire");
        Eq("Cap fire resistance (75%)", tip.Title);
    }

    static void Test_Log_DefensesRoundTrip()
    {
        string dir = Path.Combine(Path.GetTempPath(), "dc_def_" + Guid.NewGuid().ToString("N"));
        try
        {
            var log = new DeathLog(dir); log.Load();
            log.Append(FireOneShot(new() { ["Res.Fire"] = 41f, ["Armor"] = 1200f }));
            var again = new DeathLog(dir); again.Load();
            Eq(41f, again.All[0].Defenses["Res.Fire"], "survives json");
        }
        finally { try { Directory.Delete(dir, true); } catch { } }
    }

    // Review 2026-10-01 (defences): the 75 vs 0.75 scale must never invent a number.
    static Dictionary<string, float> Raw(params (string k, float v)[] kv) => kv.ToDictionary(x => x.k, x => x.v);

    static void Test_Defenses_PercentUnits()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", 41f), ("Res.Cold", 88f), ("CritAvoidance", 60f), ("Armor", 1200f)));
        Eq(41f, d["Res.Fire"]); Eq(75f, d["Res.Cold"], "capped"); Eq(88f, d["ResUncapped.Cold"], "headroom kept");
        Eq(60f, d["CritAvoidance"]); Eq(1200f, d["Armor"]);
    }

    static void Test_Defenses_FractionUnits()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", 0.41f), ("Res.Cold", 1.6f), ("CritAvoidance", 0.6f)));
        True(Math.Abs(d["Res.Fire"] - 41f) < 0.01f, "fraction scaled");
        Eq(75f, d["Res.Cold"], "160% overcap read as fraction, capped");
        True(Math.Abs(d["CritAvoidance"] - 60f) < 0.01f, "percent stat scaled too");
    }

    // Case A: percent units, a young character with Fire 1 and the rest 0.
    static void Test_Defenses_WholeSmallValuesAreNoSignal()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", 1f), ("Res.Cold", 0f), ("Res.Void", -1f), ("Block", 1f), ("Armor", 50f)));
        True(!d.ContainsKey("Res.Fire") && !d.ContainsKey("Block"), "unknown scale: no percents reported");
        Eq(50f, d["Armor"], "flat stats still reported");
    }

    static void Test_Defenses_ConflictingEvidenceReportsNothing()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", 0.5f), ("Res.Cold", 40f)));
        True(!d.ContainsKey("Res.Fire") && !d.ContainsKey("Res.Cold"), "conflict: no percents");
    }

    static void Test_Defenses_ImplausiblePercentDropped()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", 40f), ("CritAvoidance", 250f)));
        True(!d.ContainsKey("CritAvoidance"), "over 100% crit avoidance is a misread");
        Eq(40f, d["Res.Fire"]);
    }

    static void Test_Defenses_NegativeResistanceKept()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", -50f), ("Res.Cold", 30f)));
        Eq(-50f, d["Res.Fire"]);
    }

    static void Test_Advice_CapThresholdEdges()
    {
        True(!Advisor.Suggest(FireOneShot(new() { ["Res.Fire"] = 74.4f })).Single(t => t.Key == "res_Fire").Title.Contains("capped"), "74.4 not capped");
        True(Advisor.Suggest(FireOneShot(new() { ["Res.Fire"] = 74.6f })).Single(t => t.Key == "res_Fire").Title.Contains("capped"), "74.6 capped");
        True(Advisor.Suggest(FireOneShot(new() { ["Res.Fire"] = 74f })).Single(t => t.Key == "res_Fire").Body.Contains("1 point short"), "singular");
    }

    static void Test_Advice_CrittedDespiteFullAvoidanceDoesNotClaimImmunity()
    {
        var body = Advisor.Suggest(FireOneShot(new() { ["CritAvoidance"] = 100f })).Single(t => t.Key == "crit").Body;
        True(!body.Contains("You had 100%"), body);
    }

    static void Test_Advice_FreezeTipKnowsColdIsCapped()
    {
        var r = DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 900, "Wraith", Element.Cold) }, 10, Ctx(), new[] { "Freeze" });
        r.Defenses = new() { ["Res.Cold"] = 75f };
        var body = Advisor.Suggest(r).Single(t => t.Key == "cc_freeze").Body;
        True(!body.Contains("Keep cold resistance capped"), body);
    }

    static void Test_DefenseLine_MarksCapAndWardTiming()
    {
        var r = new DeathRecord { Defenses = new() { ["Res.Fire"] = 75f, ["Res.Cold"] = 41f, ["Ward"] = 120f } };
        var line = r.DefenseLine();
        True(line.Contains("Fire 75% (cap)") && line.Contains("Cold 41%") && !line.Contains("Cold 41% (cap)"), line);
        True(line.Contains("Ward 120 (after hit)"), line);
    }

    // ── Patterns (history across deaths) ─────────────────────
    static DeathRecord Died(string killer, Element el, float amt = 900, string ailment = null)
    {
        var hits = new List<HitEvent> { Hit(10, amt, killer, el, ailment) };
        return DeathAnalyzer.Analyze(hits, 10, Ctx());
    }

    static void Test_Patterns_TopKillersAndDamageShare()
    {
        var log = new List<DeathRecord> { Died("Lagon", Element.Fire), Died("Lagon", Element.Fire), Died("Rat", Element.Physical), Died("", Element.Cold) };
        var p = DeathPatterns.Build(log);
        Eq(4, p.Deaths, "deaths");
        Eq("Lagon", p.TopKillers[0].Name, "top killer");
        Eq(2, p.TopKillers[0].Count, "count");
        True(p.TopKillers.All(k => !string.IsNullOrEmpty(k.Name)), "unnamed killers skipped");
        Eq(Element.Fire, p.ElementShares[0].Element, "fire dominates");
        True(Math.Abs(p.ElementShares.Sum(e => e.Share) - 1f) < 0.001f, "shares sum to 1");
    }

    // The point of the view: "what should I invest in", counted across deaths.
    static void Test_Patterns_PrioritiesCountDeathsTheyWouldHaveHelped()
    {
        var log = new List<DeathRecord> { Died("Lagon", Element.Fire), Died("Imp", Element.Fire), Died("Fire Wraith", Element.Fire), Died("Brute", Element.Physical) };
        var p = DeathPatterns.Build(log);
        Eq("res_Fire", p.Priorities[0].Advice.Key, "fire res first");
        Eq(3, p.Priorities[0].Deaths, "helps 3 of 4");
        True(p.Priorities.Count <= DeathPatterns.MaxPriorities, "capped");
        True(p.Priorities.Any(x => x.Advice.Key == "ehp" && x.Deaths == 4), "generic tip still listed, with its true count");
        Eq("ehp", p.Priorities[1].Advice.Key, "ehp beats endurance on the weight tiebreak");
    }

    // Review: per-death-only tips must never become priorities. Needs an
    // Unknown death in the window, or the check cannot fail.
    static void Test_Patterns_UnknownDeathNeverAPriority()
    {
        var log = new List<DeathRecord> { DeathAnalyzer.Analyze(null, 10, Ctx()), DeathAnalyzer.Analyze(null, 11, Ctx()), Died("Rat", Element.Physical) };
        var p = DeathPatterns.Build(log);
        True(p.Priorities.All(x => x.Advice.Key != "unknown"), "unknown excluded");
        Eq(3, p.Deaths, "unknown deaths still count toward M");
    }

    // Review: a tip outside a death's top 3 still helped that death. A one-shot
    // 40% physical / 30% fire / 30% cold must credit cold resistance.
    static DeathRecord Mixed()
    {
        var h = new HitEvent { Time = 10, Amount = 900, Source = "Golem", MaxHealth = 1000, ByElement = new float[Elements.Count] };
        h.ByElement[(int)Element.Physical] = 360; h.ByElement[(int)Element.Fire] = 270; h.ByElement[(int)Element.Cold] = 270;
        return DeathAnalyzer.Analyze(new List<HitEvent> { h }, 10, Ctx());
    }

    static void Test_Patterns_CountsEveryTipNotJustTopThree()
    {
        var p = DeathPatterns.Build(new List<DeathRecord> { Mixed(), Mixed(), Died("Imp", Element.Fire) }, maxPriorities: 10);
        var cold = p.Priorities.FirstOrDefault(x => x.Advice.Key == "res_Cold");
        True(cold != null && cold.Deaths == 2, "cold credited in both mixed deaths");
        Eq(3, p.Priorities.First(x => x.Advice.Key == "res_Fire").Deaths, "fire in all three");
    }

    // Review: one huge one-shot must not drown ten ordinary deaths.
    static void Test_Patterns_ElementSharesWeighEachDeathEqually()
    {
        var log = new List<DeathRecord> { Died("Boss", Element.Void, 50000) };
        for (int i = 0; i < 3; i++) log.Add(Died("Imp", Element.Fire, 900));
        var p = DeathPatterns.Build(log);
        Eq(Element.Fire, p.ElementShares[0].Element, "fire is 3 of 4 deaths");
        True(Math.Abs(p.ElementShares[0].Share - 0.75f) < 0.001f, "per-death normalised");
    }

    // Found in the mockup: a priority's "why" was copied from the latest
    // death ("One hit took 79% of your life") while claiming N deaths.
    static void Test_Patterns_PriorityBodyDescribesThePattern()
    {
        var log = new List<DeathRecord> { Died("Lagon", Element.Fire), Died("Imp", Element.Fire, 990), Died("Brute", Element.Physical) };
        var p = DeathPatterns.Build(log);
        foreach (var x in p.Priorities)
            True(!x.Advice.Body.Contains("One hit took") && !x.Advice.Body.Contains("of the damage that killed you"),
                 $"{x.Advice.Key}: body is about one death: {x.Advice.Body}");
        True(p.Priorities.First(x => x.Advice.Key == "res_Fire").Advice.Body.Contains("2 of 3"), "fire body names the count");
    }

    // Found in the mockup: "Cap fire resistance" and "Ignite: fire resistance"
    // both made the top three, saying the same thing twice.
    static void Test_Patterns_AilmentTipFoldsIntoItsResistance()
    {
        var log = new List<DeathRecord>();
        for (int i = 0; i < 4; i++) log.Add(DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 900, "Lagon", Element.Fire) }, 10, Ctx(), new[] { "Ignite" }));
        var p = DeathPatterns.Build(log, maxPriorities: 10);
        True(p.Priorities.Any(x => x.Advice.Key == "res_Fire"), "fire res listed");
        True(p.Priorities.All(x => x.Advice.Key != "dot_Ignite"), "ignite folded into fire res");
    }

    static void Test_Patterns_WindowIsMostRecent()
    {
        var log = new List<DeathRecord>();
        for (int i = 0; i < 30; i++) log.Add(Died("Old Boss", Element.Cold));
        for (int i = 0; i < 20; i++) log.Add(Died("New Boss", Element.Void));
        var p = DeathPatterns.Build(log, window: 20);
        Eq(20, p.Deaths, "window");
        Eq("New Boss", p.TopKillers.Single().Name, "only recent deaths");
    }

    static void Test_Patterns_Empty()
    {
        var p = DeathPatterns.Build(new List<DeathRecord>());
        Eq(0, p.Deaths); Eq(0, p.Priorities.Count); Eq(0, p.TopKillers.Count);
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

    // ── Mitigation math (research/advice/03-MATH.md) ────────
    // Every test below pins a row of the regression table in section 7 of
    // that document. Near() is the 0.001 tolerance the table promises.
    static void Near(float want, float got, string what, float tol = 0.001f)
    {
        if (float.IsNaN(want) != float.IsNaN(got) || Math.Abs(want - got) > tol)
            throw new Exception($"{what} expected <{want}> got <{got}>");
    }

    // A1: armor curve vs three planner builds (46/48/52 percent at L100).
    static void Test_Math_Armor_MatchesPlannerDataPoints()
    {
        Near(0.4595f, MitigationMath.ArmorMitigationFraction(2763, 100), "2763 @ L100");
        Near(0.4820f, MitigationMath.ArmorMitigationFraction(2976, 100), "2976 @ L100");
        Near(0.5199f, MitigationMath.ArmorMitigationFraction(3366, 100), "3366 @ L100");
    }

    // A2: the 2020 forum quote of Tunklab's chart, "701 armor = about 20%".
    static void Test_Math_Armor_MatchesTunklabChartQuote()
    {
        Near(0.1920f, MitigationMath.ArmorMitigationFraction(701, 100), "701 @ L100");
    }

    // A3 + A6: the two terms asymptote at 0.30 + 0.55, the documented 85% cap.
    static void Test_Math_Armor_CapsAt85()
    {
        Near(0.85f, MitigationMath.ArmorMitigationFraction(1e12f, 100), "huge armor capped");
        Eq(0f, MitigationMath.ArmorMitigationFraction(0f, 100), "zero armor mitigates nothing");
    }

    // A4: armor is 70% as effective against non-physical damage.
    static void Test_Math_Armor_NonPhysicalIs70Percent()
    {
        Near(0.3217f, MitigationMath.ArmorMitigationNonPhysicalFraction(2763, 100), "2763 non-physical");
        Near(MitigationMath.ArmorMitigationFraction(2763, 100) * 0.7f,
             MitigationMath.ArmorMitigationNonPhysicalFraction(2763, 100), "exactly 70 percent of physical");
    }

    // A5: armor shred below zero increases damage taken by the same magnitude.
    static void Test_Math_Armor_NegativeMirrorsPositive()
    {
        Near(-0.1919f, MitigationMath.ArmorMitigationFraction(-700, 100), "-700 @ L100");
        Near(-MitigationMath.ArmorMitigationFraction(2763, 100),
              MitigationMath.ArmorMitigationFraction(-2763, 100), "mirror at 2763");
    }

    // A6: unknown inputs refuse instead of guessing.
    static void Test_Math_Armor_ZeroAndNaN()
    {
        True(float.IsNaN(MitigationMath.ArmorMitigationFraction(float.NaN, 100)), "NaN armor");
        True(float.IsNaN(MitigationMath.ArmorMitigationFraction(2763, float.NaN)), "NaN area level");
    }

    // B1: block effectiveness 325 at L100 computes to 22%, planner shows 22%.
    static void Test_Math_Block_MatchesPlannerDataPoint()
    {
        Near(0.2214f, MitigationMath.BlockMitigationFraction(325, 100), "325 @ L100");
    }

    // B2: 0.25 + 0.60 terms asymptote at the documented 85% cap.
    static void Test_Math_Block_CapsAt85()
    {
        Near(0.85f, MitigationMath.BlockMitigationFraction(1e12f, 100), "huge block capped");
        Eq(0f, MitigationMath.BlockMitigationFraction(0f, 100), "zero block");
    }

    // C1: 16 dodge rating at L100 gives 0.62%, planner rounds to 1%.
    static void Test_Math_Dodge_MatchesPlannerDataPoint()
    {
        Near(0.0062f, MitigationMath.DodgeChanceFraction(16, 100), "16 rating @ L100");
    }

    // C2: the curve value at 1000 rating and the 85% cap.
    static void Test_Math_Dodge_CurveAndCap()
    {
        Near(0.2909f, MitigationMath.DodgeChanceFraction(1000, 100), "1000 rating @ L100");
        Near(0.85f, MitigationMath.DodgeChanceFraction(1e12f, 100), "huge rating capped");
    }

    // D1: 1% enemy penetration per area level, capped at 75; NaN refuses.
    static void Test_Math_Penetration_ByAreaLevel()
    {
        Near(40f, MitigationMath.EnemyPenetration(40), "area 40");
        Near(75f, MitigationMath.EnemyPenetration(75), "area 75");
        Near(75f, MitigationMath.EnemyPenetration(100), "area 100");
        Near(75f, MitigationMath.EnemyPenetration(120), "area 120 capped");
        Eq(0f, MitigationMath.EnemyPenetration(0), "area 0");
        True(float.IsNaN(MitigationMath.EnemyPenetration(float.NaN)), "unknown area level");
    }

    // D2: shred before the cap, penetration after it, order pinned.
    static void Test_Math_Resist_EffectiveOrderShredCapPen()
    {
        Near(-34f, MitigationMath.EffectiveResistance(41, 0, 75), "41 uncapped, pen 75");
        Near(-20f, MitigationMath.EffectiveResistance(75, 20, 75), "shred before cap, pen after");
        Near(75f, MitigationMath.EffectiveResistance(95, 20, 0), "overcap absorbs shred");
        Near(75f, MitigationMath.EffectiveResistance(150, 60, 0), "deep overcap absorbs heavy shred");
    }

    // D3: negative effective resistance means more than 100% damage taken.
    static void Test_Math_Resist_NegativeGrowsDamage()
    {
        Near(1.34f, MitigationMath.ResistanceTakenFraction(-34), "-34% means 134% taken");
        Near(1.20f, MitigationMath.ResistanceTakenFraction(-20), "-20% means 120% taken");
    }

    // D4: the cap clamps at 75 regardless of overcap when nothing shreds or penetrates.
    static void Test_Math_Resist_OvercapClampedAtCap()
    {
        Near(75f, MitigationMath.EffectiveResistance(150, 0, 0), "150 clamped to 75");
        Near(0.25f, MitigationMath.ResistanceTakenFraction(MitigationMath.EffectiveResistance(150, 0, 0)), "25% taken at cap");
    }

    // D5: shred can push an uncapped resistance below zero.
    static void Test_Math_Resist_ShredBelowZero()
    {
        Near(-30f, MitigationMath.EffectiveResistance(10, 40, 0), "10 res, 40 shred");
        Near(-60f, MitigationMath.EffectiveResistance(75, 60, 75), "poison at 30 stacks in a L100 area");
    }

    // E1: worked example (a), fire 41% vs the 75% cap in a L100 area.
    static void Test_Math_ExampleA_Fire41_Capped_WithPen()
    {
        var cf = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PostMitigationWithWard);
        Near(0.7463f, cf.Ratio, "ratio");
        Near(1268.66f, cf.NewDamage, "new hit", 0.01f);
        Near(1000f, cf.RemainingEffectiveHealth, "you had 1000 left");
        Eq(false, cf.Survived, "1270 still kills");
        Near(0.588f, MitigationMath.SurvivalBudgetRatio(1700, 700), "survival budget");
        // capping flips the verdict only at area level 23 or lower (doc (a) step 8)
        Eq(true, MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 23, DamageMeaning.PostMitigationWithWard).Survived, "area 23 survives");
        Eq(false, MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 24, DamageMeaning.PostMitigationWithWard).Survived, "area 24 still dies");
    }

    // E2: the honest sentence for a lever that was not enough.
    static void Test_Math_ExampleA_WordingStillKilled()
    {
        var s = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PostMitigationWithWard).Sentence();
        True(s.Contains("from about 1700 to about 1270"), s);
        True(s.Contains("still above the about 1000 you had left, so it would probably still have killed you"), s);
    }

    // E3: without area-level penetration the verdict flips completely.
    static void Test_Math_ExampleA_Fire41_NoPen_Survives()
    {
        var cf = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 0, DamageMeaning.PostMitigationWithWard);
        Near(0.4237f, cf.Ratio, "ratio");
        Near(720.34f, cf.NewDamage, "new hit", 0.01f);
        Eq(true, cf.Survived, "720 < 1000 left");
        True(cf.Sentence().Contains("would likely have survived"), cf.Sentence());
    }

    // E4: at area 40 and 66 the same gap still kills.
    static void Test_Math_ExampleA_Fire40Area_StillDies()
    {
        var cf40 = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 40, DamageMeaning.PostMitigationWithWard);
        Near(0.6566f, cf40.Ratio, "area 40 ratio");
        Near(1116.16f, cf40.NewDamage, "area 40 new hit", 0.01f);
        Eq(false, cf40.Survived, "area 40");
        var cf66 = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 66, DamageMeaning.PostMitigationWithWard);
        Near(0.7280f, cf66.Ratio, "area 66 ratio");
        Near(1237.6f, cf66.NewDamage, "area 66 new hit", 0.01f);
        Eq(false, cf66.Survived, "area 66");
    }

    // E5: under the pre-mitigation reading the ratio holds but no survival claim.
    static void Test_Math_ExampleA_PreMitigationNoSurvivalClaim()
    {
        var cf = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PreMitigation);
        Near(0.7463f, cf.Ratio, "same ratio");
        Eq(true, cf.Survived == null, "survival not judged");
        True(cf.Note.Contains("pre-mitigation"), cf.Note);
        True(cf.Sentence().Contains("which may or may not have been enough"), cf.Sentence());
    }

    // E6: the ratio is identical under every damage meaning (layer cancels).
    static void Test_Math_RatioInvariantToDamageMeaning()
    {
        float a = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PostMitigationWithWard).Ratio;
        float b = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PostMitigationHealthOnly).Ratio;
        float c = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PreMitigation).Ratio;
        Near(a, b, "A vs health-only");
        Near(b, c, "health-only vs pre-mitigation");
    }

    // E7: capped resistance in a L100 area is a dead end, even with overcap.
    static void Test_Math_ExampleB_CappedResistanceIsADeadEnd()
    {
        var cf = MitigationMath.PreviewResistance(2200, 400, 75, 150, 0, 100, DamageMeaning.PostMitigationWithWard);
        Eq(1.0f, cf.Ratio, "penetration applies after the cap: ratio 1");
        Near(2200f, cf.NewDamage, "hit unchanged");
        Eq(false, cf.Survived, "still dead");
    }

    // E8: endurance 20 to 60 lands inside the borderline band.
    static void Test_Math_ExampleB_EnduranceMayOrMayNot()
    {
        var cf = MitigationMath.PreviewEndurance(2200, 400, 400, 20, 60, DamageMeaning.PostMitigationWithWard);
        Near(0.8182f, cf.Ratio, "ratio");
        Near(1800f, cf.NewDamage, "new hit");
        Eq(false, cf.Survived, "1800 is not below 1800");
        True(cf.Sentence().Contains("may or may not have been enough"), cf.Sentence());
    }

    // E9: the overkill is the exact shortfall for pool levers.
    static void Test_Math_ExampleB_PoolShortfallIsOverkill()
    {
        var cf = MitigationMath.PreviewPoolIncrease(2200, 400, 400, DamageMeaning.PostMitigationWithWard, "400 more health and ward");
        Eq(true, cf.Survived, "400 covers a 400 shortfall");
        True(cf.Sentence().Contains("short by about 400") && cf.Sentence().Contains("about equal to the shortfall"), cf.Sentence());
        var enough = MitigationMath.PreviewPoolIncrease(2200, 400, 500, DamageMeaning.PostMitigationWithWard, "500 more health and ward");
        Eq(true, enough.Survived, "500 clearly enough");
        True(enough.Sentence().Contains("would have absorbed this exact hit"), enough.Sentence());
        var shortOf = MitigationMath.PreviewPoolIncrease(2200, 400, 200, DamageMeaning.PostMitigationWithWard, "200 more health and ward");
        Eq(false, shortOf.Survived, "200 is not enough");
        True(shortOf.Sentence().Contains("would not have been enough on its own"), shortOf.Sentence());
    }

    // E10: a blocked version of the (b) hit lands below what you had left.
    static void Test_Math_ExampleB_BlockedVersionSurvives()
    {
        float block = MitigationMath.BlockMitigationFraction(325, 100);
        Near(0.2214f, block, "block effectiveness 325 @ L100");
        float blocked = 2200 * (1 - block);
        Near(1712.9f, blocked, "blocked version of 2200", 0.01f);
        True(blocked < 1800, "blocked version below the about 1800 you had left");
    }

    // E11: armor never mitigates damage over time; the preview refuses.
    static void Test_Math_ExampleC_ArmorRefusesDot()
    {
        var cf = MitigationMath.PreviewArmor(350, 30, 2763, 4000, 100, false, true, DamageMeaning.PostMitigationWithWard);
        True(float.IsNaN(cf.Ratio), "no ratio for a DoT");
        True(cf.Sentence().Contains("armor does not mitigate damage over time"), cf.Sentence());
    }

    // E12: a 15% less damage over time layer on the ignite tick.
    static void Test_Math_ExampleC_LessDotTakenTick()
    {
        var cf = MitigationMath.PreviewLessDamageTaken(350, 30, 15, DamageMeaning.PostMitigationWithWard, "a 15% less damage over time taken layer");
        Near(0.85f, cf.Ratio, "ratio");
        Near(297.5f, cf.NewDamage, "tick");
        Near(320f, cf.RemainingEffectiveHealth, "you had 320 left");
        True(cf.Sentence().Contains("may or may not have been enough"), cf.Sentence() + " (single tick, other stacks keep ticking)");
    }

    // E13: bleed is physical but still a DoT: physical resistance works, armor does not.
    static void Test_Math_ExampleD_BleedPhysicalRes()
    {
        var cf = MitigationMath.PreviewResistance(300, 40, 20, 75, 0, 100, DamageMeaning.PostMitigationWithWard);
        Near(0.6452f, cf.Ratio, "ratio");
        Near(193.55f, cf.NewDamage, "tick", 0.01f);
        Near(260f, cf.RemainingEffectiveHealth, "you had 260 left");
        Eq(true, cf.Survived, "this tick would not have killed you");
    }

    // E14: poison overcap is MEASURED advice when stacks were on you.
    static void Test_Math_ExampleE_PoisonOvercapMeasured()
    {
        float shred = 10 * MitigationMath.PoisonResShredPerStack;
        Near(20f, shred, "10 poison stacks shred 20");
        var cf = MitigationMath.PreviewResistance(900, 100, 75, 95, shred, 100, DamageMeaning.PostMitigationWithWard);
        Near(0.8333f, cf.Ratio, "ratio");
        Near(750f, cf.NewDamage, "tick");
        Near(800f, cf.RemainingEffectiveHealth, "you had 800 left");
        Eq(true, cf.Survived, "this tick survives");
        True(cf.Sentence().Contains("may or may not have been enough"), cf.Sentence() + " (inside the band)");
    }

    // E15: without evidenced shred, overcap changes nothing (ratio 1, never advised).
    static void Test_Math_ExampleE_OvercapNeedsEvidencedShred()
    {
        var cf = MitigationMath.PreviewResistance(900, 100, 75, 95, 0, 100, DamageMeaning.PostMitigationWithWard);
        Eq(1.0f, cf.Ratio, "no shred: overcap does nothing");
        Eq(false, cf.Survived, "still dead");
    }

    // E16: full crit avoidance turns the crit into the normal hit: ratio one half.
    static void Test_Math_ExampleF_AvoidanceHalvesCrit()
    {
        var cf = MitigationMath.PreviewCrit(2000, 600, true, 0, 100, DamageMeaning.PostMitigationWithWard);
        Near(0.5f, cf.Ratio, "crit is 200%, normal is 100%");
        Near(1000f, cf.NewDamage, "normal-hit version");
        Near(1400f, cf.RemainingEffectiveHealth, "you had 1400 left");
        Eq(true, cf.Survived, "1000 < 1400");
        True(cf.Sentence().Contains("would likely have survived, assuming the recorded damage was the whole post-mitigation hit including ward"), cf.Sentence());
    }

    // E17: reduced bonus crit damage 50 leaves the hit lethal.
    static void Test_Math_ExampleF_ReducedBonusCrit()
    {
        Near(2f, MitigationMath.CritHitMultiplier(0), "no reduction: 200%");
        Near(1.5f, MitigationMath.CritHitMultiplier(50), "50: 150%");
        Near(1f, MitigationMath.CritHitMultiplier(100), "100: a normal hit");
        Near(1f, MitigationMath.CritHitMultiplier(150), "capped at 100");
        var cf = MitigationMath.PreviewCrit(2000, 600, true, 0, 50, DamageMeaning.PostMitigationWithWard);
        Near(0.75f, cf.Ratio, "ratio");
        Near(1500f, cf.NewDamage, "new hit");
        Eq(false, cf.Survived, "1500 still kills");
    }

    // E18: avoidance rolls after the enemy's crit chance (official 30/50 gives 15).
    static void Test_Math_ExampleF_EffectiveCritChance()
    {
        Near(2f, MitigationMath.EffectiveEnemyCritChance(5, 60), "5% enemy chance, 60% avoidance");
        Near(15f, MitigationMath.EffectiveEnemyCritChance(30, 50), "official example");
        Near(0f, MitigationMath.EffectiveEnemyCritChance(5, 100), "full avoidance: never crit");
    }

    // E19: a non-crit killing blow gets no crit advice.
    static void Test_Math_Crit_NonCritRefuses()
    {
        var cf = MitigationMath.PreviewCrit(1000, 100, false, 0, 100, DamageMeaning.PostMitigationWithWard);
        True(float.IsNaN(cf.Ratio), "no ratio");
        True(cf.Sentence().Contains("not a critical strike"), cf.Sentence());
    }

    // E20 + E21: the survival budget says how much one lever must deliver.
    static void Test_Math_ExampleG_SmallOverkillThinMargin()
    {
        Near(1000f, MitigationMath.RemainingEffectiveHealth(1050, 50), "remaining");
        Near(0.952f, MitigationMath.SurvivalBudgetRatio(1050, 50), "budget: razor thin");
    }

    static void Test_Math_ExampleG_LargeOverkillNeedsAvoidance()
    {
        Near(1050f, MitigationMath.RemainingEffectiveHealth(5000, 3950), "remaining");
        Near(0.21f, MitigationMath.SurvivalBudgetRatio(5000, 3950), "budget: no single layer delivers a 79% cut");
    }

    // E22: missing max health blocks the endurance preview, not survival math.
    static void Test_Math_ExampleH_MissingMaxHealth()
    {
        var cf = MitigationMath.PreviewEndurance(1050, 50, float.NaN, 20, 60, DamageMeaning.PostMitigationWithWard);
        True(float.IsNaN(cf.Ratio), "endurance refuses");
        True(cf.Sentence().Contains("endurance threshold is unknown"), cf.Sentence());
        Eq(true, MitigationMath.PreviewResistance(1050, 50, 41, 75, 0, 100, DamageMeaning.PostMitigationWithWard).Survived != null,
           "survival never needed max health");
    }

    // E23: ward unknown means survival is not judged, the ratio still is.
    static void Test_Math_ExampleH_HealthOnlyMeaning()
    {
        var cf = MitigationMath.PreviewResistance(1700, 700, 41, 75, 0, 100, DamageMeaning.PostMitigationHealthOnly);
        Near(0.7463f, cf.Ratio, "ratio intact");
        Eq(true, cf.Survived == null, "ward split unknown");
        True(cf.Note.Contains("ward at the hit is unknown"), cf.Note);
        True(cf.Sentence().Contains("which may or may not have been enough"), cf.Sentence());
    }

    // E24: armor 2763 to 4000 on the (i) hit lands inside the band.
    static void Test_Math_ExampleI_ArmorRatio()
    {
        var cf = MitigationMath.PreviewArmor(1800, 300, 2763, 4000, 100, false, false, DamageMeaning.PostMitigationWithWard);
        Near(0.7904f, cf.Ratio, "ratio");
        Near(1422.68f, cf.NewDamage, "new hit", 0.01f);
        Near(1500f, cf.RemainingEffectiveHealth, "you had 1500 left");
        Eq(true, cf.Survived, "1423 < 1500");
        True(cf.Sentence().Contains("may or may not have been enough"), cf.Sentence() + " (77 <= 150 band)");
    }

    // E25: armor plus endurance clears what neither clears alone.
    static void Test_Math_ExampleI_ArmorPlusEndurance()
    {
        float armorRatio = MitigationMath.ArmorRatio(2763, 4000, 100, false);
        float newHit = 1800 * armorRatio;                       // 1422.7
        float above = 1500 - 400;                                // health at hit minus threshold
        float belowTaken = newHit - above;                       // after old endurance
        float pre = belowTaken / (1 - 0.20f);                    // undo old endurance
        float newTaken = above + pre * (1 - 0.60f);              // reapply new endurance
        Near(0.7007f, newTaken / 1800, "combined ratio");
        Near(1261.34f, newTaken, "combined new hit", 0.01f);
        True(Math.Abs(newTaken - 1500) > 0.10f * 1500, "outside the band: a clear survive");
        Near(MitigationMath.CombineRatios(armorRatio, newTaken / newHit), newTaken / 1800, "CombineRatios agrees");
    }

    // E26: the same armor upgrade is a weak lever against an elemental hit.
    static void Test_Math_ExampleI_ArmorWeakVsElemental()
    {
        var cf = MitigationMath.PreviewArmor(1800, 300, 2763, 4000, 100, true, false, DamageMeaning.PostMitigationWithWard);
        Near(0.8831f, cf.Ratio, "non-physical ratio");
        True(cf.Ratio > 0.85f, "armor is a weak lever against elemental hits, and the number says so");
    }

    // F1: a hit entirely above the endurance threshold gets nothing from endurance.
    static void Test_Math_Endurance_AboveThresholdNoEffect()
    {
        var cf = MitigationMath.PreviewEndurance(500, 0, 0, 20, 60, DamageMeaning.PostMitigationWithWard);
        Eq(1.0f, cf.Ratio, "ratio 1");
        True(cf.Note.Contains("whole hit landed above the endurance threshold"), cf.Note);
    }

    // F2: the official split example, 100 damage with 50 below the threshold.
    static void Test_Math_Endurance_OfficialSplitExample()
    {
        Near(90f, MitigationMath.EnduranceApplied(100, 1000, 950, 20), "50 plain + 50 at 20% endurance");
    }

    // F3: at or below the threshold the whole damage instance is endurance-d.
    static void Test_Math_Endurance_FullyBelowThreshold()
    {
        Near(80f, MitigationMath.EnduranceApplied(100, 400, 400, 20), "health at the threshold");
        Near(80f, MitigationMath.EnduranceApplied(100, 50, 400, 20), "health below the threshold");
    }

    // F4: ward decay, the 1.1 formula; retention enters as a fraction.
    static void Test_Math_WardDecay_Formula()
    {
        Near(208.333f, MitigationMath.WardDecayPerSecond(1000, 40), "1000 ward, 40 retention");
        Near(112.5f, MitigationMath.WardDecayPerSecond(500, 0), "500 ward, no retention");
        Near(125f, MitigationMath.WardDecayPerSecond(1000, 200), "200 retention halves the decay");
        Eq(0f, MitigationMath.WardDecayPerSecond(0, 40), "no ward, no decay");
    }

    // F5: 41 vs 0.41 style inputs; whole small values refuse to guess.
    static void Test_Math_Units_NeverGuessAmbiguousScale()
    {
        Eq(PercentScale.Fraction, MitigationMath.PercentScaleOf(0.41f));
        Near(41f, MitigationMath.AsPercent(0.41f, PercentScale.Fraction), "0.41 is 41%");
        Eq(PercentScale.Percent, MitigationMath.PercentScaleOf(41f));
        Near(41f, MitigationMath.AsPercent(41f, PercentScale.Percent), "41 is 41%");
        Eq(PercentScale.Percent, MitigationMath.PercentScaleOf(60f));
        Eq(PercentScale.Fraction, MitigationMath.PercentScaleOf(0.6f));
        Eq(PercentScale.Unknown, MitigationMath.PercentScaleOf(2f), "2 could be 2 or 200%");
        Eq(PercentScale.Unknown, MitigationMath.PercentScaleOf(1f), "1 could be 1 or 100%");
        Eq(PercentScale.Unknown, MitigationMath.PercentScaleOf(3f), "boundary stays unknown");
        True(float.IsNaN(MitigationMath.AsPercent(2f, PercentScale.Unknown)), "refuse to convert");
        Eq(PercentScale.Unknown, MitigationMath.PercentScaleOf(float.NaN), "NaN unknown");
    }

    // F6: NaN inputs never produce a number, and previews refuse.
    static void Test_Math_MissingFields_AlwaysRefuse()
    {
        True(float.IsNaN(MitigationMath.EffectiveResistance(float.NaN, 0, 0)), "NaN res");
        True(float.IsNaN(MitigationMath.ResistanceTakenFraction(float.NaN)), "NaN taken");
        True(float.IsNaN(MitigationMath.BlockMitigationFraction(float.NaN, 100)), "NaN block");
        True(float.IsNaN(MitigationMath.DodgeChanceFraction(float.NaN, 100)), "NaN dodge");
        True(float.IsNaN(MitigationMath.WardDecayPerSecond(float.NaN, 0)), "NaN ward");
        True(float.IsNaN(MitigationMath.EnduranceApplied(float.NaN, 100, 50, 20)), "NaN endurance");
        True(float.IsNaN(MitigationMath.CombineRatios(0.5f, float.NaN)), "one NaN layer poisons the product");
        True(float.IsNaN(MitigationMath.PreviewEndurance(float.NaN, 0, 400, 20, 60, DamageMeaning.PostMitigationWithWard).Ratio), "endurance refuses");
        True(float.IsNaN(MitigationMath.PreviewPoolIncrease(float.NaN, float.NaN, 100, DamageMeaning.PostMitigationWithWard).Ratio), "pool refuses");
        True(float.IsNaN(MitigationMath.PreviewLessDamageTaken(100, 10, float.NaN, DamageMeaning.PostMitigationWithWard, "layer").Ratio), "less layer refuses");
        var cf = MitigationMath.PreviewResistance(float.NaN, float.NaN, float.NaN, 75, 0, float.NaN, DamageMeaning.PostMitigationWithWard);
        True(float.IsNaN(cf.Ratio), "no area level: resistance refuses");
        True(cf.Sentence().Contains("area level unknown"), cf.Sentence());
    }

    // F7: an overkill bigger than the damage is a misread, not a survival claim.
    static void Test_Math_Overkill_OutOfRange()
    {
        True(float.IsNaN(MitigationMath.RemainingEffectiveHealth(1000, 1200)), "overkill > damage");
        True(float.IsNaN(MitigationMath.RemainingEffectiveHealth(1000, -1)), "negative overkill");
        True(float.IsNaN(MitigationMath.SurvivalBudgetRatio(1000, 1200)), "no budget");
        var cf = MitigationMath.PreviewResistance(1000, 1200, 41, 75, 0, 100, DamageMeaning.PostMitigationWithWard);
        Eq(true, cf.Survived == null, "survival not judged");
        True(cf.Note.Contains("out of range"), cf.Note);
    }

    // F8: no recorded damage number, so wording is ratio-only.
    static void Test_Math_MissingDamage_RatioOnlyWording()
    {
        var cf = MitigationMath.PreviewResistance(float.NaN, float.NaN, 41, 75, 0, 100, DamageMeaning.PostMitigationWithWard);
        Near(0.7463f, cf.Ratio, "ratio still computable");
        True(float.IsNaN(cf.NewDamage), "no new hit number");
        True(cf.Sentence().Contains("would have reduced this hit by about 25%"), cf.Sentence());
    }
}
