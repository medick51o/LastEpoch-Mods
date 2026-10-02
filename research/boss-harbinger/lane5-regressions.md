# Lane 5 regressions for CoreTests

Specifications for Codex. This lane did not add them to `tests/CoreTests` and did not change `src/`. Each case lists inputs and expected outputs. Use the existing helpers (`Near`, `Eq`, `True`) and a tolerance of 0.0001 on ratios.

Shared fixture, not a shipped boss:

```text
Profile Parent
  ResearchStatus Partial
  FollowUpId "fixture-harbinger"
  Components:
    fire-hit: Element Fire, Delivery Hit, CanCrit false, Evidence DataminedReference, CountsAsVerified true
    unknown-move: Element null, Delivery Unknown, CanCrit null, CountsAsVerified false

Profile Harbinger
  ResearchStatus Partial
  FollowUpApplies n/a (it is the follow-up)
  Components:
    void-hit: Element Void, Delivery Hit, CanCrit null, InheritedFromMoveId null,
              Evidence DataminedReference, CountsAsVerified true
    own-untyped: Element null, Delivery Unknown, CanCrit null, CountsAsVerified false

Death D, unless a case says otherwise
  Character "Test"
  UtcTime 2026-10-02 18:00:00Z
  Killer "Fixture Boss"
  IsBossFight true
  Kind Reported
  DetailSource "game death report"
  KillingElement "Fire"
  KillingCrit false
  KillingBlow 1700
  OverkillDamage 700
  MaxHealth 1000
  Hits 0
  DefenseSnapshotAgeSeconds 0
  Defenses: Res.Fire 41, AreaLevel 100, MaxHealth 1000
  AilmentsOnYou empty
```

`FollowUpApplies` is true in cases that say the timeline variant is known. Current stats are the reassessment read, not a write into `D.Defenses`.

Player-facing strings in expected bodies must not contain two consecutive hyphens.

## Recorded cause versus the fight

### L5-01. Capped killing element is not a fight A

Inputs: `D` with `Res.Fire` already 75 and `KillingCrit` false. Current stats: `Res.Fire` 75, `Res.Void` 75, `MaxHealth` 1400. Timeline variant known, so the Harbinger profile applies. Both profiles still Partial.

Expected:

- Recorded-cause grade A is allowed only when today's A checks pass (fire not worse, crit flag present, no unresolved non-pattern action on the death alone). Scope contains `not a whole-fight survival probability` and `Recorded cause only`.
- Encounter grade is B, not A. Verdict or reason contains both `untyped` and `Harbinger`.
- The fight line's grade is B.
- `D.KillingElement` remains `Fire`. Serialized `D` is unchanged after the encounter call.

### L5-02. Parent cap does not clear a Harbinger void gap

Inputs: current stats `Res.Fire` 75, `Res.Void` 40, `MaxHealth` 1400. Follow-up applies.

Expected:

- Encounter grade C.
- Reason contains `void resistance remains below 75%`.
- Reason does not contain `100%` and does not contain `survived`.
- First non-notice action key `res_Void`, group `res:Void`, weight 100.
- No second card in group `res:Fire`. The fire capped result is a notice or absent, not an action.
- No `ehp` action.
- Body contains `35 points` and `area level 100` and `26% less void`.
- Body contains `was not the recorded killing blow`.
- Body does not contain `overcap` and does not contain two consecutive hyphens.

Ratio check, shred 0, penetration 75: `ResistanceRatio(40, 75, 0, 75)` equals `1/1.35` (0.7407407407). `100 * (1 - ratio)` formats as `26`.

### L5-03. Unknown Harbinger element blocks A after the void gap is closed

Inputs: current `Res.Fire` 75, `Res.Void` 75, `MaxHealth` 1400. Follow-up applies. `own-untyped.Element` is null. `void-hit.CanCrit` is null.

Expected:

- Encounter grade B, not A, not C.
- Reason says verified resistances are capped and at least one Harbinger attack is untyped.
- No action demands a made-up element.
- No crit action is added solely because `CanCrit` is null.

### L5-04. Julra's three published elements are requirements, and still not an A

Inputs: real `julra` profile. Death killer `Julra`, `IsBossFight` true, `KillingElement` `Void`, fresh `Res.Void` 75, crit false. Current: `Res.Void` 75, `Res.Cold` 75, `Res.Lightning` 40, `MaxHealth` 1200.

Expected:

- Encounter grade C. Reason contains `lightning resistance remains below 75%`.
- Action `res_Lightning` exists. No action asks for fire, physical, necrotic, or poison.
- `ObservedTypes` is only Void. The catalog does not write Cold or Lightning onto the death.

