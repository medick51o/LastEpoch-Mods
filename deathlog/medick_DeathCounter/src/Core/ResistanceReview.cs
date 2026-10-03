using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    public sealed class ResistanceReading
    {
        public Element Element;
        public float? Effective, Total, Previous;
        public bool Relevant, KillingType;
        public bool Capped => Effective.HasValue && Effective.Value >= DefenseSnapshot.ResCap - 0.001f;
        public float? Gap => Effective.HasValue ? Math.Max(0, DefenseSnapshot.ResCap - Effective.Value) : null;
        public float? ExtraDamageAtArea;
        // A presentation threshold, not a game rule or a survival probability.
        public bool LargeRelevantGap => Relevant && Gap > 0 && ExtraDamageAtArea >= 0.25f;
    }

    public static class ResistanceReview
    {
        public static List<ResistanceReading> Build(IReadOnlyDictionary<string, float> stats, DeathRecord death,
            IReadOnlyDictionary<string, float> previous = null)
        {
            float? Read(IReadOnlyDictionary<string, float> values, string key) => values != null
                && values.TryGetValue(key, out float value) && float.IsFinite(value) ? value : null;
            var relevant = death == null ? new HashSet<Element>() : CurrentAssessment.RecordedElements(death);
            float? area = death?.ZoneLevel;
            if (!area.HasValue) area = Read(death?.Defenses, "AreaLevel");
            var rows = new List<ResistanceReading>();
            foreach (Element element in Enum.GetValues(typeof(Element)))
            {
                string name = Elements.Name(element);
                float? effective = Read(stats, "Res." + name), total = Read(stats, "ResUncapped." + name);
                if (!effective.HasValue && total.HasValue) effective = Math.Min(total.Value, DefenseSnapshot.ResCap);
                if (effective.HasValue) effective = Math.Min(effective.Value, DefenseSnapshot.ResCap);
                var row = new ResistanceReading
                {
                    Element = element, Effective = effective, Total = total,
                    Previous = Read(previous, "Res." + name), Relevant = relevant.Contains(element),
                    KillingType = Elements.TryParse(death?.KillingElement, out var first) && first == element
                        || Elements.TryParse(death?.SecondaryKillingElement, out var second) && second == element,
                };
                if (effective.HasValue && area.HasValue && area > 0 && row.Gap > 0)
                {
                    float ratio = MitigationMath.ResistanceRatio(effective.Value, DefenseSnapshot.ResCap, 0,
                        MitigationMath.EnemyPenetration(area.Value));
                    if (float.IsFinite(ratio) && ratio > 0) row.ExtraDamageAtArea = 1f / ratio - 1f;
                }
                rows.Add(row);
            }
            return rows.OrderByDescending(r => r.Capped).ThenByDescending(r => r.Relevant)
                .ThenBy(r => r.Effective.HasValue ? 0 : 1).ThenBy(r => (int)r.Element).ToList();
        }
    }
}
