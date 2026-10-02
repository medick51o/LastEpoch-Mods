# Lane 5: encounter-aware defense assessment

Baseline: `cursor/boss-research-base` (v0.1.2, 214 CoreTests). This lane changes no code. It designs the next Core pass. Generic defense formulas stay in `MitigationMath` and `research/advice/01b-MECHANICS-FACTCHECK.md`. Do not re-derive them.

Access date for every live page below: 2026-10-02. Season applicability is patch 1.5 unless a row says an older patch. No Season 5 formula change was readable in this pass. See the ledger.

## 1. Two grades, two jobs

`ReadinessRating` today grades the recorded cause. Its scope already says it is not a whole-fight survival probability. A capped killing blow can still become grade A (`Test_Grade_ARequiresImprovedMeasuredChecks`). That A must never be the grade of the fight.

| Grade | Type today | Question it answers | A allowed? |
|---|---|---|---|
| Recorded-cause | `CurrentAssessment` + `ReadinessRating` | Did current stats close the gaps on this saved death? | Yes, under the existing rules, for this death only |
| Encounter preparation | new, read-only | Are the verified threats of this encounter, including a follow-up Harbinger's own verified threats, covered? | Only when section 4 says the catalog is complete |

Show both lines when an encounter is identified. The fight line uses the encounter grade. A recorded-cause A sits under it and cannot replace it.

Proposed recorded-cause scope, keeping the existing sentence so current tests still match:

`Defense progress grade, not a whole-fight survival probability. Recorded cause only. It does not clear other attacks in this encounter.`

Fight line copy when the killing element is capped and anything else is open:

`This encounter: C. Void resistance remains below 75%. A capped recorded blow does not clear the rest of the fight.`

No survival percentage on either line. Modeled output is a damage ratio for one element, or a refusal.

## 2. Catalog contract

`BossProfile.DamageTypes` is the wrong shape for this. An empty array is what Lagon, Emperor of Corpses, Heorot, and Harbinger of Hatred use today because typing is unverified. Empty must not mean "no other damage." Julra's three elements are encounter coverage from Tunklab, and the profile already says they are not an attack-by-attack map.

Proposed fields. Absence is null. Community prose never fills them.

```text
EvidenceClass: DeveloperDocumentation | DataminedReference | CommunityReport | LocalMetadata | Deduction | Unknown

Delivery: Unknown | Hit | DamageOverTime | Mixed

ThreatComponent
  MoveId, DisplayName
  Element?                 null if untyped
  Delivery
  CanCrit?                 null if unknown
  InheritedFromMoveId?     null if this move is the Harbinger's own
  VerifiedAilmentNames     names only; never a stack count
  Evidence, SourceId
  CountsAsVerified         true only when Evidence is DeveloperDocumentation
                           or DataminedReference AND Element != null

EncounterProfile
  Id
  ResearchStatus           Unknown | Partial | Complete
  Components[]
  FollowUpId?              Harbinger id when a timeline echo can spawn one
  FollowUpApplies?         null if this death's variant is unknown
                           false for a campaign-only variant
                           true only for the verified timeline variant
```

`CountsAsVerified` is false for `CommunityReport`, `Deduction`, and `Unknown`. Ability names, scene names, and visuals do not set `Element`. "Void Rahyeh Dive Bomb" stays element null. That matches the current Harbinger of Hatred profile.

`ResearchStatus` is Complete only when every component has a non-null element, a known delivery, and a known `CanCrit`, and any follow-up that applies has the same. Partial is Julra today: three verified elements, delivery and crit unknown. Unknown is the other four starter profiles.

Adapter until Lane 2 replaces the list:

- Julra: Partial. Verified elements Void, Cold, Lightning. Delivery Unknown. CanCrit null.
- Lagon, Emperor of Corpses, Heorot, Harbinger of Hatred: Unknown. No verified elements. Named moves stay untyped.

## 3. What enters the requirement set

Recorded-cause elements stay `CurrentAssessment.RecordedElements`: killing elements, a killing ailment's element, and timeline shares at or above 25%. Catalog data is not an input to that function and is not written onto the `DeathRecord`.

