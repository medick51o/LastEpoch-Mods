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
    //    (Andrew's own fix) handles that invisibly during the loading screen.
    //  • v1's fallback chain (map-flash + era-tab text-click + searching all
    //    buttons for "VISIT X") is DELETED — it once invoked the mod's own
    //    button and melted the frame rate badly enough to summon EHG's bug
    //    reporter. Prime-then-retry replaces all of it.
    internal static class TravelService
    {
        static bool _travelInProgress;
        static bool _primed;
        static bool _primerRunning;

        public static void EnsurePrimed()
        {
            if (_primed || _primerRunning) return;
            _primerRunning = true;
            MelonCoroutines.Start(PrimeCoroutine());
        }

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

            // Make sure the controllers are primed before judging anything.
            EnsurePrimed();
            float waited = 0f;
            while (_primerRunning && waited < 10f)
            {
                yield return new WaitForSeconds(0.25f);
                waited += 0.25f;
            }

            // The game's flag for "waypoint use allowed here".
            try
            {
                WaypointManager wm = WaypointManager.getInstance();
                if (wm != null) { wm.WaypointEnabled = true; wm.EnableWaypoint(); }
            }
            catch { }

            // Unlock gate — behave exactly like the map's own locked node:
            // not unlocked → do nothing. (Safety rule #2: an ungated teleport
            // once soft-locked a fresh character in the Bazaar.)
            if (!IsUnlocked(scene))
            {
                MelonLogger.Msg($"'{scene}' is not an unlocked waypoint for this character — ignoring");
                _travelInProgress = false;
                yield break;
            }

            UIWaypointStandard wp = FindWaypointForScene(scene);
            if (wp == null)
            {
                MelonLogger.Warning($"waypoint '{scene}' not found after priming — travel unavailable this session");
                _travelInProgress = false;
                yield break;
            }

            try
            {
                wp.LoadWaypointScene();
                Dbg.Log($"travel → '{scene}'");
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"travel to '{scene}' failed: {e.Message}");
            }
            _travelInProgress = false;
        }

        // ── Unlock gate ───────────────────────────────────────────
        // True when any era controller lists the scene as unlocked.
        // If the unlock data cannot be read at all, default to allow —
        // LoadWaypointScene is the game's own gated path regardless.

        static bool IsUnlocked(string scene)
        {
            bool readAnything = false;
            try
            {
                UIWaypointController[] all = UnityEngine.Object.FindObjectsOfType<UIWaypointController>(true);
                if (all == null || all.Length == 0) return true;

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
            }
            catch { return true; }

            if (!readAnything)
            {
                Dbg.Log("unlock data unreadable — allowing travel attempt");
                return true;
            }
            return false;
        }

        // ── Waypoint lookup ───────────────────────────────────────
        // All 5 era controllers (Ancient/Divine/Imperial/Ruined/EoT) hold
        // waypointsInMenu populated at scene load; search them all —
        // FindObjectOfType (singular) was v1's original only-EoT-works bug.

        static UIWaypointStandard FindWaypointForScene(string targetScene)
        {
            try
            {
                UIWaypointController[] all = UnityEngine.Object.FindObjectsOfType<UIWaypointController>(true);
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
            }
            catch (Exception e)
            {
                MelonLogger.Warning("waypoint search failed: " + e.Message);
            }
            return null;
        }

        // ── Silent era-controller primer (Andrew's fix, v1.3.0) ───
        // For each controller: snapshot the activeSelf of its FULL ancestor
        // chain, activate root→leaf so OnEnable fires, give it a frame, then
        // restore the EXACT snapshot. Forcing everything false afterwards
        // once wiped the world map empty — snapshot-restore is law.

        static IEnumerator PrimeCoroutine()
        {
            // Wait for a real playable scene (not boot/login/character select).
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
            yield return new WaitForSeconds(0.5f);   // let the scene settle

            UIWaypointController[] all = null;
            try { all = UnityEngine.Object.FindObjectsOfType<UIWaypointController>(true); } catch { }

            if (all == null || all.Length == 0)
            {
                // Don't latch _primed — a later zone may have controllers;
                // EnsurePrimed (called on every inventory open and travel
                // click) will retry there.
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

                yield return null;                                // one frame for OnEnable

                try { ctrl.gameObject.SetActive(wasActive); } catch { }
                chain.Reverse();                                  // leaf → root
                foreach (var go in chain) { try { go.SetActive(false); } catch { } }
            }

            _primed = true;
            _primerRunning = false;
            Dbg.Log("primer: all era controllers primed — teleport ready");
        }
    }
}
