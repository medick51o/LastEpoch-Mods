# Lane 6 test cases

Cases the boss catalog, death link, and assessment path need. "Implemented" means a Core test on this branch exercises it with baseline profiles or synthetic ids. Synthetic ids are `synth-*` and are not roster claims. Real timeline, Harbinger, dungeon, and pinnacle names stay out of code until the roster lane lands. UI cases are specified for a later `DeathPanel` port. Native timing beyond the existing Core routing tests stays on the live queue.

Existing baseline tests are left in place: `Test_Boss_ExactNameRequiresExplicitGameFlag`, `Test_Boss_AddsProxiesAndVariantsDoNotMatchBySubstring`, `Test_Boss_OfflineAttackerGuideDoesNotBecomeBossClassification`, `Test_Boss_ManualCatalogCannotMutateSavedDeath`, `Test_Boss_CatalogTypesNeverFillMissingDeathTypes`, `Test_Boss_NetworkFlagSurvivesSameBlowLocalReport`, `Test_Boss_FlagDoesNotBleedAcrossDifferentBlowsOrOverrideExplicitFalse`, `Test_Zone_RawSceneAndLevelFreezeBeforeRespawnAndRoundTrip`, `Test_Zone_OldRecordsRemainReadableAndUnknownFlagsStayUnknown`.

## Mapping patterns

| Id | Case | Expect | Status |
|---|---|---|---|
| M1 | Standalone encounter, parent status absent, encounter id null | Valid. Not treated as unknown. | Implemented, `Test_Catalog_ParentCapDoesNotClearUnknownFollowUp` via `CompleteFire` |
| M2 | Parent status unknown, encounter id null | Baseline default for all five starters. | Implemented, `Test_Catalog_BaselineProjectsFiveProfilesWithExplicitUnknowns` |
| M3 | Follow-up with parent id set to another encounter | Valid only when that id exists. Distinct encounter id. | Implemented, synthetic parent and `synth-harbinger` |
| M4 | Follow-up does not inherit the parent's damage list | `ParentElementsAppliedToFollowUp` is empty. Child coverage stays whatever it already was. | Implemented |
| M5 | Alternate variant is a variant row, relationship `alternate`, not an alias | Killer "Synthetic Alternate" matches nothing. | Implemented, `Test_Catalog_AddsProxiesVariantsAndAbilityNamesDoNotMatch` |
| M6 | Empowered variant, relationship `empowered` | Same as M5. "Synthetic Empowered" is not an alias. | Implemented |
| M7 | Normal and empowered forms are not one profile with copied tips | Separate ids. No tip copy in the model. | Implemented as the synthetic pair. Real pairs wait on the roster. |
| M8 | Dungeon tier variant, relationship `tier` | Schema accepts the relationship. No tier ids invented for Julra. | Schema only. Julra variants stay unknown in the projection. |
| M9 | Reskin, relationship `reskin` | Separate id, not a substring match. | Schema only. No reskin was added. |
| M10 | Inherited move, relationship `inherited-move`, ability `inheritedFromEncounterId` must exist | Missing source fails validation. | Validator rule. No baseline ability rows, so no inherited move was projected. |
| M11 | Transformed variant, relationship `transformed` | Separate id. | Schema only. |
| M12 | Summoned add is its own encounter whose parent points at the boss | "Synthetic Add" matches the add. "Synthetic Parent Add" matches nothing. | Implemented |
| M13 | Proxy killer is not the parent alias | "Synthetic Parent Proxy" matches nothing. | Implemented |
| M14 | Pinnacle encounter is not the timeline Harbinger | Needs two real ids. | Waiting on the roster. Do not point `harbinger-hatred` at a guessed parent. |
| M15 | Same display name for campaign and monolith would be two ids | Baseline keeps one `lagon` id and unknown variants. | Implemented as "do not split". Real second id waits on the roster. |
| M16 | Name resemblance does not merge encounters | Harbinger of Hatred does not match Rahyeh, Void Rahyeh, Harbinger, or Hatred. | Implemented, `Test_Catalog_ExactAliasesMatchAndPartialsDoNot` |
| M17 | Display name is not an alias unless it is also listed | "Shown Name" with alias "Other Alias" matches only the alias. | Implemented |
| M18 | Self-parent and parent cycles | Validation error. | Implemented for self-parent. Cycle uses the same walk. |
| M19 | Known parent id that is not in the catalog | Validation error. | Implemented |
| M20 | Browse group is data, not a guess from the name | `harbinger-hatred` browse groups stay unknown. | Implemented |