Encounter requirements are a separate set:

1. Verified components of the identified encounter.
2. If `FollowUpApplies == true`, verified components of that Harbinger profile, unique and inherited.
3. Captured player deficits (`Res.*` below 75, missing `Res.*`, recorded shred). These order the cards. They do not invent elements.

Rules for the Harbinger half:

- A Harbinger does not inherit the parent's `DamageTypes` as a blob. An absorbed move counts only when that move's own component is verified. EHG says Harbingers gain abilities from the timeline boss and also have Agile or Brute styles of their own (ledger). The unique style is an extra threat list. Until those moves are typed, each unique component is element null.
- A parent's capped element does not satisfy a different Harbinger element, and it does not satisfy a null Harbinger element.
- If `FollowUpApplies` is null, do not add Harbinger resistances, and do not award encounter A. Say the follow-up is unknown.
- If the attacker is the Harbinger, requirements are that Harbinger profile only. Do not also grade the parent fight, and do not match the parent by substring. `Harbinger of Hatred's Void Rahyeh` already fails `BossCatalog.Match`.
- Campaign Lagon is not the monolith echo. No Harbinger requirements on a variant marked campaign. Unknown variant blocks A without copying Harbinger elements onto the death.
- Adds and proxies that do not match an alias do not import a boss's verified elements. `BossCatalog.ObservedTypes` remains the death report only.

Lane 1 owns the real boss-to-Harbinger ids. This lane takes `FollowUpId` as an input. It does not invent the ten mappings.

## 4. Encounter grade

Inputs: identified profile, optional follow-up profile, current stats, the original death (read-only). Missing stat stays missing. Do not reuse the death's `Res.*` when the current read lacks that key (`Test_Current_MissingCurrentStatsNeverReuseOldDefense`).

```text
verified = verified elements of profile UNION verified elements of follow-up if FollowUpApplies
gaps = verified where current Res.Name < 74.5
missingStat = verified where current Res.Name is absent
regressed = a verified Res, or MaxHealth, lower than a fresh death snapshot
incomplete = ResearchStatus != Complete
             OR FollowUpApplies == null
             OR any component Element, Delivery, or CanCrit is null
             OR (FollowUpApplies && follow-up ResearchStatus != Complete)

if no profile and IsBossFight != true: no encounter grade
if profile == null: grade "?"  "Encounter not identified"
else if missingStat and gaps empty and not regressed: grade "?"
else if gaps or (crit recorded or any CanCrit == true) and crit still open:
    grade "D" if regressed else "C"
else if incomplete or any non-notice action remains in groups other than pattern:
    grade "B"
else:
    grade "A"
```

Encounter A additionally requires every verified resistance at or above 74.5, no regression, recorded crit closed when the blow was a crit, and every verified `CanCrit == true` covered by current crit avoidance or reduced bonus crit damage at or above 99.5. `CanCrit == null` is incomplete, so the result stays B or lower. A is not a claim that the player survives.

Mapped onto today's letters:

- C or D use the same wording rule as `ReadinessRating`: name the resistances still below 75%. D if a measured defense dropped.
- `?` if the cause or a required current resistance is unreadable. Unknown is not zero.
- B if verified caps are met and the catalog is still incomplete, or a pool, endurance, recovery, or control action remains.
- The Julra case with Void, Cold, and Lightning all capped is B, not A. The Tunklab list is not a complete move map.
- Harbinger of Hatred with a capped killing element is B or `?`, never A, while its components are null.
- A non-boss death keeps today's recorded-cause grade and has no fight line. Existing A tests stay valid.

There is no path from "killing element capped" to encounter A.

## 5. Which defense addresses which move

Use a component only when delivery and element are known. Unknown delivery does not get an armor card and does not get an "armor does nothing" sentence.

