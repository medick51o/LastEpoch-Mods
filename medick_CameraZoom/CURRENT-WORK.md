# CURRENT-WORK — medick_CameraZoom (MedicK's Terrible Zoom)
Read this first. Update after every step. Present tense.

## Now (2026-09-11, overnight council pass)
- v1.0.1 is BUILT and STAGED in `bin\Release\net6.0\medick_CameraZoom.dll` (25,088 B, md5 2ef9a852…). NOT deployed, NOT committed, NOT pushed. The game's Mods folder still runs v1.0.0 (24,064 B, 2026-07-01, md5 14c58069…).
- Waiting on Andrew's in-hand pass: `docs\build-2026-09-11\MORNING-README.md` (5 steps; step 5 is the zoom-direction test that decides Tier B #2).
- Working tree: 8 tracked files modified by the Tier A ticket (csproj, BuildInfo, CameraZoomMod, InputGuard, Prefs, CameraState comments, README, CHANGELOG) + new `docs\` + this file. Nothing else.

## Verify
- `dotnet build -c Release --nologo -v q -p:DeployToMods=false` from the repo root → 0 warnings, 0 errors (VERIFY-OUTPUT.txt).
- Mods DLL must stay 24,064 B / md5 14c5806947db8a1a4321ffa746d8069e until Andrew copies v1.0.1 himself.

## Decisions pending (Andrew)
- Tier B #2: is the mod's zoom sign backwards? Game reports zoomMin -7.0 ABOVE default -17.5; code assumes the zoom-out limit sits BELOW default. His daily ZoomMin = -1.0 is the tiebreaker. Council split (GLM BLOCKER / Kimi coherent / Astra NOT PROVEN / Gemini MINOR). No Tier A change touched it.
- Ship v1.0.1 to Nexus #27 after in-hand: he pulls the trigger.

## History
- 2026-09-11 overnight: 4-seat council (Astra, Gemini, Kimi, GLM) → SYNTHESIS (15 findings) → TICKET-A (6 items) → Codex build DONE 0 warnings → Gemini review PASS, 0 repair rounds. Mirror C:\Sync\Projects\zoom-review-2026-09-11 (806b1c1 pre, 9217df8 post).
- 2026-07-01: v1.0.0 Terrible-era release (renumbered from 2.0.0; Nexus #27). Byte-identical to the Mods DLL tonight.
- 2026-04-09: v1.2 initial Nexus release.
