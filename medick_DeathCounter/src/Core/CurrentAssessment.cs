using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // A read-only review of today's normalized stats against one historical
    // death. Neither the death record nor the caller's snapshot is modified.
    public sealed class CurrentAssessment
    {
        public bool Available { get; private set; }
        public string Message { get; private set; }
        public List<string> Changes { get; private set; } = new();
        public List<Advice> Actions { get; private set; } = new();
        public ReadinessRating Rating { get; private set; }

        public static CurrentAssessment Build(DeathRecord death, string currentCharacter, IReadOnlyDictionary<string, float> stats, IEnumerable<DeathRecord> history = null)
        {
            var result = new CurrentAssessment();
            if (death == null || string.IsNullOrWhiteSpace(death.Character) || death.Character != currentCharacter?.Trim())
            {
                result.Message = "Load the same character as this death to reassess it.";
                return result;
            }
            var now = ReadableStats(stats);
            // An empty/area-only read cannot support an updated assessment.
            if (!now.Any(kv => kv.Key != "AreaLevel" && kv.Key != "MaxHealth" || kv.Key == "MaxHealth" && kv.Value > 0))
            {
                result.Message = "Current defenses could not be read. Try again after the character has loaded.";
                return result;
            }
            now.Remove("AreaLevel");
            if (AdviceRules.FreshDefense(death, "AreaLevel", out float area) && area > 0) now["AreaLevel"] = area;
            var review = new DeathRecord
            {
                Character = death.Character, UtcTime = death.UtcTime, Number = death.Number,
                Killer = death.Killer, KillerAbility = death.KillerAbility, KillingAilment = death.KillingAilment,
                KillingElement = death.KillingElement, SecondaryKillingElement = death.SecondaryKillingElement,
                KillingCrit = death.KillingCrit, KillingBlow = death.KillingBlow, OverkillDamage = death.OverkillDamage,
                IsBossFight = death.IsBossFight, DetailSource = death.DetailSource, Kind = death.Kind,
                MaxHealth = death.MaxHealth, WindowDamage = death.WindowDamage, DotDamage = death.DotDamage,
                Hits = death.Hits, WindowSeconds = death.WindowSeconds,
                DamageByElement = death.DamageByElement?.ToArray(), DotByElement = death.DotByElement?.ToArray(),
                AilmentsOnYou = death.AilmentsOnYou?.ToList() ?? new(),
                Defenses = now, DefenseSnapshotAgeSeconds = 0,
            };
            var elements = RecordedElements(death);
            void Compare(string key, string label, bool percent = false, float? prior = null)
            {
                bool oldKnown = prior.HasValue && float.IsFinite(prior.Value);
                float before = oldKnown ? prior.Value : 0;
                if (!oldKnown) oldKnown = AdviceRules.FreshDefense(death, key, out before);
                bool currentKnown = now.TryGetValue(key, out float value);
                string F(float v) => v.ToString(percent ? "0.#" : "N0", CultureInfo.InvariantCulture) + (percent ? "%" : "");
                string line = label + ": " + (oldKnown ? F(before) : "unknown at death") + " → " + (currentKnown ? F(value) : "unknown now");
                if (key.StartsWith("Res.") && oldKnown && currentKnown && before != value && now.ContainsKey("AreaLevel"))
                {
                    float ratio = MitigationMath.ResistanceRatio(before, value, 0, MitigationMath.EnemyPenetration(area));
                    if (float.IsFinite(ratio) && ratio != 1)
                        line += $". Models about {100 * Math.Abs(1 - ratio):0}% {(ratio < 1 ? "less" : "more")} {key.Substring(4).ToLowerInvariant()} damage if both resistance readings hold, with other modifiers unchanged.";
                }
                result.Changes.Add(line);
            }
            foreach (var element in elements.OrderBy(e => (int)e)) Compare("Res." + Elements.Name(element), Elements.Name(element) + " resistance", true);
            Compare("MaxHealth", "Max health", prior: death.MaxHealth > 0 ? death.MaxHealth : null);
            // Ward is instantaneous, never treated as a sustained combat value.
            if (now.ContainsKey("Ward") || death.Defenses?.ContainsKey("Ward") == true) Compare("Ward", "Ward (instant reading)");
            if (death.KillingCrit == true)
            {
                Compare("CritAvoidance", "Crit avoidance", true);
                Compare("ReducedBonusCritDamage", "Reduced crit bonus damage", true);
            }
            if (elements.Contains(Element.Physical)) Compare("Armor", "Armor");
            var candidates = AdviceRules.Evaluate(review, history, current: true);
            result.Actions = candidates.Where(t => !t.IsNotice).Take(Advisor.MaxShown).ToList();
            result.Rating = ReadinessRating.Evaluate(death, now, candidates);
            result.Available = true;
            result.Message = "Current stats checked against this death's recorded cause" + (now.ContainsKey("AreaLevel") ? $" at its original area level {area:0}" : "; original area level is unknown")
                + ". Current buffs may expire and the old debuffs may return.";
            return result;
        }

        internal static HashSet<Element> RecordedElements(DeathRecord death)
        {
            var elements = new HashSet<Element>();
            if (Elements.TryParse(death.KillingElement, out var primary)) elements.Add(primary);
            if (Elements.TryParse(death.SecondaryKillingElement, out var secondary)) elements.Add(secondary);
            if ((Ailments.ByName(death.KillingAilment) ?? Ailments.Find(death.KillingAilment))?.Element is Element dot) elements.Add(dot);
            if (death.DamageByElement?.Length == Elements.Count && death.DamageByElement.All(v => float.IsFinite(v) && v >= 0))
            {
                float total = death.DamageByElement.Sum();
                if (float.IsFinite(total) && total > 0)
                    foreach (int i in Enumerable.Range(0, Elements.Count).Where(i => death.DamageByElement[i] / total >= 0.25f)) elements.Add((Element)i);
            }
            return elements;
        }
        internal static Dictionary<string, float> ReadableStats(IReadOnlyDictionary<string, float> stats) =>
            (stats ?? new Dictionary<string, float>()).Where(kv => float.IsFinite(kv.Value)
                && (kv.Key != "MaxHealth" || kv.Value > 0) && (kv.Key != "Ward" || kv.Value >= 0))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
