# SPEC — MedicK's Terrible fog_OFwar v2.0.0

*a fog control mod* — "fog_OF_war" for short. Internal name `medick_The_fogOFwar` (frozen: DLL, prefs category, cfg path, Nexus upgrade path).

## Behavior contract

### Vision levels (shipped v1.0 values — frozen)

| # | Level | RevealRadius | Minimap UI | Notes |
|---|-------|--------------|------------|-------|
| 0 | BLIND   | 0 | **hidden** | overlay map still openable — honor system |
| 1 | HARD    | 0 | visible | minimap shows but reveals nothing |
| 2 | LIMITED | 69% of zone default | visible | |
| 3 | NORMAL  | zone default | visible | mod is inert |
| 4 | SCOUT   | 3× zone default | visible | |
| 5 | ORACLE  | 600 flat | visible | full zone, no FPS tax (tested v1 value) |

Zone-default fallback when no capture exists yet: 150.

### Apply rules

1. **Everything applies live. No restarts, ever.** (v1 required a full game
   restart to enter or leave BLIND; that was a tooling limitation, not a
   design decision. v2 hides/shows the minimap GameObject at runtime.)
2. On `Minimap.Awake` (each zone): capture **that instance's** pre-write
   `RevealRadius` as the zone default (refreshed per zone, never latched
   globally), then apply the current level. **A value the mod itself wrote
   is never adopted as a default** (last-written guard) — otherwise
   pooled/cloned/re-awoken minimaps would compound LIMITED/SCOUT zone after
   zone.
3. On level change (dropdown or legend click): persist the pref, apply to the
   live minimap instance. Dead/absent instance → safe no-op; next zone
   applies. Selecting a level from the login screen is valid.
4. BLIND hide scope: the **outermost ancestor whose name contains "minimap"**
   (case-insensitive); fallback = the Minimap's own GameObject. Never match
   "hud"/"corner" (v1 did — one rename away from hiding the entire HUD).
   Remember **every** GameObject BLIND hid (overlapping minimap lifetimes
   across a BLIND→BLIND zone change are tracked individually); leaving BLIND
   restores them all and clears the memory. **The restore is never gated on
   a live Minimap instance** — leaving BLIND from a loading screen must
   still un-hide.
5. Engine reality, documented in UI copy: lowering the radius does not
   re-fog already-explored area; the next zone starts clean.

### Settings UI (native injection — no IMGUI panel; this is the mod's identity)

- Injected into the game's own settings screen (`SettingsPanelTabNavigable.Awake`)
  under category **"Terrible fog_OFwar"**, via the KG/war3i4i instantiation
  technique (credited).
- One enum dropdown (primary control) + six colour-coded legend rows that are
  **clickable** — clicking a legend row selects that level and syncs the
  dropdown (v1's legend rows were dead buttons labelled "not clickable").
- v1's restart-warning rows are deleted — obsolete once everything is live.
- Injection must be idempotent: re-running against a hierarchy that already
  contains our rows rebinds instead of duplicating.
- Brand voice in row copy is part of the product. Keep the jokes.

### Frozen identifiers (v1.x upgrade path)

- Assembly/DLL: `medick_The_fogOFwar`
- Prefs category: `medick_The_fogOFwar` (display "Map Vision")
- Pref entry: `VisionLevel` (enum, default NORMAL)
- Cfg file: `UserData/medick_The_fogOFwar.cfg` (autoload)
- New in v2: `DebugLog` (bool, default false)

### Known fragility (accepted, guarded)

Native injection depends on game hierarchy details that EHG can change:
template rows `"Toogle - Minion Health Bars"` / `"Dropdown - Language
Selection"`, the `Header - Interface` anchor, and the
`settings.transform.GetChild(0).GetChild(0)` root path. Every access is
guarded; on failure the mod logs **one latched warning per session** and
degrades to cfg-file-only configuration (fog control itself keeps working).
Clones are named last and destroyed on build failure, so a half-built row
can never be mistaken for success later. Harmony patches are applied
manually, one try/catch per patch class (`HarmonyDontPatchAll`), so a
renamed game method disables that single feature instead of killing the
whole mod at registration.

### Non-goals

- No IMGUI/Theme panel — native settings integration is this mod's design.
- No per-zone level memory, no keybind cycling (candidate for v2.1 if asked).
- No attempt to re-fog explored areas (engine owns that).

## Why this mod matters beyond itself

The native-settings injection pattern (NativeSettings.cs) is the same
technique Terrible Tooltips and Terrible Inventory use. This mod is the
small, clean proving ground for the pattern before those two get revamped.
