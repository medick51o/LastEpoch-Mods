# TICKET-02b — REPAIR after cross-vendor REJECT (fog_OF_war v1.0.1-dev)

## ADJUDICATION of the review finding (conductor, 2026-09-10 21:31)
🟢 Gemini · BLOCKER · ACCEPTED. Basis: `HasProtectedItemUI` checks `GetComponentInChildren<GroundItemLabel>(true)` at hide time (zone-in). Ground item labels are spawned per dropped item at runtime, so at zone-in the label container is normally EMPTY, the check passes, the outermost "*minimap*" ancestor is hidden, and every label spawned afterwards is parented into the hidden subtree. This reproduces the exact incident (labels present at zone-in are the rare case). Reviewer's repair direction: hide the leaf `minimap.gameObject` only.

## TASK
Repair `medick_FogOfWar\src\FogController.cs` `Hide(Minimap)` so that the hidden target is ALWAYS `minimap.gameObject` itself — no ancestor walk, no name matching, ever. Delete the ancestor-candidate loop and the `HasProtectedItemUI` selection logic (or reduce it to a single defensive check on the leaf: if `minimap.gameObject` itself has a `GroundItemLabel`/`UITooltipItem` in its subtree, refuse to hide and log a warning — that would mean the game restructured and BLIND must degrade to HARD behaviour rather than eat loot). Keep the CanvasGroup-with-SetActive-fallback mechanism and `ShowHidden()` symmetry from TICKET-02. Keep the Dbg.Log of the chosen target and reason. Update the comment block above `Hide()` to state the new law: "the leaf only — an ancestor is never hidden, because the ground-label canvas can share it and labels spawn at runtime (2026-09-10 incident)".

## EXPECTED OUTCOME
1. `dotnet build -c Release --nologo -v q -p:DeployToMods=false` → Build succeeded, 0 warnings; nothing copied to the game's Mods folder (still 22016 B, Jul 12).
2. No `transform.parent` walk remains in `Hide()`; grep `parent` in FogController.cs returns nothing inside `Hide`.
3. CHANGELOG.md v1.0.1 entry rewritten to say: BLIND now hides only the minimap object itself, never a parent container, because the ground item labels live under the same parent and are spawned after zone-in.
4. Report: status word; build command + tail; per-file changes; Mods-folder check.

## CONTEXT / CONSTRAINTS / MUST NOT / LAWS
As TICKET-02 (same folder). WRITE SET unchanged: src\FogController.cs, src\BuildInfo.cs, medick_FogOfWar.csproj, CHANGELOG.md. Do not commit, run the game, deploy, or spawn. "'I could not tell what you meant' is a good outcome. Propose, don't guess."
