# Lane 4: native identity, zone, and phase context

Inspection date: 2026-10-02. Assembly inspected: `Il2CppLE.dll` from the supplied zip entry dated 2026-10-02 00:09 (the brief calls that stamp 00:09 PT). SHA256 `2c44072c76a1ed7e31187f699bee42f6d2b651554d80c600595658f3a72538a6` matches the brief. File size 52,743,168 bytes. Metadata only: `System.Reflection.Metadata` and `PEReader`. The assembly was not loaded for execution, and no member was invoked.

Every finding below is metadata presence unless a sentence says otherwise. Runtime behavior is unverified. Scene names and ability names do not prove boss identity. A missing boss flag stays unknown.

## Assembly identity

| Field | Value |
| --- | --- |
| Assembly | Il2CppLE, version 0.0.0.0, culture neutral, no public key |
| Module | Il2CppLE.dll |
| Target framework attribute | `.NETCoreApp,Version=v6.0` (partially decoded attribute blob) |
| Original native image | `OriginalNameAttribute` values on the enums below name `LE.dll` |

The assembly version is the proxy version. It is not a Last Epoch or Season 5 patch number. Il2CppInterop method bodies are thunks. They do not contain the game's logic.

Referenced assemblies are not in the zip. Member bodies and nested members of those assemblies cannot be resolved. Types that appear only as signature tokens are still readable when an `Il2CppLE.dll` member names them. Notable unresolved references: `Il2Cppmscorlib`, `Il2CppInterop.Runtime`, `UnityEngine.CoreModule` (includes `UnityEngine.SceneManagement.Scene`), `Il2CppLE.Core`, `Il2CppLE.Addressables`, `Il2CppUniTask`, `Unity.Localization`, `Il2CppLE.Networking.Core`. The full reference list is in the source ledger. `Il2CppLE.Data.CharacterData` is defined in this DLL.

The user-string heap was not scanned. Display-name literals are not established as embedded in this DLL. Map contents (`timelineHarbingerMap`, actor lists, scene lists) are asset data and are not in the metadata.

## Reconfirmed death signatures

Both known signatures are present, one overload each. Parameter names and order match `medick_DeathCounter/docs/CURRENT-LOCAL-DEATH-API-v0.1.2.txt`.

**F1.** Instance `Il2Cpp.PlayerActorSync::ReceiveDetailedDeathInfo(Il2Cpp.DamageType primaryDamageType, Il2CppSystem.Nullable<Il2Cpp.DamageType> secondaryDamageType, System.Boolean wasCrit, System.Int32 damage, System.Int32 overkillDamage, Il2Cpp.Ailment ailment, Il2Cpp.Ability ability, Il2Cpp.ActorSync attacker, Il2Cpp.IrregularDamageSourceID irregularDamageSourceID, System.Boolean isBossfight)`. `Nullable<T>` resolves to assembly `Il2Cppmscorlib`. Custom attributes on the method are `CallerCountAttribute` and `CachedScanResultsAttribute` only. Neither attribute was fully decoded. Evidence class: local metadata. Runtime unverified.

**F2.** Static `Il2Cpp.DeathInformationText::UpdateDeathInfo` has the same first nine parameters and no `isBossfight`. The same nine parameters appear on static `ComposeLocalizedDeathInfo`, which returns `System.String`. Static `GetLocalizedAttackerName(ActorSync)`, `GetLocalizedAbilityName(Ability)`, and `GetLocalizedAilmentName(Ailment)` each return `System.String`. Static `get_mostRecentDeathInfo` returns `System.String`. Evidence class: local metadata. Runtime unverified.

**F3.** Neither death method has a sequence parameter. The `PlayerActorSync` method dump contains no method whose name contains `Sequence`, `DeathId`, or `deathId`. A death id may still exist under a name this pass did not search. Evidence class: local metadata. Runtime unverified.

## Findings

**F4. Stable actor-data id.** Instance `Il2Cpp.ActorData::get_id` returns `System.Int32`. Static `Il2Cpp.ActorDataList::TryGetData(System.Int32 id, Il2Cpp.ActorData& actorData)` returns `System.Boolean`. Static `get_actorByID` returns `Lazy<Dictionary<System.Int32, ActorData>>` (`Lazy` and `Dictionary` resolve to `Il2Cppmscorlib`). Static `Il2Cpp.ActorDataList::instance` returns `ActorDataList`. Static `Il2CppLE.AssetManagement.GlobalAssets::get_ActorDataList` returns `ActorDataList`, and static `get_ActorDataListAvailable` returns `System.Boolean`. This contradicts the earlier note in `medick_DeathCounter/research/advice/CODEX-ZONE-BOSS-RESEARCH.md` that no stable boss id was found. That note was a narrower pass. `ActorData.id` is present in this DLL. The integer's uniqueness and stability across patches are runtime and data questions, not settled by the signature. Evidence class: local metadata. Runtime unverified.

**F5. Boss, miniboss, and harbinger flags on the actor.** These members do not read `isBossfight`.

| Member | Kind | Returns |
| --- | --- | --- |
| `Il2Cpp.ActorData::get_actorType` | instance | `ActorData+Type` |
| `Il2Cpp.ActorData::isBossOrMiniBoss` | instance | `System.Boolean` |
| `Il2Cpp.ActorData::get_IsLoneBoss` | instance | `System.Boolean` |
| `Il2Cpp.ActorData::get_isHarbinger` | instance | `System.Boolean` |
| `Il2Cpp.ActorData::get_isChampion` | instance | `System.Boolean` |
| `Il2Cpp.ActorData::get_isOmen` | instance | `System.Boolean` |
| `Il2Cpp.ActorData::get_isNemesis` | instance | `System.Boolean` |
| `Il2Cpp.Actor::isBoss` | instance | `System.Boolean` |
| `Il2Cpp.Actor::isBossOrMiniboss` | instance | `System.Boolean` |
| `Il2Cpp.BaseDisplayInformation::get_actorClass` | instance | `DisplayActorClass` |

`ActorData+Type` is an `int` enum: Normal 0, MiniBoss 1, Boss 2, Minion 3, Special 4, Ally 5, FriendlyNeutral 6, Container 7. `DisplayActorClass` is a `byte` enum: Normal 0, Magic 1, Rare 2, Boss 3. `Il2Cpp.IsBoss` is a `UnityEngine.MonoBehaviour` with a constructor only. A present component is a candidate marker. A missing component leaves the actor unclassified. Evidence class: local metadata. Runtime unverified.

