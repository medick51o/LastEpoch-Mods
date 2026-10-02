# CURRENT-WORK: MedicK's Terrible Deaths (medick_DeathCounter)
Read this first. Update after every step. Last update: 2026-10-02 (v0.1.1 player resolution, still no game on the machine).

## State right now
- v0.1.1 is a small runtime fix on top of v0.1.0. **Not run in game.** Do not treat the monolith death as fixed until a fresh log shows `player resolved via ...` (or one `player not resolved:` line) and one `death:` line with one new jsonl row.
- Resolution order: `getPlayerActor`, then `getLocalPlayerHealth` → `actor` → `actor.gameObject`, then the unproven names. Health is adopted even when that object is missing, and retried while it is null. `HasPlayer` also requires readable character data, so a menu preview does not raise the counter.
- The `CharacterData.Deaths` poll and the death-screen / analytics signals are not behind `HasPlayer`. A hooked death is still ignored while readable health is above 0. If health cannot be read, the hook waits and the counter has to move. Deaths are not stored under "Unknown Hero".
- v0.1.0 source is the base. **Never run in game before this pass either.** Four same-vendor (Claude) reviews ran on 2026-10-01: 12 findings, 5 on the repair, 6 on the Patterns tab, 8 on the defences snapshot; all fixed or noted. Same-vendor reads are not independent review.
- **Prebuilt for Saturday:** `release/medick_DeathCounter.dll` (110,592 B, sha256 bade7ead…3372; Patterns tab and defences snapshot included) and `release/medick_DeathCounter_v0.1.0.zip`. Built with `-p:NoGame=true`; the exact shipped file passes InteropGuard. Every CI run also uploads the DLL as an artifact.
- The mod references **no game assembly**: PlayerFinder, Actor, EpochInputManager and every health/damage/ailment member are reached by name (`Refl`). So the DLL can be built without the game: `-p:NoGame=true` (NuGet MelonLoader 0.7.2, HarmonyX 2.10.2, Il2CppInterop 1.5.1 = the versions ML 0.7.2 ships, UnityEngine.Modules 2021.3.33).
- Gates (cloud, 2026-10-01): CoreTests 36/36 (was 19/19 at first build); NoGame build 0 warnings / 0 errors; InteropGuard: 71 of 72 interop calls proven by shipped Terrible DLLs, the 72nd (`Application.OpenURL`) is fenced in its own try.
- `mockups/build.py` renders the panel from real Core output; see `mockups/death-panel.png`.
- InteropGuard already caught one real crash: `GUIContent.none` compiled as a FIELD against plain Unity; in game it is a property (`get_none`). Also replaced unproven `CalcHeight`/word wrap, `GUI.enabled`, `Event.shift`, `Color.Lerp`, `Rect.center`.
- Hook names now come from public Last Epoch mods (RCInet LastEpoch_Mods for LE 1.4, le-pandora, Fallen_LE_Mods) and a 2023 dump, see `docs/RESEARCH-game-api.md`. None of the first-draft guesses existed. Still NOT run in game: exact signatures may have drifted since the 2023 dump.
- Three death signals: hooks, health watch, and the game's own `CharacterData.Deaths` counter (the safety net if every hook is wrong).

## First in-hand launch (Andrew)
1. Either take the prebuilt `release/medick_DeathCounter.dll` (or the CI artifact), or `dotnet build medick_DeathCounter -c Release -p:DeployToMods=false`.
2. Copy the DLL to `Mods/`, launch once, quit. A load failure (`MissingMethodException`, `TypeLoadException`) names the member; send it. In `UserData/medick_DeathCounter.cfg` set `ProbeApi = true` and `DebugLog = true`.
3. Launch, load a character, take a few hits, die once (normal mode, low-level character).
4. Send `MelonLoader/Latest.log`. Look for:
   - startup line `hooks N hit / N death / N ailment`; `0 hit` means the damage hook names are wrong
   - `[probe] type Il2Cpp.…Health…` blocks: the real method and member names
   - `[probe] player hit via …`: what the damage call's arguments look like
   - `death: …` line: what got recorded
5. Then fix `GameHooks.Candidates`, `PlayerProbe` member names and `ArgReader` from the probe output (or try `HookOverrides` in the cfg first: no rebuild needed).

## Open questions for the probe
- **Defences scale:** are percent stats stored as 75 or 0.75? The `[probe] defences raw:` line answers it. Until then `DefenseSnapshot` only reports percents when the evidence is unambiguous; once known, hardcode the scale.
- Are `ProtectionClass.uncapped<Element>Resistance`, `armour`, `dodgeRating`, `blockChance`, `endurance`, `critAvoidance` still the field names (2023 dump)? Missing ones are simply left out of the snapshot.
- Does `ProtectionClass.ApplyDamage` still have the 2023 signature, and does Harmony accept the boxed `HitEvents` `__result` (log: `HitEvents postfix refused` in DebugLog means crit flags are lost, nothing else)?
- Is `ActorDisplayInformation` on the actor or a child (attacker names)?
- What does `ProtectionClass.deathInformation.deathInfo` actually say ("Game says" line)?
- Does `CharacterData.Deaths` increase at the moment of death or at respawn (the 60 s grace covers both)?
- Online play: server-side hooks may not fire on the client; the Deaths counter and death screen should still.
- `ActorVisuals.Die` may live on a child object and never match the player (harmless).

## Laws for this mod
- Reference no game assembly. Every Last Epoch type is reached by name through `Refl`.
- Every Unity/Il2CppInterop call must pass InteropGuard (proven by a shipped DLL) or be fenced in its own method inside a try and allow-listed in the workflow.
- Hook miss path stays cheap: damage hooks fire for every monster.
- A hooked death never counts while readable health is above 0.
- Never draw without a player (no ghost counter on menus).
- Frozen identifiers: assembly `medick_DeathCounter`, prefs category and entry names, cfg path `UserData/medick_DeathCounter.cfg`, log folder `UserData/medick_DeathCounter/`.
- Nothing copies into the game's Mods folder without Andrew (`-p:DeployToMods=false` on every agent build).