## Exact aliases

| Id | Alias | Match | Status |
|---|---|---|---|
| A1 | `Lagon` | `lagon` | Implemented, also `Test_Boss_ExactNameRequiresExplicitGameFlag` |
| A2 | `  lagon  ` | `lagon` | Implemented |
| A3 | `Emperor of Corpses` | `emperor-corpses` | Implemented |
| A4 | `Heorot` | `heorot` | Implemented |
| A5 | `Chronomancer Julra` and `CHRONOMANCER JULRA` | `julra` | Implemented |
| A6 | `Julra` | `julra` | Implemented |
| A7 | `Harbinger of Hatred` and `harbinger of hatred` | `harbinger-hatred` | Implemented |
| A8 | `Chronomancer`, `Emperor`, `Harbinger`, `Hatred` | no match | Implemented |
| A9 | Empty, whitespace, null killer | no match | Implemented |
| A10 | Duplicate alias across encounters, case-insensitive | validation error | Implemented |
| A11 | Locale tag unknown is not a wildcard | Baseline aliases store locale status unknown and tag null. Matching uses the exact text only. | Implemented in the projection assertions |
| A12 | A second locale of the same text | Would be a second alias row when lane 4 has a tag. Not in the baseline. | Waiting on locale evidence |

## Adds, proxies, and non-names

| Id | Killer or field | Expect | Status |
|---|---|---|---|
| P1 | `Lagon's Tentacle`, `Lagon Clone`, `Emperor's Remains` | no match with the boss flag true | Existing `Test_Boss_AddsProxiesAndVariantsDoNotMatchBySubstring`, repeated on the document matcher |
| P2 | `Harbinger of Hatred's Void Rahyeh`, `Void Rahyeh`, `Julra's Echo` | no match | Implemented |
| P3 | Killer ability `Lagon Lightning Blast` or `Lightning Blast` | not an encounter id | Existing test plus `Test_Catalog_AddsProxiesVariantsAndAbilityNamesDoNotMatch` |
| P4 | Zone `Temporal Sanctum` | not Julra | Implemented |
| P5 | Raw scene `Boss_Arena_55` | not an encounter. Label keeps the suffix. | Implemented, and `Test_Zone_RawSceneAndLevelFreezeBeforeRespawnAndRoundTrip` |
| P6 | Irregular source `Heorot` with a different killer | not Heorot | Implemented |
| P7 | Ability titles Void Rahyeh, Ice Spike, Lightning Blast, Moon Blast, Soul Bomb | `AbilityNameEstablishesDamage` is false | Implemented |

## Flags

| Id | Flag | Alias | Expect | Status |
|---|---|---|---|---|
| F1 | true | each of A1 through A7 | kind `identified`, caption `Boss tips: [name]` | Implemented |
| F2 | null | each of those aliases | kind `unconfirmed-guide`, `Match` is null, caption `Attacker guide: [name]`, flag stays null | Implemented, plus `Test_Boss_OfflineAttackerGuideDoesNotBecomeBossClassification` |
| F3 | false | each of those aliases | kind `not-boss`, no caption, no attacker guide | Implemented |
| F4 | true | unmatched killer | kind `confirmed-unidentified`, caption `Browse boss tips` | Implemented |
| F5 | null | unmatched killer | kind `unknown`, no caption, label `Boss encounter unknown` | Existing zone test plus old-record test |
| F6 | Network true on the same blow, local report omits the flag | flag preserved, then identified if the alias matches | Existing `Test_Boss_NetworkFlagSurvivesSameBlowLocalReport` |
| F7 | Different killer or amount, or explicit false | flag does not carry and false stays false | Existing `Test_Boss_FlagDoesNotBleedAcrossDifferentBlowsOrOverrideExplicitFalse` |

## Zone, late reports, old records

