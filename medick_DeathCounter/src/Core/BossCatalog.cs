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
                "EHG names Moon Blast, Tidal Wave, Lightning Blast and a melee attack. Campaign and Monolith variants may differ.",
                "Attack names are verified; their complete damage types are not verified in this catalog. Use the element captured for your death below.",
                Array.Empty<Element>(), new[] {
                    "Movement plan: keep a clear route away from the next Moon Blast or Lightning Blast telegraph. Stop attacking to move before it resolves; an attack name is not proof of its element.",
                    "Wave plan: reposition for each Tidal Wave rather than chasing damage while another hazard blocks your route. Reassess the recorded death's resistance gap before adding more health." },
                new BossSource("EHG 0.9 ability list (March 2023)", "https://forum.lastepoch.com/t/the-convergence-update-beta-0-9-patch-notes/51975")),
            new BossProfile("emperor-corpses", "Emperor of Corpses", new[] { "Emperor of Corpses" },
                "EHG documents Soul Bomb: damage decreases with distance from its center. The arena needs enough room to avoid it.",
                "Soul Bomb's element is not verified in this catalog. Do not assume every attack shares the element of one recorded death.",
                Array.Empty<Element>(), new[] {
                    "Soul Bomb plan: move well away from the center before it detonates. Use the arena's open space; barely leaving the center still exposes you to the blast.",
                    "Positioning plan: reserve a clear escape route before committing to a long attack animation. More health does not replace avoiding the center of Soul Bomb." },
                new BossSource("EHG 0.9l distance scaling (May 2023)", "https://forum.lastepoch.com/t/beta-0-9l-patch-notes/58685"),
                new BossSource("EHG 0.9i arena context (April 2023)", "https://forum.lastepoch.com/t/beta-0-9i-patch-notes/57471")),
            new BossProfile("heorot", "Heorot", new[] { "Heorot" },
                "EHG documents Ice Spike and freezing interactions in this encounter. Historical fixes are not evidence of a current bug or boss immunity.",
                "The complete damage table is not verified here. An Ice Spike name alone does not establish damage typing.",
                Array.Empty<Element>(), new[] {
                    "Ice Spike plan: avoid its telegraph before resuming attacks. Keep a route clear rather than standing still to finish a cast.",
                    "Freeze plan: if your recorded death shows Freeze, review the ailment advice and recovery options in Last death. Do not assume freezing the boss will stop every hazard." },
                new BossSource("EHG 1.0.3 encounter notes (March 2024)", "https://forum.lastepoch.com/t/last-epoch-patch-1-0-3-patch-notes/68385")),
            new BossProfile("julra", "Chronomancer Julra", new[] { "Chronomancer Julra", "Julra" },
                "Temporal Sanctum provides Temporal Shift between Divine and Ruined eras. Higher dungeon tiers and selected modifiers increase danger.",
                "Tunklab's published dungeon data lists Void, Cold and Lightning for Julra. This is encounter coverage, not an attack-by-attack breakdown.",
                new[] { Element.Void, Element.Cold, Element.Lightning }, new[] {
                    "Era plan: keep Temporal Shift available for hazards and learn the safe timing in your tier. Switching eras keeps your position, so do not assume it also moves you out of a ground hazard.",
                    "Preparation plan: check Void, Cold and Lightning coverage. Prioritize the element captured in your death; if capped, review critical protection and measured health loss in Last death before spending gear slots on a larger pool." },
                new BossSource("Tunklab dungeon damage and era data", "https://lastepoch.tunklab.com/dungeon/temporal_sanctum"),
                new BossSource("EHG 0.8.4 dungeon announcement (December 2021)", "https://www.lastepochtools.com/news/article/eternal-legends-beta-0-8-4-patch-notes-45588")),
            new BossProfile("harbinger-hatred", "Harbinger of Hatred", new[] { "Harbinger of Hatred" },
                "EHG names this variant's Void Rahyeh Dive Bomb. This entry does not describe the ordinary Rahyeh timeline encounter.",
                "Damage typing is not verified here. The word Void in an ability name is not a measured damage report.",
                Array.Empty<Element>(), new[] {
                    "Dive Bomb plan: leave the landing telegraph early and leave room to reposition after it lands. Do not follow the landing point to keep attacking.",
                    "Variant plan: use the captured ability and element in Last death. A Harbinger imitation is not interchangeable with the original boss's full move set." },
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
