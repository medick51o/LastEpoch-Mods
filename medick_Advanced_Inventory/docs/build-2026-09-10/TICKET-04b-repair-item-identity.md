# TICKET-04b — REPAIR after cross-vendor REJECT (Terrible Inventory v2.0.1)

## ADJUDICATION (conductor, 2026-09-10 22:48)
🟢 Gemini · BLOCKER · ACCEPTED. `src\InventoryUi.cs` ~241: `ReferenceEquals(currentItem, move.item)` compares Il2CppInterop MANAGED wrappers; `inv.GetEntryAt(pos)?.data?.TryCast<ItemDataUnpacked>()` yields a fresh wrapper per read, so the comparison is false for every slot → every item is "skipped — slot changed" → STASH ALL moves nothing.

## TASK
Replace the managed-reference identity check with a native-pointer comparison: `move.item != null && currentItem != null && currentItem.Pointer == move.item.Pointer` (Il2CppObjectBase.Pointer). If `ItemDataUnpacked` exposes a stable per-item id field you can prove from Il2CppLE.dll stubs, you may use that instead — say which. Keep the snapshot structure, counters, and log wording from TICKET-04. Update the comment above the check to state WHY pointer equality (wrappers are not stable). Nothing else changes.

## EXPECTED OUTCOME
`dotnet build -c Release --nologo -v q -p:DeployToMods=false` → Build succeeded, 0 warnings; game Mods folder untouched (medick_Terrible_Inventory.dll still 37376 B, 2026-07-21). Report: status word; build tail; the exact new comparison line with file:line; Mods-folder check.

## WRITE SET
src\InventoryUi.cs only.

## LAWS / MUST NOT
As TICKET-04. No commit, no deploy, no game run, no spawns. "'I could not tell what you meant' is a good outcome. Propose, don't guess."