**F6. Name surfaces are separate members.** On `ActorData`, instance `get_actorName`, `GetActorName`, `GetRawActorName`, and `GetLocalizedActorName` each return `System.String`. Instance `get_actorNameArticle` and `GetActorNameArticle` return `ActorData+Article` (`int` enum: None 0, A 1, An 2, The 3). On `BaseDisplayInformation`, instance `get_displayName`, `get_baseDisplayName`, and `GetLocalizedName` return `System.String`. `ActorDisplayInformation` adds instance `get_actor`, `get_actorSync`, `get_prefixIndex`, `get_suffixIndex`, and `get_specialIndex`, plus `ReceivedInitialize(System.Byte displayRarity, System.Int32 prefixIndex, System.Int32 suffixIndex, SpecialNameEffect specialNameEffect)`. Static `DeathInformationText.GetLocalizedAttackerName(ActorSync)` is the string the current hook stores as the killer. Whether that string includes a rare prefix is native logic. The catalog tests already reject substring aliases such as an add name. An exact localized display name is still not `ActorData.id`. Evidence class: local metadata for the members; the prefix composition is a deduction and is runtime unverified.

**F7. Timeline id.** `Il2Cpp.TimelineID` is a `byte` enum. `OriginalName` is `LE.dll`, empty namespace, `TimelineID`. Members, and only these members: None 0, UndeadAbom 1, OsprixWithLance 2, VoidRahyeh 3, FrostLich 4, Lagon 5, UndeadVsVoid 6, Dragons 7, Gaspar 8, Heorot 9, Volcano 10, Activities 99. Lagon, Heorot, Gaspar, and VoidRahyeh are identifier names. They are not proof of the current localized timeline title. The other names are internal identifiers. This pass does not assign display titles to them. Whether a later timeline reuses one of these values is not in the enum. Evidence class: local metadata. Runtime unverified.

**F8. How a current monolith run could be reached.** `Il2Cpp.MonolithRunsManager` is a `MonoBehaviour`. It has instance `get_actor` returning `Actor`, and instance `TryGetCurrentOrMostRecentlyRequestedRun(Il2Cpp.MonolithRun& run)` returning `System.Boolean`. It also has instance `get_mostRecentlyRequestedTimelineID` and `get_previousEchoTimelineID`, both `TimelineID`, and instance `diedInEcho` with no parameters. No `get_instance` method exists on this type. The component has an actor property. Finding that component on the local player is a candidate reach path and is runtime unverified.

`Il2Cpp.MonolithRun` instance members: `get_timelineID` returns `TimelineID`; `get_timeline` returns `MonolithTimeline`; `get_IsEmpowered` returns `System.Boolean`; `get_difficultyIndex`, `get_questCompletion`, `get_questBranch`, and `get_depth` return `System.Int32`; `IsBossQuestEcho(System.Int32 questEchoIndex)` returns `System.Boolean`; `nextQuestEcho` returns `MonolithTimeline+QuestZone`. The constructor parameter list includes `TimelineID`. `MonolithTimeline` instance `get_timelineID` returns `TimelineID` and instance `get_displayName` returns `System.String`. Static `DifficultyIndexIsEmpowered(System.Int32)` returns `System.Boolean`. Instance `ContainsScene(System.String sceneName, System.Boolean includeArenaScenes, System.Boolean includeQuestScenes)` returns `System.Boolean`. Instance `getZoneFromIndex(System.Boolean, EchoWebIsland+IslandType, System.Int32)` returns `System.String`. `TimelineList` instance `get_timelines` returns `List<MonolithTimeline>`. Scene-name properties on that asset include `shadeSceneName`, `sanctuarySceneName`, `thresholdOfEternitySceneName`, `confluenceOfOblivionSceneName`, and `cemeteryDisplayName`. Their string values are not in the DLL. Static `GlobalAssets.get_TimelineList` and `get_TimelineListAvailable` are the asset accessors. Evidence class: local metadata. Runtime unverified.

**F9. Saved timeline ints are a different type.** Instance `Il2CppLE.Data.CharacterData::get_CurrentMonolithRunTimelineID` and `get_PreviousMonolithEchoTimelineID` return `System.Int32`, not `TimelineID`. They are saved character fields. They can disagree with a live `MonolithRun.timelineID`. Use them only as a provenance comparison against the live run. Evidence class: local metadata. Runtime unverified.

**F10. Harbinger phase, independent of the parent boss.** `Il2Cpp.MonolithZoneManager` has static `get_instance` returning `MonolithZoneManager` and static `get_instanceExistsAndInitialised` returning `System.Boolean`. Instance `get_isHarbingerFight` and `get_harbingerSpawned` return `System.Boolean`. Instance `get_harbinger` returns `ActorData`. Instance `SetUpHarbingerBossEncounter(MonolithRun)` returns void. Instance `get_harbingerBossSpawner` returns `Spawner`. Instance `get_timelineBossesToKill` and `get_timelineBossesKilled` return `System.Int32`. Instance `newTimelineBossSpawned(Actor actor)` and `TimelineBossDied(Dying dyingActorState)` return void. Instance `spawnBoss(System.Int32& bossId, System.Boolean& hasSecondBoss, System.Int32& secondBossId)` returns void. The native method-pointer name for `spawnBoss` includes `Private`. The managed proxy method is public. `bossId` is an integer argument. Equating it with `ActorData.id` is a hypothesis. Static `get_HARBINGER_SPAWNED_EVENT_ID` and `get_HARBINGER_DEATH_EVENT_ID` return `System.String`. The string values are not in the metadata.

`Il2Cpp.HarbingersData::Get` is static and returns `HarbingersData`. Static `GlobalAssets.get_HarbingersData` and `get_HarbingersDataAvailable` are the asset accessors. Instance `GetTimelineHarbinger(TimelineID id)` returns `ActorData`. Instance `get_timelineHarbingerMap` returns `Dictionary<TimelineID, ActorData>`. The dictionary contents are not in the DLL. Static `IsNoEmpoweredHarbingerTimeline(TimelineID id)` returns `System.Boolean`. Static `get_NoEmpoweredHarbingerTimelines` returns `TimelineID[]`. The array contents are not in the metadata.