| Defense | Helps | Does not help | How the card behaves |
|---|---|---|---|
| Resistance of that element | Hits and damage over time of that element | A different element; area-level penetration, which is already inside the ratio | First. One `res:{Element}` card per element |
| Overcap (`ResUncapped`) | Shred, Shock, Poison's own shred, Marked for Death, and only those | Area-level penetration. Ratio is 1.0 at area level 75+ if shred points are 0 | Conditional. Stack count only if captured. Otherwise shred points stay 0 and the sentence says the count was not captured |
| Armor | Verified hits. Full value for physical. 70% as effective for other hits. Cap 85% physical, 59.5% non-physical | Damage over time, including a physical ailment such as Bleed | Existing `armor` group. Skip when delivery is unknown |
| Endurance | Health damage below the threshold, hits and damage over time. Cap 60%. Threshold starts at 20% of max health and has no cap | Ward, and the part of the hit above the threshold | Existing `endurance` group. One card |
| Crit avoidance or reduced bonus crit damage | A recorded crit, or a verified hit with `CanCrit == true`. Avoidance is a roll. Reduced bonus damage scales the hit. 100% reduced bonus makes a crit a normal hit | Damage over time, which cannot crit | Existing `crit` group. `CanCrit == null` adds no card and blocks encounter A |
| Dodge, block, glancing blow | Hits only, as chances. Block still counts as a hit. Dodge does not | Damage over time | Existing `avoidance` group. Wording stays "sometimes." No survival promise |
| Recovery and cleanse | Damage over time ticks; a captured or verified shred, Shock, or control ailment the build can cleanse | A substitute for the resistance card | One `recovery` group. The shred sentence lives inside the matching `res:` card, which is how `AdviceRules` already dedups |
| Freeze or chill protection | Captured Freeze or Chill, or a verified component that names them | Cold resistance. Cold resistance does not stop freeze or chill | Existing `control` group. One card |
| Stun avoidance and max health | Captured Stun, or a verified stun. Stun avoidance is a flat rating. More max health raises the threshold | Ward added to max health. The current stun article does not add ward | Existing `control:stun` group |
| Pool (max health, and ward only as below) | A heavy recorded hit or burst after resistance and crit gaps are closed | A resistance gap; a damage-over-time kill; an unknown second element | Existing `pool` group. Suppress it while any resistance or crit action is open, including an encounter-only gap |

Ward, same rule as `Test_Review_LargeHealthLossWithCapSupportsPoolButNotWardDefault`:

- Recommend nothing about ward unless the fresh snapshot has `Ward > 0`.
- That clause sits on the pool card. It is not its own card.
- `Ward == 0` is not a ward build. A missing key is unknown, not zero.
- One reading is not sustain. Decay needs retention and the ward above the decay threshold (`WardDecayPerSecond`). Those inputs are not on the death. Do not print a decayed-ward number.
- Boss ward is the boss's health-bar mechanic. It is not the player's `Ward` stat.
- A community claim that a pool bypasses ward (Emperor blood pools, Maxroll Harbinger "Spirit Pools") does not change the math until the component is verified. If a later lane sets a verified bypass flag, the pool sentence says ward does not absorb that move and still does not invent a number.

Echo modifiers and corruption can change incoming damage and crit chance. The 2026 corruption article confirms the direction only. Do not scale a counterfactual by corruption. An uncaptured modifier does not add a crit card. A recorded crit still uses the death's crit flag.

Patch 1.2.1 says enemy abilities no longer apply Time Rot or Damned. This lane found no later note restoring them, and it did not read a Season 5 body that confirms they stayed removed. Do not put those ailments on a boss from memory. If a death captures the name, the captured ailment still drives the existing card.

## 6. Ordering, three cards, no triplicated gear

Reuse `Advisor.MaxShown` (3) and `Advice.Group`. `AdviceRules.Evaluate` already keeps one candidate per group.

Encounter-only resistance uses the same group key, `res:{Element}`.

| Situation | Weight | Group |
|---|---|---|
| Captured resistance below 75 on a recorded element | 110, existing | `res:{Element}` |
| Verified encounter or Harbinger element below 75, not the recorded blow | 100 | same `res:{Element}` |
| Recorded shred or a verified shred ailment with no stack count, resistance otherwise capped | 92, existing shred path | same group |
| Element verified, current resistance missing | 55 | same group |
| Element null | no resistance card | incomplete flag only |
| Capped, no shred | 20 notice, existing | same group |

