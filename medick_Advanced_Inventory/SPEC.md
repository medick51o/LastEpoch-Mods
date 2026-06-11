# SPEC — MedicK's Terrible Inventory v2.0.0

*an inventory mod* — **Terrible Inventory** for short. Internal/assembly name
`medick_Terrible_Inventory` (frozen: DLL filename, Nexus #29 upgrade path).
Companion document: [ARCHAEOLOGY.md](ARCHAEOLOGY.md) — the recovered API truth
table and the seven blood-bought safety rules. This spec assumes both.

## Product contract (what v1 promised, v2 keeps)

1. **Footer buttons** on the inventory panel, after the game's Transfer/Sort:
   **STASH** (open stash anywhere) · **STASH ALL** (dump inventory to stash) ·
   **VENDOR** (open the NPC shop anywhere).
2. **Quick Teleport menu** attached left of the inventory panel: an
   always-visible QUICK TELEPORT master tab; a collapsible column with three
   collapsible groups — **FACTIONS** (Circle of Fortune, Merchant's Guild,
   Forgotten Knights, The Woven) · **DUNGEONS** (Lightless Arbor, Temporal
   Sanctum, Soulfire Bastion) · **HUBS** (End of Time, Champion's Gate/Arena).
   Collapse choices survive panel rebuilds/zone changes within a session.
3. **Controller / Steam Deck**: all buttons remain visible and usable in
   controller mode (right-stick cursor); Deck-touch-sized targets.
4. Stash All uses the game's own quick-move at KG's 3-frame server-safe
   cadence; respects stash affinity; full-tab fallback is the game's behavior.
5. Era controllers are silently primed once per session (snapshot-restore),
   so every teleport works without ever opening the map.

## v2.0 — the EHG-proud mandate

**Nothing hand-painted. Every visual element is cloned or harvested from the
game's own UI.** v1's teleport column was flat-color rectangles + a scavenged
font + a faux two-rectangle "gold border" — the "slapped on top at 2am" look.

- **Footer buttons**: clone the native Sort button and KEEP its skin — no
  flat color overlay on the background sprite. Differentiation comes from the
  label text. The Sort glyph child is disabled (it overlaps custom labels);
  the label's `LocalizeStringEvent` is destroyed (or the game rewrites it).
- **The game's own buttons are never modified.** No resizing Transfer/Sort,
  no squeezing the footer layout, no touching the currency row.
  (`TryCompactCurrency` — v1's shipped debug session — is deleted. If currency
  compacting ever returns, it returns as a designed opt-in feature.)
- **Teleport buttons, headers, and the master tab** are built from the same
  cloned native button base (sprite, material, font, hover transition all
  inherited), resized for the column. Faction identity = a slim colored
  accent bar + tinted second label line — NOT whole-button color slabs.
  Andrew's requirements stand: faction colors identifiable at a glance,
  touch-sized on Deck.
- Collapse arrows stay ASCII `v` / `>` — the game's TMP font has no Unicode
  triangle glyphs (verified April 2026; renders as □).
- Column layout via a container + VerticalLayoutGroup (collapse = SetActive);
  v1's hand-rolled Reflow list is retired.

## Travel engine (hardened)

1. Click → if `_travelInProgress`, ignore (concurrency guard stays).
2. **Unlock gate (NEW — safety rule #2 finally implemented):** the target
   scene must appear in a `UIWaypointController.unlockedScenes`; if not, show
   nothing destructive — one quiet log line, button does nothing, exactly like
   the map's own locked node.
3. Travel = find `UIWaypointStandard` in `waypointsInMenu`, call
   `LoadWaypointScene()`. Never `SendAttemptWaypoint` (disconnect).
   Immediately before the load — and ONLY once the gate has passed and the
   waypoint is found, so a non-travelling click leaves zero footprint — the
   v1-carried `WaypointManager` enable runs (`WaypointEnabled = true` +
   `EnableWaypoint()`): some zones disable waypoint use and this allows the
   jump the way v1 always did. The successful scene load resets the flag.
4. If the waypoint isn't found: re-run the primer ONCE, retry once, then give
   up with a single warning **per destination per session** ("travel
   unavailable here"). **The v1 fallback chain is deleted** — no map-flash +
   era-tab text-click, no searching 3,671 buttons for "VISIT X" (the path
   that once invoked the mod's own button and summoned EHG's bug reporter).
   Primer-then-retry replaces all of it.
4b. The travel concurrency guard spans the whole scene transition (released
   on scene load, 10 s failsafe) — not just the frame the load fires.
5. Primer: once per session on first inventory open in a playable scene;
   snapshot `activeSelf` of the full ancestor chain per controller, activate
   root→leaf, one frame for OnEnable, restore the EXACT snapshot (forcing
   false wiped the world map once — never again).

## Controller mode

- `OnLateUpdate` keep-alive re-activates `Left_Buttons_Container` while its
  parent footer is `activeInHierarchy` — unchanged, it's the load-bearing
  trick. Comments must say OnLateUpdate (v1's still said OnUpdate).
- Accepted cosmetic: gamepad X/Y prompts overlap the native Transfer/Sort in
  controller mode. (Deliberately NOT hiding Gamepad_Prompts_Container — pad
  users need those prompts for the native buttons.)
- No resolution scaling of any kind. CanvasScaler owns resolution
  (the Screen.width scaling pass was v1's confirmed dead-end).
  `childForceExpandWidth/Height = false` on the bar HLG stays.

## Settings (native injection — fog_OF_war's hardened NativeSettings template)

Category **"Terrible Inventory"** in the game's settings screen. Row
taxonomy per the family playbook (every row classified, none dishonest):

| Row | Class | Why |
|---|---|---|
| Show STASH button | toggle (real control) | the April 2026 dropped wish: "allow the users to select what they want showing" |
| Show STASH ALL button | toggle | 〃 |
| Show VENDOR button | toggle | 〃 |
| Show Quick Teleport menu | toggle | 〃 — hides the master tab + column entirely |
| Debug logging | toggle | gates ALL diagnostic output (default off; v1 logged scene + hierarchy dumps every inventory open) |

No dropdowns needed in this mod; no info rows needed (the buttons explain
themselves in-game). Toggles apply live (SetActive on the injected roots) and
persist via MelonPreferences (`medick_Terrible_Inventory` category — NEW in
v2; v1 had no preferences at all, so there is no legacy to migrate).

## Frozen identifiers

- Assembly/DLL: `medick_Terrible_Inventory` · Nexus #29
- Injected object names keep the `medick_` prefix (self-exclusion in any
  future searches; recognizable in dumps)
- Scene-name table as in ARCHAEOLOGY.md (Dun1Q10 = Temporal Sanctum,
  Dun2Q10 = Lightless Arbor — the v1.0 swap is not to be re-introduced)

## Non-goals

- No Bazaar/player-market button (`openBazaar` rejected in April).
- No stash tab rearranging/splitting (game limitation; "dump tab" philosophy).
- No per-zone teleport favorites, no dungeon-key Y-press parity (candidate
  v2.1 items, recorded in ARCHAEOLOGY.md wishes).
- Online gray-area warning stays in the Nexus copy, voice intact.

## Engineering standard (family playbook)

src/ modules · nested Harmony patches + `HarmonyDontPatchAll` with per-class
TryPatch degradation · BuildInfo single version source (v1 had three
disagreeing version strings) · Dbg-gated logging, one startup line ·
clones named LAST + destroyed in catch · CS1626 coroutine pattern ·
`new Action(...)` wrappers for Il2Cpp listeners · listener refs kept alive in
a static list · README/CHANGELOG rewritten · review fleet before release
(6 lenses: + spec-conformance + il2cpp-risk + native-look fidelity).
