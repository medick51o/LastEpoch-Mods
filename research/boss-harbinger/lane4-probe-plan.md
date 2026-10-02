# Lane 4 probe plan

Read-only in-game probes for Codex. Do not change classification, storage, or player-facing copy while logging. Every line below is a candidate from `lane4-native.md`. A log confirms or refutes runtime behavior. It does not add a new metadata fact.

Log one record per death, plus the last living sample that `DeathTracker.SampleDefenses` already stores. Write it from the existing read points: `DeathReportHooks.Capture`, `DeathReportHooks.NetworkReportPostfix`, `DeathTracker.OnDeath` / `SampleDefenses`, `PlayerProbe`, and `PlayContextProbe`. New reads stay in a debug log. Do not write them into the saved death.

Stamp every record with UTC time, `Time.time`, character name, local `ActorSync` pointer, and whether the line is `living` or `callback`.

## Shared record

At the living sample and again at the callback, log:

- Character name, class, level, hardcore.
- `PlayerFinder.getLocalActorSync` pointer, `getPlayerActor` pointer, `livingPlayerInScene`.
- Raw `SceneManager` scene name, `SceneList.getSceneName`, `GetCurrentSceneDisplayName`, `GetSceneDisplayName(raw)`, and `SceneDetails` `scene`, `Name`, `DisplayName`, `LocalizedName`, `sceneType`, `Level`, `chapter`.
- `Localization.Locale`, `IsOriginalLocale`, `Localization.Scenes.GetLocationName(raw, fallback)` with a fixed fallback such as the raw scene name, and `TryGetText` only if a key was actually returned by another call. Do not invent a key.
- `ZoneInfoManager.ZoneLevel`, `DifficultyManager.instance.areaLevel`, `SceneDetails.Level`.
- `LoadingScreen.IsVisible`, `TransitionSceneManager.IsActive`, `TransitionSceneManager.SceneName`. Log `ClientSceneService.IsLoading`, `CurrentScene`, and `LoadingSceneNames` only after the instance is found. If it is not found, log `client scene service unresolved`.
- `CharacterData.IsOffline` and `_isOffline` (`HasValue` and `Value`). `CurrentMonolithRunTimelineID` and `PreviousMonolithEchoTimelineID` as ints.
- `MonolithZoneManager.instanceExistsAndInitialised`. If true: `isHarbingerFight`, `harbingerSpawned`, `harbinger.id`, `timelineBossesToKill`, `timelineBossesKilled`.
- `MonolithRunsManager` found or not. If found: `TryGetCurrentOrMostRecentlyRequestedRun` success, `timelineID` numeric value and enum name, `IsEmpowered`, `difficultyIndex`, `questCompletion`, `IsBossQuestEcho` for that quest index, `timeline.displayName`.
- `DungeonRunManager` from `PlayerFinder.getDungeonRunManagerInSingleplayerOrOnClient`: null manager, null `activeRun`, or `activeRun.dungeonID` plus `DungeonList.getDungeonName`. `SceneList.IsDungeon(raw)` and `GetCurrentArenaOrDungeonTier`.

At the callback only, also log the death arguments by index, without assuming argument 9 exists:

- Which method ran: `UpdateDeathInfo` or `ReceiveDetailedDeathInfo`.
- For the network method: callback instance pointer and local `ActorSync` pointer.
- Arguments 0 through 8: damage type names, crit, damage, overkill, localized ailment, localized ability, localized attacker, `IrregularDamageSourceID` name.
- Argument 9 present or absent. If present, the bool.
- Attacker pointer zero or not. If present: `actorData` pointer, `id`, `actorType` name, `isHarbinger`, `IsLoneBoss`, `isChampion`, `isOmen`, `isNemesis`, `GetRawActorName`, `GetLocalizedActorName`, `actorName`.
- `GameplayActor` pointer zero, Unity-null, or readable. If readable: `isBoss`, `isBossOrMiniboss`, `IsMinion`, `IsMinionFromAbility` for the killing `Ability`, `summoned` present, `hasCustomName`, `minionType`.
- Display information if present: `actorClass`, `displayName`, `baseDisplayName`, `GetLocalizedName`, `prefixIndex`, `suffixIndex`, `specialIndex`. `IsBoss` component present or absent on the gameplay actor.
- `Ability.abilityName`, `playerAbilityID`, and `GetLocalizedAbilityName`.
- `HarbingersData.Get()` null or not. If the run's `TimelineID` was read: `GetTimelineHarbinger(id).id` and `IsNoEmpoweredHarbingerTimeline`.
- `mostRecentDeathInfo` length, and whether this call is the one that ran `UpdateDeathInfo`.

