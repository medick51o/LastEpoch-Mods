# Passdown: Terrible Deaths (medick_DeathCounter), first local in-game test

From: Claude (cloud session, wrote the mod, never had the game).
To: Codex, running locally on Andrew's laptop with Last Epoch Season 5 installed.
Date: 2026-10-02 (Pacific Time).

Goal: build the mod against the real game, get it loading, run the probe, fix the
hook names from the probe output, and get to one correctly recorded death.

## Ground rules from Andrew
- Andrew decides what goes into the game's `Mods/` folder. Ask before copying, or
  have him do it. Build with `-p:DeployToMods=false` unless he says otherwise.
- Do not post to Nexus or GitHub on his behalf. Commits and pushes to the branch
  below are fine when he asks.
- Prose style: no double dashes. Give times in Pacific Time.
- Separate what you saw in a log from what you assume.

## Where things are
- Repo: `medick51o/LastEpoch-Mods`, branch `claude/last-epoch-death-counter-yspgnf`
  (draft PR #2). `git pull` first: Andrew pushed three Season 5 commits overnight
  (up to 1c3b289).
- Mod: `medick_DeathCounter/`. Read `CURRENT-WORK.md` first, then `README.md`.
- Game API notes: `docs/RESEARCH-game-api.md`, `RESEARCH-player-stats.md`,
  `RESEARCH-damage-and-defenses.md`.
- Prebuilt DLL: `release/medick_DeathCounter.dll` (v0.1.0). It was built **before**
  Season 5 and does **not** include the CoreModule repair, so treat it as stale.

## Key facts about the mod
- MelonLoader mod; it references **no game assembly**. Every Last Epoch type and
  member is reached by name through `src/Game/Refl.cs`. A wrong name costs one
  feature plus a log warning, not a crash.
- Three ways it notices a death: Harmony hooks (`src/Game/GameHooks.cs`,
  `Candidates`), a health watch, and the game's own `CharacterData.Deaths` counter
  (`DeathCountLedger` reconciles them).
- Hook names come from public LE mods and a 2023 dump. **None of them has been run
  in game.** Season 5 is known to have moved APIs (Tooltips 3.1.2 needed
  `individualID` as uint, a parameterless ground label hook, and a new affix lookup).
- Pure logic is in `src/Core` with 51 unit tests in `tests/CoreTests`.
- `tools/InteropGuard` checks that every Unity/Il2CppInterop call the mod makes is
  also made by a shipped Terrible DLL (it caught a real crash: `GUIContent.none`).
- CI: `.github/workflows/death-counter.yml` runs the tests, the NoGame build with
  `-warnaserror`, and InteropGuard.

## Season 5 changes that affect this mod (do this first)
Season 5 runs Unity 6000.4.8f1 with **MelonLoader 0.7.3**. The generated
`MelonLoader/Il2CppAssemblies/UnityEngine.CoreModule.dll` has a duplicate `<>O`
type, so any mod that references CoreModule fails with `BadImageFormatException`.
Andrew's fix for the other four mods embeds a repair in each mod DLL:
`tools/TerribleInteropRepair/CoreModuleRepair.cs` plus `EmbeddedRepairBootstrap.cs`
(a `[ModuleInitializer]` that normalizes the file with Mono.Cecil, keeps a backup,
and uses a SHA256 receipt).

Terrible Deaths does not have it yet. Copy the pattern from
`medick_CameraZoom/medick_CameraZoom.csproj`:
- `<PlatformTarget>x64</PlatformTarget>`
- `Compile Include` both repair files (Link into `Compatibility\`)
- a `Mono.Cecil` reference from `$(ML)Mono.Cecil.dll`, `Private=false`
- for the NoGame build (CI), add a matching `Mono.Cecil` PackageReference with
  `ExcludeAssets="runtime"` (pick the version ML 0.7.3 ships), and bump the NoGame
  MelonLoader package to 0.7.3 if it is on NuGet
- InteropGuard: the repair uses no Unity calls, but confirm it still passes

If Andrew already runs one of the updated Terrible mods, CoreModule is probably
already repaired on his machine, so the death counter may load even without the
embed. Still add it, because the mod must stand alone for Nexus users.

## Build
- Against the game (default): `dotnet build medick_DeathCounter -c Release -p:DeployToMods=false`.
  The csproj hint paths assume `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\`.
  If the game is elsewhere, override with `-p:ML=...\MelonLoader\net6\ -p:GM=...\MelonLoader\Il2CppAssemblies\`.
- Tests: `dotnet test medick_DeathCounter/tests/CoreTests` (all must pass).
- Output: `medick_DeathCounter/bin/Release/net6.0/medick_DeathCounter.dll`.

## Test plan
1. **Load test.** With Andrew's OK, put the DLL in `Mods/`, launch, reach the
   character select screen, quit. In `MelonLoader/Latest.log` check for:
   the embedded repair line, `medick_DeathCounter` initialized, and no
   `MissingMethodException` / `TypeLoadException`. A load failure names the member.
2. **Turn on the probe.** In `UserData/medick_DeathCounter.cfg` set
   `ProbeApi = true` and `DebugLog = true`.
3. **One death.** Use a low-level **softcore** character. Take a few hits, then die
   once. Use an ailment (bleed, poison or ignite) if possible.
4. **Read the log.** Look for:
   - startup `hooks N hit / N death / N ailment`. `0 hit` means the damage hook
     names are wrong
   - `[probe] type Il2Cpp....Health...` blocks: the real method and member names
   - `[probe] player hit via ...`: what the damage call's arguments look like
   - `[probe] defences raw:`: whether percent stats are stored as 75 or 0.75
   - the `death: ...` line: what got recorded
   - `HitEvents postfix refused`: only the crit flag is lost
5. **Fix from the probe.** Try `HookOverrides` in the cfg first (no rebuild), e.g.
   `hit:Il2Cpp.BaseHealth.ReceiveDamage; death:Il2Cpp.PlayerHealth.Die`. Then make
   the fix permanent in `GameHooks.Candidates`, `PlayerProbe` member names and
   `ArgReader`.
6. **UI check.** The counter pill shows only in game (not on menus). `Insert` opens
   the panel (LAST DEATH and PATTERNS tabs); `Shift+Insert` toggles the counter.
   Mouse clicks must not reach the game while the panel is open. Compare with
   `mockups/death-panel.png`.
7. **Files.** `UserData/medick_DeathCounter/` should hold `deaths.jsonl` and
   `deaths.txt` with one entry per death. The count must match the game's own
   death count (no double counting from hook plus health watch plus counter).
8. Repeat 3 to 7 until a death shows the correct killer, ability, damage types,
   ailment and a sensible suggestion.

## Open questions the test should answer
- Defence scale 75 vs 0.75. Once known, hardcode it in `DefenseSnapshot.Normalize`
  and add a unit test.
- Are `ProtectionClass.uncapped<Element>Resistance`, `armour`, `dodgeRating`,
  `blockChance`, `endurance`, `critAvoidance` still the field names?
- Does `ProtectionClass.ApplyDamage(DamageStats, DamageSource, float, Actor, bool)`
  still have that signature?
- Is `ActorDisplayInformation` (attacker names) on the actor or on a child object?
- What does `ProtectionClass.deathInformation.deathInfo` say (the "Game says" line)?
- Does `CharacterData.Deaths` go up at death or at respawn (a 60 s grace covers both)?
- Online play: server-side hooks may not fire on the client; the Deaths counter
  and death screen should still work.

## Laws (do not break)
- Reference no game assembly; reach game types by name through `Refl`.
- Every Unity/Il2CppInterop call passes InteropGuard, or is fenced in its own
  method inside a try and allow-listed in the workflow.
- The hook miss path stays cheap: damage hooks fire for every monster.
- A hooked death never counts while readable health is above 0.
- Never draw without a player (no ghost counter on menus).
- Frozen names: assembly `medick_DeathCounter`, prefs category and entry names,
  `UserData/medick_DeathCounter.cfg`, `UserData/medick_DeathCounter/`.

## When done
- Update `CURRENT-WORK.md` with what was proven in game, then `CHANGELOG.md`.
- Rebuild `release/medick_DeathCounter.dll` and the zip (bump to v0.1.1 or v0.2.0),
  a single DLL at the zip root like the other Season 5 packages.
- Run the tests and InteropGuard before every push; CI must stay green.
- Hand back a short report: what loaded, the hook counts, one recorded death line,
  what you changed, and what is still unproven.
