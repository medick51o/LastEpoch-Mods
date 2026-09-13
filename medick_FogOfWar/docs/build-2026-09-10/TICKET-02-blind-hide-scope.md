# TICKET-02 — BLIND hide must never touch ground item labels (fog_OF_war v1.0.0 → v1.0.1-dev)

## TASK (the boss's words, verbatim)
"i threw a idol on the ground from my inventory and now it wont show up and i cant pick it up this is very dangerous for players" … "actually all my items im throwing on the ground are not showing up right now" … "i logged in as offline and i dropped a item and it indeed did not show up so yes blind loading into the game likely is the culprit"

## ESTABLISHED IN-HAND (2026-09-10, Last Epoch 1.4.7, MelonLoader 0.7.3)
- Fog DLL off → dropped items show ground labels. Fog on at HARD → labels show. Fog on at BLIND, zone-in → labels gone, items cannot be picked up (Last Epoch pickup is by clicking the label). Same session, one variable at a time.
- `FogController.Hide(Minimap)` (medick_FogOfWar\src\FogController.cs ~line 141) deactivates the OUTERMOST ancestor whose name contains "minimap". In 1.4.7 that ancestor evidently also contains the ground item label canvas (GroundItemLabel objects), so `SetActive(false)` takes the labels down with the map. Unity Player.log from that session: 354 `DMM.DMMapIcon.OnDisable` / `SceneChangeableDMMapIcon.OnDisable` NullReferenceExceptions at zone-in — the disable cascade over the hidden subtree.

## EXPECTED OUTCOME (gradeable)
1. `dotnet build -c Release -p:DeployToMods=false` succeeds, 0 warnings, and NOTHING is copied into `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\` (the boss is testing another mod there; touching that folder is a BLOCKER). If the csproj has a post-build copy target, give it `Condition="'$(DeployToMods)' != 'false'"` first.
2. `Hide()` chooses its target so that it NEVER contains a `GroundItemLabel` (type `Il2Cpp.GroundItemLabel`; check with `GetComponentInChildren<GroundItemLabel>(true)`), and never contains a `UITooltipItem`. Concretely: walk up from `minimap.transform` collecting ancestors whose name contains "minimap"; pick the OUTERMOST candidate whose subtree contains no GroundItemLabel/UITooltipItem; if none qualifies, fall back to `minimap.gameObject`. Log the chosen target name and the reason via `Dbg.Log` (existing helper) so the next incident is diagnosable.
3. Also stop the OnDisable exception cascade if cheaply possible: prefer hiding via a `CanvasGroup` (alpha 0, blocksRaycasts false, interactable false) on the chosen target instead of `SetActive(false)` when the target has or can take a CanvasGroup; keep `SetActive(false)` as the fallback. `ShowHidden()` must restore whichever mechanism was used. If this is not cheaply possible, say so and keep SetActive with the scoping fix only — scope is the fix, CanvasGroup is the nice-to-have.
4. Bump the version (BuildInfo.cs + csproj) to 1.0.1 and add a CHANGELOG.md entry "## v1.0.1 — BLIND no longer hides ground item labels" with two honest sentences.
5. Report: first line a status word; the exact build command + tail; per-file change list; the Mods-folder listing showing `medick_The_fogOFwar.dll` unchanged (22016 bytes, dated Jul 12).

## CONTEXT
Repo: `C:\Users\andre\Downloads\LastEpoch-Mods\medick_FogOfWar` — source `medick_FogOfWar\src\*.cs`, build `medick_FogOfWar\medick_FogOfWar.csproj`. Read `SPEC.md` and the comments in `FogController.cs` (the v1 "hud/corner" rename lesson is a hard constraint: never widen name matching). Game assemblies for type lookup: `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\MelonLoader\Il2CppAssemblies\Il2CppLE.dll` (namespace `Il2Cpp`). Baselines + md5 of the write set: `docs\build-2026-09-10\baseline\`.

## CONSTRAINTS
Minimum change. No refactor of the level system, no new files. Do not run the game. Do not commit. Do not deploy.

## MUST DO
Verify: `cd medick_FogOfWar && dotnet build -c Release --nologo -v q -p:DeployToMods=false` → Build succeeded, 0 warnings. Then `Get-Item "C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\medick_The_fogOFwar.dll" | Select LastWriteTime,Length` and confirm unchanged.

## MUST NOT
No undeclared spawns. No edits outside the WRITE SET. No commits. No deployment.

## OUTPUT FORMAT
First line DONE / DONE_WITH_CONCERNS / NEEDS_CONTEXT / BLOCKED. Then build command + tail. Then per-file changes. Then the Mods-folder check.

## WRITE SET
- medick_FogOfWar\src\FogController.cs
- medick_FogOfWar\src\BuildInfo.cs
- medick_FogOfWar\medick_FogOfWar.csproj
- CHANGELOG.md

## LAWS
SPINE.md §7 / §8 by reference. "'I could not tell what you meant' is a good outcome. Propose, don't guess."
