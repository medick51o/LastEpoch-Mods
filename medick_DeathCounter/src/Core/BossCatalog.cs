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
    public sealed class BossProfile
    {
        public string Id { get; }
        public string Name { get; }
        public string Mechanics { get; }
        public string DamageEvidence { get; }
        public IReadOnlyList<string> Aliases { get; }
        public IReadOnlyList<string> Tips { get; }
        public IReadOnlyList<Element> DamageTypes { get; }
        public IReadOnlyList<BossSource> Sources { get; }
        public BossProfile(string id, string name, string[] aliases, string mechanics, string evidence,
            Element[] types, string[] tips, params BossSource[] sources)
        {
            Id = id; Name = name; Mechanics = mechanics; DamageEvidence = evidence;
            Aliases = Array.AsReadOnly(aliases.ToArray()); Tips = Array.AsReadOnly(tips.ToArray());
            DamageTypes = Array.AsReadOnly(types.ToArray()); Sources = Array.AsReadOnly(sources.ToArray());
        }
    }
    public static class BossCatalog
    {
        public const string Reviewed = "Sources reviewed October 2, 2026. Historical mechanics and published game data; Season 5 encounters still need live verification.";
        public static IReadOnlyList<BossProfile> All { get; } = Array.AsReadOnly(new[]
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
                Array.Empty<Element>(), new[] {
                    "A Harbinger imitation is not interchangeable with the original boss's full move set." },
                new BossSource("EHG 1.2.1 variant notes (April 2025)", "https://forum.lastepoch.com/t/last-epoch-patch-1-2-1-notes/76245"))
        });
        public static BossProfile Match(DeathRecord death)
        {
            return death?.IsBossFight == true ? AttackerGuide(death) : null;
        }
        // A guide lookup helps offline reports which lack the network flag.
        // It is never proof that the game classified this death as a boss fight.
        public static BossProfile AttackerGuide(DeathRecord death)
        {
            if (death == null || death.IsBossFight == false || string.IsNullOrWhiteSpace(death.Killer)) return null;
            string name = death.Killer.Trim();
            return All.FirstOrDefault(p => p.Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)));
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
