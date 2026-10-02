using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Grades describe measured defense progress, never calibrated fight odds.
    public sealed class ReadinessRating
    {
        public string Grade { get; set; }
        public string Verdict { get; set; }
        public string Reason { get; set; }
        public string Scope { get; set; } = "Defense progress grade, not a whole-fight survival probability.";

        internal static ReadinessRating Evaluate(DeathRecord death, Dictionary<string, float> now, List<Advice> candidates)
        {
            var types = CurrentAssessment.RecordedElements(death);
            bool Have(string key, out float v) => now.TryGetValue(key, out v) && float.IsFinite(v);
            bool missing = types.Count == 0 || types.Any(e => !Have("Res." + Elements.Name(e), out _)) || !Have("MaxHealth", out float hp) || hp <= 0;
            bool crit = death.KillingCrit == true;
            bool haveAvoid = Have("CritAvoidance", out float avoid), haveBonus = Have("ReducedBonusCritDamage", out float bonus);
            if (crit && !haveAvoid && !haveBonus) missing = true;
            var gaps = types.Where(e => Have("Res." + Elements.Name(e), out float r) && r < 74.5f).ToList();
            bool critGap = crit && (haveAvoid || haveBonus) && candidates.Any(t => t.Group == "crit" && !t.IsNotice);
            bool improved = false, worsened = death.MaxHealth > 0 && Have("MaxHealth", out float currentHp) && currentHp < death.MaxHealth;
            if (death.MaxHealth > 0 && Have("MaxHealth", out currentHp) && currentHp > death.MaxHealth) improved = true;
            foreach (var e in types)
                if (Have("Res." + Elements.Name(e), out float r) && AdviceRules.FreshDefense(death, "Res." + Elements.Name(e), out float old))
                {
                    improved |= r > old;
                    worsened |= r < old;
                }
            foreach (string key in crit ? new[] { "CritAvoidance", "ReducedBonusCritDamage" } : Array.Empty<string>())
                if (Have(key, out float v) && AdviceRules.FreshDefense(death, key, out float old)) { improved |= v > old; worsened |= critGap && v < old; }
            string gap = gaps.Count > 0 ? string.Join(" and ", gaps.Select(Elements.Name)).ToLowerInvariant() + " resistance remains below 75%. " : "";
            if (critGap) gap += "Protection against the recorded crit still needs review. ";
            if (gaps.Count > 0 || critGap)
                return new() { Grade = worsened ? "D" : "C", Verdict = "Still vulnerable to the recorded cause", Reason = gap + (worsened ? "At least one measured defense is lower than at death. " : "") + (missing ? "Some other current defenses are unreadable. " : "") + "Those gaps leave a repeat death risk." };
            if (missing)
                return new() { Grade = "?", Verdict = "Not enough evidence to rate readiness", Reason = "The cause or relevant current defenses are missing. Unknown values are not zero and cannot establish that the next fight is safe." };
            bool unresolved = candidates.Any(t => !t.IsNotice && t.Group != "pattern" && t.Group != "unknown");
            if (worsened)
                return new() { Grade = "C", Verdict = "A measured defense has regressed", Reason = "Core resistance and crit checks are covered, but health or another measured resistance is lower than at death. Review that tradeoff before repeating the fight." };
            if (unresolved || !improved || !death.KillingCrit.HasValue)
                return new() { Grade = "B", Verdict = improved ? "Better prepared, with unresolved risks" : "Core checks covered; survival still uncertain", Reason = "Relevant resistance and recorded crit checks are covered. " + (unresolved ? "The recorded hit, recovery or debuff risks still warrant the actions below." : "The capture does not demonstrate that all threats in this fight are covered.") };
            return new() { Grade = "A", Verdict = "Measured defensive gaps improved", Reason = "Relevant resistance and recorded crit checks are covered, max health has not regressed, and a measured defense improved." };
        }
    }
}
