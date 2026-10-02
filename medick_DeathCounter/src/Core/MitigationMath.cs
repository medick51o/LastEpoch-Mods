using System;

namespace medick_DeathCounter.Core
{
    // What the recorded killing-blow number means. The game's death report
    // (PlayerActorSync.ReceiveDetailedDeathInfo) hands us one damage number
    // and one overkill number without documenting them. The 1.1.3 patch notes
    // describe them as "how much damage was dealt by the killing blow" and
    // "how much damage was dealt beyond what was necessary to reduce the
    // player to 0 health". Community checks of the death screen (forum,
    // 2025) found displayed damage = health lost + ward lost, i.e. the full
    // post-mitigation hit. That is our primary interpretation, but it is NOT
    // officially confirmed, so every survival claim is flagged with the
    // meaning it assumed and the wording says "likely" / "may or may not".
    public enum DamageMeaning
    {
        // damage = the whole post-mitigation hit, ward included.
        // overkill = damage beyond ward + health you had at that moment.
        // Only under this meaning can we say whether a smaller hit survives.
        PostMitigationWithWard,
        // damage = post-mitigation damage that reached health, ward unknown.
        PostMitigationHealthOnly,
        // damage = the raw hit before resistances/armor/endurance.
        // No survival claim is possible from it.
        PreMitigation
    }

    // How a single percent stat was recorded when its scale is uncertain.
    public enum PercentScale
    {
        Percent,   // 41 means 41%
        Fraction,  // 0.41 means 41%
        Unknown    // whole small value: could be either, refuse to guess
    }

    // The result of one "what if" calculation: how a recorded hit changes if
    // one defensive lever had been different. Ratios hold under every damage
    // meaning; survival only under PostMitigationWithWard.
    public sealed class Counterfactual
    {
        public enum LeverKind { ShrinksTheHit, GrowsThePool }

        public string Lever;            // "fire resistance 41% to 75%"
        public LeverKind Kind = LeverKind.ShrinksTheHit;
        public float Ratio;             // newDamage / recordedDamage
        public float RecordedDamage;    // NaN when the report had no number
        public float NewDamage;         // NaN without a recorded number
        public float RemainingEffectiveHealth; // ward + health left at the hit; NaN when unknown
        public float AddedPool;           // pool lever only: health + ward added
        public bool? Survived;          // null = cannot be known
        public DamageMeaning Meaning;   // meaning the survival claim assumed
        public string Assumptions;      // plain language, one per line
        public string Note;             // why something is unknown, when it is

        public const float BorderlineBand = 0.10f; // within 10% of remaining = "may or may not"

        // The honest sentence. Never promises the gear change would have
        // prevented the death unless survival is computed AND clear of the
        // borderline band, and even then it carries the interpretation.
        public string Sentence()
        {
            if (!float.IsFinite(Ratio) || Ratio < 0f)
                return Note ?? "not enough recorded data to compute this";
            string lever = string.IsNullOrEmpty(Lever) ? "This change" : Lever;
            if (Kind == LeverKind.GrowsThePool)
                return PoolSentence(lever);
            if (Ratio > 1f)
            {
                int up = (int)Math.Round((Ratio - 1f) * 100f);
                return $"{lever} would have made this hit about {up}% bigger, so it is not a fix.";
            }
            int pct = (int)Math.Round((1f - Ratio) * 100f);
            string lever0 = string.IsNullOrEmpty(Lever) ? "This change" : Lever;
            if (!float.IsFinite(RecordedDamage) || RecordedDamage <= 0f)
                return $"{lever0} would have reduced this hit by about {pct}%.";
            int nd = (int)Math.Round(NewDamage / 10f) * 10;
            if (!float.IsFinite(RemainingEffectiveHealth))
                return $"{lever0} would have reduced this hit from about {Round0(RecordedDamage)} to about {nd}, which may or may not have been enough.";
            int rem = (int)Math.Round(RemainingEffectiveHealth / 10f) * 10;
            bool? s = Survived;
            if (s == null)
                return $"{lever0} would have reduced this hit from about {Round0(RecordedDamage)} to about {nd}, which may or may not have been enough.";
            if (Math.Abs(NewDamage - RemainingEffectiveHealth) <= BorderlineBand * Math.Max(1f, RemainingEffectiveHealth))
                return $"{lever0} would have reduced this hit from about {Round0(RecordedDamage)} to about {nd}, about equal to the about {rem} you had left, so it may or may not have been enough.";
            if (s == true)
                return $"{lever0} would have reduced this hit from about {Round0(RecordedDamage)} to about {nd}, below the about {rem} you had left, so you would likely have survived, assuming the recorded damage was the whole post-mitigation hit including ward.";
            return $"{lever0} would have reduced this hit from about {Round0(RecordedDamage)} to about {nd}, still above the about {rem} you had left, so it would probably still have killed you.";
        }