A candidate phase test, all runtime unverified: while `instanceExistsAndInitialised` is true, read `isHarbingerFight`, `harbingerSpawned`, and `harbinger.id`. Compare the killer's `ActorData.id` with `harbinger.id` and with `GetTimelineHarbinger(run.timelineID).id`. A match on the harbinger id, with `isHarbinger` true on that `ActorData`, is the identity candidate for the harbinger phase. The parent boss remains a different `ActorData.id`. `timelineBossesKilled` versus `timelineBossesToKill` is a progress counter, not a name.

`Il2Cpp.MonolithRunsManager::StartQuestEcho(TimelineID timelineID, System.Int32 difficultyIndex, System.Int32 questEchoIndex, System.Boolean harbingerOptIn)` returns an operation result. The bool is an opt-in argument on a start method. It is not a live phase flag.

Outside a monolith, `instanceExistsAndInitialised` may be false. That outcome is runtime unverified. Evidence class: local metadata. Runtime unverified.

**F11. Harbinger words that are not a per-timeline harbinger id.** `Il2CppLE.Factions.HarbingerType` is an `int` enum. `OriginalName` is `LE.dll`, namespace `LE.Factions`, name `HarbingerType`. Members: Unknown 0, Brute 1, Agile 2. `Il2Cpp.WovenEchoType` is a `byte` enum. Relevant members: HarbingerEcho 29, HarbingerBossRush 30, UberAterroth 18. The enum continues through EndgameYrun 49. These are echo modes. `Il2Cpp.WovenEchoHarbingerEcho::get_harbinger` returns one `ActorData`. `Il2Cpp.WovenEchoHarbingerBossRush::get_harbingers` returns `List<ActorData>`, and `get_timelineIDsToDropLootFor` returns `List<TimelineID>`. `Il2Cpp.MonolithSceneType` (`int`: Normal 0, Arena 1, Shade 2, SanctuaryOfEterra 3, WovenEcho 4) has no Harbinger member. `Il2Cpp.MonolithObjectiveType` has no Harbinger member. It does have Boss 1 and AmbushDisguisedAsBoss 13. An ambush objective named like a boss is a collision risk if it is used as identity. Evidence class: local metadata. Runtime unverified.

**F12. Inherited abilities and proxy killers.** Instance `Il2Cpp.Ability::get_abilityName` and `get_playerAbilityID` return `System.String`. Static `DeathInformationText.GetLocalizedAbilityName(Ability)` returns the localized label. The same `Ability` reference can be delivered by a different actor. Compare the killer's `ActorData.id` and `isHarbinger` with the parent id. The ability name, localized or not, does not choose between them.

Proxy and minion members: instance `Actor::IsMinion`, `IsMinionOf(Actor)`, `IsMinionOfPlayer`, and `IsMinionFromAbility(Ability)` return `System.Boolean`. `ActorData+Type.Minion` is 3. `Il2Cpp.Summoned` (base `PreventsDrops`) has instance `get_actor` (`Actor`), `get_hasCustomName` (`Boolean`), `get_minionType` (`Summoned+MinionType`), `get_primaryAbility` (`Ability`), and `get_tracker` (`SummonTracker`). No creator, owner, or summoner property was found on `Summoned`. `Il2Cpp.ClientMinionCreationReferences::get_actorData` returns `ActorData`. That is a creation reference, not a proven owner chain. Rare display indexes are F6. Catalog exact-alias matching already rejects substring adds. Evidence class: local metadata. The creator chain beyond `IsMinionFromAbility` is unresolved. Runtime unverified.

**F13. Dungeon id.** `Il2Cpp.DungeonID` is a `byte` enum. `OriginalName` is `LE.dll`, `DungeonID`. Members: Dungeon1 0, LightlessArbor 1, SoulfireBastion 2. There is no TemporalSanctum member. Dungeon1 is an identifier. It is not established as Temporal Sanctum, and it is not player-facing copy. Reach path: static `Il2Cpp.PlayerFinder::getDungeonRunManagerInSingleplayerOrOnClient` returns `DungeonRunManager`. Instance `DungeonRunManager::get_activeRun` returns `Il2CppLE.Dungeons.DungeonRun`. Instance `DungeonRunManager::get_actor` returns `Actor`. Instance `DungeonRun::get_dungeonID` returns `DungeonID`. Static `Il2Cpp.DungeonList::getDungeon(DungeonID)` returns `DungeonList+Dungeon`. Static `getDungeonName(DungeonID)` returns `System.String`. Static `TryGetDungeon(System.String sceneName, DungeonList+Dungeon& dungeon)` and `isDungeonZone(System.String sceneName)` return `System.Boolean`. Static `GlobalAssets.get_DungeonList` and `get_DungeonListAvailable` are the asset accessors. Static `SceneList.IsDungeon(System.String)` returns `System.Boolean`. Static `SceneList.GetCurrentArenaOrDungeonTier` returns `System.Int32`. `Il2CppLE.Data.SavedDungeonRun::get_DungeonID` returns `System.Int32`, not the enum. A null `activeRun` outside a dungeon is runtime unverified. Evidence class: local metadata. Runtime unverified.

**F14. Quest id.** Instance `Il2Cpp.Quest::get_id` returns `System.Int32`. Instance `get_timelineID` returns `TimelineID`. Instance `get_displayName` returns `System.String`. `QuestType` is a `byte` enum: Normal 0, Monolith 1, Stash 2. It is not a quest catalog. Instance `PlayerActorSync::ReceiveStartQuest(System.UInt16 id)` returns void. Static `GlobalAssets.get_MasterQuestListAvailable` returns `System.Boolean`. The quest list asset's entries are not in this metadata. Evidence class: local metadata. Runtime unverified.

**F15. Localized zone and area name.** Candidate lookup, in order, while loading flags in F17 are false:

1. Static `Il2Cpp.SceneList::get` and `get_instance` return `SceneList`.
2. Static `getSceneName` with no parameters returns `System.String`. Static `GetCurrentSceneDisplayName` returns `System.String`. Static `GetCurrentSceneDetails` and `GetActiveSceneDetails` return `SceneDetails`. Static `GetSceneDisplayName(System.String sceneName)` returns `System.String`. Static `FindSceneDetails(System.String)` and `GetSceneDetails` overloads return `SceneDetails`.
3. On `SceneDetails`: instance `get_scene` and `get_Name` and `get_DisplayName` and `get_LocalizedName` return `System.String`. Instance `get_sceneType` returns `SceneType`. Instance `get_era` returns `SceneList+Era`. Instance `get_Level` returns `System.Int32`. Instance `get_chapter` returns `ZoneChapterManager+Chapter`. `SceneType` is an `int` enum: Normal 0, Monolith 1, Arena 2, Town 3, NonGameplay 4, Dungeon 5, Pinnacle 6.
4. Static `Il2Cpp.Localization::get_Scenes` returns `Il2CppLocalizationSystem.LocalizationScenes`. Instance `LocalizationScenes::GetLocationName(System.String scene, System.String fallback)` returns `System.String`. Static `Localization::GetText(System.String, System.String)` returns `System.String`. Static `TryGetText(System.String, System.String&)` returns `System.Boolean`. Static `get_Locale` returns `System.String`. Static `get_IsOriginalLocale` returns `System.Boolean`.

