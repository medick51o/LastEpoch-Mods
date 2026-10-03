using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace medick_DeathCounter.Core
{
    // Reported categorical attack facts are read-only browsing data. They never
    // supply a captured death type, hit delivery, defense snapshot or grade.
    public sealed class BossGuideMove
    {
        public string EncounterId { get; }
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<Element> Elements { get; }
        public IReadOnlyList<string> Ailments { get; }
        public string Delivery { get; }
        public string InheritedFromEncounterId { get; }
        public string SourceUrl { get; }
        public string Updated { get; }
        public string EvidenceClass => "community-guide";

        internal BossGuideMove(GuideMoveRow row)
        {
            EncounterId = row.EncounterId; Id = row.Id; Name = row.Name;
            Elements = Array.AsReadOnly(row.Elements.Select(e => medick_DeathCounter.Core.Elements.TryParse(e, out var value)
                ? value : throw new InvalidDataException("Unrecognized guide element: " + e)).Distinct().ToArray());
            Ailments = Array.AsReadOnly((row.Ailments ?? new List<string>()).Distinct(StringComparer.Ordinal).ToArray());
            Delivery = row.Delivery ?? "unknown";
            InheritedFromEncounterId = row.InheritedFromEncounterId; SourceUrl = row.SourceUrl; Updated = row.Updated;
        }
    }

    public static class BossGuideMoves
    {
        static readonly IReadOnlyDictionary<string, IReadOnlyList<BossGuideMove>> _byEncounter = Load();
        public static string LoadError { get; private set; }
        public static IReadOnlyList<BossGuideMove> For(string encounterId) =>
            encounterId != null && _byEncounter.TryGetValue(encounterId, out var moves) ? moves : Array.Empty<BossGuideMove>();

        // The guide move behind a recorded killing ability, only when the
        // attacker resolves to exactly one profile and exactly one move. Game
        // labels such as "Melee Attack (Portal Spear)" match on either part.
        public static BossGuideMove KillingMove(DeathRecord death)
        {
            var boss = BossCatalog.AttackerGuide(death);
            if (boss == null || string.IsNullOrWhiteSpace(death.KillerAbility)) return null;
            var wanted = NameParts(death.KillerAbility);
            var matches = For(boss.Id).Where(m => NameParts(m.Name).Overlaps(wanted)).Take(2).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        static HashSet<string> NameParts(string name)
        {
            var parts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string text = (name ?? "").Trim();
            if (text.Length == 0) return parts;
            parts.Add(text);
            int open = text.IndexOf('('), close = text.LastIndexOf(')');
            if (open > 0 && close > open)
            {
                string before = text.Substring(0, open).Trim(), inside = text.Substring(open + 1, close - open - 1).Trim();
                if (before.Length > 0 && !before.Equals("Melee Attack", StringComparison.OrdinalIgnoreCase)) parts.Add(before);
                if (inside.Length > 0) parts.Add(inside);
            }
            return parts;
        }

        static IReadOnlyDictionary<string, IReadOnlyList<BossGuideMove>> Load()
        {
            try
            {
                using var stream = typeof(BossGuideMoves).Assembly.GetManifestResourceStream("medick_DeathCounter.BossGuideMoves.json");
                if (stream == null) throw new InvalidDataException("Embedded community move notes are missing.");
                using var reader = new StreamReader(stream);
                var rows = JsonSerializer.Deserialize<List<GuideMoveRow>>(reader.ReadToEnd(), BossCatalogSchema.Json);
                if (rows == null) throw new InvalidDataException("Community move notes are empty.");
                var ids = new HashSet<string>(StringComparer.Ordinal);
                var profiles = BossCatalog.All.ToDictionary(p => p.Id, p => p.Encounter, StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    if (row == null || row.EvidenceClass != "community-guide" || row.Elements == null
                        || !ids.Add(row.Id ?? "") || string.IsNullOrWhiteSpace(row.Name)
                        || !profiles.TryGetValue(row.EncounterId ?? "", out var profile)
                        || profile.Abilities?.Items?.Any(a => a.Id == row.Id) != true)
                        throw new InvalidDataException("Community move note is not linked to one catalog ability.");
                }
                return rows.Select(r => new BossGuideMove(r)).GroupBy(m => m.EncounterId, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => (IReadOnlyList<BossGuideMove>)Array.AsReadOnly(g.ToArray()), StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                LoadError = ex.Message;
                return new Dictionary<string, IReadOnlyList<BossGuideMove>>(StringComparer.Ordinal);
            }
        }
    }

    internal sealed class GuideMoveRow
    {
        public string EncounterId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Elements { get; set; }
        public List<string> Ailments { get; set; }
        public string Delivery { get; set; }
        public string InheritedFromEncounterId { get; set; }
        public string SourceUrl { get; set; }
        public string Updated { get; set; }
        public string EvidenceClass { get; set; }
    }
}
