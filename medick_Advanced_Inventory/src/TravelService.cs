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
        static bool _primerRunning;
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
        public static void NotifySceneLoaded() => _travelInProgress = false;

        public static void RequestTravel(string scene)
        {
            if (_travelInProgress)
            {
                Dbg.Log("travel already in progress — click ignored");
                return;
            }
            MelonCoroutines.Start(TravelCoroutine(scene));
        }

        // ── Travel ────────────────────────────────────────────────

        static IEnumerator TravelCoroutine(string scene)
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
                _travelInProgress = false;
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
                _travelInProgress = false;
                yield break;
            }

            // Carried from v1, deliberately AFTER the gate and the lookup so a
            // click that doesn't travel leaves no footprint: some zones set
            // WaypointEnabled=false and this allows the jump the way v1 did.
            // A successful travel loads a new scene, which resets the flag.
            try
            {
                WaypointManager wm = WaypointManager.getInstance();
                if (wm != null) { wm.WaypointEnabled = true; wm.EnableWaypoint(); }
            }
            catch { }

            bool fired = false;
            try
            {
                wp.LoadWaypointScene();
                fired = true;
                Dbg.Log($"travel → '{scene}'");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"travel to '{scene}' failed: {e.Message}");
            }

            if (!fired)
            {
                _travelInProgress = false;
                yield break;
            }

            // Hold the guard across the transition; NotifySceneLoaded clears
            // it on arrival, the timeout covers a silently failed load.
            float guard = 0f;
            while (_travelInProgress && guard < 10f)
            {
                yield return new WaitForSeconds(0.5f);
                guard += 0.5f;
            }
            _travelInProgress = false;
        }

        static UIWaypointController[] FindControllers()
        {
            try { return UnityEngine.Object.FindObjectsOfType<UIWaypointController>(true); }
            catch { return null; }
        }

        // ── Unlock gate ───────────────────────────────────────────
        // True when any era controller lists the scene as unlocked. If the
        // unlock data cannot be read at all, default to allow — documented
        // tradeoff; LoadWaypointScene is the game's own gated path anyway.

        static bool IsUnlocked(UIWaypointController[] all, string scene)
        {
            if (all == null || all.Length == 0) return true;
            bool readAnything = false;
            foreach (UIWaypointController ctrl in all)
            {
                try
                {
                    var unlocked = ctrl.unlockedScenes;
                    if (unlocked == null) continue;
                    int n = unlocked.Count;
                    readAnything = true;
                    for (int i = 0; i < n; i++)
                        if ((unlocked[i] ?? "") == scene) return true;
                }
                catch { }
            }
            if (!readAnything)
            {
                Dbg.Log("unlock data unreadable — allowing travel attempt");
                return true;
            }
            return false;
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

            _primed = true;
            _primerRunning = false;
            Dbg.Log("primer: all era controllers primed — teleport ready");
        }
    }
}
