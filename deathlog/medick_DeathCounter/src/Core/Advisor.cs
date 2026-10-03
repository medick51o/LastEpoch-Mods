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
        public string Group;    // one recommendation per defensive problem
        public bool IsNotice;   // recorded fact, excluded from action cards
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
            => AdviceRules.Evaluate(d, history).Take(Math.Max(0, max)).ToList();

        public static List<Advice> Show(DeathRecord d, IEnumerable<DeathRecord> history = null)
            => AdviceRules.Evaluate(d, history).Where(t => !t.IsNotice).Take(MaxShown).ToList();
        // The death toast names one step only when this record supports a
        // concrete defensive change. Low-weight checks and "already capped"
        // facts stay in the log, where their caveats are shown.
        public const float ToastMinWeight = 50f;
        public static string ToastStep(DeathRecord d)
        {
            var top = AdviceRules.Evaluate(d, null).FirstOrDefault(t => !t.IsNotice);
            return top != null && top.Weight >= ToastMinWeight && !string.IsNullOrWhiteSpace(top.Title) ? top.Title.Trim() : null;
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

        // Confidence is capture coverage, never a promise of survival.
        public static string Confidence(DeathRecord d)
        {
            if (d == null) return "Low: no death capture";
            bool report = !string.IsNullOrWhiteSpace(d.DetailSource) || d.Kind == DeathKind.Reported;
            bool timeline = d.Hits > 0;
            bool maxHp = d.MaxHealth > 0f;
            bool hasDefense = d.Defenses?.Any(kv => float.IsFinite(kv.Value)) == true;
            bool fresh = d.DefenseSnapshotAgeSeconds is float age && float.IsFinite(age) && age >= 0 && age <= 2;
            var types = new[] { d.KillingElement, d.SecondaryKillingElement }.Select(s => Elements.TryParse(s, out var e) ? Elements.Name(e) : null).Where(s => s != null).Distinct().ToList();
            bool relevant = types.Count > 0 && types.All(s => AdviceRules.FreshDefense(d, "Res." + s, out _))
                && (d.KillingCrit != true || AdviceRules.FreshDefense(d, "CritAvoidance", out _) || AdviceRules.FreshDefense(d, "ReducedBonusCritDamage", out _));
            string conflict = Conflict(d);
            string level = conflict != null ? "Low" : report && timeline && maxHp && relevant && fresh && d.KillingCrit.HasValue ? "High"
                : (report && (maxHp || hasDefense || timeline)) || (timeline && (maxHp || hasDefense)) ? "Medium" : "Low";
            var facts = new List<string> { report ? "game report" : "no game report", timeline ? $"{d.Hits} health-loss events" : "no hit timeline" };
            facts.Add(!hasDefense ? "defenses missing" : fresh ? (relevant ? "relevant defenses within 2s" : "partial defenses within 2s") : d.DefenseSnapshotAgeSeconds.HasValue ? "stale defense snapshot" : "defense timing unknown");
            if (!maxHp) facts.Add("max health unknown");
            facts.Add(d.KillingCrit.HasValue ? "crit flag known" : "crit unknown");
            if (conflict != null) facts.Add(conflict);
            return level + ": " + string.Join(", ", facts);
        }

        static string Conflict(DeathRecord d)
        {
            if (d.KillingCrit != true) return null;
            if ((Ailments.ByName(d.KillingAilment) ?? Ailments.Find(d.KillingAilment))?.IsDot == true)
                return "crit flag conflicts with damage over time";
            bool vulnerability = (d.AilmentsOnYou ?? new()).Any(n => (Ailments.ByName(n) ?? Ailments.Find(n))?.Name == "Critical Vulnerability");
            if (!vulnerability && AdviceRules.FreshDefense(d, "CritAvoidance", out float avoid) && avoid >= 99.5f)
                return "crit conflicts with snapshot avoidance";
            return null;
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
                return $"{Cap(el)} damage was recorded in {of}. Check the resistance snapshots for those deaths before changing gear. Above-cap resistance does not counter area-level penetration.";
            }
            if (a.Key.StartsWith("dot_") && a.Key != "dot_sustain")
                return $"{a.Key.Substring(4)} was recorded in {of}. Improve recovery during its damage over time and reduce further applications. Armor, dodge and block are not the usual tick defenses.";
            return a.Key switch
            {
                "armor"       => $"Physical hits were a large part of {of}. Armor reduces hits and physical resistance is a separate layer; close resistance and crit gaps first.",
                "crit"        => $"Crit protection warranted review in {of}. Compare critical strike avoidance, reduced bonus damage from crits and any recorded Critical Vulnerability before changing gear.",
                "ehp"         => $"Large recorded loss or reported overkill accompanied closed defensive gaps in {of}. Review maximum health and reliable hit mitigation; no guaranteed survival amount was calculated.",
                "endurance"   => $"An endurance gap was recorded in {of}. The reduction caps at 60% and protects health below the threshold, including damage over time. It does not protect ward.",
                "avoid"       => $"A hit-defense layer warranted review in {of}. Dodge avoids hits; block reduces successful blocks according to effectiveness. Neither stops damage-over-time ticks.",
                "sustain"     => $"You were worn down over several seconds in {of}. Health regen, leech and potion upkeep matter most there.",
                "dot_sustain" => $"Damage over time did most of the work in {of}. Health regen, leech and reducing further exposure to the recorded source can help.",
                "bleed_armor" => $"Physical damage over time (bleed) hit you in {of}. Armor does not reduce it; physical resistance, regen and leech do.",
                "recovery"    => $"Recovery warranted review in {of}. Check the recorded ailments: Damned reduces regen, while damage-over-time deaths call for sustained recovery and fewer fresh applications.",
                "cc_freeze"   => $"Freeze was recorded in {of}. Check supported freeze protection and reduce fresh Frostbite applications. Cold resistance does not stop freeze.",
                "cc_chill"    => $"Chill was recorded in {of}. Check supported chill protection or cleansing and keep an escape available. Cold resistance does not prevent chill.",
                "cc_stun"     => $"You were stunned in {of}. Stun avoidance keeps you acting.",
                "cc_shock"    => $"Shock lowered your lightning resistance in {of}. Lightning resistance above 75% absorbs that loss, and stun avoidance limits the extra stun chance.",
                "cc_slow"     => $"You were slowed in {of}. Movement speed and a short-cooldown movement skill help you walk out.",
                "shred_armor" => $"Your armor was being shredded in {of}. Limit repeat applications and check armor while the shred is active.",
                "shred_res"   => $"A resistance was being shredded in {of}. Resistance above 75% absorbs shred; limit repeat applications.",
                _             => a.Body,
            };
        }

        // A pattern spans several deaths, so one death's number never heads it.
        public static string PatternTitle(Advice a)
        {
            string title = a?.Title ?? "";
            int cut = title.IndexOf(": you ha", StringComparison.Ordinal);
            return cut > 0 ? title.Substring(0, cut) : title;
        }

        static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);


    }
}

