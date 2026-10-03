using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Who the character was and where, at the moment of death.
    public sealed class DeathContext
    {
        public string Character      = "Unknown Hero";
        public string CharacterClass = "";
        public int    Level;
        public string Zone           = "";
        public string RawSceneId;
        public int? ZoneLevel;
        public bool   Hardcore;
        public PlayContext PlayContext;
        public string GameDeathInfo  = "";
        public float  MaxHealth      = -1f;
        public string Detection      = "health";
        public string ModVersion     = "";
        public DateTime UtcNow       = DateTime.UtcNow;
    }

    // Turns the last seconds of hits into a DeathRecord. Pure logic: no Unity,
    // no MelonLoader, unit-tested in tests/CoreTests.
    public static class DeathAnalyzer
    {
        public const float WindowSeconds   = 5f;    // "the fight that killed you"
        public const float BurstSeconds    = 2f;
        public const float OneShotFraction = 0.7f;  // one hit took >= 70% of max life
        public const float BurstFraction   = 0.8f;  // >= 80% of max life inside BurstSeconds
        public const float DotFraction     = 0.5f;  // >= half the window damage was DoT

        public static DeathRecord Analyze(
            IReadOnlyList<HitEvent> recentHits,
            double deathTime,
            DeathContext ctx,
            IEnumerable<string> ailmentsSeen = null)
        {
            var rec = new DeathRecord
            {
                UtcTime        = ctx.UtcNow,
                Character      = string.IsNullOrWhiteSpace(ctx.Character) ? "Unknown Hero" : ctx.Character,
                CharacterClass = ctx.CharacterClass ?? "",
                Level          = ctx.Level,
                Zone           = ctx.Zone ?? "",
                RawSceneId     = ctx.RawSceneId,
                ZoneLevel      = ctx.ZoneLevel > 0 ? ctx.ZoneLevel : null,
                Hardcore       = ctx.Hardcore,
                PlayContext    = ctx.PlayContext?.Copy(),
                GameDeathInfo  = ctx.GameDeathInfo ?? "",
                Detection      = ctx.Detection,
                ModVersion     = ctx.ModVersion,
                WindowSeconds  = WindowSeconds,
            };

            var window = (recentHits ?? Array.Empty<HitEvent>())
                .Where(h => h != null && h.Amount > 0f && h.Time >= deathTime - WindowSeconds && h.Time <= deathTime + 0.5)
                .OrderBy(h => h.Time)
                .ToList();

            float maxHp = ctx.MaxHealth;
            if (maxHp <= 0f)
                maxHp = window.Select(h => h.MaxHealth).DefaultIfEmpty(-1f).Max();
            rec.MaxHealth = maxHp > 0f ? maxHp : 0f;

            // Ailments: anything that ticked on you in the window, plus what
            // the ailment tap saw applied (freeze, shock and other setups).
            var ailments = new List<string>();
            foreach (var h in window)
                if (!string.IsNullOrEmpty(h.Ailment) && !ailments.Contains(h.Ailment)) ailments.Add(h.Ailment);
            if (ailmentsSeen != null)
                foreach (var a in ailmentsSeen)
                {
                    var info = Ailments.Find(a);
                    string n = info?.Name ?? a;
                    if (!string.IsNullOrEmpty(n) && !ailments.Contains(n)) ailments.Add(n);
                }
            rec.AilmentsOnYou = ailments;

            if (window.Count == 0)
            {
                rec.Kind   = DeathKind.Unknown;
                rec.Killer = "";
                return rec;
            }

            rec.Hits = window.Count;
            foreach (var h in window)
            {
                rec.WindowDamage += h.Amount;
                h.AddTo(rec.DamageByElement);
                if (h.IsDot)
                {
                    rec.DotDamage += h.Amount;
                    h.AddTo(rec.DotByElement);
                }
            }

            rec.TopSources = window
                .GroupBy(h => string.IsNullOrWhiteSpace(h.Source) ? "Unknown" : h.Source)
                .Select(g => new SourceShare { Name = g.Key, Amount = g.Sum(x => x.Amount), Hits = g.Count() })
                .OrderByDescending(s => s.Amount)
                .Take(3)
                .ToList();

            // Grace-period events may contribute to the window totals, but
            // must not replace the last observed cause at the death time.
            var last = window.LastOrDefault(h => h.Time <= deathTime) ?? window[window.Count - 1];
            rec.KillingBlow    = last.Amount;
            rec.KillingCrit    = last.Crit;
            rec.KillerAbility  = last.Ability ?? "";
            rec.KillingAilment = last.IsDot ? (last.Ailment ?? "") : "";
            rec.KillingElement = last.MainElement() is Element e ? Elements.Name(e) : "";
            rec.Killer         = !string.IsNullOrWhiteSpace(last.Source)
                ? last.Source
                : rec.TopSources.FirstOrDefault(s => s.Name != "Unknown")?.Name ?? "";
            rec.KillingHealthLoss = last.HealthLost;
            rec.KillingWardLoss = last.WardLost;

            rec.Kind = Classify(window, last, maxHp, deathTime, rec);
            return rec;
        }

        // Health loss against max health when every hit in the window recorded it.
        // Otherwise the ward-inclusive amount against max health plus ward at the
        // killing hit, when that ward was read. Timeline totals are unchanged.
        static float ClassAmount(HitEvent h, bool healthBasis) => healthBasis ? Math.Max(0f, h.HealthLost) : h.Amount;
        static float ClassPool(IReadOnlyList<HitEvent> window, HitEvent last, float maxHp, bool healthBasis)
        {
            if (healthBasis) return maxHp;
            float ward = last.WardAtHit >= 0f ? last.WardAtHit : window.Select(h => h.WardAtHit).DefaultIfEmpty(-1f).Max();
            return ward >= 0f ? maxHp + ward : maxHp;
        }

        static DeathKind Classify(List<HitEvent> window, HitEvent last, float maxHp, double deathTime, DeathRecord rec)
        {
            if (rec.WindowDamage > 0f && rec.DotDamage / rec.WindowDamage >= DotFraction)
                return DeathKind.DamageOverTime;

            if (maxHp > 0f)
            {
                bool healthBasis = window.Count > 0 && window.All(h => h.HealthLost >= 0f);
                float pool = ClassPool(window, last, maxHp, healthBasis);
                rec.ClassPool = pool;
                float lastLoss = ClassAmount(last, healthBasis);
                float burst = window.Where(h => h.Time >= deathTime - BurstSeconds).Sum(h => ClassAmount(h, healthBasis));
                rec.ClassLoss = Math.Max(lastLoss, burst);
                if (!last.IsDot && lastLoss >= OneShotFraction * pool)
                    return DeathKind.OneShot;

                if (burst >= BurstFraction * pool)
                    return DeathKind.Burst;

                return DeathKind.Attrition;
            }

            // No max life known: judge by shape alone.
            if (window.Count == 1) return DeathKind.OneShot;
            double span = last.Time - window[0].Time;
            return span <= BurstSeconds ? DeathKind.Burst : DeathKind.Attrition;
        }
    }
}
