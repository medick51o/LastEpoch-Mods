# Last Epoch player defensive stats research (for medick_DeathCounter)

Compiled 2026-10-01 from public sources only. No game DLL was opened. Builds on `RESEARCH-game-api.md` (same source list and labels; not repeated here).

Labels:

- **CONFIRMED (current)**: seen in C# source or data compiled against MelonLoader `Il2Cpp*` interop for LE 1.4.x (RCInet/LastEpoch_Mods commit `6054a5f`, 2026-04-15).
- **CONFIRMED (dump 2023)**: seen in the Il2CppInspector dump in voideh/EpochTool commit `66f1e21` (2023-09-06, pre 1.0 build, Unity 2019.4.2x). The name existed then; it may have changed.
- **INFERRED**: my conclusion, not seen directly. Verify with the in game probe (`research/ApiProbe`).

Dump files used:

- https://raw.githubusercontent.com/voideh/EpochTool/master/appdata/il2cpp-types.h (structs, enums; `SP__Enum` at line 622161, `Stats__Fields` 622538, `ProtectionClass__Fields` 623808, `BaseStats__Fields` 628756, `CharacterStats__Fields` 628774, `CharacterSheet__Fields` 902224)
- https://raw.githubusercontent.com/voideh/EpochTool/master/appdata/il2cpp-functions.h (method signatures)

Searched with no result: GitHub code search for `uncappedFireResistance`, `armourForCharacterSheet`, `dodgeRatingForCharacterSheet`, `getLocalPlayerStats` (only RCInet), `GetStatValue` with `SP.`; web search for these names. No public mod reads the player's resistances or armor. No public dump of a 1.x build exists. deafwave/le-pandora, FallenStar08/Fallen_LE_Mods, ItsJustMeChris/Last-Epoch-Hud and medick51o/kg_LastEpoch_Improvements contain no defensive stat reads (Fallen only takes `SP modProperty, AT tags` as patch parameters in `Dev/AffixDisplayManager.cs`).

## Short answer

The final defensive numbers are not read through the stat list. The game pushes them into **fields on the player's `ProtectionClass` component** when stats update. That component is what `ApplyDamage` uses, so it is the value that actually mattered when the player died.

```csharp
Actor actor = PlayerFinder.getPlayerActor();                         // CONFIRMED (current)
ProtectionClass pc = actor.gameObject.GetComponent<ProtectionClass>(); // CONFIRMED (current, RCInet Refs_Manager.cs line 148)
// or actor.protection                                               // CONFIRMED (dump 2023 field)
float fireUncapped = pc.uncappedFireResistance;                      // CONFIRMED (dump 2023 field), percent units INFERRED
float fireShown    = Math.Min(fireUncapped, 75f);                    // INFERRED (cap is a hard 75, see section 5)
float armor        = pc.armour;                                      // CONFIRMED (dump 2023 field), British spelling
float armorSheet   = pc.armourForCharacterSheet();                   // CONFIRMED (dump 2023 method)
```

## 1. Where the player's final stats live, and how to get them

### CONFIRMED (current, RCInet `LastEpoch_Hud/Scripts/Refs_Manager.cs`)

- `Stats player_stats = PlayerFinder.getLocalPlayerStats();` (line 153). Return type `Il2Cpp.Stats`. RCInet only uses its `atLowHealth` bool (`Mods/Character/Character_LowLife.cs`).
- `ProtectionClass player_protection_class = player_actor.gameObject.GetComponent<ProtectionClass>();` (line 148), with `player_actor = PlayerFinder.getPlayerActor()`.
- `Refs_Manager.player_actor.stats.GetAttributeValue(CoreAttribute.Attribute.Strength)` and `player_actor.stats.level` (`Mods/NewItems/Items_Mjolner.cs` line 414, `Mods/Minimap/Minimap_Icons.cs` line 75). So `Actor.stats` exists today and is a `BaseStats` (or subclass) with `level` and `GetAttributeValue`.

### CONFIRMED (dump 2023): the class hierarchy

