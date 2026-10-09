using System;
using System.Collections;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace medick_Terrible_Inventory
{
    // The hardened teleport engine. History (see ARCHAEOLOGY.md):
    //  • NEVER PlayerSync.SendAttemptWaypoint — the server force-disconnects
    //    unless you are physically standing on a waypoint.
    //  • LoadWaypointScene() on a UIWaypointStandard from a controller's
    //    waypointsInMenu IS the game's own waypoint-click path and routes
    //    correctly online and offline.
    //  • Each of the 5 era UIWaypointControllers must have OnEnable fired
    //    once per session before travel works — the silent primer below
    //    (Andrew's own fix) handles that invisibly.
    //  • v1's fallback chain (map-flash + era-tab text-click + searching all
    //    buttons for "VISIT X") is DELETED. Prime-then-retry replaces it.
    internal static class TravelService
    {
        static bool _travelInProgress;
        static bool _primed;
        static int _primeGen;
        static bool _primerRunning;
        static bool _unlockUnreadableWarned;
        static readonly HashSet<string> _warnedScenes = new();

        public static void EnsurePrimed()
        {
            if (_primed || _primerRunning) return;
            _primerRunning = true;
            MelonCoroutines.Start(PrimeCoroutine());
        }

        // The travel guard spans the whole scene transition (safety rule #3:
        // concurrent travel once summoned EHG's bug reporter) — it is cleared
        // here on scene load, with a timeout failsafe inside TravelCoroutine.
        public static void NotifySceneLoaded(string sceneName)
        {
            _travelInProgress = false;
            string l = (sceneName ?? "").ToLower();
            if (l.Contains("character") || l.Contains("login") || l.Contains("menu"))
            {
                _armedScenes.Clear();
                _primed = false;   // a new character may bring new controllers (review, Codex)
                _primeGen++;       // a primer still running from the old session must not re-latch
            }
        }

        static readonly HashSet<string> _armedScenes = new HashSet<string>();

        public static void RequestTravel(string scene)
        {
            if (_travelInProgress)
            {
                Dbg.Log("travel already in progress — click ignored");
                return;
            }
            MelonCoroutines.Start(TravelCoroutine(scene, ++_travelOp));
        }

        // ── Travel ────────────────────────────────────────────────

        // Each request gets an operation id; only the CURRENT operation may release the
        // guard, so a stale coroutine (its scene already loaded) can never unlock a newer
        // teleport mid-load (review, Codex 2026-10-08).
        static int _travelOp;

        static IEnumerator TravelCoroutine(string scene, int op)
        {
            _travelInProgress = true;
            Dbg.Log($"travel requested: '{scene}'");

            EnsurePrimed();
            float waited = 0f;
            while (_primerRunning && waited < 10f)
            {
                yield return new WaitForSeconds(0.25f);
                waited += 0.25f;
            }

            // One scene scan per click, shared by the gate and the lookup
            // (FindObjectsOfType over a big town scene is hitch-prone on Deck).
            UIWaypointController[] controllers = FindControllers();

            // Unlock gate — behave exactly like the map's own locked node:
            // not unlocked → do nothing, leave NO game-state footprint.
            if (!IsUnlocked(controllers, scene))
            {
                MelonLogger.Msg($"'{scene}' is not an unlocked waypoint for this character — ignoring");
                if (op == _travelOp) _travelInProgress = false;
                yield break;
            }

            UIWaypointStandard wp = FindWaypointForScene(controllers, scene);

            // SPEC travel rule 4: waypoint miss → re-run the primer ONCE,
            // retry once (a controller can miss its OnEnable, or a relog can
            // re-instantiate controllers the latched prime never saw).
            if (wp == null)
            {
                Dbg.Log($"'{scene}' not found — re-priming once");
                _primed = false;
                EnsurePrimed();
                float w2 = 0f;
                while (_primerRunning && w2 < 10f)
                {
                    yield return new WaitForSeconds(0.25f);
                    w2 += 0.25f;
                }
                controllers = FindControllers();
                wp = FindWaypointForScene(controllers, scene);
            }

            if (wp == null)
            {
                if (_warnedScenes.Add(scene))   // once per scene per session
                    MelonLogger.Warning($"waypoint '{scene}' not found after re-priming — travel unavailable here");
                if (op == _travelOp) _travelInProgress = false;
                yield break;
            }

            // Since the Oct 2026 season a jump is ignored until the real map
            // has been opened on the target's section (verified in-hand
            // 2026-10-08: open+close arms it; a one-frame MapPanel enable does
            // not). Open and close it through the game's own openMap, once
            // per target per login.
            if (!_armedScenes.Contains(scene))
            {
                bool opened = false;
                bool viaKey = false;
                UIBase ui = null;
                try { ui = UIBase.instance; } catch { }
                if (ui == null) Dbg.Log("map arm: UIBase.instance is null");
                else
                {
                    // The interop wrapper rejects null for the Nullable<> params
                    // (Il2CppObjectBaseToPtrNotNull), so pass empty instances.
                    try
                    {
                        // Zoom to the TARGET so the game builds that era's map
                        // section; arming only the current era left Bazaar/
                        // Observatory ignored (log 2026-10-08 18:49-18:50).
                        ui.openMap(false, false, new Il2CppSystem.Nullable<int>(), scene, null,
                                   new Il2CppSystem.Nullable<TimelineID>(), false);
                        opened = true;
                    }
                    catch (Exception e)
                    {
                        Dbg.Log($"map arm: openMap failed ({e.Message}) — trying the map key path");
                        try { ui.MapKeyDown(); opened = true; viaKey = true; }
                        catch (Exception e2) { Dbg.Log($"map arm: MapKeyDown failed: {e2.Message}"); }
                    }
                }
                if (opened)
                {
                    yield return null;
                    yield return null;
                    bool closed = false;
                    try { if (viaKey) ui.MapKeyDown(); else ui.closeMap(); closed = true; }
                    catch (Exception e) { Dbg.Log($"map arm: closeMap failed: {e.Message}"); }
                    yield return null;
                    // Only a focused open AND a confirmed close counts as armed
                    // (the key path opens only the current section).
                    if (!viaKey && closed) _armedScenes.Add(scene);
                    Dbg.Log($"map arm: real map opened on '{scene}' via {(viaKey ? "MapKeyDown" : "openMap")}, " +
                            (closed ? "closed" : "CLOSE FAILED — not armed"));
                }
            }

            // Council A2: use the verified waypoint-click path without mutating WaypointManager state.
            bool fired = false;
            try
            {
                wp.LoadWaypointScene();
                fired = true;
                if (Prefs.DebugLog != null && Prefs.DebugLog.Value)
                    Dbg.Log($"travel → '{scene}' from '{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}' " +
                            $"(waypointEnabled={SafeWaypointEnabled()}, wpActive={wp.gameObject.activeInHierarchy}, mapPanelLinked={SafeMapPanelLinked(wp)})");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"travel to '{scene}' failed: {e.Message}");
            }

            if (!fired)
            {
                if (op == _travelOp) _travelInProgress = false;
                yield break;
            }

            // Hold the guard across the transition; NotifySceneLoaded clears it
            // on arrival. Phase 1 (≤20s): wait for arrival or for the game's
            // transition to start. Phase 2: once a transition is running, hold
            // until arrival — never release mid-load on a timer (review,
            // Codex) — with a 120s emergency release so the buttons can never
            // stay dead. If the transition flag never reads true, phase 1
            // behaves exactly like the old 20s timeout.
            float startWait = 0f;
            bool started = false;
            while (_travelInProgress && op == _travelOp && startWait < 20f)
            {
                if (TransitionRunning()) { started = true; break; }
                yield return new WaitForSeconds(0.25f);
                startWait += 0.25f;
            }
            if (started)
            {
                Dbg.Log($"travel to '{scene}': transition started after {startWait:0.00}s — holding until arrival");
                float held = 0f;
                while (_travelInProgress && op == _travelOp && held < 120f)
                {
                    yield return new WaitForSeconds(0.5f);
                    held += 0.5f;
                }
                if (_travelInProgress && op == _travelOp)
                    MelonLogger.Warning($"travel to '{scene}': transition running 120s with no arrival — releasing the travel guard");
            }
            else if (_travelInProgress && op == _travelOp)
            {
                _armedScenes.Remove(scene);   // next click re-opens the map on it
                Dbg.Log($"travel to '{scene}': no transition and no scene load within 20s — the game ignored the jump; map will be re-armed");
            }
            if (op == _travelOp) _travelInProgress = false;
        }

        static bool TransitionRunning()
        {
            try { return TransitionSceneManager.IsActive; }
            catch { return false; }
        }

        static string SafeMapPanelLinked(UIWaypointStandard wp)
        {
            try { return (wp._mapPanel != null).ToString(); }
            catch (Exception e) { return "unreadable(" + e.GetType().Name + ")"; }
        }

        static string SafeWaypointEnabled()
        {
            try { return WaypointManager.WaypointIsEnabled().ToString(); }
            catch (Exception e) { return "unreadable(" + e.GetType().Name + ")"; }
        }

        static UIWaypointController[] FindControllers()
        {
            try { return UnityEngine.Object.FindObjectsOfType<UIWaypointController>(true); }
            catch { return null; }
        }

        // ── Unlock gate ───────────────────────────────────────────
        // True only when a player-side unlock list or an era controller
        // positively lists the scene as unlocked. Since the Oct 2026 season
        // the controllers' unlockedScenes stay empty, so the player-side
        // lists (PlayerUnlockSources) are checked first.

        static bool IsUnlocked(UIWaypointController[] all, string scene)
        {
            // Any interop fault while reading lists refuses travel instead of
            // faulting the coroutine with the travel guard held (review, Codex).
            try { return IsUnlockedCore(all, scene); }
            catch (Exception e)
            {
                Dbg.Log($"unlock check failed: {e.GetType().Name}: {e.Message} — travel refused");
                return false;
            }
        }

        static bool IsUnlockedCore(UIWaypointController[] all, string scene)
        {
            // The live player list is decisive when readable — a stale cached
            // list must never authorize a scene the live list excludes.
            var live = LiveUnlockList();
            if (live != null)
            {
                for (int i = 0; i < live.Count; i++)
                    if ((live[i] ?? "") == scene) return true;
                DumpUnlockState(all, scene);
                return false;
            }

            // Live list unreadable → travel unavailable. The era controllers'
            // own lists are no longer populated this season and could carry a
            // prior character's state, so they never authorize (review, Codex).
            if (!_unlockUnreadableWarned)
            {
                _unlockUnreadableWarned = true;
                Dbg.Log("live unlock list unreadable — travel refused");
            }
            DumpUnlockState(all, scene);
            return false;
        }

        static Il2CppSystem.Collections.Generic.List<string> LiveUnlockList()
        {
            try
            {
                LocalPlayerInfoProvider info;
                return PlayerFinder.TryGetLocalPlayerInfo(out info) ? info?.getUnlockedScenes() : null;
            }
            catch { return null; }
        }

        // Player-side unlock lists for the DebugLog dump only (the gate uses
        // LiveUnlockList alone), live source first. Each read is isolated
        // so one moved/renamed field cannot hide the others.
        static List<(string name, Il2CppSystem.Collections.Generic.List<string> list)> PlayerUnlockSources()
        {
            var r = new List<(string, Il2CppSystem.Collections.Generic.List<string>)>();
            try
            {
                LocalPlayerInfoProvider info;
                r.Add(("LocalPlayerInfo", PlayerFinder.TryGetLocalPlayerInfo(out info) ? info?.getUnlockedScenes() : null));
            }
            catch { r.Add(("LocalPlayerInfo(read failed)", null)); }
            try { r.Add(("CharacterData", PlayerFinder.getPlayerData()?.UnlockedWaypointScenes)); }
            catch { r.Add(("CharacterData(read failed)", null)); }
            try { r.Add(("DataTracker.charData", PlayerFinder.getPlayerDataTracker()?.charData?.UnlockedWaypointScenes)); }
            catch { r.Add(("DataTracker.charData(read failed)", null)); }
            return r;
        }

        // Diagnostic (DebugLog only): on a refused unlock, print what every
        // controller actually lists, so a renamed scene or an empty list
        // shows up in one click instead of a guess.
        static void DumpUnlockState(UIWaypointController[] all, string scene)
        {
            if (Prefs.DebugLog == null || !Prefs.DebugLog.Value || all == null) return;
            Dbg.Log($"unlock dump for '{scene}': {all.Length} controllers");
            foreach (var src in PlayerUnlockSources())
            {
                try
                {
                    var names = new List<string>();
                    if (src.list != null)
                        for (int i = 0; i < src.list.Count; i++) names.Add(src.list[i] ?? "<null>");
                    Dbg.Log($"  {src.name}[{(src.list == null ? "null" : names.Count.ToString())}]=" + string.Join(",", names));
                }
                catch (Exception e) { Dbg.Log($"  {src.name} read failed: {e.GetType().Name}"); }
            }
            foreach (UIWaypointController ctrl in all)
            {
                try
                {
                    var unlocked = ctrl.unlockedScenes;
                    var names = new List<string>();
                    if (unlocked != null)
                        for (int i = 0; i < unlocked.Count; i++) names.Add(unlocked[i] ?? "<null>");
                    var menu = new List<string>();
                    int m = ctrl.waypointsInMenu?.Count ?? 0;
                    for (int i = 0; i < m; i++)
                    {
                        UIWaypointStandard w = ctrl.waypointsInMenu[i]?.TryCast<UIWaypointStandard>();
                        if (w != null) menu.Add(w.sceneName ?? "<null>");
                    }
                    Dbg.Log($"  {ctrl.gameObject.name} active={ctrl.gameObject.activeInHierarchy} " +
                            $"unlocked[{names.Count}]=" + string.Join(",", names) +
                            $" | menu[{menu.Count}]=" + string.Join(",", menu));
                }
                catch (Exception e)
                {
                    Dbg.Log($"  controller read failed: {e.GetType().Name}: {e.Message}");
                }
            }
        }

        // ── Waypoint lookup ───────────────────────────────────────
        // Search ALL controllers — FindObjectOfType (singular) was v1's
        // original only-End-of-Time-works bug.

        static UIWaypointStandard FindWaypointForScene(UIWaypointController[] all, string targetScene)
        {
            if (all == null) return null;
            foreach (UIWaypointController ctrl in all)
            {
                int count = 0;
                try { count = ctrl.waypointsInMenu?.Count ?? 0; } catch { }
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        UIWaypointStandard w = ctrl.waypointsInMenu[i]?.TryCast<UIWaypointStandard>();
                        if (w != null && (w.sceneName ?? "") == targetScene)
                            return w;
                    }
                    catch { }
                }
            }
            return null;
        }

        // ── Silent era-controller primer (Andrew's fix, v1.3.0) ───
        // Snapshot the activeSelf of each controller's FULL ancestor chain,
        // activate root→leaf so OnEnable fires, one frame, restore the EXACT
        // snapshot. Forcing false afterwards once wiped the world map empty —
        // snapshot-restore is law.

        static IEnumerator PrimeCoroutine()
        {
            int gen = _primeGen;
            float waited = 0f;
            while (waited < 30f)
            {
                bool inGame = false;
                try
                {
                    string s = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "";
                    string l = s.ToLower();
                    inGame = s.Length > 0 && !l.Contains("loading") && !l.Contains("menu")
                          && !l.Contains("boot") && !l.Contains("splash") && !l.Contains("character")
                          && !l.Contains("login");
                }
                catch { }
                if (inGame) break;
                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }
            yield return new WaitForSeconds(0.5f);

            UIWaypointController[] all = FindControllers();
            if (all == null || all.Length == 0)
            {
                // Don't latch _primed — a later zone may have controllers;
                // EnsurePrimed (every inventory open + travel click) retries.
                Dbg.Log("primer: no era controllers in this scene — will retry later");
                _primerRunning = false;
                yield break;
            }

            Dbg.Log($"primer: activating {all.Length} era controllers silently");

            foreach (UIWaypointController ctrl in all)
            {
                bool wasActive = false;
                var chain = new List<GameObject>();
                try
                {
                    wasActive = ctrl.gameObject.activeSelf;
                    Transform t = ctrl.transform.parent;
                    while (t != null)
                    {
                        if (!t.gameObject.activeSelf) chain.Add(t.gameObject);
                        t = t.parent;
                    }
                }
                catch { continue; }

                chain.Reverse();                                  // root → leaf
                foreach (var go in chain) { try { go.SetActive(true); } catch { } }

                try
                {
                    if (ctrl.gameObject.activeSelf) ctrl.gameObject.SetActive(false);
                    ctrl.gameObject.SetActive(true);              // OnEnable fires here
                }
                catch { }

                yield return null;

                try { ctrl.gameObject.SetActive(wasActive); } catch { }
                chain.Reverse();
                foreach (var go in chain) { try { go.SetActive(false); } catch { } }
            }

            // Latch only if no session boundary happened while we ran.
            _primerRunning = false;
            if (gen == _primeGen)
            {
                _primed = true;
                Dbg.Log("primer: all era controllers primed — teleport ready");
            }
            else Dbg.Log("primer: session changed while priming — not latched, next open re-primes");
        }
    }
}
