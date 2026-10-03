using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    public sealed class BossSource
    {
        public string Label { get; }
        public string Url { get; }
        public BossSource(string label, string url) { Label = label; Url = url; }
    }

    // A read-only view of one encounter. Encounter returns a fresh deep copy so
    // consumers may use the schema DTO without mutating catalog state.
    public sealed class BossProfile
    {
        readonly EncounterRecord _encounter;
        readonly IReadOnlyList<string> _aliases;
        readonly IReadOnlyList<string> _tips;
        readonly IReadOnlyList<Element> _damageTypes;
        readonly IReadOnlyList<BossSource> _sources;
        readonly IReadOnlyList<string> _browseGroups;

        public string Id { get; }
        public string Name { get; }
        public string Mechanics { get; }
        public string DamageEvidence { get; }
        public IReadOnlyList<string> Aliases => _aliases;
        public IReadOnlyList<string> Tips => _tips;
        public IReadOnlyList<Element> DamageTypes => _damageTypes;
        public IReadOnlyList<BossSource> Sources => _sources;
        public IReadOnlyList<string> BrowseGroups => _browseGroups;
        public EncounterRecord Encounter => BossResearchCatalog.Clone(_encounter);
        public EncounterRecord RawData => Encounter;

        // Kept for the small historical Starter catalog and existing callers.
        public BossProfile(string id, string name, string[] aliases, string mechanics, string evidence,
            Element[] types, string[] tips, params BossSource[] sources)
        {
            Id = id ?? "";
            Name = name ?? "";
            Mechanics = mechanics ?? "";
            DamageEvidence = evidence ?? "";
            _aliases = Array.AsReadOnly((aliases ?? Array.Empty<string>()).ToArray());
            _tips = Array.AsReadOnly((tips ?? Array.Empty<string>()).ToArray());
            _damageTypes = Array.AsReadOnly((types ?? Array.Empty<Element>()).Distinct().ToArray());
            _sources = Array.AsReadOnly((sources ?? Array.Empty<BossSource>()).Where(s => s != null)
                .GroupBy(s => s.Url ?? "", StringComparer.Ordinal).Select(g => g.First()).ToArray());
            _encounter = BossCatalogSchema.Project(this);
            _browseGroups = ReadBrowseGroups(_encounter);
        }

        internal BossProfile(EncounterRecord encounter)
        {
            _encounter = BossResearchCatalog.Clone(encounter) ?? new EncounterRecord();
            Id = _encounter.Id ?? "";
            Name = _encounter.DisplayName ?? "";
            _aliases = Array.AsReadOnly((_encounter.Aliases ?? new List<AliasRecord>())
                .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Text)).Select(a => a.Text).ToArray());
            _tips = Array.AsReadOnly((_encounter.Tips ?? new List<TipRecord>())
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Text)).Select(t => t.Text).ToArray());
            _damageTypes = Array.AsReadOnly((_encounter.DamageCoverage?.Elements ?? new List<string>())
                .Select(s => Elements.TryParse(s, out var value) ? (Element?)value : null)
                .Where(e => e.HasValue).Select(e => e.Value).Distinct().ToArray());
            _sources = Array.AsReadOnly((_encounter.Claims ?? new List<ClaimRecord>())
                .Where(c => c?.Source != null)
                .GroupBy(c => c.Source.Url ?? "", StringComparer.Ordinal)
                .Select(g => g.First())
                .Select(c => new BossSource(c.Source.Label, c.Source.Url)).ToArray());
            _browseGroups = ReadBrowseGroups(_encounter);
            Mechanics = ReferenceContext(_encounter);
            var coverage = _encounter.DamageCoverage;
            DamageEvidence = CoverageSummary(coverage);
        }

        static IReadOnlyList<string> ReadBrowseGroups(EncounterRecord encounter) =>
            Array.AsReadOnly((encounter?.BrowseGroups?.Items ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.Ordinal).ToArray());

        static string ReferenceContext(EncounterRecord encounter)
        {
            var parts = new List<string>();
            var locations = encounter?.Locations;
            if (locations?.Status == "known")
            {
                string[] names = (locations.Items ?? new List<LocationRecord>())
                    .Where(l => !string.IsNullOrWhiteSpace(l?.DisplayName))
                    .Select(l => l.DisplayName.Trim()).Distinct(StringComparer.Ordinal).ToArray();
                if (names.Length > 0)
                    parts.Add("Where: " + string.Join(", ", names) + ".");
            }
            if (encounter?.Parent?.Status == "known" && !string.IsNullOrWhiteSpace(encounter.Parent.EncounterId))
                parts.Add("Follow-up to: " + encounter.Parent.EncounterId.Replace('-', ' ') + ".");
            return string.Join(" ", parts);
        }

        static string CoverageSummary(DamageCoverage coverage)
        {
            if (coverage == null || coverage.Status != "known")
                return "Damage types are still being researched for this encounter.";

            string basis = coverage.EvidenceClass == "community-guide"
                ? "Community-reported damage"
                : coverage.EvidenceClass == "datamined-reference" ? "Game-data reference"
                : coverage.EvidenceClass == "developer-documentation" ? "Developer-documented damage"
                : "Reported damage";
            return basis + "; current-season coverage may be incomplete.";
        }
    }

    public static class BossCatalog
    {
        public const string Reviewed = "Field notes assembled October 2, 2026. Season 5 verification is incomplete.";

        static readonly IReadOnlyList<BossProfile> StarterProfiles = Array.AsReadOnly(new[]
        {
            new BossProfile("lagon", "Lagon", new[] { "Lagon" },
                "EHG's 0.9 performance notes name Moon Blast, Tidal Wave, Lightning Blast and a melee attack. Those names are not a current skill list.",
                "Attack names are verified as historical labels; their complete damage types are not verified in this catalog. Use the element captured for your death below.",
                Array.Empty<Element>(), Array.Empty<string>(),
                new BossSource("EHG 0.9 ability list (March 2023)", "https://forum.lastepoch.com/t/the-convergence-update-beta-0-9-patch-notes/51975")),
            new BossProfile("emperor-corpses", "Emperor of Corpses", new[] { "Emperor of Corpses" },
                "EHG documents Soul Bomb: damage decreases with distance from its center. The arena needs enough room to avoid it.",
                "Soul Bomb's element is not verified in this catalog. Do not assume every attack shares the element of one recorded death.",
                Array.Empty<Element>(), Array.Empty<string>(),
                new BossSource("EHG 0.9l distance scaling (May 2023)", "https://forum.lastepoch.com/t/beta-0-9l-patch-notes/58685"),
                new BossSource("EHG 0.9i arena context (April 2023)", "https://forum.lastepoch.com/t/beta-0-9i-patch-notes/57471")),
            new BossProfile("heorot", "Heorot", new[] { "Heorot" },
                "EHG's 1.0.3 note fixes Ice Spike stopping projectiles, and being frozen not stopping actions in multiplayer. That historical fix is not evidence of a current bug or of boss immunity.",
                "The complete damage table is not verified here. An Ice Spike name alone does not establish damage typing.",
                Array.Empty<Element>(), Array.Empty<string>(),
                new BossSource("EHG 1.0.3 encounter notes (March 2024)", "https://forum.lastepoch.com/t/last-epoch-patch-1-0-3-patch-notes/68385")),
            new BossProfile("julra", "Chronomancer Julra", new[] { "Chronomancer Julra", "Julra" },
                "Temporal Sanctum provides Temporal Shift between Divine and Ruined eras. Higher dungeon tiers and selected modifiers increase danger.",
                "Tunklab's published dungeon data lists Void, Cold and Lightning for Julra. This is encounter coverage, not an attack-by-attack breakdown.",
                new[] { Element.Void, Element.Cold, Element.Lightning }, Array.Empty<string>(),
                new BossSource("Tunklab dungeon damage and era data", "https://lastepoch.tunklab.com/dungeon/temporal_sanctum"),
                new BossSource("EHG 0.8.4 dungeon announcement (December 2021)", "https://www.lastepochtools.com/news/article/eternal-legends-beta-0-8-4-patch-notes-45588")),
            new BossProfile("harbinger-hatred", "Harbinger of Hatred", new[] { "Harbinger of Hatred" },
                "EHG names this variant's Void Rahyeh Dive Bomb. This entry does not describe the ordinary Rahyeh timeline encounter.",
                "Damage typing is not verified here. The word Void in an ability name is not a measured damage report.",
                Array.Empty<Element>(), new[] { "A Harbinger imitation is not interchangeable with the original boss's full move set." },
                new BossSource("EHG 1.2.1 variant notes (April 2025)", "https://forum.lastepoch.com/t/last-epoch-patch-1-2-1-notes/76245"))
        });

        static readonly BossResearchLoadResult _research = BossResearchCatalog.LoadEmbedded(
            typeof(BossCatalog).Assembly, BossResearchCatalog.ResourceName, StarterProfiles);

        // Starter is the historical five-profile set used by the project baseline.
        public static IReadOnlyList<BossProfile> Starter => StarterProfiles;
        // All is the manual browse catalog when the embedded data is valid.
        public static IReadOnlyList<BossProfile> All => _research.Profiles;
        public static IReadOnlyList<string> LoadDiagnostics => _research.Diagnostics;
        public static CatalogDocument Research => BossResearchCatalog.Clone(_research.Document);
        public static CatalogDocument ResearchDocument => Research;

        public static BossProfile Match(DeathRecord death)
        {
            if (death?.IsBossFight != true) return null;
            return AttackerGuide(death);
        }

        // This is a guide lookup for reports without the network boss flag; a
        // death flag of false always blocks it. Ambiguous aliases never choose first.
        public static BossProfile AttackerGuide(DeathRecord death)
        {
            if (death == null || death.IsBossFight == false || string.IsNullOrWhiteSpace(death.Killer)) return null;
            string name = death.Killer.Trim();
            var matches = All.Where(p => p.Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)))
                .Distinct().Take(2).ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        public static string EncounterLabel(DeathRecord death)
        {
            var boss = Match(death);
            return boss != null ? "Boss encounter: " + boss.Name + " (game flag and attacker match)"
                : death?.IsBossFight == true ? "Boss encounter confirmed by game; boss not identified"
                : death?.IsBossFight == false ? "Game did not flag this as a boss encounter"
                : "Boss encounter unknown";
        }

        public static IReadOnlyList<Element> ObservedTypes(DeathRecord death)
        {
            var result = new List<Element>();
            if (Elements.TryParse(death?.KillingElement, out var primary)) result.Add(primary);
            if (Elements.TryParse(death?.SecondaryKillingElement, out var secondary) && !result.Contains(secondary)) result.Add(secondary);
            return result.AsReadOnly();
        }
    }
}