- `Stats : MonoBehaviour` fields: `List<Stats.Stat> stats`, `atFullHealth`, `atHighHealth`, `atLowHealth`.
- `BaseStats : Stats` adds `int level`, events `updateStatsEvent`, `updateStatsDisplayEvent`, `updateGoldEvent` (delegate `BaseStats.UpdateStatsAction()`), `Actor actor`, `perceivedUnimportanceModifier`, `increasedLeechRate`, `moreFreezeRatePerStackOfChill`.
- `CharacterStats : BaseStats` (the player) adds `myCharacterDataTracker`, `expTracker`, `goldTracker`, `usingAbility`, `abilityList`, `characterMutator`, `currentTransformedStats`, `statConversionsForTransform`, `currentChannellingStats`, `attributes`, `bowEquipped`, `rightWeaponAttackRate`, `leftWeaponAttackRate`, `attackRate`.
- `Actor` fields: `data`, `actorSync`, `health` (BaseHealth), `protection` (ProtectionClass), `stats` (BaseStats), `healthPotion`, ...
- `Stats.Stat` (nested class, the modifier record) fields: `SP property` (byte), `byte specialTag`, `AT tags` (int flags), `float addedValue`, `float increasedValue`, `List<float> moreValues`. Matches what RCInet writes into buffs today (`stat.property`, `stat.addedValue`, `stat.increasedValue`, `stat.moreValues`, `stat.tags`).
- `CharacterSheet : MonoBehaviour` (the character sheet UI) has static `CharacterSheet.instance` and fields `characterStats` (CharacterStats), `protClass` (ProtectionClass), `player` (GameObject), plus `TMP_Text` fields `armour`, `armourPercent`, `PhysicalRes`, `LightningRes`, `ColdRes`, `FireRes`, `VoidRes`, `NecroticRes`, `PoisonRes`, `blockEffectiveness`, `blockMitigation`, `BlockPhysRes` ... `BlockNecroticRes`, `dodge`, `DodgeChance`, `enduranceThreshold`, `PotionHealth`, `float nonPhysicalArmourMitigation`. Methods `UpdateSheet()`, `resistanceText(float res) : string`, static `RoundToPercentString(float) : string`, `ChangePlayer(GameObject)`. The sheet reads from `protClass`, which supports the ProtectionClass route below.

### INFERRED

- `PlayerFinder.getLocalPlayerStats()` returns the player's `CharacterStats` typed as `Stats`; cast with `.TryCast<CharacterStats>()` or `.TryCast<BaseStats>()` if you need `level` or the events. `getLocalPlayerStats` is absent from the 2023 dump, so it was added later.
- `BaseStats.UpdateStats()` (dump 2023, no args) recomputes everything and copies the protection results into `ProtectionClass` fields, then fires `updateStatsEvent`. Evidence: `BaseStats.hasProtectionStat()`, and `ProtectionClass` holds pre computed `armour`, `dodgeRating`, `uncapped*Resistance` floats rather than a `Stats` reference.

## 2. Reading one stat through `Stats` (generic route)

### CONFIRMED (dump 2023): methods on `Stats`

- `float GetStatValue(SP property, AT checkTags, float added, float increased, float more, byte specialTag)`
- `float GetTotalAdded(SP property, AT checkTags, byte specialTag)`
- `float GetTotalIncreased(SP property, AT checkTags, byte specialTag)`
- `float GetTotalModifier(SP property, AT checkTags, byte specialTag)`
- `float GetProtectionValue(SP property, AT checkTags, float added, float increased, float more)`
- `float GetStatValueExactMatch(SP, AT, float added, float increased, float more, byte specialTag, bool ignoreAdded)`
- `float GetConvertedStatValue(SP, AT, float added, float increased, float more, byte specialTag)`
- `float GetStatValueForHitEvents(SP, AT, HitEvents, float added, float increased, float more)`
- `Stats.Stat GetExactStatMatch(SP, AT, byte specialTag)`
- `float getPropertyMultiplier(SP, AT)`, `bool usesPropertyMultipliers()`
- Static factories: `Stats.AddedStat(SP, AT, float value, byte specialTag)`, `IncreasedStat`, `MoreStat`, `QuotientStat`.
- `BaseStats.ChangeStatModifier(SP property, float changeValue, BaseStats.ModType modificationType, AT tags, byte specialTag, bool overrideValue)`, `BaseStats.NewStat(SP, AT, byte)`, `GetAttributeValue(CoreAttribute.Attribute) : int`, `UpdateStats()`.
- `enum BaseStats.ModType : int { ADDED = 0, INCREASED = 1, MORE = 2, QUOTIENT = 3 }`.
- `enum AT : int` flags begin `None = 0, Physical = 1, Lightning = 2, Cold = 4, Fire = 8, Void = 16, ...`.