        string PoolSentence(string lever)
        {
            float over = RecordedDamage - RemainingEffectiveHealth;
            if (!float.IsFinite(over) || over < 0f)
                return Note ?? "the shortfall is unknown, so pool advice cannot be quantified";
            int o = (int)Math.Round(over / 10f) * 10;
            bool? s = Survived;
            if (s == null)
                return $"You were short by about {o}. {lever} may or may not have been enough.";
            if (s == true)
            {
                if (Math.Abs(AddedPool - over) <= BorderlineBand * Math.Max(1f, over))
                    return $"You were short by about {o}: {lever} is about equal to the shortfall, so it may or may not have been enough.";
                return $"You were short by about {o}: {lever} would have absorbed this exact hit.";
            }
            return $"You were short by about {o}: {lever} would not have been enough on its own.";
        }

        static int Round0(float f) => (int)Math.Round(f);
    }

    // Pure counterfactual math for death advice. No game references: every
    // input is a number the death record already carries, or an assumption
    // the caller states. Sources are dated in research/advice/03-MATH.md;
    // headline ones:
    //   resistances: cap 75, shred before cap, penetration after cap,
    //     1% enemy penetration per area level up to 75% (official support
    //     articles, Resistances July 14 2026, Penetrations Feb 24 2026)
    //   armor: hits only, 70% as effective vs non-physical, cap 85%, scales
    //     with area level (official support article, July 13 2026); the
    //     curve below is Tunklab's, verified against three build-planner
    //     data points at area level 100 (Oct 2 2026)
    //   endurance: base 20%, cap 60%, threshold base 20% of max health,
    //     applies below the threshold only, to health not ward, to hits and
    //     damage over time (official support article, Feb 13 2026)
    //   enemy crits: 200% multiplier, avoidance turns crits into normal
    //     hits after the crit roll, reduced bonus crit damage caps at 100%
    //     (official support article, Feb 13 2026)
    public static class MitigationMath
    {
        // ── Constants (percent points unless stated) ─────────────
        public const float ResistanceCap = 75f;
        public const float PenetrationPerAreaLevel = 1f;
        public const float PenetrationCap = 75f;
        public const float ArmorMitigationCap = 0.85f;          // fraction
        public const float ArmorNonPhysicalEffectiveness = 0.7f;
        public const float BlockMitigationCap = 0.85f;         // fraction
        public const float DodgeChanceCap = 0.85f;             // fraction
        public const float EnemyCritMultiplier = 2f;           // 200%
        public const float EnemyBaseCritChance = 5f;
        public const float CritAvoidanceCap = 100f;
        public const float ReducedBonusCritDamageCap = 100f;
        public const float EnduranceBase = 20f;
        public const float EnduranceCap = 60f;
        public const float EnduranceThresholdBaseFraction = 0.20f; // of max health
        public const float PlayerResShredPerStack = 2f;        // per stack on players
        public const int PlayerResShredMaxStacks = 20;
        public const float PoisonResShredPerStack = 2f;        // players, first 30 stacks
        public const int PoisonShredStacksThatCount = 30;
        public const float ShockResShredPerStack = 2f;         // lightning res, on players
        public const int ShockMaxStacks = 10;

