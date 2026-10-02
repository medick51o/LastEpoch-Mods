# Lane 3: Counterfactual math and regression tests

MedicK's Terrible Death Counter and Log, research lane 3 of 4. Written 2026-10-02 against `src/Core` of `cursor/advice-research-base`. Everything here targets Season 5 (patch 1.5, live since 2026-10-01). The code that implements this file is `src/Core/MitigationMath.cs` (pure C#, no game references), tested in `tests/CoreTests/Program.cs`.

Goal: when the death report shows the killing blow and the defence snapshot shows a number that was below par, say something defensible about what that number would have changed. Never claim a gear change would have prevented a death when the data cannot support that claim.

## 1. Ground rules

1. A counterfactual is only computed where damage and defence units are known. Missing, NaN, or scale-ambiguous inputs produce a refusal, not a guess.
2. The death report gives one damage number and one overkill number. What they mean is not officially documented, so every survival claim names the interpretation it assumed (section 2).
3. A MEASURED defence gap (fire resistance 41% recorded, cap 75%) can be quantified. A CONDITIONAL suggestion (overcap for a shred that may or may not happen, "consider block") is worded qualitatively and never gets a survival promise. Section 5 draws this line precisely.
4. Overcapping a resistance is recommended only when shred is evidenced on the death record (poison stacks on you, shock stacks on you, a resistance shred ailment on you). Penetration is evidenced by area level, but overcap does not help against penetration (official, S4, S5), so penetration alone never justifies overcap.
5. Every worked example below is pinned by a regression test. The table in section 7 maps each case to its test.
6. Wording rules for the "may or may not" band and the four canonical sentences are in section 4.

## 2. The recorded numbers and what they mean

The hook reads `PlayerActorSync.ReceiveDetailedDeathInfo(primaryDamageType, secondaryDamageType, wasCrit, damage, overkillDamage, ailment, ability, attacker, irregularDamageSourceID, isBossfight)`.

What is known:

- Patch 1.1.3 added exactly three numbers to the death screen: how much damage the killing blow dealt, how much overkill it had ("how much damage was dealt beyond what was necessary to reduce the player to 0 health"), and whether it was a crit (S13, July 2024).
- Community checks of the death screen (S17, 2025 forum thread) found displayed damage equals health lost plus ward lost, e.g. "289 health lost + 950 ward lost = 1239, the screen said 1238" and "516 + 820 = 1336, the screen said 1343". So the number the API calls `damage` is almost certainly the whole post-mitigation hit, ward included, and `overkillDamage` is the part beyond ward plus health you had left.

What is NOT verified:

- Whether `damage` is pre- or post-mitigation officially. We adopt the community-verified reading as primary:

  - Interpretation A (primary): `damage` = whole post-mitigation hit including ward. Then `damage - overkill` = ward plus health you had at that moment, so survival questions are answerable, and `overkill` is exactly the pool shortfall.
  - Interpretation B (fallback): `damage` = raw pre-mitigation hit. Then no survival claim is possible at all (we never see what actually landed), and even the absolute "the hit would have been about X" changes meaning (X is then the damage that would have reached you, not a share of the recorded number).

- Whether ward is included. Under interpretation A the community evidence says yes. If some future in-game check shows ward is NOT included, then `damage - overkill` is health alone, ward adds on top, and every "you had about X left" line under-counts by the ward you had; survival verdicts become "more likely to survive than stated", never less. The ratio maths (section 3) is unaffected either way.
- What `irregularDamageSourceID` means. Not used by any formula.
- How primary and secondary damage types split the recorded number. When a secondary type exists the split is unknown, so any resistance counterfactual on such a hit is ratio-only with a note, or uses the primary type alone, stated as an assumption.
- Whether the recorded `damage` already had endurance applied. It did (endurance is a "less damage taken" layer inside mitigation), so the endurance preview must undo the current endurance before applying the new one (section 3.5).

The key invariance: a layer's damage ratio (new taken divided by old taken) is the same under both interpretations, because the layer's multiplier cancels. So every preview returns a Ratio that is always safe to state, while NewDamage and survival carry the interpretation flag. `MitigationMath.PreviewResistance(...)` computes survival only under `DamageMeaning.PostMitigationWithWard` and sets `Survived = null` otherwise.

## 3. Formulas, Season 5 / patch 1.5

Patch 1.5 changed no core defence formula: the Season 5 patch notes (S12) touch skills, items and ailments, not resistance, armor, endurance, ward, crit, block or dodge mechanics. Each formula below cites its source and date. Percentages are written as points (41 means 41%).

### 3.1 Resistance

Taken fraction = 1 - effective resistance / 100. Effective resistance, in points:

```
effective = min(uncapped - shred, 75) - penetration
```

- Cap: 75% for every damage type, resistances work against hits AND damage over time, and negative resistance adds 1% damage taken per point (S4, July 14 2026).
- Shred applies BEFORE the cap, so overcap absorbs it (S4, S9). Resistance shred is 2 points per stack against players (5 against normal monsters), max 20 stacks, 4 s per stack (S16 game guide mirror, Mar 2024; S11 April 2026). Shock is a separate 2 points of lightning resistance per stack on players (S4 official example: 75 with one shock stack becomes 73), max 10 stacks (S11).
- Penetration applies AFTER the cap and can push effective resistance negative. All enemies gain 1 point of penetration per area level, capped at 75, so in a level 75+ area a capped resistance is effectively 0% and an uncapped one is negative (S5, Feb 24 2026; S8, Feb 12 2026). Overcap does NOT help against penetration (S4, S5, S9 official wording; S10).
- Penetration also applies to damage over time, including enemy ailments like ignite and poison (S19 wiki formula page; S20 forum answer by a Maxroll author, Dec 2021; S21 2025 forum).
- Poison additionally shreds poison resistance by 2 points per stack on players, only the first 30 stacks count (S16; S11).