### INFERRED

- `added`, `increased`, `more` are base values that the modifiers are applied on top of (the usual `(base + added) * (1 + increased) * more` shape). For a raw total pass `added = 0, increased = 0, more = 1`, `checkTags = AT.None`, `specialTag = 0`. Whether `more` is a multiplier (1) or an additive percent (0) is not visible; test both in the probe.
- This route returns the **raw sum of modifiers** from the stat list, **not** the capped value and not including conversions, buffs applied elsewhere or class/level base values. `GetProtectionValue` is probably the variant used for armor, dodge and resistances. Prefer the `ProtectionClass` fields for anything the character sheet shows.
- Stat list values for resistances are likely stored as fractions or percents depending on the modifier; the ProtectionClass field is the safer unit check (see section 5).

## 3. SP enum member names

### CONFIRMED (current, LE 1.4.x)

Seen as `SP.<name>` in RCInet C# or as strings matched against `SP.ToString()` in `AssetBundleExport/Assets/Headhunter/TextAsset/HH_Buffs.json` (loaded by `Mods/NewItems/Items_HeadHunter.cs`, `GetPropertyFromName`):
`Damage`, `AttackSpeed`, `CastSpeed`, `CriticalChance`, `CriticalMultiplier`, `Health`, `Mana`, `Movespeed`, `Armour`, `DodgeRating`, `HealthRegen`, `ManaRegen`, `BlockChance`, `HealthLeech`, `IncreasedCooldownRecoverySpeed`, `IncreasedDropRate`, `IncreasedExperience`, `IncreasedAreaForAreaSkills` (new since 2023), `FireResistance` (`Items_SandsOfSilk.cs` line 309), `LightningResistance` (`Items_EssentiaSanguis.cs` line 308), `Strength`, `Dexterity`, `Intelligence`, `Vitality`, `Attunement`, `None`.

Because `IncreasedAreaForAreaSkills` exists now and not in 2023, **the enum has grown. Do not hardcode numbers; use names.**

### CONFIRMED (dump 2023), `enum SP : byte`, with 2023 values

| Stat you want | SP member | 2023 value |
|---|---|---|
| Physical resistance | `PhysicalResistance` | 64 (0x40) |
| Fire resistance | `FireResistance` | 13 |
| Cold resistance | `ColdResistance` | 14 |
| Lightning resistance | `LightningResistance` | 15 |
| Necrotic resistance | `NecroticResistance` | 27 |
| Void resistance | `VoidResistance` | 26 |
| Poison resistance | `PoisonResistance` | 28 |
| All / elemental / pairs | `AllResistances` 30, `ElementalResistance` 52, `PhysicalAndVoidResistance` 106, `NecroticAndPoisonResistance` 107 | |
| Negative res modifiers | `NegativePhysicalResistance` 72, `NegativeFireResistance` 78, `NegativeColdResistance` 79, `NegativeLightningResistance` 80, `NegativeVoidResistance` 81, `NegativeNecroticResistance` 82, `NegativePoisonResistance` 83, `NegativeElementalResistance` 84 | |
| Armor | `Armour` (British spelling) | 10 |
| Negative armor | `NegativeArmour` | 77 |
| Dodge rating | `DodgeRating` | 11 |
| Block chance | `BlockChance` | 29 |
| Block effectiveness | `BlockEffectiveness` | 53 |
| Endurance | `Endurance` | 75 |
| Endurance threshold | `EnduranceThreshold` | 76 |
| Crit avoidance | `CritAvoidance` | 89 |
| Chance to be crit | `ChanceToBeCrit` | 112 |
| Max health | `Health` | 7 |
| Health regen | `HealthRegen` | 17 |
| Stun avoidance | `StunAvoidance` | 12 |
| Glancing blow | `GlancingBlowChance` | 62 |
| Ward | **no SP for ward amount.** Related: `WardRetention` 16, `WardRegen` 92, `WardGain` 39, `WardOnPotionUse` 91, `PotionHealthConvertedToWard` 90, `ManaBeforeWardPercent` 94, `ChanceToGain30WardWhenHit` 97 | |
| Other defensive | `DamageTaken` 6, `ManaBeforeHealthPercent` 24, `FreezeAvoidance` 68, `StunImmunity` 56, `Thorns` 85, `PercentReflect` 86, `MaximumHealthGainedAsEnduranceThreshold` 96, `DamageTakenBuff` 108, `IncreasedChanceToBeStunned` 109, `DamageTakenFromNearbyEnemies` 110, `BlockChanceAgainstDistantEnemies` 111, `DamageTakenWhileMoving` 113 | |

