# Last Epoch game API research (for medick_DeathCounter)

Compiled 2026-10-01 from public sources only. No game DLL was opened.

Labels used below:

- **CONFIRMED (current)**: seen in C# source that compiles against MelonLoader's `Il2Cpp*` interop for a recent game build (2025 or 2026).
- **CONFIRMED (dump 2023)**: seen in a full Il2CppInspector dump of a pre 1.0 build. The name existed then; it may have been renamed since.
- **INFERRED**: a conclusion drawn from the above, not seen directly. Verify with the in game probe.

## Sources

1. **RCInet/LastEpoch_Mods** ("LastEpoch_Hud", author Ash). Commit `6054a5f`, 2026-04-15. Comments in the code reference LE 1.2, 1.3, 1.3.2 and 1.4, so it targets LE 1.4.x. Compiles against `Il2Cpp`, `Il2CppLE`, `Il2CppLE.Data`. Best current source.
   https://github.com/RCInet/LastEpoch_Mods (key files: `LastEpoch_Hud/Scripts/Refs_Manager.cs`, `Mods/UI/DamageMeter.cs`, `Mods/Character/Character_GodMode.cs`, `Mods/Character/Character_AutoPotions.cs`, `Mods/Summon/Summon_GodMode.cs`, `Mods/NewItems/Items_HeadHunter.cs`, `Mods/NewItems/Items_EssentiaSanguis.cs`, `Hud_Manager.cs`, `Mods/Bank/Bank_Quad.cs`)
2. **deafwave/le-pandora**. Commit `b2dbcf4`, 2025-04-21 (LE 1.2 era, inferred from date). MelonLoader net6, references `Il2CppLE.dll`.
   https://github.com/deafwave/le-pandora/blob/main/Services/DamageService.cs
3. **FallenStar08/Fallen_LE_Mods**. Commit `50138f3`, 2026-05-05 (current).
   https://github.com/FallenStar08/Fallen_LE_Mods (key files: `Shared/GameReferencesCache.cs`, `Dev/GameStatsTracker.cs`)
4. **medick51o/kg_LastEpoch_Improvements** (fork of war3i4i/LastEpochImprovements). Commit `b283fff`, 2026-04-11, mod version 1.4.6. Checked as requested: it touches only items, tooltips, filters, shrines, camera and map icons. It contains **no** health, damage, death, ailment or character data usage. Only relevant names: `ActorSync.ReceiveInitDisplayInformation(..., byte rarity)` and `PlayerActorSync` (`RaresOnMap.cs`).
   https://github.com/medick51o/kg_LastEpoch_Improvements
5. **ItsJustMeChris/Last-Epoch-Hud**. Commit `b0f79a1`, 2024-02-27 (LE 1.0 launch era).
   https://github.com/ItsJustMeChris/Last-Epoch-Hud (key files: `Mod/Cheats/ESP/Actors.cs`, `Mod/Cheats/AutoPotion.cs`)
6. **voideh/EpochTool**. Commit `66f1e21`, 2023-09-06. Contains a complete Il2CppInspector C++ dump (`appdata/il2cpp-types.h`, 60 MB, and `appdata/il2cpp-functions.h`, 269k lines), Unity 2019.4.21 to 2019.4.24, metadata v24.5, so a pre 1.0 (0.9.x) game build. This is the "dump" source for every field list and method signature below.
   https://github.com/voideh/EpochTool/tree/master/appdata
7. **moxjet200/Last-Epoch**. Commit `9e91d69`, 2018-07-01. Leaked/old alpha Unity project (ability scripts only). Used only to see the call chain `DamageStatsHolder.applyDamage -> ProtectionClass.ApplyDamage` and `BaseHealth.HealthDamage` usage.
   https://github.com/moxjet200/Last-Epoch/blob/master/Assets/Abilities/Components/DamageStatsHolder.cs
8. Cheat Engine table (themaoci/Game-Cheat-Tables-CE-, "Last Epoch Cheat Table.CT", unknown build): disassembly comments show calls to `BaseHealth.restoreHealth`, `ProtectionClass.GainWard`, `Actor.isDying`, `HealthPotion.UsePotion`.

Not found: no public `dump.cs` for LE 1.x, no repo named "LE_Tools" or "LastEpochTrainer" with source, no public mod that hooks player death. lastepochtools.com publishes item/skill data, not class names.

## 1. Player health component, current/max, hit method