Higher weight replaces the same group. A fire notice at 20 loses to a real fire gap at 110. A void gap does not create a second fire card.

After the merge, drop `pool` if any non-notice `res:` or `crit` action exists. Then `Where(t => !t.IsNotice).OrderByDescending(Weight).ThenBy(Key).Take(3)`.

`BossProfile.Tips` stay movement and positioning. They are not `Advice` cards. The same resistance sentence is not repeated in Boss tips, Last death, and the current assessment. Last death and the current assessment already share `AdviceRules`; encounter prep joins that list before the take-3. Patterns keeps using death records only, never catalog expectations.

Player-facing bodies stay free of a double dash. Sample void card:

`Void damage is part of this Harbinger's verified coverage. It was not the recorded killing blow. You are 35 points short of the 75% cap. At area level 100, closing this gap models about 26% less void damage from a comparable enemy source. Resistance above the cap does not counter area-level penetration.`

The 26% figure is the existing formatter: `100 * (1 - ratio)` with format `0`. Arithmetic is in section 8.

## 7. Immutability

`CurrentAssessment.Build` already copies the death into a review record and does not write back. Encounter prep does the same. It must not:

- set `KillingElement`, `SecondaryKillingElement`, `AilmentsOnYou`, `IsBossFight`, `Zone`, or `Defenses` from the catalog
- replace `OriginalCapture` inside `ReassessmentLog.Save`
- edit a previous `AssessmentSnapshot` when the catalog later gains an element
- treat manual browsing as a label on the saved death (`Test_Boss_ManualCatalogCannotMutateSavedDeath`)

`ReassessmentLog` stays schema 1. Add an optional `Encounter` object on `AssessmentSnapshot`. Old lines omit it and still load. New saves store the fight grade from the click. Later catalog edits do not rewrite the file. Algorithm on the death half stays `defense-progress-v1`. The optional block can say `encounter-prep-v1`.

A stale or missing original area level still refuses the numeric ratio. `ZoneLevel` and today's town level do not fill it.

## 8. Counterfactual math

Every example uses `MitigationMath` as it stands. Units: resistance, penetration, shred, and endurance are percent points. Armor is the raw rating. Damage and overkill are the report numbers. Area level is the original fresh `AreaLevel`.

Shared assumptions, stated on every result:

- Shred points are 0 unless a stack count was captured. A verified ailment name without a count does not become max stacks.
- Penetration is `EnemyPenetration(areaLevel)`: 1 point per area level, capped at 75, applied after the 75 cap.
- Effective resistance is `min(uncapped - shred, 75) - penetration`.
- Ratio = new taken fraction / old taken fraction. The ratio does not need a damage meaning. `NewDamage` and `Survived` use `DamageMeaning.PostMitigationWithWard` only. Otherwise survival is null.
- The report split between primary and secondary types is unknown. Do not multiply two elements' ratios into one hit.
- A Harbinger element that was not the killing blow has no recorded damage number. Publish the ratio, or a refusal. Do not scale the killing blow by the other element's ratio.
- No calibrated chance of surviving the fight.

### 8.1 Resistance gap

Recorded fire hit. Uncapped fire resistance 41. Area level 100. Shred 0. Damage 1700. Overkill 700. Not a crit. This is the recorded-cause case. It does not claim the rest of the fight is fire.

- Penetration 75.
- Effective now: `min(41 - 0, 75) - 75 = -34`. Taken fraction 1.34.
- Effective at cap: `min(75, 75) - 75 = 0`. Taken fraction 1.
- Ratio `1 / 1.34 = 0.7462686567`. About 25% less fire damage (`25.373` rounds to 25).
- New damage `1700 * 0.7462686567 = 1268.6567`, about 1270.
- Remaining `1700 - 700 = 1000`.
- 1270 is above 1000, and outside the 10% band, so the hit would probably still have killed under the post-mitigation-with-ward reading.
- Overcap to 95 with shred 0: ratio 1.0. Do not recommend it.
- Missing area level: `PreviewResistance` refuses the penetration term. No percent.
- Encounter grade is C while fire is below 75. If this death is also a Harbinger with a null unique component, raising fire to 75 still cannot make the fight A.

