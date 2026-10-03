using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Conditional preparation reminders. These never read or change a death,
    // promise fight survival, or treat community coverage as live evidence.
    public static class BossFieldNotes
    {
        public static IReadOnlyList<string> Preparation(BossProfile boss)
        {
            if (boss == null || boss.DamageTypes.Count == 0)
                return Array.AsReadOnly(new[] { "Damage coverage is not mapped yet. Use your captured death to choose a resistance change." });

            var notes = new List<string>();
            var priority = PriorityResistances(boss);
            notes.Add(boss.Id == "majasa-phase-1"
                ? "Cap physical resistance first for the whole fight, then fire, lightning, and poison. This phase's mapped hits are the last three. Physical is highest because the next phase is physical only."
                : boss.Id == "shade-of-orobyss"
                ? "Check Void resistance first, then the variant's other element. The list covers possible variants, not every attack in one fight."
                : priority.Count > 0 && priority.Count < boss.DamageTypes.Count
                ? $"Cap {Join(priority)} resistance first. Community guides report most of this fight's damage as {(priority.Count == 1 ? "that type" : "those types")}; then close gaps below {DefenseSnapshot.ResCap:0}% in the other listed types."
                : $"Fix gaps below the normal {DefenseSnapshot.ResCap:0}% cap for the listed resistances before investing in more damage.");
            var encounter = boss.Encounter;
            var ailments = (encounter.Ailments?.Items ?? new List<AilmentRecord>())
                .Select(a => a.Name ?? "").ToArray();

            if (ailments.Contains("Curse of Aberroth", StringComparer.OrdinalIgnoreCase))
                notes.Add("A capped sheet can become uncapped after Curse of Aberroth. Resistance buffer helps absorb the reduction; avoiding stacks remains the priority.");
            else if (ailments.Any(a => a.Contains("Shred Cold Resistance", StringComparison.OrdinalIgnoreCase)))
                notes.Add("Cold resistance shred can reopen a capped defense. Extra uncapped Cold resistance gives a buffer while those stacks are active.");
            else if (ailments.Any(a => a.Contains("Shred Void Resistance", StringComparison.OrdinalIgnoreCase)))
                notes.Add("Void resistance shred can reopen a capped defense. Extra uncapped Void resistance gives a buffer while those stacks are active.");
            else if (ailments.Any(a => a.Contains("Shred Armour", StringComparison.OrdinalIgnoreCase)))
                notes.Add("Armor shred weakens repeated-hit protection. Bring recovery between hits and avoid relying on your standing armor value alone.");

            string[] damagingAilments = ailments.Where(a =>
                new[] { "Ignite", "Frostbite", "Poison", "Bleed", "Damned" }.Contains(a, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (damagingAilments.Length > 0)
                notes.Add("Plan recovery and a usable cleanse for " + string.Join(", ", damagingAilments) + ". Armor and crit protection do not stop their damage over time.");
            else if (encounter.DamageCoverage?.PerAbility?.Items?.Any(c => c.Delivery == "dot") == true)
                notes.Add("This fight includes reported damage over time. Prepare resistance and recovery; armor and crit protection address hits, not that portion.");

            return Array.AsReadOnly(notes.Distinct(StringComparer.Ordinal).Take(3).ToArray());
        }

        // Resistances community boss guides name as the main damage of a fight.
        // Returned types stay inside coverage, except Majasa phase 1: the guide
        // puts physical first for the whole fight, while this phase's mapped
        // hits are fire, lightning, and poison.
        static readonly Dictionary<string, Element[]> GuidePriority = new(StringComparer.Ordinal)
        {
            ["volcanic-shaman"] = new[] { Element.Fire, Element.Necrotic },
            ["emperor-of-corpses"] = new[] { Element.Necrotic, Element.Physical },
            ["frost-lich-formosus"] = new[] { Element.Cold, Element.Necrotic, Element.Lightning },
            ["god-hunter-argentus"] = new[] { Element.Fire, Element.Cold },
            ["hartons-husk"] = new[] { Element.Void, Element.Physical },
            ["heorot-monolith"] = new[] { Element.Cold, Element.Physical },
            ["lagon-monolith"] = new[] { Element.Lightning, Element.Cold, Element.Physical },
            ["rahyeh-the-black-sun"] = new[] { Element.Void },
            ["husk-of-elder-gaspar"] = new[] { Element.Void, Element.Fire, Element.Cold, Element.Lightning },
            ["lagon-campaign"] = new[] { Element.Cold, Element.Lightning, Element.Physical },
            ["majasa-phase-1"] = new[] { Element.Physical, Element.Fire, Element.Lightning, Element.Poison },
            ["majasa-phase-2"] = new[] { Element.Physical },
            ["aberroth"] = new[] { Element.Void, Element.Physical },
            ["herald-of-oblivion"] = new[] { Element.Void, Element.Physical },
            ["vision-of-the-observer"] = new[] { Element.Void },
            ["shade-of-orobyss"] = new[] { Element.Void },
        };

        public static IReadOnlyList<Element> PriorityResistances(BossProfile boss)
        {
            if (boss == null || !GuidePriority.TryGetValue(boss.Id, out var list))
                return Array.AsReadOnly(Array.Empty<Element>());
            var kept = list.Where(e => boss.DamageTypes.Contains(e)).ToList();
            if (boss.Id == "majasa-phase-1" && list.Contains(Element.Physical) && !kept.Contains(Element.Physical))
                kept.Insert(0, Element.Physical);
            return Array.AsReadOnly(kept.ToArray());
        }

        // Shown on the death card itself, so the matching boss note appears
        // when the player is reading why they died, not only in Boss notes.
        public static IReadOnlyList<string> ForDeath(DeathRecord death, out BossProfile boss, out BossGuideMove move)
        {
            boss = BossCatalog.AttackerGuide(death);
            move = boss == null ? null : BossGuideMoves.KillingMove(death);
            var lines = new List<string>();
            if (boss == null) return Array.AsReadOnly(lines.ToArray());
            var found = move;
            if (found != null)
            {
                string types = found.Elements.Count > 0 ? Join(move.Elements) + " damage" : "an unmapped damage type";
                lines.Add($"{found.Name} is reported as {types}"
                    + (found.Ailments.Count > 0 ? " and can apply " + string.Join(", ", found.Ailments) : "") + ".");
                var recorded = BossCatalog.ObservedTypes(death);
                if (recorded.Count > 0 && found.Elements.Count > 0 && !recorded.All(e => found.Elements.Contains(e)))
                    lines.Add("Your capture recorded " + Join(recorded) + " damage. Trust the capture for this death.");
                // "Slam" must not borrow the "Echo Slam" tip: mentions inside a
                // longer move name of the same fight do not count.
                var longer = BossGuideMoves.For(boss.Id).Select(m => m.Name)
                    .Where(n => !string.Equals(n, found.Name, StringComparison.OrdinalIgnoreCase)
                        && n.IndexOf(found.Name, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                string tip = boss.Tips.FirstOrDefault(t => longer.Aggregate(t, (text, n) =>
                        System.Text.RegularExpressions.Regex.Replace(text, System.Text.RegularExpressions.Regex.Escape(n), "",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    .IndexOf(found.Name, StringComparison.OrdinalIgnoreCase) >= 0);
                if (tip != null) lines.Add(tip);
            }
            else if (!string.IsNullOrWhiteSpace(death.KillerAbility))
                lines.Add($"{death.KillerAbility.Trim()} is not in the move list for {boss.Name} yet.");
            var priority = PriorityResistances(boss);
            if (priority.Count > 0 && priority.Count < boss.DamageTypes.Count)
                lines.Add($"For this fight, cap {Join(priority)} resistance first.");
            if (found != null)
                lines.AddRange(MaxrollPlayerNotes.MoveLines(found.Id));
            string resists = MaxrollPlayerNotes.Resists(boss.Id);
            if (resists != null) lines.Add(resists);
            return Array.AsReadOnly(lines.Distinct(StringComparer.Ordinal).ToArray());
        }

        // Attribution for the notes shown in Boss notes: the catalog's cited
        // references and the attack-fact guide pages, as plain text. Labels
        // already say whether a source is a community or game-data reference.
        public static IReadOnlyList<string> SourceLines(BossProfile boss)
        {
            var pages = new List<(string Url, string Label, string Updated)>();
            if (boss == null) return Array.Empty<string>();
            static string Bare(string url) => (url ?? "").Trim().Replace("https://", "").Replace("http://", "").TrimEnd('/');
            void Add(string url, string label, string updated)
            {
                string bare = Bare(url);
                if (bare.Length == 0) return;
                int at = pages.FindIndex(p => string.Equals(p.Url, bare, StringComparison.OrdinalIgnoreCase));
                if (at < 0) { pages.Add((bare, label, updated)); return; }
                var old = pages[at];
                pages[at] = (old.Url, old.Label ?? label,
                    string.CompareOrdinal(updated ?? "", old.Updated ?? "") > 0 ? updated : old.Updated);
            }
            foreach (var source in boss.Sources) Add(source?.Url, string.IsNullOrWhiteSpace(source?.Label) ? null : source.Label.Trim(), null);
            foreach (var move in BossGuideMoves.For(boss.Id))
                Add(move.SourceUrl, "Attack facts", string.IsNullOrWhiteSpace(move.Updated) ? null : move.Updated.Trim());
            return pages.Select(p => (p.Label ?? "Reference") + ": " + p.Url + (p.Updated != null ? " (guide updated " + p.Updated + ")" : ""))
                .ToList().AsReadOnly();
        }

        static string Join(IEnumerable<Element> elements)
        {
            var names = elements.Select(Elements.Name).ToArray();
            return names.Length <= 1 ? string.Join("", names)
                : string.Join(", ", names.Take(names.Length - 1)) + " and " + names[^1];
        }
    }
}
