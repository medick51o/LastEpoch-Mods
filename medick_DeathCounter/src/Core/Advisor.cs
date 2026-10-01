using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    public sealed class Advice
    {
        public string Key;      // dedupe id
        public string Title;    // short imperative ("Cap fire resistance")
        public string Body;     // one or two sentences of why
        public float  Weight;   // higher = shown first
    }

    // "How do I not die to that again?" Rules read the raw DeathRecord, so
    // tuning advice here improves every death already in the log.
    //
    // Keep the advice true to Last Epoch (docs/RESEARCH-damage-and-defenses.md):
    // resistances cap at 75%; armor reduces all hits but is only 70% as
    // effective against non-physical, and does not normally reduce damage
    // over time; endurance cuts the part of damage that lands below the
    // endurance threshold; 100% critical strike avoidance means no enemy
    // crits; Shock lowers lightning resistance and raises stun chance (it
    // is NOT a general damage-taken debuff).
    public static class Advisor
    {
        public const int MaxTips = 5;

        public static List<Advice> Suggest(DeathRecord d, IEnumerable<DeathRecord> history = null, int max = MaxTips)
        {
            var tips = new Dictionary<string, Advice>();
            void Add(string key, float w, string title, string body)
            {
                if (tips.TryGetValue(key, out var have)) { if (w > have.Weight) have.Weight = w; return; }
                tips[key] = new Advice { Key = key, Weight = w, Title = title, Body = body };
            }

            if (d == null) return new List<Advice>();

            float total = d.DamageByElement?.Sum() ?? 0f;

            // 1. What the damage was made of → the matching defence.
            if (total > 0f)
            {
                for (int i = 0; i < Elements.Count; i++)
                {
                    float share = d.DamageByElement[i] / total;
                    if (share < 0.25f) continue;
                    var el = (Element)i;
                    float w = 50f + 40f * share;
                    if (el == Element.Physical)
                    {
                        // Armor only helps against the HIT part; bleed and
                        // other physical DoT get their own tip below.
                        float dot = d.DotByElement?.Length == Elements.Count ? d.DotByElement[i] : 0f;
                        float hitShare = (d.DamageByElement[i] - dot) / total;
                        if (hitShare >= 0.25f)
                            Add("armor", 50f + 40f * hitShare, "Stack armor",
                                $"{Pct(hitShare)} of the damage that killed you was physical hits. Armor is the main defence against those; physical resistance on gear helps too.");
                    }
                    else
                    {
                        string n = Elements.Name(el);
                        Add("res_" + n, w, $"Cap {n.ToLowerInvariant()} resistance (75%)",
                            $"{Pct(share)} of the damage that killed you was {n.ToLowerInvariant()}. Check your character sheet: every point below 75% is damage you are taking for free.");
                    }
                }
            }
            else if (Elements.TryParse(d.KillingElement, out var ke))
            {
                if (ke == Element.Physical)
                    Add("armor", 60f, "Stack armor", "The killing blow was physical. Armor is the main defence against physical hits.");
                else
                    Add("res_" + Elements.Name(ke), 60f, $"Cap {Elements.Name(ke).ToLowerInvariant()} resistance (75%)",
                        $"The killing blow was {Elements.Name(ke).ToLowerInvariant()} damage.");
            }

            // 2. How you died → the right kind of defence.
            switch (d.Kind)
            {
                case DeathKind.OneShot:
                    Add("ehp", 85f, "Raise your effective health pool",
                        $"One hit took {(d.MaxHealth > 0 ? Pct(d.KillingBlow / d.MaxHealth) + " of your life" : "you out")}. More health, ward, and a higher endurance threshold are what let you survive the next one.");
                    Add("endurance", 55f, "Get endurance and endurance threshold",
                        "Endurance cuts the part of any hit that lands below your endurance threshold, which turns lethal hits into survivable ones. The threshold affix rolls on belts.");
                    if (d.KillingCrit == true)
                        Add("crit", 95f, "Get critical strike avoidance to 100%",
                            "The killing blow was a critical strike, which hits for double. At 100% critical strike avoidance enemies cannot crit you at all.");
                    break;

                case DeathKind.Burst:
                    Add("avoid", 70f, "Avoid hits: dodge and block",
                        $"You took {Pct(d.MaxHealth > 0 ? d.WindowDamage / d.MaxHealth : 1f)} of your life in a few seconds. Dodge rating and block chance (with block effectiveness) stop hits before they land.");
                    Add("ehp", 60f, "Raise your effective health pool",
                        "More health and ward buy the second you need to react or use a potion.");
                    break;

                case DeathKind.DamageOverTime:
                    Add("dot_sustain", 75f, "Out-heal damage over time",
                        "Most of the damage was over time. Health regen and leech keep pace with it; moving out of the ground effect or away from the source stops the stacks.");
                    if (d.DotByElement?.Length == Elements.Count && d.DotByElement[(int)Element.Physical] / System.Math.Max(1f, total) >= 0.25f)
                        Add("bleed_armor", 72f, "Armor will not save you from bleed",
                            "Armor does not reduce damage over time. Physical resistance, health regen and leech do.");
                    break;

                case DeathKind.Attrition:
                    Add("sustain", 70f, "Improve sustain",
                        $"You were worn down over {d.WindowSeconds:0} seconds. Health regen, leech, health on kill and potion upkeep matter more than raw health here.");
                    Add("avoid", 50f, "Avoid hits: dodge and block",
                        "Fewer hits landing means less to heal back.");
                    break;

                case DeathKind.Unknown:
                    Add("unknown", 10f, "No hit data for this death",
                        "The mod did not see the hits that killed you, so it cannot tell what to fix. Enable ProbeApi in UserData/medick_DeathCounter.cfg and share the log so the hooks can be updated.");
                    break;
            }

            // 3. Ailments that were on you.
            foreach (var name in d.AilmentsOnYou ?? new List<string>())
            {
                var a = Ailments.ByName(name) ?? Ailments.Find(name);
                if (a == null) continue;
                bool killing = string.Equals(a.Name, d.KillingAilment, System.StringComparison.OrdinalIgnoreCase);
                switch (a.Name)
                {
                    case "Freeze":
                    case "Chill":
                        Add("cc_freeze", killing ? 80f : 65f, $"You were {(a.Name == "Freeze" ? "frozen" : "chilled")}",
                            "More max health and ward lower your chance to be frozen, and Frostbite stacks raise it. Keep cold resistance capped and save a movement skill to break away.");
                        break;
                    case "Stun":
                        Add("cc_stun", 65f, "Get stun avoidance", "You were stunned in the fight that killed you. Stun avoidance and a bigger health pool keep you acting.");
                        break;
                    case "Shock":
                        Add("cc_shock", 60f, "Shock lowered your lightning resistance", "Each Shock stack lowers your lightning resistance and makes you easier to stun. Overcap lightning resistance and get stun avoidance.");
                        break;
                    case "Slow":
                        Add("cc_slow", 45f, "You were slowed", "Slows stop you walking out of danger. Movement speed and a movement skill on a short cooldown help.");
                        break;
                    case "Armor Shred":
                        Add("shred_armor", 60f, "Your armor was being shredded", "Kill the shredding enemy first, or carry more armor so the shred stacks matter less.");
                        break;
                    case "Resistance Shred":
                        Add("shred_res", 60f, "Your resistances were being shredded", "Shred pushes resistances below cap. Overcap where you can and kill the shredder first.");
                        break;
                    default:
                        if (a.IsDot && a.Element.HasValue)
                        {
                            string el = Elements.Name(a.Element.Value).ToLowerInvariant();
                            string extra = a.Name switch
                            {
                                "Bleed"     => " Armor does not reduce bleed.",
                                "Poison"    => " Each poison stack also lowers your poison resistance.",
                                "Damned"    => " It also cuts your health regen, so lean on leech.",
                                "Doom"      => " It also makes you take more melee damage.",
                                "Time Rot"  => " It also makes stuns on you last longer.",
                                "Frostbite" => " It also makes you easier to freeze.",
                                _           => "",
                            };
                            Add("dot_" + a.Name, killing ? 88f : 58f, $"{a.Name}: {el} resistance",
                                $"{a.Name} deals {el} damage over time.{extra} Cap {el} resistance and keep health regen or leech up.");
                        }
                        break;
                }
            }

            // 4. The one that keeps getting you.
            if (history != null && !string.IsNullOrEmpty(d.Killer))
            {
                int times = history.Count(h => h != null && h.Killer == d.Killer);
                if (times >= 3)
                    Add("nemesis", 40f, $"{d.Killer} has killed you {times} times",
                        "That is a pattern, not bad luck. Watch for its wind-up and keep your movement skill ready when it shows up.");
            }

            return tips.Values.OrderByDescending(t => t.Weight).Take(max).ToList();
        }

        // The same tip, worded for a pattern across n of m deaths rather than
        // for one death (the Patterns tab). Falls back to the tip's own body.
        public static string PatternBody(Advice a, int n, int m)
        {
            string of = $"{n} of {m} deaths";
            if (a.Key.StartsWith("res_"))
            {
                string el = a.Key.Substring(4).ToLowerInvariant();
                return $"{Cap(el)} damage was a big part of {of}. Every point of {el} resistance below 75% is damage you take for free.";
            }
            if (a.Key.StartsWith("dot_") && a.Key != "dot_sustain")
                return $"{a.Key.Substring(4)} was on you in {of}. {Ailments.ByName(a.Key.Substring(4))?.Effect} Cap the matching resistance and keep health regen or leech up.";
            return a.Key switch
            {
                "armor"       => $"Physical hits were a big part of {of}. Armor is the main defence against them.",
                "crit"        => $"The killing blow was a critical strike in {of}. At 100% critical strike avoidance enemies cannot crit you.",
                "ehp"         => $"Big hits took most of your life in {of}. More health, ward and endurance threshold are what let you live through them.",
                "endurance"   => $"Endurance cuts the part of a hit below your endurance threshold; it would have softened {of}.",
                "avoid"       => $"You were taking many hits fast in {of}. Dodge rating and block chance stop hits before they land.",
                "sustain"     => $"You were worn down over several seconds in {of}. Health regen, leech and potion upkeep matter most there.",
                "dot_sustain" => $"Damage over time did most of the work in {of}. Health regen, leech and stepping out of the ground effect beat it.",
                "bleed_armor" => $"Physical damage over time (bleed) hit you in {of}. Armor does not reduce it; physical resistance, regen and leech do.",
                "cc_freeze"   => $"You were frozen or chilled in {of}. More max health and ward make you harder to freeze; keep cold resistance capped.",
                "cc_stun"     => $"You were stunned in {of}. Stun avoidance keeps you acting.",
                "cc_shock"    => $"Shock lowered your lightning resistance in {of}. Overcap lightning resistance and get stun avoidance.",
                "cc_slow"     => $"You were slowed in {of}. Movement speed and a short-cooldown movement skill help you walk out.",
                "shred_armor" => $"Your armor was being shredded in {of}. Kill shredders first, or carry more armor.",
                "shred_res"   => $"Your resistances were being shredded in {of}. Overcap where you can and kill shredders first.",
                _             => a.Body,
            };
        }

        static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        static string Pct(float f) => $"{System.Math.Clamp(f, 0f, 9.99f) * 100f:0}%";
    }
}
