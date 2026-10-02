using System;
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
        public const int MaxShown = 3;

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
                    // The window split is the game's pre-mitigation mix, rescaled
                    // onto health lost (HitEvent.AddTo). It is not the mitigated
                    // split of the killing blow, so the sentence says so.
                    string why = $"Recorded health lost was {Pct(share)} {Elements.Name(el).ToLowerInvariant()} on the pre-mitigation mix.";
                    if (el == Element.Physical)
                    {
                        // Armor only helps against the HIT part; bleed and
                        // other physical DoT still use physical resistance.
                        float dot = d.DotByElement?.Length == Elements.Count ? d.DotByElement[i] : 0f;
                        float hitShare = (d.DamageByElement[i] - dot) / total;
                        if (hitShare >= 0.25f)
                            Add("armor", 50f + 40f * hitShare, "Stack armor",
                                $"{Pct(hitShare)} of recorded health lost was physical hits, on the pre-mitigation mix. Armor reduces those hits.{ArmorNote(d)}");
                    }
                    ResTip(Elements.Name(el), w, why);
                }
            }
            else if (Elements.TryParse(d.KillingElement, out var ke))
            {
                var ail = Ailments.ByName(d.KillingAilment) ?? Ailments.Find(d.KillingAilment);
                if (ke == Element.Physical && ail?.IsDot != true)
                    Add("armor", 60f, "Stack armor", "The killing blow was a physical hit. Armor reduces physical hits." + ArmorNote(d));
                ResTip(Elements.Name(ke), 60f, $"The killing blow was {Elements.Name(ke).ToLowerInvariant()} damage.");
            }

            // With a snapshot of your defences the tip names your number; a
            // resistance already at cap stops being the top fix.
            void ResTip(string n, float w, string why)
            {
                string el = n.ToLowerInvariant();
                if (!d.TryDefense("Res." + n, out float have))
                    Add("res_" + n, w, $"Cap {el} resistance (75%)",
                        $"{why} Check your character sheet: every point below 75% is damage you are taking for free.");
                else if (have >= DefenseSnapshot.ResCap - 0.5f)
                {
                    string over = d.TryDefense("ResUncapped." + n, out float un) && un > DefenseSnapshot.ResCap + 0.5f
                        ? $" ({un:0}% before the cap, which is headroom against shred)" : "";
                    Add("res_" + n, 30f, $"{Cap(el)} resistance was already capped",
                        $"{why} You were at {have:0}%{over}, so it got through a capped resistance: more health, ward and endurance are the next layer.");
                }
                else
                {
                    // Weighted a little above the generic tip: a known gap is the most concrete fix there is.
                    int gap = (int)MathF.Round(DefenseSnapshot.ResCap - have);
                    Add("res_" + n, w + 5f, $"Cap {el} resistance: you had {have:0}%",
                        $"{why} You were {gap} point{(gap == 1 ? "" : "s")} short of the 75% cap; every one of them is damage you took for free.");
                }
            }

            // 2. How you died → the right kind of defence.
            switch (d.Kind)
            {
                case DeathKind.OneShot:
                    Add("ehp", 85f, "Raise your effective health pool",
                        d.MaxHealth > 0
                            ? $"The last recorded hit removed {Pct(HealthRemoved(d) / d.MaxHealth)} of your max health. More health, ward, and a higher endurance threshold are what let you survive the next one."
                            : "One hit was recorded and max health was not, so this is not a share of your life and not a confirmed one-shot. More health, ward, and a higher endurance threshold are what let you survive a large hit.");
                    Add("endurance", 55f, "Get endurance and endurance threshold", EnduranceBody(d));
                    if (d.KillingCrit == true)
                        Add("crit", 95f, "Get critical strike avoidance to 100%",
                            "The killing blow was a critical strike, which hits for double. "
                            + (d.TryDefense("CritAvoidance", out float ca) && ca < 99.5f ? $"You had {ca:0}% critical strike avoidance; at 100% " : "At 100% critical strike avoidance ")
                            + "enemies cannot crit you at all.");
                    break;

                case DeathKind.Burst:
                    Add("avoid", 70f, "Avoid hits: dodge and block",
                        d.MaxHealth > 0
                            ? $"Recorded hits in the burst removed {Pct(d.WindowDamage / d.MaxHealth)} of your max health. Dodge rating and block chance (with block effectiveness) stop hits before they land."
                            : "Several hits landed close together, and max health was not recorded, so this is not a share of your life. Dodge rating and block chance (with block effectiveness) stop hits before they land.");
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
                            "More max health and ward lower your chance to be frozen, and Frostbite stacks raise it. Cold resistance does not stop freeze or chill. Save a movement skill to break away.");
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

        // What the panel should show: at most three tips that ask for a change.
        // "Already capped" is a fact, not an action, so it does not take a slot.
        // Suggest() still returns it for the log and for tests.
        public static List<Advice> Show(DeathRecord d, IEnumerable<DeathRecord> history = null)
        {
            var all = Suggest(d, history);
            var shown = all.Where(t => !IsCappedNotice(t)).Take(MaxShown).ToList();
            if (shown.Count > 0) return shown;
            if (all.Any(IsCappedNotice))
                return new List<Advice>
                {
                    new()
                    {
                        Key = "ehp", Weight = 40f,
                        Title = "Raise health, ward, and endurance",
                        Body = "The resistance on the killing blow was already at the 75% cap. The next layer is more health, more ward, and endurance on the damage that lands below the endurance threshold. Endurance does not apply to ward.",
                    },
                };
            return new List<Advice>
            {
                new()
                {
                    Key = "none", Weight = 0f,
                    Title = "No supported change from this record",
                    Body = "The recorded facts do not support a specific fix. Nothing here is a measured gap.",
                },
            };
        }

        // One sentence, only from fields that were actually recorded.
        public static string Quote(DeathRecord d)
        {
            if (d == null) return "The killing blow was not recorded.";
            string what = FirstText(d.KillerAbility, d.KillingAilment);
            string who = FirstText(d.Killer);
            bool typed = FirstText(d.KillingElement, d.SecondaryKillingElement) != null;
            bool numbered = d.KillingBlow > 0f;
            if (what == null && who == null && !typed && !numbered && d.KillingCrit != true)
                return "The killing blow was not recorded.";

            string lead = what != null && who != null ? $"Killed by {what} from {who}"
                : what != null ? $"Killed by {what}"
                : who != null ? $"Killed by {who}"
                : "Killing blow";
            if (numbered)
            {
                string types = TypePhrase(d);
                lead += types != null ? $", {Num(d.KillingBlow)} {types} damage" : $", {Num(d.KillingBlow)} damage";
            }
            else if (typed)
            {
                string types = TypePhrase(d);
                if (types != null) lead += $", {types} damage";
            }
            if (d.KillingCrit == true) lead += ", crit";
            if (d.OverkillDamage > 0f) lead += $", {Num(d.OverkillDamage)} overkill";
            return lead + ".";
        }

        // High needs the game report, a hit timeline, and max health.
        // Anything less says which of those is missing.
        public static string Confidence(DeathRecord d)
        {
            if (d == null) return "Low: no hit timeline, max health unknown";
            bool report = !string.IsNullOrWhiteSpace(d.DetailSource) || d.Kind == DeathKind.Reported;
            bool timeline = d.Hits > 0;
            bool maxHp = d.MaxHealth > 0f;
            if (report && timeline && maxHp) return "High: game death report, hit timeline, and max health";
            if (report && timeline) return "Medium: game death report and hit timeline, max health unknown";
            if (report && maxHp) return "Medium: game death report and max health, no hit timeline";
            if (report) return "Medium: game death report, no hit timeline, max health unknown";
            if (timeline && maxHp) return "Medium: hit timeline and max health, no game death report";
            if (timeline) return "Low: hit timeline, max health unknown";
            if (maxHp) return "Low: max health only, no hit timeline";
            return "Low: no hit timeline, max health unknown";
        }

        static bool IsCappedNotice(Advice t) =>
            t?.Title != null && t.Title.IndexOf("already capped", StringComparison.OrdinalIgnoreCase) >= 0;

        static string EnduranceBody(DeathRecord d)
        {
            string have = "";
            if (d.TryDefense("Endurance", out float en))
                have = $" Endurance was {en:0}%.";
            if (d.TryDefense("EnduranceThreshold", out float th))
                have += $" Endurance threshold was {Num(th)}.";
            return "Endurance cuts the part of any damage, hit or damage over time, that lands below your endurance threshold. It does not apply to ward. The reduction caps at 60%." + have;
        }

        static string ArmorNote(DeathRecord d) =>
            d.TryDefense("Armor", out float ar)
                ? $" Recorded armor was {Num(ar)}. That is a rating; mitigation also depends on area level, so no mitigation percent is stated here."
                : "";

        // Health the killing blow removed. A game report's damage includes
        // overkill past 0 health, so overkill is taken back out. Ward that
        // was lost earlier is not in this number.
        static float HealthRemoved(DeathRecord d)
        {
            if (!string.IsNullOrWhiteSpace(d.DetailSource) && d.OverkillDamage > 0f && d.KillingBlow > d.OverkillDamage)
                return d.KillingBlow - d.OverkillDamage;
            return d.KillingBlow;
        }

        static string TypePhrase(DeathRecord d)
        {
            string a = FirstText(d.KillingElement);
            string b = FirstText(d.SecondaryKillingElement);
            if (a != null && b != null && !string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return a.ToLowerInvariant() + " and " + b.ToLowerInvariant();
            return (a ?? b)?.ToLowerInvariant();
        }

        static string FirstText(params string[] parts)
        {
            if (parts == null) return null;
            foreach (var p in parts)
                if (!string.IsNullOrWhiteSpace(p)) return p.Trim();
            return null;
        }

        static string Num(float v) =>
            MathF.Round(v).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

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
                "cc_freeze"   => $"You were frozen or chilled in {of}. More max health and ward make you harder to freeze. Cold resistance does not stop freeze or chill.",
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
