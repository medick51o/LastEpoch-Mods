using System.Collections.Generic;

namespace medick_DeathCounter.Core
{
    // One chunk of damage the player took, as the game glue saw it.
    // Everything except Time and Amount is best-effort and may be missing.
    public sealed class HitEvent
    {
        public double  Time;            // seconds on the game clock
        public float   Amount;          // damage actually taken (health lost when known)
        public float[] ByElement;       // length Elements.Count, or null when the split is unknown
        public string  Source;          // attacker display name
        public string  Ability;         // attacker ability, when the game says
        public string  Ailment;         // canonical Ailments name when this was an ailment tick
        public bool    IsDot;
        public bool?   Crit;
        public float   HealthBefore = -1f;
        public float   MaxHealth    = -1f;

        // The element that did most of this hit, falling back to the ailment's
        // own element, then null.
        public Element? MainElement()
        {
            if (ByElement != null && ByElement.Length == Elements.Count)
            {
                int best = -1; float bestV = 0f;
                for (int i = 0; i < Elements.Count; i++)
                    if (ByElement[i] > bestV) { bestV = ByElement[i]; best = i; }
                if (best >= 0) return (Element)best;
            }
            return Ailments.ByName(Ailment)?.Element;
        }

        // Spread Amount across elements: the game's split when we have it
        // (rescaled to Amount, since the split is usually pre-mitigation),
        // else all of it on the ailment's element. Unknown stays unassigned.
        public void AddTo(float[] totals)
        {
            if (Amount <= 0f) return;
            if (ByElement != null && ByElement.Length == Elements.Count)
            {
                float sum = 0f;
                for (int i = 0; i < Elements.Count; i++) sum += System.Math.Max(0f, ByElement[i]);
                if (sum > 0f)
                {
                    for (int i = 0; i < Elements.Count; i++)
                        totals[i] += Amount * System.Math.Max(0f, ByElement[i]) / sum;
                    return;
                }
            }
            var el = Ailments.ByName(Ailment)?.Element;
            if (el.HasValue) totals[(int)el.Value] += Amount;
        }
    }

    // Rolling window of recent hits. Only the last few seconds matter for
    // "what killed me", so this never grows past Capacity or WindowSeconds.
    public sealed class HitBuffer
    {
        public const int Capacity = 256;
        readonly List<HitEvent> _hits = new();
        readonly double _window;

        public HitBuffer(double windowSeconds = 12.0) { _window = windowSeconds; }

        public int Count => _hits.Count;

        public void Add(HitEvent h)
        {
            if (h == null) return;
            _hits.Add(h);
            Prune(h.Time);
            if (_hits.Count > Capacity) _hits.RemoveRange(0, _hits.Count - Capacity);
        }

        public void Prune(double now)
        {
            int drop = 0;
            while (drop < _hits.Count && now - _hits[drop].Time > _window) drop++;
            if (drop > 0) _hits.RemoveRange(0, drop);
        }

        public List<HitEvent> Since(double from)
        {
            var list = new List<HitEvent>();
            foreach (var h in _hits) if (h.Time >= from) list.Add(h);
            return list;
        }

        public void Clear() => _hits.Clear();
    }
}
