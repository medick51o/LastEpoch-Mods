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
    // Keep the advice true to Last Epoch: resistances cap at 75%, armor
    // reduces hit damage (best vs physical) but not damage over time,
    // endurance cuts damage taken below the endurance threshold, 100%
    // critical strike avoidance makes you immune to enemy crits.
    public static class Advisor
    {
        public const int MaxTips = 5;

        public static List<Advice> Suggest(DeathRecord d, IEnumerable<DeathRecord> history = null)
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
                        "Endurance cuts the damage you take while your health is below the endurance threshold, which turns lethal hits into survivable ones.");
                    if (d.KillingCrit == true)
                        Add("crit", 95f, "Get critical strike avoidance to 100%",
                            "The killing blow was a critical strike. At 100% critical strike avoidance enemies cannot crit you at all.");
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
                            "Keep cold resistance capped and look for freeze immunity or chill avoidance on gear. Save a movement skill to break away.");
                        break;
                    case "Stun":
                        Add("cc_stun", 65f, "Get stun avoidance", "You were stunned in the fight that killed you. Stun avoidance and a bigger health pool keep you acting.");
                        break;
                    case "Shock":
                        Add("cc_shock", 60f, "Shock made you fragile", "Shocked targets take more damage and are stunned more easily. Keep lightning resistance capped.");
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
                            string extra = a.Name == "Bleed" ? " Armor does not reduce bleed." :
                                           a.Name == "Poison" ? " Each poison stack also lowers your poison resistance." : "";
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

            return tips.Values.OrderByDescending(t => t.Weight).Take(MaxTips).ToList();
        }

        static string Pct(float f) => $"{System.Math.Clamp(f, 0f, 9.99f) * 100f:0}%";
    }
}
