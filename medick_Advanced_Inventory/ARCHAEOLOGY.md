# ARCHAEOLOGY — Terrible Inventory rebuild dossier

Recovered 2026-06-10 from the April 2026 dev transcripts (152MB session log,
5-theme mining pass). This is the institutional memory the v2 rebuild is
built on. Treat the API table and safety rules as load-bearing.

## Game API truth table (verified in-game, April 2026 / Season 4)

| Concern | Truth |
|---|---|
| Injection hook | `EnableWovenEchoesTabIfRelevant.Awake()` postfix — fires on every inventory panel (re)build; ALL injection must be idempotent (marker-name guards) and per-panel state must reset |
| Footer path | `Tab Contents/Items Tab/Inventory Tab Footer Base/Left_Buttons_Container/Sort` (relative to the hook component). Footer children: `[0] Left_Buttons_Container` (Transfer, Sort) · `[1] Gamepad_Prompts_Container` · `[2] Currencies` (Gold / Ancient Bones / Faction Favor) |
| Open stash | `UIBase.instance.openStash(true, false)` (guard `UIBase.instanceExists`) |
| Open vendor | `UIBase.instance.openShop(true)` — works anywhere, no NPC needed (`openBazaar(null)` = player market, rejected) |
| Stash All | `ItemContainersManager.Instance.TryQuickMove(ContainerID.INVENTORY, ContainerID.STASH, pos, false, false)` per item; snapshot positions first via `inv.content.ToArray().Select(e => e._Position_k__BackingField)`; `openStash(false,false)` + 1 frame before starting; **3-frame yield between moves (KG's server-safe cadence)**; respects stash affinity, full-tab fallback is a game limitation |
| Teleport | Find target `UIWaypointStandard` inside one of the **5 per-era `UIWaypointController`s'** `waypointsInMenu`, call `LoadWaypointScene()` — routes correctly through Offline/ClientTransitionService in both modes because it IS the game's own waypoint-click path |
| Scene names | Observatory (CoF) · Bazaar (MG) · ArenaLobby (Champion's Gate) · EoT · M_Knight (Forgotten Knights) · WeaversHub (Woven) · Dun1Q10 (Temporal Sanctum) · Dun2Q10 (Lightless Arbor) · Dun3Q10 (Soulfire Bastion). Names live in ScriptableObject assets, NOT the DLL — verified empirically. v1.0 shipped TS/LA swapped. |
| Era controllers | All 5 populate `waypointsInMenu` at scene load (Ancient 20, Divine 63, Imperial 37, Ruined 35, EoT 14) BUT travel only works after the controller's `OnEnable` fired this session |
| The primer | Once per session, on first inventory open in a playable scene: for each controller, snapshot `wasActive` of the FULL ancestor chain, activate root→leaf so `OnEnable` fires, then **restore the exact snapshot** (forcing false wiped the world map empty — shipped regression, fixed). Runs during the loading screen; invisible. THE USER INVENTED THIS FIX. |
| Controller mode | Game hides `Left_Buttons_Container` and shows `Gamepad_Prompts_Container` EVERY FRAME while pad input is active. Fix: `OnLateUpdate` keep-alive (runs after all game Updates = final say) re-activating the bar, guarded by `parent.activeInHierarchy` (inventory actually open). Right-stick cursor makes the buttons clickable on pad — this is why force-visible is a feature. |
| Resolution | Unity's CanvasScaler already handles it. The Screen.width scaling pass (first v1.2.1) was WRONG and reverted. Fixed reference-resolution sizes only. Keep `childForceExpandWidth/Height = false` on the bar HLG. |

## Hard safety rules (each one bought with blood)

1. **NEVER `PlayerSync.SendAttemptWaypoint`** — server validates you're standing on a waypoint; otherwise force-disconnect ("i think ehg is onto me lol").
2. **Teleports must be gated on waypoint unlock state** — exactly like the map's own node click; firing unlocked teleports soft-locked a character in the Bazaar (frozen movement, all waypoints locked).
3. **`_travelInProgress` concurrency guard** — concurrent travel coroutines (the mod once click-matched its OWN button searching 3,671 Buttons for "OBSERVATORY") caused frame collapse bad enough to auto-open EHG's F8 bug reporter. Never search button text without excluding `medick_*` objects; require exact intent.
4. **Snapshot-and-restore any temporary activation** — see primer regression.
5. **CS1626**: no `yield return` inside try/catch — isolate risky calls in their own try blocks that assign locals.
6. **Destroy `LocalizeStringEvent`** on any cloned label or the game's localization overwrites custom text; destroy cloned behaviour components (`SortInventoryButton`); wire clicks with explicit `new Action(...)` for IL2CPP delegate marshaling.
7. Deploy script must not print "Deployed" after a failed build (the `&&` chain once copied a stale DLL).

## Why v1 looks "slapped on top" (the aesthetic post-mortem)

- Teleport buttons are **100% hand-drawn flat rectangles**: new GameObject + flat-color `Image` + scratch TMP label. Zero sprites, zero 9-slices. KG's mod embedded base64 PNG art; v1 embedded nothing.
- The "gold border" is two stacked flat rectangles (gold root Image + inset BG child) faking it — no filigree, no rounded corners.
- The font is scavenged: `FindObjectOfType<TMP_Text>().font` — literally the first font found in the scene. Game font lacks Unicode glyphs (▼→□), hence ASCII `v`/`>` arrows.
- Footer buttons clone the native Sort button (good!) then paint flat saturated green/red/gold over its styled background sprite (bad — kills the native skin).
- The mod resizes EHG's own Transfer/Sort buttons (55×22) and squeezes/mutates the currency row (`TryCompactCurrency` — a shipped debug session that logs the whole footer hierarchy every inventory open).
- Layout was tuned by screenshot pixel-archaeology ("move up half a box"), anchored top-left with magic offsets; the column bleeds outside the panel via `COL_X = -162` (works because the panel doesn't mask — fragile assumption).
- Collapse layout is a hand-rolled `Reflow()` over a static item list — no LayoutGroups.

**v2 design pivot: nothing hand-painted. Every visual element is cloned or
harvested from the game's own UI** — native sprites (9-slice from the game's
buttons), native font + material (inherited by cloning a native label),
native hover transitions (the cloned Button's own ColorBlock). Faction color
coding survives as label/accent tinting, not whole-button flat fills.

## Recovered wishes & deferred items (v2 candidates)

- **"allow the users to select what they want showing"** (April, dropped) — per-button/per-menu visibility config → native settings category (taxonomy: toggles = real controls)
- Controller users: vanilla Y-on-dungeon-key teleports; faction-hub shortcuts praised; small-screen readability explicitly appreciated on Nexus
- Currency-row compacting was a user wish that was never properly finished — decide: do it right or drop it (v1's half-attempt ships as debug noise)
- Andrew's stated bar: faction colors for instant identification, Deck-touch-sized targets, "look native"; STASH ALL is the flagship ("everyone is going to be impressed")
- Nexus framing: honest gray-area warning for online use; "not teleport hacks — waypoint must be unlocked"

## v1 file map (pre-rebuild)

- `Main.cs` (2.3KB) — MelonMod, OnLateUpdate keep-alive (comments still say "OnUpdate" — stale)
- `InventoryButtonsPatch.cs` (15.4KB) — footer buttons + StashAllCoroutine + TryCompactCurrency debug fossil + LogHierarchy
- `TeleportButtonsPatch.cs` (35.5KB) — scratch-built teleport column, collapse Reflow, era primer, travel coroutine
- No preferences of any kind. Top-level patch classes. Internal name `medick_Advanced_Inventory` / assembly per csproj (verify before release; DLL filename gotcha from Tooltips applies).
