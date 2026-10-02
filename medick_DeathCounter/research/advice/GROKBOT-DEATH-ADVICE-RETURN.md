# Death advice return for Codex

Read this file first. It is the synthesis of the four research lanes plus the fact-check. The lane files stay in this folder so you can see what each one said. Where they disagree, this file wins, and `01b-MECHANICS-FACTCHECK.md` wins over `01-MECHANICS.md`.

Do not merge this branch to main. Do not install the mod. Do not publish a release. Apply the patch, or port by hand where your working tree has already moved.

## a) Base identity

Codex local snapshot: 2026-10-02 08:50 PT. The git commit that stores that snapshot is dated 2026-10-02 16:13:24 UTC.

Base branch: `cursor/advice-research-base`

Base SHA: `4b5341e5de9aa67c09e268ff30699165ba4b9ee4`

That base sits on `claude/last-epoch-death-counter-yspgnf` at `6790c86`. It is a research snapshot, not a branch to merge.

Combined branch: `cursor/advice-research-combined`

Synthesis commit (parent of the commit that adds the patch file): filled in section j once that commit exists. The branch tip is the child of that synthesis commit. The tip SHA is the head of the draft pull request. It is not repeated inside this paragraph, because writing the tip hash into the commit that creates the tip would change the hash.

Warning: your live tree is newer than 4b5341e. Do not overwrite it with this branch. If a file you have touched is also in the patch, port the change by hand. `src/Game` and `src/UI` are byte for byte the base. Nothing in those folders is in the research diff, so a patch apply will not revert hook or panel work you have done since the snapshot.

Lanes merged, all onto 4b5341e:

- Mechanics (Gemini), branch `cursor/advice-research-mechanics-e6a3`, commit `0eb0743`: `01-MECHANICS.md`.
- Fact-check (PR 9), branch `cursor/advice-research-factcheck`, commit `7689dcd`: `01b-MECHANICS-FACTCHECK.md`.
- Rules (Kimi), branch `cursor/advice-research-rules`, commit `b23caa8`: `02-RULES.md` and `02-RULES.json`.
- Math (GLM), branch `cursor/advice-research-math`, commit `61d76ec`: `03-MATH.md`, `src/Core/MitigationMath.cs`, 49 tests.
- Review (Grok), branch `cursor/advice-research-review`, commit `1619954`: `04-REVIEW-AND-WORDING.md`, Core fixes, 11 tests.

The only merge conflict was `tests/CoreTests/Program.cs`. Both the math lane and the review lane appended tests at the end. The resolution keeps every test from both. This synthesis then added one more test, `Test_Math_ResShred_PlayerStackCapIsTen`.

## b) Executive summary

The ten findings that should change what you port:

1. Season 5 is patch 1.5, Rage of the Frostborn, live 2026-10-01, with hotfix 1.5.0.1 the same day. Patch 1.1.3 is 2024-07-25. It is not Season 5. The July 2026 guide images predate the launch, and the official 1.5 patch-note page did not yield a readable body, so a Season 5 formula edit is still unchecked.

2. Armor, dodge, and block use the two-term guide formulas in `01b`, not the simple curves in `01-MECHANICS.md`. `MitigationMath` already implements those guide formulas. 2763 armor at area level 100 mitigates about 46% of a physical hit (0.4595). 1000 dodge rating at area level 100 is about 29.1% chance, not the "10 times area level is about 50%" rule of thumb.

3. Ward decay is the quadratic guide formula. 1000 ward at 40% retention decays at 208.33 per second. The old linear `0.4 * ward` formula is outdated. Endurance never reduces damage taken by ward.

4. Enemies penetrate 1% per area level, maximum 75%, after the 75% resistance cap. Overcap does not fight that penetration. Overcap only absorbs shred, Shock, and Marked for Death. Players do not gain area-level penetration.

5. Resistance shred against players and bosses is 2 points per stack, maximum 10 stacks, 4 seconds. The cap of 20 is pre-0.9. Poison shreds poison resistance by 2 points per stack on players and bosses, and only the first 30 stacks do that. Poison's own damage stacks are still unlimited.

6. Enemy crits use a 200% multiplier. Avoidance is rolled after the attacker's crit chance. 100% less bonus damage taken from crits turns the crit into a normal hit. Damage over time cannot crit.

7. Freeze chance uses max health plus current ward in the denominator. Cold resistance does not stop freeze. Do not ship the max-health-only formula from `01-MECHANICS.md`. Do not add a numeric freeze function yet: the guide example turns a 500% multiplier into a factor of 6, while the formula image multiplies by the multiplier directly.

8. Glancing blow reduces a hit by 35% and comes from Dusk Shroud. Silver Shroud does not grant it. Bosses use Boss Ward notches, not the old "2% damage reduction per 1% health lost" system. There is no official dodge-then-block order.

9. A gear change may be described with a damage ratio whenever the layer math is known. A survival claim ("you would likely have survived") is allowed only when the recorded damage is treated as the whole post-mitigation hit, ward included, and the new hit is clearly under the pool that was left. That reading of the death report is community evidence from 2025, not an official definition. The property names on `ReceiveDetailedDeathInfo` are not in the 1.1.3 notes.

10. The player card is one quote, at most three actions, then one confidence line. A capped resistance is not an action. Online defenses, the damage-type fallback, the crit-flag overwrite, the secondary type, the killer window, ward-only hits, a late report attaching to the wrong death, the HasPlayer drop, `IrregularDamageSourceID` and `IsBossfight`, and the theme colors (fire should be red) are still open. They live in `src/Game` and `src/UI`, which this branch does not change.

## Conflict resolutions

Each row is a place the lanes did not say the same thing. The winner is the fact-check when it ruled, otherwise the newest dated primary source.

**Endurance and ward.** Every lane already agreed, and the fact-check confirms it. Endurance reduces damage to health below the threshold. It does not apply to ward. It applies to hits and to damage over time. Cap 60% less damage. Everyone starts at 20% endurance and a threshold of 20% of max health. The threshold has no cap. A hit that crosses the threshold is split: only the portion below the line is reduced. Source: endurance article, updated 2026-02-20, https://support.lastepoch.com/hc/en-us/articles/46361855013787. `MitigationMath.EnduranceApplied` and `Advisor.EnduranceBody` already say this. No code change.

**Area-level penetration, formula and scope.** The 1% rule itself is not in dispute. Enemies gain 1% penetration per area level, maximum 75% at area level 75 and above. It applies after the 75% cap, so a capped resistance in a high area is 0%, and anything under the cap is negative. Overcap does not offset it. Players do not gain this penetration. Source: Penetrations article, updated 2026-02-24, https://support.lastepoch.com/hc/en-us/articles/46361871026971. Gemini cited a forum thread and dated it 2023-07. That thread is player commentary from 2023-11-25, not a dev post. Use the support article. Maxroll (2026-04-02) calls the level "enemy level, not your level." The official variable is area level. That difference is still an in-game check (section i). `EnemyPenetration` matches the official rule. Kimi's hard rule C3 lists penetration as a reason you may overcap, then the next sentence says overcap does nothing against penetration. Follow the second sentence, F2, and R-23. Do not overcap to fight area penetration.

**Armor formula.** Gemini's formula `1 - 1 / (1 + Armor / (area level + 1))` is wrong. At area level 100 and 1000 armor it is about 91%, then clamped to 85%. The guide formula at the same inputs is about 23.7% physical and about 16.6% for a non-physical hit. Guide image, armor article updated 2026-07-13: https://support.lastepoch.com/hc/en-us/articles/46361891210651 and image https://support.lastepoch.com/hc/article_attachments/52978116787995. Use coefficient 0.0015, not the older tools image that shows 0.0012, until a character sheet says otherwise. GLM's `ArmorMitigationFraction` is that guide formula. 2763 armor at area level 100 is 0.4595, which is the planner's 46%. That matches the fact-check and the spot check. Armor is hits only, 70% as effective against non-physical hits, and the physical result approaches 85% and does not pass it. Non-physical therefore tops out at 59.5% (85% times 70%; Maxroll states 59.5% directly, 2026-04-02). `01-MECHANICS.md` is superseded on this row.

**Dodge formula.** Gemini's formula is wrong. The guide image (Dodge article, updated 2026-07-14, https://support.lastepoch.com/hc/en-us/articles/46361899172379, image https://support.lastepoch.com/hc/article_attachments/53015724748059) is the two-term curve in `DodgeChanceFraction`. It approaches 85% and the page says chance cannot go above 85%. At area level 100, 1000 rating is about 29.1%. `Test_Math_Dodge_CurveAndCap` pins 0.2909. The same page's rule of thumb, "rating equal to 10 times the area level is about 50%," does not match the image. Kimi F10 repeats that rule of thumb. Do not quote 50%. The formula in code was already right. The comment now cites the guide image. No coefficient change.