        // ── Resistances ────────────────────────────────────────
        // Enemy penetration from area level: every enemy has 1% per area
        // level, capped at 75%. NaN area level -> NaN (refuse to guess).
        public static float EnemyPenetration(float areaLevel)
        {
            if (!float.IsFinite(areaLevel)) return float.NaN;
            return Math.Min(PenetrationCap, Math.Max(0f, areaLevel)) * PenetrationPerAreaLevel;
        }

        // Effective resistance a hit sees, in percent points. Shred is
        // subtracted before the cap (so overcap absorbs it), penetration
        // after the cap (can push the result negative; no floor).
        public static float EffectiveResistance(float uncappedResPoints, float shredPoints, float penetrationPoints)
        {
            if (!float.IsFinite(uncappedResPoints) || !float.IsFinite(shredPoints) || !float.IsFinite(penetrationPoints))
                return float.NaN;
            return Math.Min(uncappedResPoints - shredPoints, ResistanceCap) - penetrationPoints;
        }

        // Fraction of damage taken through a resistance. -34% effective
        // resistance means 1.34 times damage.
        public static float ResistanceTakenFraction(float effectiveResPoints)
        {
            if (!float.IsFinite(effectiveResPoints)) return float.NaN;
            return 1f - effectiveResPoints / 100f;
        }

        // Ratio of damage taken with the new resistance versus the old one.
        // Valid no matter whether the recorded hit number was pre- or
        // post-mitigation: the multipliers cancel.
        public static float ResistanceRatio(float oldUncappedPoints, float newUncappedPoints,
                                            float shredPoints, float penetrationPoints)
        {
            float oldT = ResistanceTakenFraction(EffectiveResistance(oldUncappedPoints, shredPoints, penetrationPoints));
            float newT = ResistanceTakenFraction(EffectiveResistance(newUncappedPoints, shredPoints, penetrationPoints));
            if (!float.IsFinite(oldT) || oldT <= 0f) return float.NaN;
            return newT / oldT;
        }

        // ── Armor (hits only; never damage over time) ───────────
        // Fraction of a physical hit mitigated by armor at an area level.
        // Current formula (in place since 0.8.2): two terms that together
        // asymptote at 85%. Negative armor returns negative mitigation,
        // i.e. increased damage taken, by the same magnitude (Tunklab).
        public static float ArmorMitigationFraction(float armor, float areaLevel)
        {
            if (!float.IsFinite(armor) || !float.IsFinite(areaLevel)) return float.NaN;
            if (armor < 0f) return -ArmorMitigationFraction(-armor, areaLevel);
            if (armor == 0f) return 0f;
            float l5 = areaLevel + 5f;
            float t1 = 0.30f * (1.2f * armor) / (80f + 0.05f * l5 * l5 + 1.2f * armor);
            float t2 = 0.55f * (0.0015f * armor * armor) / (180f * l5 + 0.0015f * armor * armor);
            return Math.Min(t1 + t2, ArmorMitigationCap);
        }

        public static float ArmorMitigationNonPhysicalFraction(float armor, float areaLevel)
            => ArmorMitigationFraction(armor, areaLevel) * ArmorNonPhysicalEffectiveness;

        // Ratio of damage taken with new armor versus old armor.
        public static float ArmorRatio(float oldArmor, float newArmor, float areaLevel, bool nonPhysical)
        {
            float oldT = 1f - (nonPhysical ? ArmorMitigationNonPhysicalFraction(oldArmor, areaLevel) : ArmorMitigationFraction(oldArmor, areaLevel));
            float newT = 1f - (nonPhysical ? ArmorMitigationNonPhysicalFraction(newArmor, areaLevel) : ArmorMitigationFraction(newArmor, areaLevel));
            if (!float.IsFinite(oldT) || oldT <= 0f) return float.NaN;
            return newT / oldT;
        }

