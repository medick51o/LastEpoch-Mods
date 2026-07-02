using System;
using System.Collections.Generic;
using Il2CppLE.UI.Minimap;
using MelonLoader;
using UnityEngine;

namespace medick_FogOfWar
{
    // Owns everything written to the game's Minimap.
    //
    // v2 rule: EVERYTHING applies live — including BLIND's minimap hide,
    // which in v1 required a full game restart in both directions. Every
    // GameObject BLIND hides is remembered; leaving BLIND restores them all,
    // and the restore is NEVER gated on a live minimap instance (leaving
    // BLIND from a loading screen must still un-hide).
    internal static class FogController
    {
        static Minimap _minimap;                            // current zone's instance
        static readonly List<GameObject> _hidden = new();   // everything BLIND hid

        // EVERY radius written this session (capped): pooled minimap
        // instances can alternate, so a single remembered value lets an old
        // mod-written radius smuggle back in as a "zone default" and the
        // LIMITED/SCOUT compounding bug returns through the side door.
        static readonly List<float> _writtenRadii = new();
        const int WrittenCap = 32;
        static bool _restoring;   // ShowHidden in progress — the Update postfix must not react
        static bool _shutdown;    // mod unloading — nothing may re-hide after RestoreDefaults

        static bool WrittenBefore(float v)
        {
            for (int i = 0; i < _writtenRadii.Count; i++)
                if (Math.Abs(_writtenRadii[i] - v) < 0.01f) return true;
            return false;
        }

        // Captured fresh on every Minimap.Awake — zones can differ, and a
        // global once-only latch goes stale (lesson from the sibling mods).
        public static float ZoneDefaultRadius { get; private set; } = -1f;

        // BLIND re-assert, called from the Minimap.Update postfix.
        // Default capture stays Awake-only; this path never adopts values.
        public static void OnMinimapUpdate(Minimap minimap)
        {
            // A minimap receiving Update while BLIND means the game re-activated
            // a container this mod hid (overlay-map close, cutscene end, HUD
            // refresh) — hide it again. Hosted on Update because the interop
            // Minimap declares no OnEnable to hook (verified against
            // Il2CppLE.dll); a hidden minimap receives no Updates, so this
            // costs nothing in BLIND and two cheap checks per frame outside it.
            if (_shutdown || _restoring) return;
            if (!FogLevels.HidesMinimap(Prefs.FogLevel.Value)) return;
            if (minimap == null) return;
            _minimap = minimap;
            Apply();
        }

        public static void OnMinimapAwake(Minimap minimap)
        {
            if (minimap == null) return;
            _minimap = minimap;
            try
            {
                float pre = minimap.RevealRadius;
                // SPEC rule 2 demands the PRE-WRITE value. Enforce it
                // regardless of how the game invokes Awake: a value this mod
                // wrote itself (pooled/cloned/re-awoken instances) is never
                // adopted as a zone default — otherwise LIMITED/SCOUT would
                // compound multiplicatively zone after zone.
                if (pre > 0f
                    && !WrittenBefore(pre)
                    && Math.Abs(pre - ZoneDefaultRadius) > 0.01f)
                {
                    ZoneDefaultRadius = pre;
                    Dbg.Log($"zone default RevealRadius captured: {pre:F0}");
                }
            }
            catch { }
            Apply();
        }

        // Idempotent; safe to call any time (settings click, zone load,
        // login screen, loading screen).
        public static void Apply()
        {
            var level = Prefs.FogLevel.Value;

            // Restore needs only the remembered objects — never gate it on
            // minimap aliveness, or leaving BLIND between zones strands the
            // hidden minimap until restart (the v1 disease, reincarnated).
            if (!FogLevels.HidesMinimap(level)) ShowHidden();

            var m = _minimap;
            bool alive;
            try { alive = m != null; } catch { alive = false; }
            if (!alive) return;

            try
            {
                if (FogLevels.HidesMinimap(level)) Hide(m);
                float radius = FogLevels.RadiusFor(level, ZoneDefaultRadius);
                m.RevealRadius = radius;
                // Record MODIFIED radii only: NORMAL writes the zone default
                // back, and recording that would poison the capture guard
                // against every genuine default we've ever visited (towns!) —
                // the hunt fleet caught that regression before it shipped.
                if (Math.Abs(radius - ZoneDefaultRadius) > 0.01f && !WrittenBefore(radius))
                {
                    if (_writtenRadii.Count >= WrittenCap) _writtenRadii.RemoveAt(0);
                    _writtenRadii.Add(radius);
                }
                Dbg.Log($"applied {level} — RevealRadius {radius:F0}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("apply failed: " + ex.Message);
            }
        }

        // Best-effort vanilla restore (mod unload).
        public static void RestoreDefaults()
        {
            _shutdown = true;   // the Update postfix may outlive this call
            try
            {
                var m = _minimap;
                if (m != null && ZoneDefaultRadius > 0f)
                    m.RevealRadius = ZoneDefaultRadius;
            }
            catch { }
            ShowHidden();
        }

        // Hide scope: the OUTERMOST ancestor whose name contains "minimap"
        // (case-insensitive), falling back to the Minimap's own GameObject.
        // v1 also matched "hud" and "corner" while walking up — one rename
        // away from deactivating the entire HUD. Never again.
        //
        // The hidden set is judged per incoming target, not against a single
        // remembered object — a BLIND→BLIND zone change with overlapping
        // minimap lifetimes hides the new zone's minimap too.
        static void Hide(Minimap minimap)
        {
            try
            {
                Transform best = null;
                for (Transform t = minimap.transform; t != null; t = t.parent)
                    if (t.name.IndexOf("minimap", StringComparison.OrdinalIgnoreCase) >= 0)
                        best = t;
                var target = best != null ? best.gameObject : minimap.gameObject;

                foreach (var known in _hidden)
                {
                    try
                    {
                        if (known == target)
                        {
                            if (target.activeSelf) target.SetActive(false);
                            return;                  // already tracked
                        }
                    }
                    catch { }                        // collected entry — ignore
                }

                // Never adopt something the mod didn't hide: a game-parked
                // (pooled, inactive) target would get force-activated by the
                // next ShowHidden — a ghost minimap resurrected from the pool.
                if (!target.activeSelf) return;

                target.SetActive(false);
                _hidden.Add(target);
                Dbg.Log("BLIND — minimap hidden: " + target.name);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("minimap hide failed: " + ex.Message);
            }
        }

        static void ShowHidden()
        {
            _restoring = true;   // our SetActive(true) can fire Awake on
                                 // never-awoken children (Minimap has no
                                 // OnEnable); the latch keeps our own restore
                                 // from being mistaken for game activity
            try
            {
                for (int i = _hidden.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        var go = _hidden[i];
                        if (go != null && !go.activeSelf)
                        {
                            go.SetActive(true);
                            Dbg.Log("minimap restored: " + go.name);
                        }
                    }
                    catch { }                        // dead object — nothing to restore
                }
                _hidden.Clear();
            }
            finally { _restoring = false; }
        }
    }
}