Full 2023 list (0 to 113): Damage, AilmentChance, AttackSpeed, CastSpeed, CriticalChance, CriticalMultiplier, DamageTaken, Health, Mana, Movespeed, Armour, DodgeRating, StunAvoidance, FireResistance, ColdResistance, LightningResistance, WardRetention, HealthRegen, ManaRegen, Strength, Vitality, Intelligence, Dexterity, Attunement, ManaBeforeHealthPercent, ChannelCost, VoidResistance, NecroticResistance, PoisonResistance, BlockChance, AllResistances, DamageTakenAsPhysical ... DamageTakenAsPoison (31 to 37), HealthGain, WardGain, ManaGain, AdaptiveSpellDamage, IncreasedAilmentDuration, IncreasedAilmentEffect, IncreasedHealing, IncreasedStunChance, AllAttributes, IncreasedPotionDropRate, PotionHealth, PotionSlots, HasteOnHitChance, HealthLeech, ElementalResistance, BlockEffectiveness, None (54), IncreasedStunImmunityDuration, StunImmunity, ManaDrain, AbilityProperty, Penetration, CurrentHealthDrain, MaximumCompanions, GlancingBlowChance, CullPercentFromPassives, PhysicalResistance, CullPercentFromWeapon, ManaCost, FreezeRateMultiplier, FreezeAvoidance, ManaEfficiency, IncreasedCooldownRecoverySpeed, ReceivedStunDuration, NegativePhysicalResistance, ChillRetaliationChance, SlowRetaliationChance, Endurance, EnduranceThreshold, NegativeArmour, NegativeFireResistance ... NegativeElementalResistance, Thorns, PercentReflect, ShockRetaliationChance, LevelOfSkills, CritAvoidance, PotionHealthConvertedToWard, WardOnPotionUse, WardRegen, OverkillLeech, ManaBeforeWardPercent, IncreasedStunDuration, MaximumHealthGainedAsEnduranceThreshold, ChanceToGain30WardWhenHit, PlayerProperty, ManaSpentGainedAsWard, AilmentConversion, PerceivedUnimportanceModifier, IncreasedLeechRate, MoreFreezeRatePerStackOfChill, IncreasedDropRate, IncreasedExperience, PhysicalAndVoidResistance, NecroticAndPoisonResistance, DamageTakenBuff, IncreasedChanceToBeStunned, DamageTakenFromNearbyEnemies, BlockChanceAgainstDistantEnemies, ChanceToBeCrit, DamageTakenWhileMoving.

Note: `None` is 54, not 0, and it is not the last member. Code like RCInet's `for (i < Enum.GetValues(typeof(SP)).Length) (SP)i` only works because values are contiguous.

Also CONFIRMED (dump 2023): `enum ResistanceStatType : int { NotResistance = 0, IndividualElementalResistance = 1, IndividualPhysicalOrVoidResistance = 2, CombinedResistance = 3, IndividualNecroticOrPoisonResistance = 4 }` and static `Tags.getResistanceStatType(SP property) : ResistanceStatType`.

## 4. Defensive values as ProtectionClass and BaseHealth fields

### CONFIRMED (dump 2023): `ProtectionClass` fields (all `float` unless noted)

