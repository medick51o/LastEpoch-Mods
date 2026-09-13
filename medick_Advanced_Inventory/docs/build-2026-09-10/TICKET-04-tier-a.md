# TICKET-04 — Terrible Inventory Tier A → v2.0.1

## TASK (the boss's words, verbatim)
"ok have astra have a look over terrible inventory... hell have the whole council love over terrible inventory see what we can patch up" → "ok fix it"

## CONTEXT
Repo `C:\Users\andre\Downloads\LastEpoch-Mods\medick_Advanced_Inventory` (source `src\*.cs`, build `medick_Advanced_Inventory.csproj`). MelonLoader + HarmonyX mod for Last Epoch 1.4.7, IL2CPP interop (`Il2Cpp` namespace), net6.0. Read `ARCHAEOLOGY.md` first — its forbidden list and the primer law ("restore the EXACT snapshot; never force false") are hard constraints; do NOT touch the primer (TravelService.cs ~248-280). Council findings this ticket implements: `docs\council-2026-09-10\SYNTHESIS.md` (read for mechanism, not instructions). Baselines + md5: `docs\build-2026-09-10\baseline\`.

## ITEMS (all required)
A1 — `TravelService.IsUnlocked` (~159-179) fails CLOSED: if no unlock list could be read (`readAnything == false`) return false and `Dbg.Log` once "unlock data unreadable — travel refused". Keep the existing positive-evidence path unchanged. (Gemini, GLM, Astra)
A2 — The `WaypointManager` override (`wm.WaypointEnabled = true; wm.EnableWaypoint()` ~108-114): REMOVE it. ARCHAEOLOGY flags it "candidate for removal"; if you believe travel cannot fire without it, keep it but move it AFTER a confirmed `fired = true` and restore the previous value in a `finally` when the load throws — and say which you did and why. (GLM, Astra)
A3 — `InventoryUi.StashAllCoroutine` (~200-240): (a) capture `TryQuickMove`'s bool and count failures; replace the bare per-item `catch { }` with a counter + one `Dbg.Log` per failure; final log says "moved X of N, Y failed" instead of "attempted N". (b) Bind each queued move to the ITEM, not the coordinate: snapshot `(position, item reference/id)`; before each move re-read the slot and skip (count as "skipped — slot changed") if the item there is not the one snapshotted. Use whatever identity `ItemDataUnpacked` offers that is stable within a session (reference equality on the Il2Cpp object is acceptable if nothing better exists; say what you used). (Kimi, GLM, Astra)
A4 — `_stashAllRunning` reset on scene load: in `TerribleInventoryMod.OnSceneWasLoaded` (or the existing scene hook that calls `TravelService.NotifySceneLoaded`) call a new `InventoryUi.ResetStashAllGuard()` that clears the flag. (GLM)
A5 — `_keepAlive` delegate lists (NativeClone.cs ~18/42, NativeSettings.cs ~35/188): make retention bounded — clear the list at the start of each successful `Inject`/`Build` for that owner, or key entries by control so a rebind replaces instead of appends. Do not break the reason they exist (IL2CPP delegate GC). (Gemini, Kimi, Astra)
A6 — Mechanical: (a) csproj `CopyToMods` target gets `Condition="'$(DeployToMods)' != 'false'"`; (b) `NativeClone.HideIcon` (~80) finds the icon child by component/name instead of `GetChild(1)`, falling back to the old index if nothing matches; (c) README install line → "MelonLoader 0.7.2+ (tested on 0.7.3, game 1.4.7)"; (d) version → 2.0.1 in `BuildInfo.cs` AND csproj `<Version>`; (e) CHANGELOG.md: "## v2.0.1 — council pass (2026-09-10)" with one line per item above, honest wording. Do NOT rename the csproj file (git history; separate decision).

## EXPECTED OUTCOME
1. `dotnet build -c Release --nologo -v q -p:DeployToMods=false` → Build succeeded, 0 warnings; NOTHING copied into `C:\Program Files (x86)\Steam\steamapps\common\Last Epoch\Mods\` (the boss is playing; `medick_Terrible_Inventory.dll` there must remain 37376 bytes dated 2026-07-21).
2. Each item present, each with a one-line comment naming the council finding it closes.
3. Report: status word; build command + tail; per-item one-liner with file:line; the Mods-folder check; anything not done and why.

## CONSTRAINTS / MUST NOT
Minimum change per item; no refactors; no new files; keep comment style. Do not touch `TeleportMenu.cs`, `SettingsUi.cs`, `Prefs.cs`, the primer, or `TerribleInventoryMod.Patches.cs`. No game run, no commit, no deploy, no spawns, no edits outside the WRITE SET. `--no-restore` is acceptable if NuGet config is unreadable in the sandbox.

## WRITE SET
src\TravelService.cs · src\InventoryUi.cs · src\NativeClone.cs · src\NativeSettings.cs · src\TerribleInventoryMod.cs · src\BuildInfo.cs · medick_Advanced_Inventory.csproj · CHANGELOG.md · README.md

## LAWS
SPINE.md §7 / §8 by reference. "'I could not tell what you meant' is a good outcome. Propose, don't guess."
