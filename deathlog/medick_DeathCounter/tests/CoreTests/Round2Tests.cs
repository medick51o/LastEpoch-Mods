using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_R2_EnduranceClaimsSurvivalOnlyWhenWardIsZero()
    {
        var unknown = MitigationMath.PreviewEndurance(500, 100, 2000, 20, 60, DamageMeaning.PostMitigationWithWard);
        Eq(null, unknown.Survived);
        True(unknown.Sentence().Contains("reduced this hit") && unknown.Sentence().Contains("may or may not"), unknown.Sentence());
        True(!unknown.Sentence().Contains("would likely have survived"), unknown.Sentence());

        var wardUp = MitigationMath.PreviewEndurance(500, 100, 2000, 20, 60, DamageMeaning.PostMitigationWithWard, null, 350);
        Eq(null, wardUp.Survived);

        var none = MitigationMath.PreviewEndurance(500, 100, 2000, 20, 60, DamageMeaning.PostMitigationWithWard, null, 0);
        Eq(true, none.Survived);
        True(none.Sentence().Contains("would likely have survived"), none.Sentence());

        var still = MitigationMath.PreviewEndurance(5000, 3000, 400, 20, 60, DamageMeaning.PostMitigationWithWard, null, float.NaN);
        Eq(false, still.Survived);
        True(still.Sentence().Contains("probably still have killed you"), still.Sentence());

        var saved = AdviceDeath();
        saved.Kind = DeathKind.OneShot;
        saved.KillingBlow = 500;
        saved.OverkillDamage = 100;
        saved.Defenses["Endurance"] = 20;
        saved.Defenses["EnduranceThreshold"] = 2000;
        saved.Defenses["Ward"] = 0;
        var claimed = Advisor.Suggest(saved, max: 100).Single(t => t.Key == "endurance");
        True(claimed.Body.Contains("would likely have survived"), claimed.Body);
    }

    static void Test_R2_EmptyRebuildDoesNotReplaceAFilledCapture()
    {
        var keep = CaptureRefresh.Choose(fresh: false, liveHas: false, frozenHas: false, existingHas: true);
        True(keep.KeepExisting && !keep.UseFrozen, "filled capture stays");
        var frozen = CaptureRefresh.Choose(fresh: false, liveHas: false, frozenHas: true, existingHas: true);
        True(!frozen.KeepExisting && frozen.UseFrozen, "frozen copy is used when live buffers are empty");
        var live = CaptureRefresh.Choose(fresh: false, liveHas: true, frozenHas: true, existingHas: true);
        True(!live.KeepExisting && !live.UseFrozen, "a live list replaces the capture");
        var emptyFresh = CaptureRefresh.Choose(fresh: true, liveHas: false, frozenHas: false, existingHas: false);
        True(!emptyFresh.KeepExisting && !emptyFresh.UseFrozen, "the first signal may build an empty capture");
        var freshOverEmpty = CaptureRefresh.Choose(fresh: true, liveHas: false, frozenHas: false, existingHas: true);
        True(!freshOverEmpty.KeepExisting, "a brand new death does not keep the previous capture");
    }

    static void Test_R2_CounterPressOwnsOnlyTheButtonThatWentDown()
    {
        var latch = new CounterPressLatch();
        True(!latch.Update(true, false, false, false, true, false, false), "a button already held on entry is not a press");
        True(latch.Update(true, true, false, false, true, false, false), "the button that went down is owned");
        True(latch.Update(true, false, false, false, true, true, false), "a second button that was already held does not join");
        True(!latch.Update(true, false, false, false, false, true, false), "releasing the owned button clears it");
        True(latch.Update(true, false, true, false, false, true, false), "the other button can be owned on its own down");
        True(!latch.Update(false, false, false, false, false, true, false), "leaving the counter clears the latch");
        True(!latch.Update(true, false, false, false, false, true, false), "coming back while the button is still held does not re-own it");
        latch.Clear();
        True(!latch.Update(true, false, false, false, false, true, false), "clear drops ownership");
    }

    static void Test_R2_CancelAfterSpentCreditReturnsTheStep()
    {
        var ledger = new DeathCountLedger();
        Eq(0, ledger.Observe(4, 0));
        ledger.Recorded(1);
        Eq(0, ledger.Observe(5, 0.4), "the credit is spent inside the commit window");
        True(ledger.RetractLatest(), "a spent credit steps the baseline back");
        Eq(1, ledger.Observe(5, 1), "the same counter step is seen again");
    }

    static void Test_R2_WardOnlyHitUsesFallbackWhenWardIsUnreadable()
    {
        Eq(40f, HitLoss.Amount(100, 100, float.NaN, float.NaN, 40));
        Eq(null, HitLoss.Amount(100, 100, 50, 50, 40));
        Eq(50f, HitLoss.Amount(100, 50, float.NaN, float.NaN, 40));
        Eq(30f, HitLoss.Amount(float.NaN, float.NaN, 80, 50, 40));
    }

    static void Test_R2_AberrothAilmentTextAndAdvice()
    {
        var shock = Ailments.ByName("Shock of Aberroth");
        True(shock.Effect.Contains("5% increased damage taken per stack"), shock.Effect);
        True(!shock.Effect.Contains("5% more"), shock.Effect);
        var frailty = Ailments.ByName("Frailty of Aberroth");
        True(frailty.Effect.Contains("5% less damage per stack"), frailty.Effect);
        True(frailty.Effect.Contains("10% less health leech, ward retention, and health regen per stack"), frailty.Effect);
        True(frailty.Effect.Contains("50% reduced healing effectiveness per stack"), frailty.Effect);
        True(frailty.Effect.Contains("Unlimited stacks") && frailty.Effect.Contains("4 seconds"), frailty.Effect);
        True(frailty.Effect.Contains("Void Beams") && frailty.Effect.Contains("Quicksand"), frailty.Effect);
        Eq("Shock of Aberroth: " + shock.Effect, Ailments.CardLine("Shock of Aberroth"));
        Eq("Frailty of Aberroth: " + frailty.Effect, Ailments.CardLine("Frailty of Aberroth"));

        var chilled = AdviceDeath("Void", 40);
        chilled.AilmentsOnYou = new List<string> { "Chill of Aberroth" };
        var chillTip = Advisor.Suggest(chilled, max: 100).Single(t => t.Key == "cc_chill_aberroth");
        True(chillTip.Body.Contains("Chill of Aberroth") && chillTip.Body.Contains("not Chill"), chillTip.Body);
        Eq("control:aberroth-chill", chillTip.Group);

        var shocked = AdviceDeath("Void", 40);
        shocked.AilmentsOnYou = new List<string> { "Shock of Aberroth" };
        var shockTip = Advisor.Suggest(shocked, max: 100).Single(t => t.Key == "cc_shock_aberroth");
        True(shockTip.Body.Contains("5% increased damage taken per stack"), shockTip.Body);
        True(!shockTip.Body.Contains("Shock lowered lightning"), shockTip.Body);
        Eq("control:aberroth-shock", shockTip.Group);

        var plain = AdviceDeath("Void", 40);
        plain.AilmentsOnYou = new List<string> { "Chill" };
        True(Advisor.Suggest(plain, max: 100).Any(t => t.Key == "cc_chill"), "plain Chill still has its own card");
        True(!Advisor.Suggest(plain, max: 100).Any(t => t.Key == "cc_chill_aberroth"), "plain Chill is not the Aberroth ailment");
    }

    static void Test_R2_TornResetsKeepTheLastGoodBaselines()
    {
        string dir = Path.Combine(Path.GetTempPath(), "deathlog-r2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string path = Path.Combine(dir, "counter-resets.json");
            File.WriteAllText(path, "{\"Hero\":12,\"Alt\":");
            var torn = new CounterResets(path);
            Eq(CounterResets.Unavailable, torn.Label("Hero", 15));
            True(torn.Broken && !torn.HasBaselines, "no baselines yet");
            byte[] tornBytes = File.ReadAllBytes(path);
            True(!torn.Reset("Hero", 15), "reset refused while torn");
            True(tornBytes.SequenceEqual(File.ReadAllBytes(path)), "torn bytes stay");

            File.WriteAllText(path, "{\"Hero\":12}");
            var good = new CounterResets(path);
            Eq("3", good.Label("Hero", 15));
            File.WriteAllText(path, "{\"Hero\":");
            True(!good.Refresh(), "a later torn read fails");
            True(good.Broken && good.HasBaselines, "the last baselines stay");
            Eq("3", good.Label("Hero", 15));
            byte[] again = File.ReadAllBytes(path);
            True(!good.Reset("Newbie", 1), "reset still refused");
            True(again.SequenceEqual(File.ReadAllBytes(path)), "file not rewritten");

            File.WriteAllText(path, "{\"Hero\":12,\"Alt\":4}");
            True(good.Refresh(), "a later good read applies");
            True(!good.Broken, "broken clears");
            Eq("3", good.Label("Hero", 15));
            Eq("11", good.Label("Alt", 15));
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }

    static void Test_R2_DeathKindUsesHealthLossOrHealthPlusWard()
    {
        var health = DeathAnalyzer.Analyze(new List<HitEvent>
        {
            new() { Time = 10, Amount = 900, HealthLost = 100, WardLost = 800, WardAtHit = 800, MaxHealth = 1000, Source = "Boss" }
        }, 10, new DeathContext { Character = "Hero", MaxHealth = 1000 });
        Eq(DeathKind.Attrition, health.Kind);
        Eq(900f, health.WindowDamage);
        Eq(1000f, health.ClassPool);
        Eq(100f, health.ClassLoss);
        True(health.WardDominatedLine() != null && health.WardDominatedLine().Contains("health loss"), health.WardDominatedLine());

        var buffered = DeathAnalyzer.Analyze(new List<HitEvent>
        {
            new() { Time = 10, Amount = 900, WardAtHit = 2000, MaxHealth = 1000, Source = "Boss" }
        }, 10, new DeathContext { Character = "Hero", MaxHealth = 1000 });
        Eq(DeathKind.Attrition, buffered.Kind);
        Eq(3000f, buffered.ClassPool);
        Eq(900f, buffered.WindowDamage);

        var heavy = DeathAnalyzer.Analyze(new List<HitEvent>
        {
            new() { Time = 10, Amount = 2000, WardAtHit = 500, MaxHealth = 1000, Source = "Boss" }
        }, 10, new DeathContext { Character = "Hero", MaxHealth = 1000 });
        Eq(DeathKind.OneShot, heavy.Kind);
        Eq(1500f, heavy.ClassPool);
        Eq(2000f, heavy.WindowDamage);

        var wardy = AdviceDeath("Fire", 75);
        wardy.Kind = DeathKind.OneShot;
        wardy.Hits = 1;
        wardy.MaxHealth = 1000;
        wardy.WindowDamage = 900;
        wardy.ClassPool = 2000;
        wardy.ClassLoss = 100;
        wardy.DetailSource = null;
        wardy.KillingBlow = 100;
        wardy.OverkillDamage = 0;
        True(!Advisor.Suggest(wardy, max: 100).Any(t => t.Key == "ehp"), "ward-sized timeline totals do not demand a bigger pool");

        var old = AdviceDeath("Fire", 75);
        old.Kind = DeathKind.OneShot;
        old.Hits = 1;
        old.MaxHealth = 1000;
        old.WindowDamage = 900;
        old.ClassPool = 0;
        old.DetailSource = null;
        old.KillingBlow = 100;
        old.OverkillDamage = 0;
        var pool = Advisor.Suggest(old, max: 100).Single(t => t.Key == "ehp");
        True(pool.Body.Contains("900") && pool.Body.Contains("1,000"), pool.Body);

        var split = AdviceDeath("Fire", 75);
        split.Kind = DeathKind.OneShot;
        split.Hits = 1;
        split.MaxHealth = 1000;
        split.WindowDamage = 1800;
        split.ClassPool = 1000;
        split.ClassLoss = 900;
        split.DetailSource = null;
        split.KillingBlow = 100;
        split.OverkillDamage = 0;
        var compared = Advisor.Suggest(split, max: 100).Single(t => t.Key == "ehp");
        True(compared.Body.Contains("900") && compared.Body.Contains("1,000") && compared.Body.Contains("Timeline totals still include ward"), compared.Body);
    }

    static void Test_R2_BossAttackPagesPutTheKillingMoveFirst()
    {
        var ids = new List<string> { "a", "b", "c", "d", "e", "kill" };
        var ordered = BossAttackPages.Order(ids, "kill");
        Eq("kill", ordered[0]);
        Eq(6, ordered.Count);
        Eq("kill,a,b,c", string.Join(",", BossAttackPages.Page(ordered, 0)));
        Eq("d,e", string.Join(",", BossAttackPages.Page(ordered, 1)));
        Eq(1, BossAttackPages.Clamp(9, ordered.Count));
        Eq(0, BossAttackPages.Clamp(-3, ordered.Count));
        Eq("a", BossAttackPages.Order(ids, "missing")[0]);

        var heights = new HeightCache();
        True(!heights.TryGet("pride:0:400:1.00:kill", out _), "empty cache");
        heights.Store("pride:0:400:1.00:kill", 180f);
        heights.Store("pride:0:400:1.00:kill", 0f);
        True(heights.TryGet("pride:0:400:1.00:kill", out float h) && h == 180f, "a later zero does not wipe the height");
        Eq(1, heights.Count);
    }
}
