# 🔵 Codex (default model) — BUILD TICKET-A report — 2026-09-11 (verbatim; threadId 01a08f91-5e4c-7d52-8b1d-11fd93e566d2)

DONE

### Build

`dotnet build -c Release --nologo -v q -p:DeployToMods=false --no-restore`

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The initial restore attempt hit the documented sandbox NuGet.Config denial; the ticket-authorized `--no-restore` build passed.

### Items

- A1 — Corrected input-lock ownership and release semantics in src/InputBlocker.cs:32.
- A2 — Added finite-value repairs, integer clamps, and one consolidated warning in src/Prefs.cs:66.
- A3 — Cached `Camera.main` once per frame with exception handling in src/UI/OverheadRenderer.cs:64.
- A4 — Added live replacement diagnostics and one-session warning in src/SlotRegistry.cs:64. Native `.Pointer` identity is used because IL2CPP may provide different wrappers for the same object.
- A5 — Removed stale version narration from src/InputDetection.cs:13, src/UI/OverheadRenderer.cs:9, and src/UI/Theme.cs:254.
- A6 — Updated installation requirements in README.md:43.
- A7 — Added project version and docs exclusion in medick_CooldownTracker.csproj:8, updated src/BuildInfo.cs:13, and added the release/legacy notes in CHANGELOG.md:3. No deploy target exists.

### Mods-folder check

`medick_CooldownTracker.dll` remains unchanged: 40,448 bytes, `2026-07-01 20:58:54`, MD5 `c4210da03fef2976a49e0f7bad636b2a`.

### Not done

Nothing omitted. No game run, deployment, commit, or edits outside the ticket write set.

🔵 Codex