| Id | Case | Expect | Status |
|---|---|---|---|
| Z1 | Missing localized zone, raw scene present | Location label is `Scene: [id]`, not a catalog area name. Scene does not select an encounter. | Implemented |
| Z2 | Both zone and scene missing | `Zone unavailable` | Implemented |
| Z3 | Positive zone level frozen before respawn, including numeric scene suffix | Existing zone test | Implemented there |
| Z4 | Negative zone level | Stored as null by the analyzer | Existing `Test_Zone_OldRecordsRemainReadableAndUnknownFlagsStayUnknown` |
| Z5 | Old JSON with only `Zone` and `Killer: Julra` | Flag null, scene null, unconfirmed guide, label `Boss encounter unknown`. Browsing `lagon` does not change killer or flag. | Implemented |
| Z6 | Late report sets Julra, Lightning, damage 50, boss true | Those report fields stick. Zone and scene stay empty. Catalog coverage is not copied. Damage-by-element sum stays 0. | Implemented, `Test_Catalog_LateReportDoesNotBackfillZoneOrCatalogDamage` |
| Z7 | Late report inside the 5 second window on the pending death | Existing pending and report routing tests | Already in `PendingDeathTests` |
| Z8 | Overlapping second death, other character, or report after the window | Reject, do not attach | Existing routing tests |
| Z9 | Counter-only death after respawn | Does not substitute town zone | Existing `Test_Pending_LateCounterNeverUsesTownAsDeathSnapshot` |
| Z10 | Party death, stale native pointer, actor unload | Not modeled in Core | Native queue. Lane 4. |

## Manual browsing

| Id | Case | Expect | Status |
|---|---|---|---|
| B1 | Death is identified Lagon, browse selection is `heorot` in Harbinger mode | Death JSON unchanged. Classification stays `lagon`. Caption stays `Boss tips: Lagon`. | Implemented |
| B2 | Mode outside timeline, boss, harbinger, dungeon, or blank id | Browse returns false. Death unchanged. | Implemented |
| B3 | Browse with no death loaded | Selection can still be returned. Nothing is written. | Covered by the method contract. UI empty state waits on the port. |
| B4 | Manual browse must not set `IsBossFight`, killer, elements, zone, or death id | Implemented for the serialized record | Implemented |
| B5 | Open guide that is not the linked encounter shows the unlinked sentence | UI copy in `lane6-ui-design.md`. Not drawn in this lane. | Waiting on the UI port |
| B6 | Mode lists stay empty while `browseGroups` is unknown | Projection assertion. The empty-state sentence is design, not a widget test. | Data implemented. UI waiting. |

## Damage typing

| Id | Case | Expect | Status |
|---|---|---|---|
| D1 | Julra identified, no captured element | Observed types empty. Advice elements empty. Killing element stays null. Coverage row still Void, Cold, Lightning. | Implemented, plus existing `Test_Boss_CatalogTypesNeverFillMissingDeathTypes` |
| D2 | Mixed Fire and Physical on a Julra death | Advice elements are Fire then Physical only | Implemented |
| D3 | Secondary element repeats the primary | One observed type | Existing catalog test |
| D4 | Killing element `not-an-element` | Zero advice elements. The raw string stays on the death. | Implemented |
| D5 | Unknown coverage must not be given an element list | Validation error if elements are set while status is unknown | Implemented |
| D6 | Known coverage needs a positive evidence class | Tactical inference on a component fails validation | Implemented |
| D7 | Empty `DamageTypes` on the four untyped starters | Status unknown, elements null, scope null. Not status absent. | Implemented |
| D8 | Julra per-ability list | Status unknown, items null. Scope stays `encounter`. | Implemented |
| D9 | `TryApplyCoverageToDeath` | Always false. Serialized death unchanged, including blow amount and damage array. | Implemented |

## Assessment regressions

