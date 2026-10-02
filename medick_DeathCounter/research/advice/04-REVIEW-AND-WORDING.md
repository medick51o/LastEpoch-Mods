# Lane 4: advice-engine review and player wording

Reviewed against `cursor/advice-research-base` at `4b5341e` (Codex snapshot, 2026-10-02). This branch is `cursor/advice-research-review`. Nothing here was checked inside Last Epoch. Do not merge, do not install, do not publish.

Older baseline PR #4 / `6fabc17` was not copied over this snapshot. `DefenseSnapshot.cs`, `DeathAnalyzer.cs`, and `Elements.cs` were unchanged from that baseline until the Core edits in this branch. `Advisor.cs` had already grown a physical-hit branch. `DeathDetails.cs`, `DeathText.cs`, `DeathReportHooks.cs`, and the online name fallbacks in `PlayerProbe.cs` are new in the Codex snapshot.

Core edits in this branch are limited to `Advisor.cs`, `DefenseSnapshot.cs`, `DeathText.cs`, `Elements.cs`, and `tests/CoreTests/Program.cs`. Hook and UI files are reviewed and left as Codex wrote them. Each finding says whether the patch is already in this branch or still only proposed.

## How to read a number

These are the rules the advice is graded against. A sentence in the mod that disagrees with one of them is a bug.

