using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Browse context only, from existing catalog relationships. This cannot
    // identify the actual timeline/zone or killing blow of a saved death.
    public static class BossContextLabels
    {
        static readonly Dictionary<string, string> TimelineCache = new(StringComparer.Ordinal);
        static readonly Dictionary<string, string> LabelCache = new(StringComparer.Ordinal);
        static readonly HashSet<string> Timelines = new(StringComparer.Ordinal)
        {
            "Fall of the Outcasts", "The Stolen Lance", "The Black Sun",
            "Blood, Frost, and Death", "Ending the Storm", "Fall of the Empire",
            "Reign of Dragons", "The Last Ruin", "The Age of Winter", "Spirits of Fire",
        };
        public static string Timeline(BossProfile boss)
        {
            if (boss == null) return null;
            if (TimelineCache.TryGetValue(boss.Id, out var cached)) return cached;
            var encounter = boss.Encounter;
            string timeline = encounter?.Locations?.Status == "known" ? encounter.Locations.Items?.Select(l => l?.DisplayName)
                .FirstOrDefault(n => n != null && Timelines.Contains(n)) : null;
            TimelineCache[boss.Id] = timeline; return timeline;
        }
        public static string Label(BossProfile boss)
        {
            if (boss == null) return null;
            if (LabelCache.TryGetValue(boss.Id, out var cached)) return cached;
            string label = BuildLabel(boss); LabelCache[boss.Id] = label; return label;
        }
        static string BuildLabel(BossProfile boss)
        {
            var encounter = boss.Encounter;
            string timeline = Timeline(boss);
            if (boss.BrowseGroups.Contains("harbinger"))
            {
                var parent = encounter.Parent?.Status == "known"
                    ? BossCatalog.All.FirstOrDefault(p => p.Id == encounter.Parent.EncounterId) : null;
                string after = parent == null ? "Follow-up boss not mapped" : "Spawns after " + parent.Name;
                return after + (timeline == null ? "" : " · " + timeline);
            }
            if (timeline != null) return "Timeline: " + timeline;
            if (boss.Id == "lagon-campaign" || boss.Id == "heorot-campaign" || boss.Id.StartsWith("majasa-phase-", StringComparison.Ordinal)) return "Campaign encounter";
            if (boss.Id == "shade-of-orobyss") return "Monolith encounter · Multiple timelines";
            string location = encounter.Locations?.Status == "known" ? encounter.Locations.Items?.FirstOrDefault()?.DisplayName : null;
            return string.IsNullOrWhiteSpace(location) ? null : "Location: " + location;
        }
    }
}