**Block formula.** Gemini stated the 85% cap and did not print a formula. The fact-check prints the guide image (Block article, updated 2026-07-14, https://support.lastepoch.com/hc/en-us/articles/46361876155547, image https://support.lastepoch.com/hc/article_attachments/53015659036571). `BlockMitigationFraction` matches that image. 325 block effectiveness at area level 100 is 0.2214, which a planner shows as 22%. The 85% cap is the share of the hit removed when a block succeeds. Block chance is a separate additive chance. The guide does not cap block chance at 85%. A block still counts as a hit. Spells can be blocked. Damage over time cannot. No coefficient change. Do not pass the snapshot key `Block` (that key is block chance) into `BlockMitigationFraction` (that argument is block effectiveness).

**Freeze.** Gemini's formula, freeze rate times (1 + multiplier) divided by enemy max health, is wrong. The guide image (updated 2026-07-14, https://support.lastepoch.com/hc/en-us/articles/46361855307035) is `(freeze rate * freeze rate multiplier) / (max health + current ward)`. Current ward is in the denominator. Chill's freeze rate per stack is applied before the multiplier. Frostbite multiplies the final chance, and only the first 15 frostbite stacks add that chance (+20% each). Boss max health is treated as 50% higher. The page's own example turns "500% freeze rate multiplier" into a factor of 6, which is 1 + 5, while the image multiplies by the multiplier and the page says the stat defaults to 100%. Do not ship either numeric reading until section i, item 6, is done in game. Kimi F14 has the right denominator and the frostbite clause, and omits the multiplier. `Advisor` and `Ailments` say more max health and current ward lower the chance, and that cold resistance does not stop freeze. That wording matches the safe part of the fact-check. `MitigationMath` has no freeze function, on purpose. None was added.

**Stun.** Gemini adds ward and treats stun avoidance as extra health in one pool. That is wrong. The stun article (updated 2026-02-20, https://support.lastepoch.com/hc/en-us/articles/46361891772443) says a non-player hit must deal more than 5% of max health to be able to stun when increased stun chance and stun avoidance are both 0, and every 100 stun avoidance raises that threshold by 1% of max health. The current article does not mention ward. Players start at 250 + 5 stun avoidance per level. Base stun duration is 0.4 seconds. The formula image is missing from the page HTML, so do not invent the rest. Kimi F13 matches this. `DefenseSnapshot` no longer scales `StunAvoidance` as a percent. The stun tip talks about stun avoidance and a bigger health pool. It does not add ward. No further code change.

**Ward decay.** Gemini's linear formula is the old wiki and is outdated. The guide image (Ward article, updated 2026-07-14, https://support.lastepoch.com/hc/en-us/articles/46361876030235) is quadratic on ward above the ward decay threshold: `(0.00005 * excess^2 + 0.2 * excess) / (1 + 0.5 * retention)`. EHG published that shape on 2025-04-17. In the denominator, 100% retention counts as 1, because 200% retention then doubles how long ward lasts. `WardDecayPerSecond` divides retention percent points by 100, which is that reading. 1000 ward and 40% retention decay at 208.333 per second. 200% retention halves the decay versus 0% retention (125 versus 250 at 1000 ward). Both are pinned by `Test_Math_WardDecay_Formula` and match the spot check. The function does not subtract the threshold. The caller must pass the excess, `max(0, ward - threshold)`. `03-MATH.md` also says decay is halved when ward equals the threshold. The fact-check formula does not say that. At the threshold the excess is 0, so decay is 0. Do not add a separate half-at-threshold rule. Intelligence is 2% ward retention per point (Attributes article, updated 2026-07-14), not the 4% still printed on Maxroll.

**Glancing blow source.** Gemini says Dusk Shroud and Silver Shroud, and that old guides reduced the hit by 50%. Silver Shroud does not grant glancing blow. Dusk Shroud: "5% Chance to receive a Glancing Blow when hit." Silver Shroud: "Dodge your next hit and gain ward per stack." Source updated 2026-02-20, https://support.lastepoch.com/hc/en-us/articles/46361907080475. The reduction is 35%. Chance above 100% does nothing. A glancing blow still counts as a hit. No primary source was found for a former 50% damage reduction. Patch 1.0's "Glancing Blow chance: 50%" is a chance modifier, not the damage reduction. The official glancing-blow article does not say the stat is class-locked. Maxroll calls it rogue-only and names Dusk Shroud. Kimi F12 already says 35% and does not mention Silver Shroud. Code does not mention Silver Shroud. No change.

**Hit order.** Gemini's dodge, then block or glancing blow, then multiplicative resistance, armor, and endurance, is unsupported. The cited thread is players, dated 2023-06-24, with no dev post. Resistance, armor, block effectiveness, and glancing blow multiply, so order among those multipliers does not matter. Endurance is not one of those whole-hit multipliers. Dodge and block are rolls. Nobody has an official page that says which roll happens first. `03-MATH.md` already treats the multipliers as order-free and previews endurance last. Do not encode a dodge-then-block order.

**Enemy crit multiplier.** No real disagreement. Enemy hits that can crit use a 200% multiplier. Source: Critical Strikes article, updated 2026-02-20, https://support.lastepoch.com/hc/en-us/articles/46361891709211. Gemini's forum thread (2024-03) is players discussing the same rule. The support article is the primary source. 100% less bonus damage taken from crits is a hard cap and makes the crit deal a normal hit. Avoidance is rolled after the attacker's crit chance. The article's example: 30% enemy crit chance and 50% avoidance leaves 15%. Damage over time cannot crit. `CritHitMultiplier` and the crit tips match this. GLM's source list dates the same article 2026-02-13. Use the fact-check date, 2026-02-20.

**Death report damage and overkill.** The 1.1.3 notes (published 2024-07-25, https://forum.lastepoch.com/t/last-epoch-patch-1-1-3-notes/73083) added three facts to the death screen: how much damage the killing blow dealt, how much of that was overkill past 0 health, and whether the blow was a crit. They do not name `ReceiveDetailedDeathInfo`, `damage`, `overkillDamage`, or `wasCrit`. Gemini's table treats those property names as confirmed and labels the row "Patch 1.1.3 / Season 5." Both of those claims are unsupported. `ArgReader` still probes several crit property names. Keep doing that. GLM's interpretation A (damage is the whole post-mitigation hit, ward included, and overkill is the pool shortfall) comes from a 2025 forum thread that matched displayed damage to health lost plus ward lost. That is the best community reading, and it is not official. Every survival sentence in `Counterfactual.Sentence` already says so. Kimi marks the units unproven and downgrades the overkill rule until a probe compares the numbers to health and ward. `Advisor.HealthRemoved` goes further than the evidence: when a report has overkill, it subtracts overkill from the blow and divides by max health as if the remainder were health alone. If interpretation A is right, that remainder is ward plus health, and the percent of max health is wrong whenever ward was up. Leave the method as it is until section i is measured, and do not add new sentences that depend on it. See the bug list.

**Shred versus players.** Confirmed. Resistance shred is +5% negative resistance per stack, 60% less against players and 60% less against bosses, so 2 points per stack on a player or a boss. Maximum 10 stacks, 4 seconds. Applied by lowering the resistance stat, so it happens before the 75% cap and overcap absorbs it. Source: ailments article, updated 2026-02-20, https://support.lastepoch.com/hc/en-us/articles/46361887879963. The cut from 20 stacks to 10 is the 0.9 Convergence update. `03-MATH.md` still says maximum 20 stacks, from a March 2024 guide mirror. That sentence is superseded. `PlayerResShredMaxStacks` was 20 and is now 10. `Test_Math_ResShred_PlayerStackCapIsTen` pins 10 stacks and 2 points. Armor shred is a different ailment: +100 negative armor, unlimited stacks, 4 seconds. Do not reuse the 10-stack cap for armor shred. Shock on a player or boss is 2% lightning resistance per stack, maximum 10, 4 seconds, and it also raises stun chance. Marked for Death is -25% to all resistances, 8 seconds, 1 stack.

**Poison self-shred.** Confirmed, with one scope note Gemini understated. Poison's damage stacks are unlimited. Only the first 30 stacks apply the poison-resistance shred. The shred is 5% negative poison resistance, 60% less against players and 60% less against bosses, so 2 points per stack on players and on bosses. Same ailments article, updated 2026-02-20. Kimi and GLM already use 2 points and 30 stacks. The Advisor poison line says each stack lowers poison resistance and does not invent a 5% player number. When you attach a number, use 2 points and the first 30 stacks. `PoisonResShredPerStack` is 2 and `PoisonShredStacksThatCount` is 30.

**Season label and "1.5 changed no formulas."** Gemini's title pairs Season 5 with patch 1.1.3. That is wrong. Season 5 is patch 1.5, live 2026-10-01. Kimi and GLM say the 1.5 notes contain no core defense changes. The fact-check is stricter: the client-rendered patch-note page did not return a body, and nothing in the Season 5 forum thread discusses these formulas. Treat a Season 5 formula change as unchecked. Do not delete the July 2026 formulas, and do not tell the player the formulas were re-verified on launch day.

**Where Kimi, Grok, and GLM already agree with the corrected mechanics.** Resistances cap at 75% and work on hits and damage over time. Armor, dodge, block, and glancing blow do nothing to damage over time. Endurance does, on the health portion. Crit avoidance at 100% stops enemy crits. The Advisor no longer tells a frozen player to cap cold resistance, no longer prints stun avoidance as a percent, no longer calls an unknown max health "100% of your life," and quotes physical resistance, endurance, and armor rating when they were recorded. It does not print an armor mitigation percent, which is correct while area level is not recorded. It does not call `MitigationMath` yet. The counterfactual sentences are ready and unwired. Wire them only for a measured gap, and only claim survival under `DamageMeaning.PostMitigationWithWard`.

**Where the rules or the math notes still need a port-time edit, without a behavior change in this branch.**

- `02-RULES.md` hard rule C3: delete "or penetration" from the list of reasons to overcap. R-23 already says the right thing.
- `02-RULES.md` F10: delete the 50% dodge rule of thumb. Use the guide curve. Do not print a dodge percent until area level is known.
- `02-RULES.md` F14: add the freeze-rate multiplier, and do not compute a chance until the 500% question is tested.
- `03-MATH.md` section 3.1 and assumption 3: resistance shred max stacks is 10, not 20.
- `03-MATH.md` section 3.4: drop the "halved at the threshold" sentence. Decay is the quadratic on ward above the threshold.
- `03-MATH.md` section 3.7 and assumption 8: the dodge curve is the official July 2026 image, not a fallback chosen because the guide is stale. The rule of thumb on that page is the stale part.
- `03-MATH.md` opening and assumption 8: "patch 1.5 changed no core defence formula" is not confirmed. See the season row above.
- `Advisor.Show` is not Kimi's scored selector. See section d. Do not assume the weights already implement the rule ranks.
- `Advisor.HealthRemoved` must not gain new callers until the damage and overkill units are measured.

## c) Corrected mechanics table

`01-MECHANICS.md` is superseded on every row where `01b-MECHANICS-FACTCHECK.md` says WRONG, OUTDATED, or UNSUPPORTED. Those rows are armor formula, dodge formula, freeze formula, stun formula, ward decay formula, boss mitigation, glancing blow source, hit order, the Season 5 / 1.1.3 title, and the claim that the death-screen property names are confirmed. Rows the fact-check marks CONFIRMED may still be used, with the correction column in `01b` (dates, stack math, what the official page does not print). The table below is the set to code against. Confidence is High when an EHG support article states it, Medium when a second source is required for a number the article does not print, and Low when it still needs an in-game measurement.

**Resistance cap.** Behavior: resistances cap at 75%. A value above 75% does not by itself reduce damage. Units: percent points. Cap 75. URL: https://support.lastepoch.com/hc/en-us/articles/46361885661851. Date: updated 2026-07-14. Confidence: High. Implication: if the killing element is under 75, say how many points short. If it is at 75, do not recommend more of that resistance unless shred, Shock, or Marked for Death was recorded.

**Overcap.** Behavior: uncapped resistance above 75% absorbs resistance shred, Shock, and Marked for Death. It does not absorb area penetration. URL: same resistances article, and the 2026-02-12 FAQ https://support.lastepoch.com/hc/en-us/articles/46363284011419. Date: 2026-07-14 and 2026-02-12. Confidence: High. Implication: recommend overcap only when one of those three was on the player. Poison's own shred counts as resistance shred for poison.

**Area penetration.** Behavior: all enemies gain 1% penetration per area level, maximum 75%. Applied after the cap. Can push resistance negative. Players do not gain it. Units: percent points. At area level 75 or higher, penetration is 75. URL: https://support.lastepoch.com/hc/en-us/articles/46361871026971. Date: updated 2026-02-24. Confidence: High for the rule, Low for "area level versus monster level" until measured. Implication: in a high area, capping removes the negative-resistance penalty. It does not create a positive resistance against this penetration. Do not use character level as area level.

**Resistance shred.** Behavior: 5% negative resistance per stack, 60% less against players and bosses, so 2 points on a player or boss. Before the cap. 10 stacks, 4 seconds. At most 20 points stripped from a player. URL: https://support.lastepoch.com/hc/en-us/articles/46361887879963. Date: updated 2026-02-20. Confidence: High. Implication: 10 stacks of overcap headroom is the whole counter. Armor shred is not this ailment.

**Armor shred.** Behavior: +100 negative armor per stack, unlimited stacks, 4 seconds. The current article lists no player reduction. URL: same ailments article. Date: 2026-02-20. Confidence: Medium, because an older forum reading claimed a player reduction and no in-game check has settled it. Implication: say "about 100 armor per stack, no stack cap" and mark it medium. Do not say 2%.

**Armor mitigation.** Behavior: hits only. Not damage over time. 70% as effective against non-physical hits. The percent cannot exceed 85% physical, so 59.5% non-physical. Formula, x = armor, a = area level: `0.30 * (1.2x) / (80 + 0.05*(a+5)^2 + 1.2x) + 0.55 * (0.0015*x^2) / (180*(a+5) + 0.0015*x^2)`. URL: https://support.lastepoch.com/hc/en-us/articles/46361891210651. Date: updated 2026-07-13. Confidence: High for the July 2026 image, Low for whether 1.5 replaced 0.0015. Implication: quote the armor rating. Do not quote a mitigation percent unless area level is known. Never recommend armor for bleed or any other damage over time.

**Ward decay.** Behavior: ward above the decay threshold is lost each second by the quadratic formula. It does not decay below the threshold. 100% retention is 1 in the half-retention term. Intelligence grants 2% ward retention per point. Formula, excess = ward minus threshold, r = retention percent / 100: `(0.00005*excess^2 + 0.2*excess) / (1 + 0.5*r)`. URL: https://support.lastepoch.com/hc/en-us/articles/46361876030235. Date: updated 2026-07-14. The quadratic was published 2025-04-17. Confidence: High for the shape, Medium for the retention unit until a timed decay is measured. Implication: do not use the 0.4 linear formula. Do not tell a ward build to get endurance for the ward itself.

**Ward as a pool.** Behavior: ordinary damage, including ordinary damage over time, comes off ward before health. Some percent-of-current-health ground effects (Maxroll names Emperor of Corpses blood pools) ignore ward and cannot reduce health to 0. The official ward page does not mention that exception. URL: ward article above, and Maxroll defenses, updated 2026-04-02, https://maxroll.gg/last-epoch/resources/defenses-explained. Confidence: High that ward is in front of health, Medium for the ground-effect bypass. Implication: word damage-over-time advice as resistance, endurance on health, and sustain first. Ward is a pool, not a mitigation stat.

**Endurance.** Behavior: less damage taken on the part of a hit or a damage-over-time instance that lands on health below the threshold. Not on ward. Starts at 20%. Caps at 60%, multiplicative with other modifiers. Threshold starts at 20% of max health and has no cap. URL: https://support.lastepoch.com/hc/en-us/articles/46361855013787. Date: updated 2026-02-20. Confidence: High. Implication: quote the recorded percent and threshold. Say the cap is 60% and that it does not apply to ward. A ward-only hit does not get this tip as a survival claim.

**Critical strikes.** Behavior: enemy crit multiplier 200%. Avoidance is rolled after the attacker's crit chance and caps the practical chance at zero when avoidance is 100%. Less bonus damage taken from crits caps at 100% and removes the bonus, so the crit hits for normal damage. Damage over time cannot crit. URL: https://support.lastepoch.com/hc/en-us/articles/46361891709211. Date: updated 2026-02-20. Confidence: High. Implication: if the killing blow was a crit and not a damage-over-time ailment, this outranks a resistance gap. If avoidance was already 100% and the blow still crit, do not claim the stat worked. Look for Critical Vulnerability before saying the record is broken.

**Dodge.** Behavior: the guide curve above, approaching 85%. A dodge is not a hit and stops the hit's ailments. No effect on damage over time. 1000 rating at area level 100 is about 29.1%, not about 50%. URL: https://support.lastepoch.com/hc/en-us/articles/46361899172379. Date: updated 2026-07-14. Confidence: High for the image. Implication: talk about dodge rating, not a percent, until area level is known. Silver Shroud is "dodge your next hit," not extra dodge rating.

**Block.** Behavior: the guide curve above. The mitigated portion cannot exceed 85%. Block chance is separate and is not given an 85% cap. A blocked hit still counts as being hit. Spells can be blocked. Damage over time cannot. URL: https://support.lastepoch.com/hc/en-us/articles/46361876155547. Date: updated 2026-07-14. Confidence: High. Implication: "a blocked version of this hit" is a sometimes claim, never a flat save. Less damage taken from block, if you ever read it, multiplies with block effectiveness.

**Glancing blow.** Behavior: 35% less damage on that hit. Chance above 100% does nothing. Not applied to damage over time. The hit still counts. Dusk Shroud grants chance. Silver Shroud does not. URL: https://support.lastepoch.com/hc/en-us/articles/46361871332251 and the shroud article https://support.lastepoch.com/hc/en-us/articles/46361907080475. Date: glancing blow updated 2026-02-20, shrouds updated 2026-02-20. Confidence: High. Implication: do not tell the player Silver Shroud is glancing blow. The snapshot does not read glancing blow yet, so this stays a conditional example, not measured advice.

**Damaging ailments.** Behavior: Ignite, bleed, poison, damned, and electrify have no stack cap. Doom caps at 4. Time Rot caps at 12. Chill caps at 3, at 12% less attack, cast, and movement speed, with 50% less effect against players and bosses. URL: ailments article above. Date: 2026-02-20. Confidence: High. Implication: name the recorded ailment. Armor, dodge, block, and glancing blow do not reduce the damage over time. Resistance and endurance do. Ward is the pool in front.

**Poison shred.** Behavior: first 30 stacks only, 2 points of poison resistance per stack on players and bosses. URL: same ailments article. Date: 2026-02-20. Confidence: High. Implication: overcap is a measured lever only when stacks were counted. At 30 stacks the shred is 60 points, which is not a realistic overcap. Say cleanse or leave.

**Freeze.** Behavior: chance falls as max health and current ward rise. Cold resistance does not stop it. Frostbite's first 15 stacks raise the chance. The exact multiplier (500% as 6, or as the image's direct multiply) is not settled. URL: https://support.lastepoch.com/hc/en-us/articles/46361855307035. Date: updated 2026-07-14. Confidence: High for the denominator and for "cold resistance does not stop freeze." Low for a printed percent. Implication: the freeze tip is more max health and ward, plus a movement skill. A cold hit in the same death can still get a cold-resistance tip.

**Stun.** Behavior: threshold starts above 5% of max health and rises 1% of max health per 100 stun avoidance. Do not add ward. Do not add avoidance as flat health. Players start at 250 + 5 per level. URL: https://support.lastepoch.com/hc/en-us/articles/46361891772443. Date: updated 2026-02-20. Confidence: High for the threshold sentence, Low for the missing formula image. Implication: store stun avoidance as a rating. Never print it as a percent.

**Bosses.** Behavior: Boss Ward notches. A consumed notch grants ward based on max health and a short stun. That ward's decay speeds up. Not every boss has it. The old 2% reduction per 1% health lost is retired (Season 2, 2025-04-17). URL: https://support.lastepoch.com/hc/en-us/articles/46361900580379 and https://forum.lastepoch.com/t/endgame-balance-and-itemization-updates-coming-to-last-epoch-april-17th/75189. Date: article updated 2026-07-14. Confidence: High that the old system is gone, Low for the ward fraction and the decay rate. Implication: a boss flag means "the telegraph matters." Do not invent a boss mechanic, and do not tune advice around adaptive mitigation.

**Corruption.** Behavior: empowered timelines start at 100 corruption and have no maximum. They raise monster health, monster damage, experience, and item rarity. Standard timelines cap at 50. The 1.0 percentages (100 corruption = 60% more health and damage, and so on) were not reprinted on the 2026 support page, and Season 2 halved the damage-over-time portion of that bonus relative to hits. URL: https://support.lastepoch.com/hc/en-us/articles/46361874426523. Date: updated 2026-07-13. Confidence: High for the bounds and the direction, Low for any percent. Implication: do not hard-code the 1.0 curve.

**Layer math.** Behavior: resistance, armor, block effectiveness, and glancing blow multiply. Endurance applies only to the health portion below the threshold. There is no official dodge-then-block order. Confidence: High for the multipliers, Low for roll order. Implication: a combined ratio is the product of layer ratios. Preview endurance on the health split, not as another whole-hit multiplier.

**Death screen.** Behavior: since patch 1.1.3 (2024-07-25) the screen shows killing-blow damage, overkill past 0 health, and whether that blow crit. The IL2CPP property names are not in the notes. Units of damage and overkill are not officially defined. URL: https://forum.lastepoch.com/t/last-epoch-patch-1-1-3-notes/73083. Date: published 2024-07-25. Confidence: High for the three screen facts, Low for field names and units. Implication: print the numbers you stored. Do not say what they mean in health and ward until section i.

**Version.** Behavior: Season 5 is patch 1.5, hotfix 1.5.0.1, live 2026-10-01. URL: https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-is-now-live/81860 and https://forum.lastepoch.com/t/last-epoch-hotfix-1-5-0-1-notes/81887. Date: live 2026-10-01. Confidence: High. Implication: do not label 1.1.3 content as Season 5.

## d) Ranked rule matrix

Condensed from `02-RULES.md` and corrected where section b disagreed. Rank is the default order. A rule fires only when its required evidence is present and no exclusion matches. Missing optional evidence downgrades the wording. It does not invent a number.

The five hard constraints outrank every rank:

1. Never call a death a one-shot or a burst from the killing blow alone. Those labels need a hit timeline (`Hits > 0`).
2. Never assume a class, passive, item, or encounter mechanic without a recorded name.
3. Never recommend resistance above 75% unless shred, Shock, poison self-shred, or Marked for Death was recorded. Area penetration is not a reason to overcap. This corrects the "or penetration" clause in C3.
4. Never say a change would have prevented the death unless units, ward, remaining pool, modifiers, damage split, and timeline are all known. In practice, state the recorded fact and stop, or use a `MitigationMath` sentence that already carries its assumption.
5. When evidence is missing, say so.

1. R-01 crit-killing-blow. Fires when the killing blow was a crit and not a damage-over-time ailment. Advice: get critical strike avoidance to 100%, because enemy crits deal double. Confidence High.
2. R-02 res-gap. Fires when the killing element's resistance is known and under 75%. Advice: cap it. Under the cap is negative after area penetration. Confidence High.
3. R-03 shred-recorded. Fires when a recorded ailment strips that element's resistance, or armor on a hit. Advice: overcap that resistance, or remove the shred source. Confidence High. Armor shred is Medium.
4. R-04 dot-kill. Fires when the killing ailment is damage over time, or the window was at least half damage over time. Advice: resistance, endurance, sustain. Armor, dodge, and block do nothing. Confidence High.
5. R-05 overkill-exceeds-pool. Fires when overkill is at least max health and the units have been validated. Advice: that hit was larger than your full health. Confidence High, otherwise downgrade or skip.
6. R-06 irregular-source. Fires when the irregular source id is captured and is not "none". Advice: hazard or ground effect, so move, and treat defenses as damage over time. Confidence Medium.
7. R-07 oneshot-ehp. Fires when the timeline says OneShot and max health is known. Advice: bigger pool and endurance threshold. Confidence High.
8. R-08 burst-avoidance. Fires when the timeline says Burst and the window is not damage-over-time dominated. Advice: dodge rating and block chance. The pool buys time. Confidence High.
9. R-09 attrition-sustain. Fires when the timeline says Attrition, or a small overkill after many hits. Advice: leech, regen, potions. Confidence Medium.
10. R-10 res-capped. Fires when the killing resistance is already at 75% and no shred was recorded. Advice: resistance is not the gap. Pool and avoidance are. Confidence High.
11. R-11 crit-vulnerability. Fires when the blow crit, avoidance is at 100%, and Critical Vulnerability was recorded. Advice: that ailment breaks avoidance. Cleanse or avoid the applier. Confidence High.
12. R-12 freeze. Fires when Freeze, Chill, or Frostbite is on a cold death. Advice: bigger max health and ward lower freeze chance. Cold resistance is for the cold damage, not for the freeze. Confidence Medium.
13. R-13 stun. Fires when Stun was recorded. Advice: stun avoidance and max health raise the threshold. Do not add ward. Confidence Medium.
14. R-14 amplifier. Fires when Marked for Death or another recorded amplifier is present. Advice: name that ailment and its recorded counter. Confidence Medium.
15. R-15 low-pool. Fires when the pool is known and under the community floor for the level. Advice: raise health or ward toward that floor. Confidence Medium.
16. R-16 armor-for-physical. Fires when physical hit damage dominates, and the death is not bleed. Advice: armor rating is the physical hit layer. Confidence Medium.
17. R-17 no-avoidance. Fires on a hit death when dodge rating and block chance are both recorded at 0. Advice: add one avoidance layer. Confidence Medium.
18. R-18 endurance-gap. Fires when endurance is known and under 60% on a health hit. Advice: raise endurance toward 60% and raise the threshold. Confidence Medium.
19. R-19 repeat-killer. Fires when the same attacker appears in at least 3 of the last 20 deaths. Advice: it is a pattern, so learn the wind-up. Confidence High.
20. R-20 repeat-ability. Fires when the same ability appears in at least 3 of the last 20. Advice: that ability is the problem. Confidence High.
21. R-21 repeat-element. Fires when one element is at least half of recent death damage. Advice: shore up that resistance across deaths. Confidence Medium.
22. R-22 boss-context. Fires when `IsBossfight` is captured and true. Advice: telegraph discipline. Never invent a mechanic. Confidence Medium.
23. R-23 penetration-context. Fires when area level is known and at least 75. Advice: below-cap resistance is increased damage. Overcap does not fight penetration. Confidence Medium.
24. R-24 unknown. Fires when nothing else fired. Advice: not enough data, and how to turn on ProbeApi. No confidence label.

R-06 and R-22 cannot fire until `Capture` stores args 8 and 9. R-23 does not fire standalone until area level is recorded. Its sentence may ride inside R-02 without naming the current area's penetration. R-05 stays on the downgrade wording until section i validates units. R-11 and R-14 need ailment names the current `Ailments` table does not know (Critical Vulnerability, Marked for Death, and the other amplifiers). Add them by name, never by enum number.

### Selection algorithm

This is the target. `Advisor.Show` does not implement it yet. `Show` sorts `Suggest` by the existing weights, drops any title that contains "already capped," and keeps three. That behavior is tested. When you port the matrix, replace the weight sort with this selector and move the tests with it. Until then, do not pretend the weights are the ranks.

1. Evaluate every rule. A rule fires only when all required evidence is present and no exclusion matches.
2. Score a fired rule as `(100 - rank) + 2 * (number of required evidence fields beyond the first) + 3` if the wording quotes at least one recorded value. Rank dominates. Recorded numbers beat generalities.
3. Dedupe so two tips never say the same thing. Group `res:<element>` is R-02, R-03, and R-10 for that element (only one can fire). Group `crit` is R-01 and R-11. Group `dot` is R-04 and R-06 when the irregular source is damage over time. Group `pool` is R-05, R-07, and R-15. Group `pattern` is R-19, R-20, and R-21, and a single-death card keeps at most one pattern tip.
4. Sort by score descending, then rank ascending. Take three.
5. Never pad. Two rules produce two lines. Zero rules produce R-24. Do not fill with a generic resistance checklist.
6. The confidence line is always shown and does not count toward the three.

### Confidence line format

Target, from the rules, one line under the advice:

`Data: death report yes or no. Crit flag yes or no. Defences yes, partial, or no. Hit timeline yes (N hits) or no. Ailments yes or no. History N deaths. Confidence: High, Medium, or Low (one short reason).`

High: death report, defences, and timeline are all present, or the claim rests only on recorded values (a repeat killer from the log).

Medium: report and defences without a timeline, or timeline and defences without a report, or the threshold is community guidance (R-15).

Low: only one tier is present, or a recorded conflict exists (a crit on a damage-over-time ailment, ward remaining after a lethal hit, a crit at capped avoidance without Critical Vulnerability). Name the conflict.

What the code emits today, and what the tests lock, is the shorter `Advisor.Confidence` line:

- `High: game death report, hit timeline, and max health`
- `Medium: game death report and hit timeline, max health unknown`
- `Medium: game death report and max health, no hit timeline`
- `Medium: game death report, no hit timeline, max health unknown`
- `Medium: hit timeline and max health, no game death report`
- `Low: hit timeline, max health unknown`
- `Low: max health only, no hit timeline`
- `Low: no hit timeline, max health unknown`

High does not mean the element split is post-mitigation. It means those three inputs exist. Keep these strings until the panel calls `Show` and you update `Test_Confidence_NamesWhatIsMissing` in the same change. The target line adds crit flag, defences, ailments, and history, which the short line does not name.

## e) Counterfactual formulas and worked arithmetic

All of this is `MitigationMath` in `src/Core/MitigationMath.cs`. Ratios are safe under every reading of the damage number, because the layer multiplier cancels. `NewDamage` and `Survived` are computed only for `DamageMeaning.PostMitigationWithWard`. A result inside 10% of the remaining pool is "may or may not," even if the strict comparison says survived. Missing, NaN, or scale-ambiguous inputs refuse. A whole number that could be either 2 or 200% is refused (`PercentScaleOf`).

Shred is subtracted before the 75% cap. Penetration is subtracted after the cap. There is no floor, so effective resistance may be negative. Taken fraction = `1 - effective/100`. Enemy penetration = `min(75, max(0, area level))` percent points. Unknown area level refuses the resistance preview.

Armor, block, and dodge are the guide formulas in section c. Negative armor mirrors: mitigation of -x is the negative of mitigation of x. Armor on a damage-over-time death refuses with the reason string. Endurance is undone and reapplied, because the recorded hit already had the old endurance on it. The endurance preview assumes no ward was left, which is the smallest benefit. A crit that was not a crit refuses. 100% less bonus crit damage makes the multiplier 1. Partial avoidance is an odds sentence, not a smaller hit.

The worked numbers below are the ones the tests pin. "About" values in sentences are rounded to tens.

### Fire resistance 41 versus 75

Recorded damage 1700, overkill 700, fire resistance 41, area level 100, no shred. Remaining pool = 1000. Penetration = 75.

Effective now = min(41, 75) - 75 = -34. Taken = 1.34.

Effective at the cap = min(75, 75) - 75 = 0. Taken = 1.00.

Ratio = 1.00 / 1.34 = 0.7463. New hit = 1700 * 0.7463 = 1268.7, about 1270.

1270 is still above the about 1000 left, so it would probably still have killed you. Capping removes the penalty. It does not make a positive resistance against 75 penetration.

Same hit with penetration forced to 0, which is a comparison and not a real area: ratio = 0.25 / 0.59 = 0.4237. New hit = 720.3, about 720, which is under 1000, so you would likely have survived. The verdict flips when penetration is ignored. That is why area level is required.

Area level 40, penetration 40: ratio 0.6566, new hit about 1116, still dead.

If the damage number is treated as pre-mitigation, the ratio is still 0.7463 and `Survived` is null. The sentence must not claim survival.

### Capped fire, lethal hit

Damage 2200, overkill 400, fire resistance already 75, area 100, max health 2000, endurance 20, threshold 400. Remaining = 1800.

Raising fire resistance, even to 150 uncapped, has ratio 1.0. Penetration after the cap zeroes it. The resistance line is "already capped," and it is not an action.

Endurance 20 to 60: the part above the threshold is 1400. The part below, after the old 20%, is 800, which was 1000 before endurance. New hit = 1400 + 1000 * 0.40 = 1800. Ratio 0.8182. New hit about 1800 versus about 1800 left, inside the 10% band, so it may or may not have been enough.

Pool: the overkill of 400 is the shortfall under interpretation A. About 400 more health and ward is the borderline case. 500 would absorb this exact hit. 200 would not. More max health also raises the endurance threshold, so 400 is the smallest requirement, and the note says so.

Block effectiveness 325 at area 100 removes 0.2214 of a blocked hit. 2200 * (1 - 0.2214) = 1713, about 1710, under the about 1800 left. The wording is that block would sometimes have saved you, not that it did.

### Ignite at the cap

Killing tick 350, overkill 30, fire resistance 75, area 100. Remaining = 320. Armor preview refuses: armor does not mitigate damage over time. Dodge, block, and crit avoidance do not apply. Overcap without a recorded fire shred has ratio 1.0.

A 15% less damage over time layer makes the tick 297.5, about 300, under the about 320 left. Other ignite stacks were still ticking, so one tick surviving is not the death surviving. Say "may or may not" for the death.

With 10 evidenced resistance-shred stacks (20 points, the player rate at the 10-stack cap), overcap to 95 restores effective 0 instead of -20. That is a measured case. Do not use 20 stacks. The player cap is 10.

### Bleed

Tick 300, overkill 40, physical resistance 20, armor 2763, area 100. Remaining = 260. Armor refuses even though the ailment is physical.

Effective now = min(20, 75) - 75 = -55. Taken = 1.55. Capped taken = 1.00. Ratio = 0.6452. New tick = 193.5, about 194, under the about 260 left for this tick. Bleed keeps ticking, so the death verdict keeps the caveat. Physical resistance is scarce on gear. Say that next to the number.

### Poison stacks

Tick 900, overkill 100, poison resistance 75 uncapped, 10 poison stacks counted, area 100. Remaining = 800.

Shred = 10 * 2 = 20, before the cap. Effective now = min(75 - 20, 75) - 75 = -20. Taken = 1.20.

Overcap to 95: min(95 - 20, 75) - 75 = 0. Taken = 1.00. Ratio = 0.8333. New tick = 750, about 750 versus about 800 left, inside the band, so it may or may not have been enough for this tick. This overcap is measured because the stacks were on you. Also say kill the poisoner, cleanse, or leave.

At 30 stacks, shred is 60. Effective = 15 - 75 = -60. You would need 135 uncapped to pin 0. That is not the advice. Avoidance and cleanse are. Without evidenced stacks, overcap ratio is 1.0 and the rule must not fire.

### Crit

Damage 2000, overkill 600, was a crit, avoidance 0, reduced bonus 0. Remaining = 1400. Enemy crits deal 200%, so the normal-hit version of this blow is 1000.

100% critical strike avoidance: ratio 0.5, new hit about 1000, under about 1400, so you would likely have survived, assuming the recorded damage was the whole post-mitigation hit including ward. This is the strong survival case.

Reduced bonus crit damage of 50: multiplier = 1.5. Ratio = 0.75. New hit = 1500, still above 1400, so it would probably still have killed you. At 100 reduced bonus, multiplier = 1.0, new hit = 1000, survives.

Partial avoidance of 60 against a 5% enemy crit chance: effective chance = 2%. Say "60 of every 100 such crits would have been normal hits." Do not shrink this hit. Damage over time never gets this lever.

### Other pinned checks, short

Armor 2763, 2976, and 3366 at area 100: 0.4595, 0.4820, 0.5199. Armor 701 at area 100: 0.1920. Huge armor caps at 0.85. Non-physical 2763 is 0.3217. Armor -700 mirrors to about -0.1919.

Armor 2763 to 4000 at area 100, on a physical hit of 1800 with 300 overkill: ratio 0.7904, new 1422.7, inside the band versus 1500 left. The same armor step plus endurance 20 to 60 lands at combined ratio 0.7007, new about 1260, which is a likely survival. The same armor step on a non-physical hit is only ratio 0.8831. Armor is a weak lever against elemental hits, and the number should say so.

Ward: 1000 ward at 40 retention is 208.333 per second. 500 ward at 0 retention is 112.5 per second. Pass excess over the decay threshold, not raw ward, when the threshold is not zero.

A hit entirely above the endurance threshold has ratio 1. The official split: 100 damage with 50 below the threshold at 20% endurance deals 90. Overkill greater than damage, or negative overkill, refuses. Missing damage still yields the ratio sentence and no new hit number.

## f) Regression test list

Command: dotnet run, with the project file `medick_DeathCounter/tests/CoreTests`.

Result on this branch: 117 tests, all passed, exit code 0. SDK 8.0.425. The runner counts every private static method whose name starts with `Test_`.

56 of these existed on the base. 49 came from the math lane. 11 came from the review lane. 1, the shred cap, was added in this synthesis. The merge kept all 49 and all 11.

### Math lane, plus the shred-cap pin

- `Test_Math_Armor_MatchesPlannerDataPoints`: 2763, 2976, 3366 armor at area 100 are 0.4595, 0.4820, 0.5199.
- `Test_Math_Armor_MatchesTunklabChartQuote`: 701 armor at area 100 is 0.1920.
- `Test_Math_Armor_CapsAt85`: huge armor caps at 0.85.
- `Test_Math_Armor_NonPhysicalIs70Percent`: 2763 at area 100 is 0.3217 against non-physical.
- `Test_Math_Armor_NegativeMirrorsPositive`: negative armor mirrors.
- `Test_Math_Armor_ZeroAndNaN`: zero armor is 0, NaN stays NaN.
- `Test_Math_Block_MatchesPlannerDataPoint`: 325 block effectiveness at area 100 is 0.2214.
- `Test_Math_Block_CapsAt85`: huge block effectiveness caps at 0.85, zero is 0.
- `Test_Math_Dodge_MatchesPlannerDataPoint`: 16 dodge rating at area 100 is 0.0062.
- `Test_Math_Dodge_CurveAndCap`: 1000 rating at area 100 is 0.2909, huge rating caps at 0.85.
- `Test_Math_Penetration_ByAreaLevel`: area 40, 75, 100, 120 give 40, 75, 75, 75. NaN stays NaN.
- `Test_Math_ResShred_PlayerStackCapIsTen`: player shred cap is 10 at 2 points, poison shred is 2 points for 30 stacks, shock cap is 10. This is the synthesis pin. `03-MATH.md` still says 20.
- `Test_Math_Resist_EffectiveOrderShredCapPen`: shred before the cap, penetration after. 41 uncapped with 75 penetration is -34.
- `Test_Math_Resist_NegativeGrowsDamage`: effective -34 takes 1.34.
- `Test_Math_Resist_OvercapClampedAtCap`: 150 uncapped with no penetration is still 75.
- `Test_Math_Resist_ShredBelowZero`: shred can push a low resistance negative before penetration.
- `Test_Math_ExampleA_Fire41_Capped_WithPen`: 1700/700, 41 to 75, area 100, ratio 0.7463, still dead.
- `Test_Math_ExampleA_WordingStillKilled`: the sentence says still above the about 1000 left.
- `Test_Math_ExampleA_Fire41_NoPen_Survives`: penetration 0, ratio 0.4237, survives.
- `Test_Math_ExampleA_Fire40Area_StillDies`: area 40, ratio 0.6566, still dead.
- `Test_Math_ExampleA_PreMitigationNoSurvivalClaim`: same ratio, `Survived` is null.
- `Test_Math_RatioInvariantToDamageMeaning`: the ratio does not depend on the damage reading.
- `Test_Math_ExampleB_CappedResistanceIsADeadEnd`: 75 to 150 uncapped at area 100 is ratio 1.
- `Test_Math_ExampleB_EnduranceMayOrMayNot`: endurance 20 to 60, ratio 0.8182, borderline sentence.
- `Test_Math_ExampleB_PoolShortfallIsOverkill`: +400 is borderline, +500 absorbs, +200 does not.
- `Test_Math_ExampleB_BlockedVersionSurvives`: block effectiveness 325, blocked hit about 1713, under 1800.
- `Test_Math_ExampleC_ArmorRefusesDot`: armor preview refuses on damage over time.
- `Test_Math_ExampleC_LessDotTakenTick`: 15% less on a 350 ignite tick is 297.5, with the multi-tick caveat.
- `Test_Math_ExampleD_BleedPhysicalRes`: physical resistance 20 to 75, ratio 0.6452, armor not involved.
- `Test_Math_ExampleE_PoisonOvercapMeasured`: 10 stacks, 75 to 95, ratio 0.8333, borderline.
- `Test_Math_ExampleE_OvercapNeedsEvidencedShred`: shred 0, overcap ratio is 1.
- `Test_Math_ExampleF_AvoidanceHalvesCrit`: a crit at 200% becomes ratio 0.5 and survives.
- `Test_Math_ExampleF_ReducedBonusCrit`: 50 reduced bonus is ratio 0.75 and still dead.
- `Test_Math_ExampleF_EffectiveCritChance`: 5% enemy crit and 60% avoidance is 2%.
- `Test_Math_Crit_NonCritRefuses`: a non-crit refuses the crit preview.
- `Test_Math_ExampleG_SmallOverkillThinMargin`: 1050/50, survival budget 0.952.
- `Test_Math_ExampleG_LargeOverkillNeedsAvoidance`: 5000/3950, budget 0.21.
- `Test_Math_ExampleH_MissingMaxHealth`: endurance refuses without a threshold, survival math does not need max health.
- `Test_Math_ExampleH_HealthOnlyMeaning`: health-only reading nulls survival and keeps the ratio.
- `Test_Math_ExampleI_ArmorRatio`: 2763 to 4000, ratio 0.7904, borderline.
- `Test_Math_ExampleI_ArmorPlusEndurance`: that armor step plus endurance 60, combined 0.7007, survives.
- `Test_Math_ExampleI_ArmorWeakVsElemental`: the same armor step on a non-physical hit is ratio 0.8831.
- `Test_Math_Endurance_AboveThresholdNoEffect`: a hit entirely above the threshold has ratio 1.
- `Test_Math_Endurance_OfficialSplitExample`: 100 damage with 50 below the threshold at 20% endurance deals 90.
- `Test_Math_Endurance_FullyBelowThreshold`: health at or below the threshold takes endurance on the whole instance.
- `Test_Math_WardDecay_Formula`: 1000 ward at 40 retention is 208.333 per second. 200 retention halves decay versus 0 retention.
- `Test_Math_Units_NeverGuessAmbiguousScale`: 0.41 is 41%, 41 is 41%, 2 is refused.
- `Test_Math_MissingFields_AlwaysRefuse`: NaN inputs never become a number.
- `Test_Math_Overkill_OutOfRange`: overkill above damage, or negative, refuses.
- `Test_Math_MissingDamage_RatioOnlyWording`: no damage number still yields the percent sentence.

### Review lane

- `Test_Defenses_StunAvoidanceIsRatingNotResistance`: stun avoidance 750 stays 750 while fire 0.75 becomes 75 and crit avoidance 0.4 becomes 40.
- `Test_AilmentFind_RejectsStatCompounds`: Shockwave, FreezeRate, and SlowRetaliation are not ailments. Shock and Frozen still match.
- `Test_Advice_UnknownMaxHealthDoesNotClaimFullLife`: a single hit with no max health does not say 100% or "you out."
- `Test_Advice_BurstWithoutMaxHealthDoesNotClaimFullLife`: a burst with no max health does not say 100%.
- `Test_Advice_PhysicalHitQuotesResistanceAndEndurance`: physical resistance 40, endurance 20, threshold 400, and armor 1800 are quoted. Armor is a rating. The endurance tip does not say "belt."
- `Test_Advice_FreezeDoesNotSayCapCold`: freeze does not say to keep cold resistance capped. It says cold resistance does not stop freeze.
- `Test_Advice_ShowSkipsCappedAndStopsAtThree`: at most three tips, and a capped notice is not one of them.
- `Test_Advice_ShowFallbackWhenOnlyCapped`: a report-only capped fire death shows one health, ward, and endurance action, not the capped title.
- `Test_Quote_UsesOnlyRecordedFields`: quote uses ability, attacker, amount, type, and crit. A second type is not a 50/50 split. An empty record says the killing blow was not recorded.
- `Test_Confidence_NamesWhatIsMissing`: the eight short confidence lines in section d.
- `Test_DeathText_KeepsShortHexAndCyan`: `#f00` becomes `#ff0000`, and `cyan` is `#13A8C9`.

### Base tests that must stay green

These pin the analyzer, the log, the ledger, and the advice that already shipped on the snapshot. Do not drop them while porting.

- `Test_GameReport_BleedDoesNotRecommendArmorForItsDamage`: a bleed report does not recommend armor.
- `Test_GameReport_EnrichesWithoutInventingTimeline`: a report fills killer, ability, elements, damage, and crit, and does not invent hits.
- `Test_GameReport_PreservesObservedHitClassification`: an explicit crit false and a Burst classification survive a report. This is why a missing crit box must not be stored as false without a hook change that still passes this test.
- `Test_LateGameReport_PersistsSameRecordAndCount`: a late report updates the same row and does not add a second death.
- `Test_CounterReset_PersistsPerCharacterAndKeepsHistory`: counter resets are per character and do not wipe the log.
- `Test_AilmentFind_MatchesGameishNames`, `Test_AilmentFind_IgnoresStatNames`, `Test_AilmentFind_GameAilmentIdNames`, `Test_AilmentFind_ExactIdBeforeSubstring`: ailment name matching, including exact id before substring.
- `Test_Ledger_CounterLagsLongAfterHook`, `Test_Ledger_UnexplainedIncreaseRecords`, `Test_Ledger_FirstReadIsBaseline_AndCreditsExpire`, `Test_Ledger_ResetOnCharacterChange`: the death-count ledger.
- `Test_OneShot_BigFireHit`: one hit at least 70% of max health is a one-shot.
- `Test_Burst_ManyHitsFast`: at least 80% of max health inside 2 seconds is a burst.
- `Test_Dot_PoisonTicks`: poison ticks classify as damage over time.
- `Test_Attrition_SlowGrind`: a slow grind is attrition.
- `Test_KillingDotTick_NamesAilment`: the tick at the death time is the named killer, even after a larger earlier hit.
- `Test_NoHits_IsUnknown`: no hits is Unknown.
- `Test_OldHitsOutsideWindowIgnored`: hits outside the window do not count.
- `Test_UnknownMaxHp_StillClassifies`: unknown max health still receives a shape label. The advice must not turn that label into "100% of your life." The label itself is still open. See bug 2.
- `Test_HitBuffer_PrunesByTime`, `Test_HitBuffer_Capped`: the hit buffer.
- `Test_Advice_FireOneShotCrit`: a fire crit one-shot recommends crit avoidance and fire resistance.
- `Test_Advice_BleedMentionsArmorDoesNotHelp`: bleed says armor does not help.
- `Test_Advice_PhysicalHitSaysArmor`: a physical hit recommends armor.
- `Test_Advice_FreezeFromAilmentTap`: an ailment tap can raise the freeze tip without a damage timeline.
- `Test_Advice_Nemesis`: three deaths to the same killer raise the pattern tip.
- `Test_Advice_UnknownDeathExplainsItself`: an unknown death tells the player the hits were not seen.
- `Test_Advice_ShockIsLightningResShred`: Shock is lightning resistance and stun, not a general damage amp.
- `Test_Advice_DamnedMentionsRegenCut`: Damned cuts health regen.
- `Test_Advice_UsesYourResistance`: a known gap quotes your number.
- `Test_Advice_CappedResistanceSaysSo`: 75% is described as capped.
- `Test_Advice_UsesYourCritAvoidance`: a known avoidance percent is quoted.
- `Test_Advice_NoSnapshotKeepsGenericText`: no snapshot keeps the generic "check your sheet" line.
- `Test_Log_DefensesRoundTrip`: the defense dictionary survives the log.
- `Test_Defenses_PercentUnits`, `Test_Defenses_FractionUnits`, `Test_Defenses_WholeSmallValuesAreNoSignal`, `Test_Defenses_ConflictingEvidenceReportsNothing`, `Test_Defenses_ImplausiblePercentDropped`, `Test_Defenses_NegativeResistanceKept`: percent scale. Ambiguous small whole numbers are dropped. Negative resistance is kept.
- `Test_Advice_CapThresholdEdges`: the 74.5 boundary around the 75 cap.
- `Test_Advice_CrittedDespiteFullAvoidanceDoesNotClaimImmunity`: a crit at 100% avoidance does not say the stat worked.
- `Test_Advice_FreezeTipKnowsColdIsCapped`: a freeze tip can coexist with capped cold resistance without telling the player to cap cold for the freeze.
- `Test_DefenseLine_MarksCapAndWardTiming`: the defense line marks the cap, and ward is described as after the hit.
- `Test_Patterns_TopKillersAndDamageShare`, `Test_Patterns_PrioritiesCountDeathsTheyWouldHaveHelped`, `Test_Patterns_UnknownDeathNeverAPriority`, `Test_Patterns_CountsEveryTipNotJustTopThree`, `Test_Patterns_ElementSharesWeighEachDeathEqually`, `Test_Patterns_PriorityBodyDescribesThePattern`, `Test_Patterns_AilmentTipFoldsIntoItsResistance`, `Test_Patterns_WindowIsMostRecent`, `Test_Patterns_Empty`: pattern report. Each death counts once. An ignite tip folds into fire resistance. The window is the most recent deaths.
- `Test_Log_RoundTripAndNumbering`: per-character numbers, a torn JSON line warns once and does not drop the history.

## g) Bug list

Status is either fixed in Core on this branch, or open with a proposed patch. Hook and UI files were not edited. Line numbers are the review's readings of the base snapshot. Re-check them in your tree before applying.

### Fixed in Core on this branch

**1. High. Stun avoidance was scaled as a percent.** Fixed in `DefenseSnapshot.Normalize`. `PercentKeys` is Block, Endurance, and CritAvoidance only. `StunAvoidance` is copied through. Pinned by `Test_Defenses_StunAvoidanceIsRatingNotResistance`.

**2. High. Unknown max health was worded as a full life.** Fixed in `Advisor.Suggest` for OneShot and Burst. The body no longer says "100%" or "you out" when `MaxHealth` is missing. Pinned by `Test_Advice_UnknownMaxHealthDoesNotClaimFullLife` and `Test_Advice_BurstWithoutMaxHealthDoesNotClaimFullLife`. The analyzer label is still open. See open item 2.

**3. High. The element split was described as the killing damage.** Fixed in `Advisor.Suggest`. The sentence now says the mix is the pre-mitigation mix rescaled onto health lost. The numbers are still pre-mitigation. The panel heading is still open. See open item 3.

**4. High. Freeze and chill told the player to cap cold resistance.** Fixed in `Advisor.Suggest`, `Advisor.PatternBody`, and `Ailments` effect text for Freeze and Chill. Cold resistance does not stop freeze or chill. A cold hit can still get a cold-resistance tip. Pinned by `Test_Advice_FreezeDoesNotSayCapCold`.

**5. Medium. Physical resistance and endurance were ignored when the snapshot had them.** Fixed in `Advisor.ResTip`, `Advisor.EnduranceBody`, and `Advisor.ArmorNote`. Armor is quoted as a rating, with no mitigation percent. Endurance says it caps at 60% and does not apply to ward. Pinned by `Test_Advice_PhysicalHitQuotesResistanceAndEndurance`.

**6. Medium. A capped resistance took a recommendation slot.** Fixed in `Advisor.Show`. Capped notices stay in `Suggest` for the log and the tests. `Show` drops them, keeps at most three, and if nothing else remains it returns one health, ward, and endurance action that says endurance does not apply to ward. Pinned by `Test_Advice_ShowSkipsCappedAndStopsAtThree` and `Test_Advice_ShowFallbackWhenOnlyCapped`. The panel still calls `Suggest`. See open item 6.

**7. Medium. Ailment names matched by substring.** Fixed in `Ailments.Find`. Token match. Shockwave, FreezeRate, and SlowRetaliation do not match. Pinned by `Test_AilmentFind_RejectsStatCompounds` and the older find tests.

**8. Low. Short hex and named colors were stripped.** Fixed in `DeathText.Parse`. 3-digit hex expands (`#f00` becomes `#ff0000`). `cyan` is stored as `#13A8C9` and `teal` as `#29AB85`. Pinned by `Test_DeathText_KeepsShortHexAndCyan`. Theme palette is still the old one. See open item 18.

**9. Player resistance shred cap was 20.** Fixed in this synthesis, in `MitigationMath.PlayerResShredMaxStacks`, now 10. `03-MATH.md` still says 20. Pinned by `Test_Math_ResShred_PlayerStackCapIsTen`. Dodge and block formulas were checked against `01b` and left in place. Freeze was not given a numeric function.

### Open, with a proposed patch

**2b. Medium. Unknown max health is still classified as a one-shot.** Open. `DeathAnalyzer.Analyze`. `Test_UnknownMaxHp_StillClassifies` locks the label, so do not change it without updating that test. The advice body is already safe. When a report arrives, `DeathDetails.Apply` should downgrade `OneShot` to `Reported` only when `MaxHealth <= 0` and `Hits <= 1`. Do not downgrade a Burst, and do not downgrade a OneShot that had a real max health. `Test_GameReport_PreservesObservedHitClassification` must stay green.

**3b. Medium. The panel prints the type split with no caveat.** Open, UI. `DeathPanel` around the element rows. Proposed heading: "Health lost by type, pre-mitigation mix." Do not invent a mitigated split. Not edited.

**6b. Medium. The panel does not call `Show`.** Open, UI. `DeathPanel` still calls `Advisor.Suggest`. Proposed call: `Advisor.Show(d, _records)`, then drop the `unknown` key if you replace a hidden unknown tip with the fallback sentence. Also needs `Quote` and `Confidence` on the card. That is a layout change. Not edited.

**8. High. Online defenses are never read.** Open, hook. `PlayerProbe.Defenses`. If `ProtectionClass` is null the method returns null. Nothing calls `PlayerFinder.getLocalPlayerPrecalculatedStatsHolder`. There is no `PrecalculatedStatsHolder` in the 2023 dump, and RCInet's public refs for 1.4 still use `getPlayerActor` plus `ProtectionClass`. The names below are the 2023 `ProtectionClass` names to try by reflection. They are not a guaranteed Season 5 field list.

Proposed shape, inside `Defenses` only. Do not hardcode `SP` enum integers. The enum grew (for example `IncreasedAreaForAreaSkills` in 1.4).

```csharp
var prot = Refl.Get(Actor, "protection") ?? Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ProtectionClass"));
var holder = Refl.Static("Il2Cpp.PlayerFinder", "getLocalPlayerPrecalculatedStatsHolder");
if (prot == null && holder == null) return null;
object src = prot ?? holder;
```

If both exist, read `prot` as today and log holder disagreements once under `ProbeApi`. Do not average them. Try each name with `Refl.Get` / `Refl.TryFloat`. On the first successful holder read, if `ProbeApi` is on, log every field and property name and type once.

- `Res.Physical`: try `uncappedPhysicalResistance`, `physicalResistance`. Percent, cap 75. Keep raw overcap on `ResUncapped.Physical`.
- `Res.Fire`: try `uncappedFireResistance`, `fireResistance`. Same percent rules.
- `Res.Cold`: try `uncappedColdResistance`, `coldResistance`. Same.
- `Res.Lightning`: try `uncappedLightningResistance`, `lightningResistance`. Same.
- `Res.Necrotic`: try `uncappedNecroticResistance`, `necroticResistance`. Same.
- `Res.Void`: try `uncappedVoidResistance`, `voidResistance`. Same.
- `Res.Poison`: try `uncappedPoisonResistance`, `poisonResistance`. Same.
- `Endurance`: try `endurance`. Percent. The reduction caps at 60. Do not clamp the stored value at 75.
- `EnduranceThreshold`: try `enduranceThreshold`. Flat health, not a percent.
- `CritAvoidance`: try `critAvoidance`. Percent. Drop if the scaled value is above 100.5.
- `Block`: try `blockChance`. Percent, additive. This is chance, not effectiveness.
- `BlockEffectiveness`: try `blockProtection`. Rating. 2023 name, inferred. Not a percent.
- `Armor`: try `armour`, `armor`, else `armourForCharacterSheet()`. Rating. Not a percent.
- `Dodge`: try `dodgeRating`. Rating. Do not store `dodgeChance()`. That method needs area level.
- `Ward`: try `CurrentWard`, `currentWard`. Flat. This is after the hit if you read it at death. Say so.
- `StunAvoidance`: try `stunAvoidance`. Rating. Never a percent key.
- `GlancingBlow`: try `glancingBlowChance`. Percent, optional.
- `MaxHealth`: player health, not the holder. Flat. `PlayerProbe.MaxHealth` is already the source.

If a member looks like less bonus crit damage (`lessBonusDamageTakenFromCrits` or similar), log it and do not clamp it onto a resistance. Misses stay missing. A missing key is better than a made-up 0.

**9. Medium. DamageType fallback uses the 2023 order.** Open. `ArgReader.ElementMap.Build`. When the enum type is missing it returns `{ 0, 1, 2, 3, 4, 5, 6 }`. If Season 5 inserts a value, or the arrays are in a different order, every element total is mislabeled and the advice caps the wrong resistance. Proposed: return `{ -1, -1, -1, -1, -1, -1, -1 }`. `ToOurOrder` already skips negative slots. Log the failure. When the type loads, keep the name-based map.

**10. Medium. An unreadable crit flag is stored as not a crit.** Open. `DeathReportHooks.Capture` sets `Crit = args[2] is bool crit && crit`. `DeathDetails.Apply` always assigns `record.KillingCrit = Crit`. `KillingCrit` is `bool?`. False means the game said it was not a crit. A boxed Il2Cpp bool that fails `is bool` becomes false and overwrites a timeline true. The player then loses the crit tip. `Test_GameReport_PreservesObservedHitClassification` requires an explicit false to overwrite, so Apply must keep writing when the hook really saw a bool.

Proposed hook: `bool? crit = args[2] is bool b ? b : (bool?)null`, and pass it only when non-null. Proposed `DeathDetails`: change `Crit` to `bool?`, and assign `KillingCrit` only when `Crit.HasValue`. An explicit false still overwrites. A missing box does not. `Quote` already appends ", crit" only when `KillingCrit == true`.

**11. Medium. Secondary type, and primary type, are easy to drop.** Open. `DeathReportHooks.ElementName` parses `value.ToString()` with `Elements.TryParse`. A numeric `1`, or a string like `DamageType.FIRE`, fails and the element is stored null. Secondary type is kept only when `Refl.Get(args[1], "HasValue", "hasValue")` is true. An Il2Cpp `Nullable<DamageType>` often does not expose `HasValue` that way, so the second type disappears.

Proposed `ElementName`: if the runtime type is an enum, use `Enum.GetName`. If `ToString` contains a dot, parse the segment after the last dot. Log the runtime type the first time parsing fails. Proposed secondary read: try `HasValue` as a bool, and also try `GetValueOrDefault` or a non-null `Value`. If the nullable cannot be reflected, leave `SecondaryElement` null and say so in the confidence line. Do not split the damage number in half.

**12. Medium. The named killer is the last event in the window, which extends past the death.** Open. `DeathAnalyzer.Analyze` takes killer, ability, element, crit, and blow from the last hit in the window. The window includes hits up to 0.5 seconds after `deathTime`. A tick in that grace period replaces the name. The Kill flag in `GameHooks` detects death and is not stored on `HitEvent`. When the report arrives, `DeathDetails.Apply` overwrites non-empty strings. Timeline-only deaths keep the analyzer's choice.

Proposed, without breaking `Test_KillingDotTick_NamesAilment`: use the last hit with `Time <= deathTime`, and fall back to the last hit in the window. Keep post-death ticks in the damage totals. A later change can store the Kill flag on the hit and prefer that hit.

**13. Medium. Ward-only hits never enter the timeline.** Open. `GameHooks.HitPostfix`. Health lost at or below 0 returns immediately. The comment says dodge, block, or ward. A hit that only breaks ward is invisible. A hit that breaks ward and then health is recorded as the health piece alone. The report's damage is a different quantity from the sum of timeline amounts. No hook edit here. Wording rule: if `DetailSource` is set, the quoted blow is the report. The timeline is "other hits in the last few seconds," not a second killing blow.

**14. Medium. A late report can attach to the wrong death.** Open. `DeathTracker.OnDeath` and `OnDeathDetails`. The enrichment path does not add a second row. `Test_LateGameReport_PersistsSameRecordAndCount` and the ledger tests lock that. The residual race is different. `_details` is applied to `JustDied` when the character matches and the death was within 5 seconds. Two deaths inside that window can take each other's killing blow. A stale packet can also be applied to the next commit inside 5 seconds.

Proposed: stamp the report with the pending death id or `_pendingUtc`, and Apply only when the report time is within 0.75 seconds of that death's `_deadAt`, not 5 seconds of the latest death.

**15. Medium. An online report is dropped when `HasPlayer` is false.** Open. `DeathReportHooks.Capture` returns unless `PlayerProbe.HasPlayer`. `OnDeathDetails` checks again. `HasPlayer` needs a character name and either a live object pointer or local-player health. The network hook can fire for `getLocalActorSync` while `HasPlayer` is false, and the packet is discarded.

Proposed: if the network filter already matched `getLocalActorSync`, store the `DeathDetails` even when the actor object is null, as long as a character name resolved. Do not require `ProtectionClass`. Creating the death record can still wait for health or the game counter.

**16. Low. `IrregularDamageSourceID` and `IsBossfight` are not captured.** Open. `DeathReportHooks.Capture` reads args 0 to 7 only. The 10-arg network report has the irregular source id at arg 8 and the boss flag at arg 9. The 9-arg local report's last argument is unmapped. Proposed: one field each on `DeathDetails`, read in `Capture`, and say in the confidence line when the none encoding is unknown. Do not name a specific hazard from the id. Do not invent a boss mechanic from the flag. R-06 and R-22 wait on this.

**17. Low. `Advisor.HealthRemoved` treats damage minus overkill as health.** Open, Core, left in place on purpose. `Advisor.HealthRemoved`. Used by the one-shot percent when a report has overkill. The comment says ward lost earlier is not in the number. Under interpretation A the remainder is the whole pool, ward included. Do not add callers. After section i, either divide by the pool or stop using the percent. No test pins the overkill subtraction, which is why it was not changed in this branch.

**18. Low. Theme colors. Fire should be red.** Open, UI. `Theme.ElementColors` is still Physical `#C8B9A6`, Fire `#E2683C` (orange), Cold `#6FB7E8`, Lightning `#E8D04A` (yellow), Necrotic `#4FB0A0`, Void `#9B6BD6`, Poison `#7BC043`. Not edited. Proposed constants are in section h. `Theme.GameColor` parses 6-digit and 8-digit hex and does not know the names cyan and teal. `DeathText` now expands those before they reach Theme.

## h) Wording spec, color palette, and 10 examples

The card is three pieces, in this order. Plain sentences. Commas and periods. No double dashes and no em dashes in player text.

1. Quote. One sentence, only from fields that are set. `Advisor.Quote`.
2. Recommendations. `Advisor.Show` today, the selector in section d once you port it. At most three. The title is the action. The body is one or two short sentences and includes the recorded number. A capped resistance is not one of the three.
3. Confidence. One line. The short form is what ships today. The long form is the target.

Quote shape, when the fields exist: `Killed by {ability or ailment} from {attacker}, {damage} {type} damage, crit, {overkill} overkill.`

Ability wins over ailment, ailment wins over attacker alone. ", crit" only when the flag is true. Two types are named, and the number is not halved: `800 fire and physical damage`. Overkill is included only when it is greater than 0. It is damage past 0 health, not a second hit. If nothing was recorded: `The killing blow was not recorded.` Do not say one-shot, and do not say a percent of life, inside the quote.

Each recommendation body must contain the recorded fact it used. A resistance gap quotes the sheet value and the points to 75. Armor quotes the rating and does not invent a percent. Endurance quotes the percent and the threshold when they were read, says the cap is 60%, and says it does not apply to ward. A crit quotes avoidance when it was read and says enemy crits deal double. Freeze and chill do not say to cap cold resistance. A repeated killer (3 or more) can be one of the three only when specific defenses are missing.

If every tip was a capped notice, the card is one action: raise health, ward, and endurance, because the killing resistance was already at 75%, and endurance does not apply to ward. If the record supports nothing, say there is no supported change. An unknown death that the hooks missed still uses the probe tip, because it tells the player why the card is empty. Do not hide that tip and leave a blank.

The card must not say:

- A percent of life when max health was not recorded.
- One-shot when max health was not recorded, or when the only evidence is a single timeline hit.
- A mitigated type split. Say "pre-mitigation mix" or omit the percent.
- A 50/50 split of one reported damage number.
- An armor or dodge percent without area level.
- "Cap cold resistance" as the fix for freeze or chill.
- "Cap this resistance" when the snapshot shows it at 75% or above (74.5 rounds to the capped branch).
- Stun avoidance as a percent.
- "Damage taken increased" for ordinary Shock. Shock is lightning resistance and stun chance.
- Ground, unless the ability or ailment text itself says ground, lava, pool, or similar. The irregular source id is not verified.
- A boss damage type the report did not name.
- Online defenses that were not read. Say they were not readable. Do not print zeros.

### Color palette

Text color follows the word on the character sheet, not the skill effect and not the icon. Sampled from the Maxroll defenses sheet image `resistances-new.png` (https://assets-ng.maxroll.gg/wordpress/resistances-new.png), article updated 2026-03-26. Column order: Fire, Lightning, Cold, Physical, Poison, Necrotic, Void. Percent digits are the same warm off-white in every column (`#F3E8DF` at the top of that band). They are not type-colored.

Use the word highlight. Do not use the letter body or the icon highlight for UI text.

- Fire: `#D24322`. The word is red. The flame icon's brightest pixel is yellow (`#FAF52B`). Using the icon would make fire look orange or yellow.
- Lightning: `#1F58C8`. The word is blue. In-world bolts can still be yellow. UI text follows the word.
- Cold: `#13A8C9`.
- Physical: `#DEC29F`. Warm tan. Not dark red.
- Poison: `#18933B`.
- Necrotic: `#29AB85`. Teal. A separate necrotic symbol remeasured in the same teal family.
- Void: `#6F17A4`.

Proposed `Theme.ElementColors` replacement, not applied:

```csharp
Hex(0xDEC29F),   // Physical
Hex(0xD24322),   // Fire, red
Hex(0x13A8C9),   // Cold
Hex(0x1F58C8),   // Lightning, blue
Hex(0x29AB85),   // Necrotic
Hex(0x6F17A4),   // Void
Hex(0x18933B),   // Poison
```

`DeathText` already stores cyan as `#13A8C9` and teal as `#29AB85` so a recap tag can paint before Theme learns the names.

### Ten example outputs

These are the cards the player should read. Quotes and confidence lines match `Advisor.Quote` and `Advisor.Confidence` for the record described. The three lines match `Advisor.Show`, shortened only where the quote already carries the type.

**1. Fire hit, resistance under the cap.** Timeline, max health 1000, last hit 900 fire from Lagon, crit, fire resistance 41, no report.

> Killed by Lagon, 900 fire damage, crit.
>
> Cap fire resistance: you had 41%. You were 34 points short of the 75% cap.
>
> Get critical strike avoidance to 100%. The killing blow was a critical strike, which hits for double.
>
> Raise your effective health pool. The last recorded hit removed 90% of your max health.
>
> Medium: hit timeline and max health, no game death report.

**2. Capped fire, lethal hit.** Report plus timeline, max health 1400, blow 1600 fire, overkill 200, fire resistance 75, not a crit.

> Killed by Lagon, 1,600 fire damage, 200 overkill.
>
> Raise your effective health pool. Health removed is 1,400 after overkill is taken out. Treat that percent as provisional until the units in section i are measured, because the remainder may include ward.
>
> Get endurance and endurance threshold. Endurance was 20%. Endurance threshold was 280. The reduction caps at 60% and does not apply to ward.
>
> High: game death report, hit timeline, and max health.

Capped fire resistance is not listed. Raising it does nothing until something shreds it.

**3. Ignite.** Ignite ticks, fire resistance 52, last tick 80 fire from a Fire Wraith, max health 1000, no report. Armor is not the tip.

> Killed by Ignite from Fire Wraith, 80 fire damage.
>
> Cap fire resistance: you had 52%. You were 23 points short of the 75% cap.
>
> Ignite: fire resistance. Ignite deals fire damage over time. Keep health regen or leech up.
>
> Out-heal damage over time. Do not say ground from the ignite id alone.
>
> Medium: hit timeline and max health, no game death report.

**4. Bleed.** Bleed ticks, physical resistance 30, armor 2200, max health known, no report.

> Killed by Bleed from Bandit, 40 physical damage.
>
> Cap physical resistance: you had 30%. You were 45 points short of the 75% cap. Armor does not reduce bleed.
>
> Bleed: physical resistance. Armor does not reduce bleed.
>
> Out-heal damage over time. Endurance does not apply to ward.
>
> Medium: hit timeline and max health, no game death report.

**5. Poison.** Poison ticks, poison resistance 60 with uncapped 90, max health known.

> Killed by Poison from Spider, 36 poison damage.
>
> Cap poison resistance: you had 60%. You were 15 points short of the 75% cap. The 90% before the cap is headroom against shred, and it does not raise the cap.
>
> Poison: poison resistance. Each poison stack also lowers your poison resistance.
>
> Out-heal damage over time.
>
> Medium: hit timeline and max health, no game death report.

If poison resistance is already 75, drop the cap action. The overcap is headroom against the first 30 stacks only, at 2 points per stack.

**6. Critical physical hit.** Report, Brute, 2000 physical, crit, max health 1500, physical resistance 40, armor 1800, crit avoidance 25, endurance 20, threshold 400, timeline present.

> Killed by Brute, 2,000 physical damage, crit.
>
> Get critical strike avoidance to 100%. You had 25% critical strike avoidance. Enemy crits deal double.
>
> Cap physical resistance: you had 40%. You were 35 points short of the 75% cap.
>
> Stack armor. Recorded armor was 1,800. That is a rating. Mitigation also depends on area level, so no mitigation percent is stated.
>
> High: game death report, hit timeline, and max health.

Endurance is the fourth fact and stays off the card unless one of the three is missing.

**7. Void, report only.** Void Nova from Void Horror, 1234 void, Time Rot, crit, overkill 100, no timeline, no max health.

> Killed by Void Nova from Void Horror, 1,234 void damage, crit, 100 overkill.
>
> Cap void resistance (75%). The personal void resistance was not readable, so the gap is not stated.
>
> Time Rot: void resistance. It also makes stuns on you last longer.
>
> Get critical strike avoidance to 100%. The killing blow was a critical strike, which hits for double.
>
> Medium: game death report, no hit timeline, max health unknown.

Do not call this a one-shot. Overkill says the blow went past 0 health. It does not say the fight was one hit.

**8. Necrotic attrition.** Timeline, Wraith, necrotic, Damned on you, necrotic resistance 10, max health known, no report.

> Killed by Wraith, 60 necrotic damage.
>
> Cap necrotic resistance: you had 10%. You were 65 points short of the 75% cap.
>
> Damned: necrotic resistance. It also cuts your health regen, so lean on leech.
>
> Improve sustain. You were worn down over several seconds.
>
> Medium: hit timeline and max health, no game death report.

**9. Ground effect, named by the ability.** Ability text "Lava Pool," fire, 400, fire resistance 70, max health known, report present, no timeline.

> Killed by Lava Pool, 400 fire damage.
>
> Cap fire resistance: you had 70%. You were 5 points short of the 75% cap.
>
> Move out of the lava pool. The pool is named because the ability text says so.
>
> Medium: game death report and max health, no hit timeline.

If the ability is only "Fireball," do not say ground.

**10. Missing data, and the online case next to it.** Empty record:

> The killing blow was not recorded.
>
> No hit data for this death. The mod did not see the hits, so it cannot tell what to fix.
>
> Low: no hit timeline, max health unknown.

Online, same numbers as example 7, `ProtectionClass` null, holder not called:

> Killed by Void Nova from Void Horror, 1,234 void damage, crit, 100 overkill.
>
> Cap void resistance (75%). Your resistances were not readable online, so this does not use your sheet.
>
> Time Rot: void resistance.
>
> Get critical strike avoidance to 100%.
>
> Medium: game death report, no hit timeline, max health unknown.
>
> Defenses: not readable.

Do not print zeros. Do not say the player is uncapped.

## i) What must be measured in game

The July 2026 support images predate the 2026-10-01 launch. Open the Season 5 patch notes in a browser and search for armor, dodge, block, ward, endurance, freeze, stun, and corruption. If none of those sections change a formula, the July images still stand. The four procedures below are the ones the advice engine cannot ship numbers for until you run them. The rest of the fact-check's list follows.

**1. Damage and overkill units.** Unequip gear so ward and endurance are minimal. In a low-level zone, let one enemy hit you for a known amount, then let them kill you. Log every argument of `PlayerActorSync.ReceiveDetailedDeathInfo`. Compare the damage argument with health actually lost. Question: is damage the final health lost (post-mitigation), the whole post-mitigation hit, or the roll before armor and resistances? The 1.1.3 notes confirm the screen, not the answer. Until this is done, R-05 stays on the downgrade wording, and no new sentence may treat `Advisor.HealthRemoved` as health.

**2. Ward inside overkill.** Generate about 1000 ward with about 100 health. Let one hit kill you for a blow larger than both. Record health lost, ward lost, the damage argument, and the overkill argument. Question: is overkill `blow - (health + ward)`, or `blow - health` with ward left out? Repeat a damage-over-time death that eats ward first, then a small killing blow. If displayed damage equals health lost plus ward lost, interpretation A stands and `HealthRemoved / MaxHealth` must stop. If damage equals health lost only, interpretation A is too generous and the math survival claims must switch to `PostMitigationHealthOnly`.

**3. Secondary type.** Die to a dual-element attack, a bleed or poison tick, and a ground effect. Log the runtime type of args 0 and 1, including `ToString`, enum name, and whether `HasValue` or `Value` reflects. Question: does the secondary argument populate on dual-element skills, or only for some ailments, and does the current `HasValue` check drop a real second type? Do not split one damage number across the two types in any of these logs.

**4. Irregular source id.** On the same three deaths, log arg 8 (`IrregularDamageSourceID`) and arg 9 (`IsBossfight`) from the 10-arg report, and log the last argument of the 9-arg local report. Question: what value means none, what value means a ground effect or a damage-over-time source, and is the boss flag true only for the fights that have Boss Ward? Do not name a hazard from an id you have not seen.

**5. Area level versus monster level.** In an echo, read the area level on the map, find a monster whose level differs, and compare the character sheet's armor and dodge percents with the formulas using each level. Sheet values follow whichever input the game uses. Do not use character level as a proxy.

**6. Armor coefficient 0.0015 versus 0.0012.** At a known area level and armor total, compute both. The July 2026 image is 0.0015. Prefer the value that matches the sheet. 2763 armor at area 100 should read about 46% if 0.0015 is still live.

**7. Ward retention units.** Gain a round amount of ward retention, for example 100% on the sheet. With threshold 0, time a large ward pool. If 100% retention makes the denominator 1.5, the sheet percent is divided by 100 before the formula, which is what `WardDecayPerSecond` does. If the pool barely moves, the formula is being fed the raw percent and this reading is wrong.

**8. Ward and percent-of-current-health ground damage.** On a ward build, stand in a pool the tooltip describes as a percent of current health, such as Emperor of Corpses blood. If health falls while ward does not, Maxroll's bypass is still in the game.

**9. Freeze multiplier.** Take a skill whose tooltip shows a known freeze rate and a known freeze rate multiplier. Freeze a normal enemy with known health and no ward, and a boss. Check whether 500% on the tooltip behaves as times 6, as in the guide example, or as times 5. Include a target that has ward and confirm the denominator grew by that ward.

**10. Stun threshold.** Level 1 already has 255 built-in avoidance, so a clean zero is hard. Compare two hits of known size against the same health, one with extra avoidance and one without, and repeat with a large ward pool. If ward does not change the stun, the current support article is right. Each 100 extra avoidance should demand about 1% more of max health.

**11. Corruption percents after Season 2.** Read the corruption tooltip in an empowered echo at 100 and at 200. If it still says 60% more health and damage at 100, record whether damage over time is half of that.

**12. Boss Ward numbers.** Record health when a notch breaks, the ward gained, and how fast that ward falls. That replaces the retired 2% reduction rule.

**13. Crit property name.** On a crit death and a non-crit death, log which argument is the crit flag and its runtime type. Check `wasCrit` and the other names `ArgReader` already probes. Do not collapse a failed read to false.

## j) Gate results and file manifest

SDK installed with the official dotnet-install script, channel 8.0. Version: 8.0.425.

Tests: dotnet run, with the project file `medick_DeathCounter/tests/CoreTests`.

117 tests, all passed. Exit code 0.

Build: `dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror`

Build succeeded. 0 Warning(s). 0 Error(s). Time about 5 seconds on the first restore. Assembly: `medick_DeathCounter/bin/Release/net6.0/medick_DeathCounter.dll`. The project still targets net6.0. The 8.0 SDK compiled it. `DeployToMods=false` kept the DLL out of any game folder. Nothing was installed into Last Epoch. Nothing was published.

`src/Game` and `src/UI` have no diff against `4b5341e`. Hook files and panel files are unchanged.

Synthesis commit (the parent of the patch file): see the sentence under the patch hash below. It is written when the patch is generated, because the hash covers this document as of that commit.

### Patch

`GROKBOT-DEATH-ADVICE.patch` in this directory is the full diff of `medick_DeathCounter` from `4b5341e5de9aa67c09e268ff30699165ba4b9ee4` to the synthesis commit. The path limit is the `medick_DeathCounter` directory. The file is binary-safe in the sense that `git diff` emitted it and no binary files are in the diff.

The sha256 below is of that patch file. The patch does not contain itself. Applying the patch onto a tree whose `medick_DeathCounter` matches `4b5341e` reproduces the synthesis commit. On the branch tip, the two lines below are filled in. The copy of this file inside the patch is the synthesis commit's copy, so those two lines there still show the unfilled markers. Trust the copy on the branch tip, or the pull request body. Your newer files outside that snapshot, especially anything you have changed in `src/Game` or `src/UI`, should be ported by hand rather than overwritten.

Synthesis commit: `0b01440b500776aa2ce0fbe73f2d90af1ad5dbf8`

Patch sha256: `3b389f149a28e437cf90ed51ca65295de615948011e2bac478f8c3e7babcb722`

### File manifest

Against the base, the synthesis tree changes only these paths. The patch file is the extra file on the branch tip.

- `medick_DeathCounter/research/advice/01-MECHANICS.md` (Gemini. Superseded where 01b disagrees. Left in place so the fact-check table still has a subject.)
- `medick_DeathCounter/research/advice/01b-MECHANICS-FACTCHECK.md` (authoritative verdicts, 2026-10-02)
- `medick_DeathCounter/research/advice/02-RULES.md` and `02-RULES.json` (Kimi. Correct the three sentences named in section b when you port.)
- `medick_DeathCounter/research/advice/03-MATH.md` (GLM. Formulas for armor, dodge, block, and ward decay match 01b. Shred cap 20, the dodge rule-of-thumb framing, the half-at-threshold sentence, and the "1.5 changed nothing" sentence do not.)
- `medick_DeathCounter/research/advice/04-REVIEW-AND-WORDING.md` (Grok. Bug list, wording, palette, examples.)
- `medick_DeathCounter/research/advice/GROKBOT-DEATH-ADVICE-RETURN.md` (this file)
- `medick_DeathCounter/research/advice/GROKBOT-DEATH-ADVICE.patch` (branch tip only)
- `medick_DeathCounter/src/Core/MitigationMath.cs` (new)
- `medick_DeathCounter/src/Core/Advisor.cs`
- `medick_DeathCounter/src/Core/DefenseSnapshot.cs`
- `medick_DeathCounter/src/Core/Elements.cs`
- `medick_DeathCounter/src/Core/DeathText.cs`
- `medick_DeathCounter/tests/CoreTests/Program.cs`

Not changed: every file under `medick_DeathCounter/src/Game` and `medick_DeathCounter/src/UI`, including `PlayerProbe.cs`, `DeathReportHooks.cs`, `ArgReader.cs`, `GameHooks.cs`, `DeathTracker.cs`, `DeathPanel.cs`, and `Theme.cs`.