**Resistances.** Seven types: Physical, Fire, Cold, Lightning, Necrotic, Void, Poison. The cap is a hard 75% and cannot be raised. Overcap only soaks shred, Shock, and Marked for Death, and that soak happens before the cap. Penetration applies after the cap. One point of negative resistance adds 1% damage. Source: [Resistances](https://support.lastepoch.com/hc/en-us/articles/46361885661851-Resistances) and the [penetration FAQ](https://support.lastepoch.com/hc/en-us/articles/46363284011419-If-75-resistance-is-the-max-and-the-enemies-get-penetration-per-level-up-to-75-do-I-need-150-resistance). A claim that area level adds 1% penetration per level up to 75% is widely repeated and is not on those pages. Do not state it.

**Armor.** Hits only. Damage over time, including bleed, is not reduced. Cap 85% against physical. Against other hits the armor value is treated as 70% as effective, so the practical cap is 59.5%. The official page shows the formula as an image, so this review does not invent a mitigation percent. A community transcription of that image (tunklab calculator, LastEpochBuilding `CalcDefence.lua` commit `1b7da72`) is `adjusted = areaLevel + 5`, then two terms that sum and clamp at 85. Without area level the mod must quote the armor rating and stop. Armor shred on the current support page is +100 negative armor per stack, not 2% per stack. Source: [Armor](https://support.lastepoch.com/hc/en-us/articles/46361854859419-Armor) and [Negative Ailments](https://support.lastepoch.com/hc/en-us/articles/46361887879963-Negative-Ailments) (updated 2026-02-14).

**Endurance.** Base 20% less damage, threshold 20% of max health. The reduction caps at 60%. The threshold itself is not capped. It applies to the portion of any damage, hit or damage over time, that crosses below the threshold. It does not apply to ward. Source: [Endurance](https://support.lastepoch.com/hc/en-us/articles/46361855013787-Endurance). "The threshold affix only rolls on belts" is a 1.2 note and is not restated as a Season 5 fact.

**Ward.** Ward is lost before health. Endurance does not apply to ward. The known exceptions are percent-current-health effects (arena traps, Emperor of Corpses blood pools), and those cannot kill. Ward decays. Source: [Ward](https://support.lastepoch.com/hc/en-us/articles/46361876030235-Ward) and the ward FAQ `46363263694235`. Intelligence granting 4% ward retention per point is a Maxroll write-up, not the support page.

**Critical strikes.** Damage over time cannot crit. Enemy crits deal 200% damage. Crit avoidance is rolled after the attacker's crit chance. At 100% avoidance, enemy hits do not crit. "Less bonus damage taken from critical strikes" caps at 100% and turns a crit into a normal hit. Source: [Critical Strikes](https://support.lastepoch.com/hc/en-us/articles/46361891709211-Critical-Strikes). The 2023 `ProtectionClass` dump has no field for that less-crit-damage stat. Do not invent a member name.

**Stun avoidance is a rating.** It is not a resistance and not a percent. With no increased stun chance and no avoidance, a non-player hit must deal more than 5% of max health before it can stun. Every 100 stun avoidance raises that threshold by 1%. Players have 250 plus 5 per level built in. Source: [Stun](https://support.lastepoch.com/hc/en-us/articles/46361891772443-Stun). Clamping it at 100, or letting a value of 750 decide the percent-versus-fraction scale, is wrong.

**Dodge and block.** Both are hits only. Dodge rating converts to a chance that falls as area level rises, and the chance caps at 85%. A dodge avoids the hit and the ailments on it. Damage over time cannot be dodged. Block chance is an additive percent. Block effectiveness is a rating scaled by area level, and its mitigation caps at 85%. Glancing blow still exists (35% less damage, chance cap 100%, hits only) and has been removed from most items. Sources: [Dodge](https://support.lastepoch.com/hc/en-us/articles/46361899172379-Dodge), [Block](https://support.lastepoch.com/hc/en-us/articles/46361876113947-Block). The dodge and block formulas on those pages are images. The same tunklab file transcribes them. Do not print a chance without area level.

**Freeze and chill are not cold resistance.** Freeze needs freeze rate. The chance falls as the target's max health and current ward rise. Frostbite and chill raise it. Base duration is 1.2 seconds. Bosses are evaluated as if they had 1.5 times max health. Chill is a slow: up to 3 stacks, 12% less attack, cast, and movement speed, and 50% less effect against players. Cold resistance does not stop either. There is no Freeze ailment id. Freeze is a hit flag. Source: [Freeze](https://support.lastepoch.com/hc/en-us/articles/46361855307035-Freeze) and the negative ailments page.

**Other ailments, from that same page.** Frostbite is cold damage over time and +20% increased chance to be frozen per stack for the first 15 stacks. Shock is +5% negative lightning resistance (60% less against players, about 2% per stack, max 10 stacks) plus stun chance. It is not a general damage-taken debuff. Shock of Aberroth is the damage-taken variant. Poison is poison damage over time plus the same style of poison-resistance shred, and only the first 30 stacks shred. Bleed is physical damage over time. Armor does not reduce it. Ignite is fire damage over time. Damned is necrotic damage over time and less health regen. Time Rot is void damage over time and longer stuns. Doom is void damage over time and more melee damage taken. Electrify is lightning damage over time. Resistance shred ailments are +5% negative resistance, 60% less against players, max 10 stacks.

**What the death screen number is.** Patch 1.1.3 added killing-blow damage, overkill, and whether that blow crit. Overkill is damage past what was needed to reach 0 health. Source: [patch 1.1.3 notes](https://forum.lastepoch.com/t/last-epoch-patch-1-1-3-notes/73083). A 2024 report (r/LastEpoch, "Killing damage") of 1000 health and 1500 ward showing 500 damage and 150 overkill, plus Llama8 on the forum, agrees on the reading: the number is the single hit or tick that took health through zero. It is not the whole fight, and it is not the ward that was already gone. Health removed is about `damage - overkill`. Comparing that blow to max health is not "percent of the life you had left", and it is not a one-shot if you were already low.

**Do not split one reported number across two types.** A two-type skill splits its base damage, then each type is mitigated on its own. The post-mitigation split is not 50/50. If the report names two types, name both and keep the one number.

**DamageType order in the 2023 Il2Cpp dump** (`voideh/EpochTool` commit `66f1e21`): Physical 0, Fire 1, Cold 2, Lightning 3, Necrotic 4, Void 5, Poison 6. That matches `Elements`. It is not proof for Season 5. If the enum cannot be loaded, labeling by that order is a guess.

**Percent versus fraction is still unknown.** `DefenseSnapshot` refuses to pick 75 versus 0.75 unless the values themselves agree. That rule is right. Keep it.

## Recheck of the earlier review, on this snapshot

| Earlier item | On `4b5341e` before this branch | After the Core edits |
| --- | --- | --- |
| Pre-mitigation split called the damage that killed you | `HitEvent.AddTo` (lines 34 to 48) still rescales a pre-mitigation `ByElement` mix onto health lost. Advisor said "of the damage that killed you". There is no literal `INCOMING` label. | Advisor now says the figure is recorded health lost on the pre-mitigation mix. The panel still prints the raw percent (`DeathPanel.cs` line 146). |
| Unknown max health called a one-shot or 100% of life | `DeathAnalyzer.Classify` lines 133 to 134 still returns `OneShot` for a single hit when max health is missing. `Test_UnknownMaxHp_StillClassifies` locks that. Advisor one-shot text said "you out". Burst text used `1f` and printed "100% of your life". | Advisor text no longer says either. The analyzer classification is unchanged, because that file was left alone and the test requires `OneShot`. |
| `stunAvoidance` in percent keys | `DefenseSnapshot` line 20 included it. A rating near 750 plus a resistance of 0.75 made `Scale` return null and dropped every resistance. | Removed from `PercentKeys`. Stored raw. |
| Substring ailment match | `Ailments.Find` used `Contains` after an exact-id pass. `Shockwave`, `FreezeRate`, and `SlowRetaliation` matched. | Token match. Those three return null. Existing ids (`Ailment_Bleed`, `FireResistanceShred`, `PoisonResShred`, `StackingAbyssalDecay`, `Frozen`, `ShrineStun`) still resolve as the tests require. |
| Freeze tip says to cap cold resistance | Per-death text appended that sentence whenever cold was under the cap. `PatternBody` for `cc_freeze` always said it. | Both removed. Cold damage still gets its own `res_Cold` tip, so a cold hit while frozen still recommends cold resistance for the hit. |
| Physical resistance and endurance ignored | A physical hit added armor only. Endurance text never read `Endurance` or `EnduranceThreshold`, and it said the threshold affix rolls on belts. | Physical hits and physical damage over time get `res_Physical`. Armor is still withheld for physical damage over time. Endurance quotes the recorded percent and threshold. The belt sentence is gone. |
| Capped resistance treated as the gap to fix | `ResTip` already lowered that tip to weight 30 and titled it "already capped". It was not the top tip. It still occupied a slot in `Suggest`. | `Suggest` is unchanged, so the capped fact remains available. New `Show` drops it and keeps three actions. `DeathPanel` still calls `Suggest` (line 153). |
| Missing `DamageType` falls back to the identity map | `ArgReader.ElementMap.Build` lines 200 to 203. The 2023 order matches. Season 5 is unverified. | Not edited. Proposed patch below. |

Advice rules are not inside the hooks. `DeathReportHooks` only fills a `DeathDetails`. `DeathPanel` is the only caller of `Advisor.Suggest`. Theme only colors. No advice text was moved out of the hooks because none was there.

## Bug list

### 1. High. Stun avoidance was classified as a percent. Fixed.

`DefenseSnapshot.PercentKeys`, previously line 20. `PlayerProbe.Defenses` still reads `stunAvoidance` into the raw dictionary (`PlayerProbe.cs` line 282). That read is fine. The scale logic was not.

Why it is wrong. The stun page defines stun avoidance as a rating. A level 80 player with no extra avoidance already has `250 + 5*80 = 650`. A build with some extra rating sits near 750. `Scale` treats any percent-key above 3 as "these values are already percents", and any non-whole value at or under 1.5 as "these values are fractions". Both signals at once return null, and then every resistance is discarded. A normal online or offline character with fractional resistances and a real stun rating would lose the entire defensive snapshot.

Patch, now in `DefenseSnapshot.cs` lines 20 to 25: `PercentKeys` is `Block`, `Endurance`, `CritAvoidance` only. Non-percent keys, including `StunAvoidance`, are copied through unchanged. `Test_Defenses_StunAvoidanceIsRatingNotResistance` feeds fire 0.75, crit avoidance 0.4, stun avoidance 750 and expects fire 75, crit avoidance 40, stun 750.

Do not add `stunAvoidance` back, and do not clamp it at 100.

### 2. High. Unknown max health was worded as a full life. Advice fixed. Classification still overclaims.

`DeathAnalyzer.Classify`, lines 133 to 134: one hit and no max health returns `OneShot`. `Advisor` one-shot body (previously the `"you out"` branch) and burst body (previously `Pct(... : 1f)`).

Why it is wrong. With no max health, `KillingBlow / MaxHealth` is undefined. Substituting 1 prints "100% of your life". "You out" still tells the player the hit was measured as lethal relative to their pool. The death-screen reading above also means a large hit on a character who was already at 10% health is not a one-shot. The analyzer cannot know that from one number.

What changed. `Advisor` lines 104 to 121. Missing max health now says the hit was recorded and max health was not, so it is not a share of life and not a confirmed one-shot. The burst sentence says the same. When max health is known, the one-shot sentence uses "of your max health", and a game report subtracts overkill first (`HealthRemoved`, lines 303 to 310), because overkill never removed health.

What did not change, on purpose. `DeathAnalyzer` still returns `OneShot` for that shape. `Test_UnknownMaxHp_StillClassifies` requires it, and this lane did not edit the analyzer. The player-facing card must not repeat the word as if it were measured. `Quote` does not say one-shot at all.

Proposed analyzer patch, for a later lane:

```csharp
if (window.Count == 1) return DeathKind.Unknown; // shape only; do not call it a one-shot
```

Update `Test_UnknownMaxHp_StillClassifies` in the same change. Until then, `Confidence` stays Low or Medium and the body refuses the percent.

### 3. High. The element split was described as the killing damage. Wording fixed. The numbers are still pre-mitigation.

`HitEvent.AddTo`, lines 34 to 48. The comment already says the split is usually pre-mitigation and is rescaled onto `Amount`. `Amount` itself is health lost (`GameHooks.HitPostfix`, lines 287 to 293). Advisor then divided `DamageByElement` by the total and spoke as if that were the mitigated killing blow.

Why it is wrong. Two effects stack. Ward-only damage is dropped, so the timeline undercounts the fight. A mixed hit's type ratio is the ratio before mitigation, stretched to equal health lost. "62% of the damage that killed you was fire" overclaims both.

What changed. Advisor lines 52 to 66 say "recorded health lost" and "pre-mitigation mix". Pattern text never used the old phrase (the pattern test forbids it) and was left in that safer form.

Still open, UI, not edited. `DeathPanel.cs` lines 142 to 146 prints `Physical: 1,200 (62%)` with no caveat. Suggested one-line heading: "Health lost by type, pre-mitigation mix". Do not invent a mitigated split.

### 4. High. Freeze and chill told the player to cap cold resistance. Fixed.

`Advisor` per-death freeze branch, now lines 155 to 158. `PatternBody` `cc_freeze`, now line 355. Previously the per-death sentence was added whenever `Res.Cold` was under 74.5, and the pattern sentence always included it.

Why it is wrong. The freeze page: chance depends on freeze rate, max health, and current ward. Chill is a slow. Cold resistance mitigates cold hits and cold damage over time (frostbite). It does not mitigate being frozen or chilled.

What changed. Both strings say cold resistance does not stop freeze or chill, and they point at max health, ward, and frostbite. A cold hit in the same death still produces `res_Cold` from the share loop, which is the right tip for the damage. `Test_Advice_FreezeFromAilmentTap` still requires both keys. `Elements` effect lines for Freeze and Chill now match the support page (3 stacks, 12% less speed, half of that against players).

### 5. Medium. Physical resistance and endurance were ignored even when the snapshot had them. Fixed.

`Advisor` share loop and killing-element branch, previously lines 51 to 70. Endurance body, previously the belt sentence.

Why it is wrong. Physical resistance is one of the seven capped resistances and it reduces physical hits and bleed. Armor does not reduce bleed. Endurance applies to the slice of damage under the threshold, including damage over time, and the snapshot keys `Endurance` and `EnduranceThreshold` already exist on the record. Quoting neither, and pointing at a belt-only affix from patch 1.2, sends the player to the wrong craft.

What changed.

- Physical share at or above 25% always calls `ResTip("Physical")`, hit or damage over time.
- Armor is added only for the hit portion (`hitShare >= 0.25`), or for a killing element of Physical whose ailment is not a damage over time. `Test_GameReport_BleedDoesNotRecommendArmorForItsDamage` and `Test_Advice_BleedMentionsArmorDoesNotHelp` still hold.
- The armor sentence quotes `Armor` as a rating and says the mitigation percent needs area level.
- `EnduranceBody` quotes endurance percent and threshold when present, states the 60% cap, and states that endurance does not apply to ward.

`Test_Advice_PhysicalHitQuotesResistanceAndEndurance` covers a physical hit with 40% physical resistance, 20% endurance, threshold 400, armor 1800.

Pattern ranking is unchanged for the locked case (three fire one-shots and one physical). `res_Physical` applies to one death. `ehp` applies to four and stays second after the generic 0.6 discount. `Test_Patterns_PrioritiesCountDeathsTheyWouldHaveHelped` still expects that.

### 6. Medium. A capped resistance still took a recommendation slot. `Show` fixed. The panel does not call it yet.

`ResTip` capped branch, Advisor lines 85 to 90. Weight 30, title "already capped". That part of the earlier review is already correct: it is not worded as "go cap this", and it is not the top `Suggest` tip (`Test_Advice_CappedResistanceSaysSo`).

Why the slot still mattered. `DeathPanel` prints every `Suggest` tip except `unknown`, up to `MaxTips` (5). A capped line crowds out endurance or health.

What changed. `Advisor.Show` (lines 211 to 235) drops titles that contain "already capped" and returns at most `MaxShown` (3). If the only tips were capped notices, `Show` returns one action: raise health, ward, and endurance, and it says endurance does not apply to ward. If nothing at all was supported, it returns "No supported change from this record". `Suggest` and `MaxTips` are unchanged, so the tests that look up the capped tip still find it.

Proposed panel patch, one call site, `DeathPanel.cs` line 153. Not applied. The panel is outside this lane's edit list, and the card also needs `Quote` and `Confidence`, which are a layout change rather than a one-line fix.

```csharp
var tips = Advisor.Show(d, _records).Where(t => t.Key != "unknown").ToList();
```

### 7. Medium. Ailment names matched by substring. Fixed.

`Ailments.Find`, now lines 84 to 108.

Why it is wrong. `Contains("shock")` is true for `Shockwave`. `Contains("freeze")` is true for `FreezeRate` and `FreezeRateMultiplier`. `Contains("slow")` is true for `SlowRetaliation`. Those are stat names. Treating them as ailments on the player manufactures a freeze or shock tip for a death that did not have one. The old guards (resistance without shred, chance, shrine) did not cover rate, multiplier, retaliation, or avoidance.

What changed. Exact letter-only id still wins, so `ArmourShred`, `TimeRot`, `Shock`, `Stun`, `Frozen` resolve as before. Otherwise the name is split on separators and camel case. Tokens `chance`, `shrine`, `rate`, `multiplier`, `retaliation`, and `avoidance` reject the whole string. A keyword must be a whole token or a consecutive run of tokens (`Res` + `Shred` = `resshred`, `Abyssal` + `Decay`). Leftover tokens must be one of the seven element names. Wrapper tokens `ailment`, `clone`, `stacking`, `debuff`, `buff`, `effect` are ignored, which keeps `Ailment_Bleed`, `IgniteAilment(Clone)`, and `StackingAbyssalDecay`.

`Test_AilmentFind_RejectsStatCompounds` adds `Shockwave`, `ShockWave`, `FreezeRate`, `FreezeRateMultiplier`, and `SlowRetaliation`.

### 8. High. Online defensive stats are never read. Not fixed. Hook file.

`PlayerProbe.Defenses`, lines 286 to 311. If `ProtectionClass` is null the method returns null at line 289. Nothing in the repo calls `PlayerFinder.getLocalPlayerPrecalculatedStatsHolder`. Codex is right that the actor and protection path is null online. RCInet's public `Refs_Manager` for 1.4 still uses `getPlayerActor` plus `ProtectionClass`, so this holder is not confirmed by that file either. There is no `PrecalculatedStatsHolder` in the 2023 dump. Member names below are the 2023 `ProtectionClass` names to try by reflection. They are not a guaranteed field list for Season 5.

Why it is wrong. An online death then has no `Res.*`, no armor, no endurance, no ward. Every tip falls back to "cap X (75%)" with no personal number, including when the player is already capped. The advice becomes generic exactly where the network death report is the better data source.

Proposed patch, `PlayerProbe.Defenses` only. Do not hardcode `SP` enum integers. The enum grew (for example `IncreasedAreaForAreaSkills` in 1.4).

```csharp
var prot = Refl.Get(Actor, "protection") ?? Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ProtectionClass"));
var holder = Refl.Static("Il2Cpp.PlayerFinder", "getLocalPlayerPrecalculatedStatsHolder");
if (prot == null && holder == null) return null;
object src = prot ?? holder;
```

Read by name with `Refl.Get(src, names)` / `Refl.TryFloat`. Try each name on `src`. If `prot` and `holder` both exist, read `prot` as today and log holder disagreements once under `ProbeApi`. Do not average them.

| Record key | Names to try | Unit once `DefenseSnapshot` accepts it |
| --- | --- | --- |
| `Res.Physical` | `uncappedPhysicalResistance`, `physicalResistance` | Percent. Cap 75. Keep the raw overcap on `ResUncapped.Physical` the way `Normalize` already does. |
| `Res.Fire` | `uncappedFireResistance`, `fireResistance` | Same. |
| `Res.Cold` | `uncappedColdResistance`, `coldResistance` | Same. |
| `Res.Lightning` | `uncappedLightningResistance`, `lightningResistance` | Same. |
| `Res.Necrotic` | `uncappedNecroticResistance`, `necroticResistance` | Same. |
| `Res.Void` | `uncappedVoidResistance`, `voidResistance` | Same. |
| `Res.Poison` | `uncappedPoisonResistance`, `poisonResistance` | Same. |
| `Endurance` | `endurance` | Percent. Cap of the *reduction* is 60. Do not clamp the stored value at 75. |
| `EnduranceThreshold` | `enduranceThreshold` | Flat health, not a percent. |
| `CritAvoidance` | `critAvoidance` | Percent. Drop if the scaled value is above 100.5, as today. |
| `Block` | `blockChance` | Percent, additive. |
| `BlockEffectiveness` | `blockProtection` | Rating. Not a percent. 2023 name, inferred as block effectiveness. |
| `Armor` | `armour`, `armor`, else `armourForCharacterSheet()` | Rating. Not a percent. |
| `Dodge` | `dodgeRating` | Rating. Do not store `dodgeChance()` as if it were the rating. That method needs area level. |
| `Ward` | `CurrentWard`, `currentWard` | Flat. This is after the hit if you read it at death. Say so, as `DefenseLine` already does. |
| `StunAvoidance` | `stunAvoidance` | Rating. Never a percent key. |
| `GlancingBlow` | `glancingBlowChance` | Percent, optional. |
| `MaxHealth` | player health, not the holder | Flat. `PlayerProbe.MaxHealth` is already the right source. |

On the first successful holder read, if `ProbeApi` is on, log every field and property name and type once. If a member looks like less-crit-damage (`lessBonusDamageTakenFromCrits` or similar), log it and do not clamp it onto a resistance. The 2023 `ProtectionClass` has no such field. Misses stay missing. A missing key is better than a made-up 0.

`DefenseSnapshot` will then do the right thing with stun avoidance, because of bug 1.

### 9. Medium. A missing `DamageType` enum is labeled with the 2023 order. Not fixed.

`ArgReader.ElementMap.Build`, lines 200 to 203.

```csharp
if (t == null || !t.IsEnum) return new[] { 0, 1, 2, 3, 4, 5, 6 };
```

Why it is a risk. That array matches the 2023 dump. If Season 5 inserts a value, or if the type fails to load and the arrays are in a different order, every element total is mislabeled and the advice caps the wrong resistance. An unlabeled hit is a smaller mistake than a confident wrong color.

Proposed patch:

```csharp
if (t == null || !t.IsEnum) return new[] { -1, -1, -1, -1, -1, -1, -1 };
```

`ToOurOrder` already skips negative slots. Log the failure. When the type loads, keep the name-based map (lines 210 to 214), which is the right approach.

### 10. Medium. An unreadable crit flag is stored as "not a crit". Not fixed.

`DeathReportHooks.Capture`, line 65: `Crit = args[2] is bool crit && crit`. `DeathDetails.Apply`, line 23: `record.KillingCrit = Crit` always.

Why it is wrong. `KillingCrit` is `bool?`. False means "the game said this was not a crit". A boxed Il2Cpp bool that `is bool` misses becomes false, and Apply overwrites a timeline `true`. The player then loses the crit-avoidance tip. `Test_GameReport_PreservesObservedHitClassification` requires an explicit false to overwrite, so Apply must keep writing when the hook really saw a bool.

Proposed hook patch:

```csharp
bool? crit = args[2] is bool b ? b : (bool?)null;
// pass crit only when non-null
```

Proposed `DeathDetails` patch: change `Crit` to `bool?`, and in `Apply` assign `record.KillingCrit` only when `Crit.HasValue`. An explicit false still overwrites. A missing box does not. This lane did not change it, because the hook is the only place that can tell "false" from "not a bool", and the existing test constructs a definite false.

`Quote` already appends ", crit" only when `KillingCrit == true`. It does not say "not a crit" for false or null.

### 11. Medium. Primary type and secondary type are easy to drop. Not fixed.

`DeathReportHooks.ElementName`, lines 78 to 79, parses `value.ToString()` with `Elements.TryParse`. A numeric `1`, or a string `DamageType.FIRE` that includes the type name, fails the parse and the element is stored null. Secondary type is kept only when `Refl.Get(args[1], "HasValue", "hasValue") is true` (line 64). An Il2Cpp `Nullable<DamageType>` often does not expose `HasValue` that way, so the second type disappears. The advice then talks about one type.

Proposed `ElementName`: if the runtime type is an enum, use `Enum.GetName`. If `ToString` contains a dot, parse the segment after the last dot. Log the runtime type the first time parsing fails.

Proposed secondary read: try `HasValue` as a bool, and also try `GetValueOrDefault` / a non-null `Value`. If the nullable's type name cannot be reflected, leave `SecondaryElement` null and say so in the confidence line. Do not split the damage number in half to invent the other type.

### 12. Medium. The named killer is the last event in the window, which extends past the death. Not fixed.

`DeathAnalyzer.Analyze`, lines 102 to 110, sets killer, ability, element, crit, and blow from `window[window.Count - 1]`. The window includes hits up to 0.5 seconds after `deathTime` (line 53).

Why it is partly right and partly wrong. The death screen's killing blow is the hit or tick that took health through zero, which is often the last event at the death time. `Test_KillingDotTick_NamesAilment` locks that: a 25 bleed tick at the death time is the named killer even after a 200 physical hit. A tick that lands in the 0.5 second grace period after death is not that blow, and it currently replaces the name. The Kill flag (`GameHooks` line 305, `FlagKill = 4`) is used to *detect* death and is not stored on `HitEvent`, so the analyzer cannot prefer it.

When the game report arrives, `DeathDetails.Apply` overwrites killer, ability, element, and damage if those strings are non-empty. Timeline-only deaths keep the analyzer's choice. `Quote` prefers ability, then ailment, then killer, so a report with an ability name wins once Apply has run.

Proposed analyzer patch, without breaking the bleed-tick test:

```csharp
var lethal = window.LastOrDefault(h => h.Time <= deathTime) ?? window[window.Count - 1];
// use lethal for Killer / KillingBlow / KillingElement / KillingCrit
```

Keep post-death ticks in the damage totals. A later lane can add `HitEvent.Killed` from the Kill flag and prefer that hit.

### 13. Medium. Ward damage never enters the timeline. Not fixed.

`GameHooks.HitPostfix`, lines 289 to 293. Health lost at or below 0 returns immediately. The comment says dodge, block, or ward. A hit that only breaks ward is invisible. A hit that breaks ward and then health is recorded as the health piece alone. The death-report `damage` is the killing blow after mitigation, which is a different quantity from the sum of timeline `Amount`s.

Consequence. "You took 40% of your max health in this window" can be true of health and still miss the ward that was the real pool. `Quote` prefers the report's damage and overkill when those fields exist. The timeline percent must not be placed next to the report number as if they were the same measurement.

No hook edit in this lane. The wording rule is: if `DetailSource` is set, the quoted blow is the report. The timeline is context ("other hits in the last few seconds"), not a second killing blow.

### 14. Medium. A late report does not double-count. It can enrich the wrong death. Not fixed.

`DeathTracker.OnDeath`, lines 213 to 264. The first signal only stores `_pendingDetection` and returns. About 0.75 seconds later `Update` commits one record. A second `OnDeath` while `_dead` returns at line 215. `OnDeathDetails`, lines 318 to 334, Apply's onto `JustDied` when the character matches and the death was within 5 seconds, then `SaveUpdated`. It does not call `Log.Append`. It then calls `OnUnownedDeathSignal`, which calls `OnDeath("hook")`, which returns immediately because `_dead` is true. The game counter is reconciled by `DeathCountLedger`: a detection other than `"game"` calls `_ledger.Recorded`, and a later counter tick inside the credit window adds nothing (`Test_Ledger_CounterLagsLongAfterHook`, `Test_LateGameReport_PersistsSameRecordAndCount`).

So the enrichment path does not add a second row. The residual race is different. `_details` is applied to whatever `JustDied` is, if it is the same character and younger than 5 seconds (line 327). Two deaths inside that window can take each other's killing blow. `_details` is also applied at commit time if it is within 5 seconds of the pending death (lines 288 to 289). A packet that arrives after a respawn, once `_dead` is cleared (line 99, health above 0 for 1 second), can start a new pending death through `OnUnownedDeathSignal` only while health is still 0. If health is already above 0, that signal is ignored (lines 133 to 136). The stale `_details` can still be applied to the next commit inside 5 seconds.

Proposed patch: stamp the report with a death id or with `_pendingUtc`, and Apply only when the report time is within 0.75 seconds of that death's `_deadAt`, not 5 seconds of the latest death. Five seconds is long enough for a second death in a boss fight.

### 15. Medium. An online report is dropped when `HasPlayer` is false. Not fixed.

`DeathReportHooks.Capture`, line 59, returns unless `PlayerProbe.HasPlayer`. `OnDeathDetails` line 320 does it again. `HasPlayer` needs a character name and either a live `GameObject` pointer or local-player health. Codex added name fallbacks (`getLocalPlayerInMultiplayer`, `getLocalTreeData`). The network hook can still fire for `getLocalActorSync` while `HasPlayer` is false, and the killing blow is discarded before the 0.75 second wait.

Proposed patch: if the network filter already matched `getLocalActorSync` (lines 49 to 51), store the `DeathDetails` even when the actor object is null, as long as a character name resolved. Do not require `ProtectionClass`. Creating the death record can still wait for health or the game counter. Losing the packet is the worse failure, because it does not arrive twice.

### 16. Low. A timeline one-shot is not cleared when the report arrives. Not fixed, and not always wrong.

`DeathDetails.Apply` sets `Kind` from `Unknown` to `Reported` and does not touch `OneShot` or `Burst`. `Test_GameReport_PreservesObservedHitClassification` requires a `Burst` to stay a `Burst`. That is reasonable when the timeline is real.

The bad case is bug 2: a single hit with unknown max health is `OneShot`, then a report arrives and the kind stays `OneShot`. Apply should downgrade `OneShot` to `Reported` when `MaxHealth <= 0` and `Hits <= 1`. Do not downgrade a `Burst` or a `OneShot` that had a real max health. Not edited, so the locked test stays green.

### 17. Low. Crit avoidance at 100% while the blow still crit is handled. No change.

`Advisor` lines 110 to 114. If crit avoidance is at or above 99.5, the body does not say "You had 100%". The blow crit anyway, so claiming the stat worked would be false. `Test_Advice_CrittedDespiteFullAvoidanceDoesNotClaimImmunity` locks this. Leave it.

### 18. Low. Death-recap color tags. Small Core fix. Theme palette is still the old one.

`DeathText` previously accepted only 6-digit and 8-digit hex, plus red, white, yellow, orange, green, blue, purple. A 3-digit tag or `cyan` / `teal` was stripped, so the run was uncolored. `Theme.GameColor` already parses 6-digit and 8-digit hex. It does not know the names cyan and teal.

What changed. `DeathText` accepts 3-digit, 6-digit, and 8-digit hex, and the extra names. A 3-digit tag is expanded (`#f00` becomes `#ff0000`) so the existing Theme parser paints it. `cyan` is stored as `#13A8C9` and `teal` as `#29AB85`, the sheet word colors from the palette section. 6-digit and 8-digit values are untouched. `Test_DeathText_KeepsShortHexAndCyan` covers this.

`Theme.ElementColors` (lines 30 to 38) is a separate bug. See the palette section for the proposed constants. Not edited, because this lane does not change UI files, and no end-user DLL is added. The colors are compile-time constants.

## Player-facing card

Three pieces, in this order. At most three recommendations. Plain sentences. Commas and periods. No double dashes, no em dashes.

1. **Quote.** One sentence, only from fields that are actually set. `Advisor.Quote`.
2. **Recommendations.** `Advisor.Show`. At most three. Each title is the action. Each body is one or two short sentences that include the recorded number the action is based on. A capped resistance is not one of the three.
3. **Confidence.** One line. `Advisor.Confidence`.

`Suggest` remains the full list (up to five), including the capped notice, for the pattern counter and the tests. The panel should call `Show`, not `Suggest`.

### Quote

Shape, when the fields exist:

`Killed by {ability or ailment} from {attacker}, {damage} {type} damage, crit, {overkill} overkill.`

Rules:

- Ability is used if it was recorded. Otherwise the ailment name. Otherwise just the attacker: `Killed by Rahyeh, 1,240 lightning damage, crit.`
- ", crit" only when `KillingCrit` is true. Silence when it is false or unknown.
- Two types: `800 fire and physical damage`. The number is not halved.
- Overkill is included only when it is greater than 0. It is damage past 0 health, not a second hit.
- No attacker, no ability, no ailment, no type, no damage, and no crit: `The killing blow was not recorded.`
- Do not say one-shot, do not say "100% of your life", and do not say "percent of the life you had" inside the quote.

### Recommendations

Rank by the existing weights, after dropping capped notices, then take three.

Each body must contain the recorded fact it used:

- A resistance gap quotes the sheet value and the points to 75. "You had 41%. You were 34 points short of the 75% cap."
- A capped killing type is not listed. The next action is health, ward, and endurance.
- Armor quotes the rating. It does not invent a mitigation percent. It is not recommended for bleed or other physical damage over time.
- Endurance quotes the percent and the threshold when they were read. It says the reduction caps at 60% and does not apply to ward.
- A crit quotes crit avoidance when it was read, and it says enemy crits deal double. At 100% avoidance, enemy hits do not crit. If the blow crit anyway while avoidance was 100%, do not claim the stat worked.
- Freeze and chill quote max health and ward as the levers. They do not say to cap cold resistance. A cold hit in the same death still gets a cold-resistance action.
- A repeated killer (3 or more in the history passed in) can be one of the three when its weight reaches the cut. The title already quotes the count.

### Confidence

| Line | When |
| --- | --- |
| `High: game death report, hit timeline, and max health` | All three present. |
| `Medium: game death report and hit timeline, max health unknown` | Report and hits, no max health. |
| `Medium: game death report and max health, no hit timeline` | Report and max health, no hits. |
| `Medium: game death report, no hit timeline, max health unknown` | Report only. |
| `Medium: hit timeline and max health, no game death report` | The usual offline timeline death. The killer is the analyzer's last in-window hit, not the death screen. |
| `Low: hit timeline, max health unknown` | Hits, no pool, no report. |
| `Low: max health only, no hit timeline` | Pool known, nothing else. |
| `Low: no hit timeline, max health unknown` | Nothing. This is the empty record. |

High does not mean the element split is post-mitigation. It means the three inputs exist. The recommendation text still calls the split a pre-mitigation mix.

### Fallback

If `Show` has no action left because every tip was a capped notice, the card is:

> Raise health, ward, and endurance.
> The resistance on the killing blow was already at the 75% cap. The next layer is more health, more ward, and endurance on the damage that lands below the endurance threshold. Endurance does not apply to ward.

If the record supports nothing (no type, no ailment, no kind the rules understand), the card is:

> No supported change from this record.
> The recorded facts do not support a specific fix. Nothing here is a measured gap.

An unknown death that the hooks missed still uses the existing probe tip ("No hit data for this death"), because that one tells the player why the card is empty. The panel currently hides `unknown`. Hiding it without the fallback leaves a blank. Prefer the fallback sentence over a blank panel.

### What the card must not say

- "100% of your life", or any percent of life, when max health was not recorded.
- "One-shot" when max health was not recorded, or when the only evidence is a single timeline hit.
- A mitigated type split. Say "pre-mitigation mix" or omit the percent.
- A 50/50 split of one reported damage number.
- An armor mitigation percent without area level.
- "Cap cold resistance" as the fix for freeze or chill.
- "Cap this resistance" when the snapshot shows it at 75% or above (74.5 rounds to the capped branch).
- Stun avoidance as a percent, or any advice that treats it as a resistance.
- "Damage taken increased" for ordinary Shock. Shock is lightning resistance and stun chance.
- Ground effect, unless the ability or ailment text itself says ground, lava, pool, or similar. `irregularDamageSourceID` is not verified.
- A boss damage type that the report did not name. `isBossfight` only means the game flagged a boss fight. The blow is still one hit, not the whole fight.
- Online defenses that were not read. Say they were not readable.

## Color palette

Text color for a damage type should follow the **word** on the character sheet, not the skill VFX and not the icon highlight.

Sampled from the Maxroll "Defenses for Beginners" sheet image `resistances-new.png` (`https://assets-ng.maxroll.gg/wordpress/resistances-new.png`, article updated 2026-03-26). The file used here was 933 by 201. Column order, confirmed by cropping: Fire, Lightning, Cold, Physical, Poison, Necrotic, Void. The name row is about y = 126 to 144. Highlight means the brightest saturated pixel in that row. Body means the average of the middle half of saturated pixels, which is darkened by the sheet background and is the wrong choice for UI text. Percent digits in the row below peak at `#F3E8DF` (top of that band) and are the same warm off-white in every column. They are not type-colored.

| Type | Text color (word highlight) | Letter body, do not use for text | Icon highlight, do not use for the word |
| --- | --- | --- | --- |
| Fire | `#D24322` | `#8A2811` | `#FAF52B` yellow, plus an orange flame |
| Lightning | `#1F58C8` | `#143B89` | `#26D1D8` cyan |
| Cold | `#13A8C9` | `#0A6679` | pale cyan, up to `#CDEBF4` |
| Physical | `#DEC29F` | `#988063` | grey helmet, no saturated pixels in the icon band |
| Poison | `#18933B` | `#0E5E22` | `#7EE4B6` |
| Necrotic | `#29AB85` | `#186D4F` | `#8AFFFE` |
| Void | `#6F17A4` | `#4B0D68` | `#F190F5` |

Fire is red. The flame icon's brightest pixel is yellow. Using the icon color would make fire look orange or yellow. The word is `#D24322`.

Lightning's word is blue. Older posts call lightning skill effects yellow. Both can be true: the sheet label sampled here is blue, and in-world bolts can still be yellow. UI text should follow the sheet word, `#1F58C8`.

Physical's word is a warm tan. FoveonBlue (forum thread 57334, 2023) described physical *effects* as dark red. The sheet label is not dark red. UI text should use `#DEC29F`.

Necrotic is teal. The same Maxroll crop peaks at `#29AB85`. A separate character-sheet necrotic symbol posted by Heavy (forum thread 31279, `https://i.imgur.com/sKzFKwI`) was remeasured at 75 by 52: brightest saturated pixel `#38E5C1`, mean of saturated pixels `#228267`. That is the same teal family as the sheet icon highlight `#8AFFFE` and the word `#29AB85`. Llama8 (forum thread 74875) calls necrotic cyan and void purple, which agrees.

Cold as light blue, void as purple, and poison as green agree with FoveonBlue and with this crop.

`Theme.cs` lines 32 to 38 today: Physical `#C8B9A6`, Fire `#E2683C`, Cold `#6FB7E8`, Lightning `#E8D04A`, Necrotic `#4FB0A0`, Void `#9B6BD6`, Poison `#7BC043`. Fire is orange there. Lightning is yellow. Proposed replacement, UI file, not applied:

```csharp
Hex(0xDEC29F),   // Physical, sheet word highlight
Hex(0xD24322),   // Fire, sheet word. Red.
Hex(0x13A8C9),   // Cold
Hex(0x1F58C8),   // Lightning, sheet word. Blue.
Hex(0x29AB85),   // Necrotic
Hex(0x6F17A4),   // Void
Hex(0x18933B),   // Poison
```

No new DLL. These are integer constants. Search-engine hexes that were not measured from a sheet or a posted symbol are not used.

## Example cards

Each card is what the player should read. The quote and the confidence line match `Advisor.Quote` and `Advisor.Confidence` for the record described. The three lines match `Advisor.Show` for that record, shortened only by dropping the repeated "pre-mitigation mix" clause where the same death's quote already carries the type. Where today's `Suggest` body is longer, the card keeps its facts.

### 1. Fire hit, resistance under the cap, offline timeline

Record: timeline, max health 1000, last hit 900 fire from Lagon, crit, fire resistance 41, no game report.

> Killed by Lagon, 900 fire damage, crit.
>
> 1. Cap fire resistance: you had 41%. Recorded health lost was 100% fire on the pre-mitigation mix. You were 34 points short of the 75% cap.
> 2. Get critical strike avoidance to 100%. The killing blow was a critical strike, which hits for double. Enemies cannot crit you at 100% critical strike avoidance.
> 3. Raise your effective health pool. The last recorded hit removed 90% of your max health.
>
> Medium: hit timeline and max health, no game death report.

### 2. Capped fire resistance, lethal hit, report and timeline

Record: report plus timeline, max health 1400, killing blow 1600 fire, overkill 200, fire resistance 75, not a crit. Kind stays OneShot because max health is known.

> Killed by Lagon, 1,600 fire damage, 200 overkill.
>
> 1. Raise your effective health pool. The last recorded hit removed 100% of your max health. Health removed is 1,400 after overkill is taken out.
> 2. Get endurance and endurance threshold. Endurance was 20%. Endurance threshold was 280. The reduction caps at 60% and does not apply to ward.
> 3. Avoid hits: dodge and block, if the window was a burst rather than one hit. Otherwise stop at two. Capped fire resistance is not listed.
>
> High: game death report, hit timeline, and max health.

The next layer is health, ward, and endurance. Raising fire resistance does nothing while it is on the cap, until something shreds it.

### 3. Ignite

Record: ignite ticks, fire resistance 52, the last tick is ignite from a Fire Wraith, 80 fire damage, max health 1000, no report. Armor is not the tip. Ignite is fire damage over time.

> Killed by Ignite from Fire Wraith, 80 fire damage.
>
> 1. Cap fire resistance: you had 52%. You were 23 points short of the 75% cap.
> 2. Ignite: fire resistance. Ignite deals fire damage over time. Keep health regen or leech up.
> 3. Out-heal damage over time. Step out of the burning ground if the ability name says ground. Do not say ground from the ignite id alone.
>
> Medium: hit timeline and max health, no game death report.

### 4. Bleed

Record: bleed ticks, physical, physical resistance 30, armor 2200, max health known, no report. Armor must not be recommended for the bleed itself.

> Killed by Bleed from Bandit, 40 physical damage.
>
> 1. Cap physical resistance: you had 30%. You were 45 points short of the 75% cap. Armor does not reduce bleed.
> 2. Bleed: physical resistance. Armor does not reduce bleed.
> 3. Out-heal damage over time. Health regen and leech are the sustain. Ward helps only until the ward is gone, and endurance does not apply to ward.
>
> Medium: hit timeline and max health, no game death report.

### 5. Poison

Record: poison ticks, poison resistance 60 with uncapped 90, so shred headroom exists, max health known.

> Killed by Poison from Spider, 36 poison damage.
>
> 1. Cap poison resistance: you had 60%. You were 15 points short of the 75% cap. Poison stacks also shred poison resistance. The 90% before the cap is headroom against that shred, and it does not raise the cap.
> 2. Poison: poison resistance. Each poison stack also lowers your poison resistance.
> 3. Out-heal damage over time.
>
> Medium: hit timeline and max health, no game death report.

If poison resistance is already 75 and uncapped is 110, the capped notice is dropped. The actions are the poison sustain tip and health regen or leech, and the body says the overcap is headroom against shred only.

### 6. Critical physical hit

Record: report, Rahyeh absent, Brute, 2,000 physical, crit, max health 1500, physical resistance 40, armor 1800, crit avoidance 25, endurance 20, threshold 400. Timeline present.

> Killed by Brute, 2,000 physical damage, crit.
>
> 1. Get critical strike avoidance to 100%. You had 25% critical strike avoidance. The killing blow was a critical strike, which hits for double.
> 2. Cap physical resistance: you had 40%. You were 35 points short of the 75% cap.
> 3. Stack armor. Recorded armor was 1,800. That is a rating. Mitigation also depends on area level, so no mitigation percent is stated.
>
> High: game death report, hit timeline, and max health.

Endurance is the fourth fact and stays off the card unless one of the three above is missing. Enemy crits deal 200%. Damage over time cannot crit, so this tip is for the hit.

### 7. Void

Record: report, Void Nova from Void Horror, 1,234 void, Time Rot, crit, overkill 100, no timeline, no max health.

> Killed by Void Nova from Void Horror, 1,234 void damage, crit, 100 overkill.
>
> 1. Cap void resistance (75%). The killing blow was void damage. The personal void resistance was not readable, so the gap is not stated.
> 2. Time Rot: void resistance. Time Rot deals void damage over time. It also makes stuns on you last longer.
> 3. Get critical strike avoidance to 100%. The killing blow was a critical strike, which hits for double.
>
> Medium: game death report, no hit timeline, max health unknown.

Do not call this a one-shot. Overkill says the tick went past 0 health. It does not say the fight was one hit.

### 8. Necrotic

Record: timeline attrition, Wraith, necrotic, damned on you, necrotic resistance 10, max health known, no report.

> Killed by Wraith, 60 necrotic damage.
>
> 1. Cap necrotic resistance: you had 10%. You were 65 points short of the 75% cap.
> 2. Damned: necrotic resistance. It also cuts your health regen, so lean on leech.
> 3. Improve sustain. You were worn down over several seconds. Health regen, leech, and potion upkeep matter here.
>
> Medium: hit timeline and max health, no game death report.

### 9. Ground effect

Record: ability text "Lava Pool", fire, 400, not a crit, fire resistance 70, max health known, report present, no timeline. The ability name is the only reason the card says ground.

> Killed by Lava Pool, 400 fire damage.
>
> 1. Cap fire resistance: you had 70%. You were 5 points short of the 75% cap.
> 2. Out-heal damage over time, if the ticks were damage over time. Move out of the lava pool. The pool is named because the ability text says so.
> 3. Raise your effective health pool if the kind was a burst or a one-shot with known max health.
>
> Medium: game death report and max health, no hit timeline.

If the ability is only "Fireball", do not say ground. `irregularDamageSourceID` is not verified and is not quoted.

### 10. Boss

Record: `isBossfight` true, report, Lagon, 3,400 cold, crit, overkill 900, cold resistance 75, timeline of many smaller hits, max health 2000. The boss flag does not add a damage type.

> Killed by Lagon, 3,400 cold damage, crit, 900 overkill.
>
> 1. Get critical strike avoidance to 100%. The killing blow was a critical strike, which hits for double.
> 2. Raise your effective health pool. This blow is the hit that took health through zero, not the whole fight.
> 3. Avoid hits: dodge and block. The earlier hits are why the last tick finished you. Cold resistance was already capped, so it is not an action. Overcap only helps if something shredded cold resistance first.
>
> High: game death report, hit timeline, and max health.

### 11. Repeated killer

Record: Heorot has killed you 4 times in the loaded history. This death is an attrition with no element. The nemesis tip is allowed into the three because the specific defenses are missing.

> Killed by Heorot.
>
> 1. Improve sustain. You were worn down over several seconds.
> 2. Heorot has killed you 4 times. Watch for the wind-up and keep a movement skill ready.
> 3. Avoid hits: dodge and block.
>
> Medium: hit timeline and max health, no game death report. (Hits exist, type does not.)

If this same death is also a crit fire one-shot, the nemesis line loses to crit avoidance, fire resistance, and health, and it stays off the card. The history view can still show the count.

### 12. Missing data

Record: empty. Hooks did not see hits, the report did not arrive, max health is unknown.

> The killing blow was not recorded.
>
> 1. No hit data for this death. The mod did not see the hits, so it cannot tell what to fix.
>
> Low: no hit timeline, max health unknown.

If the panel hides the unknown tip, replace it with "No supported change from this record." Do not fill the gap with a guessed element.

### 13. Offline, protections read

Record: local `ProtectionClass` present, fire 41, cold 75, armor 1200, endurance 35, threshold 500, ward 0 after the hit, stun avoidance 750 stored raw and not shown as a resistance. Death is example 1.

The card is example 1, plus the defense line the panel already prints: `Fire 41%  ·  Cold 75% (cap)  ·  Armor 1,200  ·  Endurance 35%`. Stun avoidance must not appear as "750%". Ward after the hit is not the ward you had when the fight started. Say "after the hit" if it is shown, which `DefenseLine` already does.

Confidence stays Medium until the game report is also stored.

### 14. Online, holder not read

Record: the network report arrived (example 7's numbers), `ProtectionClass` was null, and `getLocalPlayerPrecalculatedStatsHolder` was not called. Defenses are null.

> Killed by Void Nova from Void Horror, 1,234 void damage, crit, 100 overkill.
>
> 1. Cap void resistance (75%). Your resistances were not readable online, so this does not use your sheet.
> 2. Time Rot: void resistance.
> 3. Get critical strike avoidance to 100%.
>
> Medium: game death report, no hit timeline, max health unknown.
>
> Defenses: not readable. The local protection object was missing, and the precalculated stats holder was not read.

Do not print zeros. Do not say the player is uncapped.

### 15. Capped fire, report only, no timeline

Record: `Kind` Reported, fire, damage 900, fire resistance 75, nothing else.

> Killed by the blow the game reported, 900 fire damage.
>
> 1. Raise health, ward, and endurance. The resistance on the killing blow was already at the 75% cap. Endurance does not apply to ward.
>
> Medium: game death report, no hit timeline, max health unknown.

This is the `Show` fallback when the only `Suggest` tip is the capped notice. It is one action, not three empty slots.

### 16. Mixed fire and physical, one number

Record: report, primary fire, secondary physical, damage 800, crit false, ability "Searing Slash", attacker "Forge Guard".

> Killed by Searing Slash from Forge Guard, 800 fire and physical damage.
>
> 1. Cap fire resistance (75%), if fire resistance was not read.
> 2. Cap physical resistance (75%).
> 3. Stack armor, because the killing blow was not a damage over time. Quote the armor rating if it was read.
>
> Medium: game death report, no hit timeline, max health unknown.

The card does not say "400 fire and 400 physical". False crit is omitted, not described as a non-crit.

### 17. Freeze plus a cold hit

Record: frozen, three cold hits, cold resistance 40, max health known, no report.

> Killed by Frost Wraith, 300 cold damage.
>
> 1. Cap cold resistance: you had 40%. You were 35 points short of the 75% cap. This is for the cold damage, not for the freeze.
> 2. You were frozen. More max health and ward lower your chance to be frozen. Frostbite stacks raise it. Cold resistance does not stop freeze or chill.
> 3. Avoid hits: dodge and block, when the hits are a burst.
>
> Medium: hit timeline and max health, no game death report.

### 18. Shock, so the card does not claim a general damage amp

Record: shocked, lightning hit, lightning resistance 50.

> Killed by the lightning hit, with Shock on you.
>
> 1. Cap lightning resistance: you had 50%. Shock also lowers lightning resistance. Overcap is headroom against that, and the cap is still 75.
> 2. Shock lowered your lightning resistance. Each stack also makes you easier to stun. Get stun avoidance. Stun avoidance is a rating, not a percent.
> 3. The lightning hit's own health or avoidance tip, by kind.
>
> Medium, or High if the report and max health are both present.

The body does not say "you take more damage" for ordinary Shock.

## What this lane changed in code

- `src/Core/DefenseSnapshot.cs`: stun avoidance is a rating.
- `src/Core/Elements.cs`: ailment match is tokens. Freeze and chill effect text matches the support page.
- `src/Core/Advisor.cs`: physical resistance, endurance numbers, armor rating, no false 100% life, no cold-resistance freeze tip, `Quote`, `Confidence`, `Show`.
- `src/Core/DeathText.cs`: 3-digit hex, cyan, and teal survive.
- `tests/CoreTests/Program.cs`: the new cases named in the bug list.
- This file.

Not changed: `DeathAnalyzer.cs`, `DeathDetails.cs`, `PlayerProbe.cs`, `DeathReportHooks.cs`, `DeathTracker.cs`, `ArgReader.cs`, `GameHooks.cs`, `DeathPanel.cs`, `Theme.cs`. The patches for those are in the bug list. No extra DLL is added for players.

## Verification

SDK `8.0.425`.

`dotnet run --project medick_DeathCounter/tests/CoreTests`: 67 tests, all passed. Exit code 0.

`dotnet build medick_DeathCounter -c Release -p:NoGame=true -p:DeployToMods=false -warnaserror`: succeeded. 0 warnings, 0 errors. The assembly is the existing mod output under `bin/Release/net6.0/`. `DeployToMods=false` kept it out of any game folder.
