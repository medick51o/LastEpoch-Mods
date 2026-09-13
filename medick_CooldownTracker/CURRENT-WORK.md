# CURRENT-WORK — medick_CooldownTracker (Terrible Cooldowns)

_Updated 2026-09-11 01:35 by the overnight council conductor (auto mode, Andrew asleep)._

## State
- **Shipped / in game:** v1.0.0 (Nexus #26, Mods DLL 2026-07-01, 40,448 B).
- **Staged, NOT deployed:** v1.0.1 council pass — built to `bin\Release\net6.0\medick_CooldownTracker.dll`, cross-vendor reviewed (Gemini PASS), uncommitted working-tree changes in 10 write-set files + new `docs\`.
- **Gate:** Andrew's in-hand check — `docs\build-2026-09-11\MORNING-README.md` (5 steps). Then commit, then decide on Nexus upload.

## What v1.0.1 changes
InputBlocker no longer releases a sibling's lock · non-finite/out-of-range prefs sanitized on load · Camera.main cached per frame · one-per-session diagnostic when a slot is re-registered from a different live AbilityBarIcon · stale v4/v5 comments retconned · README MelonLoader 0.7.x · csproj `<Version>` + `docs\**\*.cs` compile exclusion · CHANGELOG.

## Verify
```
dotnet build -c Release --nologo -v q -p:DeployToMods=false
```
→ 0 warnings, 0 errors (2026-09-11 01:28). No copy-to-Mods target exists in this csproj; the property is inert here and kept for parity with the sibling mods.

## Open queue
Tier B / C list in `docs\council-2026-09-11\SYNTHESIS.md` and the morning README. Key unknown: whether 1.4.7 ever spawns a second `AbilityBarIcon` set (finding #2) — the new warning line is the evidence.

## Laws for this repo
Never copy into the game's Mods folder from a build; version lives in BuildInfo.cs AND csproj; prefs category/keys are the upgrade path — never rename.
