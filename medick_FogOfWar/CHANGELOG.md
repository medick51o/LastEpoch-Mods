# Changelog — MedicK's Terrible fog_OF_war

## v1.0.0 — 2026-07-01

Ground-up engineering pass and rebrand: **The fog OF war** is now
**MedicK's Terrible fog_OF_war** ("fog_OF_war" for short), joining the
Terrible family. Internal name, DLL, prefs and cfg path are unchanged
(`medick_The_fogOFwar`). The version resets to **1.0.0** — the Terrible era
starts here (succeeds The fog OF war v1.0 from April).

### Fixed
- **No more restarts. Ever.** v1's "BLIND requires a full game restart —
  both ways" warning wall is gone: entering BLIND hides the minimap live,
  leaving BLIND restores it live (the hidden object is remembered and
  re-activated). All six levels apply the moment you pick them.
- **HUD blast-radius hazard defused.** v1's minimap-hiding walk matched any
  ancestor named "minimap", "hud" or "corner" — one hierarchy rename away
  from deactivating the entire HUD. v2 only ever hides a minimap-named
  ancestor (outermost match) or the minimap object itself.
- **Zone default radius is captured per zone**, not latched once globally —
  zones with different defaults now scale LIMITED/SCOUT correctly.
- The mod no longer reports its errors as "[Terrible Tooltips]" — v1's
  settings helpers were copy-pasted from the sibling mod, header and log
  prefixes included.

### Fixed in the release-night fable pass (2026-07-01)
- BLIND now re-asserts itself when the game re-activates the minimap
  without a fresh zone load (overlay-map close, cutscene end). No more
  minimap popping back mid-BLIND.
- Leaving BLIND can no longer resurrect a game-parked minimap: the mod
  never adopts a container it did not itself hide.
- The zone-default guard now remembers every radius the mod wrote this
  session, closing the last pooled-instance path for the old compounding
  LIMITED/SCOUT drift.
- Settings row failures warn once per session instead of spamming on
  every settings open.
- The HARD legend row now says that already-explored fog stays revealed
  until your next zone.

### Changed
- **The level legend IS the control now** — each colour-coded row selects its
  level on click and the active row stays checked, radio-style (v1's legend
  rows were dead buttons captioned "this box is not clickable, it is just
  here for reference"). The dropdown is gone — once the rows became real
  controls it was redundant.
- Settings injection is idempotent (re-runs rebind instead of duplicating
  rows) and degrades gracefully with one warning if a game update changes
  the settings hierarchy — fog control keeps working from the cfg file.
- Source restructured into `src/` modules with a written behavior contract
  (SPEC.md); Harmony patches nested in the mod class (MelonLoader
  auto-discovery guardrail); csproj relics removed (AllowUnsafeBlocks,
  Optimize, duplicate Il2CppLE.Core reference).
- Restores vanilla map state if the mod is unloaded.
- Settings carry over from v1.0 unchanged (same category, entry, cfg path).

### Added
- `DebugLog` preference — apply/capture logging is opt-in; default output is
  one startup line.

---

## v1.0 (The fog OF war) — 2026-04-12
- Initial release: 6-level fog of war control (BLIND / HARD / LIMITED /
  NORMAL / SCOUT / ORACLE), Map Vision dropdown + level legend injected into
  the game's settings screen, persisted to UserData/medick_The_fogOFwar.cfg.
- BLIND required a full game restart in both directions.
