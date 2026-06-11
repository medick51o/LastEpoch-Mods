using System;
using Il2CppLE.UI.Minimap;
using MelonLoader;
using UnityEngine;

namespace medick_FogOfWar
{
    // Owns everything written to the game's Minimap.
    //
    // v2 rule: EVERYTHING applies live — including BLIND's minimap hide,
    // which in v1 required a full game restart in both directions (a tooling
    // limitation that shipped as a documented warning). The hidden GameObject
    // is remembered so leaving BLIND restores it instantly.
    internal static class FogController
    {
        static Minimap    _minimap;        // current zone's instance
        static GameObject _hiddenTarget;   // what BLIND hid (restored on leave)

        // Captured fresh on every Minimap.Awake — zones can differ, and a
        // global once-only latch goes stale (lesson from the sibling mods).
        public static float ZoneDefaultRadius { get; private set; } = -1f;

        public static void OnMinimapAwake(Minimap minimap)
        {
            if (minimap == null) return;
            _minimap = minimap;
            try
            {
                float pre = minimap.RevealRadius;   // pre-write prefab value
                if (pre > 0f && Math.Abs(pre - ZoneDefaultRadius) > 0.01f)
                {
                    ZoneDefaultRadius = pre;
                    Dbg.Log($"zone default RevealRadius captured: {pre:F0}");
                }
            }
            catch { }
            Apply();
        }

        // Idempotent; safe to call any time (settings click, zone load,
        // login screen). Dead or absent minimap → no-op, next zone applies.
        public static void Apply()
        {
            var m = _minimap;
            bool alive;
            try { alive = m != null; } catch { alive = false; }
            if (!alive) return;

            var level = Prefs.FogLevel.Value;
            try
            {
                if (FogLevels.HidesMinimap(level)) HideMinimap(m);
                else                               ShowMinimap();

                float radius = FogLevels.RadiusFor(level, ZoneDefaultRadius);
                m.RevealRadius = radius;
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
            try
            {
                var m = _minimap;
                if (m != null && ZoneDefaultRadius > 0f)
                    m.RevealRadius = ZoneDefaultRadius;
            }
            catch { }
            ShowMinimap();
        }

        // Hide scope: the OUTERMOST ancestor whose name contains "minimap"
        // (case-insensitive), falling back to the Minimap's own GameObject.
        // v1 also matched "hud" and "corner" while walking up — one rename
        // away from deactivating the entire HUD. Never again.
        static void HideMinimap(Minimap minimap)
        {
            try
            {
                if (_hiddenTarget != null && !_hiddenTarget.activeSelf)
                    return;                              // already hidden
            }
            catch { _hiddenTarget = null; }              // stale reference

            try
            {
                Transform best = null;
                for (Transform t = minimap.transform; t != null; t = t.parent)
                    if (t.name.IndexOf("minimap", StringComparison.OrdinalIgnoreCase) >= 0)
                        best = t;

                var target = best != null ? best.gameObject : minimap.gameObject;
                target.SetActive(false);
                _hiddenTarget = target;
                Dbg.Log("BLIND — minimap hidden: " + target.name);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("minimap hide failed: " + ex.Message);
            }
        }

        static void ShowMinimap()
        {
            try
            {
                if (_hiddenTarget != null && !_hiddenTarget.activeSelf)
                {
                    _hiddenTarget.SetActive(true);
                    Dbg.Log("minimap restored: " + _hiddenTarget.name);
                }
            }
            catch { }
            _hiddenTarget = null;
        }
    }
}