## Probes

### P1. The two death signatures

Where: `DeathReportHooks.Install` and `Capture`.

Log: method name, parameter count, and whether argument 9 arrived.

Confirms F1 and F2 at runtime if the 10-argument instance method and the 9-argument static method both run, and only the 10-argument method has a bool at the end.

Refutes the install if either method is missing or the parameter count differs from 10 and 9.

### P2. Boss flag matrix

Where: callback record, one death in each situation.

Situations: a monolith timeline boss, a harbinger after that boss, a normal monster, an offline boss, a death whose only report is `UpdateDeathInfo`.

Log: `isBossfight` true, false, or absent, beside `actorType`, `isBoss`, `isBossOrMiniBoss`, `IsLoneBoss`, `isHarbinger`, `DisplayActorClass`, and `IsBoss` component.

Confirms F5, F19, and F20 when the actor flags can be true while argument 9 is absent, and when explicit false stays false on that blow.

Refutes a plan that treats a missing argument 9 as false, if the same actor flags are true on an offline or local-only report. Refutes using `IsBoss` absence as "not a boss" if a flagged boss has no component.

### P3. Actor id versus display name

Where: callback, on Lagon, Heorot, and one rare with a prefix.

Log: `ActorData.id`, raw name, localized actor name, display name, base display name, and `GetLocalizedAttackerName`.

Confirms F4 and F6 if the id stays stable across two deaths of the same enemy while a rare prefix changes only the display and the localized attacker string.

Refutes using the localized attacker string as an id if the prefix changes it, or if two different ids share one display string.

### P4. Harbinger phase after the parent

Where: living sample when the parent dies, living sample when the harbinger is up, and the callback of a death to each.

Log: killer `id`, `isHarbinger`, `actorType`, zone manager `isHarbingerFight`, `harbingerSpawned`, `harbinger.id`, `GetTimelineHarbinger(current TimelineID).id`, `timelineBossesKilled`, `timelineBossesToKill`.

Confirms F10 if, during the harbinger phase, the killer id equals both `harbinger.id` and `GetTimelineHarbinger` id, `isHarbinger` is true, and that id differs from the parent killer id logged earlier. `isHarbingerFight` true on that same window supports the phase flag.

Refutes the phase test if the harbinger's killer id differs from `GetTimelineHarbinger`, if the flag stays false for the whole harbinger fight, or if the flag stays true after the harbinger is gone.

### P5. Inherited ability

Where: callback on a harbinger blow whose ability name matches a parent ability, and on a parent blow with that ability.

Log: `abilityName`, `playerAbilityID`, ability object pointer if readable, killer `id`, `isHarbinger`.

Confirms F12 if the ability name or pointer matches and the actor ids differ, with `isHarbinger` true only on the harbinger.

Refutes identity-by-ability if a non-harbinger add shares that ability name. The ability name still must not be stored as the boss id.

### P6. Proxy, add, and minion

Where: callback on an add or summoned killer, including a name the catalog must not exact-match.

Log: `actorType`, `IsMinion`, `IsMinionFromAbility`, `Summoned.hasCustomName`, `minionType`, `primaryAbility.abilityName`, display indexes, killer id, parent id if the zone manager still has `harbinger` or the timeline boss id from P4.

Confirms F12 if the add's id differs from the boss id and the localized name is not an exact catalog alias. A missing creator on `Summoned` stays unresolved. That absence confirms the metadata gap. It does not identify the parent.

Refutes a creator-chain plan if a future read finds an owner the metadata pass missed. Record the declaring type and signature before using it.

### P7. Zone name and locale

Where: living sample in a campaign zone, a monolith echo, a dungeon, and a town, on the current locale and on one other locale if available.

Log: the shared scene and localization fields. Compare them with the on-screen area name.

Confirms F15 if `GetCurrentSceneDisplayName` or `LocalizedName` or `GetLocationName` equals the on-screen area name while the raw scene name does not.

Refutes a lookup if it returns empty, returns the raw scene, returns a key, or stays in English when the locale is not the original. Record which call failed. Unknown locale then stays unknown in the death record.