        // ── Block and dodge (hits only) ──────────────────────────
        // Fraction of a blocked hit's damage removed by block
        // effectiveness at an area level, capped at 85%.
        public static float BlockMitigationFraction(float blockEffectiveness, float areaLevel)
        {
            if (!float.IsFinite(blockEffectiveness) || !float.IsFinite(areaLevel)) return float.NaN;
            if (blockEffectiveness <= 0f) return 0f;
            float l5 = areaLevel + 5f;
            float t1 = 0.25f * (3f * blockEffectiveness) / (40f + 0.03f * l5 * l5 + 3f * blockEffectiveness);
            float t2 = 0.60f * (1.2f * blockEffectiveness + 0.0006f * blockEffectiveness * blockEffectiveness)
                              / (60f * l5 + 1.2f * blockEffectiveness + 0.0006f * blockEffectiveness * blockEffectiveness);
            return Math.Min(t1 + t2, BlockMitigationCap);
        }

        // Dodge chance as a fraction from dodge rating at an area level,
        // capped at 85%. Tunklab's curve (Maxroll embeds it as current);
        // note the official guide still prints an older "10 times area level
        // rating gives about 50%" rule of thumb that this curve contradicts.
        public static float DodgeChanceFraction(float dodgeRating, float areaLevel)
        {
            if (!float.IsFinite(dodgeRating) || !float.IsFinite(areaLevel)) return float.NaN;
            if (dodgeRating <= 0f) return 0f;
            float l5 = areaLevel + 5f;
            float t1 = 0.25f * dodgeRating / (80f + 0.05f * l5 * l5 + dodgeRating);
            float t2 = 0.60f * (0.001f * dodgeRating * dodgeRating) / (32f * l5 + 0.001f * dodgeRating * dodgeRating);
            return Math.Min(t1 + t2, DodgeChanceCap);
        }

        // ── Endurance ───────────────────────────────────────────
        // Applies to the part of the damage that lands on HEALTH below the
        // endurance threshold. Ward is paid first and gets no endurance.
        // healthBefore: health the moment the hit lands. damageToHealth:
        // the part of the hit that reached health after ward.
        public static float EnduranceApplied(float damageToHealth, float healthBefore,
                                             float threshold, float endurancePoints)
        {
            if (!float.IsFinite(damageToHealth) || !float.IsFinite(healthBefore) ||
                !float.IsFinite(threshold) || !float.IsFinite(endurancePoints))
                return float.NaN;
            float e = Math.Clamp(endurancePoints, 0f, EnduranceCap) / 100f;
            if (damageToHealth <= 0f || healthBefore <= threshold)
                return damageToHealth * (1f - e);
            float above = Math.Min(damageToHealth, healthBefore - threshold);
            float below = damageToHealth - above;
            return above + below * (1f - e);
        }

        // ── Critical strikes ────────────────────────────────────
        // A critical strike's damage multiplier for the enemy. The bonus
        // part (the extra 100%) is cut by "reduced/less bonus damage taken
        // from critical strikes", capped at 100% which makes a crit hit for
        // exactly normal damage.
        public static float CritHitMultiplier(float reducedBonusPoints)
        {
            if (!float.IsFinite(reducedBonusPoints)) return float.NaN;
            float r = Math.Clamp(reducedBonusPoints, 0f, ReducedBonusCritDamageCap) / 100f;
            return EnemyCritMultiplier - r;
        }

        // The enemy's effective crit chance after your avoidance (avoidance
        // is rolled after their crit chance).
        public static float EffectiveEnemyCritChance(float enemyCritChancePoints, float critAvoidancePoints)
        {
            if (!float.IsFinite(enemyCritChancePoints) || !float.IsFinite(critAvoidancePoints))
                return float.NaN;
            float a = Math.Clamp(critAvoidancePoints, 0f, CritAvoidanceCap) / 100f;
            return enemyCritChancePoints * (1f - a);
        }

        // ── Ward ────────────────────────────────────────────────
        // Ward lost per second to decay. retentionPoints is ward retention
        // in percent points (40 means 40%); the formula divides by
        // 1 + half the retention FRACTION, per Tunklab's implementation,
        // which calls the same expression with retention/100. Below the
        // ward decay threshold ward does not decay; the caller applies
        // that rule around this value (Tunklab passes max(0, ward - threshold)).
        public static float WardDecayPerSecond(float ward, float retentionPoints)
        {
            if (!float.IsFinite(ward) || !float.IsFinite(retentionPoints)) return float.NaN;
            if (ward <= 0f) return 0f;
            return (0.2f * ward + 0.00005f * ward * ward) / (1f + 0.5f * retentionPoints / 100f);
        }