`SceneDetails.LocalizedName` is a method. Whether it returns a localized title, a key, a fallback, or empty text is native logic. `GetText` and `GetLocationName` both take an explicit fallback argument. Unknown-locale behavior is runtime unverified. `Il2Cpp.SceneNameTMP` has instance `OnActiveSceneChanged` and `get_tmp`. It is not a scene-to-area lookup. `Il2Cpp.ZoneInformation` is a `MonoBehaviour` with a constructor only, which agrees with the earlier zone research file. Evidence class: local metadata. Runtime unverified.

**F16. Scene and timeline mapping that does not prove a boss.** Static `SceneList.CurrentlyInMonolith`, `CurrentlyInCampaignZone`, `isMonolithZone(System.String)`, `IsMonolithEcho`, `IsMonolithQuestZone`, `IsMonolithRestZone`, `IsTownZone`, and `IsLoaded` classify scenes. Static `zoneCanHaveBossDamageReduction(System.String)` is a scene classifier. It is not a boss id. `MonolithTimeline.ContainsScene` maps a scene string onto a timeline asset. A scene string, including one that contains "Lagon" or a numeric suffix, does not identify the killer. The current mod stores the raw `SceneManager` scene name and a cleaned zone string separately (`PlayerProbe.RawSceneId` and `PlayerProbe.Zone`). `UnityEngine.SceneManagement.Scene` is in `UnityEngine.CoreModule`, which is not in the zip. Evidence class: local metadata for the classifiers; mod source for the two stored strings. Runtime unverified.

**F17. Loading state.** Static `Il2CppLE.UI.LoadingScreen::get_IsVisible` returns `System.Boolean`. Instance `OnBeforeSceneLoaded(System.String prevSceneName, System.String nextSceneName, LoadSceneMode loadMode)`, `OnSceneLoaded(System.String sceneName, LoadSceneMode loadMode)`, and `OnLocalPlayerInitialized(PlayerActorSync pas)` exist. Instance `get_sceneDetails` returns `SceneDetails`. Instance `get_isPlayerInitialized` returns `System.Boolean`. Instance `get_sceneNameText` and `get_eraNameText` exist. Their declared types are UI text components in assemblies that were not fully expanded here. Static `Il2Cpp.TransitionSceneManager::get_IsActive` returns `System.Boolean`. Static `get_SceneName` returns `System.String`. Instance `Il2Cpp.ClientSceneService::IsLoading` returns `System.Boolean`. Instance `get_CurrentScene` and `get_LoadingSceneNames` return `System.String`. Instance `OnBeforeSceneUnloaded(System.String obj)` and `OnActiveSceneChanged(Scene from, Scene to)` exist. How to obtain the `ClientSceneService` instance was not established. No `get_instance` was confirmed, because that dump was limited to load and scene methods. Static `Il2Cpp.ZoneInfoManager::OnSceneLoaded(Scene, LoadSceneMode)` and `SetZoneLevel(System.Int32 level, System.Boolean alwaysUpdateClient)` exist, so the zone level can change when a scene loads, including before a death report. If any loading flag is true, scene and zone reads are transition context. Evidence class: local metadata. Runtime unverified.

**F18. Three different level fields.** Static `Il2Cpp.ZoneInfoManager::get_ZoneLevel` returns `System.Int32` (the setter is also static). Static `get_ZoneLevelRatio` returns `System.Single`. Static `ApplyMonolithLevelScaling(MonolithRun)`, `ApplyDungeonLevelScaling(DungeonRun)`, and `ApplyArenaLevelScaling(System.Int32)` return void. Instance `Il2Cpp.DifficultyManager::get_areaLevel` returns a numeric area level. Static `DifficultyManager.get_instance` returns `DifficultyManager`. Instance `SceneDetails.get_Level` returns `System.Int32`. The current probe stores `ZoneInfoManager.ZoneLevel` only when the int is greater than 0, and it stores `DifficultyManager.areaLevel` inside the defense snapshot under a different key. The relationship among the three values is runtime unverified. `Il2CppLE.Networking.ZoneInfoNetworkManager` has static `SendZoneLevel(System.Int32)`, `SendZoneLevelToIndividual(System.Int32, ActorSync)`, and `ReceiveZoneLevel`. A delayed zone-level packet is possible in the signature set. Whether it arrives after death is runtime unverified. Evidence class: local metadata. Runtime unverified.

**F19. Explicit offline and local boss evidence.** Instance `Il2CppLE.Data.CharacterData::get_IsOffline` returns `System.Boolean`. Instance `get__isOffline` returns `Nullable<System.Boolean>`. The current `PlayContextProbe` reads only the non-nullable `IsOffline`. A false from that getter is not established as "online" when the nullable backing value has no value. Log both. Static `PlayerFinder.getPlayerData` returns `CharacterData`.

Local boss evidence that does not use the network `isBossfight` argument and does not use a substring: `Actor.isBoss`, `Actor.isBossOrMiniboss`, `ActorData.actorType` equal to Boss or MiniBoss, `IsLoneBoss`, `isHarbinger`, `DisplayActorClass` equal to Boss, a present `IsBoss` component, and `MonolithZoneManager.isHarbingerFight` with `harbinger.id`. Each can be true, false, or unreadable. Unreadable stays unknown. `Il2Cpp.Features::get_Harbingers` returns `HarbingerFeatures`. Instance `get_EnabledOffline` and `get_EnabledOnline` return `Il2Cpp.Feature`, which is a feature gate. They are not a classification of one death. Instance `get_EnabledSpawnHarbingers` and `get_EnabledPinnacleBoss` return `System.Boolean` and are also gates. Evidence class: local metadata. Runtime unverified.

