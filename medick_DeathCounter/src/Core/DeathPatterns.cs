using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Across a character's recent deaths: who keeps killing you, with what,
    // and which defences would have helped most often. The Patterns view of
    // the panel. Pure logic, unit-tested.
    public sealed class PatternReport
    {
        public int Deaths;
        public List<(string Name, int Count)> TopKillers = new();
        public List<(Element Element, float Share)> ElementShares = new();   // of all damage in the death windows
        public List<(string Name, int Count)> TopAilments = new();
        public Dictionary<DeathKind, int> Kinds = new();
        public List<Priority> Priorities = new();
    }

    public sealed class Priority
    {
        public Advice Advice;   // the latest wording of this tip
        public int    Deaths;   // how many deaths in the window it was a top tip for
    }

    public static class DeathPatterns
    {
        public const int DefaultWindow = 20;
        public const int MaxPriorities = 3;
        const int TopTipsPerDeath = 3;     // only a death's strongest tips count toward a priority

        // Tips that only make sense for one death, never as a build priority.
        static readonly HashSet<string> PerDeathOnly = new() { "nemesis", "unknown" };

        // Kind-of-death tips that fire on nearly every death of that kind. As a
        // build priority they would win every count and say nothing new, so a
        // specific defence (a resistance, armor, crit avoidance, an ailment
        // counter) outranks them unless they are clearly more common.
        static readonly HashSet<string> Generic = new() { "ehp", "endurance", "avoid", "sustain", "dot_sustain" };
        const float GenericDiscount = 0.6f;

        public static PatternReport Build(IReadOnlyList<DeathRecord> deaths, int window = DefaultWindow)
        {
            var r = new PatternReport();
            var recent = (deaths ?? new List<DeathRecord>()).Where(d => d != null).TakeLast(window).ToList();
            r.Deaths = recent.Count;
            if (recent.Count == 0) return r;

            r.TopKillers = recent
                .Where(d => !string.IsNullOrWhiteSpace(d.Killer))
                .GroupBy(d => d.Killer)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2).ThenBy(x => x.Key)
                .Take(3).ToList();

            var totals = new float[Elements.Count];
            foreach (var d in recent)
                if (d.DamageByElement?.Length == Elements.Count)
                    for (int i = 0; i < Elements.Count; i++) totals[i] += d.DamageByElement[i];
            float sum = totals.Sum();
            if (sum > 0f)
                r.ElementShares = Enumerable.Range(0, Elements.Count)
                    .Where(i => totals[i] > 0f)
                    .Select(i => ((Element)i, totals[i] / sum))
                    .OrderByDescending(x => x.Item2).ToList();

            r.TopAilments = recent
                .SelectMany(d => (d.AilmentsOnYou ?? new List<string>()).Distinct())
                .GroupBy(a => a)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2).ThenBy(x => x.Key)
                .Take(4).ToList();

            foreach (var d in recent)
                r.Kinds[d.Kind] = r.Kinds.TryGetValue(d.Kind, out var n) ? n + 1 : 1;

            // Each death votes for its strongest tips; a priority's score is how
            // many deaths it would have helped (generic tips discounted), then
            // total weight as tiebreak. Deaths shown is the undiscounted count.
            var votes = new Dictionary<string, (Advice latest, int deaths, float weight)>();
            foreach (var d in recent)
                foreach (var t in Advisor.Suggest(d).Where(t => !PerDeathOnly.Contains(t.Key)).Take(TopTipsPerDeath))
                {
                    votes.TryGetValue(t.Key, out var v);
                    votes[t.Key] = (t, v.deaths + 1, v.weight + t.Weight);
                }
            r.Priorities = votes.Values
                .OrderByDescending(v => v.deaths * (Generic.Contains(v.latest.Key) ? GenericDiscount : 1f))
                .ThenByDescending(v => v.weight)
                .Take(MaxPriorities)
                .Select(v => new Priority { Advice = v.latest, Deaths = v.deaths })
                .ToList();
            return r;
        }
    }
}