        // ── Overkill and survival ────────────────────────────────
        // Ward + health you had at the moment of the killing blow. Only
        // meaningful when the recorded damage was the whole post-mitigation
        // hit including ward.
        public static float RemainingEffectiveHealth(float damage, float overkill)
        {
            if (!float.IsFinite(damage) || !float.IsFinite(overkill) || damage <= 0f) return float.NaN;
            if (overkill < 0f || overkill > damage) return float.NaN;
            return damage - overkill;
        }

        // The damage ratio at which the player exactly survives: any lever
        // bringing the hit below this fraction of the recorded damage would
        // likely have saved them (post-mitigation-with-ward meaning only).
        public static float SurvivalBudgetRatio(float damage, float overkill)
        {
            float rem = RemainingEffectiveHealth(damage, overkill);
            if (!float.IsFinite(rem)) return float.NaN;
            return rem / damage;
        }

        // Combine several lever ratios (multiplicative layers).
        public static float CombineRatios(params float[] ratios)
        {
            float p = 1f;
            foreach (float r in ratios ?? Array.Empty<float>())
            {
                if (!float.IsFinite(r)) return float.NaN;
                p *= r;
            }
            return p;
        }

        // ── Units ────────────────────────────────────────────────
        // Percent stats may arrive as 41 or 0.41 depending on the game's
        // internal scale. Never guess a value that could be either.
        public static PercentScale PercentScaleOf(float v)
        {
            if (!float.IsFinite(v)) return PercentScale.Unknown;
            bool percent = Math.Abs(v) > 3f;
            bool fraction = Math.Abs(v) <= 1.5f && v != MathF.Round(v);
            if (percent == fraction) return PercentScale.Unknown;
            return percent ? PercentScale.Percent : PercentScale.Fraction;
        }

        public static float AsPercent(float v, PercentScale scale)
        {
            if (!float.IsFinite(v) || scale == PercentScale.Unknown) return float.NaN;
            return scale == PercentScale.Fraction ? v * 100f : v;
        }

        // ── Previews: one lever, one honest sentence ─────────────
        // Resistance lever, the common case. shredPoints must be 0 unless
        // shred was evidenced on the death (poison stacks, shock stacks, a
        // resistance shred ailment). isDot notes that penetration applies
        // to damage over time too, so the same math holds for ailment ticks.
        public static Counterfactual PreviewResistance(
            float recordedDamage, float overkill,
            float oldUncappedPoints, float newUncappedPoints,
            float shredPoints, float areaLevel,
            DamageMeaning meaning, string lever = null)
        {
            float pen = EnemyPenetration(areaLevel);
            float ratio = ResistanceRatio(oldUncappedPoints, newUncappedPoints, shredPoints, pen);
            return Finish(recordedDamage, overkill, ratio, meaning,
                lever ?? $"resistance {oldUncappedPoints:0}% to {newUncappedPoints:0}%",
                $"area level {areaLevel:0} (enemy penetration {pen:0}%), shred {shredPoints:0} points, resistance cap 75%",
                !float.IsFinite(pen) ? "area level unknown, so enemy penetration (1% per level, up to 75%) is unknown" : null);
        }

        // Armor lever. Refuses damage over time: armor only mitigates hits.
        public static Counterfactual PreviewArmor(
            float recordedDamage, float overkill,
            float oldArmor, float newArmor, float areaLevel,
            bool nonPhysical, bool isDot, DamageMeaning meaning, string lever = null)
        {
            if (isDot)
                return Refuse("armor does not mitigate damage over time, so this lever cannot change this death");
            float ratio = ArmorRatio(oldArmor, newArmor, areaLevel, nonPhysical);
            return Finish(recordedDamage, overkill, ratio, meaning,
                lever ?? $"armor {oldArmor:0} to {newArmor:0}",
                $"area level {areaLevel:0}, armor is 70% as effective against non-physical hits",
                !float.IsFinite(areaLevel) ? "area level unknown, so armor mitigation is unknown" : null);
        }

