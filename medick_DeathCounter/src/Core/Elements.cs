using System;

namespace medick_DeathCounter.Core
{
    // Last Epoch's seven damage types, in the game's own DamageType order
    // (PHYSICAL, FIRE, COLD, LIGHTNING, NECROTIC, VOID, POISON). The game
    // glue re-maps by enum NAME at runtime if the order ever changes, so this
    // order is only the fallback.
    public enum Element { Physical, Fire, Cold, Lightning, Necrotic, Void, Poison }

    public static class Elements
    {
        public const int Count = 7;

        public static readonly string[] Names =
            { "Physical", "Fire", "Cold", "Lightning", "Necrotic", "Void", "Poison" };

        public static string Name(Element e) => Names[(int)e];

        public static bool TryParse(string text, out Element e)
        {
            e = Element.Physical;
            if (string.IsNullOrWhiteSpace(text)) return false;
            return Enum.TryParse(text.Trim(), ignoreCase: true, out e) && Enum.IsDefined(typeof(Element), e);
        }
    }

    // What an ailment is and does to YOU. Effects checked against
    // docs/RESEARCH-damage-and-defenses.md (patch 1.4.x). IsDot = it deals the damage itself;
    // the rest are setups (control, shred, amplify) that let something else
    // land the kill.
    public sealed class AilmentInfo
    {
        public string   Name;
        public Element? Element;     // null = no damage type of its own
        public bool     IsDot;
        public string[] Keywords;    // matched case-insensitively against game names
        public string   Effect;      // one line, shown in the panel
    }

    public static class Ailments
    {
        // Order matters for Find(): first match wins, so the specific
        // entries come before the general ones.
        public static readonly AilmentInfo[] All =
        {
            // Shreds first: the game's AilmentID names them PoisonResShred,
            // FireResShred, ArmourShred ... which must not read as Poison/Ignite.
            Setup("Armor Shred",      null,             "Lowers your armor so hits land harder.", "armourshred", "armorshred", "armour shred", "armor shred"),
            Setup("Resistance Shred", null,             "Lowers one of your resistances below its cap.", "resshred", "resistanceshred", "resistance shred", "shred"),

            Dot("Bleed",     Element.Physical,  "Physical damage over time. Armor does not reduce it.", "bleed"),
            Dot("Ignite",    Element.Fire,      "Fire damage over time.", "ignite", "burning"),
            Dot("Poison",    Element.Poison,    "Poison damage over time; each stack also lowers your poison resistance a little.", "poison"),
            Dot("Frostbite", Element.Cold,      "Cold damage over time that also makes you easier to freeze.", "frostbite"),
            Dot("Electrify", Element.Lightning, "Lightning damage over time.", "electrify"),
            Dot("Time Rot",  Element.Void,      "Void damage over time that also makes stuns on you last longer.", "timerot", "time rot", "time_rot"),
            Dot("Abyssal Decay", Element.Void,  "Void damage over time.", "abyssaldecay", "abyssal decay"),
            Dot("Doom",      Element.Void,      "Void damage over time that also makes you take more melee damage.", "doom"),
            Dot("Damned",    Element.Necrotic,  "Necrotic damage over time that also cuts your health regen.", "damned"),

            Setup("Freeze",          Element.Cold,      "You cannot act while frozen. More max health and ward make you harder to freeze.", "freeze", "frozen"),
            Setup("Chill",           Element.Cold,      "Slows your attacks, casts and movement.", "chill"),
            Setup("Shock",           Element.Lightning, "Lowers your lightning resistance and makes you easier to stun.", "shock"),
            Setup("Stun",            null,              "You cannot act while stunned.", "stun"),
            Setup("Slow",            null,              "Slows your movement, so you cannot walk out of danger.", "slow"),
            Setup("Blind",           null,              "Lowers your chance to land critical strikes.", "blind"),
            Setup("Frailty",         null,              "Lowers the damage you deal.", "frailty"),
        };

        static AilmentInfo Dot(string name, Element el, string effect, params string[] kw) =>
            new() { Name = name, Element = el, IsDot = true, Keywords = kw, Effect = effect };

        static AilmentInfo Setup(string name, Element? el, string effect, params string[] kw) =>
            new() { Name = name, Element = el, IsDot = false, Keywords = kw, Effect = effect };

        // Map any game-side text (type name, ability name, buff name) to a
        // known ailment. Null when nothing matches.
        public static AilmentInfo Find(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text.ToLowerInvariant();
            string bare = t.Replace(" ", "").Replace("_", "");
            // An exact AilmentID name (Stun, Chill, ArmourShred ...) wins outright.
            foreach (var a in All)
                foreach (var k in a.Keywords)
                    if (bare == k.Replace(" ", "").Replace("_", "")) return a;
            // Not ailments on you: stat names ("PoisonResistance", "IgniteChance")
            // and shrine buffs ("ShrineStun").
            if (t.Contains("resistance") && !t.Contains("shred")) return null;
            if (t.Contains("chance") || t.Contains("shrine")) return null;
            foreach (var a in All)
                foreach (var k in a.Keywords)
                    if (t.Contains(k)) return a;
            return null;
        }

        public static AilmentInfo ByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var a in All)
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a;
            return null;
        }
    }
}
