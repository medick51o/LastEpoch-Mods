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

    // Review 2026-10-02: stun avoidance is a rating. A value of 750 must not
    // fight a fractional resistance and wipe every percent stat.
    static void Test_Defenses_StunAvoidanceIsRatingNotResistance()
    {
        var d = DefenseSnapshot.Normalize(Raw(("Res.Fire", 0.75f), ("CritAvoidance", 0.4f), ("StunAvoidance", 750f)));
        True(Math.Abs(d["Res.Fire"] - 75f) < 0.01f, "fire scaled from a fraction");
        True(Math.Abs(d["CritAvoidance"] - 40f) < 0.01f, "crit avoidance still a percent");
        Eq(750f, d["StunAvoidance"], "stun avoidance kept as a rating");
    }

    static void Test_AilmentFind_RejectsStatCompounds()
    {
        Eq(null, Ailments.Find("Shockwave")?.Name);
        Eq(null, Ailments.Find("ShockWave")?.Name);
        Eq(null, Ailments.Find("FreezeRate")?.Name);
        Eq(null, Ailments.Find("FreezeRateMultiplier")?.Name);
        Eq(null, Ailments.Find("SlowRetaliation")?.Name);
        Eq("Shock", Ailments.Find("Shock")?.Name);
        Eq("Freeze", Ailments.Find("Frozen")?.Name);
    }

    static void Test_Advice_UnknownMaxHealthDoesNotClaimFullLife()
    {
        var hits = new List<HitEvent> { new() { Time = 10, Amount = 500, Source = "Boss" } };
        var r = DeathAnalyzer.Analyze(hits, 10, new DeathContext());
        var body = Advisor.Suggest(r).Single(t => t.Key == "ehp").Body;
        True(!body.Contains("100%"), body);
        True(!body.Contains("you out"), body);
        True(body.Contains("max health"), body);
    }

    static void Test_Advice_BurstWithoutMaxHealthDoesNotClaimFullLife()
    {
        var hits = new List<HitEvent>
        {
            new() { Time = 9.2, Amount = 200, Source = "A" },
            new() { Time = 10, Amount = 200, Source = "A" },
        };
        var r = DeathAnalyzer.Analyze(hits, 10, new DeathContext());
        Eq(DeathKind.Burst, r.Kind);
        var body = Advisor.Suggest(r).Single(t => t.Key == "avoid").Body;
        True(!body.Contains("100%"), body);
        True(body.Contains("max health"), body);
    }

    static void Test_Advice_PhysicalHitQuotesResistanceAndEndurance()
    {
        var r = DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 800, "Brute", Element.Physical) }, 10, Ctx());
        r.Defenses = new() { ["Res.Physical"] = 40f, ["Endurance"] = 20f, ["EnduranceThreshold"] = 400f, ["Armor"] = 1800f };
        var tips = Advisor.Suggest(r);
        True(tips.Single(t => t.Key == "res_Physical").Title.Contains("40%"), "physical res quoted");
        var end = tips.Single(t => t.Key == "endurance").Body;
        True(end.Contains("20%"), end);
        True(end.Contains("400"), end);
        True(!end.Contains("belt"), end);
        True(tips.Single(t => t.Key == "armor").Body.Contains("1,800"), "armor rating quoted, not a percent");
    }

    static void Test_Advice_FreezeDoesNotSayCapCold()
    {
        var r = DeathAnalyzer.Analyze(new List<HitEvent> { Hit(10, 400, "Wraith", Element.Cold) }, 10, Ctx(), new[] { "Freeze" });
        var body = Advisor.Suggest(r).Single(t => t.Key == "cc_freeze").Body;
        True(!body.Contains("Keep cold resistance capped"), body);
        True(body.Contains("does not stop freeze"), body);
        True(!Advisor.PatternBody(new Advice { Key = "cc_freeze" }, 2, 4).Contains("keep cold resistance capped"), "pattern freeze");
    }

    static void Test_Advice_ShowSkipsCappedAndStopsAtThree()
    {
        var shown = Advisor.Show(FireOneShot(new() { ["Res.Fire"] = 75f }));
        True(shown.Count <= Advisor.MaxShown, "at most three");
        True(shown.All(t => !t.Title.Contains("already capped")), "capped notice is not a recommendation");
        True(shown.Any(t => t.Key == "crit"), "a real action remains");
    }

    static void Test_Advice_ShowFallbackWhenOnlyCapped()
    {
        var d = new DeathRecord { Kind = DeathKind.Reported, KillingElement = "Fire", KillingBlow = 900, DetailSource = "game death report" };
        d.Defenses = new() { ["Res.Fire"] = 75f };
        var shown = Advisor.Show(d);
        Eq(1, shown.Count);
        Eq("ehp", shown[0].Key);
        True(shown[0].Body.Contains("75%"), shown[0].Body);
        True(!shown[0].Title.Contains("already capped"), shown[0].Title);
    }

    static void Test_Quote_UsesOnlyRecordedFields()
    {
        var d = new DeathRecord
        {
            Killer = "Rahyeh", KillerAbility = "Lightning Bolt", KillingElement = "Lightning",
            KillingBlow = 1240f, KillingCrit = true,
        };
        Eq("Killed by Lightning Bolt from Rahyeh, 1,240 lightning damage, crit.", Advisor.Quote(d));
        d.SecondaryKillingElement = "Physical";
        True(!Advisor.Quote(d).Contains("620"), "a mixed hit is not split in half");
        True(Advisor.Quote(d).Contains("lightning and physical"), Advisor.Quote(d));
        Eq("The killing blow was not recorded.", Advisor.Quote(new DeathRecord()));
        Eq("The killing blow was not recorded.", Advisor.Quote(null));
    }

    static void Test_Confidence_NamesWhatIsMissing()
    {
        Eq("Low: no hit timeline, max health unknown", Advisor.Confidence(new DeathRecord()));
        Eq("High: game death report, hit timeline, and max health",
            Advisor.Confidence(new DeathRecord { Hits = 4, MaxHealth = 1000, DetailSource = "game death report" }));
        Eq("Medium: hit timeline and max health, no game death report",
            Advisor.Confidence(new DeathRecord { Hits = 2, MaxHealth = 1000 }));
        Eq("Medium: game death report, no hit timeline, max health unknown",
            Advisor.Confidence(new DeathRecord { Kind = DeathKind.Reported, DetailSource = "game death report", KillingBlow = 500 }));
    }

    static void Test_DeathText_KeepsShortHexAndCyan()
    {
        var runs = DeathText.Parse("Hit for <color=#f00>12</color> <color=cyan>cold</color>");
        Eq("#ff0000", runs[1].Color);
        Eq("12", runs[1].Text);
        Eq("#13A8C9", runs[3].Color);
        Eq("cold", DeathText.Plain("<color=cyan>cold</color>"));
    }
}