### CONFIRMED (current)

- Class `Il2Cpp.PlayerHealth`. Get it with the static `PlayerFinder.getLocalPlayerHealth()` (RCInet `Refs_Manager.cs`), or `actor.gameObject.GetComponent<PlayerHealth>()` (le-pandora, ItsJustMeChris).
- Fields on `PlayerHealth` (inherited from `BaseHealth`): `currentHealth` (float), `maxHealth`, `damageable` (bool, writable), `canDie` (bool, writable). RCInet `Character_AutoPotions.cs`: `player_health.currentHealth / player_health.maxHealth * 100`. RCInet `Character_GodMode.cs` toggles `damageable` and `canDie`.
- Method `getHealthPercent()` on PlayerHealth (ItsJustMeChris `AutoPotion.cs`, returns 0..1).
- `Il2Cpp.BaseHealth` exists and `Il2Cpp.UnitHealth` derives from it: `unit_health.TryCast<BaseHealth>()` then `base_health.damageable` (RCInet `Summon_GodMode.cs`). `UnitHealth.currentHealth` (le-pandora). `UnitHealth.get_EffectiveHealthMultiplier` is patched in RCInet `MonsterScaling.cs`.
- Ward: `Il2Cpp.ProtectionClass` component on the player actor, property `CurrentWard` (float, get/set). RCInet `Items_EssentiaSanguis.cs`: `player_protection_class.CurrentWard += __0`.
- There is **no** class `Il2Cpp.Health` in any source.

### CONFIRMED (dump 2023): field layout

`BaseHealth : MonoBehaviour` fields, in order: `actor` (Actor), `maxHealth` (**int32**), `currentHealth` (float), `damageable`, `canDie`, `healable`, `useHealthCap`, `healthCap`, `baseHealthRegenPerSecond`, `addedHealthRegenPerSecond`, `alignmentManager`, `doNotTarget`, `damageTakenEvent` (delegate `BaseHealth.DamageTakenAction(float flatDamage, float damagePercent)`), `lethalDamageTakenEvent` (delegate `BaseHealth.LethalDamageTakenAction(float overkill)`), `healthChangeEvent` (delegate `BaseHealth.SimpleAction()`), `unimportanceModifier`, `unimportanceCoroutine`, `origonalUnimportance`, `oldHealthRatio`, `healthRatio` (byte), `tForm`.

`PlayerHealth : BaseHealth` adds: `healthBar`, `yellowBar` (UIBar), `yellowHealth`, `yellowMaxMove`, `playerLight`, `previousMaxHealth`, `previousHealth`, `playerZoneTransition`. Own methods: `Update()`, `isDamageable()`.

`UnitHealth : BaseHealth` adds `healthSerialisation`, `effectiveHealthModifier`, `startingHealthPercentage`, `protectionDamageTypes`, `relativeProtectionValues`.

Note: `maxHealth` was `int` in 2023. Read it as a generic number (Convert.ToSingle) in case it is float now.

### CONFIRMED (dump 2023): the health damage methods on BaseHealth