| Want | Field |
|---|---|
| Physical res | `uncappedPhysicalResistance` |
| Fire res | `uncappedFireResistance` |
| Cold res | `uncappedColdResistance` |
| Lightning res | `uncappedLightningResistance` |
| Void res | `uncappedVoidResistance` |
| Necrotic res | `uncappedNecroticResistance` |
| Poison res | `uncappedPoisonResistance` |
| Armor | `armour` |
| Dodge rating | `dodgeRating` (plus `dodgeConversion`, enum `ProtectionClass.DodgeConversion { None = 0, Armour = 1, GlancingBlow = 2 }`) |
| Block chance | `blockChance` (also `blockChanceAgainstDistantEnemies`) |
| Block effectiveness | `blockProtection` (INFERRED to be the "block effectiveness" number; there is no field named `blockEffectiveness`) |
| Endurance | `endurance` |
| Endurance threshold | `enduranceThreshold` |
| Crit avoidance | `critAvoidance` (also `addedChanceToBeCrit`) |
| Ward | `currentWard` (property `CurrentWard`, CONFIRMED current in RCInet `Items_EssentiaSanguis.cs`), `wardRetention`, `wardRegen`, `wardRegenFromStats` |
| Stun avoidance | `stunAvoidance` (also `increasedChanceToBeStunned`) |
| Glancing blow | `glancingBlowChance` |
| Other | `fixedDamageReduction`, `untaggedDamageTakenModifier`, `damageTakenBuff`, `moreDamageTakenWhileMoving`, `moreDamageTakenFromNearbyEnemies`, `manaBeforeHealthPercent`, `manaBeforeWardPercent`, `thorns`, `percentReflect`, `int silverShrouds`, `penetration` (DamageStats.Penetration) |

### CONFIRMED (dump 2023): `ProtectionClass` methods giving derived (sheet) numbers

- `float armourForCharacterSheet()`
- `float dodgeRatingForCharacterSheet()`
- `float dodgeChance()` and `float CalculateDodgeChance(float dodgeRating, bool ignoreConversion)`
- `float blockMitigation(float blockEffectiveness)`
- `float mitigationFromArmour(float effectiveArmour, bool nonPhysical)`
- `float getGlancingBlowChance()`
- `float getPenetrationFromZoneLevel()`
- `ProtectionClass.DodgeConversion getDodgeConversion()`
- `float ApplyResistance(float damage, DamageType type, bool isBlock, float penetration)`

### BaseHealth (CONFIRMED in prior research, dump 2023 and current)

- Max health: `PlayerHealth.maxHealth` (int in 2023; read via `Convert.ToSingle`).
- Health regen: `baseHealthRegenPerSecond` and `addedHealthRegenPerSecond` fields on `BaseHealth` (dump 2023). INFERRED: the sheet value is their sum. Alternative: `Stats.GetStatValue(SP.HealthRegen, AT.None, 0, 0, 1, 0)` (raw).
- Current health: `currentHealth`.

### INFERRED

- These fields are written by `BaseStats.UpdateStats()` and read by `ProtectionClass.ApplyDamage`, so at the moment of death they hold the values used for the killing hit. They include buffs and shred only if the game folds those in at update time; ailment shred (e.g. `FireResShred`, `ArmourShred`) may instead be applied inside `ApplyResistance` per hit. To report shred, also list the player's `actor.ailmentReceiver` ailments at death (see `RESEARCH-game-api.md` section 5).
- Il2CppInterop exposes these private fields as C# properties of the same name on `Il2Cpp.ProtectionClass`, so `pc.uncappedFireResistance` compiles. If a field was renamed in 1.x, read it by reflection (`typeof(ProtectionClass).GetProperty(name)`) and log a miss instead of crashing.

## 5. The 75% cap and "max resistance"

### CONFIRMED (dump 2023)

- Resistances are stored as `uncapped*Resistance` on ProtectionClass. There is **no** field or SP member for "max resistance" or a resistance cap (no `MaxResistance`, `ResistanceCap`, `maxFireResistance` anywhere in the dump). `ProtectionClass` static fields contain only `delayedHitDamageNumberDelay`, so the cap is a literal inside `ApplyResistance` and `CharacterSheet.resistanceText(float res)`.

