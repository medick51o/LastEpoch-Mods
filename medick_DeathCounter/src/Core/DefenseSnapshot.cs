using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Raw defensive values read from the game → what the death record states
    // as fact. The game's unit for percent stats (75 or 0.75) is unknown until
    // the first in-game log, so the scale is decided from evidence and is
    // NEVER guessed (review 2026-10-01):
    //   percent evidence   some percent value with |v| > 3
    //   fraction evidence  some non-whole percent value with |v| <= 1.5
    //   whole |v| <= 1     no signal either way (1% or 100%?)
    // Exactly one kind of evidence picks the scale; none or both = unknown,
    // and then no percent stat is reported at all (flat stats still are).
    public static class DefenseSnapshot
    {
        public const float ResCap = 75f;

        static readonly string[] PercentKeys = { "Block", "Endurance", "CritAvoidance", "StunAvoidance" };

        static bool IsPercentKey(string k) => k.StartsWith("Res.") || PercentKeys.Contains(k);

        public static float? Scale(IReadOnlyDictionary<string, float> raw)
        {
            var pct = raw.Where(kv => IsPercentKey(kv.Key) && float.IsFinite(kv.Value)).Select(kv => kv.Value).ToList();
            bool percent  = pct.Any(v => Math.Abs(v) > 3f);
            bool fraction = pct.Any(v => Math.Abs(v) <= 1.5f && v != MathF.Round(v));
            if (percent == fraction) return null;   // neither, or conflicting
            return percent ? 1f : 100f;
        }

        public static Dictionary<string, float> Normalize(IReadOnlyDictionary<string, float> raw)
        {
            var d = new Dictionary<string, float>();
            if (raw == null) return d;
            float? scale = Scale(raw);
            foreach (var (k, v) in raw)
            {
                if (!float.IsFinite(v)) continue;
                if (!IsPercentKey(k)) { d[k] = v; continue; }
                if (scale is not float sc) continue;
                float x = v * sc;
                if (k.StartsWith("Res."))
                {
                    d[k] = Math.Min(x, ResCap);
                    d["ResUncapped." + k.Substring(4)] = x;   // headroom above cap matters against shred
                }
                else if (x <= 100.5f) d[k] = x;              // over 100% block/crit avoidance is a misread
            }
            return d;
        }
    }
}