**F20. The 9-argument report leaves the flag unknown.** `UpdateDeathInfo` cannot supply `isBossfight`. The current hook sets `IsBossFight` only when argument 9 is a bool (`DeathReportHooks.Capture`). `BossCatalog.Match` requires `IsBossFight == true` plus an exact alias. `AttackerGuide` returns null when the flag is explicit false, and it can still return a profile when the flag is null. `EncounterLabel` then says the encounter is unknown. That mod behavior matches the metadata split between the 10-argument and 9-argument methods. It is not a runtime observation of the game. Evidence class: local metadata for the signatures; deduction from v0.1.2 source for the hook.

**F21. Environmental damage.** `Il2Cpp.IrregularDamageSourceID` is a `byte` enum. `OriginalName` is `LE.dll`, `IrregularDamageSourceID`. Members: None 0, Calamity 1. It is argument 9 of both death methods (argument 10 of the network method is `isBossfight`). The attacker parameter is a reference type and can be a null argument. What the game passes for Calamity is runtime unverified. Calamity does not identify a boss. Evidence class: local metadata. Runtime unverified.

**F22. Localization, aliases, and collisions.** The v0.1.2 catalog aliases, from `BossCatalog` source, are exact English strings: Lagon, Emperor of Corpses, Heorot, Chronomancer Julra, Julra, Harbinger of Hatred. No locale table for those aliases is in this DLL. `LocalizationActors.GetName(System.String creatureName)` and `TryGetNameFromKey(System.String key, System.String& name)` exist. The keys themselves were not extracted. Unknown locale: callers of `GetText` and `GetLocationName` pass a fallback. Whether `SceneDetails.LocalizedName` returns the key, the fallback, or empty text is runtime unverified. Log `Localization.Locale` and `IsOriginalLocale` with every name.

Collisions to keep apart:

| Tokens | What each one is |
| --- | --- |
| `TimelineID.Lagon` (5), an actor whose names equal Lagon, a scene string containing Lagon | enum member, actor data, scene text |
| Catalog alias "Harbinger of Hatred", `ActorData.isHarbinger`, `HarbingerType`, `WovenEchoType.HarbingerEcho` | display alias, bool on one actor, brute/agile archetype, echo mode |
| `DisplayActorClass.Boss` (3), `ActorData+Type.Boss` (2), `isBossfight`, `IsBoss` component, `MonolithObjectiveType.Boss` (1), `AmbushDisguisedAsBoss` (13) | rarity display, actor type, death argument, marker component, echo objective, ambush objective |
| `Aberroth` types and `Aterroth` types | different spellings in this DLL |
| `baseDisplayName`, `displayName`, `GetRawActorName`, `GetLocalizedActorName`, `GetLocalizedAttackerName` | separate strings |

`Aberroth` types that exist: `Il2CppLE.UI.PanelSystem.AberrothEntrancePanel`, `Il2CppLE.Services.AberrothKillData`, `Il2CppLE.Services.AberrothKillAnnouncer`, and `CharacterData.get_AberrothKillTracking`. `Aterroth` types that exist: `Il2Cpp.AterrothDeath` with instance `OnAterrothDeath(Dying obj)`, `WovenEchoType.UberAterroth`, and `Il2CppLE.Factions.HarbingersProgress` members `get_AterrothUnlocked` and `get_DefeatedAterroths`. Do not collapse the spellings. Evidence class: local metadata for the tokens; mod source for the English aliases. Runtime unverified.

**F23. Actor unload and stale pointers.** Instance `ActorSync::get_actorData` returns `ActorData`. Instance `get_GameplayActor` returns `Actor`. Instance `get_IsDead` and `get_IsLocalPlayer` return `System.Boolean`. Instance `get_actorVisuals` returns `ActorVisuals`. Instance `get_Variant` returns `ActorVariant`. `actorData` can still be readable when `GameplayActor` is gone. That split is a signature possibility. It is runtime unverified. The network hook compares `PlayerFinder.getLocalActorSync` pointer with the callback instance and drops other instances (`DeathReportHooks.NetworkReportPostfix`). A cached dead `ActorSync` must not be reused as the local player. Evidence class: local metadata; deduction from v0.1.2 source for the pointer compare.

**F24. Party deaths.** `ReceiveDetailedDeathInfo` is an instance method, so each `PlayerActorSync` can receive it. Static `PlayerFinder.getAllPlayerActorsNonAlloc`, `iterateAllPlayers`, and `getLocalActorSync` exist. The current hook drops a network callback whose instance pointer is not the local `ActorSync`. A party member's death must not attach. Whether the 9-argument static `UpdateDeathInfo` runs for a remote party death is runtime unverified. Log both pointers. Evidence class: local metadata; deduction from v0.1.2 source for the filter.

**F25. Character switch and rapid deaths.** Static `PlayerFinder.get_playerChangedEvent` returns `PlayerFinder+PlayerChangedAction`. `DeathTracker` clears `_livingZone`, `_livingScene`, and `_livingZoneLevel` when the player object changes, and it replaces the character ledger when the character name changes. Two pending deaths that could match one report are rejected by the existing tracker. Those are mod behaviors, not observed game timing. Evidence class: deduction from v0.1.2 source. The event's firing on the character-select screen is runtime unverified.

**F26. Last living moment versus the death callback.** While health is above 0, `DeathTracker.SampleDefenses` stores zone, raw scene, zone level, class, level, hardcore, and play context. On death it keeps that sample when the character name matches and the sample age is at most 2 seconds. Otherwise it re-reads the probes. A game-counter detection with unreadable or still-positive health is treated as late and does not keep the living zone. Death details attach when the character matches and the report is within 5 seconds of the pending death. `mostRecentDeathInfo` is attached only when `UpdateDeathInfo` ran for that call. A network-only packet does not receive the previous static string.

At the callback, a fresh scene or zone read can already be a town or a respawn. The living sample is the candidate to keep when provenance matches. Provenance checks: character name equality, local `ActorSync` pointer equality, `PlayerFinder.livingPlayerInScene`, loading flags from F17, raw scene equality between the sample and the callback, and both offline members from F19. `CurrentMonolithRunTimelineID` alone is not provenance. There is no sequence id to pair the packet with the sample (F3). Evidence class: deduction from v0.1.2 source for the windows; local metadata for the members being sampled. Runtime unverified.