### P8. Loading and respawn

Where: callback immediately after death, and a second read 1 second later.

Log: all three loading flags, both scene strings, both zone levels, character name.

Confirms F17 and F26 if a loading flag is true while the callback scene or zone level has already changed, and the living sample still holds the fight scene.

Refutes keeping the callback scene if that read is the town or the respawn scene. The living sample is the one to keep only when its character, pointer, and age checks in P14 pass.

### P9. Three level fields

Where: one death in a monolith echo and one in a campaign zone. Living sample and callback.

Log: `ZoneLevel`, `areaLevel`, `SceneDetails.Level`, side by side.

Confirms F18 if they differ. The death record should keep the field the game uses for monster scaling once that is seen. Until then, keep them as three labeled numbers.

Refutes treating them as one value if any pair disagrees. Refutes dropping a zero `ZoneLevel` without logging it: the current probe stores only values greater than 0, so a real zero would look missing.

### P10. Timeline reach

Where: first monolith echo after selecting a timeline, then a later echo, then the town.

Log: how the `MonolithRunsManager` instance was found (component on `getPlayerActor`, or another object). `TryGetCurrentOrMostRecentlyRequestedRun`, `timelineID` name and number, `displayName`, saved `CurrentMonolithRunTimelineID`.

Confirms F7 and F8 if the enum name matches the timeline that was entered and `displayName` matches the on-screen timeline title. Confirms F9 if the saved int disagrees after leaving the echo.

Refutes a static-singleton plan if no component is found. Refutes mapping internal names such as `UndeadAbom` onto a display title if `displayName` is different. Leave those titles unset.

### P11. Dungeon id

Where: enter Lightless Arbor, Soulfire Bastion, and Temporal Sanctum if that dungeon is in the install. Also stand in town.

Log: `activeRun` null or `dungeonID` name and number, `getDungeonName`, raw scene, `IsDungeon`.

Confirms F13 if Lightless Arbor and Soulfire Bastion return those enum names. Temporal Sanctum confirms a mapping only if its `dungeonID` is logged. Dungeon1 stays an identifier until that happens.

Refutes equating Dungeon1 with Temporal Sanctum if Sanctum returns a different id, or if `activeRun` is non-null in town.

### P12. Offline pair

Where: one offline character and one online character, each with a boss death and a normal death.

Log: `IsOffline`, `_isOffline.HasValue`, `_isOffline.Value`, and which death method ran.

Confirms F19 if offline play has `HasValue` true and `Value` true, and the 10-argument method does not run. A true `isBoss` or `actorType` Boss on that death is local boss evidence while `isBossfight` stays absent.

Refutes reading a false `IsOffline` as online if `HasValue` is false. Refutes using `HarbingerFeatures.EnabledOffline` as the death's offline flag. Log that feature only if a tester confuses it with `CharacterData`.

### P13. Calamity and null attacker

Where: callback on an environmental or ground death if one can be produced without cheating items or other players.

Log: `IrregularDamageSourceID` name, attacker pointer, `isBossfight`.

Confirms F21 if the enum name is `Calamity` and the attacker pointer is zero. Do not attach a boss id.

Refutes a null-attacker assumption if Calamity still carries an actor. Record that actor's id and type. Still do not treat Calamity as a boss id.

### P14. Last living sample versus callback

Where: `SampleDefenses` and `OnDeath`, one death that respawns in town before the report.

Log: both timestamps, both character names, both raw scenes, both zone levels, both local pointers, sample age, health at the sample, loading flags.

Confirms F26 if the sample is at most 2 seconds old, the character matches, the sample scene is the fight, and the callback scene is the town. The stored zone must remain the sample.

Refutes the 2 second window if a correct fight scene is older than 2 seconds when the report arrives, or if a sample from the previous zone is still inside the window. Record the ages. Do not widen the window in this probe.

### P15. Late packet

Where: die, respawn, wait, and watch for a second report.

Log: report time, pending death time, scene at each, argument 9, attacker id. There is no sequence parameter to log (F3). If a field that looks like a death sequence appears at runtime, record its declaring type and signature and leave it unused until a second death shows it changes.

Confirms F3 and F24's delayed-packet case if a report more than 5 seconds later, or after the scene changed, does not overwrite the stored death. The existing matcher should refuse it.

Refutes the matcher if the late report adopts the new scene or a new character.

