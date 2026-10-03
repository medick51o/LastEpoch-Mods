using System;
using System.Collections.Generic;
using System.Text;

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
            Setup("Armor Shred",      null,             "Lowers your armor so hits land harder.", "armourshred", "armorshred", "armour shred", "armor shred", "shred armor"),
            Shred("Physical", Element.Physical),
            Shred("Fire", Element.Fire),
            Shred("Cold", Element.Cold),
            Shred("Lightning", Element.Lightning),
            Shred("Necrotic", Element.Necrotic),
            Shred("Void", Element.Void),
            Shred("Poison", Element.Poison),
            Setup("Resistance Shred", null,             "Lowers one of your resistances below its cap.", "resshred", "resistanceshred", "resistance shred", "shred"),
            Setup("Critical Vulnerability", null, "Raises the chance to be critically hit and lowers critical strike avoidance.", "criticalvulnerability", "critical vulnerability"),
            Setup("Marked for Death", null, "Lowers all resistances by 25 points.", "markedfordeath", "marked for death"),

            Dot("Bleed",     Element.Physical,  "Physical damage over time. Armor does not reduce it.", "bleed"),
            Dot("Ignite",    Element.Fire,      "Fire damage over time.", "ignite", "burning"),
            Dot("Poison",    Element.Poison,    "Poison damage over time; each stack also lowers your poison resistance a little.", "poison"),
            Dot("Frostbite", Element.Cold,      "Cold damage over time that also makes you easier to freeze.", "frostbite"),
            Dot("Electrify", Element.Lightning, "Lightning damage over time.", "electrify"),
            Dot("Time Rot",  Element.Void,      "Void damage over time that also makes stuns on you last longer.", "timerot", "time rot", "time_rot"),
            Dot("Abyssal Decay", Element.Void,  "Void damage over time.", "abyssaldecay", "abyssal decay"),
            Dot("Doom",      Element.Void,      "Void damage over time that also makes you take more melee damage.", "doom"),
            Dot("Damned",    Element.Necrotic,  "Necrotic damage over time that also cuts your health regen.", "damned"),
            Dot("Plague", Element.Poison, "Poison damage over time.", "plague"),
            Dot("Spreading Flames", Element.Fire, "Fire damage over time.", "spreadingflames", "spreading flames"),

            // Aberroth ailments are their own effects. They must not match Shock, Frailty, or Chill.
            Setup("Curse of Aberroth", null, "Lowers every resistance by 10 points per stack. There is no stack limit, and it cannot be cleansed.", "curseofaberroth", "curse of aberroth"),
            Setup("Shock of Aberroth", null, "5% increased damage taken per stack. It does not lower lightning resistance.", "shockofaberroth", "shock of aberroth"),
            Setup("Frailty of Aberroth", null, "5% less damage per stack. 10% less health leech, ward retention, and health regen per stack. 50% reduced healing effectiveness per stack. Unlimited stacks, duration 4 seconds. Inflicted by multiple abilities, but Void Beams and Quicksand apply most of it.", "frailtyofaberroth", "frailty of aberroth"),
            Setup("Chill of Aberroth", null, "Slows your movement, attacks and casts. It is not Chill, and cold resistance does not stop it.", "chillofaberroth", "chill of aberroth"),

            Setup("Freeze",          Element.Cold,      "You cannot act while frozen. More max health and current ward make you harder to freeze. Cold resistance does not stop freeze.", "freeze", "frozen"),
            Setup("Chill",           Element.Cold,      "Slows your attacks, casts and movement. Cold resistance does not prevent chill.", "chill"),
            Setup("Shock",           Element.Lightning, "Lowers your lightning resistance and makes you easier to stun.", "shock"),
            Setup("Stun",            null,              "You cannot act while stunned.", "stun"),
            Setup("Slow",            null,              "Slows your movement, so you cannot walk out of danger.", "slow"),
            Setup("Blind",           null,              "Lowers your chance to land critical strikes.", "blind"),
            Setup("Frailty",         null,              "Lowers the damage you deal.", "frailty"),
        };

        static AilmentInfo Dot(string name, Element el, string effect, params string[] kw) =>
            new() { Name = name, Element = el, IsDot = true, Keywords = kw, Effect = effect };

        static AilmentInfo Shred(string name, Element el) => Setup(name + " Resistance Shred", el,
            "Lowers " + name.ToLowerInvariant() + " resistance, by 2 points per stack on players, up to 10 stacks.",
            name + "ResShred", name + "ResistanceShred", "Shred " + name + " Resistance");

        static AilmentInfo Setup(string name, Element? el, string effect, params string[] kw) =>
            new() { Name = name, Element = el, IsDot = false, Keywords = kw, Effect = effect };

        // Map game-side text (type name, ability name, buff name) to a known
        // ailment. An exact id wins. Otherwise the name is split on separators
        // and camel case, and a keyword must be a whole token or a run of
        // whole tokens. A stat name that only contains the word (Shockwave,
        // FreezeRate, SlowRetaliation) does not match.
        public static AilmentInfo Find(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string bare = Letters(text);
            foreach (var a in All)
                foreach (var k in a.Keywords)
                    if (bare == Letters(k)) return a;

            var tokens = Tokens(text);
            if (tokens.Count == 0) return null;
            if (tokens.Exists(x => x is "chance" or "shrine" or "rate" or "multiplier" or "retaliation" or "avoidance"))
                return null;
            if (tokens.Contains("resistance") && !tokens.Contains("shred")) return null;

            var core = new List<string>();
            foreach (var tok in tokens)
                if (tok is not ("ailment" or "clone" or "stacking" or "debuff" or "buff" or "effect"))
                    core.Add(tok);
            if (core.Count == 0) return null;

            foreach (var a in All)
                foreach (var k in a.Keywords)
                    if (KeywordFits(core, Tokens(k))) return a;
            return null;
        }

        static bool KeywordFits(List<string> core, List<string> kw)
        {
            if (kw.Count == 0) return false;
            for (int i = 0; i <= core.Count - kw.Count; i++)
            {
                bool eq = true;
                for (int j = 0; j < kw.Count; j++)
                    if (core[i + j] != kw[j]) { eq = false; break; }
                if (eq && ExtrasAreElements(core, i, kw.Count)) return true;
            }
            string smashed = string.Concat(kw);
            for (int i = 0; i < core.Count; i++)
            {
                var join = new StringBuilder();
                for (int j = i; j < core.Count && join.Length < smashed.Length; j++)
                {
                    join.Append(core[j]);
                    if (join.ToString() == smashed && ExtrasAreElements(core, i, j - i + 1)) return true;
                }
            }
            return false;
        }

        static bool ExtrasAreElements(List<string> core, int start, int len)
        {
            for (int i = 0; i < core.Count; i++)
            {
                if (i >= start && i < start + len) continue;
                switch (core[i])
                {
                    case "physical": case "fire": case "cold": case "lightning":
                    case "necrotic": case "void": case "poison":
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        static string Letters(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            return sb.ToString();
        }

        static List<string> Tokens(string text)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            void Flush()
            {
                if (sb.Length == 0) return;
                list.Add(sb.ToString());
                sb.Clear();
            }
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsLetterOrDigit(c)) { Flush(); continue; }
                if (sb.Length > 0 && char.IsUpper(c)) Flush();
                sb.Append(char.ToLowerInvariant(c));
            }
            Flush();
            return list;
        }

        public static AilmentInfo ByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var a in All)
                if (string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) return a;
            return null;
        }

        // Name plus the effect line, for the death card.
        public static string CardLine(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var info = ByName(name) ?? Find(name);
            if (info == null) return name.Trim();
            return string.IsNullOrWhiteSpace(info.Effect) ? info.Name : info.Name + ": " + info.Effect;
        }
    }
}

