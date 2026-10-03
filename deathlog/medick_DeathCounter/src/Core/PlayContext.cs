using System;
using System.Text.RegularExpressions;

namespace medick_DeathCounter.Core
{
    public enum PlayRealm { Unknown, Seasonal, Legacy, Other }

    // Facts captured for this death, never resolved again when history loads.
    // CycleId is the game's identifier, NOT a player-facing season number.
    public sealed class PlayContext
    {
        public PlayRealm Realm { get; set; }
        public int? CycleId { get; set; }
        public string CycleCode { get; set; }
        public string SeasonName { get; set; }
        public bool? Offline { get; set; }
        public string Source { get; set; }

        public static PlayContext FromGameCycle(int? id, string code, string label, bool effectiveCycleKnown, bool? offline)
        {
            code = Clean(code);
            var context = new PlayContext { CycleId = id, CycleCode = code, Offline = offline };
            if (string.Equals(code, "Legacy", StringComparison.Ordinal))
                context.Realm = PlayRealm.Legacy;
            else if (string.Equals(code, "Beta", StringComparison.Ordinal))
                context.Realm = PlayRealm.Other;
            else if (effectiveCycleKnown && id.HasValue && code != null)
                context.Realm = PlayRealm.Seasonal;
            context.SeasonName = context.Realm == PlayRealm.Seasonal ? Clean(label) : null;
            context.Source = effectiveCycleKnown ? "game effective cycle" : code != null ? "character cycle, effective realm unavailable" : null;
            return context;
        }

        public PlayContext Copy() => new()
        {
            Realm = Realm, CycleId = CycleId, CycleCode = CycleCode,
            SeasonName = SeasonName, Offline = Offline, Source = Source,
        };

        public string Label() => Realm switch
        {
            PlayRealm.Legacy => "Legacy",
            PlayRealm.Seasonal => SeasonLabel(),
            PlayRealm.Other => string.Equals(CycleCode, "Beta", StringComparison.Ordinal) ? "Beta" : "Other realm",
            _ => "Season / Legacy unknown",
        };

        string SeasonLabel()
        {
            string name = Clean(SeasonName);
            if (name == null) return "Season (name unavailable)";
            return name.StartsWith("Season", StringComparison.OrdinalIgnoreCase) ? name : "Season: " + name;
        }

        static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string text = Regex.Replace(value, "<[^>]*>", "");
            text = Regex.Replace(text, @"\s+", " ").Trim();
            return text.Length > 0 ? text.Substring(0, Math.Min(text.Length, 160)) : null;
        }
    }
}
