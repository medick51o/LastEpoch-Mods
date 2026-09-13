# CURRENT-WORK — MedicK's Terrible fog_OF_war (medick_The_fogOFwar)
Read this first. Update after every step. Last update: 2026-09-11 02:35 (overnight council pass, auto mode, conductor "council-fog").

## State right now
- Repo tree = **v1.0.1, unreleased, UNCOMMITTED** (git diff vs HEAD efc80e5: CHANGELOG, README, SPEC, csproj, BuildInfo.cs, FogController.cs, FogOfWarMod.Patches.cs, NativeSettings.cs, Prefs.cs; docs\ untracked).
- Game's Mods folder runs the **pre-Tier-A 1.0.1-dev** DLL (24,064 B, 2026-09-10 21:35, md5 9f168102…) = last night's BLIND leaf-only fix only. The 1.0.0 release DLL is kept beside it as `.1.0.0-restore`.
- Tonight's Tier A build is staged at `medick_FogOfWar\bin\Release\net6.0\medick_The_fogOFwar.dll` (26,112 B, md5 c09af22a…), Gemini-reviewed PASS, NOT deployed. Andrew's in-hand steps: `docs\build-2026-09-11\MORNING-README.md`.
- Nexus: released July 1 (5-upload night); Nexus ID never recorded. v1.0.1 not published.

## Open items (in order)
1. In-hand (Andrew): 5 steps in MORNING-README — incl. the BLIND minimap-FRAME check (NOT PROVEN since last night) and pasting the `root children:` list from the settings warning.
2. Tier B #8: settings rows do not build on Last Epoch 1.4.7 (template `Toogle - Minion Health Bars` missing). Repair the adapter once the real row name is known (step 5); same fix for Terrible Tooltips / Terrible Inventory.
3. Tier B #9 (GLM vs Astra, named disagreement): CanvasGroup restore does not `SetActive(true)` a leaf the GAME parked while hidden — test BLIND → cutscene → leave BLIND.
4. Then: commit the whole 1.0.1 delta, package `release\medick_The_fogOFwar_v1.0.1.zip`, upload to Nexus (Andrew pulls the trigger).

## Verify commands
- Build without deploying (from `medick_FogOfWar\medick_FogOfWar\`): `dotnet build -c Release --nologo -v q -p:DeployToMods=false` → 0 warnings, 0 errors.
- Game DLL untouched? `Get-Item "C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\medick_The_fogOFwar.dll" | Select Length,LastWriteTime` → 24064, 2026-09-10 21:35 until Andrew copies the new one.
- Delta of tonight: `docs\build-2026-09-11\DELTA-A.patch`; baselines `docs\build-2026-09-11\baseline\`.

## Laws for this repo
- BLIND hides ONLY `minimap.gameObject` — never an ancestor (2026-09-10 loot incident). Never widen name matching ("hud"/"corner" lesson from v1).
- Never adopt/resurrect an object the mod did not hide. Restore is never gated on a live Minimap.
- Frozen identifiers: assembly `medick_The_fogOFwar`, prefs category/entry, cfg path `UserData/medick_The_fogOFwar.cfg`.
- Nothing copies into the game's Mods folder without Andrew (`-p:DeployToMods=false` on every agent build).