Repeat with Lightning also 75.

Expected: encounter grade B, not A. Reason says the published list is not an attack-by-attack map (the profile's evidence string already says this).

### L5-05. Empty DamageTypes is not "no threats"

Inputs: real `lagon` profile. Death killer `Lagon`, `IsBossFight` true, `KillingElement` `Lightning`, fresh and current `Res.Lightning` 75, crit false, `MaxHealth` raised from 1000 to 1400.

Expected:

- `DamageTypes` count is 0.
- Encounter grade is not A. Grade B or `?`. Reason says attack typing is not verified.
- Recorded-cause grade may be A. Fight line is not A.
- No card types Moon Blast, Tidal Wave, or Lightning Blast as an element.

### L5-06. Ability name does not type Harbinger of Hatred

Inputs: real `harbinger-hatred` profile. Killer `Harbinger of Hatred`, `IsBossFight` true, `KillerAbility` `Void Rahyeh Dive Bomb`, `KillingElement` null. Current `Res.Void` 0, `MaxHealth` 2000.

Expected:

- Encounter verified elements do not include Void.
- No `res_Void` action.
- Grade is not A.
- `Match` is the hatred profile. Killer `Harbinger of Hatred's Void Rahyeh` still matches nothing.

### L5-07. Unknown follow-up does not import Harbinger resistances

Inputs: parent profile with `FollowUpApplies` null. Current `Res.Fire` 75, `Res.Void` 10.

Expected:

- No `res_Void` action.
- Encounter grade is not A.
- Reason says whether a Harbinger follows this attempt is unknown.
- `D.Killer` unchanged.

### L5-08. Campaign variant does not take the timeline Harbinger

Inputs: same parent, `FollowUpApplies` false. Current fire 75, void 10.

Expected:

- No Harbinger void card.
- Incomplete flag from the parent's null component still blocks A.
- Death JSON unchanged.

### L5-09. Harbinger death does not copy the parent blob

Inputs: death killer is the fixture Harbinger, `IsBossFight` true, `KillingElement` `Fire`, current fire 75, void 40. Parent profile has verified Fire. Harbinger's only verified element is Void. Fire is not a verified component on the Harbinger.

Expected:

- `res_Void` is an action because void is the Harbinger's verified element and it is below 75.
- Fire on the killing blow still follows recorded-cause rules (capped notice if current fire is 75).
- The parent's fire component is not added a second time as a Harbinger inherited move.
- Closing void and fire still leaves grade B while `own-untyped` is null.

## Resistance first, overcap, stacks

### L5-10. Killing-blow gap outranks the Harbinger gap

Inputs: fresh and current `Res.Fire` 41, current `Res.Void` 40, crit false. Follow-up applies.

Expected:

- Action order starts `res_Fire` weight 110, then `res_Void` weight 100.
- At most 3 actions, 3 distinct groups.
- No `ehp`.
- Fire body contains `area level 100` and `25% less fire`.
- `ResistanceRatio(41, 75, 0, 75)` equals `1/1.34` (0.7462686567).

### L5-11. Missing current resistance stays a check

Inputs: current stats `MaxHealth` 1400 only. Death fire was 75. Follow-up applies and void is verified.

Expected:

- Fire change line contains `75% → unknown now`.
- `res_Fire` title contains `Check`.
- `res_Void` title contains `Check`.
- Neither body invents `0%`.
- Encounter grade `?`.
- No `ehp`.

### L5-12. Overcap is refused without a captured or verified debuff

Inputs: fire and void both current 75, uncapped fire 110, uncapped void 110. No ailments. Area 100. Follow-up applies.

Expected:

- `ResistanceRatio(75, 110, 0, 75)` is 1.
- No action title or body contains `overcap` or `110`.
- A notice may say the snapshot was capped and no resistance-loss debuff was recorded.

### L5-13. Verified Shock without a stack count does not invent 20 points

Inputs: add a Harbinger component `shock-hit`, Element Lightning, Delivery Hit, `VerifiedAilmentNames` = `Shock`, Evidence DataminedReference. Current `Res.Lightning` 75, `ResUncapped.Lightning` 90. Death `AilmentsOnYou` does not contain Shock. No stack field anywhere.

Expected:

- One lightning card, group `res:Lightning`.
- Body contains `Shock` and `Stack count was not captured` (or the existing `exact lost resistance were not captured`).
- Body contains `does not offset area-level penetration` or the same current sentence.
- `PreviewResistance` is called with shred 0, or is not called. It is not called with shred 2, 8, or 20.
- Ratio of 75 to 90 at penetration 75 is 1.
- No second lightning card.

### L5-14. Captured Marked for Death uses 25 points once

Inputs: `D.AilmentsOnYou` contains `Marked for Death`. Current fire 75, uncapped fire 90, void 40. Follow-up applies.

Expected:

- Fire card is the shred/headroom card, not a new overcap-from-penetration card. Body contains `25` and does not contain a second invented stack.
- Void card is the 35-point gap card and does not repeat a separate Marked for Death card.
- Groups `res:Fire` and `res:Void` appear once each.
- The 25-point figure is the ailment's stated effect, not a measured stack count. Do not also subtract `10 * 2`.

### L5-15. Counted shred can be quantified, max stacks stay 10

Inputs: a test double that supplies captured fire shred stacks = 10, and another call with stacks = 0.

Expected:

- 10 stacks, player rate: shred 20. `EffectiveResistance(95, 20, 75)` is 0. `EffectiveResistance(75, 20, 75)` is -20. Ratio `1 / 1.20`.
- 0 stacks: overcap ratio is 1, and the overcap action does not fire.
- A call with 20 stacks is not the player cap. `PlayerResShredMaxStacks` is 10.

## Defenses matched to delivery

### L5-16. Armor for a verified physical hit, refused for a verified damage over time

Inputs: component Physical, Delivery Hit, area 100, armor 1000, a physical killing hit that is not a dot. Second case: same element, Delivery DamageOverTime, killing ailment Bleed.

Expected hit case:

- One `armor` card. `ArmorMitigationFraction(1000, 100)` is about 0.237 (the 01b worked point is about 23.7% at area 100).
- Non-physical uses `ArmorMitigationNonPhysicalFraction`, 70% of that.

Expected dot case:

- No armor card.
- `PreviewArmor(..., isDot: true)` note contains `armor does not mitigate damage over time`.
- Physical resistance card still allowed when `Res.Physical` is below 75.

Third case: Delivery Unknown.

Expected: no armor card, and no sentence that says armor does nothing.

### L5-17. Capped heavy hit uses endurance and pool, not more resistance

Inputs: section 8.2 numbers. Fire 75, damage 2200, overkill 400, max health 2000, endurance 20, threshold 400, area 100, shred 0, crit false. No follow-up verified gaps. Set the death `Kind` to `OneShot`, `Hits` 1, `WindowDamage` 2000 so the existing pool and endurance rules can see a heavy health loss. Current stats equal the snapshot except `MaxHealth` 2000 (not improved).

Expected:

- `PreviewResistance(2200, 400, 75, 75, 0, 100, PostMitigationWithWard)` ratio is 1. `Survived` is false if computed, and the resistance sentence does not claim a reduction.
- `PreviewEndurance(2200, 400, 400, 20, 60, PostMitigationWithWard)` ratio `0.8181818182`, new damage 1800, remaining 1800. `Sentence()` contains `may or may not`.
- `PreviewPoolIncrease(2200, 400, 400, PostMitigationWithWard)` is the borderline pool sentence. Added pool 500 survives. Added pool 200 does not.
- Actions include `endurance` and `ehp`. They share no group with each other.
- Recorded-cause grade is not A.
- If the profile still has a null component, encounter grade is not A either.
- Body of `ehp` contains `No guaranteed` or the existing `No guaranteed survival amount` sentence.

### L5-18. Ward peak is not a ward build and not a grade

Inputs: heavy capped fire as in `Test_Grade_HugeOverkillCannotBecomeSafeFromCapsOrWardPeak`, plus current `Ward` 50000. Encounter profile Unknown, like Lagon.

Expected:

- If the pool card is present, its body contains the existing sustain warning and does not contain `survived`.
- If a resistance action suppressed the pool card, the ward number does not appear on some other card.
- `Ward` 0 does not produce `Ward was`.
- Missing `Ward` key does not produce `Ward was` and is not treated as 0.
- Neither recorded-cause nor encounter grade is A because of the ward number.
- No call to `WardDecayPerSecond` is shown to the player: retention and threshold were not captured.

### L5-19. Crit, stun, and freeze stay in their own groups

Inputs: `KillingCrit` true, `CritAvoidance` 20, ailments `Freeze` and `Stun`, verified Harbinger void gap at 40, fire gap at 41.

Expected:

- At most 3 actions, 3 distinct groups.
- The first two are `res:Fire` and `res:Void` (weights 110 and 100). The third is `crit` only if its weight beats `control` and `control:stun`. With current weights, crit is 100, freeze is 80, stun is 79, so the third card is `crit`, not a duplicate resistance card.
- No card's body says cold resistance stops freeze.
- Stun body does not add ward to max health.
- Raising only fire to 75 leaves void, crit, freeze, and stun open. Encounter grade is not A.

### L5-20. Damage over time does not grow the pool and does not crit

Inputs: killing ailment Ignite, `KillingCrit` true, fire resistance 75, `Kind` `DamageOverTime`.

Expected:

- No `ehp` action.
- No crit action (`dotKill` conflicts with the crit flag).
- One `recovery` action. Body says armor does not normally reduce damage over time.
- Encounter grade is not A.

## Immutability and dedup

### L5-21. Catalog lookup cannot rewrite the death or a saved reassessment

Inputs: save a reassessment of `D` with current fire 75 and void 40. Then add `Element.Cold` to the parent profile in memory and build again. Also browse the catalog manually.

Expected:

- `OriginalCapture.KillingElement` is still `Fire`.
- `OriginalCapture` has no Cold defense and no catalog id written into `Killer`.
- The first saved snapshot still has the actions from the first click. It does not gain `res_Cold` when the file is reloaded.
- Schema remains 1. A line without `Encounter` still loads.
- Death count and `deaths.jsonl` bytes are unchanged.
- `IsBossFight` on a record that was null stays null. `AttackerGuide` may return a profile; `Match` stays null; encounter grade for that guide says classification is unknown and is not A.

### L5-22. Three sections do not repeat one gear card

Inputs: parent fire gap and Harbinger void gap, plus `BossProfile.Tips` containing movement text only.

Expected:

- The merged action list has at most 3 items.
- `res:Fire` appears once. `res:Void` appears once.
- No action body equals a tip string.
- Tips are not appended as extra `Advice` cards.
- `DeathPatterns` priorities are computed from death records only. A catalog void element that never appeared on a death does not add a pattern card.

### L5-23. Secondary type is not half the hit

Inputs: `KillingElement` Fire, `SecondaryKillingElement` Void, both resistances 41, damage 1700, overkill 700, area 100. No `DamageByElement` split.

Expected:

- Two resistance cards, fire weight 110, void weight 110, take 3 still unique groups.
- Each card may state its own ratio (25% less for 41 to 75).
- No sentence multiplies the ratios (`0.7463 * 0.7463`) or says the 1700 hit becomes `1700 * 0.7463 * 0.7463`.
- `Survived` is null on both previews, or each preview states it assumed the whole hit was that one element. Do not report a single survival verdict for the sum.

### L5-24. Stale area level refuses the percent

Inputs: `DefenseSnapshotAgeSeconds` 10. Current area level 100. Fire 41 to 75.

Expected:

- No `Models about` clause.
- Message says the original area level is unknown.
- Current zone level 1 is not used.

### L5-25. Non-boss A still behaves as it does now

Inputs: `AdviceDeath("Fire", 41)` with `KillingCrit` false and no boss flag. Current fire 75 and `MaxHealth` 1000. No encounter profile.

Expected:

- Recorded-cause grade A.
- No encounter fight line, or the fight line is absent rather than a false A.
- Existing `Test_Grade_ARequiresImprovedMeasuredChecks` expectations remain true.

## Worked numbers to pin directly

These call `MitigationMath` only. They duplicate section 8 so a catalog change cannot silently retune them.

| Id | Call | Expected |
|---|---|---|
| L5-M1 | `ResistanceRatio(41, 75, 0, 75)` | 0.7462686567. `1700 * ratio` = 1268.6567. Remaining 1000. New damage is greater than remaining by more than 10% of remaining |
| L5-M2 | `ResistanceRatio(75, 95, 0, 75)` | 1 |
| L5-M3 | `PreviewEndurance(2200, 400, 400, 20, 60, PostMitigationWithWard)` | Ratio 0.8181818182. New damage 1800. Remaining 1800. Sentence contains `may or may not` |
| L5-M4 | `PreviewPoolIncrease(2200, 400, 500, PostMitigationWithWard)` | `Survived` true. Sentence contains `would have absorbed this exact hit` |
| L5-M5 | `ResistanceRatio(40, 75, 0, 75)` | 0.7407407407. Formatter yields 26. `NewDamage` is NaN when the caller passes no void damage number |
| L5-M6 | `EffectiveResistance(95, 20, 75)` and `(75, 20, 75)` | 0 and -20. Shred 20 is 10 captured player stacks, not an assumed cap |
| L5-M7 | `PreviewArmor(300, 40, 2763, 2763, 100, nonPhysical: false, isDot: true, ...)` | Ratio NaN. Note contains `damage over time` |