        // Reduced bonus damage taken from critical strikes, for a recorded
        // crit. targetReducedPoints 100 means the crit hits for normal
        // damage. Avoidance is a chance and cannot be previewed as a number;
        // pair this with EffectiveEnemyCritChance for the odds wording.
        public static Counterfactual PreviewCrit(
            float recordedDamage, float overkill, bool wasCrit,
            float currentReducedBonusPoints, float targetReducedBonusPoints,
            DamageMeaning meaning, string lever = null)
        {
            if (!wasCrit)
                return Refuse("the killing blow was not a critical strike, so crit defenses cannot change it");
            float oldM = CritHitMultiplier(currentReducedBonusPoints);
            float newM = CritHitMultiplier(targetReducedBonusPoints);
            if (!float.IsFinite(oldM) || oldM <= 0f)
                return Refuse("current reduced bonus crit damage is unknown");
            float ratio = newM / oldM;
            return Finish(recordedDamage, overkill, ratio, meaning,
                lever ?? $"reduced bonus crit damage {currentReducedBonusPoints:0}% to {targetReducedBonusPoints:0}%",
                "enemy critical strikes deal 200% damage; the bonus part is what this lever cuts",
                null);
        }

        // A plain multiplicative lever: "X% less damage taken" of any kind
        // (less damage over time taken, less fire damage taken, glancing
        // blow's 35%). lessPoints is the total "less" in percent points.
        public static Counterfactual PreviewLessDamageTaken(
            float recordedDamage, float overkill, float lessPoints,
            DamageMeaning meaning, string lever)
        {
            if (!float.IsFinite(lessPoints)) return Refuse("the size of this lever is unknown");
            float ratio = Math.Max(0f, 1f - lessPoints / 100f);
            return Finish(recordedDamage, overkill, ratio, meaning, lever,
                $"a single {lessPoints:0}% less damage taken layer, multiplicative with the rest",
                null);
        }

        // Pool lever: more health or ward. Under the post-mitigation-with-ward
        // meaning the overkill IS the shortfall: any pool increase at least
        // that big would have absorbed this exact hit. The ratio framing does
        // not fit pool changes, so this fills Lever with the shortfall rule.
        public static Counterfactual PreviewPoolIncrease(
            float recordedDamage, float overkill, float addedPool,
            DamageMeaning meaning, string lever = null)
        {
            if (meaning != DamageMeaning.PostMitigationWithWard)
                return Refuse("pool math needs the recorded damage to be the post-mitigation hit, so the overkill is the exact shortfall");
            float rem = RemainingEffectiveHealth(recordedDamage, overkill);
            if (!float.IsFinite(rem) || !float.IsFinite(addedPool))
                return Refuse("recorded damage, overkill or the pool increase is missing");
            var cf = new Counterfactual
            {
                Lever = lever ?? $"about {overkill:0} more health and ward",
                Kind = Counterfactual.LeverKind.GrowsThePool,
                Ratio = 1f,
                RecordedDamage = recordedDamage,
                NewDamage = recordedDamage,
                RemainingEffectiveHealth = rem,
                AddedPool = addedPool,
                Survived = addedPool >= overkill,
                Meaning = meaning,
                Assumptions = "the overkill is exactly how much you were short by",
                Note = "a bigger pool does not shrink the hit; it absorbs it. This ignores that more max health also raises the endurance threshold, so it is the smallest benefit"
            };
            return cf;
        }