| Id | Case | Expect | Status |
|---|---|---|---|
| R1 | Catalog cannot manufacture death damage | D9. A Julra lookup does not fill blow, element, or damage array. | Implemented, `Test_Catalog_CoverageCannotManufactureDeathDamage` |
| R2 | Parent's capped element does not clear an unknown follow-up | Recorded synthetic encounter can be fully prepared alone when Fire is capped and its rows are known. With `synth-harbinger` coverage unknown, `FullyPrepared` is false and the open list contains `follow-up: unknown additional threats`. | Implemented |
| R3 | Capping the follow-up's element on the parent death still does not close it | Known follow-up Void stays open even if the capped set includes Void. Parent elements are not copied onto the child. | Implemented |
| R4 | Grade A on the recorded fire gap is not clearance for the follow-up | `CurrentAssessment` grade A on a fire death while preparation for the pair is not fully prepared. Death JSON unchanged. | Implemented, `Test_Catalog_GradeAOnParentElementDoesNotPrepareFollowUp` |
| R5 | Catalog edits do not rewrite a saved reassessment | Save an update, rewrite the Julra tip and add Fire to coverage, browse another id, reload. Saved JSON, killer, flag, element, and action bodies stay frozen. | Implemented, `Test_Catalog_EditsDoNotRewriteSavedReassessments` |
| R6 | Source lookup does not create three generic resistance cards | Julra lookup leaves `Advisor.Show` keys unchanged and does not emit `res_Void`, `res_Cold`, or `res_Lightning`. Stuffing those three shares into a death would fill the three-card limit. The lookup path does not do that. | Implemented, `Test_Catalog_SourceLookupDoesNotCreateThreeResistanceCards` |
| R7 | Three-card dedupe still holds for ordinary advice | Existing `Test_Advice_ShowSkipsCappedAndStopsAtThree` and `Test_Rules_CardsHaveUniqueGroupsAndStableOrder` | Already implemented |
| R8 | A capped killing blow does not grade the whole fight as safe when other captured types or debuffs remain | Existing `Test_Grade_MixedTypesAndRecurringDebuffsPreventFalseClearance` and `Test_Grade_ARequiresImprovedMeasuredChecksWithoutMajorKnownRisk` | Already implemented |

## Schema and projection

| Id | Case | Expect | Status |
|---|---|---|---|
| S1 | Version `1.0.0`, reviewed line copied from `BossCatalog.Reviewed` | Implemented | |
| S2 | Encounter order `lagon`, `emperor-corpses`, `heorot`, `julra`, `harbinger-hatred` | Implemented | |
| S3 | Tips copied exactly and classed `tactical-inference`, confidence unknown, level null | Implemented. Deleting weak tips is an audit recommendation, not a code edit. | |
| S4 | Alias locale status unknown, tag null | Implemented | |
| S5 | JSON null is distinct from status unknown | Parent encounter id, unknown elements, ability items, browse items, locale tag, and a prose claim's source are JSON null. Julra coverage status is the string known. | Implemented, `Test_Catalog_BaselineJsonKeepsNullDistinctFromUnknown` |
| S6 | Lagon forum URL classed developer documentation. Julra Tunklab URL classed datamined reference. LE Tools reprint classed unknown. | Implemented | |
| S7 | Mechanics prose is not parsed into ability rows | Lagon abilities stay unknown. | Implemented |
| S8 | Unsupported JSON property | Validation error | Implemented |
| S9 | Wrong schema version, placeholder display name, double dash in a tip | Validation errors | Implemented |
| S10 | Baseline document validates, including a JSON round trip | Implemented | |
| S11 | Player-facing catalog strings reject `--`, `TODO`, `TBD`, and `placeholder` | Implemented | |

## UI cases for the later port

These are not widget tests. `lane6-ui-design.md` is the spec.

| Id | Case | Expect |
|---|---|---|
| U1 | Timeline, Boss, Harbinger, and Dungeon with all groups unknown | Empty state naming that mode. Not the five-button list. |
| U2 | A known Harbinger group | Only those rows. Harbinger of Hatred appears only after its group is set. |
| U3 | Open card | Name, status line, chips or Type unknown, at most three tips. Details and Sources collapsed. |
| U4 | Unknown abilities, ailments, variants, locations, per-ability damage | One explicit unknown line each. No filler paragraph. |
| U5 | Compact death card | Quote, place, encounter sentence, one link, three titles. Timeline and defenses behind More. |
| U6 | Identified, unconfirmed, confirmed-unidentified, false, and unknown | Captions in the flag table. False and unknown draw no boss button. |
| U7 | Manual row change | Death file bytes unchanged, including after restart. |
| U8 | Long localized name | Wraps on the card. Row may ellipsize. Link button wraps inside the card width. |
| U9 | Damage color | Chip or line includes the element word. Void stays on the dark surface. |
| U10 | Controller | Focus ring, order from Close through the open card, shoulder scroll, visible scroll thumb or equivalent. |
| U11 | Scale 1 and 1.4, short viewport | One body scroll region. Mode chips remain reachable. |

## Not in this lane's code

Real boss-to-Harbinger pairs, empowered and dungeon tier names, locale tags, localized zone lookup, and native party or pointer cases. Add those tests when the roster and probe lanes have evidence. Do not fill them with guessed names in `BossCatalog`.
