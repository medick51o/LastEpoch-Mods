# Lane 2 of 4: Death advice decision rules (evidence to advice matrix)

Project: MedicK's Terrible Death Counter and Log (`medick_DeathCounter`), a MelonLoader mod for Last Epoch Season 5 (patch 1.5, "Rage of the Frostborn", released 2026-10-01).
Researched and written 2026-10-02 (UTC). Experimental research only: do not merge to main, do not install, do not publish.

Base and provenance: written against branch `cursor/advice-research-base` (commit 4b5341e, Codex's local snapshot of 2026-10-02). This lane started before that branch existed on the remote, so the first draft used the PR #4 era baseline `claude/last-epoch-death-counter-yspgnf` (6790c86) with the newer field names marked PLANNED; when the base branch appeared, the draft was rebased onto it and every planned name was reconciled against the real files (`src/Core/DeathDetails.cs`, `src/Game/DeathReportHooks.cs`, `src/Core/DeathRecord.cs`, `src/Game/PlayerProbe.cs`). Two things remain PLANNED and are marked below: the `IrregularDamageSourceID` and `IsBossfight` wire parameters (present in the hooked signature but not yet read into `DeathDetails`), and the precalculated stats holder (`PlayerFinder.getLocalPlayerPrecalculatedStatsHolder`), which `PlayerProbe` does not use yet.

The goal: specific, defensible death advice computed from the recorded killing blow and the recorded defensive stats. Never a generic "cap your resistances, get health" list.

## 0. Hard rules (from the owner, restated as constraints)

Every rule in this file was designed inside these five constraints. They outrank every threshold below.

- C1. Never infer a one-shot or burst from the killing blow alone. One-shot and burst classifications require a real hit timeline (`DeathRecord.Hits > 0`). The killing blow is the last hit, not necessarily the big one.
- C2. Never assume a class, passive, item, encounter mechanic, or unique effect without recorded evidence. Advice may name recorded attackers, abilities, ailments, zones and stats only.
- C3. Never recommend stacking a resistance above the 75% cap unless shred, Shock, Poison self-shred, Marked for Death, or penetration is evidenced. Overcap does nothing against area-level penetration; it only absorbs resistance-reducing effects (see section 2, F2 and F3).
- C4. Never claim a change "would have prevented" the death unless units, ward, remaining health, modifiers, damage split and timeline are all known. In practice this means the mod states facts ("that hit was larger than your full health") and stops there.
- C5. When evidence is missing, say so. A downgrade line or an honest "not enough data" beats a guess.

Style: plain language, no double dashes, quote recorded values, say why the advice applies to this death.

## 1. Evidence model

Advice reads four evidence tiers. Most deaths will have only tiers 0 to 2: the first real in-game death had zero captured hits, so every rule must specify what it does with no timeline.

### 1.1 Tier 0: the death itself (always present)

| Field | From | Notes |
|---|---|---|
| `DeathRecord.Number`, `UtcTime`, `Character`, `CharacterClass`, `Level`, `Zone`, `Hardcore` | existing `DeathRecord.cs` | identity and context |
| `DeathRecord.Detection` | existing | "hook", "health", or "game"; how the death was noticed |
| `DeathRecord.GameDeathInfo` | `ProtectionClass.deathInformation.deathInfo` | the game's own death text, when readable |

### 1.2 Tier 1: the death report (on the base branch)

Source: two hooks installed by `src/Game/DeathReportHooks.cs`: `Il2Cpp.DeathInformationText.UpdateDeathInfo` (9 arguments, the local death screen, online and offline) and `Il2Cpp.PlayerActorSync.ReceiveDetailedDeathInfo` (10 arguments, the network report, filtered to the local player). Both funnel into `Capture()`, which reads arguments 0 to 7 into a `src/Core/DeathDetails.cs` record. `DeathDetails.Apply()` then merges them into `DeathRecord`, sets `DetailSource` to "game death report", sets `Kind` to `DeathKind.Reported` when no timeline exists, and appends a recorded `Ailment` to `AilmentsOnYou` (so ailment rules can fire without a timeline). Names are localized through `DeathInformationText.GetLocalizedAilmentName/AbilityName/AttackerName`. Wire parameter names below are from the lane briefing; the hook itself reads by index.

| Arg | Wire name (briefing) | `DeathDetails` field | Merged `DeathRecord` field | Status |
|---|---|---|---|---|
| 0 | primaryDamageType | `PrimaryElement` | `KillingElement` (string) | captured |
| 1 | secondaryDamageType | `SecondaryElement` | `SecondaryKillingElement` | captured; nullable, null means no second type |
| 2 | wasCrit | `Crit` | `KillingCrit` (`bool?`) | captured |
| 3 | damage | `Damage` | `KillingBlow` (float) | captured; units unproven (U3) |
| 4 | overkillDamage | `Overkill` | `OverkillDamage` | captured; units unproven (U3) |
| 5 | ailment | `Ailment` | `KillingAilment` (string) | captured; also appended to `AilmentsOnYou` |
| 6 | ability | `Ability` | `KillerAbility` | captured |
| 7 | attacker | `Killer` | `Killer` | captured |
| 8 | irregularDamageSourceID | PLANNED `IrregularDamageSourceID` | none | NOT captured: `Capture()` reads args 0 to 7 only; R-06 needs this one-line addition |
| 9 | isBossfight | PLANNED `IsBossfight` | none | NOT captured: 10-arg network report only; R-22 needs this one-line addition |

`DeathDetails.Text` and `RichText` (the composed death text, plain and colored) merge into `GameDeathInfo` and `GameDeathInfoRich`. The 9-arg local report's last argument is unmapped: it may be one of the two PLANNED rows or something else, which the next in-game probe can settle.

Units caution: whether `Damage` and `Overkill` are post-mitigation health loss or pre-mitigation ability damage is unproven until the in-game probe compares them against health deltas. Rules that compare `Damage` to the player's pool carry an explicit units precondition (R-05).

### 1.3 Tier 2: the defences snapshot (existing, being extended)

Existing: `DeathRecord.Defenses`, a `Dictionary<string, float>` built by `PlayerProbe.Defenses()` and normalized by `DefenseSnapshot.Normalize()`. On the base branch `PlayerProbe` still reads `ProtectionClass` fields (`uncappedPhysicalResistance` and friends, `armour`, `dodgeRating`). PLANNED, not yet implemented: the same values via `PlayerFinder.getLocalPlayerPrecalculatedStatsHolder`. Keys (missing key or non-finite value means unknown):

| Key | Meaning | Unit |
|---|---|---|
| `Res.Physical` ... `Res.Poison` (seven, one per element) | effective resistance after the 75% cap | percent (0 to 75) |
| `ResUncapped.<Element>` | resistance before the cap; headroom above 75 absorbs shred | percent |
| `Armor` | armor value (mitigation depends on area level) | flat |
| `Dodge` | dodge rating (chance depends on area level) | flat |
| `Block` | block chance | percent |
| `Endurance` | endurance percent | percent (cap 60) |
| `EnduranceThreshold` | health value below which endurance applies | flat |
| `CritAvoidance` | critical strike avoidance | percent (cap 100) |
| `StunAvoidance` | stun avoidance | flat |
| `Ward` | ward remaining, read after the killing hit's absorb | flat |
| `MaxHealth` | maximum health | flat |

Percent scale caution: `DefenseSnapshot.Scale()` decides 75 versus 0.75 from evidence and reports nothing when ambiguous. Rules must treat a missing `Res.*` key as "unknown", never as zero.

### 1.4 Tier 3: the hit timeline (existing; often absent)

`DeathRecord` fields computed by `DeathAnalyzer` from `HitEvent`s, only when hit hooks captured events (`DeathRecord.Hits > 0`):

| Field | Meaning |
|---|---|
| `Hits` | number of captured hits in the 12 s buffer |
| `Kind` | `Unknown`, `OneShot`, `Burst`, `DamageOverTime`, `Attrition` (meaningful only when `Hits > 0`) |
| `WindowSeconds`, `WindowDamage` | analysis window (5 s) and total damage in it |
| `DotDamage`, `DotByElement[7]` | the damage-over-time part |
| `DamageByElement[7]` | damage split across the seven elements |
| `TopSources` | top attackers by damage (`SourceShare.Name/Amount/Hits`) |
| `AilmentsOnYou` | canonical ailment names seen on the player (ticks plus ailment taps, 6 s memory) |
| `KillingBlow`, `KillingCrit`, `Killer`, `KillerAbility`, `KillingAilment`, `KillingElement` | derived from the last hit |
| `HitEvent.Time/Amount/ByElement/Source/Ability/Ailment/IsDot/Crit/HealthBefore/MaxHealth` | per-hit detail |

Analyzer constants (existing `DeathAnalyzer.cs`): window 5 s, burst window 2 s, one-shot = one hit ≥ 70% of max health, burst = ≥ 80% of max health inside 2 s, DoT dominant = ≥ 50% of window damage.

### 1.5 Tier 4: history (existing `DeathPatterns.cs`)

`PatternReport` over the last 20 deaths (`DefaultWindow`): `Deaths`, `TopKillers` (name, count), `ElementShares` (per-element share of deaths, each death counted once), `TopAilments`, `Kinds`, `Priorities`. Pattern rules read this tier only.

Reconciliation results (done 2026-10-02 against `cursor/advice-research-base` 4b5341e): the `DeathDetails` property names in section 1.2 are the real ones. `Ailment` arrives as an object the hook resolves through `GetLocalizedAilmentName`, with a displayName/ailmentName fallback, so rules compare localized name strings; if an AilmentID ever comes through instead, map by enum name, never by number (`docs/RESEARCH-game-api.md` warns the enum grows between builds). Still open: how `IrregularDamageSourceID` encodes "none" (the field is not captured yet, so this could not be checked), and which defences the precalculated stats holder will expose (not implemented yet).

## 2. Mechanics grounding (verified 2026-10-02)

Each fact lists its source with the page's own "updated" date where shown. Access date for every URL: 2026-10-02. Season 5 (1.5) patch notes and hotfix 1.5.0.1 contain no changes to these core systems, so the current support articles stand.

- F1. Resistances cap at 75% for every type including Physical and Poison. Negative resistance increases damage taken point for point. Resistances reduce damage over time as well as hits. Source: https://support.lastepoch.com/hc/en-us/articles/46361885661851-Resistances (updated 2026-07-14).
- F2. All enemies gain 1% penetration per area level, capped at 75% from area level 75 on. Penetration applies after the resistance cap, so overcap does not help against it. At area level 75+ a capped resistance is the baseline the game is balanced around (effective 0%), and anything below cap is effectively negative. Sources: https://support.lastepoch.com/hc/en-us/articles/46361871026971-Penetrations (updated 2026-02-24), https://support.lastepoch.com/hc/en-us/articles/46363284011419-If-75-resistance-is-the-max-and-the-enemies-get-penetration-per-level-up-to-75-do-I-need-150-resistance (updated 2026-02-12).
- F3. Overcap resistance only absorbs effects that reduce resistance: resistance shreds, Shock, Poison stacks, Marked for Death (25% all resistances, 8 s). Same sources as F2 plus the Negative Ailments article below.
- F4. Resistance shred: 5% per stack, 60% less effect against players (so 2% per stack), max 10 stacks, 4 s duration: at most 20% resistance stripped from a player. Shock: 5% negative lightning resistance and 20% increased stun chance per stack, 60% less against players (so 2% and 8%), max 10 stacks. Poison: each stack also applies 5% negative poison resistance, 60% less against players (so 2%), only the first 30 stacks count (up to 60%). Armor shred: 100 negative armor per stack, 4 s, unlimited stacks, and the current article lists no player reduction for it. Source: https://support.lastepoch.com/hc/en-us/articles/46361887879963-Negative-Ailments (updated 2026-02-14). Stack cap 10 (down from 20) per the 0.9 patch notes: https://forum.lastepoch.com/t/the-convergence-update-beta-0-9-patch-notes/51975.
- F5. Armor reduces all hits, is 70% as effective against non-physical hits, mitigation caps at 85% (so 59.5% against non-physical), scales down with area level, and does nothing against damage over time. Source: https://support.lastepoch.com/hc/en-us/articles/46361891210651-Armor (updated 2026-07-13). "Armor mitigation also applies to damage over time" exists only on specific items (for example Eternal Gauntlets, Oracle Amulet implicits, an experimental glove affix), so armor advice must never be given for a DoT death unless such an effect is recorded, which the mod cannot see. Item source: https://www.lastepochtools.com/db/items/IIwBgTCkBySQ.
- F6. Endurance: everyone starts at 20% endurance with a threshold of 20% of max health; endurance caps at 60%, the threshold has no cap; it reduces the part of any damage (hits and DoT) that lands below the threshold, including the crossing part of a big hit; it does not apply to damage absorbed by ward. Source: https://support.lastepoch.com/hc/en-us/articles/46361855013787-Endurance (updated 2026-02-13).
- F7. Ward is a shield that takes damage before health, has no cap, and decays (slower with ward retention, never below the ward decay threshold). Only Arena Echoes traps bypass ward, and they cannot kill. Sources: https://support.lastepoch.com/hc/en-us/articles/46361876030235-Ward (updated 2026-07-14), https://support.lastepoch.com/hc/en-us/articles/46363263694235-What-is-Ward-and-can-damage-bypass-it (updated 2026-02-20). Whether ward absorbs DoT: the ward article says damage is dealt to ward first, and the DoT article lists only resistances as DoT mitigation. Treat "ward absorbs DoT" as high confidence but not explicit: word DoT advice around resistances, endurance and sustain first. DoT source: https://support.lastepoch.com/hc/en-us/articles/46361875581211-Hit-Vs-Damage-Over-Time (updated 2026-02-13).
- F8. Enemy hits have a base 5% crit chance and a fixed 200% crit multiplier (double damage). Critical strike avoidance is rolled after the attacker's crit chance and multiplies it: at 100% avoidance no enemy hit can crit. "Less bonus damage taken from critical strikes" caps at 100% and removes only the bonus part. Damage over time cannot crit. Sources: https://support.lastepoch.com/hc/en-us/articles/46361891709211-Critical-Strikes (updated 2026-02-13), plus the Hit vs DoT article above.
- F9. Critical Vulnerability breaks capped avoidance: each stack gives +2% chance to receive a crit and -10% critical strike avoidance, max 10 stacks, 4 s. A player at 100% avoidance with one stack is effectively at 90%. Source: the Negative Ailments article (F4).
- F10. Dodge: chance from dodge rating, lower at higher area level, cap 85% (rule of thumb: rating near 10 times the area level gives about 50%). Dodging avoids the hit and its ailments; DoT cannot be dodged. Sources: https://www.lastepochtools.com/guide/section/dodge (Season 4 game guide mirror), https://maxroll.gg/last-epoch/resources/defenses-explained.
- F11. Block: block chance rolls on every hit including spells; a blocked hit is mitigated by block effectiveness, which scales with area level and caps at 85%; blocking counts as being hit; DoT cannot be blocked. Source: https://support.lastepoch.com/hc/en-us/articles/46361876155547-Block (updated 2026-07-14).
- F12. Glancing blow: 35% less damage from the hit, chance caps at 100%, hits only. Source: https://support.lastepoch.com/hc/en-us/articles/46361871332251-Glancing-Blow (updated 2026-02-13).
- F13. Stun: base duration 0.4 s. A non-player hit must deal more than 5% of the target's max health to have any chance to stun; every 100 stun avoidance raises that threshold by 1%; players have 250 + 5 per level inherent stun avoidance; Shock raises stun chance. Source: https://support.lastepoch.com/hc/en-us/articles/46361891772443-Stun (updated 2026-02-13).
- F14. Freeze: base duration 1.2 s; freeze chance is the attacker's freeze rate divided by the target's (max health + current ward), so a bigger pool directly lowers freeze chance; the first 15 Frostbite stacks each add 20% increased chance to be frozen. Source: https://support.lastepoch.com/hc/en-us/articles/46361855307035-Freeze (updated 2026-07-14).
- F15. Corruption raises monster health and damage (the official article gives no per-point formula; the often quoted 0.5% per point each is a 2023 community figure, treat as outdated). Empowered monoliths are area level 100 and start at 100 corruption with no cap. Echo modifiers exist that reduce player resistances, raise enemy crit chance, or raise enemy damage. Sources: https://support.lastepoch.com/hc/en-us/articles/46363308884635-What-is-affected-by-Corruption (updated 2026-02-12), https://maxroll.gg/last-epoch/monolith/empowered-guide, https://maxroll.gg/last-epoch/monolith/the-last-ruin-timeline-guide.
- F16. Community health floors for empowered (level 100) content: "anything under 2000 HP is very low", common targets 3000 to 3500 health, ward builds 4000 to 5000 ward. These are community guidance, not game rules: always Medium confidence. Sources: https://forum.lastepoch.com/t/what-do-you-do-to-solve-your-survivability-when-you-first-reach-empowered-monos/73162, https://www.thegamer.com/last-epoch-all-defensive-layers-explained/, https://steamcommunity.com/app/899770/discussions/0/4295943164872849597/.
- F17. Ailment damage and debuff table (from the Negative Ailments article, F4): Ignite 40 fire over 2.5 s; Bleed 53 physical over 3 s; Poison 28 poison over 3 s plus the self-shred; Frostbite 50 cold over 3 s plus freeze chance; Electrify 44 lightning over 2.5 s; Time Rot 60 void over 3 s, max 12 stacks, +5% stun duration received per stack; Doom 400 void over 4 s, max 4 stacks, +4% melee damage taken per stack; Damned 35 necrotic over 2.5 s, 20% reduced health regen; Plague 150 poison over 4 s, spreads; Spreading Flames 200 fire over 4 s, spreads; Abyssal Decay void over time, remaining damage applies when hit. DoTs cannot crit and cannot be dodged, blocked or glanced (F7, F8).
- F18. Season 5 status: patch 1.5 launched 2026-10-01 with hotfix 1.5.0.1 the same day; no core defense changes found in the notes. Sources: https://forum.lastepoch.com/t/season-5-rage-of-the-frostborn-patch-notes/81789 (2026-09-25), https://forum.lastepoch.com/t/last-epoch-hotfix-1-5-0-1-notes/81887 (2026-10-01). Re-verify after any 1.5.x balance patch.

Uncertainty register (flag, never paper over):
- U1. Armor shred against players: the current official article lists flat 100 negative armor per stack with no player reduction; an older forum reading claimed 2% per stack. Use the flat-100 figure and mark it Medium confidence.
- U2. Ward versus DoT: believed absorbed, not explicit in official text (F7).
- U3. `Damage`/`Overkill` units in the death report: unproven until the probe compares them to health deltas (section 1.2).
- U4. Corruption per-point scaling: official article confirms direction only (F15).
- U5. Armor and dodge mitigation need the area level, which the mod does not record yet. Never quote a mitigation percent from a recorded armor or dodge value.
- U6. `IrregularDamageSourceID` values are unmapped, and the field is not captured yet (section 1.2). The rule that uses it may say "hazard or ground effect" and nothing more specific.

## 3. Classifying the killing blow (runs before the rules)

Most rules need to know whether the death was a hit, an ailment, or an irregular source. Decision order, first match wins:

1. Ailment death: the report's `Ailment` (or `KillingAilment`) names a known damaging ailment (`Ailments.ByName(...).IsDot == true`). DoTs cannot crit (F8): if `Crit` is also true the data conflicts, so mark the death Low confidence and let R-01 skip.
2. Irregular source: `IrregularDamageSourceID` is present and not the "none" value (PLANNED field, not yet captured; the none encoding is unconfirmed until it is), or no attacker and no ability but a DoT-ish ailment. Ground effects, traps and hazards live here.
3. Direct hit: `Killer` or `Ability` present. `Crit` is meaningful.
4. Otherwise: unknown. Only tier-agnostic rules and the fallback may fire.

Element resolution: map `PrimaryElement` by enum name through `Elements.TryParse`. If `SecondaryElement` is a real second type, wording may say "mostly {primary} with some {secondary}" but must never quantify the split: the report carries no ratio, and only the timeline (`DamageByElement`) gives shares.

## 4. The rule matrix (summary)

Rank order is the default display order; section 5 defines the final top-3 selection. "Evidence" lists the tiers from section 1. Confidence assumes all required fields are present; section 6 defines the per-death data line.

| ID | Rank | Fires when (short) | Advice in one line | Evidence | Confidence |
|---|---|---|---|---|---|
| R-01 crit-killing-blow | 1 | killing blow was a crit | get 100% crit avoidance (enemy crits deal double) | T1 (+T2 for the number) | High |
| R-02 res-gap-killing-element | 2 | killing element's resistance known and below 75% | cap that resistance; below cap is negative after area penetration | T1+T2 | High |
| R-03 shred-recorded | 3 | a recorded ailment strips the killing element's resistance or armor | overcap that resistance or remove the shred source | T1/T3 + T2 | High |
| R-04 dot-kill | 4 | killing ailment is a DoT, or the window was DoT dominated | resistances, endurance, ward, sustain; armor, dodge, block do nothing | T1 or T3 | High |
| R-05 overkill-exceeds-pool | 5 | overkill alone ≥ recorded max health (units validated) | that hit was larger than your full health: raise the pool | T1+T2 | High, else skip |
| R-06 irregular-source | 6 | `IrregularDamageSourceID` set (planned capture) | hazard or ground effect: move; treat as DoT for defenses | T1 | Medium |
| R-07 oneshot-ehp | 7 | timeline says OneShot | bigger pool and endurance threshold | T3+T2 | High |
| R-08 burst-avoidance | 8 | timeline says Burst | dodge and block stop hits; pool buys reaction time | T3 (+T2) | High |
| R-09 attrition-sustain | 9 | timeline says Attrition, or small overkill after many hits | sustain: leech, regen, potions | T3 | Medium |
| R-10 res-capped-next-layer | 10 | killing element's resistance already at cap, no shred | resistance is not the gap; pool and avoidance are | T1+T2 | High |
| R-11 crit-vulnerability-breaks-avoidance | 11 | Crit, avoidance ≥ 100%, Critical Vulnerability recorded | CV stacks subtract avoidance: cleanse or avoid the applier | T1+T2+T3 | High |
| R-12 freeze-frostbite | 12 | Freeze/Chill/Frostbite recorded on a cold death | bigger pool lowers freeze chance; cold res for Frostbite | T1/T3 (+T2) | Medium |
| R-13 stun-recorded | 13 | Stun recorded | stun avoidance and max health raise the stun threshold | T3 (+T2) | Medium |
| R-14 amplifier-ailment | 14 | Marked for Death, Acid Skin, Stagger, Efficacious Toxin, Mimic Feast etc. recorded | name the recorded amplifier and its recorded counter | T3 | Medium |
| R-15 low-pool | 15 | pool known and below the community floor for the level | raise health or ward toward the floor | T2 (+T0) | Medium |
| R-16 armor-for-physical | 16 | physical hit damage dominates, armor known low or absent | armor is the main physical hit mitigation | T1/T3 (+T2) | Medium |
| R-17 no-avoidance-layer | 17 | hit death, dodge and block both recorded at 0 | add one avoidance layer | T2+T1/T3 | Medium |
| R-18 endurance-gap | 18 | endurance known below 60% on a health damage death | raise endurance toward 60% and the threshold | T2+T1/T3 | Medium |
| R-19 repeat-killer | 19 | same attacker in ≥ 3 of the last 20 deaths | pattern, not luck: learn the wind-up | T4 | High |
| R-20 repeat-ability | 20 | same ability in ≥ 3 of the last 20 deaths | that specific ability is the problem | T4 | High |
| R-21 repeat-element | 21 | one element ≥ 50% of recent death damage | shore up that resistance across deaths | T4+T2 | Medium |
| R-22 boss-fight-context | 22 | `IsBossfight` true (planned capture) | telegraph discipline; never invent mechanics | T1 | Medium |
| R-23 area-penetration-context | 23 | evidence of area level ≥ 75 (planned field) or rides on R-02 | below-cap resistance is negative after penetration | T0/T1 | Medium |
| R-24 unknown-no-data | 24 | nothing actionable recorded | honest "not enough data" plus what to enable | T0 only | n/a |

## 5. The rules in detail

Placeholders in wording templates use the exact field names from section 1, in {braces}. "Skip" means the rule does not fire at all. "Downgrade" means it fires with weaker wording and a lower confidence label. "Data line" means the rule stays silent and the missing field is named in the per-death data-confidence line (section 6).

### R-01 crit-killing-blow (rank 1)

- Required evidence: `Crit` (or `KillingCrit`). Optional: `CritAvoidance`, `Damage`, `AilmentsOnYou`.
- Conditions: `Crit == true`. If `CritAvoidance` known and < 99.5: name the gap. If `CritAvoidance` ≥ 99.5: only fire when `AilmentsOnYou` contains "Critical Vulnerability" (then R-11 owns the wording); otherwise the record contradicts itself, so skip and note the conflict in the data line.
- Thresholds and justification: enemy crit multiplier is a fixed 200% and avoidance at 100% multiplies enemy crit chance to zero (F8). The 99.5 boundary tolerates float rounding of a true 100.
- Exclusions: killing blow classified as an ailment or DoT (DoTs cannot crit, F8); `Crit` false; avoidance at cap without Critical Vulnerability recorded.
- Wording (avoidance known): Title: "Get critical strike avoidance to 100%". Body: "The killing blow was a critical strike for {Damage}. Enemy crits deal double damage, and your critical strike avoidance was {CritAvoidance}%: at 100% no enemy hit can crit you."
- Wording (avoidance unknown): Body: "The killing blow was a critical strike for {Damage}. Enemy crits deal double damage. Check critical strike avoidance on your character sheet: at 100% no enemy hit can crit you."
- Confidence label: "High confidence: the crit flag was recorded by the game's own death report." (Unknown avoidance: "Medium confidence: the crit was recorded, but your avoidance was not.")
- Missing data: `Crit` unknown: skip. `CritAvoidance` missing: downgrade to the second wording. `Damage` missing: drop "for {Damage}".
- Counterexample: the killing blow was an Ignite tick (`Ailment` = Ignite). DoTs cannot crit, so the rule must not fire even if a buggy report sets `Crit`.

### R-02 res-gap-killing-element (rank 2)

- Required evidence: killing element (`PrimaryElement` or `KillingElement`), and `Res.<Element>`. Optional: `ResUncapped.<Element>`, `DamageByElement` share, `Level`, `Zone`.
- Conditions: element resolved; `Res.<Element>` < 74.5 (half a point of tolerance around the 75 cap, F1). If a timeline exists, require the element to be ≥ 25% of `DamageByElement` total or be the killing element; the killing element alone is enough.
- Thresholds and justification: the cap is 75% (F1). Below cap at high area level is worse than "no protection": enemies penetrate 1% per area level up to 75%, applied after the cap, so each missing point is a point of increased damage taken (F2).
- Exclusions: `Res.<Element>` ≥ 74.5 (R-10 owns that case). A matching shred ailment recorded (R-03 owns it: there the fix is overcap, not cap). Element unknown: skip.
- Wording: Title: "Cap {element} resistance: you had {Res}%". Body: "The killing blow was {element}. Your {element} resistance was {Res}%, which is {gap} points under the 75% cap. In high level areas enemies also penetrate up to 75% resistance, so below the cap you are not just unprotected, you take increased damage."
- Confidence label: "High confidence: the killing damage type and your resistance were both recorded."
- Missing data: element unknown: skip. `Res.<Element>` missing: downgrade to "The killing blow was {element}. Check your {element} resistance: every point under the 75% cap is damage you take for free." and Medium confidence. Timeline missing: fire anyway on the killing element (no share check possible).
- Counterexample: `Res.Fire` reads 75% and the killing blow was fire: the rule must not fire (that is R-10).

### R-03 shred-recorded (rank 3)

- Required evidence: `AilmentsOnYou` (tier 3) or a killing `Ailment` that self-shreds (Poison). Optional: `ResUncapped.<Element>`, `Res.<Element>`, `Armor`.
- Conditions: any of these recorded on the player: a resistance shred matching the killing element ("Fire Resistance Shred" etc.), "Resistance Shred" generic, "Shock" when the killing element is lightning, "Poison" when the killing element is poison (each stack self-shreds 2%, first 30 stacks, F4), "Marked for Death" (25% all resistances, F3), "Armor Shred" or "Stagger" when the death was hit-based.
- Thresholds and justification: shred versus players is 2% per stack, max 10 stacks, so at most 20% resistance stripped, applied before the cap (F4). That is exactly what overcap headroom absorbs (F3), which is why this rule is the only one allowed to recommend overcap (C3).
- Exclusions: no shred-class ailment recorded: never mention overcap. DoT death where the shred does not match the DoT's element: skip (a cold shred did not help the ignite kill you).
- Wording (resistance shred): Title: "Your {element} resistance was being shredded". Body: "You had {Ailment} stacks on you. Each stack strips 2% {element} resistance (up to 20% at 10 stacks), applied before the 75% cap. Your sheet showed {ResUncapped}% uncapped: keep {element} resistance at least 20 points over the cap against this enemy, or kill the shredder first."
- Wording (armor shred): Title: "Your armor was being shredded". Body: "Armor Shred removes about 100 armor per stack with no stack cap. Kill the shredder first, or lean on a layer shred cannot touch: dodge, block, or a bigger pool." (Medium confidence, U1.)
- Confidence label: "High confidence: the shred ailment was recorded on you."
- Missing data: `AilmentsOnYou` unavailable (no timeline and no ailment taps): skip; shred is never assumed (C2). `ResUncapped` missing: drop the sentence that quotes it.
- Counterexample: `AilmentsOnYou` is empty and the killing blow was fire: advice must say "cap fire resistance" (R-02) or "resistance was not the gap" (R-10), never "overcap fire resistance".

### R-04 dot-kill (rank 4)

- Required evidence: killing `Ailment` that is a known DoT (`Ailments.ByName(ailment).IsDot`), or `KillingAilment`, or a timeline with `DotDamage / WindowDamage` ≥ 0.5 (`DeathAnalyzer.DotFraction`). Optional: `Res.<Element>`, `DotByElement`, `Endurance`.
- Conditions: classification step (section 3) says ailment death, or the timeline is DoT dominated.
- Thresholds and justification: 50% DoT share is the existing analyzer boundary. DoTs are reduced by resistances and endurance (health portion), absorbed by ward (U2), and healed through by leech and regen; they cannot be dodged, blocked, glanced, or crit (F7, F8, F17). Armor does nothing unless a recorded item says otherwise, which the mod cannot see (F5).
- Exclusions: the killing blow was a hit and no timeline shows DoT dominance: skip. `Crit` true with a DoT ailment is a data conflict: fire with Low confidence and note the conflict.
- Wording (per ailment, quoting the recorded name): Title: "{Ailment}: {element} resistance and sustain". Body examples: Ignite: "Ignite deals fire damage over time. Armor, dodge and block do nothing against damage over time: fire resistance, endurance, ward and sustain (leech, regen, potions) are what help." Bleed: "Bleed deals physical damage over time, and armor does not reduce it. Physical resistance, endurance and sustain do." Poison: "Poison deals poison damage over time, and each stack also strips 2% of your poison resistance (first 30 stacks). Poison resistance with overcap headroom, and sustain." Damned: "Damned deals necrotic damage over time and cuts your health regen by 20%: lean on leech or ward instead of regen." Time Rot: "Time Rot deals void damage over time and makes stuns on you last longer. Void resistance, sustain, and stun avoidance." Doom: "Doom deals void damage over time and makes you take 4% more melee damage per stack: keep out of melee of the source." Frostbite: "Frostbite deals cold damage over time and each of the first 15 stacks makes you 20% more likely to be frozen: cold resistance, sustain, and a bigger pool."
- Confidence label: "High confidence: the killing ailment was recorded." (Timeline-only: "Medium confidence: no death report, but most of the recorded damage was over time.")
- Missing data: ailment name unknown but timeline DoT dominated: generic wording "Most of the damage that killed you was over time. ..." Element unknown: drop the resistance sentence, keep the DoT mechanics sentence.
- Counterexample: the death report shows a melee hit with `Crit` false and no ailment, and no timeline exists: the rule must not fire.

### R-05 overkill-exceeds-pool (rank 5)

- Required evidence: `Damage`, `Overkill`, `MaxHealth`, and validated report units (U3). Optional: `Ward` (after hit), `Hits`.
- Conditions: `Overkill` ≥ `MaxHealth`. Interpretation: the killing hit alone dealt more than the player's entire maximum health, so it would have killed from full health no matter what came before. This is a comparison of two recorded numbers, not a prevention claim (C4).
- Thresholds and justification: none beyond the comparison itself; the rule exists because "that hit was bigger than your whole pool" is the strongest factual framing a death report can support without a timeline.
- Exclusions: units not validated (U3): downgrade to the fact line below and Medium confidence. `MaxHealth` unknown: skip (the owner's constraint: large hits are judged against the pool only when the pool is known). `Ward` after hit > 0 contradicts the death (ward takes damage first, F7): note the inconsistency in the data line and skip the pool comparison.
- Wording: Title: "That one hit was larger than your full health". Body: "The killing hit dealt {Damage}, and {Overkill} of that was more than needed to kill you: it would have killed you from your full {MaxHealth} health on its own. Against a hit that size the only answers are a much bigger pool (health or ward), not standing there, or recorded mitigation you were missing."
- Wording (units unvalidated downgrade): "The game's death report says the killing blow dealt {Damage} with {Overkill} overkill. The mod has not yet confirmed how that number maps to your health, so treat it as the game's own figure."
- Confidence label: "High confidence: killing damage, overkill and your max health were all recorded." (Downgrade: "Low confidence: the damage figure comes straight from the game and its units are unconfirmed.")
- Missing data: `Overkill` missing: skip (the comparison is the rule). `Damage` missing: skip. Timeline present: R-07/R-08 usually say more; this rule still may fire alongside them.
- Counterexample: `Overkill` is 50 against a 2,000 max health: the player died to a finishing blow while already low, so a "big hit" framing is wrong and the rule must not fire (R-09 owns that death if a timeline exists).

### R-06 irregular-source (rank 6)

- Required evidence: `IrregularDamageSourceID` present and not the none value, or (no attacker, no ability, and a DoT-class or absent ailment). Optional: `Ailment`, `Zone`. PLANNED: the wire value sits at args[8] of the 10-arg report but `Capture()` does not read it yet; until that one-line `DeathDetails` addition lands, only the fallback half of this condition can fire.
- Conditions: classification step says irregular source.
- Thresholds and justification: ground effects and hazards in Last Epoch are typically damage over time or repeated pulses (F7 names Fire Aura, Tornado, Consecrated Ground as area DoT examples), so the correct generic counters are resistances, ward, endurance, sustain, and moving. The ID values are unmapped (U6), so the rule never names a specific hazard (C2).
- Exclusions: a recorded attacker or ability (a normal monster hit): skip. Any specific mechanic claim: forbidden.
- Wording: Title: "A hazard or ground effect killed you". Body: "The killing damage came from a hazard or ground effect, not a direct monster hit. Those are usually damage over time: they cannot be dodged, blocked or glanced. {element} resistance, ward, endurance and simply moving out are what help."
- Confidence label: "Medium confidence: the game flagged an irregular damage source, but the mod cannot tell which hazard it was."
- Missing data: element unknown: drop the resistance phrase, keep "the matching resistance". `IrregularDamageSourceID` absent: skip.
- Counterexample: `Killer` = "Rogue Archer" with `Ability` = "Multishot": a normal hit, rule must not fire.

### R-07 oneshot-ehp (rank 7)

- Required evidence: `Kind` == OneShot with `Hits` > 0, and `MaxHealth` > 0 (the analyzer's shape-only fallback when max health is unknown is not enough for this rule's wording). Optional: `KillingBlow`, `Endurance`, `EnduranceThreshold`, `Ward`.
- Conditions: one recorded hit took ≥ 70% of max health (`OneShotFraction`).
- Thresholds and justification: 70% is the existing analyzer constant. The answer to a single huge hit is effective health: max health, ward, and endurance threshold (which decides how much of the hit gets reduced by endurance, F6).
- Exclusions: `Hits` == 0 (C1: never infer a one-shot from the killing blow alone). `MaxHealth` unknown: downgrade to R-05's fact line if overkill allows, else skip.
- Wording: Title: "Raise your effective health pool". Body: "One hit took {KillingBlow} damage, {share}% of your {MaxHealth} health. More health, more ward, and a higher endurance threshold ({EnduranceThreshold} now) are what let you survive the next one."
- Confidence label: "High confidence: the hit timeline and your max health were both recorded."
- Missing data: `EnduranceThreshold` unknown: drop that clause. `KillingBlow` unknown: drop the number, keep the share.
- Counterexample: `Hits` == 0 and the death report shows a 1,800 killing blow: the rule must not fire; that blow may have been the tenth hit.

### R-08 burst-avoidance (rank 8)

- Required evidence: `Kind` == Burst with `Hits` > 0. Optional: `WindowDamage`, `MaxHealth`, `Dodge`, `Block`.
- Conditions: ≥ 80% of max health inside 2 s (`BurstFraction`, `BurstSeconds`), or the shape fallback (several hits within 2 s, max health unknown) with Medium confidence.
- Thresholds and justification: existing analyzer constants. Dodge avoids a hit and its ailments entirely (F10); block mitigates a hit by block effectiveness (F11); both work on spells as well as attacks.
- Exclusions: DoT dominated window (R-04 owns it; dodge and block do nothing there). `Hits` == 0: skip (C1).
- Wording: Title: "Avoid hits: dodge and block". Body: "You took {WindowDamage} damage in about {WindowSeconds} seconds across {Hits} hits. Dodge rating and block chance stop hits before they land, and a bigger pool buys the second you need to react or potion."
- Confidence label: "High confidence: the hit timeline was recorded." (Shape fallback: Medium.)
- Missing data: `MaxHealth` unknown: drop the percent framing. `Dodge`/`Block` unknown: keep generic (do not claim the player lacks them; that is R-17's job and needs the snapshot).
- Counterexample: `DotDamage` is 60% of `WindowDamage`: the rule must not recommend dodge against a bleed.

### R-09 attrition-sustain (rank 9)

- Required evidence: `Kind` == Attrition with `Hits` > 0, or (`Hits` ≥ 3 and `Overkill` known and < 10% of `MaxHealth`). Optional: `WindowSeconds`, `TopSources`.
- Conditions: worn down over several seconds, or the killing blow was a small finishing hit.
- Thresholds and justification: the 10% overkill boundary marks "you were already nearly dead": the fix is what happened over the previous seconds, which is sustain (leech is applied over time, regen per second, potions on belt slots) and fewer chip hits landing (F16 community consensus on sustain as a required layer).
- Exclusions: single-hit deaths (R-07). DoT dominated (R-04). No timeline and overkill unknown: skip.
- Wording: Title: "Improve sustain". Body: "You were worn down over {WindowSeconds} seconds and the killing blow was only {Damage}. Health regen, leech, health on kill and potion discipline matter more here than any single mitigation layer."
- Confidence label: "Medium confidence: the timeline shows a slow death, but the mod cannot see your leech or regen."
- Missing data: `Damage` unknown: drop "was only {Damage}". `WindowSeconds` unknown: skip.
- Counterexample: `Hits` == 1: the rule must not fire.

### R-10 res-capped-next-layer (rank 10)

- Required evidence: killing element, `Res.<Element>` ≥ 74.5. Optional: `ResUncapped.<Element>`.
- Conditions: resistance at cap for the killing element and no matching shred recorded (R-03's exclusion is reciprocal).
- Thresholds and justification: at cap, more resistance does nothing against normal hits, and overcap does nothing against area penetration (F1, F2). The honest advice redirects to the layers that still help: pool, endurance, armor (hits), dodge, block.
- Exclusions: any matching shred or Marked for Death recorded (R-03). Element unknown: skip.
- Wording: Title: "{element} resistance was already capped". Body: "Your {element} resistance was {Res}% (capped{, {ResUncapped}% before the cap, which only helps against shred}), and the {element} damage still killed you. Resistance is not the gap here: health, ward, endurance, and avoiding the hit are the next layer."
- Confidence label: "High confidence: your resistance was recorded at cap."
- Missing data: `ResUncapped` missing: drop the parenthetical.
- Counterexample: `Res.Fire` 75% but "Fire Resistance Shred" was recorded: the rule must not say "resistance is not the gap"; the shred made it the gap again (R-03).

### R-11 crit-vulnerability-breaks-avoidance (rank 11)

- Required evidence: `Crit` true, `CritAvoidance` ≥ 99.5, and "Critical Vulnerability" in `AilmentsOnYou`.
- Conditions: all three. This resolves the apparent contradiction "crit despite capped avoidance".
- Thresholds and justification: each Critical Vulnerability stack subtracts 10% crit avoidance and adds 2% chance to be crit, max 10 stacks (F9), so one stack turns 100% into an effective 90%.
- Exclusions: no Critical Vulnerability recorded: skip and let the data line note the unexplained crit. Avoidance below cap: R-01 owns the wording.
- Wording: Title: "Critical Vulnerability broke your crit immunity". Body: "Your critical strike avoidance read {CritAvoidance}%, but you had Critical Vulnerability on you: each stack subtracts 10% avoidance, so the crit got through. Cleanse ailments, or kill whatever applies it before the stacks build."
- Confidence label: "High confidence: the crit, your avoidance, and the ailment were all recorded."
- Missing data: `AilmentsOnYou` unavailable: skip (never assume the ailment, C2).
- Counterexample: avoidance 100%, Crit true, no Critical Vulnerability: the rule must not invent an explanation; the data line flags the conflict instead.

### R-12 freeze-frostbite (rank 12)

- Required evidence: "Freeze", "Chill" or "Frostbite" in `AilmentsOnYou`, or killing `Ailment` == Frostbite. Optional: `MaxHealth`, `Ward`, `Res.Cold`.
- Conditions: cold-aligned death (killing element cold, or cold share ≥ 25% with a timeline) plus one of the three ailments recorded.
- Thresholds and justification: freeze chance divides by (max health + current ward), so a bigger pool directly lowers it; the first 15 Frostbite stacks each add 20% freeze chance; freeze base duration 1.2 s (F14).
- Exclusions: no freeze-family ailment recorded: skip. Non-cold death with a stray Chill: downgrade to a mention, not a top tip.
- Wording: Title: "You were frozen (or chilled into it)". Body: "Freeze chance scales against your max health plus current ward: your pool was {MaxHealth} health. Each Frostbite stack (up to 15) makes you 20% easier to freeze. A bigger pool and capped cold resistance ({Res.Cold}% now) are the counters; keep a movement skill for the chill."
- Confidence label: "Medium confidence: the ailment was recorded, but freeze chance also depends on the attacker's freeze rate, which the mod cannot see."
- Missing data: `MaxHealth` unknown: drop the pool sentence. `Res.Cold` unknown: drop the parenthetical.
- Counterexample: `AilmentsOnYou` lists only "Ignite" on a fire death: the rule must not fire.

### R-13 stun-recorded (rank 13)

- Required evidence: "Stun" in `AilmentsOnYou`. Optional: `StunAvoidance`, `MaxHealth`, "Shock" co-recorded.
- Conditions: stun recorded during the fatal window.
- Thresholds and justification: a non-player hit must exceed 5% of max health to have any stun chance; each 100 stun avoidance raises that by 1%; players have 250 + 5 per level inherent; Shock adds 8% stun chance per stack versus players (F13, F4).
- Exclusions: no Stun recorded: skip.
- Wording: Title: "Get stun avoidance". Body: "You were stunned during the fight that killed you. Hits above 5% of your max health ({MaxHealth}) can stun; every 100 stun avoidance raises that threshold by 1% (you had {StunAvoidance}). A bigger health pool raises it too."
- Confidence label: "Medium confidence: the stun was recorded; the attacker's increased stun chance is not visible."
- Missing data: `StunAvoidance` unknown: drop the parenthetical. `MaxHealth` unknown: drop the 5% sentence's number.
- Counterexample: a death with no Stun in `AilmentsOnYou`: the rule must not fire, even if the player was probably stun-locked; unrecorded is unrecorded (C2).

### R-14 amplifier-ailment (rank 14)

- Required evidence: one of the following in `AilmentsOnYou`: "Marked for Death", "Acid Skin", "Stagger", "Efficacious Toxin", "Mimic Feast", "Spiders", "Frailty of Aberroth", "Curse of Aberroth", "Exposed Flesh", "Decrepify", "Melee Defense Shred".
- Conditions: recorded presence. Each ailment maps to one fixed sentence (below) so no mechanic is ever invented.
- Thresholds and justification: all values from the official Negative Ailments article (F4): Marked for Death -25% all resistances; Acid Skin +20% chance to be crit; Stagger -100 armor and 10% increased damage taken; Efficacious Toxin +12% damage over time taken; Mimic Feast void DoT and 100% less leech; Spiders +5% chance to be crit per stack (max 4); Curse of Aberroth -10% all resistances, cannot be cleansed; Exposed Flesh -15% cold resistance and +30% freeze chance; Decrepify 15% more DoT taken; Melee Defense Shred 10% more melee damage taken per stack.
- Exclusions: none recorded: skip. Naming any effect not in the fixed table: forbidden.
- Wording: Title: "{Ailment} was on you". Body: one recorded-effect sentence plus one counter sentence, for example Marked for Death: "Marked for Death lowers all your resistances by 25% for 8 seconds. Overcapped resistance absorbs it; otherwise play defensively until it falls off."
- Confidence label: "Medium confidence: the ailment was recorded, but its stack count and remaining duration were not."
- Missing data: `AilmentsOnYou` unavailable: skip.
- Counterexample: `AilmentsOnYou` contains only "Slow": not an amplifier, rule must not fire.

### R-15 low-pool (rank 15)

- Required evidence: `MaxHealth` (and `Ward` if present), plus `Level` or endgame context. Optional: `Zone`.
- Conditions: `Level` ≥ 70 and `MaxHealth` + max(0, `Ward`) < 1500. Between 1500 and 2000: fire only as a supporting tip when R-05 or R-07 also fired.
- Thresholds and justification: community floors for level 100 content are "under 2000 is very low", 2000 minimum, 3000 to 3500 comfortable, ward builds 4000 to 5000 ward (F16). The 1500 trigger is deliberately conservative so the rule only fires on clearly thin pools. Community guidance, always Medium confidence.
- Exclusions: `Level` < 70 (campaign pools are smaller by design; the floor does not map). Pool ≥ 2000: skip. `MaxHealth` unknown: skip.
- Wording: Title: "Your pool is thin for this content". Body: "You had {MaxHealth} health{ and {Ward} ward} at level {Level}. Community guides treat roughly 2,000 health (or about 4,000 ward on ward builds) as the floor for level 100 content. Hybrid health affixes, Stout idols and passive health nodes are the usual sources."
- Confidence label: "Medium confidence: your pool was recorded, but the right pool depends on build and content, and the 2,000 figure is community guidance, not a game rule."
- Missing data: `Ward` unknown: drop the ward clause. `Level` unknown: skip (the floor needs context).
- Counterexample: a level 35 character with 900 health dying in the campaign: the rule must not fire.

### R-16 armor-for-physical (rank 16)

- Required evidence: physical is the killing element or ≥ 25% of timeline damage, and the death was hit-based. Optional: `Armor`.
- Conditions: classification says direct hit; physical dominant. If `Armor` known and > 0, wording quotes it without judging it (mitigation needs area level, U5).
- Thresholds and justification: armor is the strongest general mitigation against physical hits (100% effectiveness, cap 85%, F5), and physical resistance is rarer on gear than other resistances.
- Exclusions: any DoT dominance or a physical DoT (Bleed) as the killer: armor does nothing there (F5); R-04 owns it. Killing element not physical and physical share < 25%: skip.
- Wording: Title: "Stack armor for physical hits". Body: "Physical hits were what killed you. Armor mitigates every physical hit (your sheet showed {Armor}); physical resistance and block help too. Armor does nothing for bleeds, so check whether the kill was a hit or a bleed before investing."
- Confidence label: "Medium confidence: the damage type was recorded, but how much your armor mitigates depends on the area level, which the mod does not record."
- Missing data: `Armor` unknown: drop the parenthetical.
- Counterexample: `KillingAilment` == "Bleed": physical damage, but armor must not be recommended; the rule must not fire.

### R-17 no-avoidance-layer (rank 17)

- Required evidence: `Dodge` and `Block` both known and both 0, on a hit-based death. Optional: `Hits`.
- Conditions: classification says direct hit or burst; the snapshot recorded zero dodge rating and zero block chance.
- Thresholds and justification: dodge avoids a hit and its ailments (F10); block mitigates every hit including spells (F11). Having neither means every hit landed at full value. Community checklists treat "at least one hit layer" as standard (F16).
- Exclusions: either stat missing from the snapshot (unknown is not zero, section 1.3): skip. DoT death: skip (neither layer applies, F7).
- Wording: Title: "Add one avoidance layer". Body: "Your snapshot showed 0 dodge rating and 0 block chance, so every hit landed. Either layer stops a fraction of hits before they land, and dodge also prevents the ailments those hits would apply."
- Confidence label: "Medium confidence: your dodge and block were recorded, but the right layer depends on your class and gear, which the mod does not inspect."
- Missing data: one of the two keys missing: skip (cannot prove "no layer").
- Counterexample: `Dodge` = 800 recorded: the rule must not fire, even though 800 rating may be a low chance at high area level (U5).

### R-18 endurance-gap (rank 18)

- Required evidence: `Endurance` known and < 59.5, on a death where health took damage (always true on death; ward is depleted first, F7). Optional: `EnduranceThreshold`, `MaxHealth`.
- Conditions: endurance below the 60% cap with tolerance.
- Thresholds and justification: endurance reduces the part of every hit and DoT that lands below the threshold, including the tail of a big hit; cap 60%, base 20%, threshold base 20% of max health (F6). It is the layer that most often turns a lethal hit into a survivable one for health builds.
- Exclusions: endurance ≥ 59.5: skip. `Endurance` unknown: skip. Do not tell ward builds to stack endurance: if `Ward` after hit > `MaxHealth`, downgrade to a mention (endurance does not protect ward, F6).
- Wording: Title: "Raise endurance and its threshold". Body: "Your endurance was {Endurance}% (cap 60%) with a threshold of {EnduranceThreshold}. Endurance cuts the part of each hit below the threshold, including the lethal tail of a big one. Threshold rolls on belts; endurance rolls as a suffix."
- Confidence label: "Medium confidence: your endurance was recorded, but whether it would have engaged depends on your health before the killing hit, which is only known with a timeline."
- Missing data: `EnduranceThreshold` unknown: drop the threshold sentence.
- Counterexample: `Endurance` = 60 recorded: the rule must not fire.

### R-19 repeat-killer (rank 19)

- Required evidence: `PatternReport.TopKillers` with a count ≥ 3 over the last 20 deaths (`DeathPatterns.DefaultWindow`).
- Conditions: the same attacker name killed the player at least 3 times in the window.
- Thresholds and justification: 3 of 20 is the existing nemesis threshold in `Advisor.cs` (times >= 3). Repetition is the strongest evidence that the death is a pattern rather than bad luck.
- Exclusions: count < 3: skip. Attacker name empty: skip.
- Wording: Title: "{Killer} has killed you {n} of the last {m} times". Body: "That is a pattern, not bad luck. Watch for its wind-up, keep a movement skill ready when it appears, and check whether the same damage type keeps coming with it."
- Confidence label: "High confidence: the mod's own death log is the evidence."
- Missing data: history unavailable (fresh log): skip.
- Counterexample: two deaths to "Rogue Archer" in 20: the rule must not fire.

### R-20 repeat-ability (rank 20)

- Required evidence: history of `KillerAbility` (or `Ability`) values with one ability ≥ 3 of the last 20 deaths.
- Conditions: same ability name repeats, regardless of attacker.
- Thresholds and justification: same as R-19, but naming the ability points at the telegraph rather than the monster.
- Exclusions: ability names missing or all distinct: skip.
- Wording: Title: "{Ability} keeps killing you". Body: "{n} of your last {m} deaths credit {Ability}. That specific attack is the thing to learn: its wind-up, its range, and when it is safe to stand still."
- Confidence label: "High confidence: repeated ability names come straight from the death log."
- Missing data: ability never recorded: skip.
- Counterexample: each of the last 5 deaths credits a different ability: the rule must not fire.

### R-21 repeat-element (rank 21)

- Required evidence: `PatternReport.ElementShares` with a top element ≥ 50%, and `Res.<Element>` known for at least one of those deaths.
- Conditions: one element accounts for half or more of recent death damage (each death counted once, per the existing `DeathPatterns` review fix).
- Thresholds and justification: 50% is a deliberate "most of your deaths" boundary. The per-death resistance rule (R-02) already fired on those deaths; this rule aggregates it into a build-level priority.
- Exclusions: top share < 50%: skip. Resistance never recorded: downgrade to "most of your recent deaths were {element}" without the gap claim.
- Wording: Title: "{element} is your recurring killer". Body: "{share}% of the damage across your last {m} deaths was {element}, and your {element} resistance read {Res}%. Capping it is the single change that would have helped the most deaths."
- Confidence label: "Medium confidence: shares are computed over the recorded windows; deaths without timelines do not contribute."
- Missing data: fewer than 3 deaths with timelines in the window: skip (too thin to call a pattern).
- Counterexample: shares split 30/30/40 across three elements: no element reaches half, rule must not fire.

### R-22 boss-fight-context (rank 22)

- Required evidence: `IsBossfight` true. Optional: `Killer`, `Ability`, repeat counts. PLANNED: the flag sits at args[9] of the 10-arg network report and is not read into `DeathDetails` yet, so the rule cannot fire until that one-line addition lands.
- Conditions: the game flagged the death as a boss fight.
- Thresholds and justification: none. The rule reframes, it does not diagnose: boss kills come from telegraphed abilities, and the mod must not invent encounter mechanics (C2). If the same boss or ability repeats (R-19/R-20), those rules carry the specifics.
- Exclusions: `IsBossfight` false or absent: skip. Any boss-specific claim ("dodge the beam") without a recorded ability name: forbidden.
- Wording: Title: "Boss fight: the telegraph is the mechanic". Body: "The game flagged this as a boss fight{ against {Killer}}. Boss kills almost always come from telegraphed abilities{ like {Ability}}: keep a movement skill for the wind-up and do not greed the cast."
- Confidence label: "Medium confidence: the boss flag was recorded; which ability is dangerous is only known if the death report named it."
- Missing data: `Killer`/`Ability` missing: drop the clauses.
- Counterexample: `IsBossfight` false on a regular echo death: the rule must not fire.

### R-23 area-penetration-context (rank 23)

- Required evidence (standalone): a recorded area level ≥ 75 (PLANNED field; the mod does not capture it yet) or a `Zone` that unambiguously names an empowered timeline. Until such a field exists, this rule does not fire standalone; its sentence rides inside R-02's wording.
- Conditions (standalone): area level known and ≥ 75.
- Thresholds and justification: enemies gain 1% penetration per area level up to 75% at area level 75+, applied after the cap (F2). At that point a capped resistance is the balanced baseline and anything under cap is increased damage taken.
- Exclusions: area level unknown: never claim the current area's penetration (C2). Do not use character `Level` as a proxy for area level.
- Wording (standalone): Title: "Enemies here penetrate your resistances". Body: "This area is level {areaLevel}, so enemies penetrate {pen}% of your resistances after the cap. A capped 75% resistance is exactly the baseline here; anything under cap is increased damage taken, and overcap does not help against penetration, only against shred."
- Confidence label: "Medium confidence: the penetration rule is official, but the mod cannot see which echo modifiers are active."
- Missing data: no area level field: attach the short penetration sentence to R-02 instead.
- Counterexample: a level 40 campaign zone: the rule must not fire; penetration at area level 40 is 40%, and the wording would overstate the case.

### R-24 unknown-no-data (rank 24, the honesty rule)

- Required evidence: none. Fires when no other rule produced a tip, or when `Kind` == Unknown and no death report fields exist.
- Conditions: nothing actionable.
- Thresholds and justification: C5. The first real in-game death had zero captured hits; this rule is what the player sees in that case.
- Exclusions: any other rule fired: skip.
- Wording: Title: "Not enough data to give specific advice". Body: "The mod saw the death but recorded nothing about what caused it: no killing blow, no ailments, no defence snapshot. There is nothing specific to fix from this death alone. To change that, set ProbeApi = true and DebugLog = true in UserData/medick_DeathCounter.cfg, play until you die again, and send MelonLoader/Latest.log so the hooks can be repaired."
- Confidence label: none (this is the absence of confidence).
- Missing data: n/a.
- Counterexample: a death with a recorded crit killing blow: R-01 fired, so this rule must not.

## 6. Picking the top 3

1. Evaluate every rule. A rule fires only when all its required evidence is present and no exclusion matches. Missing optional evidence only downgrades wording.
2. Score each fired rule: `score = (100 - rank) + 2 x (number of required evidence fields beyond the first) + 3 if the wording quotes at least one recorded value`. Rank dominates, specificity breaks ties, recorded numbers beat generalities.
3. Dedupe by group so the panel never shows two tips that say the same thing: group `res:<element>` = {R-02, R-03, R-10 for that element} (only one can fire by construction); group `crit` = {R-01, R-11}; group `dot` = {R-04, R-06 when the irregular source is a DoT}; group `pool` = {R-05, R-07, R-15}; group `pattern` = {R-19, R-20, R-21} (keep at most one pattern tip on a single-death view; the rest belong to the Patterns tab).
4. Sort by score descending, then rank ascending. Take the top 3.
5. Never pad. If two rules fire, show two. If none fire, show R-24's fallback. Generic advice is never used as filler (that is the project's founding constraint).
6. The data-confidence line (section 7) is always appended and does not count toward the 3.

## 7. The data-confidence line

One line under the advice, built from what was actually recorded:

`Data: death report {yes|no} · crit flag {yes|no} · defences {yes|partial|no} · hit timeline {yes ({Hits} hits)|no} · ailments {yes|no} · history {n} deaths. Confidence: {High|Medium|Low} ({reason}).`

- High: death report and defences and timeline all present, or the claim rests entirely on recorded values (for example R-19 on the log).
- Medium: death report and defences but no timeline; or timeline and defences without a death report; or the threshold is community guidance (R-15).
- Low: only one tier present, or a recorded conflict exists (crit on a DoT, ward remaining after a lethal hit, crit at capped avoidance without Critical Vulnerability). Name the conflict in the reason.
- The reason is one short clause: "no hit timeline, so burst versus one-shot cannot be told apart" or "defence snapshot missing, resistances unknown".

## 8. Fallback when nothing is actionable

R-24's wording is the whole fallback. Principles: say what was not recorded, say there is nothing specific to fix, say exactly how to turn on better data (ProbeApi and DebugLog in `UserData/medick_DeathCounter.cfg`, then share `MelonLoader/Latest.log`), and never fill the gap with a generic checklist. If `Detection` == "game" (only the game's own death counter moved), add: "The mod noticed the death only through the game's own counter, which means every hook missed: the log from a ProbeApi run is the only way to fix that."

## 9. Open questions for the next lanes

1. Done: `DeathDetails` field names reconciled against `cursor/advice-research-base` (section 1.5). Remaining: capture args[8] (`IrregularDamageSourceID`) and args[9] (`IsBossfight`) into `DeathDetails` (one line each in `Capture()`), confirm how "none" is encoded for each, and identify the 9-arg local report's last argument.
2. Units of `Damage` and `Overkill` (U3): one probe run comparing them to health deltas settles it; until then R-05 stays downgraded.
3. Whether the precalculated stats holder exposes block effectiveness, glancing blow, ward retention, and "less damage over time taken": each would sharpen an existing rule (R-08, R-04) without new logic.
4. An area-level source for R-23 and for interpreting armor and dodge (U5): scene name to area level mapping is unreliable; a game field is preferred.
5. Armor shred versus players, flat 100 or reduced (U1): needs an in-game check with a shredding enemy.
6. Whether `AilmentsOnYou` should add Critical Vulnerability, Marked for Death, Acid Skin, Stagger and the other amplifier names to `Ailments.All` in `Elements.cs`: the current table does not know them, and R-11/R-14 depend on recognizing them.
