using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // One shared candidate set for the death card and Patterns. Missing facts
    // never become zero stats. Each group owns one problem, including its
    // debuffs and history, rather than separate cards repeating the same fix.
    internal static class AdviceRules
    {
        public static List<Advice> Evaluate(DeathRecord d, IEnumerable<DeathRecord> history, bool current = false)
        {
            if (d == null) return new();
            var tips = new Dictionary<string, Advice>(StringComparer.Ordinal);
            var resistancePriority = new Dictionary<string, (bool Killing, float Gap)>(StringComparer.Ordinal);
            void Add(string key, string group, float weight, string title, string body, bool notice = false)
            {
                var candidate = new Advice { Key = key, Group = group, Weight = weight, Title = title, Body = body, IsNotice = notice };
                if (!tips.TryGetValue(group, out var old) || weight > old.Weight)
                    tips[group] = candidate;
            }
            bool Def(string key, out float v) => FreshDefense(d, key, out v);
            var names = (d.AilmentsOnYou ?? new()).Append(d.KillingAilment)
                .Select(n => Ailments.ByName(n) ?? Ailments.Find(n)).Where(a => a != null)
                .GroupBy(a => a.Name).Select(g => g.First()).ToList();
            bool Has(string n) => names.Any(a => a.Name == n);
            var lethal = Ailments.ByName(d.KillingAilment) ?? Ailments.Find(d.KillingAilment);
            bool dotKill = lethal?.IsDot == true;
            bool dotWindow = d.WindowDamage > 0 && d.DotDamage >= d.WindowDamage * 0.5f;
            bool dots = dotKill || dotWindow || d.Kind == DeathKind.DamageOverTime;
            var recent = Recent(d, history);
            float total = ValidTotal(d.DamageByElement);
            var types = new HashSet<Element>();
            if (Elements.TryParse(d.KillingElement, out var primary)) types.Add(primary);
            if (Elements.TryParse(d.SecondaryKillingElement, out var secondary)) types.Add(secondary);
            if (lethal?.Element is Element ailmentType) types.Add(ailmentType);
            if (total > 0)
                for (int i = 0; i < Elements.Count; i++)
                    if (d.DamageByElement[i] / total >= 0.25f) types.Add((Element)i);

            // Crit protection: a critical flag and a DoT ailment conflict.
            // Reduced bonus damage and avoidance are different ways to protect.
            bool measuredHits = d.Hits > 0 && float.IsFinite(d.WindowDamage) && float.IsFinite(d.DotDamage)
                && d.DotDamage >= 0 && d.WindowDamage > d.DotDamage;
            if (d.KillingCrit == true && !dotKill && (d.Kind != DeathKind.DamageOverTime || measuredHits))
            {
                bool knownAvoid = Def("CritAvoidance", out float avoid);
                bool knownBonus = Def("ReducedBonusCritDamage", out float bonus);
                if (current && ((knownBonus && bonus >= 99.5f) || (knownAvoid && avoid >= 99.5f && !Has("Critical Vulnerability"))))
                    Add("crit_check", "crit", 25, "Your current crit protection closes this gap",
                        "The old killing blow was critical, but current stats show full " + (knownBonus && bonus >= 99.5f ? "reduced bonus damage from crits." : "critical strike avoidance.")
                        + " Keep that protection active during combat.", true);
                else if (knownBonus && bonus >= 99.5f)
                    Add("crit_check", "crit", 25, "Check the crit damage and snapshot",
                        $"Reduced bonus damage from crits was {bonus:0}%. A crit flag can still appear with this protection; the bonus should be removed. "
                        + (Has("Critical Vulnerability") ? "Critical Vulnerability was also recorded, but it does not by itself establish a gap in this bonus protection. " : "")
                        + "Check snapshot timing before adding more crit protection.", true);
                else if (Has("Critical Vulnerability"))
                    Add("crit", "crit", 100, "Protect against Critical Vulnerability",
                        "Critical Vulnerability was recorded with the critical killing blow. It lowers avoidance and raises the chance to be critically hit. Reduce repeat exposure or use ailment cleansing your build supports; full reduced bonus damage from crits protects against the bonus without relying on the avoidance roll.");
                else if (knownAvoid && avoid >= 99.5f)
                    Add("crit", "crit", 25, "Check the critical-strike snapshot",
                        "A crit was reported with full critical strike avoidance in the snapshot. Check whether a temporary buff ended or a debuff was missed. This evidence does not justify stacking above 100%.", true);
                else
                    Add("crit", "crit", 100, "Protect against critical strikes",
                        "The killing blow was a critical strike. "
                        + (knownAvoid ? $"Avoidance was {avoid:0}%; close the gap to 100%, " : "Check avoidance toward 100%, ")
                        + (knownBonus ? $"or raise reduced bonus damage from {bonus:0}% toward 100%. " : "or use reduced bonus damage from crits toward 100%. ")
                        + (knownBonus ? $"Removing the remaining crit bonus would reduce a comparable critical hit by about {100 * (1 - 1 / MitigationMath.CritHitMultiplier(bonus)):0}%, with other modifiers unchanged." : "Avoidance prevents the crit roll; reduced bonus damage removes its extra damage."));
            }

            foreach (var element in types.OrderBy(e => (int)e))
            {
                string n = Elements.Name(element), el = n.ToLowerInvariant();
                string group = "res:" + n;
                bool known = Def("Res." + n, out float have);
                string curse = Has("Curse of Aberroth")
                    ? "Curse of Aberroth lowers every resistance by 10 points per stack. There is no stack limit, and it cannot be cleansed. "
                    : "";
                string shred = curse + (Has("Marked for Death") ? "Marked for Death lowered all resistances by 25 points. "
                    : element == Element.Lightning && Has("Shock") ? "Shock lowered lightning resistance and raised stun chance. "
                    : element == Element.Poison && Has("Poison") ? "Poison also lowers poison resistance: 2 points per stack on players, for the first 30 stacks. "
                    : Has(n + " Resistance Shred") ? $"{n} Resistance Shred was recorded; it lowers this resistance by 2 points per stack on players, up to 10 stacks. " : "");
                bool killingType = Elements.TryParse(d.KillingElement, out var reportedPrimary) && reportedPrimary == element
                    || Elements.TryParse(d.SecondaryKillingElement, out var reportedSecondary) && reportedSecondary == element || lethal?.Element == element;
                string why = killingType
                    ? $"{Cap(el)} damage was recorded on the killing blow. "
                    : total > 0 ? $"{100 * d.DamageByElement[(int)element] / total:0}% of recorded health loss was attributed to {el} using the incoming damage mix. " : $"{Cap(el)} damage was recorded. ";
                if (known && have < 74.5f)
                {
                    int short_ = (int)MathF.Round(75 - have, MidpointRounding.AwayFromZero);
                    string body = why + $"You were {short_} point{(short_ == 1 ? "" : "s")} short of the 75% cap. " + shred;
                    if (Def("AreaLevel", out float area) && area > 0)
                    {
                        float ratio = MitigationMath.ResistanceRatio(have, 75, 0, MitigationMath.EnemyPenetration(area));
                        if (float.IsFinite(ratio)) body += $"At area level {area:0}, closing this gap models about {100 * (1 - ratio):0}% less {el} damage from a comparable enemy source. ";
                        // The saved snapshot is the resistance the recorded hit
                        // went through, so the exact-hit preview only applies to
                        // the original death, never to current stats.
                        if (!current && ReportedSingleTypeHit(d, element))
                            body += "For this exact reported hit: " + MitigationMath.PreviewResistance(d.KillingBlow, d.OverkillDamage, have, 75, 0, area,
                                DamageMeaning.PostMitigationWithWard, $"capping {el} resistance").Sentence() + " ";
                    }
                    body += "Resistance above cap does not counter area-level penetration.";
                    int gaps = recent.Count(r => (r.KillingElement == n || r.SecondaryKillingElement == n) && FreshDefense(r, "Res." + n, out float rres) && rres < 74.5f);
                    if (recent.Count >= 3 && gaps * 2 >= recent.Count) body += $" This same gap accompanied {gaps} of your last {recent.Count} deaths.";
                    Add("res_" + n, group, 110, $"Cap {el} resistance: you had {have:0}%", body);
                    resistancePriority["res_" + n] = (killingType, 75 - have);
                }
                else if (shred.Length > 0)
                {
                    string snapshot = known ? $"The snapshot had {have:0}% {el} resistance. " : $"{Cap(el)} resistance was not captured. ";
                    string headroom = Def("ResUncapped." + n, out float un) ? $"Uncapped resistance was {un:0}%. " : "";
                    Add("res_" + n, group, 92, $"Keep {el} resistance capped through debuffs",
                        shred + snapshot + headroom + "Check the character sheet while the debuff is active. Extra uncapped resistance can absorb resistance loss; it does not offset area-level penetration. Stack counts and exact lost resistance were not captured.");
                }
                else if (known)
                {
                    string capped = why + $"The snapshot had {have:0}% {el} resistance. No matching resistance-loss debuff was recorded.";
                    if (Def("AreaLevel", out float cappedArea) && cappedArea > 0)
                    {
                        float pen = MitigationMath.EnemyPenetration(cappedArea);
                        float effective = MitigationMath.EffectiveResistance(have, 0, pen);
                        if (float.IsFinite(pen) && float.IsFinite(effective))
                        {
                            int through = (int)Math.Round(MitigationMath.ResistanceTakenFraction(effective) * 100f);
                            capped += $" At area level {cappedArea:0}, enemy penetration is {pen:0}%. After that, this layer is about {effective:0}% effective, so about {through}% of {el} damage still gets through.";
                        }
                    }
                    Add("res_" + n, group, 20, $"{Cap(el)} resistance was already capped", capped, true);
                }
                else
                    Add("res_" + n, group, 55, $"Check {el} resistance",
                        why + $"Your {el} resistance was not captured. Check the character sheet; bring it to 75% if low. If capped, this record does not establish a resistance gap.");
            }
            if (Has("Resistance Shred"))
                Add("shred_res", "res:unmapped", 70, "Identify the resistance being shredded",
                    "Resistance Shred was recorded, but its element and stack count were not preserved. Check which resistance falls below 75% during the debuff before adding overcap to your gear.");

            // One recovery card for all DoTs. Its matching resistance is handled
            // above, so this card never asks for the same gear change again.
            var mainDot = dotKill ? lethal : names.FirstOrDefault(a => a.IsDot);
            if (dots || mainDot != null)
            {
                string key = mainDot != null ? "dot_" + mainDot.Name : "dot_sustain";
                string body = mainDot != null ? $"{mainDot.Name} was recorded. " : dotWindow
                    ? "At least half the recorded health loss was damage over time. "
                    : "This death was classified as damage over time, but its ailment was not captured. ";
                if (mainDot?.Element is Element de && Def("Res." + Elements.Name(de), out float res) && res >= 74.5f)
                    body += $"Your snapshot already had {res:0}% {Elements.Name(de).ToLowerInvariant()} resistance. ";
                body += Has("Damned") ? "Damned cuts your health regen, so use another recovery source such as leech or potions while reducing exposure. "
                    : "Improve recovery with health regen, leech or potions, and stop taking fresh applications from the source. ";
                body += "Armor does not normally reduce damage over time; dodge and block cannot stop its ticks.";
                if (Has("Frostbite")) body += " Frostbite also raises freeze chance.";
                if (Has("Time Rot")) body += " Time Rot also lengthens stuns.";
                if (Has("Doom")) body += " Doom also increases melee damage taken.";
                Add(key, "recovery", dotKill || dots ? 88 : 58, "Recover through " + (mainDot?.Name ?? "damage over time"), body);
            }
            else if (d.Kind == DeathKind.Attrition && d.Hits > 1)
                Add("sustain", "recovery", 72, "Recover between hits",
                    $"The timeline recorded {d.Hits} events across {d.WindowSeconds:0.0}s. Check recovery during combat: leech while attacking, regen between hits, and potion timing. Health on kill only helps when enemies actually die.");

            // Hits counts health-loss events, including DoT ticks. A positive
            // killing amount alone cannot turn a DoT classification into a hit.
            bool hitCause = !dots && (d.KillingBlow > 0 || d.Hits > 0);
            float physicalHits = total > 0 ? Math.Max(0, d.DamageByElement[(int)Element.Physical] - At(d.DotByElement, (int)Element.Physical)) / total : 0;
            if (hitCause && (physicalHits >= 0.25f || total <= 0 && types.Contains(Element.Physical)))
            {
                string body = "Physical hits were recorded. Armor reduces hits, and physical resistance is a separate layer. ";
                if (Def("Armor", out float armor))
                {
                    body += $"Recorded armor was {armor:N0}. ";
                    if (Def("AreaLevel", out float area) && area > 0)
                        body += $"The guide formula models {100 * MitigationMath.ArmorMitigationFraction(armor, area):0}% physical hit mitigation at area level {area:0}. ";
                    else body += "That is a rating; area level was not captured, so no mitigation percent is stated. ";
                }
                body += Has("Armor Shred") ? "Armor Shred was also recorded; reduce repeat applications or check your armor while it is active." : "Compare armor options after closing measured resistance and crit gaps.";
                Add("armor", "armor", Has("Armor Shred") ? 91 : 60, Has("Armor Shred") ? "Protect your armor from repeated shred" : "Improve physical hit mitigation", body);
            }
            else if (hitCause && Has("Armor Shred"))
                Add("shred_armor", "armor", 65, "Check armor during the shred",
                    "Armor Shred was recorded during a hit death. It lowers armor, which also mitigates non-physical hits at reduced effectiveness. Check the depleted value before choosing more armor; it is not a fix for damage-over-time ticks.");

            // A pool recommendation follows evidence, never an unknown resistance
            // or an unresolved crit gap. Reported damage is not a health-loss unit.
            bool defensesClosed = types.Count > 0 && types.All(e => Def("Res." + Elements.Name(e), out float r) && r >= 74.5f)
                && !tips.Values.Any(t => !t.IsNotice && (t.Group == "crit" || t.Group.StartsWith("res:")));
            // Same pool the death kind used. WindowDamage stays ward-inclusive and is not the threshold.
            bool observedLargeLoss = d.ClassPool > 0
                ? (d.Kind == DeathKind.OneShot || d.Kind == DeathKind.Burst) && d.Hits > 0 && d.ClassLoss >= d.ClassPool * 0.8f
                : (d.Kind == DeathKind.OneShot || d.Kind == DeathKind.Burst) && d.MaxHealth > 0 && d.Hits > 0 && d.WindowDamage >= d.MaxHealth * 0.8f;
            bool reportedLargeHit = d.MaxHealth > 0 && d.KillingBlow >= d.MaxHealth && d.OverkillDamage > 0 && !string.IsNullOrWhiteSpace(d.DetailSource);
            bool poolEvidence = !dots && defensesClosed && (observedLargeLoss || reportedLargeHit);
            if (poolEvidence)
            {
                string body = observedLargeLoss
                    ? (d.ClassPool > 0
                        ? $"The death kind compared {d.ClassLoss:N0} against a pool of {d.ClassPool:N0}. Timeline totals still include ward. "
                        : $"Recorded health loss was {d.WindowDamage:N0} against {d.MaxHealth:N0} max health at death. ")
                    : $"The game reported {d.KillingBlow:N0} damage and {d.OverkillDamage:N0} overkill against {d.MaxHealth:N0} max health at death. Report units have not been verified against health and ward. "
                      + (d.OverkillDamage < d.KillingBlow ? $"If the reported damage is the whole hit after your defenses, you were about {d.OverkillDamage:N0} health and ward short of surviving this exact hit. " : "");
                body += "The relevant resistance gaps are closed" + (current ? " in your current stats" : " in the snapshot") + "; adding more resistance alone is not the next fix. ";
                if (current && Def("MaxHealth", out float currentHealth)) body += $"Current max health is {currentHealth:N0}. ";
                body += current && Def("MaxHealth", out float improvedHealth) && improvedHealth > d.MaxHealth
                    ? "Maximum health has increased since this death. Maintain that buffer and review reliable hit mitigation before adding still more health. "
                    : "Review maximum health and reliable hit mitigation so one heavy hit or burst does not exhaust the buffer. ";
                if (Def("Ward", out float ward) && ward > 0)
                    body += $"Ward was readable at {ward:N0}, but one reading does not prove sustain. If your build already maintains ward during combat, improve its reliable minimum rather than relying on a temporary peak. ";
                body += "No guaranteed survival amount can be calculated from this record.";
                Add("ehp", "pool", observedLargeLoss ? 85 : 58, "Review your buffer after closing defensive gaps", body);
            }
            if (!dots && d.Kind == DeathKind.Burst)
            {
                Add("avoid", "avoidance", 70, "Reduce repeated hits",
                    $"{d.Hits} hit events were recorded close together. " + (d.MaxHealth > 0 ? "" : "Your max health was not captured. ")
                    + "Dodge can avoid hits; block reduces successful blocks according to effectiveness. Choose a layer your build can sustain. " + AvoidanceNumbers(d));
            }
            if (hitCause && !dots && Def("Dodge", out float dodge) && dodge <= 0 && Def("Block", out float block) && block <= 0)
                Add("avoid", "avoidance", 71, "Consider one additional hit layer",
                    "Dodge rating and block chance were both zero in the snapshot. Consider a supported dodge or block layer to reduce repeated hits; block also needs effectiveness. Neither protects against damage-over-time ticks, and chance defenses do not guarantee survival.");
            if ((d.WindowDamage > 0 || d.Kind == DeathKind.OneShot || dotKill) && Def("Endurance", out float endurance) && endurance < 59.5f)
            {
                string enduranceBody = $"Endurance was {endurance:0}%; the reduction caps at 60%. "
                    + (Def("EnduranceThreshold", out float threshold) ? $"Your endurance threshold was {threshold:N0}. " : "Check the threshold as well as the percent. ")
                    + "It reduces only damage reaching health below that threshold, including damage over time. It does not protect ward or the portion above the threshold.";
                float enduranceWeight = 65;
                // Only a reported hit with a known threshold can say whether 60%
                // endurance would have changed the outcome. A miss stays under
                // the toast threshold so it does not outrank an unknown resistance.
                if (!current && Def("EnduranceThreshold", out float knownThreshold) && d.KillingBlow > 0 && d.OverkillDamage > 0
                    && d.OverkillDamage < d.KillingBlow && !string.IsNullOrWhiteSpace(d.DetailSource))
                {
                    float wardAtHit = float.NaN;
                    if (Def("Ward", out float snapshotWard)) wardAtHit = snapshotWard;
                    var preview = MitigationMath.PreviewEndurance(d.KillingBlow, d.OverkillDamage, knownThreshold, endurance, 60,
                        DamageMeaning.PostMitigationWithWard, $"raising endurance from {endurance:0}% to 60%", wardAtHit);
                    if (float.IsFinite(preview.Ratio))
                    {
                        enduranceBody += " " + preview.Sentence();
                        if (preview.Survived == false) enduranceWeight = 40;
                    }
                }
                Add("endurance", "endurance", enduranceWeight, $"Raise endurance: you had {endurance:0}%", enduranceBody);
            }

            if (Has("Freeze"))
                Add("cc_freeze", "control", 80, "Reduce freeze vulnerability",
                    "Freeze was recorded; cold resistance does not stop freeze. Check freeze protection your build can support and avoid fresh Frostbite applications, which raise freeze chance."
                    );
            else if (Has("Chill"))
                Add("cc_chill", "control", 78, "Keep mobility while chilled",
                    "Chill was recorded and slows movement, attacks and casts. Cold resistance does not prevent chill. Check chill protection or cleansing your build supports and keep a movement skill available; do not treat extra health as chill immunity.");
            if (Has("Chill of Aberroth"))
                Add("cc_chill_aberroth", "control:aberroth-chill", 78, "Chill of Aberroth slows you",
                    "Chill of Aberroth slows movement, attacks and casts. It is not Chill, and cold resistance does not stop it. Keep a movement skill ready.");
            if (Has("Shock of Aberroth"))
                Add("cc_shock_aberroth", "control:aberroth-shock", 72, "Shock of Aberroth raises damage taken",
                    "Shock of Aberroth is 5% increased damage taken per stack. It does not lower lightning resistance. Fewer stacks mean less extra damage taken.");
            if (Has("Stun"))
                Add("cc_stun", "control:stun", 79, "Improve stun avoidance",
                    "Stun was recorded. Stun avoidance is a flat rating. " + (Def("StunAvoidance", out float stun) ? $"Your snapshot had {stun:N0} stun avoidance. " : "")
                    + (tips.ContainsKey("pool") ? "" : "A larger max health pool also makes smaller hits less able to stun. ") + "Ward is not added to max health in this rule.");
            if (Has("Slow") && !Has("Chill"))
                Add("cc_slow", "control:move", 65, "Preserve an escape while slowed",
                    "Slow was recorded. Check movement speed and the availability of your movement skill so you can leave danger before another hit or application.");
            if (Has("Shock") && !types.Contains(Element.Lightning))
                Add("cc_shock", "res:Lightning", 68, "Shock lowered your lightning resistance",
                    "Shock lowered lightning resistance and raised stun chance. Lightning damage was not attributed to this death, so check exposure and the debuffed resistance before changing gear.");

            // One behavior card: a repeated named ability is more useful than
            // a repeated attacker; boss context never invents an encounter.
            int abilities = Known(d.KillerAbility) ? recent.Count(r => Same(r.KillerAbility, d.KillerAbility) && (!Known(d.Killer) || Same(r.Killer, d.Killer))) : 0;
            int killers = Known(d.Killer) ? recent.Count(r => Same(r.Killer, d.Killer)) : 0;
            if (abilities >= 3)
                Add("repeat_ability", "pattern", 45, $"Plan around {d.KillerAbility}",
                    $"This ability appeared in {abilities} of your last {recent.Count} deaths. Review its visible cue and keep your movement skill available for it; the log cannot infer its exact attack pattern.");
            else if (killers >= 3)
                Add("nemesis", "pattern", 40, $"{d.Killer} has killed you {killers} times",
                    $"That is {killers} of your last {recent.Count} deaths on this character. Watch the attacks and save a movement skill for the dangerous cue; no particular mechanic was identified by the log.");
            else if (d.IsBossFight == true && hitCause)
                Add("boss", "pattern", 38, "Keep an escape for the boss attack",
                    "The game marked this death as a boss fight. Review the cue for the recorded attack and keep a movement skill available. The report does not establish that a particular attack is avoidable or a specific mechanic was failed.");

            if (!tips.Values.Any(t => !t.IsNotice))
                Add(d.Kind == DeathKind.Unknown ? "unknown" : "none", "unknown", 0, "No supported gear change from this record",
                    "The capture does not establish a specific defensive gap. Review the recorded cause and character sheet before changing gear; missing hit data is not evidence that every defense failed.");
            if (current)
                foreach (var tip in tips.Values)
                {
                    tip.Title = tip.Title.Replace("you had", "you have").Replace("was already capped", "is currently capped");
                    tip.Body = tip.Body.Replace("You were", "You are").Replace("The snapshot had", "Current stats have")
                        .Replace("Your snapshot already had", "Current stats already have").Replace("Your snapshot had", "Current stats have")
                        .Replace("Avoidance was", "Current avoidance is").Replace("Recorded armor was", "Current armor is")
                        .Replace("Endurance was", "Current endurance is").Replace("Your endurance threshold was", "Current endurance threshold is")
                        .Replace("Uncapped resistance was", "Current uncapped resistance is").Replace("Block chance was", "Current block chance is");
                }
            return tips.Values.OrderByDescending(t => t.Weight)
                .ThenByDescending(t => resistancePriority.TryGetValue(t.Key, out var p) && p.Killing)
                .ThenByDescending(t => resistancePriority.TryGetValue(t.Key, out var p) ? p.Gap : 0)
                .ThenBy(t => t.Key, StringComparer.Ordinal).ToList();
        }

        public static bool FreshDefense(DeathRecord d, string key, out float value)
        {
            value = 0;
            // The area is frozen context, not a buff-dependent stat. Online
            // player snapshots may omit AreaLevel while ZoneLevel is readable.
            if (key == "AreaLevel" && d?.ZoneLevel > 0) { value = d.ZoneLevel.Value; return true; }
            return d != null && (!d.DefenseSnapshotAgeSeconds.HasValue || float.IsFinite(d.DefenseSnapshotAgeSeconds.Value) && d.DefenseSnapshotAgeSeconds.Value >= 0 && d.DefenseSnapshotAgeSeconds.Value <= 2)
                && d.TryDefense(key, out value);
        }
        // A game report with a usable damage and overkill pair whose damage
        // type is this one element. Overkill must be positive: only a
        // structured report sets it, and the default 0 must never be read as
        // "you had exactly the whole hit left". Mixed-type hits cannot be split, so no
        // single-resistance exact-hit preview is made for them.
        // The next-step card shows this sentence on its own. The assumption
        // stays in the same sentence, through the following period.
        internal static string ExactHitCardLine(string body)
        {
            const string marker = "For this exact reported hit: ";
            if (string.IsNullOrEmpty(body)) return null;
            int at = body.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0) return null;
            int end = body.IndexOf('.', at);
            if (end < 0) return body.Substring(at).Trim();
            return body.Substring(at, end - at + 1).Trim();
        }
        internal static bool ReportedSingleTypeHit(DeathRecord d, Element element)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.DetailSource) || !float.IsFinite(d.KillingBlow) || d.KillingBlow <= 0
                || !float.IsFinite(d.OverkillDamage) || d.OverkillDamage <= 0 || d.OverkillDamage >= d.KillingBlow) return false;
            if (!Elements.TryParse(d.KillingElement, out var primary) || primary != element) return false;
            return string.IsNullOrWhiteSpace(d.SecondaryKillingElement)
                || Elements.TryParse(d.SecondaryKillingElement, out var secondary) && secondary == element;
        }
        static List<DeathRecord> Recent(DeathRecord d, IEnumerable<DeathRecord> history) => (history ?? Enumerable.Empty<DeathRecord>())
            .Where(r => r != null && Same(r.Character, d.Character) && r.UtcTime <= d.UtcTime)
            .OrderByDescending(r => r.UtcTime).ThenByDescending(r => r.Number).Take(20).ToList();
        static bool Same(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
        static bool Known(string s) => !string.IsNullOrWhiteSpace(s) && !Same(s, "unknown") && !Same(s, "something unseen");
        static float ValidTotal(float[] a)
        {
            if (a?.Length != Elements.Count || !a.All(v => float.IsFinite(v) && v >= 0)) return 0;
            float total = a.Sum();
            return float.IsFinite(total) ? total : 0;
        }
        static float At(float[] a, int i) => a?.Length == Elements.Count && float.IsFinite(a[i]) ? Math.Max(0, a[i]) : 0;
        static string Cap(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);
        static string AvoidanceNumbers(DeathRecord d)
        {
            var parts = new List<string>();
            if (FreshDefense(d, "AreaLevel", out float area) && area > 0)
            {
                if (FreshDefense(d, "Dodge", out float dodge) && dodge > 0)
                    parts.Add($"The standard guide curve estimates {100 * MitigationMath.DodgeChanceFraction(dodge, area):0}% dodge at area level {area:0}");
                if (FreshDefense(d, "Block", out float block) && FreshDefense(d, "BlockEffectiveness", out float effect) && block > 0 && effect > 0)
                    parts.Add($"Block chance was {block:0}%; the guide curve estimates {100 * MitigationMath.BlockMitigationFraction(effect, area):0}% reduction when a block succeeds");
            }
            return parts.Count > 0 ? string.Join(". ", parts) + ". These are standard-layer models, not proof that the fatal hit was dodged or blocked." : "Neither chance layer guarantees survival.";
        }
    }
}