### CONFIRMED (public game documentation, not code)

- The resistance cap is a hard 75% and cannot be raised; resistance above 75% only absorbs shred, Shock and Marked for Death. Sources: https://steamcommunity.com/app/899770/discussions/0/6063574513308070211/ , https://forum.lastepoch.com/t/overhauling-defenses-in-last-epoch/25081 , https://sportskeeda.com/mmo/best-ways-cap-resistances-last-epoch . Penetration applies after the cap (see `RESEARCH-damage-and-defenses.md` section 2).

### INFERRED

- Effective sheet value = `Math.Min(uncappedXResistance, 75f)`; "capped" = `uncappedXResistance >= 75f`. Report both uncapped and effective.
- Units: the character sheet shows whole percents and `CharacterSheet.RoundToPercentString(float)` exists, which suggests the stored value may be either a whole number (75) or a fraction (0.75). Log the raw value once in the probe; if a capped character shows about 0.75, multiply by 100 and use 0.75 as the cap.
- Negative resistance (from `Negative*Resistance` stats or shred) is allowed; do not clamp at 0.

## Recommended read at death (INFERRED, from the confirmed names above)

```csharp
var actor = PlayerFinder.getPlayerActor();
var pc = actor?.gameObject.GetComponent<ProtectionClass>();  // or actor.protection
var hp = PlayerFinder.getLocalPlayerHealth();
// resistances (uncapped, then Math.Min(x, 75))
pc.uncappedPhysicalResistance, pc.uncappedFireResistance, pc.uncappedColdResistance,
pc.uncappedLightningResistance, pc.uncappedNecroticResistance, pc.uncappedVoidResistance,
pc.uncappedPoisonResistance
// layers
pc.armour (or pc.armourForCharacterSheet()), pc.dodgeRating (or pc.dodgeRatingForCharacterSheet(), pc.dodgeChance()),
pc.blockChance, pc.blockProtection, pc.endurance, pc.enduranceThreshold, pc.critAvoidance,
pc.CurrentWard, pc.stunAvoidance, pc.glancingBlowChance
// health
hp.maxHealth, hp.baseHealthRegenPerSecond + hp.addedHealthRegenPerSecond
// fallback for any missing field
PlayerFinder.getLocalPlayerStats().GetStatValue(SP.FireResistance, AT.None, 0f, 0f, 1f, 0)
```

Read in a `Dying.die()` prefix (or on the lethal `ProtectionClass.ApplyDamage` postfix) rather than after the death screen opens, because death may clear buffs and ward.

## Confidence

| Item | Confidence |
|---|---|
| `PlayerFinder.getPlayerActor()`, `GetComponent<ProtectionClass>()`, `PlayerFinder.getLocalPlayerStats() : Stats` | High (current source) |
| SP names `FireResistance`, `LightningResistance`, `Armour`, `DodgeRating`, `BlockChance`, `Health`, `HealthRegen` | High (current source and data) |
| SP names `PhysicalResistance`, `ColdResistance`, `NecroticResistance`, `VoidResistance`, `PoisonResistance`, `BlockEffectiveness`, `Endurance`, `EnduranceThreshold`, `CritAvoidance`, `StunAvoidance` | Medium high (2023 dump; same naming family as confirmed ones) |
| SP numeric values | Low for current build (enum grew) |
| ProtectionClass field names (`uncapped*Resistance`, `armour`, `dodgeRating`, `blockChance`, `blockProtection`, `endurance`, `enduranceThreshold`, `critAvoidance`, `stunAvoidance`) | Medium (2023 dump only; `CurrentWard` confirmed current) |
| `armourForCharacterSheet()`, `dodgeRatingForCharacterSheet()`, `dodgeChance()` | Medium (2023 dump only) |
| `Stats.GetStatValue(SP, AT, float, float, float, byte)` signature | Medium (2023 dump); meaning of `more` base value is unknown |
| 75% hard cap, no max res stat | High for game rule; Medium that no max res field was added in 1.x |
| Resistance units (75 vs 0.75) | Unknown, probe it |
