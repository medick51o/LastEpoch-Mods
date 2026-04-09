// ================================================================
//  medick_CameraZoom  v1.2
//  End key = toggle settings panel
//
//  How Last Epoch zoom actually works (verified by IL2Cpp dump)
//  ─────────────────────────────────────────────────────────────
//  Il2Cpp.CameraManager owns all camera state.  The fields we
//  actually need (confirmed from Il2CppLE.dll reflection):
//
//   zoomMin           – most-zoomed-out limit  (default ≈ -15)
//   zoomDefault       – where the camera starts  (read-only ref)
//   zoomPerScroll     – distance change per scroll notch
//   zoomSpeed         – how fast the camera lerps to targetZoom
//   targetZoom        – where the camera wants to be (writable!)
//   currentZoom       – where it is right now (writable)
//   cameraAngleMin/Max/Default – tilt angle clamps
//
//  There is NO zoomMax field.  Setting Camera.main.fieldOfView
//  has zero lasting effect — CameraManager overrides it every frame.
//
//  Controls
//  ─────────────────────────────────────────────────────────────
//  • Scroll wheel  — zoom in / out  (game handles natively)
//  • End key       — toggle settings panel
//  • Settings panel live "Current Zoom" slider jumps camera instantly
// ================================================================

using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(medick_CameraZoom.CameraZoomMod),
    "medick_CameraZoom", "1.2.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_CameraZoom
{
    public class CameraZoomMod : MelonMod
    {
        // ── Preferences ───────────────────────────────────────────
        //  zoomMin      : how far out the scroll wheel can go
        //  zoomPerScroll: sensitivity (game default ≈ 1–2 per notch)
        //  zoomSpeed    : lerp speed (game default ≈ 5–10)
        //  lockAngle    : whether to lock the camera tilt
        //  angle        : locked tilt angle in degrees
        internal static MelonPreferences_Entry<float> PrefZoomMin;
        internal static MelonPreferences_Entry<float> PrefZoomPerScroll;
        internal static MelonPreferences_Entry<float> PrefZoomSpeed;
        internal static MelonPreferences_Entry<bool>  PrefLockAngle;
        internal static MelonPreferences_Entry<float> PrefAngle;
        internal static MelonPreferences_Entry<float> PrefMenuScale;

        // ── Originals captured on first CameraManager access ─────
        internal static float OrigZoomMin      = float.NaN;
        internal static float OrigZoomDefault  = float.NaN;
        internal static float OrigZoomPerScroll= float.NaN;
        internal static float OrigZoomSpeed    = float.NaN;
        internal static float OrigAngleDef     = float.NaN;
        internal static bool  _capturedOrig    = false;

        // ── Settings panel state ──────────────────────────────────
        static bool    _showSettings;
        static Rect    _settRect = new(20, 60, 360, 30);
        static bool    _dragging;
        static Vector2 _dragOff;
        static bool    _stylesReady;
        static GUIStyle _lbl, _bold, _left, _small;

        // ── Init ──────────────────────────────────────────────────
        public override void OnInitializeMelon()
        {
            var cat = MelonPreferences.CreateCategory("medick_CameraZoom");
            // zoomMin default -40 matches the original kg_CameraZoom "enhanced" value
            PrefZoomMin      = cat.CreateEntry("ZoomMin",       -40f,  "Zoom-out limit. More negative = further out. Game default ≈ -15");
            PrefZoomPerScroll= cat.CreateEntry("ZoomPerScroll",  3f,   "How many units zoom changes per scroll notch. Game default ≈ 1-2");
            PrefZoomSpeed    = cat.CreateEntry("ZoomSpeed",      10f,  "Camera lerp speed to target zoom. Game default ≈ 5-8");
            PrefLockAngle    = cat.CreateEntry("LockAngle",      false,"Lock camera viewing angle (prevents tilting)");
            PrefAngle        = cat.CreateEntry("Angle",          55f,  "Camera tilt angle when locked (degrees). Original game default ≈ 52");
            PrefMenuScale    = cat.CreateEntry("MenuScale",      1.0f, "Settings panel scale");

            MelonLogger.Msg("medick_CameraZoom v1.2  |  Scroll = zoom  |  End = settings");
        }

        // ─────────────────────────────────────────────────────────
        //  OnUpdate — applies our settings to CameraManager every
        //  frame so live slider changes are instant, no scene reload
        // ─────────────────────────────────────────────────────────
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.End))
                _showSettings = !_showSettings;

            try
            {
                var mgr = CameraManager.instance;
                if (mgr == null) return;

                // ── Capture originals once ────────────────────────
                if (!_capturedOrig)
                {
                    _capturedOrig     = true;
                    OrigZoomMin       = mgr.zoomMin;
                    OrigZoomDefault   = mgr.zoomDefault;
                    OrigZoomPerScroll = mgr.zoomPerScroll;
                    OrigZoomSpeed     = mgr.zoomSpeed;
                    OrigAngleDef      = mgr.cameraAngleDefault;
                    MelonLogger.Msg(
                        $"[CameraZoom] Originals captured — " +
                        $"zoomMin:{OrigZoomMin:F2}  zoomDefault:{OrigZoomDefault:F2}  " +
                        $"perScroll:{OrigZoomPerScroll:F2}  speed:{OrigZoomSpeed:F2}  " +
                        $"angle:{OrigAngleDef:F2}");
                }

                // ── Apply zoom range & sensitivity every frame ────
                mgr.zoomMin      = PrefZoomMin.Value;
                mgr.zoomPerScroll= PrefZoomPerScroll.Value;
                mgr.zoomSpeed    = PrefZoomSpeed.Value;

                // Keep targetZoom from going below our new zoomMin
                if (mgr.targetZoom < PrefZoomMin.Value)
                    mgr.targetZoom = PrefZoomMin.Value;

                // ── Apply angle lock ──────────────────────────────
                if (PrefLockAngle.Value)
                {
                    float a = PrefAngle.Value;
                    mgr.cameraAngleDefault = a;
                    mgr.cameraAngleMin     = a;
                    mgr.cameraAngleMax     = a;
                }
                else if (_capturedOrig)
                {
                    mgr.cameraAngleDefault = OrigAngleDef;
                    mgr.cameraAngleMin     = OrigAngleDef - 25f;
                    mgr.cameraAngleMax     = OrigAngleDef + 25f;
                }
            }
            catch { /* CameraManager not yet active */ }
        }

        // ─────────────────────────────────────────────────────────
        //  OnGUI
        // ─────────────────────────────────────────────────────────
        public override void OnGUI()
        {
            if (!_stylesReady) BuildStyles();
            if (_showSettings) DrawSettings();
        }

        // ─────────────────────────────────────────────────────────
        //  Settings panel
        // ─────────────────────────────────────────────────────────
        static void DrawSettings()
        {
            HandleDrag();
            float sc  = Mathf.Clamp(PrefMenuScale.Value, 0.7f, 2.0f);
            float w   = 360f * sc;
            float hdr = 26f  * sc;
            float sep = 8f   * sc;

            // ── Read live values ──────────────────────────────────
            float liveZoomMin  = float.NaN, liveCur = float.NaN,
                  liveTgt      = float.NaN, livePerScroll = float.NaN,
                  liveSpeed    = float.NaN, liveAngle = float.NaN;
            bool  hasMgr       = false;
            CameraManager mgr  = null;
            try
            {
                mgr = CameraManager.instance;
                if (mgr != null)
                {
                    hasMgr       = true;
                    liveZoomMin  = mgr.zoomMin;
                    liveCur      = mgr.currentZoom;
                    liveTgt      = mgr.targetZoom;
                    livePerScroll= mgr.zoomPerScroll;
                    liveSpeed    = mgr.zoomSpeed;
                    liveAngle    = mgr.cameraAngleDefault;
                }
            }
            catch { }

            // ── Calculate panel height ────────────────────────────
            float rows = 22f * sc    // live banner
                       + sep
                       + 3  * 36f * sc  // zoomMin + perScroll + speed sliders
                       + 36f * sc       // live zoom slider
                       + 28f * sc       // angle lock toggle
                       + (PrefLockAngle.Value ? 36f * sc : 0f)
                       + 36f * sc       // menu scale
                       + sep
                       + 22f * sc       // reset button
                       + sep + 8f;

            float totalH = hdr + sep + rows;
            _settRect.width  = w;
            _settRect.height = totalH;
            _settRect.x = Mathf.Clamp(_settRect.x, 0, Screen.width  - w);
            _settRect.y = Mathf.Clamp(_settRect.y, 0, Screen.height - totalH);

            // ── Draw background ───────────────────────────────────
            GUI.color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            GUI.DrawTexture(_settRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.13f, 0.42f, 0.78f, 1f);
            GUI.DrawTexture(new Rect(_settRect.x, _settRect.y, w, hdr), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var sBold  = S(_bold,  13, sc);
            var sSmall = S(_small, 10, sc, TextAnchor.MiddleRight);
            var sLeft  = S(_left,  12, sc);

            GUI.Label(new Rect(_settRect.x + 8, _settRect.y + 3, w - 90, hdr - 4),
                      "◈ Camera Zoom  v1.2", sBold);
            GUI.Label(new Rect(_settRect.x, _settRect.y + 3, w - 6, hdr - 4),
                      "[End] close", sSmall);

            float y  = _settRect.y + hdr + sep;
            float lx = _settRect.x + 10f;
            float lw = w - 20f;

            // ── Live status banner ────────────────────────────────
            if (hasMgr)
            {
                GUI.color = new Color(0.08f, 0.20f, 0.08f, 0.95f);
                GUI.DrawTexture(new Rect(lx, y, lw, 20f * sc), Texture2D.whiteTexture);
                GUI.color = new Color(0.40f, 1f, 0.55f);
                GUI.Label(new Rect(lx + 4, y, lw - 8, 20f * sc),
                    $"Live  zoom: {liveCur:F1} → {liveTgt:F1}   " +
                    $"zoomMin: {liveZoomMin:F1}   perScroll: {livePerScroll:F1}   " +
                    $"angle: {liveAngle:F1}°",
                    S(_left, 9, sc));
            }
            else
            {
                GUI.color = new Color(0.50f, 0.30f, 0.10f, 0.90f);
                GUI.DrawTexture(new Rect(lx, y, lw, 20f * sc), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.75f, 0.35f);
                GUI.Label(new Rect(lx + 4, y, lw - 8, 20f * sc),
                    "CameraManager not yet loaded — load into a zone first",
                    S(_left, 9, sc));
            }
            y += 22f * sc;
            GUI.color = Color.white;

            Sep(lx, ref y, lw, sc);

            // ── Zoom Out Limit (zoomMin) ──────────────────────────
            y = SliderRow(lx, y, lw, sc, sLeft,
                $"Zoom Out Limit  (zoomMin)   [orig: {(float.IsNaN(OrigZoomMin) ? "?" : OrigZoomMin.ToString("F1"))}]",
                ref PrefZoomMin, -200f, -1f, "F0");

            // ── Scroll Sensitivity (zoomPerScroll) ────────────────
            y = SliderRow(lx, y, lw, sc, sLeft,
                $"Scroll Sensitivity  (per notch)   [orig: {(float.IsNaN(OrigZoomPerScroll) ? "?" : OrigZoomPerScroll.ToString("F2"))}]",
                ref PrefZoomPerScroll, 0.1f, 20f, "F1");

            // ── Zoom Lerp Speed ───────────────────────────────────
            y = SliderRow(lx, y, lw, sc, sLeft,
                $"Zoom Lerp Speed   [orig: {(float.IsNaN(OrigZoomSpeed) ? "?" : OrigZoomSpeed.ToString("F1"))}]",
                ref PrefZoomSpeed, 0.5f, 30f, "F1");

            // ── Live Zoom Slider (targetZoom) ─────────────────────
            {
                float rangeMin = PrefZoomMin.Value;
                float rangeMax = float.IsNaN(OrigZoomDefault) ? 0f : OrigZoomDefault + 5f;
                if (rangeMax <= rangeMin) rangeMax = rangeMin + 50f;

                float curTgt = hasMgr ? liveTgt : rangeMin * 0.5f;
                curTgt = Mathf.Clamp(curTgt, rangeMin, rangeMax);

                GUI.color = Color.white;
                GUI.Label(new Rect(lx, y, lw, 18 * sc),
                    $"Current Zoom  (drag to reposition)   value: {curTgt:F1}",
                    sLeft);
                y += 18f * sc;
                float newTgt = GUI.HorizontalSlider(new Rect(lx, y, lw, 14 * sc), curTgt, rangeMin, rangeMax);
                if (hasMgr && mgr != null && Mathf.Abs(newTgt - curTgt) > 0.05f)
                {
                    try { mgr.targetZoom = newTgt; } catch { }
                }
                y += 18f * sc;
            }

            // ── Camera Angle Lock ─────────────────────────────────
            {
                float togSz = 22f * sc;
                float rowH  = 24f * sc;
                float midY  = y + (rowH - togSz) * 0.5f;
                bool  locked = PrefLockAngle.Value;
                GUI.color = locked
                    ? new Color(0.20f, 0.50f, 0.80f, 1f)
                    : new Color(0.15f, 0.15f, 0.20f, 0.90f);
                GUI.DrawTexture(new Rect(lx, y, lw, rowH), Texture2D.whiteTexture);
                GUI.color = Color.white;
                string lockTxt = locked
                    ? $"Camera Angle Locked at {PrefAngle.Value:F0}°  (ON)"
                    : $"Camera Angle  free  [orig: {(float.IsNaN(OrigAngleDef) ? "?" : OrigAngleDef.ToString("F1"))}°]  (OFF)";
                GUI.Label(new Rect(lx + 4, y, lw - togSz - 8, rowH), lockTxt, S(_left, 10, sc));
                bool newLock = GUI.Toggle(new Rect(lx + lw - togSz - 2, midY, togSz, togSz), locked, "");
                if (newLock != locked) PrefLockAngle.Value = newLock;
                y += rowH + 4f * sc;
            }

            if (PrefLockAngle.Value)
                y = SliderRow(lx, y, lw, sc, sLeft, "Locked Angle (degrees)", ref PrefAngle, 20f, 85f, "F1");

            y = SliderRow(lx, y, lw, sc, sLeft, "Menu Scale", ref PrefMenuScale, 0.7f, 2.0f, "F1");

            Sep(lx, ref y, lw, sc);

            // ── Reset button ──────────────────────────────────────
            bool canReset = _capturedOrig;
            string resetTxt = canReset
                ? $"Reset All to Game Defaults  (zoomMin:{OrigZoomMin:F1}  perScroll:{OrigZoomPerScroll:F2}  speed:{OrigZoomSpeed:F1}  angle:{OrigAngleDef:F1}°)"
                : "Reset to Game Defaults  (enter a zone first to capture originals)";

            GUI.color = canReset
                ? new Color(0.22f, 0.40f, 0.22f, 0.95f)
                : new Color(0.20f, 0.20f, 0.22f, 0.70f);
            float btnH = 22f * sc;
            GUI.DrawTexture(new Rect(lx, y, lw, btnH), Texture2D.whiteTexture);
            GUI.color = canReset ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(lx, y, lw, btnH), resetTxt, S(_lbl, 9, sc));
            if (canReset && GUI.Button(new Rect(lx, y, lw, btnH), GUIContent.none, GUIStyle.none))
            {
                PrefZoomMin.Value       = OrigZoomMin;
                PrefZoomPerScroll.Value = OrigZoomPerScroll;
                PrefZoomSpeed.Value     = OrigZoomSpeed;
                PrefAngle.Value         = OrigAngleDef;
                PrefLockAngle.Value     = false;
                try
                {
                    var m = CameraManager.instance;
                    if (m != null) { m.resetZoom(); }
                }
                catch { }
            }
        }

        // ─────────────────────────────────────────────────────────
        //  Helpers
        // ─────────────────────────────────────────────────────────
        static void BuildStyles()
        {
            _stylesReady = true;
            _lbl  = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            _lbl.normal.textColor = Color.white;
            _bold = new GUIStyle(_lbl) { fontStyle = FontStyle.Bold };
            _left = new GUIStyle(_lbl) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            _left.normal.textColor = Color.white;
            _small = new GUIStyle(_lbl) { fontSize = 10 };
            _small.normal.textColor = new Color(0.75f, 0.75f, 0.80f);
        }

        static GUIStyle S(GUIStyle src, int baseSize, float sc, TextAnchor? anchor = null)
        {
            var st = new GUIStyle(src) { fontSize = Mathf.RoundToInt(baseSize * sc) };
            if (anchor.HasValue) st.alignment = anchor.Value;
            return st;
        }

        static void Sep(float lx, ref float y, float lw, float sc)
        {
            GUI.color = new Color(0.28f, 0.28f, 0.38f, 0.90f);
            GUI.DrawTexture(new Rect(lx, y, lw, 1), Texture2D.whiteTexture);
            y += 8f * sc; GUI.color = Color.white;
        }

        static float SliderRow(float x, float y, float w, float sc, GUIStyle lSt,
            string label, ref MelonPreferences_Entry<float> pref, float lo, float hi, string fmt)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, 18 * sc), $"{label}: {pref.Value.ToString(fmt)}", lSt);
            y += 18f * sc;
            pref.Value = GUI.HorizontalSlider(new Rect(x, y, w, 14 * sc), pref.Value, lo, hi);
            y += 18f * sc;
            return y;
        }

        static void HandleDrag()
        {
            var ev = Event.current; if (ev == null) return;
            float sc = Mathf.Clamp(PrefMenuScale.Value, 0.7f, 2.0f);
            var hR = new Rect(_settRect.x, _settRect.y, _settRect.width, 26f * sc);
            switch (ev.type)
            {
                case EventType.MouseDown when hR.Contains(ev.mousePosition):
                    _dragging = true;
                    _dragOff  = ev.mousePosition - new Vector2(_settRect.x, _settRect.y);
                    ev.Use(); break;
                case EventType.MouseDrag when _dragging:
                    _settRect.x = ev.mousePosition.x - _dragOff.x;
                    _settRect.y = ev.mousePosition.y - _dragOff.y;
                    ev.Use(); break;
                case EventType.MouseUp:
                    _dragging = false; break;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Harmony — patch CameraManager.Start so zoom is applied
    //  right when each scene loads its camera, before any player input
    // ─────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(CameraManager), "Start")]
    internal static class Patch_CameraManager_Start
    {
        [HarmonyPostfix]
        public static void Postfix(CameraManager __instance)
        {
            if (__instance == null) return;
            try
            {
                // Capture originals on the very first Start call
                if (!CameraZoomMod._capturedOrig)
                {
                    CameraZoomMod._capturedOrig      = true;
                    CameraZoomMod.OrigZoomMin        = __instance.zoomMin;
                    CameraZoomMod.OrigZoomDefault    = __instance.zoomDefault;
                    CameraZoomMod.OrigZoomPerScroll  = __instance.zoomPerScroll;
                    CameraZoomMod.OrigZoomSpeed      = __instance.zoomSpeed;
                    CameraZoomMod.OrigAngleDef       = __instance.cameraAngleDefault;
                    MelonLogger.Msg(
                        $"[CameraZoom] Start captured — " +
                        $"zoomMin:{CameraZoomMod.OrigZoomMin:F2}  " +
                        $"zoomDefault:{CameraZoomMod.OrigZoomDefault:F2}  " +
                        $"perScroll:{CameraZoomMod.OrigZoomPerScroll:F2}  " +
                        $"speed:{CameraZoomMod.OrigZoomSpeed:F2}  " +
                        $"angle:{CameraZoomMod.OrigAngleDef:F2}");
                }

                __instance.zoomMin       = CameraZoomMod.PrefZoomMin.Value;
                __instance.zoomPerScroll = CameraZoomMod.PrefZoomPerScroll.Value;
                __instance.zoomSpeed     = CameraZoomMod.PrefZoomSpeed.Value;

                if (CameraZoomMod.PrefLockAngle.Value)
                {
                    float a = CameraZoomMod.PrefAngle.Value;
                    __instance.cameraAngleDefault = a;
                    __instance.cameraAngleMin     = a;
                    __instance.cameraAngleMax     = a;
                }

                MelonLogger.Msg(
                    $"[CameraZoom] Applied — " +
                    $"zoomMin:{__instance.zoomMin:F1}  " +
                    $"perScroll:{__instance.zoomPerScroll:F1}  " +
                    $"speed:{__instance.zoomSpeed:F1}  " +
                    $"angleLock:{CameraZoomMod.PrefLockAngle.Value}");
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[CameraZoom] Start patch error: " + ex.Message);
            }
        }
    }
}