### 8.2 Capped heavy hit

Recorded fire hit. Fire resistance 75, uncapped 75. Area level 100. Shred 0. Damage 2200. Overkill 400. Max health 2000. Endurance 20. Threshold 400. Not a crit. Same numbers as `research/advice/03-MATH.md` example (b), already pinned in CoreTests.

- Resistance ratio 1.0. Overcap stays 1.0. Copy: `Fire resistance was already capped. No resistance number changes this hit at area level 100.`
- Remaining `2200 - 400 = 1800`.
- Endurance 20 to 60: health at the hit is assumed equal to remaining, which means no ward left. Portion above the threshold `1800 - 400 = 1400`. Portion below after old endurance `2200 - 1400 = 800`. Undo old endurance `800 / 0.80 = 1000`. New hit `1400 + 1000 * 0.40 = 1800`. Ratio `1800 / 2200 = 0.8181818182`.
- 1800 against about 1800 left is inside the 10% band, so it may or may not have been enough. Any ward left would put more of the hit under the threshold, so this is the smallest endurance benefit.
- Pool: overkill 400 is the shortfall under the same damage meaning. Adding 400 is the borderline pool sentence. Adding 500 absorbs this exact hit. Adding 200 does not. More max health also raises the threshold, so 400 is the smallest pool figure, not a recommended gear total.
- A ward reading of 50000 does not enter this math and does not raise the grade (`Test_Grade_HugeOverkillCannotBecomeSafeFromCapsOrWardPeak`).
- Recorded-cause grade stays B while the pool or endurance action is open, or while health did not improve. It is not A just because fire is capped.
- Encounter grade is not A if any other component is null or any other verified element is open.

### 8.3 Mixed boss and Harbinger fight

Calculation fixture, not a named boss's damage table. Swap the elements for Lane 2's verified components when they exist.

Catalog inputs:

- Timeline boss, ResearchStatus Partial. One verified component: Fire, Hit, CanCrit false. One component: element null.
- FollowUpApplies true. Harbinger ResearchStatus Partial. One verified unique component: Void, Hit, CanCrit null. One unique component: element null. No inherited component is verified, so the parent's fire tag is not copied onto the Harbinger as a second source.
- Original death: attacker is the timeline boss, fire killing blow, fresh fire resistance 41, area level 100, crit false, Kind Reported, no timeline, no ailments. Current stats: fire 75, void 40, max health not lower. Cold is not a requirement. Physical is not a requirement.

Recorded-cause result:

- Fire 41 to 75 uses section 8.1. About 25% less fire damage on that blow. The modeled hit is still above the remaining pool in that example.
- Recorded-cause grade can be A when the existing A checks pass (fire improved, crit flag known, no unresolved non-pattern action). Scope text still says recorded cause only.
- Fight line is not A.

Encounter result:

- Fire requirement is met at 75. That card is the existing capped notice, not an action.
- Void is a Harbinger requirement. `40` is 35 points under 75.
- Void ratio at area 100, shred 0: taken now `1 - (40 - 75) / 100 = 1.35`. Taken at cap `1`. Ratio `1 / 1.35 = 0.7407407407`. About 26% less void damage (`25.926` rounds to 26).
- There is no recorded void amount. `NewDamage` and `Survived` stay unset. Do not multiply 0.7407 by the fire killing blow.
- Overcap of void with shred 0 is ratio 1.0. Marked for Death was not captured, so do not mention a 25 point loss. Do not assume 10 Shock stacks.
- Null components and `CanCrit == null` on the Harbinger hit keep ResearchStatus incomplete.
- Encounter grade C. Reason names void. Fire being capped is not a clearance.
- If current void is then 75 and fire stays 75, grade becomes B, not A, because components are still null and Harbinger crit is unknown.
- Actions, one each: the void gap (`res:Void`, weight 100). Fire does not appear again. No pool card, because a resistance action is open. No ward sentence unless `Ward > 0` would have been on a pool card, and that card is suppressed. Boss tips are not copied into the list. Count is 1, groups are distinct, under the cap of 3.

