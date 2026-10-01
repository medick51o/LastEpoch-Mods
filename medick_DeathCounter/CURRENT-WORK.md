# CURRENT-WORK: MedicK's Terrible Deaths (medick_DeathCounter)
Read this first. Update after every step. Last update: 2026-10-01 (first build, cloud session, no game on the machine).

## State right now
- v0.1.0 source complete. **Never run in game.**
- Proven here: `tests/CoreTests` 19/19 pass; `tests/CompileCheck` builds 0 warnings / 0 errors against NuGet MelonLoader 0.7.2, HarmonyX 2.10.2, Il2CppInterop 1.4.5, UnityEngine.Modules 2021.3.33 and `GameStubs.cs`.
- NOT proven: every name in `GameHooks.Candidates` and `PlayerProbe` (health types, member names, damage/death/ailment methods, CharacterData). They are guesses from general Last Epoch modding knowledge.
- The real csproj has never built against the real `Il2CppLE.dll` either.

## First in-hand launch (Andrew)
1. `dotnet build medick_DeathCounter -c Release -p:DeployToMods=false`. If it fails, the error names the API that differs from the stubs; send it.
2. Copy the DLL to `Mods/`, launch once, quit. In `UserData/medick_DeathCounter.cfg` set `ProbeApi = true` and `DebugLog = true`.
3. Launch, load a character, take a few hits, die once (normal mode, low-level character).
4. Send `MelonLoader/Latest.log`. Look for:
   - startup line `hooks N hit / N death / N ailment`; `0 hit` means the damage hook names are wrong
   - `[probe] type Il2Cpp.…Health…` blocks: the real method and member names
   - `[probe] player hit via …`: what the damage call's arguments look like
   - `death: …` line: what got recorded
5. Then fix `GameHooks.Candidates`, `PlayerProbe` member names and `ArgReader` from the probe output (or try `HookOverrides` in the cfg first: no rebuild needed).

## Open questions for the probe
- Health component type and the current/max health member names.
- Which method every player hit flows through, and whether DoT ticks use the same one.
- Is there a player death method, or only health reaching 0?
- How ailments are applied to the player (method + type), for Freeze/Shock/Shred.
- Where the character name/class/level live (`PlayerFinder` getter returning CharacterData?).
- Does the player object get recreated on respawn (the latch re-arms either way).

## Laws for this mod
- Hard-reference only game types already proven by a shipped Terrible mod (`PlayerFinder`, `Actor`, `EpochInputManager`). Everything else by name through `Refl`.
- Hook miss path stays cheap: damage hooks fire for every monster.
- A hooked death never counts while readable health is above 0.
- Never draw without a player (no ghost counter on menus).
- Frozen identifiers: assembly `medick_DeathCounter`, prefs category and entry names, cfg path `UserData/medick_DeathCounter.cfg`, log folder `UserData/medick_DeathCounter/`.
- Nothing copies into the game's Mods folder without Andrew (`-p:DeployToMods=false` on every agent build).