        // Endurance lever. The recorded damage is the hit AFTER endurance was
        // already applied, so this undoes the old endurance before applying
        // the new one. Needs the post-mitigation-with-ward meaning: health
        // at the hit is damage minus overkill, assuming no ward was left,
        // which understates the benefit (any ward makes more of the hit land
        // below the threshold). threshold is the endurance threshold in
        // health points.
        public static Counterfactual PreviewEndurance(
            float recordedDamage, float overkill,
            float threshold,
            float oldEndurancePoints, float newEndurancePoints,
            DamageMeaning meaning, string lever = null)
        {
            if (meaning != DamageMeaning.PostMitigationWithWard)
                return Refuse("the endurance counterfactual needs the recorded damage to be the post-mitigation hit; under other meanings the size of the hit that landed is unknown");
            if (!float.IsFinite(recordedDamage) || recordedDamage <= 0f || !float.IsFinite(overkill))
                return Refuse("recorded damage or overkill is missing, so health at the hit is unknown");
            if (!float.IsFinite(threshold))
                return Refuse("the endurance threshold is unknown");
            float eOld = Math.Clamp(oldEndurancePoints, 0f, EnduranceCap) / 100f;
            float eNew = Math.Clamp(newEndurancePoints, 0f, EnduranceCap) / 100f;
            if (eOld >= 1f) return Refuse("current endurance is invalid");

            float healthAtHit = RemainingEffectiveHealth(recordedDamage, overkill);
            if (!float.IsFinite(healthAtHit))
                return Refuse("overkill is out of range, so health at the hit is unknown");
            float above = Math.Max(0f, healthAtHit - threshold);
            if (recordedDamage <= above)
            {
                var whole = Finish(recordedDamage, overkill, 1f, meaning,
                    lever ?? $"endurance {oldEndurancePoints:0}% to {newEndurancePoints:0}%",
                    $"endurance threshold {threshold:0} health",
                    "the whole hit landed above the endurance threshold, so endurance never applied to it");
                return whole;
            }
            float takenBelow = recordedDamage - above;         // after old endurance
            float preBelow = takenBelow / (1f - eOld);          // undo old endurance
            float newTaken = above + preBelow * (1f - eNew);
            if (recordedDamage <= 0f) return Refuse("recorded damage is missing");
            float ratio = newTaken / recordedDamage;
            return Finish(recordedDamage, overkill, ratio, meaning,
                lever ?? $"endurance {oldEndurancePoints:0}% to {newEndurancePoints:0}%",
                $"endurance threshold {threshold:0} health, applies to the part of the hit below it",
                "assumes no ward was left: any ward means more of the hit lands below the threshold, so this is the smallest possible benefit");
        }

        // Shared tail: apply the ratio to the recorded numbers and decide
        // survival honestly.
        static Counterfactual Finish(float recordedDamage, float overkill, float ratio,
                                     DamageMeaning meaning, string lever, string assumptions, string unknownNote)
        {
            var cf = new Counterfactual
            {
                Lever = lever,
                Ratio = ratio,
                RecordedDamage = recordedDamage,
                Meaning = meaning,
                Assumptions = assumptions,
                Note = unknownNote
            };
            bool haveDamage = float.IsFinite(recordedDamage) && recordedDamage > 0f;
            cf.NewDamage = haveDamage && float.IsFinite(ratio) ? recordedDamage * ratio : float.NaN;

            if (float.IsFinite(ratio) && haveDamage)
            {
                if (meaning == DamageMeaning.PostMitigationWithWard)
                {
                    float rem = RemainingEffectiveHealth(recordedDamage, overkill);
                    if (float.IsFinite(rem))
                    {
                        cf.RemainingEffectiveHealth = rem;
                        cf.Survived = cf.NewDamage < rem;
                    }
                    else
                    {
                        cf.Note = Append(cf.Note, "overkill is missing or out of range, so survival cannot be judged");
                    }
                }
                else
                {
                    cf.Note = Append(cf.Note, meaning == DamageMeaning.PreMitigation
                        ? "the recorded damage is treated as pre-mitigation, so survival cannot be judged"
                        : "ward at the hit is unknown, so survival cannot be judged exactly");
                }
            }
            return cf;
        }

        static string Append(string note, string more)
            => string.IsNullOrEmpty(note) ? more : note.TrimEnd(' ', ';') + "; " + more;

        static Counterfactual Refuse(string why)
            => new Counterfactual { Ratio = float.NaN, Note = why, Assumptions = "" };
    }
}