If the recorded blow had also been a crit with avoidance 0, the three cards would be `res_Fire` (110) if fire were still open, else `res_Void` (100), then `crit` (100). Equal weights sort by key, so `crit` precedes `res_Void`. A third verified gap, if one existed, would be the third card. The null component still would not manufacture an element.

## 9. Source ledger

Mechanics rows point at the fact-check that already opened the support articles. This lane did not reopen those articles. Harbinger rows were opened on 2026-10-02.

| Claim | Source | Publisher, date | Patch | Class | Supports |
|---|---|---|---|---|---|
| Resistance cap 75%. Overcap absorbs shred, Shock, and Marked for Death. It does not reduce damage by itself | https://support.lastepoch.com/hc/en-us/articles/46361885661851 | EHG support, updated 2026-07-14, via 01b | Support page predates 1.5; no 1.5 change confirmed here | Developer documentation | Cap first. Overcap only for those debuffs |
| Enemy penetration 1% per area level, max 75%, after the cap. Overcap does not offset it | https://support.lastepoch.com/hc/en-us/articles/46361871026971 | EHG support, updated 2026-02-24, via 01b | Same limit | Developer documentation | Area level required for a ratio. No overcap from penetration |
| Player shred 2 points per stack, 10 stacks. Shock 2 lightning points on players, 10 stacks. Marked for Death 25 all resistances, 1 stack. Poison shreds on the first 30 stacks at 2 points | https://support.lastepoch.com/hc/en-us/articles/46361887879963 | EHG support, updated 2026-02-20, via 01b | Current guide says 10, not the pre-0.9 cap of 20 | Developer documentation | Do not invent stacks. `PlayerResShredMaxStacks` is 10 |
| Armor is hits only, 70% as effective off physical, mitigation approaches 85% | https://support.lastepoch.com/hc/en-us/articles/46361891210651 | EHG support, updated 2026-07-13, via 01b | Formula image predates 1.5 | Developer documentation | Armor card only for verified hits |
| Endurance cap 60%, threshold starts at 20% of max health, health only, hits and damage over time | https://support.lastepoch.com/hc/en-us/articles/46361855013787 | EHG support. 01b records updated 2026-02-20. `02-RULES.md` records 2026-02-13 | Not re-opened here | Developer documentation | One endurance card. Example 8.2 |
| Enemy crit multiplier 200%. Avoidance after the attacker's roll. Reduced bonus crit damage cap 100%. Damage over time cannot crit | https://support.lastepoch.com/hc/en-us/articles/46361891709211 | EHG support, updated 2026-02-20, via 01b | Same limit | Developer documentation | One crit card. Unknown `CanCrit` is not a card and not an A |
| Ward is a shield above health. Decay is quadratic and depends on retention and the decay threshold | https://support.lastepoch.com/hc/en-us/articles/46361876030235 | EHG support, updated 2026-07-14, via 01b | Same limit | Developer documentation | A single ward reading is not sustain |
| Stun threshold uses max health and stun avoidance, not ward, on the current article | https://support.lastepoch.com/hc/en-us/articles/46361891772443 | EHG support, updated 2026-02-20, via 01b | Same limit | Developer documentation | Do not add ward to the stun pool |
| Freeze chance uses max health plus current ward | https://support.lastepoch.com/hc/en-us/articles/46361855307035 | EHG support, updated 2026-07-14, via 01b | Same limit | Developer documentation | Cold resistance is not freeze protection. A ward peak is not a freeze plan |
| Boss ward replaced dynamic boss damage reduction. Harbingers were named among bosses that kept it | https://forum.lastepoch.com/t/endgame-balance-and-itemization-updates-coming-to-last-epoch-april-17th/75189 and https://support.lastepoch.com/hc/en-us/articles/46361900580379 | EHG, Season 2 post 2025-04-17; support updated 2026-07-14, via 01b | Season 2 onward | Developer documentation | Boss ward is not player ward |
| Harbingers have Agile and Brute styles and gain abilities from the timeline boss | https://forum.lastepoch.com/t/harbingers-of-ruin-whats-new/71684 | EHG, Yayifications, 2024-06-25 | Patch 1.1, launch described as 2024-07-09 | Developer documentation | Extra-threat slot and inherited-move slot. Not a damage type |
| Each of 10 timelines has a Harbinger spawned on the timeline boss defeat. Harbingers absorb that boss's abilities | https://www.lastepochtools.com/news/article/last-epoch-harbingers-of-ruin-patch-notes-71790 | LE Tools mirror, posted by Vapid_Actions 2024-07-05. Forum original: https://forum.lastepoch.com/t/last-epoch-harbingers-of-ruin-patch-notes/71790 | Patch 1.1 | Developer documentation, via a mirror | Sequence: boss, then Harbinger. Damage types still unset |
| Harbinger of Hatred's Void Rahyeh Dive Bomb visuals did not match the damage | https://forum.lastepoch.com/t/last-epoch-patch-1-2-1-notes/76245 | EHG, EHG_Wick, 2025-04-23 | 1.2.1 | Developer documentation | Name and visual are not a damage type |
| Harbinger of Defilement creates slam shockwaves at the end of the Abomination timeline fight | Same 1.2.1 notes | EHG, EHG_Wick, 2025-04-23 | 1.2.1 | Developer documentation | A follow-up exists on that timeline. Shockwave is not typed |
| Enemy abilities no longer apply Time Rot or Damned | Same 1.2.1 notes | EHG, EHG_Wick, 2025-04-23 | 1.2.1. No restoring note found here. 1.5 body not confirmed | Developer documentation | Do not invent those ailments. Captured names still count |
| Julra encounter damage types Void, Cold, Lightning | https://lastepoch.tunklab.com/dungeon/temporal_sanctum | Tunklab, page has no build stamp | Unstamped. Already the v0.1.2 catalog source | Datamined/reference data | Those three resistances are requirements. Status stays Partial, so not encounter A |
| Harbinger of Destruction tank skills are Physical and Void, and it adds a Necrotic damage-over-time breath and percent-health pools from Emperor of Corpses | https://maxroll.gg/last-epoch/resources/harbinger-of-destruction-boss-guide | Maxroll, last updated 2026-09-25, written by McFluffin | Page does not cite a build | Community report | Not verified. Do not put Physical, Void, or Necrotic into `CountsAsVerified` from this page |
| Season 5 is patch 1.5, live 2026-10-01 | https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-is-now-live/81860 | EHG, via 01b | 1.5 | Developer documentation | Version label only. Not a defense-formula check |