**F27. Empty death-text object.** `Il2Cpp.DeathInformation` is a separate type from `DeathInformationText`. Its managed surface is a constructor only. `PlayerProbe` still tries `protection.deathInformation.deathInfo`. `Il2Cpp.ProtectionClass` has no method whose name contains `death` or `Death`, and it has no non-generated managed field. The native field-pointer names were not enumerated, so an unsurtaced native field remains unchecked. The 2023-style `deathInfo` field is not on the managed proxy. Evidence class: local metadata. Runtime unverified.

**F28. Other encounter enums that must not be used as the boss id.** `EchoWebIsland+IslandType` (`int`): Normal 0 through SuperBeacon 11 (Arena, Shade, Beacon, VesselOfChaos, VesselOfMemory, Origin, SanctuaryOfEterra, Cemetery, SpecialWovenEcho, SuperArena, SuperBeacon). `EchoChainType` (`int`): Normal 0, Champions 1, BonusStability 2, Arena 3, Nemesis 4, OmenWindow 5, TimeBeast 6, OmenWindowChampions 7. `NemesisEncounterType` (`byte`): Lightning 0, Cold 1, Poison 2, Blood 3, Void 4. `WovenEchoContentType` (`int`): None 0, Boss 1, Craft 2. Instance `WovenEchoData.get_isBossEcho` returns `System.Boolean`. `WovenEchoArenaChampions+BossType` (`int`): Evil 0, HostileNeutral 1, Final 2. `ActorData+BossWardCondition` (`int`): Never 0, Always 1, CampaignOnly 2. `ActorData+RareStatus` (`int`): Never 0, Sometimes 1, Always 2. `ActorDataList` also exposes specific assets: `shadeOfOrobyssData`, `cemeteryBoss`, `gigaTombBoss`, `uberTombBoss`, `tombBosses`, and per-element nemesis data, plus static `TryGetNemesisData(NemesisEncounterType, ActorData&)`. `offlinePlayerActorData` is a distinct `ActorData` asset from `playerActorData`. It is not the session offline flag. Evidence class: local metadata. Runtime unverified.

**F29. Damage type enum, recorded so later lanes do not invent members.** `Il2Cpp.DamageType` is a `byte` enum. `OriginalName` is `LE.dll`, `DamageType`. Members: PHYSICAL 0, FIRE 1, COLD 2, LIGHTNING 3, NECROTIC 4, VOID 5, POISON 6. This is the type of the death-report arguments. It does not say which value a given boss uses. Evidence class: local metadata.

## Signature table

Assembly for every row: `Il2CppLE.dll`. Evidence class: local metadata. Runtime: unverified, including rows the current mod already calls. `Nullable<T>` is `Il2CppSystem.Nullable<T>` in `Il2Cppmscorlib`.

