using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_R1_ExactHitSentenceStaysOnTheCard()
    {
        var body = Advisor.Suggest(ReportedFireDeath(41, 2000, 300)).Single(t => t.Key == "res_Fire").Body;
        string line = AdviceRules.ExactHitCardLine(body);
        True(line != null && line.StartsWith("For this exact reported hit: ", StringComparison.Ordinal), line ?? "missing");
        True(line.Contains("survived") && line.Contains("including ward"), line);
        Eq(null, AdviceRules.ExactHitCardLine("Closing this gap may help; it does not guarantee surviving the attack."));
        Eq(null, AdviceRules.ExactHitCardLine(null));
    }

    static void Test_R1_FormatterMissingTypeFallsBackToParser()
    {
        var cap = new DeathFormatterCapture();
        cap.Cause(1, "Hero", 1, 10, "Rahyeh", "Dive Bomb", null);
        cap.Amount(1, "Hero", 1, 10, false, 4200, 1800);
        string text = "You have been slain by Dive Bomb from Rahyeh\nKilling Blow Damage Type: Void\nKilling Blow Damage: 9,999\nOverkill Damage: 1";
        var built = cap.Build("Hero", 10, text);
        Eq(null, built.PrimaryElement);
        Eq(4200f, built.Damage);
        Eq("Rahyeh", built.Killer);
        Eq("Dive Bomb", built.Ability);
        DeathFormatterCapture.FillMissingElement(built, text);
        Eq("Void", built.PrimaryElement);
        Eq(4200f, built.Damage);
        Eq("Rahyeh", built.Killer);
        Eq("Dive Bomb", built.Ability);
        built.PrimaryElement = "Fire";
        DeathFormatterCapture.FillMissingElement(built, text);
        Eq("Fire", built.PrimaryElement);
    }

    static void Test_R1_HoverDoesNotBlockGameplay()
    {
        True(!MenuInputPolicy.BlockGameplay(false, false, false, false, true, false), "hover alone");
        True(MenuInputPolicy.BlockGameplay(false, false, false, false, true, true), "held click on the counter");
        True(MenuInputPolicy.BlockGameplay(false, true, false, false, false, false), "open log");
        True(MenuInputPolicy.BlockGameplay(false, false, true, false, false, false), "drag");
        True(!MenuInputPolicy.BlockGameplay(false, false, false, false, false, true), "mouse held off the counter");
        True(MenuInputPolicy.BlockGameplay(true, false, false, false, false, false), "gate already blocked");
        True(MenuInputPolicy.BlockGameplay(false, false, false, true, false, false), "opening key");
    }

    static void Test_R1_EnduranceMissStaysUnderTheToast()
    {
        var miss = AdviceDeath("Void", 0);
        miss.Defenses.Remove("Res.Void");
        miss.Kind = DeathKind.OneShot;
        miss.KillingBlow = 5000;
        miss.OverkillDamage = 3000;
        miss.Defenses["Endurance"] = 20;
        miss.Defenses["EnduranceThreshold"] = 400;
        miss.DefenseSnapshotAgeSeconds = 0.2f;
        var tip = Advisor.Suggest(miss, max: 100).Single(t => t.Key == "endurance");
        True(tip.Weight < Advisor.ToastMinWeight && tip.Weight < 55, "weight " + tip.Weight);
        True(tip.Body.Contains("probably still have killed you"), tip.Body);
        Eq("Check void resistance", Advisor.ToastStep(miss));

        var saved = AdviceDeath();
        saved.Kind = DeathKind.OneShot;
        saved.KillingBlow = 500;
        saved.OverkillDamage = 100;
        saved.Defenses["Endurance"] = 20;
        saved.Defenses["EnduranceThreshold"] = 2000;
        var live = Advisor.Suggest(saved, max: 100).Single(t => t.Key == "endurance");
        True(live.Weight >= 65, "weight " + live.Weight);
        True(live.Body.Contains("would likely have survived") && live.Body.Contains("including ward"), live.Body);

        var plain = AdviceDeath();
        plain.Kind = DeathKind.OneShot;
        plain.Defenses["Endurance"] = 20;
        var bare = Advisor.Suggest(plain, max: 100).Single(t => t.Key == "endurance");
        Eq(65f, bare.Weight);
        True(!bare.Body.Contains("probably still"), bare.Body);
    }

    static void Test_R1_WardJoinsHitSizeAndReportDoesNotReclassify()
    {
        Eq(80f, HitLoss.Amount(100, 100, 200, 120, 0));
        Eq(110f, HitLoss.Amount(100, 50, 80, 20, 0));
        Eq(null, HitLoss.Amount(100, 100, 50, 50, 0));
        Eq(40f, HitLoss.Amount(float.NaN, float.NaN, float.NaN, float.NaN, 40));
        var hits = new List<HitEvent>();
        for (int i = 0; i < 6; i++)
            hits.Add(new HitEvent { Time = i * 0.4, Amount = 80, MaxHealth = 1000 });
        var rec = DeathAnalyzer.Analyze(hits, 2.2, new DeathContext { Character = "Hero", MaxHealth = 1000 });
        Eq(DeathKind.Attrition, rec.Kind);
        new DeathDetails { Damage = 9000, Overkill = 6500, PrimaryElement = "Physical" }.Apply(rec);
        Eq(DeathKind.Attrition, rec.Kind);
        Eq(9000f, rec.KillingBlow);
    }

    static void Test_R1_AberrothAilmentsStaySeparateAndCurseShowsOnTheCard()
    {
        Eq("Curse of Aberroth", Ailments.Find("Curse of Aberroth").Name);
        Eq("Shock of Aberroth", Ailments.Find("Shock of Aberroth").Name);
        Eq("Frailty of Aberroth", Ailments.Find("Frailty of Aberroth").Name);
        Eq("Chill of Aberroth", Ailments.Find("Chill of Aberroth").Name);
        Eq("Shock", Ailments.Find("Shock").Name);
        Eq("Frailty", Ailments.Find("Frailty").Name);
        Eq("Chill", Ailments.Find("Chill").Name);
        var cursed = AdviceDeath("Fire", 75);
        cursed.AilmentsOnYou = new List<string> { "Curse of Aberroth" };
        var card = Advisor.Suggest(cursed, max: 100).Single(t => t.Key == "res_Fire");
        True(card.Body.Contains("cannot be cleansed") && card.Body.Contains("10 points"), card.Body);
        True(!card.Title.Contains("already capped") && !card.Title.Contains("currently capped"), card.Title);
        var shock = AdviceDeath("Void", 40);
        shock.AilmentsOnYou = new List<string> { "Shock of Aberroth" };
        True(!Advisor.Suggest(shock, max: 100).Any(t => t.Body.Contains("Shock lowered lightning")), "Shock of Aberroth is not lightning shred");
    }

    static void Test_R1_TornCounterResetsAreNotOverwritten()
    {
        string dir = Path.Combine(Path.GetTempPath(), "deathlog-r1-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "counter-resets.json");
            File.WriteAllText(path, "{\"Hero\":12,\"Alt\":");
            var resets = new CounterResets(path);
            True(resets.Broken, "torn file");
            byte[] before = File.ReadAllBytes(path);
            True(!resets.Reset("Newbie", 1), "reset refused");
            True(before.SequenceEqual(File.ReadAllBytes(path)), "file bytes unchanged");
            File.WriteAllText(path, "{\"Hero\":12,\"Alt\":3}");
            True(resets.Reset("Newbie", 4), "later read allows a reset");
            True(!resets.Broken, "broken clears after a good read");
            string saved = File.ReadAllText(path);
            True(saved.Contains("Hero") && saved.Contains("Alt") && saved.Contains("Newbie"), saved);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    static void Test_R1_AliveCommitDropsTheCredit()
    {
        var ledger = new DeathCountLedger();
        Eq(0, ledger.Observe(4, 0));
        ledger.Recorded(1);
        True(ledger.RetractLatest(), "newest credit removed");
        Eq(1, ledger.Observe(5, 2), "the next counter increase is not swallowed");
        True(DeathCommitGate.CancelBecauseAlive("hook", 12), "live signal while alive");
        True(DeathCommitGate.CancelBecauseAlive("health", 12), "health signal while alive");
        True(!DeathCommitGate.CancelBecauseAlive("game", 12), "late counter death stays");
        True(!DeathCommitGate.CancelBecauseAlive("hook", 0), "still dead");
        True(!DeathCommitGate.CancelBecauseAlive("hook", float.NaN), "unreadable health");
    }

    static void Test_R1_HistoryPagesSixRowsNewestFirst()
    {
        Eq(1, HistoryPages.Count(0));
        Eq(1, HistoryPages.Count(6));
        Eq(2, HistoryPages.Count(7));
        Eq("7,6,5,4,3,2", string.Join(",", HistoryPages.NewestFirst(8, 0)));
        Eq("1,0", string.Join(",", HistoryPages.NewestFirst(8, 1)));
        Eq(1, HistoryPages.Clamp(9, 8));
        Eq(0, HistoryPages.Clamp(-1, 8));
    }

    static void Test_R1_CappedResistanceNamesPenetrationWhenAreaIsKnown()
    {
        var known = AdviceDeath("Fire", 75);
        known.ZoneLevel = 100;
        known.DefenseSnapshotAgeSeconds = 0;
        var notice = Advisor.Suggest(known, max: 100).Single(t => t.Key == "res_Fire");
        True(notice.IsNotice, "still a notice");
        True(notice.Body.Contains("area level 100") && notice.Body.Contains("penetration") && notice.Body.Contains("0%") && notice.Body.Contains("100%"), notice.Body);
        var unknown = AdviceDeath("Fire", 75);
        True(!Advisor.Suggest(unknown, max: 100).Single(t => t.Key == "res_Fire").Body.Contains("penetration"), "no area, no penetration sentence");
    }

    static void Test_R1_BrowseBossNotesOpensTheEncounterList()
    {
        True(BossNotesFocus.ShowBrowser(false), "no matched boss opens the browser");
        True(!BossNotesFocus.ShowBrowser(true), "a matched boss stays on that fight");
    }

    static void Test_R1_AdviceCacheAndNextStepMemoAreBounded()
    {
        var map = new Dictionary<int, int>();
        for (int i = 0; i < 50; i++) BoundedCache.Put(map, i, i, BoundedCache.AdviceCap);
        Eq(BoundedCache.AdviceCap, map.Count);
        True(!map.ContainsKey(0) && !map.ContainsKey(1), "oldest keys evicted");
        True(map.ContainsKey(49), "newest key kept");
        var memo = new ViewMemo();
        True(!memo.Matches(1, 2, 3), "empty memo misses");
        memo.Remember(1, 2, 3);
        True(memo.Matches(1, 2, 3), "same key hits");
        True(!memo.Matches(1, 2, 4) && !memo.Matches(9, 2, 3) && !memo.Matches(1, 8, 3), "any part misses");
    }

    static void Test_R1_PendingSignalsMergeWithoutDroppingTheFirst()
    {
        var hookThenGame = DeathSignalMerge.Combine("hook", "game");
        Eq("hook", hookThenGame.Source);
        True(!hookThenGame.Rebuild, "a counter echo does not rebuild");
        var gameThenHook = DeathSignalMerge.Combine("game", "hook");
        Eq("hook", gameThenHook.Source);
        True(gameThenHook.Rebuild, "a live signal fills a counter-only death");
        var hookThenHealth = DeathSignalMerge.Combine("hook", "health");
        Eq("hook", hookThenHealth.Source);
        True(hookThenHealth.Rebuild, "a second live signal refreshes hits");
        var first = DeathSignalMerge.Combine(null, "health");
        Eq("health", first.Source);
        True(first.Rebuild, "the first signal builds the capture");
    }

    static void Test_R1_DuplicateJsonLineDoesNotBlockTheSave()
    {
        string raw = "{\"Character\":\"Hero\",\"Character\":\"Alt\",\"Number\":1}";
        string merged = DeathLog.TryMerge(raw, "{\"Character\":\"Hero\",\"Number\":1}", "{\"Character\":\"Hero\",\"Number\":2}");
        True(merged != null && merged.Length > 0, "merge returns text");
        True(merged.Contains("Hero") || merged.Contains("Alt"), merged);
        string clean = DeathLog.TryMerge("{\"Character\":\"Hero\",\"Number\":1}", "{\"Character\":\"Hero\",\"Number\":1}", "{\"Character\":\"Hero\",\"Number\":2}");
        True(clean.Contains("Hero") && clean.Contains("2"), clean);
    }

    static void Test_R1_MajasaPhase1PutsPhysicalFirst()
    {
        var boss = BossCatalog.All.Single(p => p.Id == "majasa-phase-1");
        Eq(Element.Physical, BossFieldNotes.PriorityResistances(boss)[0]);
        True(!boss.DamageTypes.Contains(Element.Physical), "physical is not added as a phase 1 hit type");
        string prep = BossFieldNotes.Preparation(boss)[0].ToLowerInvariant();
        True(prep.Contains("physical") && prep.Contains("first"), prep);
        string resists = MaxrollPlayerNotes.Resists(boss.Id);
        True(resists.StartsWith("Maxroll (community guide)", StringComparison.Ordinal), resists);
        int physical = resists.IndexOf("physical", StringComparison.OrdinalIgnoreCase);
        int fire = resists.IndexOf("fire", StringComparison.OrdinalIgnoreCase);
        True(physical >= 0 && fire > physical, resists);
    }

    static void Test_R1_MaxrollLinesAreAttributedShortAndNotCopied()
    {
        Eq(null, MaxrollPlayerNotes.LoadError);
        var moves = BossCatalog.All.SelectMany(b => BossGuideMoves.For(b.Id)).ToList();
        Eq(302, moves.Count);
        Eq(302, MaxrollPlayerNotes.MoveCount);
        int avoids = 0, danger = 0, oneShot = 0;
        foreach (var move in moves)
        {
            True(MaxrollPlayerNotes.TryMove(move.Id, out string spot, out string avoid, out bool flagged, out bool shot), move.Id);
            True(!string.IsNullOrWhiteSpace(spot), move.Id + " spot");
            True(WordCount(spot) <= MaxrollPlayerNotes.LineWordCap, move.Id + " spot length " + WordCount(spot));
            True(!spot.Contains("--") && !spot.Contains("survive") && !spot.Contains("guarantee"), move.Id);
            if (!string.IsNullOrWhiteSpace(avoid))
            {
                avoids++;
                True(WordCount(avoid) <= MaxrollPlayerNotes.LineWordCap, move.Id + " avoid length");
                True(!avoid.Contains("--") && !avoid.Contains("survive") && !avoid.Contains("guarantee"), move.Id);
            }
            if (flagged) danger++;
            if (shot) oneShot++;
            var lines = MaxrollPlayerNotes.MoveLines(move.Id);
            Eq(MaxrollPlayerNotes.Attribution, lines[0]);
            True(lines.Any(l => l.StartsWith("How to spot it: ", StringComparison.Ordinal)), move.Id);
            if (shot) True(lines.Any(l => l.Contains("one-shot")), move.Id);
            else if (flagged) True(lines.Any(l => l.Contains("dangerous")), move.Id);
        }
        True(avoids > 0 && danger > 0 && oneShot > 0, $"avoid {avoids} danger {danger} one-shot {oneShot}");
        Eq(16, MaxrollPlayerNotes.ResistCount);
        foreach (var boss in BossCatalog.All)
        {
            string resists = MaxrollPlayerNotes.Resists(boss.Id);
            if (resists == null) continue;
            True(resists.StartsWith(MaxrollPlayerNotes.Attribution + ": ", StringComparison.Ordinal), boss.Id);
            True(WordCount(resists) <= 40, boss.Id + " resist length");
        }
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "maxroll-merged-normalized.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(fixture));
        var sources = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var encounter in doc.RootElement.GetProperty("normalized").EnumerateObject())
            foreach (var move in encounter.Value.GetProperty("moves").EnumerateArray())
            {
                var fields = new List<string>();
                foreach (string field in new[] { "tell", "avoid", "maxrollQuote", "note" })
                    if (move.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
                        fields.Add(value.GetString());
                sources[move.GetProperty("id").GetString()] = fields;
            }
        foreach (var move in moves)
        {
            MaxrollPlayerNotes.TryMove(move.Id, out string spot, out string avoid, out _, out _);
            foreach (string line in new[] { spot, avoid })
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var spoken = Words(line);
                foreach (string field in sources[move.Id])
                    True(!HasWordRun(spoken, Words(field), 8), move.Id + " copies 8 words from the guide");
            }
        }
        int conflicts = BossCatalog.All.Sum(b => (b.Encounter.Claims ?? new List<ClaimRecord>()).Count(c => c.Id != null && c.Id.Contains(".conflict.")));
        Eq(45, conflicts);
        var card = BossFieldNotes.ForDeath(new DeathRecord { Killer = "Harbinger of Pride", KillerAbility = "Spear Beam", IsBossFight = true }, out _, out _);
        True(card.Any(l => l.StartsWith(MaxrollPlayerNotes.Attribution, StringComparison.Ordinal)), "killing move card is attributed");
        True(card.Any(l => l.StartsWith("How to spot it: ", StringComparison.Ordinal)), "killing move card has a spot line");
    }

    static int WordCount(string text) => Words(text).Count;
    static List<string> Words(string text)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        void Flush()
        {
            if (sb.Length == 0) return;
            list.Add(sb.ToString().ToLowerInvariant());
            sb.Clear();
        }
        foreach (char c in text ?? "")
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else Flush();
        }
        Flush();
        return list;
    }
    static bool HasWordRun(List<string> needle, List<string> hay, int length)
    {
        if (needle.Count < length || hay.Count < length) return false;
        for (int i = 0; i <= needle.Count - length; i++)
            for (int j = 0; j <= hay.Count - length; j++)
            {
                bool same = true;
                for (int k = 0; k < length && same; k++)
                    if (needle[i + k] != hay[j + k]) same = false;
                if (same) return true;
            }
        return false;
    }
}