- `float HealthDamage(float damage)`: the final "subtract from health" method. Returns float.
- `float HealthDamageCull(float damage, float cullPercent, float flatCull)`
- `void HealthDamagePercent(float percentageOfHealth)`
- `void CurrentHealthPercentDamage(float percentageOfCurrentHealth, float minPercentThreshold)`
- Healing: `restoreHealth(float)`, `heal(float) : float`, `HealPercent(float)`, `SetHealthToFull()`
- Queries: `getHealthPercent()`, `getMissingHealthPercent()`, `missingHealth()`, `isDamageable()`, `isInjured()`, `onFullHealth()`, `GetActor()`
- Events (C# `event`, add_/remove_): `healthChangeEvent`. The `damageTakenEvent` and `lethalDamageTakenEvent` fields are delegates.

No method named `ReceiveDamage`, `TakeDamage`, `Damage`, `ApplyDamage` or `DamageHealth` exists on `BaseHealth` or `PlayerHealth` in the dump.

### CONFIRMED (dump 2023): ProtectionClass, the per hit entry point

`ProtectionClass` (component on every actor, `Actor.protection` field) is where hits are mitigated:

- `HitEvents ApplyDamage(DamageStats damageStats, DamageSource damageSource, float damageModifier, Actor attacker, bool delayDamageNumber)`
- `bool survivesDirectDamage(Actor attacker, float damage)`
- `float[] ConvertDamageTaken(DamageStats unconvertedDamage)`
- `float ApplyResistance(float damage, DamageType type, bool isBlock, float penetration)`
- `bool DodgedHit()`, `void GainWard(float wardGain)`, `CurrentWard` get/set
- Events: `damageEvent`, `hitDamageEvent` (`SimpleDamageAction(float damage)`), `detailedDamageEvent` (`DamageActionWithReturnInfo(float damage, HitEvents hitEvents, bool glancingBlow)`), `damagedByAttackerEvent` (`DamageActionWithAttacker(float damage, HitEvents hitEvents, Actor attacker)`), `damageNumberEvent`, `potentialStunEvent`, `potentialFreezeEvent`, `dodgeEvent`, `blockEvent`, `afterBlockEvent`.
- Field `deathInformation` of type `DeathInformation` (a MonoBehaviour whose only field is `string deathInfo`). Likely the "killed by" text. Worth probing.
- Field `damageArray` (`float[]`), plus `critChance`, `critMulti`, `currentWard`.

`DamageSource` is an interface/abstract with one virtual `getDamageSourceText()`.

### CONFIRMED (current): the attacker side

- le-pandora patches `DamageStatsHolder.applyDamage(Actor target)` (prefix + postfix) and reads `__instance.GetDamageSourceInfo()` as a tuple where `.Item2` is the source `Ability` (`abilityName`) and `.Item3` is the source `Actor`. It measures damage as `PlayerHealth.currentHealth` before minus after.
- Dump 2023 shows two overloads: `applyDamage(Actor actor)` and `applyDamage(ProtectionClass enemyProtection)`, and `getCreator() : Actor`, `getAbilityName() : string`, `isHit()`, `notDoT()`, `getDamageSourceText()`.

### INFERRED: call chain

`DamageStatsHolder.applyDamage(Actor)` (on the attacker's ability object) -> `ProtectionClass.ApplyDamage(DamageStats, DamageSource, float, Actor attacker, bool)` (on the target) -> `BaseHealth.HealthDamage(float)` (on the target). The 2018 source shows `enemyProtection.ApplyDamage(damageStats, gameObject)` inside `applyDamage`, and self damage components call `health.HealthDamage(...)` directly. Best hook for "player took a hit with attacker and types": **`ProtectionClass.ApplyDamage` prefix/postfix, filtered by `__instance.actor == PlayerFinder.getPlayerActor()`**. Best hook for "health actually went down by X" regardless of source: **`BaseHealth.HealthDamage` postfix** (PlayerHealth does not override it, so patch the BaseHealth method and filter by `__instance` being the player's health).

Multiplayer caveat (INFERRED): since 0.9 the game is client/server. Offline play runs the server logic in process, so these hooks fire. Online, the client copy receives health through `ActorSync` (`setHealthRatio`, `sendHealth`, `receiveDamageEventNonHit`, `receiveDotDamageNumberEvent`, `receiveDeath`), and the server side hooks may never run on your machine.

## 2. Damage over time and ailment ticks

### CONFIRMED (dump 2023)

- `AilmentReceiver` component (field `Actor.ailmentReceiver`) owns `List<AilmentReceiver.ActiveAilment> ailments`, plus `baseTickInterval`, `firstTickDelay`, `damageIndex`. Its `Update()` ticks them.
- `ActiveAilment` fields include `ailment` (Ailment), `damageStats` (DamageStats), `creator` (Actor), `ability` (Ability), `remainingDuration`, `totalDuration`, `stacksRepresented`, `firstTickApplied`.
- `ActiveAilment.getDamageSourceText()` exists, which means the ailment is a `DamageSource` passed into `ProtectionClass.ApplyDamage`.
- `DamageStatsHolder` has `highPriorityDoT` and `notDoT()`. `ProtectionClass` has `previousHighPriorityDotTime`. `ActorSync.sendDotDamageNumberEvent(float)` separates DoT numbers from hit numbers.
- `AbilityEventListener` events: `DetailedAbilityEvent` in current builds carries `hit` (false for DoT ticks). RCInet `DamageMeter.cs` treats `!hit && !crit` as a DoT tick.

### INFERRED

DoT ticks go through the same `ProtectionClass.ApplyDamage` (with `damageStats.isHit == false` and the `ActiveAilment` as `damageSource`) and then `BaseHealth.HealthDamage`. So one hook catches both; use `DamageStats.isHit` (or `HitEvents.Hit` flag in the return value) to tell them apart. Percent based effects may call `HealthDamagePercent` or `CurrentHealthPercentDamage` directly; hook those too if you want completeness.

## 3. Death

### CONFIRMED (current)

- `Il2CppLE.Data.CharacterData` (from `PlayerFinder.getPlayerData()`) has `int Deaths` and `bool Died` (read and written by RCInet `Hud_Manager.cs`), plus `bool Hardcore`, `bool Masochist`, `bool SoloChallenge`, `SaveData()`. **The game already counts deaths.** Polling `Deaths` for an increase is the cheapest reliable death signal (INFERRED that the game increments it on each death; RCInet only shows it is editable and displayed).
- `PlayerHealth.canDie` exists (setting it false makes god mode in RCInet).
- `Il2CppLE.UI.LoadingScreen.Disable()` and `OnBeforeSceneLoaded()` are patchable (RCInet, Fallen) and fire on respawn zone loads.

### CONFIRMED (dump 2023)

- `Dying` is a state component (field `Actor.dying`). Methods: `die()`, `onEnter()`, `onExit(State)`, `isDying(bool acceptFakeDying)`, static `isDying(GameObject)`, `isDying(Transform)`, `isDying(StateController)`. Events: `deathEvent` (`Dying.DeathAction(Dying dyingEntity)`), `afterDeathEvent` (`AfterDeathAction(Dying)`), `resurrectEvent` (`ResurrectionAction()`).
- `DeathScreen : MonoBehaviour` (the "You died" UI): `Awake`, `toggle()`, `RespawnPlayer()`, `Close()`, `ExitToCharSelect()`, `reset()`, `setHardcore(bool)`, `setArena()`, `unsetArena()`. Fields `normal`, `hardcore`, `arena` (GameObjects), `isHardcore`.
- `AnalyticsManager.PlayerDeath()` (static, no args). A clean "local player died" signal if it still exists.
- `ActorSync.sendDeath()`, `receiveDeath()`; `ActorVisuals.Die()` and `ActorVisuals.deathEvent`; `Actor.diedWithAilment(AilmentID)`; `AilmentReceiver.OnDeath(Dying)`; `CharacterMutator.OnActorDeath(Actor dead, bool unsummoned)`.
- `BaseHealth.lethalDamageTakenEvent` delegate `(float overkill)`.
- Cheat table (unknown build) shows a method `Actor.isDying`.

There is no `Die`, `OnDeath`, `Death`, `PlayerDied` or `HandleDeath` method on `BaseHealth`, `PlayerHealth` or `Actor` in the dump.

### INFERRED: best hooks

1. `Dying.die()` postfix, filter `__instance.gameObject == PlayerFinder.getPlayer()` (or `__instance.GetComponent<Actor>() == PlayerFinder.getPlayerActor()`).
2. `DeathScreen.toggle()` postfix (UI side, fires once when the death screen opens, works online too).
3. `AnalyticsManager.PlayerDeath()` postfix.
4. Poll `PlayerFinder.getPlayerData().Deaths` each second as the ground truth.

## 4. Damage types, per type damage, crits

### CONFIRMED (dump 2023; RCInet uses AT/SP enums in current builds)

- `enum DamageType : int { PHYSICAL = 0, FIRE = 1, COLD = 2, LIGHTNING = 3, NECROTIC = 4, VOID = 5, POISON = 6 }`. Same order is used by `DamageStatsHolder.setBaseDamage(physical, fire, cold, lightning, necrotic, void, poison, ...)`.
- `DamageStats` (plain class) fields: `float[] damage` (indexed by DamageType), `critChance`, `critMultiplier`, `isHit`, `cullPercent`, `flatCull`, `baseDamageTypeTags` (AT), `otherTags` (AT), `increasedStunChance`, `increasedStunDuration`, `additionalLeech`, `freezeRate`, `conditionalEffects`, `penetration`.
- `DamageStatsHolder : MonoBehaviour` fields: `damageStats` (DamageStats), `baseDamageStats`, `damageTags` (AT), `damageModifier`, `highPriorityDoT`, `alwaysShowDamageNumber`, `references`, `creatorEventListener`, `localEventsListener`.
- `[Flags] enum HitEvents : int { None = 0, Hit = 1, Crit = 2, Kill = 4, Freeze = 8, Stun = 16, Block = 32, MeleeHit = 64 }`. Returned by `ProtectionClass.ApplyDamage`. **This is the crit flag for hits on the player.**
- Ability tag enum is `AT` (e.g. `AT.None`, `AT.Lightning`), stat property enum is `SP` (RCInet, current).

### CONFIRMED (current)

- `DetailedAbilityEvent` fields: `target` (Actor), `ability` (Ability, `.abilityName`), `damageDealt` (float), `hit`, `crit`, `kill` (bools), `overkill` (float). Raised through `AbilityEventListener.DetailedAbilityEvent(DetailedAbilityEvent)` (RCInet `DamageMeter.cs`). RCInet filters out `target == player`, which implies events targeting the player do pass through it.
- `RelayDamageEvents.AddDamage(float additionalDamage)` (Fallen `GameStatsTracker.cs`).

## 5. Ailments, buffs

### CONFIRMED (dump 2023)

- `Ailment : ScriptableObject` fields: `id` (AilmentID), `duration`, `positive`, `displayName`, `instanceName`, `showsInBuffUI`, `icon`, `description`, `dealsDamage`, `baseDamage`, `tags`, `buffs`, `isCurse`, `blinds`, `roots`, ... Static `Ailment.getAilment(AilmentID)`, `getAilment(int)`. Instance `getName()`, `isPositive()`, `getMaxStacks()`.
- `enum AilmentID : byte` (values from 2023; newer builds append more): None 0, Ignite 1, Bleed 2, Chill 3, Possess 4, Shock 5, Slow 6, Poison 7, ArmourShred 8, TimeRot 9, FutureAttack 10, Laceration 11, AbyssalDecay 12, StackingAbyssalDecay 13, Blind 14, BlindingPoison 15, Frailty 16, MarkedForDeath 17, Plague 18, Ravage 19, Root 20, Fear 21, DamageBoost 22, Frostbite 23, SpreadingFlames 24, FireballStackForExplosion 25, VoidEssence 26, HolyAuraStackForFlamBurst 27, PoisonResShred 28, NecroticResShred 29, VoidResShred 30, Stagger 31, DivineEssence 32, Haste 33, Frenzy 34, Swiftness 35, PoisonStackForExplosion 36, AvalancheStackForFissure 37, TempestsMight 38, Damned 39, Pestilence 40, DisintegrateStackForExplosion 41, FireResShred 42, MeleeDefShred 43, Stalwart 44, Inspiration 45, DummyHealingWhileNotTakingDamage 46, Deadly 47, AspectOfTheBoarVisuals 48, AspectOfTheSharkVisuals 49, StackingAspectOfTheSharkVisuals 50, AspectOfTheViperVisuals 51, AspectOfTheLynxVisuals 52, ArcaneMark 53, Immobilized 54, SparkCharge 55, CriticalEffluence 56, ArcaneAscendance 57, BoneCurse 58, SpiritPlague 59, ShrineHaste 60, Contempt 61, ShrineReflect 62, ShrineStun 63, ShrineCrit 64, ShrineManatee 65, ShapeshifterVisuals 66, Ferocity 67, DoomBrand 68, NecroticBoneCurse 69, Apocalypse 70, Flurry1Tag 71, Flurry2Tag 72, PhysicalResShred 73, ColdResShred 74, LightningResShred 75, Enrage 76, LightningInfusion 77, EfficaciousToxin 78, ShadowDaggers 79, MirageForm 80, SilverShroud 81, DuskShroud 82, CrimsonShroud 83, SmokeBlades 84, CriticalVulnerability 85, Sharpshooter 86, ShrineExperienceBuff 87, AspectOfTheCrow 88, AncientFlight 89, Doom 90. Note: there is **no Freeze ailment**; freeze is a `HitEvents.Freeze` flag plus `ProtectionClass.potentialFreezeEvent` / `hasJustBeenFrozen`. Shred is `ArmourShred` and the `*ResShred` members. Numeric values may shift between builds: always use names (`Enum.GetName`), never hardcoded numbers.
- **How ailments are applied**: class `AilmentReceiver` (component, `Actor.ailmentReceiver`):
  - `ApplyAilment(Ailment ailment, Actor creator, Ability ability, Stats increases, Alignment alignment, float stacks, float increasedDuration, float increasedEffect)`
  - `ApplyAilmentWithDamageStats(Ailment ailment, Actor creator, Ability ability, DamageStats damageStats, Alignment alignment, float stacks, float increasedDuration, float increasedEffect)`
  - `ApplyStackOfAilment(Ailment, float increasedDuration, float increasedEffect)` and overload taking `AilmentID`; `ApplyStacksOfAilment(int stacks, AilmentID, float, float)`; `ApplyStackOfAilmentForDuration(Ailment, float duration, float increasedEffect)`
  - Queries: `hasAilment(AilmentID)`, `getStacks(AilmentID)`, `isCursed()`, `GetUniqueNegativeAilmentCount()`, `immuneTo(AilmentID)`
  - Removal: `removeAilment(ActiveAilment, bool)`, `cleanseAilment(...)`, `cleanseNegativeAilments()`
  - Hit reactions: `whenDamaged(float damage, HitEvents hitEvents, Actor attacker)`, `whenHit(float damage, Actor attacker)` (called after a hit lands; another usable "player was hit by attacker" hook).
- `Actor.hasAilment(AilmentID)`, `Actor.ailmentStacksChanged(AilmentID, int newStackNumber)` (fires on every stack change, good single hook), `Actor.diedWithAilment(AilmentID)`.
- Client UI side: `PlayerActorSync.receiveAilmentBuffUiInfo(byte id, float duration, byte stacks)`, `ActorSync.receiveAilmentStacksChanged(byte ailmentID, byte newStackNumber)`.
- Listing active ailments: iterate `actor.ailmentReceiver.ailments` (List of ActiveAilment), read `.ailment.id`, `.ailment.displayName`, `.stacksRepresented`, `.remainingDuration`, `.creator`.

### CONFIRMED (current): buffs

- `actor.statBuffs` (class `StatBuffs`), list `statBuffs.buffs` of `Buff` with `name`, `remainingDuration`, `stat.property` (SP), `stat.addedValue`, `stat.increasedValue`, `stat.moreValues`, `stat.tags` (RCInet `Items_HeadHunter.cs`). Methods `addBuff(float duration, SP property, float added, float increased, List<float> more, AT tags, [byte specialTag,] string name)`, `removeBuffsWithName(string)`.
- StatBuffs holds stat buffs, not ailments. **There is no ApplyAilment on StatBuffs** and no `AilmentData` class in any source.

## 6. Attacker display name

### CONFIRMED (current, LE 1.0)

- `actorVisuals.gameObject.GetComponent<ActorDisplayInformation>().displayName` (ItsJustMeChris `Actors.cs`). Also `ActorDisplayInformation.actorClass` (enum `DisplayActorClass`: Normal 0, Magic 1, Rare 2, Boss 3). Players: `ActorVisuals.isPlayer` and `ActorVisuals.UserIdentity.Username`. Fallback `actor.name` (GameObject name, often contains "(Clone)").

### CONFIRMED (dump 2023)

- `ActorDisplayInformation : BaseDisplayInformation`. `BaseDisplayInformation` fields: `displayName`, `description`, `indefiniteArticle`, `baseDisplayName`, `nonActor`, `actorClass` (property), `presetActorClass`. `ActorDisplayInformation` adds `baseDisplayNameSet`, `actorVisuals`, `actor`, `actorSync`. So `displayName` is already the composed, localized name with rare prefixes/suffixes; `baseDisplayName` is the plain monster name.
- On the server side the actor's display component may live on the visuals object, not the Actor. Try `actor.GetComponent<ActorDisplayInformation>()`, then `actor.GetComponentInChildren<ActorDisplayInformation>()`.
- `Ability.abilityName` (current, RCInet/le-pandora) names the skill that hit you. `DamageStatsHolder.getAbilityName()` and `getDamageSourceText()` (dump) also exist.
- Localization: `Il2Cpp.Localization.GetText(...)` and `TryGetText(...)` are patchable (RCInet `LocalizationOverride.cs`); not needed if you use `displayName`.

## 7. Character name, class, level

### CONFIRMED (current)

- `PlayerFinder` statics: `getPlayerActor() : Actor`, `getPlayerData() : Il2CppLE.Data.CharacterData`, `getPlayerDataTracker() : CharacterDataTracker`, `getLocalPlayerHealth() : PlayerHealth`, `getLocalPlayerStats() : Stats`, `getExperienceTracker() : ExperienceTracker`, `getLocalTreeData()`, `getLocalGoldTracker()`, `getGlobalDataTracker()`, `getPlayerVisuals() : ActorVisuals`, `getAncientBonesTracker()`.
- **The type is `Il2CppLE.Data.CharacterData`, not `Il2Cpp.CharacterData`.** Properties (PascalCase): `CharacterName` (string), `Level`, `CharacterClass` (int index), `ChosenMastery`, `Deaths`, `Died`, `Hardcore`, `Masochist`, `SoloChallenge`, `Cycle`, `SoulEmbers`, `LanternLuminance`, `MonolithRuns`, `SaveData()`.
- `CharacterDataTracker.charData` gives the same CharacterData.
- Class name: `CharacterClassList.instance.classes[data.CharacterClass].className` (RCInet `Hud_Manager.cs`, `Maxroll_import.cs`).
- Level: `ExperienceTracker.CurrentLevel` (property), `NextLevelExperience`; `actor.stats.level`; `CharacterData.Level`.

### CONFIRMED (dump 2023)

- In 2023 the class was `CharacterData1` (old name) with camelCase fields `characterName`, `level`, `deaths`, `died`, `hardcore`, `characterClass`, `chosenMastery`. Renamed since. `PlayerFinder.localPlayerLevel() : byte` also existed.

## 8. Hook patterns other modders used

All CONFIRMED in current source unless marked:

- `[HarmonyPatch(typeof(DamageStatsHolder), "applyDamage", new Type[] { typeof(Actor) })]` prefix + postfix, compare `PlayerHealth.currentHealth` before and after (le-pandora). This is the only public "damage taken" hook found.
- `[HarmonyPatch(typeof(AbilityEventListener), "DetailedAbilityEvent")] Postfix(DetailedAbilityEvent __0)` (RCInet damage meter, outgoing damage).
- `[HarmonyPatch(typeof(RelayDamageEvents), "AddDamage")] Postfix(RelayDamageEvents __instance, float additionalDamage)` (Fallen DPS).
- Polling in `Update` / `OnUpdate`: `PlayerHealth.currentHealth`, `getHealthPercent()` (RCInet AutoPotions, ItsJustMeChris AutoPotion).
- Reference caching reset on `[HarmonyPatch(typeof(Il2CppLE.UI.LoadingScreen), "Disable")]` (Fallen, RCInet). Player objects are recreated per zone, so re cache after each load.
- No public mod hooks player death. Nobody hooks `PlayerHealth` methods by name.

## Summary for the mod's candidate lists

- Health types: `Il2Cpp.PlayerHealth` (confirmed), `Il2Cpp.BaseHealth` (confirmed, base class, owns the methods), `Il2Cpp.UnitHealth` (monsters/minions). Drop `Il2Cpp.Health`.
- Current health: `currentHealth`. Max: `maxHealth` (was int). Flags: `damageable`, `canDie`. Method `getHealthPercent()`.
- Hit methods: on `BaseHealth`: `HealthDamage(float)`, `HealthDamageCull`, `HealthDamagePercent`, `CurrentHealthPercentDamage`. On `ProtectionClass`: `ApplyDamage(DamageStats, DamageSource, float, Actor, bool) : HitEvents`. On `DamageStatsHolder`: `applyDamage(Actor)`. On `AilmentReceiver`: `whenDamaged(float, HitEvents, Actor)`, `whenHit(float, Actor)`.
- Death: `Dying.die()`, `DeathScreen.toggle()`, `AnalyticsManager.PlayerDeath()`, `ActorSync.receiveDeath()`, `ActorVisuals.Die()`, plus polling `CharacterData.Deaths`.
- Ailments: `AilmentReceiver.ApplyAilment`, `ApplyAilmentWithDamageStats`, `ApplyStackOfAilment`, `ApplyStacksOfAilment`; `Actor.ailmentStacksChanged(AilmentID, int)`; enum `Il2Cpp.AilmentID`.
- Enums: `Il2Cpp.DamageType` (PHYSICAL..POISON, 0..6), `Il2Cpp.HitEvents` (flags), `Il2Cpp.AilmentID`.
- Names: `ActorDisplayInformation.displayName` / `baseDisplayName`; `Ability.abilityName`; `ProtectionClass.deathInformation.deathInfo`.
- Character: `PlayerFinder.getPlayerData()` returning `Il2CppLE.Data.CharacterData` (`CharacterName`, `Level`, `CharacterClass`, `Deaths`); `CharacterClassList.instance.classes[i].className`; `ExperienceTracker.CurrentLevel`.
