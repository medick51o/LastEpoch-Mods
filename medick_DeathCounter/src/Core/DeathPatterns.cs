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
        public string KindsLine = "";                                        // "6 burst · 1 one-shot", built once
        public List<Priority> Priorities = new();
    }

    public sealed class Priority
    {
        public Advice Advice;   // Key/Title of the tip; Body reworded for the pattern
        public int    Deaths;   // how many deaths in the window this tip applied to
    }

    public static class DeathPatterns
    {
        public const int DefaultWindow = 20;
        public const int MaxPriorities = 3;

        // Tips that only make sense for one death, never as a build priority.
        static readonly HashSet<string> PerDeathOnly = new() { "nemesis", "repeat_ability", "boss", "unknown", "none" };

        // Kind-of-death tips that fire on nearly every death of that kind. As a
        // build priority they would win every count and say nothing new, so a
        // specific defence (a resistance, armor, crit avoidance, an ailment
        // counter) outranks them unless they are clearly more common.
        static readonly HashSet<string> Generic = new() { "ehp", "endurance", "avoid", "sustain", "dot_sustain", "recovery" };
        const float GenericDiscount = 0.6f;

        public static PatternReport Build(IReadOnlyList<DeathRecord> deaths, int window = DefaultWindow, int maxPriorities = MaxPriorities)
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

            // Each death counts once: one 50k one-shot must not drown ten
            // ordinary deaths (review). Shares are of deaths, by damage type.
            var totals = new float[Elements.Count];
            foreach (var d in recent)
            {
                if (d.DamageByElement?.Length != Elements.Count) continue;
                float dsum = d.DamageByElement.Sum();
                if (dsum <= 0f) continue;
                for (int i = 0; i < Elements.Count; i++) totals[i] += d.DamageByElement[i] / dsum;
            }
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
            r.KindsLine = string.Join("  ·  ", r.Kinds.OrderByDescending(k => k.Value)
                .Select(k => $"{k.Value} {new DeathRecord { Kind = k.Key }.KindLabel().ToLowerInvariant()}"));

            // A tip counts for every death it applied to, not only the deaths
            // where it made the top three (review). Ranking: deaths applied to,
            // generic tips discounted, total weight as tiebreak; the count
            // shown is undiscounted.
            var votes = new Dictionary<string, (Advice latest, int deaths, float weight)>();
            foreach (var d in recent)
                foreach (var candidate in Advisor.Suggest(d, max: int.MaxValue).Where(t => !t.IsNotice && !PerDeathOnly.Contains(t.Key)))
                {
                    var t = candidate;
                    string key = t.Group == "recovery" ? "recovery" : t.Key;
                    if (key == "recovery")
                        t = new Advice { Key = key, Group = key, Title = "Improve recovery during combat", Weight = t.Weight };
                    votes.TryGetValue(key, out var v);
                    votes[key] = (t, v.deaths + 1, v.weight + t.Weight);
                }

            r.Priorities = votes.Values
                .OrderByDescending(v => v.deaths * (Generic.Contains(v.latest.Key) ? GenericDiscount : 1f))
                .ThenByDescending(v => v.weight)
                .Take(maxPriorities)
                .Select(v => new Priority
                {
                    Advice = new Advice { Key = v.latest.Key, Group = v.latest.Group, Title = v.latest.Title, Weight = v.weight,
                                          Body = Advisor.PatternBody(v.latest, v.deaths, r.Deaths) },
                    Deaths = v.deaths,
                })
                .ToList();
            return r;
        }
    }
}