### P16. Party death

Where: two players. Kill the other player, then die locally.

Log: network callback instance pointer, local pointer, whether `Capture` ran, killer id.

Confirms F24 if the other player's network callback is dropped and the local death still records.

Refutes the filter if the other player's killer or scene is stored on the local character. Also log whether static `UpdateDeathInfo` runs for the other player. If it does, the 9-argument hook needs the same pointer or character check before it is trusted.

### P17. Character switch

Where: die on character A, then select character B before the report window ends.

Log: `playerChangedEvent` if a temporary subscription is added for the log only, living zone before and after, character name on the saved record.

Confirms F25 if B does not receive A's zone, and A's pending death is not rewritten with B's name.

Refutes the clear if A's scene remains as B's living zone.

### P18. Rapid deaths

Where: two deaths inside 5 seconds, if the game allows a second death that quickly after respawn.

Log: both pending timestamps, both attacker ids, which report applied to which death.

Confirms the existing rejection when one report could match two pending deaths: neither death should inherit the other's attacker.

Refutes that guard if one attacker is written onto both records.

### P19. Actor unload

Where: callback when the killer's body has already disappeared.

Log: `actorData` pointer and id, `GameplayActor` pointer and Unity-null, `IsDead`.

Confirms F23 if `actorData.id` is still readable when `GameplayActor` is null or Unity-null.

Refutes relying on `GameplayActor.isBoss` alone if that object is already gone while `actorData` still identifies the killer.

### P20. Ambush objective and name collisions

Where: an echo whose objective is an ambush, if one is entered during the session.

Log: killer `actorType`, `isBoss`, `isBossfight`, and any objective type that is already exposed on a live object. Do not parse the scene name for "boss".

Confirms F11 if an ambush add can carry a boss-like display name while `actorType` is not Boss and `isBossfight` is false or absent.

Refutes using `MonolithObjectiveType.Boss` or the ambush name as the killer id. Those values classify an echo, not the actor.

### P21. Aberroth and Aterroth

Where: only if the tester reaches a fight whose UI uses one of those names.

Log: killer id, raw name, localized name, and which of `AterrothDeath` or an Aberroth panel is present in the scene. Do not merge the spellings in the log.

Confirms F22 if the two names show different ids or different scenes.

Refutes a single catalog entry if both names appear for one id. Record that as one encounter with two spellings only after the ids match.

### P22. English alias versus locale

Where: P7's non-English session, on a boss whose English alias is in the catalog.

Log: `GetLocalizedAttackerName`, `GetLocalizedActorName`, `ActorData.id`, locale.

Confirms F22 if the id still matches the English-session id while the localized name is not the English alias. The catalog exact match will miss. The id is the stable candidate. The alias list would need a locale entry later. This probe does not add one.

Refutes id stability if the same on-screen boss returns a different id after the locale change.

### P23. Stale death text

Where: two deaths in a row. On the second, if only the network method runs, log `mostRecentDeathInfo`.

Confirms F2 and F27 if the static string still holds the previous death until `UpdateDeathInfo` runs, and the hook does not attach it to the network-only call.

Refutes the current guard if the previous sentence is stored on the new death.

### P24. Empty zone and death objects

Where: once per session, reflection only.

Log: whether `ZoneInformation` or `DeathInformation` has any instance field beyond the constructor, and whether `ProtectionClass` has `deathInformation` or `deathInfo`.

Confirms F15 and F27 if both reads are null. The current `PlayerProbe` death-text path then stays empty, which is expected.

Refutes the empty claim if a property appears at runtime that this metadata pass did not see. Record the signature.

### P25. Feature gate versus offline

Where: once, next to P12.

Log: `Features.get_Harbingers()` null or not, and the types of `EnabledOffline` and `EnabledOnline`. Do not log them as the death's offline state.

Confirms F19 if those members are feature objects and stay the same across an offline death and an online death.

Refutes that reading if they change with the death's offline state. They would still be gates, not actor identity.

## What this plan does not do

It does not pick a boss from a scene name or an ability name. It does not treat a missing `isBossfight` as false. It does not assign display titles to `TimelineID` names that this metadata pass did not verify. It does not equate `DungeonID.Dungeon1` with Temporal Sanctum. It does not read `HarbingerFeatures.EnabledOffline` as the death's offline flag. It does not write probe fields into the saved death.
