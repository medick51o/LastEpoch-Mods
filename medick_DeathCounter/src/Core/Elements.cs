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

    // What an ailment is and does to YOU. IsDot = it deals the damage itself;
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
        // Order matters for Find(): more specific keywords first, so
        // "ArmourShred" is not read as a generic "Shred" and "Frostbite" is
        // not read as "Frost"-anything else.
        public static readonly AilmentInfo[] All =
        {
            Dot("Bleed",     Element.Physical,  "Physical damage over time. Armor does not reduce it.", "bleed"),
            Dot("Ignite",    Element.Fire,      "Fire damage over time.", "ignite", "burning"),
            Dot("Poison",    Element.Poison,    "Poison damage over time; stacks also lower your poison resistance.", "poison"),
            Dot("Frostbite", Element.Cold,      "Cold damage over time.", "frostbite"),
            Dot("Electrify", Element.Lightning, "Lightning damage over time.", "electrify"),
            Dot("Time Rot",  Element.Void,      "Void damage over time.", "timerot", "time rot", "time_rot"),
            Dot("Doom",      Element.Void,      "Void damage over time.", "doom"),
            Dot("Damned",    Element.Necrotic,  "Necrotic damage over time.", "damned"),

            Setup("Armor Shred",     null,              "Lowers your armor so hits land harder.", "armourshred", "armorshred", "armour shred", "armor shred"),
            Setup("Resistance Shred", null,             "Lowers one of your resistances below its cap.", "resistanceshred", "resshred", "resistance shred", "shred"),
            Setup("Freeze",          Element.Cold,      "You cannot act while frozen.", "freeze", "frozen"),
            Setup("Chill",           Element.Cold,      "Slows your attacks, casts and movement.", "chill"),
            Setup("Shock",           Element.Lightning, "You take more damage and get stunned more easily.", "shock"),
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
            // "PoisonResistance" / "IgniteChance" style stat names are not ailments.
            if (t.Contains("resistance") && !t.Contains("shred")) return null;
            if (t.Contains("chance")) return null;
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