Conflict: Maxroll types Harbinger archetype moves. EHG's 1.1 blog does not. Resolution: leave the elements null until Lane 2 has datamined or developer evidence for that move. Smallest test: read the ability's damage tags in current extracted data, with file version, and compare one in-game death report on that move.

Conflict: `02-RULES.md` still lists penetration as a reason to overcap in hard rule C3, then says overcap does not work against penetration. Resolution: follow 01b and the current `AdviceRules` sentence. Penetration is not an overcap reason.

## 10. What Codex can port, and what is open

Port the two-grade split, the null-versus-empty catalog rule, the weight and group merge, the immutability rule, and the three worked examples as tests. Do not fill Harbinger damage types from Maxroll or from ability names.

Open:

- Lane 2 has not supplied per-move elements, delivery, or crit. Encounter A stays unreachable for the five starter profiles.
- Lane 1 has not supplied the verified follow-up id for each timeline variant. `FollowUpApplies` stays null, which blocks A without inventing the link.
- Season 5 patch-note body was not readable here. Time Rot and Damned removal, and the July 2026 defense formulas, are not reconfirmed against 1.5.
- Tunklab's Julra page has no build stamp.
- Death-report damage and overkill units remain the unverified post-mitigation-with-ward reading. Survival sentences keep that assumption. Fight survival stays unrated.
- Whether a captured Harbinger killer is the Agile or Brute body, or a proxy, is a Lane 4 identity problem. An unmatched name gets no parent profile.