Example of the order: uncapped 75, 10 poison stacks (20 shred), area 100: effective = min(75 - 20, 75) - 75 = 55 - 75 = -20, so you take 120% of the raw poison damage.

### 3.2 Armor

Armor mitigates HITS only, never damage over time, and is 70% as effective against non-physical damage, with a mitigation cap of 85% (which makes the non-physical cap 59.5%) (S1, July 13 2026; S10, April 2026). The mitigation fraction depends on armor x and area level a. The official article shows the formula as an image; the curve below is Tunklab's implementation of it (S14, retrieved Oct 2 2026), which I verified against three independent build planner data points at area level 100 (S15):

```
mitigation(x, a) = 0.30 * (1.2x) / (80 + 0.05*(a+5)^2 + 1.2x)
               + 0.55 * (0.0015*x^2) / (180*(a+5) + 0.0015*x^2)
```

The two terms asymptote at 0.30 + 0.55 = 0.85, which is exactly the documented cap, a strong structural check. Verification data points (physical, area level 100):

- 701 armor gives 0.192 (a 2020 forum quote of Tunklab's chart said "about 20%", S18)
- 2763 armor gives 0.4595, planner shows 46% (game version 1.0.3)
- 2976 armor gives 0.4820, planner shows 48% (version 1.0 PTR)
- 3366 armor gives 0.5199, planner shows 52% (version 0.8.5f)

Negative armor (armor shred pushes you below zero) increases damage taken by the same magnitude the positive value would have mitigated: mitigation(-x) = -mitigation(x) (S14 code; S16's "if 700 armor mitigates 20%, -700 armor increases damage taken by 20%"). Armor shred on players is 100 armor per stack, no stack cap, 4 s per stack (S16, S21).

Armor vs DoT: no effect, with narrow exceptions that are item affixes, not general mechanics ("X% of armor mitigation also applies to damage over time", e.g. Eternal Gauntlets' implicit, S16, S10). The helper refuses armor previews for DoT deaths outright.

### 3.3 Endurance

- Everyone starts with 20% endurance and a threshold of 20% of max health. Endurance caps at 60%; the threshold has no cap and its affix rolls on belts.
- Endurance is "less damage taken" applied to the part of ANY damage instance that lands below the threshold, even if the instance started above it (official example: a 100 damage hit where 50 lands below threshold gets endurance on those 50) (S2, Feb 13 2026).
- It applies to health only, NOT to ward, and to both hits and damage over time (S2).
- It is multiplicative with other layers (S2).

Counterfactual subtlety: the recorded damage already had your current endurance applied. The preview must undo it: with health at hit H, threshold T, current endurance e_old and recorded post-endurance damage D, the part below the threshold after old endurance is D - max(0, H - T), so the pre-endurance part below is (D - max(0, H - T)) / (1 - e_old), and with the new endurance the hit would have dealt max(0, H - T) + pre_below * (1 - e_new). If D <= max(0, H - T) the whole hit landed above the threshold and endurance never applied, so more endurance changes nothing for this hit (ratio 1, with that note).

Under interpretation A, H = damage - overkill, which assumes no ward was left; any ward makes more of the hit land below the threshold, so the computed benefit is the smallest possible. The helper states this in every endurance preview note.

### 3.4 Ward

- Ward sits above health and takes damage before it; nothing bypasses ward except a few percent-of-current-health ground effects that cannot kill on their own (S10, April 2026; S6, July 14 2026).
- Ward absorbs damage over time too: ailments tick into ward first. (S10 describes ward as absorbing "any damage you take"; the old lane 1 research file marked an explicit ward-vs-DoT sentence as unconfirmed. The live death screen evidence in S17 shows DoT deaths eating ward, so we treat ward as DoT-absorbing and note it as high confidence, not officially verbatim.)
- Ward decays: rate = (0.2 * w + 0.00005 * w^2) / (1 + 0.5 * retention / 100) per second, where retention is in percent points (40 points of retention means dividing by 1.2) (S14 code, the "1.1" version; the same code keeps the pre-1.1 formula 0.4w / (1 + 0.5 * retention / 100) as old, matching S10's changelog note "Updated Ward formula to the 1.1 version", July 2024; the Tunklab ward page feeds the formula max(0, ward - threshold)). Ward does not decay below the ward decay threshold, and decay at the threshold value is halved (S6 official example: threshold 500 halves decay at 1000 ward).
- Endurance does not apply to damage taken by ward (S2), which is why the endurance preview needs the health split.

### 3.5 Critical strikes

- Enemy crits deal 200% damage (double). Damage over time cannot crit. All hits have a base 5% crit chance (S3, Feb 13 2026).
- Critical strike avoidance is rolled AFTER the enemy's crit chance and turns a crit into a normal hit; cap 100%, at which enemies can never crit you. Effective enemy crit chance = enemy_chance * (1 - avoidance / 100) (official example: 30% enemy crit chance with 50% avoidance gives 15%) (S3).
- "Less/reduced bonus damage taken from critical strikes" cuts the BONUS part (the extra 100 points of the 200 multiplier), cap 100, at which a crit hits for exactly normal damage (S3). Crit multiplier with r points reduced = 2 - r/100.

Counterfactuals: for a recorded crit, 100% avoidance means this exact hit would have been a normal hit, so the ratio is 0.5. With reduced bonus r the ratio is (2 - r/100) / (2 - r_old/100). Partial avoidance is a chance, not a damage change, so it is worded as odds ("at 60% avoidance, 60 of every 100 such crits would have been normal hits"), never as a smaller hit.

### 3.6 Block

- Block chance is additive; on a block, block effectiveness removes a percentage of the hit, capped at 85%, scaled by area level; blocked hits still count as hits; DoT cannot be blocked (S10, April 2026; S7).
- Tunklab curve (S14), verified against a planner point (block effectiveness 325 at level 100 computes to 0.2214, planner shows 22%, S15):

```
block_mitigation(x, a) = 0.25 * (3x) / (40 + 0.03*(a+5)^2 + 3x)
                     + 0.60 * (1.2x + 0.0006*x^2) / (60*(a+5) + 1.2x + 0.0006*x^2)
```

Again the terms asymptote at 0.25 + 0.60 = 0.85, matching the documented cap.
- Block is a chance layer: the honest wording is "a blocked version of this hit would have been about X, so block chance would sometimes have saved you", never a flat claim.

### 3.7 Dodge

- Dodge chance from rating, lower in higher areas, cap 85%; a dodged hit deals nothing and applies no ailments; DoT cannot be dodged; dodging does not count as being hit (S7, July 14 2026).
- Tunklab curve (S14), which Maxroll embeds as the current scaling (S10):

```
dodge_chance(x, a) = 0.25 * x / (80 + 0.05*(a+5)^2 + x)
                 + 0.60 * (0.001*x^2) / (32*(a+5) + 0.001*x^2)
```

Terms asymptote at 0.85, matching the cap. Data point: 16 rating at level 100 gives 0.0062, and the planner shows 1% (S15).
- DISCREPANCY, flagged: the official Dodge article still prints the old rule of thumb "dodge rating equal to 10 times the area level gives about 50% chance" (S7), but this curve gives 1000 rating at area level 100 only 29.1%. The old rule matches a pre-1.0 formula (x / (x + 10a) gives exactly 50%). The in-game guide text is known to lag (the Neoseeker mirror of it still said the dodge cap was 75% in early 2024, S16, while the current official cap is 85%). We use the Tunklab curve because Maxroll (April 2026) sources its current dodge chart from it, and we never print an absolute dodge percentage in advice, so being wrong here costs nothing user-visible.
- Dodge is a chance layer: same "sometimes" wording as block.

### 3.8 Layer ordering

Layers are multiplicative and order does not matter (official Maxroll worked example: resistance, then block effectiveness, then glancing blow, then reduction-while-moving: 100 -> 50 -> 32.5 -> 24.375, S10). Endurance is the one layer that is position-aware (threshold on health), so in the helper it is previewed last, on the output of the other layers.

## 4. The counterfactual framework

For a recorded hit D with overkill O and a lever that changes damage by ratio R:

1. Ratio R is computed from the lever's formulas (section 3). Safe under every interpretation.
2. NewDamage = D * R. Meaning depends on interpretation (share of the recorded number under A; the damage that would have reached you under B).
3. Under interpretation A only: RemainingEffectiveHealth = D - O (ward plus health you had). Survived = (D * R) < (D - O). The SurvivalBudgetRatio = (D - O) / D is the ratio any single lever must beat.
4. Borderline band: if |NewDamage - Remaining| <= 10% of Remaining, the sentence is "about equal to the about X you had left, so it may or may not have been enough", even when the strict comparison says survived. This absorbs rounding, the second hit you did not see, and regen between ticks.
5. Pool levers (more health or ward) do not shrink the hit; under interpretation A the overkill IS the shortfall, so "any pool increase of at least about O would have absorbed this exact hit", with the note that more max health also raises the endurance threshold, so O is the smallest possible requirement.
6. Chance levers (block, dodge, partial crit avoidance) are worded as frequencies: "would sometimes have saved you".

### 4.1 The four canonical sentences

- No recorded damage: "Lever would have reduced this hit by about P%."
- No survival info (missing overkill, or interpretation B, or DoT tick among other ticks): "Lever would have reduced this hit from about D to about N, which may or may not have been enough."
- Survival computed, outside the band, survives: "Lever would have reduced this hit from about D to about N, below the about R you had left, so you would likely have survived, assuming the recorded damage was the whole post-mitigation hit including ward."
- Survival computed, outside the band, dies: "Lever would have reduced this hit from about D to about N, still above the about R you had left, so it would probably still have killed you."

Numbers are rounded to "about" values (tens). "Likely", "probably", "may or may not" are load-bearing words; they appear exactly as shown, never "would have survived" flat.

## 5. Measured gaps vs conditional suggestions

MEASURED (the death record or snapshot holds the number, so the counterfactual is quantified):

- Resistance below cap: recorded uncapped value vs the 75 cap. Example: fire 41 recorded. Note that the recorded capped value alone does not show overcap; the snapshot keeps `ResUncapped.*` for that.
- Armor value, block effectiveness, dodge rating recorded.
- Endurance value and threshold recorded (or threshold assumed at 20% of max health, stated).
- Crit avoidance recorded; overkill and damage from the death report.
- Pool shortfall: overkill, always measured under interpretation A.
- Shred evidenced by counted stacks: poison stacks on you (2 points each, first 30), shock stacks on you (2 points each of lightning res, max 10), a resistance shred ailment on you (2 points each, max 20), armor shred (100 armor each, unlimited).

CONDITIONAL (no number recorded, so wording is qualitative and never promises survival):

- Overcapping resistance "because there might be shred". Only when stacks are counted (section 1 rule 4). Even then, in a level 75+ area penetration pins the effective resistance at or below 0 for players AT cap, so the honest frame is "overcap restores what shred took, not more".
- Less damage over time taken, less elemental damage taken, glancing blow: affixes the snapshot does not read. Wording: "a 15% less damage over time taken layer would have cut each tick by 15%" is quantifiable as a hypothetical, but it is not advice about YOUR gear, since we cannot see what you had.
- Block and dodge presence (chance framing, section 4 rule 6).
- Stun/freeze context, movement, "do not stand in it": always conditional.

The Advisor integration (lane 4) should only let a MEASURED gap produce a sentence with numbers in it. CONDITIONAL items keep the current qualitative tips.

## 6. Worked examples, full arithmetic

All examples assume interpretation A unless the line says otherwise. Every example is pinned by tests (section 7). Percent points everywhere.

### (a) Fire killing blow, fire resistance 41% vs the 75% cap

Recorded: damage 1700, overkill 700, fire resistance 41 (uncapped 41, so no overcap), area level 100, no shred on you, not a crit.

1. Remaining pool: 1700 - 700 = 1000.
2. Penetration: min(75, 100) = 75 points.
3. Effective resistance now: min(41 - 0, 75) - 75 = 41 - 75 = -34. Taken fraction = 1 - (-34/100) = 1.34. (You were taking 134% of raw fire damage.)
4. Effective resistance capped: min(75 - 0, 75) - 75 = 0. Taken fraction = 1.00.
5. Ratio = 1.00 / 1.34 = 0.7463.
6. New hit = 1700 * 0.7463 = 1268.7, about 1270.
7. 1270 > 1000 remaining, so it would probably still have killed you; the overkill would have been about 270 instead of 700.
8. Survival budget: 1000 / 1700 = 0.588. Capping alone flips the verdict only when (25 + p) / (59 + p) < 0.588, that is p < 23.5, that is area level 23 or lower.

Without area-level penetration (comparison only; every real area has at least 1 point):

1. Effective now: 41. Taken 0.59. Capped: 75. Taken 0.25.
2. Ratio = 0.25 / 0.59 = 0.4237. New hit = 1700 * 0.4237 = 720.3, about 720.
3. 720 < 1000, so you would likely have survived. The verdict flips completely when penetration is out of the picture; this is why the area level is a required input, not a nicety.

At area level 40 (penetration 40): taken now = 1 - (41 - 40)/100 = 0.99; capped = 1 - (75 - 40)/100 = 0.65. Ratio 0.6566, new hit about 1116, still dead. At area level 66: ratio 0.7280, about 1238, still dead.

Under interpretation B (damage is pre-mitigation): the raw hit is 1700. With 41% (effective -34) you took 1700 * 1.34 = 2278. Capped you would have taken 1700 * 1.00 = 1700, that is 25% less damage reaching you (the same ratio, 0.7463, as always). Whether 1700 would have killed you cannot be said, because health and ward at that moment are not observable under this interpretation. The sentence degrades to the "which may or may not have been enough" form, and in fact to the ratio-only form.

### (b) Fire killing blow, fire resistance already capped

Recorded: damage 2200, overkill 400, fire resistance 75 (uncapped 75), area 100, max health 2000, endurance 20 (base), threshold 400 (20% of 2000).

What resistance can still do here: nothing. Effective = min(75, 75) - 75 = 0, and overcapping to any value still gives min(uncapped, 75) - 75 = 0, because penetration applies after the cap. Ratio = 1.0. The honest resistance line is "your fire resistance was already capped; penetration from the area level zeroed it, so no resistance number would have helped".

The levers that remain, quantified:

1. Endurance 20 -> 60 (cap), threshold 400, ward assumed 0 so health at hit = 2200 - 400 = 1800.
   - Above threshold: 1800 - 400 = 1400 at full damage.
   - Below threshold after old endurance: 2200 - 1400 = 800; pre-endurance: 800 / (1 - 0.20) = 1000.
   - New damage: 1400 + 1000 * (1 - 0.60) = 1800. Ratio = 1800 / 2200 = 0.8182.
   - New hit about 1800 vs about 1800 left: inside the borderline band, so "may or may not have been enough". This is the canonical (b) sentence.
2. Pool: you were short by exactly the overkill, 400. About 400 more health and ward absorbs this exact hit (borderline at exactly 400; 500 is clearly enough). More max health also raises the endurance threshold, so 400 is the smallest possible requirement.
3. 15% less fire damage taken: 2200 * 0.85 = 1870 > 1800: still dead by about 70. Conditional (we did not record that stat).
4. If it was a crit (separate case), 100% crit avoidance halves it to 1100 < 1800: survives.
5. Block 40% chance with block effectiveness 325 (mitigation 0.2214 at level 100): a blocked version = 2200 * (1 - 0.2214) = 1713, about 1710, below the about 1800 you had left, so "block would sometimes have saved you".

### (c) Ignite killing blow, fire resistance capped

Recorded: the killing tick dealt 350, overkill 30, fire resistance 75, area 100, ignite stacks on you.

What works on an ignite tick: fire resistance (it did, effective 75 - 75 = 0 here), ward, endurance below threshold, health pool, "less damage over time taken". What never works: armor (DoT), dodge, block, crit avoidance (DoT cannot crit). The helper's armor preview refuses with exactly that reason string.

Honest options:

1. A 15% less damage over time taken layer: tick = 350 * 0.85 = 297.5, about 300, below the about 320 you had left, so this tick would not have killed you. But other ignite stacks were still ticking, so "may or may not have been enough" for the death as a whole. Single-tick survival is not death survival for DoT deaths; every DoT preview carries that caveat.
2. Overcap: without evidenced fire shred, overcap does nothing here (ratio 1.0). With, say, 10 evidenced resistance shred stacks (20 points), overcap to 95 would restore min(95 - 20, 75) - 75 = 0 instead of 55 - 75 = -20, a 17% cut. Recommended only in that evidenced case.
3. Regen or leech outpacing the tick rate, and moving out of the fire, are the conditional classic answers; they get no numbers.

### (d) Bleed killing blow, the armor vs DoT question

Recorded: the killing tick dealt 300, overkill 40, physical resistance 20 (measured), armor 2763 (measured), area 100.

The point of this case: the ailment is PHYSICAL, and armor still does nothing, because bleed is damage over time. The helper refuses the armor preview with "armor does not mitigate damage over time". Physical resistance does work:

1. Effective now: min(20, 75) - 75 = -55. Taken 1.55.
2. Capped: 0. Taken 1.00. Ratio = 1.00 / 1.55 = 0.6452.
3. Tick would have been 300 * 0.6452 = 193.5, about 194, below the about 260 you had left: this tick would not have killed you. Bleed stacks keep ticking, so the death verdict keeps the caveat.
4. Physical resistance is rare on gear (conditional note; Sentinel's Battle Hardened and a few uniques aside), so the advice pairs the number with "hard to gear for".
5. Less damage over time taken, regen, leech: same as (c).

### (e) Poison killing blow, poison resistance and shred

Recorded: tick 900, overkill 100, poison resistance 75 uncapped, 10 poison stacks on you (counted), area 100. Remaining = 800.

1. Shred from stacks: 10 * 2 = 20 points, before the cap.
2. Effective now: min(75 - 20, 75) - 75 = 55 - 75 = -20. Taken 1.20.
3. Overcap to 95: min(95 - 20, 75) - 75 = 75 - 75 = 0. Taken 1.00. Ratio = 1 / 1.20 = 0.8333.
4. Tick would have been 900 * 0.8333 = 750, about 750 vs about 800 left: inside the band (|750 - 800| = 50 <= 80), so "may or may not have been enough" for this tick.
5. This overcap recommendation is MEASURED, because the 10 stacks were on you. 20 points of overcap is about 4 resistance affix tiers, a real cost, so the advice also names the alternatives (kill the poisoner, cleanse, leave the pool).
6. At 30 stacks: shred 60, effective = 15 - 75 = -60, taken 1.60. You would need 135 uncapped to pin 0, which is not realistic; the honest advice at high stacks is avoidance and cleansing, not overcap.
7. Without evidenced stacks, overcap gives ratio 1.0 and is never recommended (section 1 rule 4).

### (f) Critical strike killing blow

Recorded: damage 2000, overkill 600, wasCrit true, crit avoidance 0 (measured), reduced bonus crit damage 0, area 100. Remaining = 1400.

1. Enemy crits deal 200%. The normal-hit version of this exact hit is 1000.
2. 100% critical strike avoidance: ratio 0.5, new hit about 1000 < about 1400 left, so "you would likely have survived, assuming the recorded damage was the whole post-mitigation hit including ward". This is the one case where a survival claim is strong.
3. Reduced bonus crit damage 50: multiplier = 2 - 0.50 = 1.5. New hit 1500 > 1400: still dead, overkill about 100. At 100: multiplier 1.0, new hit 1000: survives.
4. Partial avoidance 60 with the enemy's base 5% crit chance: effective crit chance = 5 * (1 - 0.60) = 2%. Wording: "60 of every 100 such crits would have been normal hits"; the hit size is unchanged, so no survival claim from partial avoidance.
5. DoT deaths never get this lever (DoT cannot crit, official).

### (g) Overkill: small vs large relative to max health

The overkill is the shortfall (interpretation A), and the SurvivalBudgetRatio = (D - O) / D says how much damage reduction any single lever must deliver.

1. Small: damage 1050, overkill 50, max health 2000. Remaining 1000. Budget = 1000 / 1050 = 0.952. Any single layer worth about 5% (one resistance point at low area level, a small armor increase, glancing blow's 35%) flips it. Honest sentence: "you were short by about 50: this death was razor thin, nearly any defence investment changes it."
2. Large: damage 5000, overkill 3950, max health 2000. Remaining 1050. Budget = 0.21: you needed a 79% damage cut from one lever, which no realistic layer delivers (resistance cap is 75 points and penetration already eats it; armor cap 85% needs far more armor than gear gives). The hit is 2.5 times your whole health pool. Honest advice: dodge, block, crit avoidance if it crit, or do not stand in the telegraph. Mitigation math refuses to promise anything here, and that refusal is the deliverable.
3. Missing overkill (NaN): ratio statements only, no survival claims, no shortfall line.

### (h) Missing max health, missing ward

1. Missing max health: the endurance threshold (20% of max) is unknown, so the endurance preview refuses ("the endurance threshold is unknown"). The "% of your life" phrasing is unavailable. The overkill survival math is UNAFFECTED, because remaining = damage - overkill never needed max health. Lane 4 must not gate survival claims on max health being present.
2. Missing ward at the hit: under interpretation A the remaining pool is ward + health combined, so survival math still works on the pool, but the health/ward split is unknown, so the endurance preview assumes ward 0 (smallest benefit, stated in its note), and no sentence may claim "your ward would have absorbed it". If a future check proves the API's damage excludes ward, remaining is health-only and real survival is strictly more likely than computed; ratios remain identical.

### (i) Physical hit with armor and endurance

Recorded: damage 1800, overkill 300, armor 2763 (measured), area 100, max health 2000, endurance 20, threshold 400. Remaining = 1500.

1. Current armor mitigation at level 100: term1 = 0.30 * 3315.6 / (80 + 551.25 + 3315.6) = 0.30 * 3315.6 / 3946.85 = 0.2521; term2 = 0.55 * 11451.3 / (18900 + 11451.3) = 0.55 * 11451.3 / 30351.3 = 0.2075. Total 0.4595 (the planner shows 46%).
2. Armor 4000: term1 = 0.30 * 4800 / (80 + 551.25 + 4800) = 0.30 * 4800 / 5431.25 = 0.2651; term2 = 0.55 * 24000 / (18900 + 24000) = 0.55 * 24000 / 42900 = 0.3077. Total 0.5728.
3. Ratio = (1 - 0.5728) / (1 - 0.4595) = 0.4272 / 0.5405 = 0.7904. New hit = 1800 * 0.7904 = 1422.7, about 1420 vs about 1500 left: inside the band (|1423 - 1500| = 77 <= 150), so "may or may not have been enough" for armor alone.
4. Endurance 20 -> 60 on top of the new armor: above = 1500 - 400 = 1100; D' = 1422.7; below taken = 322.7; pre = 322.7 / 0.8 = 403.4; new = 1100 + 403.4 * 0.4 = 1261.3. Combined ratio = 0.7904 * (1261.3 / 1422.7) = 0.7904 * 0.8866 = 0.7007. New hit about 1260 vs about 1500: outside the band, so "you would likely have survived". Two modest layers together clear what neither clears alone; that is the standard pattern the advisor should show.
5. Non-physical variant: armor mitigation = 0.4595 * 0.7 = 0.3217 (32.2%), cap 0.595. The armor ratio for the same 2763 -> 4000 on a fire hit = (1 - 0.4010) / (1 - 0.3217) = 0.5990 / 0.6783 = 0.8831: armor is a weak lever against elemental hits, and the advice should say so by number.

## 7. Regression case table

Every row is pinned by a test in `tests/CoreTests/Program.cs` (prefix `Test_Math_`). Tolerances: formula outputs (ratios, fractions, points) within 0.001; damage amounts within 0.01 (they are recorded damage times a ratio, printed here rounded); sentence checks by substring.

| # | Case | Inputs | Expected | Test |
|---|------|--------|----------|------|
| A1 | Armor at 3 planner points | (2763, 2976, 3366) @ L100 | 0.4595, 0.4820, 0.5199 | Test_Math_Armor_MatchesPlannerDataPoints |
| A2 | Armor 2020 forum point | 701 @ L100 | 0.1920 | Test_Math_Armor_MatchesTunklabChartQuote |
| A3 | Armor cap | huge armor @ L100 | 0.85 | Test_Math_Armor_CapsAt85 |
| A4 | Armor non-physical | 2763 @ L100 | 0.3217 | Test_Math_Armor_NonPhysicalIs70Percent |
| A5 | Negative armor | -700 @ L100 | -0.1919 | Test_Math_Armor_NegativeMirrorsPositive |
| A6 | Armor zero/NaN | 0, NaN @ L100 | 0, NaN | Test_Math_Armor_ZeroAndNaN |
| B1 | Block planner point | 325 @ L100 | 0.2214 | Test_Math_Block_MatchesPlannerDataPoint |
| B2 | Block cap | huge @ L100 | 0.85 | Test_Math_Block_CapsAt85 |
| C1 | Dodge planner point | 16 @ L100 | 0.0062 | Test_Math_Dodge_MatchesPlannerDataPoint |
| C2 | Dodge cap + curve | 1000, huge @ L100 | 0.2909, 0.85 | Test_Math_Dodge_CurveAndCap |
| D1 | Penetration by area level | 40, 75, 100, 120, NaN | 40, 75, 75, 75, NaN | Test_Math_Penetration_ByAreaLevel |
| D2 | Effective resistance order | 41 uncapped, pen 75 | -34 | Test_Math_Resist_EffectiveOrderShredCapPen |
| D3 | Negative taken fraction | eff -34 | 1.34 | Test_Math_Resist_NegativeGrowsDamage |
| D4 | Overcap clamped | 150 uncapped, pen 0 | eff 75, taken 0.25 | Test_Math_Resist_OvercapClampedAtCap |
| D5 | Shred below zero | 10 uncapped, 40 shred | eff -30 | Test_Math_Resist_ShredBelowZero |
| E1 | Example (a) with pen | 1700/700, 41->75, L100 | ratio 0.7463, new 1268.7, dead | Test_Math_ExampleA_Fire41_Capped_WithPen |
| E2 | Example (a) wording | same | "still above the about 1000" | Test_Math_ExampleA_WordingStillKilled |
| E3 | Example (a) no pen | pen 0 | ratio 0.4237, new 720.3, survives | Test_Math_ExampleA_Fire41_NoPen_Survives |
| E4 | Example (a) area 40 | pen 40 | ratio 0.6566, new 1116.2, dead | Test_Math_ExampleA_Fire40Area_StillDies |
| E5 | Example (a) pre-mitigation | meaning PreMitigation | ratio same 0.7463, Survived null | Test_Math_ExampleA_PreMitigationNoSurvivalClaim |
| E6 | Ratio invariance | both meanings | identical ratio | Test_Math_RatioInvariantToDamageMeaning |
| E7 | Example (b) capped dead end | 75->150 uncapped, L100 | ratio 1.0 | Test_Math_ExampleB_CappedResistanceIsADeadEnd |
| E8 | Example (b) endurance | 2200/400, T400, 20->60 | ratio 0.8182, new 1800, borderline sentence | Test_Math_ExampleB_EnduranceMayOrMayNot |
| E9 | Example (b) pool | +400, +500, +200 | borderline, absorbs, not enough | Test_Math_ExampleB_PoolShortfallIsOverkill |
| E10 | Example (b) block | 325 eff @ L100 | blocked 1713 < 1800 | Test_Math_ExampleB_BlockedVersionSurvives |
| E11 | Example (c) armor refuses | DoT | refusal string | Test_Math_ExampleC_ArmorRefusesDot |
| E12 | Example (c) less DoT | 350/30, 15% | 297.5, may-or-may-not caveat | Test_Math_ExampleC_LessDotTakenTick |
| E13 | Example (d) bleed res | 300/40, 20->75, L100 | ratio 0.6452, new 193.5 | Test_Math_ExampleD_BleedPhysicalRes |
| E14 | Example (e) poison overcap | 900/100, 75->95, shred 20 | ratio 0.8333, new 750, borderline | Test_Math_ExampleE_PoisonOvercapMeasured |
| E15 | Overcap without shred | shred 0 | ratio 1.0 | Test_Math_ExampleE_OvercapNeedsEvidencedShred |
| E16 | Example (f) full avoidance | 2000/600, crit | ratio 0.5, new 1000, survives | Test_Math_ExampleF_AvoidanceHalvesCrit |
| E17 | Example (f) reduced bonus | 0->50 | ratio 0.75, new 1500, dead | Test_Math_ExampleF_ReducedBonusCrit |
| E18 | Example (f) effective chance | 5% enemy, 60 avoid | 2% | Test_Math_ExampleF_EffectiveCritChance |
| E19 | Crit on non-crit refuses | wasCrit false | refusal | Test_Math_Crit_NonCritRefuses |
| E20 | Example (g) small overkill | 1050/50 | budget 0.952 | Test_Math_ExampleG_SmallOverkillThinMargin |
| E21 | Example (g) large overkill | 5000/3950 | budget 0.21 | Test_Math_ExampleG_LargeOverkillNeedsAvoidance |
| E22 | Example (h) missing max health | no threshold | endurance refuses, survival intact | Test_Math_ExampleH_MissingMaxHealth |
| E23 | Example (h) missing ward | meaning PostMitigationHealthOnly | survival null, ratio intact | Test_Math_ExampleH_HealthOnlyMeaning |
| E24 | Example (i) armor ratio | 1800/300, 2763->4000 | ratio 0.7904, new 1422.7, borderline | Test_Math_ExampleI_ArmorRatio |
| E25 | Example (i) combined | + endurance 60 | combined 0.7007, new about 1260, survives | Test_Math_ExampleI_ArmorPlusEndurance |
| E26 | Example (i) non-physical armor | same, nonPhysical | ratio 0.8831 | Test_Math_ExampleI_ArmorWeakVsElemental |
| F1 | Endurance whole-above | hit above threshold | ratio 1, note | Test_Math_Endurance_AboveThresholdNoEffect |
| F2 | Endurance split | official 100/50 example | 50 unmitigated, 50 at endurance | Test_Math_Endurance_OfficialSplitExample |
| F3 | Endurance below threshold | H <= T | full application | Test_Math_Endurance_FullyBelowThreshold |
| F4 | Ward decay | 1000 ward, 40 retention; 500 ward, 0 retention | 208.33/s; 112.5/s | Test_Math_WardDecay_Formula |
| F5 | Unknown units | 0.41 fraction, 41 percent, 2 ambiguous | 41, 41, refused | Test_Math_Units_NeverGuessAmbiguousScale |
| F6 | NaN/missing everywhere | all NaN | refusals, never guesses | Test_Math_MissingFields_AlwaysRefuse |
| F7 | Overkill out of range | overkill > damage, negative | remaining NaN, survival null | Test_Math_Overkill_OutOfRange |
| F8 | Missing damage | damage NaN | ratio-only sentence | Test_Math_MissingDamage_RatioOnlyWording |

Sentences are additionally snapshot-checked where the row says so (E2, E8, E9, E12).

## 8. Assumption list

Every one of these is stated in code comments, doc notes, or test comments, and each can flip a verdict:

1. Interpretation A: the death report's `damage` is the whole post-mitigation hit including ward; `overkill` is the pool shortfall. Community-verified (S17), not official. All survival claims hang on this and say so.
2. Area level is known (the mod records the zone). Without it, penetration is unknown and resistance previews refuse rather than guess 1..75.
3. No shred is assumed unless stacks are counted on you. Poison 2/stack (first 30), shock 2/stack (max 10, lightning only), resistance shred 2/stack (max 20), armor shred 100/stack (unlimited).
4. Enemy penetration applies to enemy DoTs (ignite, poison ticks). Multiple community sources agree (S19, S20); the official Penetrations article says "a source of damage" without limiting it to hits. If 1.5 ever excludes DoTs from area penetration, capped resistances become 75% effective against DoT deaths at any area level and every DoT example above gets LESS damage; the resistance lever for capped DoT deaths would change from "nothing more available" to "already working, look elsewhere", the ratio math is unchanged.
5. Endurance previews assume no ward was left (smallest-benefit bound), because the health/ward split at the hit is not recorded.
6. The recorded hit is one instance. Bursts, simultaneous second hits, and regen between ticks are modeled only by the borderline band and the "may or may not" wording, never by numbers.
7. Enemy base crit chance 5% is used only for the chance framing in (f); the deterministic halving preview does not need it.
8. The armor, block and dodge curves are Tunklab's implementation of the official formula images, verified against planner points from versions 0.8.5f, 1.0 PTR and 1.0.3 at area level 100. Patch 1.5 changed none of these mechanics per the Season 5 notes (S12); if a future patch changes the armor curve, tests A1/A2 fail loudly and the curve is re-derived.
9. The dodge rule of thumb conflict (official article says 10x area level rating gives about 50%, the curve says 29% at L100) is documented in 3.7; advice never prints absolute dodge chances, so the conflict is inert.
10. Ailment base damages (bleed 53 over 3 s, ignite 40 over 2.5 s, and so on, S11) are context only; no formula depends on them.
11. "Less damage taken" layers are assumed available on gear/skills at the stated size when used in conditional examples; the snapshot does not read them, so they never produce MEASURED advice.
12. Endurance threshold defaults to 20% of max health when not recorded; belt affixes can raise it, which only improves survival beyond our numbers.

## 9. Helper API map

`src/Core/MitigationMath.cs`, pure C#, no game references, all inputs percent points or plain numbers, NaN-safe:

- Constants: caps and per-stack values with source comments.
- `EnemyPenetration(areaLevel)`, `EffectiveResistance(uncapped, shred, pen)`, `ResistanceTakenFraction(eff)`, `ResistanceRatio(old, new, shred, pen)`.
- `ArmorMitigationFraction(armor, areaLevel)`, `ArmorMitigationNonPhysicalFraction(...)`, `ArmorRatio(old, new, areaLevel, nonPhysical)`; negative armor mirrored.
- `BlockMitigationFraction(blockEffectiveness, areaLevel)`, `DodgeChanceFraction(dodgeRating, areaLevel)`, both capped 0.85.
- `EnduranceApplied(damageToHealth, healthBefore, threshold, endurancePoints)`: the forward layer, including the official split example.
- `CritHitMultiplier(reducedBonusPoints)`, `EffectiveEnemyCritChance(enemyCritChance, avoidance)`.
- `WardDecayPerSecond(ward, retentionPoints)`.
- `RemainingEffectiveHealth(damage, overkill)`, `SurvivalBudgetRatio(damage, overkill)`, `CombineRatios(params)`.
- `PercentScaleOf(v)` / `AsPercent(v, scale)`: the never-guess unit heuristic for 41 vs 0.41 style inputs.
- Previews returning `Counterfactual` with `Sentence()`: `PreviewResistance`, `PreviewArmor` (refuses DoT), `PreviewCrit` (refuses non-crits), `PreviewLessDamageTaken`, `PreviewEndurance` (undo-then-reapply), `PreviewPoolIncrease`.
- `DamageMeaning` drives whether `Survived` is computed; `Counterfactual.Sentence()` implements the four canonical sentences of section 4.1, the borderline band and the pool phrasing.

Lane 4 (Advisor integration) should call the previews from the existing tips, keep the MEASURED/CONDITIONAL split of section 5, and is out of scope for this lane.

## 10. Sources

All retrieved and re-verified 2026-10-02. Official support articles carry Zendesk update dates; Maxroll pages carry "Last Updated"; forum threads carry post dates.

1. Last Epoch Support, Armor, updated 2026-07-13: https://support.lastepoch.com/hc/en-us/articles/46361891210651-Armor
2. Last Epoch Support, Endurance, updated 2026-02-13: https://support.lastepoch.com/hc/en-us/articles/46361855013787-Endurance
3. Last Epoch Support, Critical Strikes, updated 2026-02-13: https://support.lastepoch.com/hc/en-us/articles/46361891709211-Critical-Strikes
4. Last Epoch Support, Resistances, updated 2026-07-14: https://support.lastepoch.com/hc/en-us/articles/46361885661851-Resistances
5. Last Epoch Support, Penetrations, updated 2026-02-24: https://support.lastepoch.com/hc/en-us/articles/46361871026971-Penetrations
6. Last Epoch Support, Ward, updated 2026-07-14: https://support.lastepoch.com/hc/en-us/articles/46361876030235-Ward
7. Last Epoch Support, Dodge, updated 2026-07-14: https://support.lastepoch.com/hc/en-us/articles/46361899172379-Dodge
8. Last Epoch Support, "If 75% resistance is the max and enemies get penetration per level up to 75, do I need 150%?", updated 2026-02-12: https://support.lastepoch.com/hc/en-us/articles/46363284011419
9. Last Epoch Support, Common Terminology (capped/uncapped/overcap): https://support.lastepoch.com/hc/en-us/articles/46361668314523-Common-Terminology
10. Maxroll, Defenses Explained, updated 2026-04-02: https://maxroll.gg/last-epoch/resources/defenses-explained
11. Maxroll, Ailments Explained, updated 2026-04-02: https://maxroll.gg/last-epoch/resources/ailments-explained
12. Maxroll, Season 5 Patch Notes (1.5), posted 2026-09 (Season 5 launch 2026-10-01): https://maxroll.gg/last-epoch/news/season-5-patch-notes
13. Last Epoch 1.1.3 patch notes (death screen numbers), 2024-07: https://forum.lastepoch.com/t/last-epoch-patch-1-1-3-notes/73083
14. Tunklab formula implementations (armor, dodge, block, ward decay), retrieved 2026-10-02: https://lastepoch.tunklab.com/armor , https://lastepoch.tunklab.com/dodge
15. lastepochtools build planner snapshots: 1.0.3 Forge Guard (armor 2763 -> 46%, block 325 -> 22%): https://www.lastepochtools.com/planner/BgJrRZRQ ; 1.0 PTR Paladin (2976 -> 48%): https://www.lastepochtools.com/planner/zQq4lDYA ; 0.8.5f Paladin (3366 -> 52%): https://www.lastepochtools.com/planner/bBGyjdAV
16. Neoseeker, Combat Mechanics (in-game guide mirror, edited 2024-03-09; shred values, negative armor example, note it still showed the old 75% dodge cap): https://www.neoseeker.com/last-epoch/guides/Combat_Mechanics
17. Last Epoch Forums, "Dying almost only to stuff that goes through ward" (death screen damage = health lost + ward lost, with arithmetic), 2025: https://forum.lastepoch.com/t/dying-almost-only-to-stuff-that-goes-through-ward-and-its-depressing/76973
18. Last Epoch Forums, "Negative armor formula" (701 armor = 20% at L100 chart quote; symmetric shred graph), 2020-11: https://forum.lastepoch.com/t/negative-armor-formula/26045
19. Last Epoch wiki, Penetration (DoT benefits; damage formula `((1 - min(Resistance - shred, 75)) + penetration) * damage`): https://lastepoch.fandom.com/wiki/Penetration
20. Last Epoch Forums, "Does penetration work for dot builds?" (answer by Maxroll's ailments author: yes, pen is universal, shred needs a hit), 2021-12-23: https://forum.lastepoch.com/t/does-penetration-work-for-dot-builds/46800
21. Last Epoch Forums, "Increased Armor Shred Effect vs. maximum Shred stacks" (100 armor per stack, no cap, dev-confirmed on Discord), 2020-10: https://forum.lastepoch.com/t/increased-armor-shred-effect-vs-maximum-shred-stacks/25498
22. Last Epoch itemization tools (affix availability: endurance threshold on belts, armor + reduced crit combos): http://www.epochdps.com/