| Declaring type | Signature | Kind |
| --- | --- | --- |
| `Il2Cpp.PlayerActorSync` | `ReceiveDetailedDeathInfo(DamageType, Nullable<DamageType>, Boolean, Int32, Int32, Ailment, Ability, ActorSync, IrregularDamageSourceID, Boolean isBossfight)` | instance |
| `Il2Cpp.DeathInformationText` | `UpdateDeathInfo(DamageType, Nullable<DamageType>, Boolean, Int32, Int32, Ailment, Ability, ActorSync, IrregularDamageSourceID)` | static |
| `Il2Cpp.DeathInformationText` | `ComposeLocalizedDeathInfo(same 9) : String` | static |
| `Il2Cpp.DeathInformationText` | `GetLocalizedAttackerName(ActorSync) : String` | static |
| `Il2Cpp.DeathInformationText` | `GetLocalizedAbilityName(Ability) : String` | static |
| `Il2Cpp.DeathInformationText` | `GetLocalizedAilmentName(Ailment) : String` | static |
| `Il2Cpp.DeathInformationText` | `get_mostRecentDeathInfo() : String` | static |
| `Il2Cpp.ActorData` | `get_id() : Int32` | instance |
| `Il2Cpp.ActorData` | `get_actorType() : ActorData+Type` | instance |
| `Il2Cpp.ActorData` | `get_isHarbinger() : Boolean` | instance |
| `Il2Cpp.ActorData` | `get_IsLoneBoss() : Boolean` | instance |
| `Il2Cpp.ActorData` | `isBossOrMiniBoss() : Boolean` | instance |
| `Il2Cpp.ActorData` | `GetLocalizedActorName() : String` | instance |
| `Il2Cpp.ActorData` | `GetRawActorName() : String` | instance |
| `Il2Cpp.Actor` | `isBoss() : Boolean` | instance |
| `Il2Cpp.Actor` | `isBossOrMiniboss() : Boolean` | instance |
| `Il2Cpp.Actor` | `get_data() : ActorData` | instance |
| `Il2Cpp.Actor` | `get_summoned() : Summoned` | instance |
| `Il2Cpp.Actor` | `IsMinionFromAbility(Ability) : Boolean` | instance |
| `Il2Cpp.ActorSync` | `get_actorData() : ActorData` | instance |
| `Il2Cpp.ActorSync` | `get_GameplayActor() : Actor` | instance |
| `Il2Cpp.ActorSync` | `get_IsLocalPlayer() : Boolean` | instance |
| `Il2Cpp.ActorSync` | `get_IsDead() : Boolean` | instance |
| `Il2Cpp.PlayerFinder` | `getLocalActorSync() : ActorSync` | static |
| `Il2Cpp.PlayerFinder` | `getPlayerActor() : Actor` | static |
| `Il2Cpp.PlayerFinder` | `getPlayerData() : Il2CppLE.Data.CharacterData` | static |
| `Il2Cpp.PlayerFinder` | `getDungeonRunManagerInSingleplayerOrOnClient() : DungeonRunManager` | static |
| `Il2Cpp.PlayerFinder` | `livingPlayerInScene() : Boolean` | static |
| `Il2Cpp.PlayerFinder` | `get_playerChangedEvent() : PlayerChangedAction` | static |
| `Il2Cpp.ActorDataList` | `instance() : ActorDataList` | static |
| `Il2Cpp.ActorDataList` | `TryGetData(Int32 id, ActorData&) : Boolean` | static |
| `Il2Cpp.ActorDataList` | `get_actorByID() : Lazy<Dictionary<Int32, ActorData>>` | static |
| `Il2CppLE.AssetManagement.GlobalAssets` | `get_ActorDataList() : ActorDataList` | static |
| `Il2CppLE.AssetManagement.GlobalAssets` | `get_TimelineList() : TimelineList` | static |
| `Il2CppLE.AssetManagement.GlobalAssets` | `get_HarbingersData() : HarbingersData` | static |
| `Il2CppLE.AssetManagement.GlobalAssets` | `get_DungeonList() : DungeonList` | static |
| `Il2CppLE.AssetManagement.GlobalAssets` | `get_ActorDataListAvailable() : Boolean` and the matching `*Available` getters for the three lists above | static |
| `Il2Cpp.BaseDisplayInformation` | `get_actorClass() : DisplayActorClass` | instance |
| `Il2Cpp.BaseDisplayInformation` | `get_displayName() : String` | instance |
| `Il2Cpp.BaseDisplayInformation` | `get_baseDisplayName() : String` | instance |
| `Il2Cpp.BaseDisplayInformation` | `GetLocalizedName() : String` | instance |
| `Il2Cpp.ActorDisplayInformation` | `get_actorSync() : ActorSync` | instance |
| `Il2Cpp.ActorDisplayInformation` | `get_prefixIndex() : Int32`, `get_suffixIndex() : Int32`, `get_specialIndex() : Int32` | instance |
| `Il2Cpp.MonolithZoneManager` | `get_instance() : MonolithZoneManager` | static |
| `Il2Cpp.MonolithZoneManager` | `get_instanceExistsAndInitialised() : Boolean` | static |
| `Il2Cpp.MonolithZoneManager` | `get_isHarbingerFight() : Boolean` | instance |
| `Il2Cpp.MonolithZoneManager` | `get_harbingerSpawned() : Boolean` | instance |
| `Il2Cpp.MonolithZoneManager` | `get_harbinger() : ActorData` | instance |
| `Il2Cpp.MonolithZoneManager` | `spawnBoss(Int32& bossId, Boolean& hasSecondBoss, Int32& secondBossId)` | instance, proxy public |
| `Il2Cpp.HarbingersData` | `Get() : HarbingersData` | static |
| `Il2Cpp.HarbingersData` | `GetTimelineHarbinger(TimelineID id) : ActorData` | instance |
| `Il2Cpp.HarbingersData` | `IsNoEmpoweredHarbingerTimeline(TimelineID) : Boolean` | static |
| `Il2Cpp.MonolithRunsManager` | `TryGetCurrentOrMostRecentlyRequestedRun(MonolithRun& run) : Boolean` | instance |
| `Il2Cpp.MonolithRunsManager` | `get_actor() : Actor` | instance |
| `Il2Cpp.MonolithRun` | `get_timelineID() : TimelineID` | instance |
| `Il2Cpp.MonolithRun` | `get_IsEmpowered() : Boolean` | instance |
| `Il2Cpp.MonolithRun` | `IsBossQuestEcho(Int32 questEchoIndex) : Boolean` | instance |
| `Il2Cpp.MonolithTimeline` | `get_displayName() : String` | instance |
| `Il2Cpp.MonolithTimeline` | `ContainsScene(String sceneName, Boolean includeArenaScenes, Boolean includeQuestScenes) : Boolean` | instance |
| `Il2Cpp.SceneList` | `GetCurrentSceneDisplayName() : String` | static |
| `Il2Cpp.SceneList` | `GetCurrentSceneDetails() : SceneDetails` | static |
| `Il2Cpp.SceneList` | `GetSceneDisplayName(String sceneName) : String` | static |
| `Il2Cpp.SceneList` | `IsDungeon(String) : Boolean` | static |
| `Il2Cpp.SceneList` | `CurrentlyInMonolith() : Boolean` | static |
| `Il2Cpp.SceneDetails` | `get_scene() : String` | instance |
| `Il2Cpp.SceneDetails` | `get_Name() : String` | instance |
| `Il2Cpp.SceneDetails` | `get_DisplayName() : String` | instance |
| `Il2Cpp.SceneDetails` | `get_LocalizedName() : String` | instance |
| `Il2Cpp.SceneDetails` | `get_sceneType() : SceneType` | instance |
| `Il2Cpp.SceneDetails` | `get_Level() : Int32` | instance |
| `Il2Cpp.Localization` | `GetText(String, String) : String` | static |
| `Il2Cpp.Localization` | `TryGetText(String, String&) : Boolean` | static |
| `Il2Cpp.Localization` | `get_Locale() : String` | static |
| `Il2Cpp.Localization` | `get_IsOriginalLocale() : Boolean` | static |
| `Il2Cpp.Localization` | `get_Scenes() : LocalizationScenes` | static |
| `Il2CppLocalizationSystem.LocalizationScenes` | `GetLocationName(String scene, String fallback) : String` | instance |
| `Il2CppLocalizationSystem.LocalizationActors` | `GetName(String creatureName) : String` | instance |
| `Il2Cpp.ZoneInfoManager` | `get_ZoneLevel() : Int32` | static |
| `Il2Cpp.ZoneInfoManager` | `OnSceneLoaded(Scene, LoadSceneMode)` | static |
| `Il2Cpp.DifficultyManager` | `get_instance() : DifficultyManager` | static |
| `Il2Cpp.DifficultyManager` | `get_areaLevel()` | instance |
| `Il2CppLE.UI.LoadingScreen` | `get_IsVisible() : Boolean` | static |
| `Il2Cpp.TransitionSceneManager` | `get_IsActive() : Boolean` | static |
| `Il2Cpp.TransitionSceneManager` | `get_SceneName() : String` | static |
| `Il2Cpp.ClientSceneService` | `IsLoading() : Boolean` | instance |
| `Il2Cpp.ClientSceneService` | `get_CurrentScene() : String` | instance |
| `Il2Cpp.DungeonRunManager` | `get_activeRun() : DungeonRun` | instance |
| `Il2CppLE.Dungeons.DungeonRun` | `get_dungeonID() : DungeonID` | instance |
| `Il2Cpp.DungeonList` | `getDungeonName(DungeonID) : String` | static |
| `Il2Cpp.DungeonList` | `isDungeonZone(String sceneName) : Boolean` | static |
| `Il2CppLE.Data.CharacterData` | `get_IsOffline() : Boolean` | instance |
| `Il2CppLE.Data.CharacterData` | `get__isOffline() : Nullable<Boolean>` | instance |
| `Il2CppLE.Data.CharacterData` | `get_CurrentMonolithRunTimelineID() : Int32` | instance |
| `Il2Cpp.Quest` | `get_id() : Int32` | instance |
| `Il2Cpp.WovenEchoHarbingerEcho` | `get_harbinger() : ActorData` | instance |
| `Il2Cpp.IrregularDamageSourceID` | enum byte: None 0, Calamity 1 | enum |
| `Il2Cpp.TimelineID` | enum byte: None 0 through Volcano 10, Activities 99 | enum |
| `Il2Cpp.DungeonID` | enum byte: Dungeon1 0, LightlessArbor 1, SoulfireBastion 2 | enum |

## Reach path for the local player's current encounter

All steps are candidates. None is observed runtime behavior.

1. Local player: `PlayerFinder.getPlayerActor`, `getLocalActorSync`, and `getPlayerData`. Confirm the character name before trusting any saved id.
2. Killer: death-report argument `attacker` (`ActorSync`). Read `actorData.id`, `actorType`, `isHarbinger`, `IsLoneBoss`, `Actor.isBoss` on `GameplayActor` when that object is readable, display and raw and localized names, `Ability.abilityName`, `GetLocalizedAbilityName`, and `IrregularDamageSourceID`.
3. Harbinger phase: `MonolithZoneManager` only when `instanceExistsAndInitialised` is true. Compare killer id with `harbinger.id` and with `HarbingersData.Get().GetTimelineHarbinger(run.timelineID).id`.
4. Timeline: find the `MonolithRunsManager` component. There is no static instance. Call `TryGetCurrentOrMostRecentlyRequestedRun`. Read `timelineID`, `IsEmpowered`, `IsBossQuestEcho`, and `timeline.displayName`. Compare `CharacterData.CurrentMonolithRunTimelineID` only as a saved int.
5. Zone name: while loading flags are false, read `SceneList.GetCurrentSceneDisplayName`, `GetCurrentSceneDetails` (`Name`, `scene`, `DisplayName`, `LocalizedName`, `sceneType`, `Level`), the raw active scene name, `ZoneInfoManager.ZoneLevel`, and `DifficultyManager.areaLevel`. Also read `Localization.Scenes.GetLocationName(rawScene, fallback)`, `Locale`, and `IsOriginalLocale`.
6. Dungeon: `getDungeonRunManagerInSingleplayerOrOnClient().activeRun.dungeonID`, then `DungeonList.getDungeonName`, `SceneList.IsDungeon(rawScene)`, and `GetCurrentArenaOrDungeonTier`.
7. Offline: read `IsOffline` and `_isOffline` together. Local boss evidence comes from the actor flags even when `isBossfight` is absent.

## Signatures and behaviors that still need runtime verification

Metadata presence is the whole of this file's game evidence. The probe plan names the log that confirms or refutes each one. The list:

- `isBoss`, `isBossOrMiniboss`, `isBossOrMiniBoss`, `actorType`, `IsLoneBoss`, `isHarbinger`, `DisplayActorClass`, and a present or missing `IsBoss` component, against `isBossfight` true, false, and absent.
- `GetTimelineHarbinger` map contents, and whether the killer id matches `harbinger.id` after the parent dies.
- Inherited ability: same `Ability` with a different `ActorData.id`.
- Minion creator. No owner property was found on `Summoned`.
- `GameplayActor` null after unload while `actorData` remains.
- `GetCurrentSceneDisplayName`, `LocalizedName`, `GetLocationName`, and `GetText` fallback, including a non-English locale.
- `ZoneInfoManager.ZoneLevel`, `DifficultyManager.areaLevel`, and `SceneDetails.Level` on the same death.
- Loading flags during the death callback.
- How to find `MonolithRunsManager` and `ClientSceneService`.
- `activeRun` null outside a dungeon.
- `IsOffline` versus nullable `_isOffline`.
- Staleness of `CurrentMonolithRunTimelineID`.
- `IrregularDamageSourceID.Calamity` with a null attacker.
- Party pointer filter, rapid deaths, character switch, and a late packet after respawn.
- Absence of a sequence id that could pair those events.
- `ZoneInformation` and `DeathInformation` remaining empty at runtime, and `ProtectionClass` having no `deathInformation` property.
- Whether `TimelineID` and `DungeonID` numbers match current display names. Dungeon1 is not established as Temporal Sanctum.
- "Harbinger of Hatred" versus `isHarbinger`.
- Aberroth versus Aterroth as separate encounters.
- `HarbingerFeatures.EnabledOffline` as a feature gate, not a death classification.

## Source ledger

| Source | Date | Evidence class | What it supports |
| --- | --- | --- | --- |
| `Il2CppLE.dll` in the supplied zip, SHA256 above, entry timestamp 2026-10-02 00:09 | Inspected 2026-10-02 | local metadata | Every signature, enum value, and "member absent" claim in this file |
| `OriginalNameAttribute` on the enums | Same DLL | local metadata | Native names live in `LE.dll`. This pass did not open `LE.dll` |
| `medick_DeathCounter/docs/CURRENT-LOCAL-DEATH-API-v0.1.2.txt` | v0.1.2 dump already in the tree | local metadata, same signatures re-read here | F1 and F2 match that dump |
| `medick_DeathCounter/src/Game/DeathReportHooks.cs`, `DeathTracker.cs`, `PlayerProbe.cs`, `PlayContextProbe.cs` | v0.1.2 source on `cursor/boss-research-base` | deduction from mod source, not a runtime observation | Current hook indexes, 2 second living sample, 5 second detail window, pointer filter, `IsOffline` only, `deathInformation.deathInfo` read |
| `medick_DeathCounter/src/Core/BossCatalog.cs` | same tree | deduction from mod source | English exact aliases and the null versus false flag split |
| `medick_DeathCounter/research/advice/CODEX-ZONE-BOSS-RESEARCH.md` | earlier advice pass | local metadata of a narrower scan, now in conflict | Claimed no stable boss id. F4 supersedes that claim for this DLL. Its empty `ZoneInformation` note still matches F15 |

Unresolved assemblies, version as referenced by this DLL: `Il2Cppmscorlib` 4.0.0.0, `Il2CppInterop.Runtime` 0.0.0.0, `UnityEngine.CoreModule` 0.0.0.0, `Il2CppLE.Core` 0.0.0.0, `UnityEngine.PhysicsModule`, `Il2CppLE.Addressables`, `Il2CppUniTask`, `Unity.Localization`, `Il2CppLE.Networking.Core`, `Unity.TextMeshPro`, `Il2CppSystem`, plus the rest of the 75 assembly references recorded from the manifest. Their types cannot be expanded here.
