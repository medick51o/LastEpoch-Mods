using Il2Cpp;
using UnityEngine;

namespace medick_CameraZoom
{
    // The End-key settings panel — Terrible family design system. Screen
    // position persists across sessions (PanelX/PanelY prefs).
    internal static class SettingsPanel
    {
        public static bool Visible;

        static Rect    _rect = new(Prefs.DefaultPanelX, Prefs.DefaultPanelY, 380, 30);
        static bool    _posLoaded;
        static bool    _dragging;
        static bool    _dragMoved;
        static Vector2 _dragOff;
        static Rect    _closeRect;
        static float   _sc = 1f;   // layout scale, frozen while a control is hot (see Draw)

        public static void Close()
        {
            Visible   = false;
            _dragging = false;   // a missed MouseUp must not leak into the next open
            Prefs.Save();
        }

        public static void Draw()
        {
            if (!_posLoaded)
            {
                _posLoaded = true;
                _rect.x = Prefs.PanelX.Value;
                _rect.y = Prefs.PanelY.Value;
            }

            HandleDrag();

            // Layout scale is frozen while any IMGUI control is hot: the Menu
            // scale slider's own rect derives from this value, so applying it
            // mid-drag remaps the cursor against a moving rect — a divergent
            // feedback loop that strobes the panel between min and max scale.
            // The new scale takes effect on mouse-up.
            if (GUIUtility.hotControl == 0)
                _sc = Mathf.Clamp(Prefs.MenuScale.Value, Prefs.MenuScaleLo, Prefs.MenuScaleHi);
            float sc = _sc;
            float w  = 380f * sc;

            // ── Live camera state ─────────────────────────────────
            CameraManager mgr = null;
            float liveCur = 0f, liveTgt = 0f, liveAngle = 0f;
            bool  hasMgr  = false;
            try
            {
                mgr = CameraManager.instance;
                if (mgr != null)
                {
                    liveCur   = mgr.currentZoom;
                    liveTgt   = mgr.targetZoom;
                    liveAngle = mgr.cameraAngleDefault;
                    hasMgr    = true;
                }
            }
            catch { }
            bool ready = hasMgr && CameraState.Captured;

            // ── Height budget ─────────────────────────────────────
            float titleH  = 30f * sc;
            float pad     = 10f * sc;
            float hdrH    = 20f * sc;
            float rowH    = 26f * sc;
            float status  = 20f * sc;
            float gapSect = 8f * sc;
            float zoomSect  = hdrH + 3 * rowH + (ready ? rowH : 0f);   // 3 prefs + live zoom
            float angleSect = hdrH + rowH + (Prefs.LockAngle.Value ? rowH : 0f);
            float panelSect = hdrH + 2 * rowH;
            float actSect   = hdrH + rowH;
            float footH     = 22f * sc;
            float total = titleH + pad + status + gapSect
                + zoomSect + gapSect + angleSect + gapSect
                + panelSect + gapSect + actSect
                + pad * 0.5f + footH;

            _rect.width  = w;
            _rect.height = total;
            _rect.x = Mathf.Clamp(_rect.x, 0, Mathf.Max(0, Screen.width  - w));
            _rect.y = Mathf.Clamp(_rect.y, 0, Mathf.Max(0, Screen.height - total));

            GUI.color = Color.white;
            Theme.Box(_rect, Theme.Panel);
            DrawTitleBar(sc, w, titleH);

            float x  = _rect.x + pad;
            float y  = _rect.y + titleH + pad * 0.7f;
            float cw = w - pad * 2f;

            // ── Status ────────────────────────────────────────────
            if (ready)
                Widgets.StatusRow(x, ref y, cw, sc,
                    $"live — zoom {liveCur:F1} → {liveTgt:F1} · angle {(Prefs.LockAngle.Value ? "locked" : "default")} {liveAngle:F1}° · game default {CameraState.ZoomDefault:F1}",
                    Theme.Ready);
            else if (hasMgr)
                Widgets.StatusRow(x, ref y, cw, sc,
                    CameraState.CaptureStalled
                        ? "camera values not readable — check the MelonLoader log"
                        : "syncing with camera…", Theme.Warning);
            else
                Widgets.StatusRow(x, ref y, cw, sc,
                    "camera not loaded — enter a zone first", Theme.Warning);
            y += gapSect;

            // ── ZOOM ──────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "ZOOM");
            Widgets.SliderRow(x, ref y, cw, sc,
                ZoomLabel("Zoom out limit", CameraState.ZoomMin, "F0"),
                Prefs.ZoomMin, Prefs.ZoomMinLo, Prefs.ZoomMinHi, "F0");
            Widgets.SliderRow(x, ref y, cw, sc,
                ZoomLabel("Scroll sensitivity", CameraState.ZoomPerScroll, "F1"),
                Prefs.ZoomPerScroll, Prefs.PerScrollLo, Prefs.PerScrollHi, "F1");
            Widgets.SliderRow(x, ref y, cw, sc,
                ZoomLabel("Zoom speed", CameraState.ZoomSpeed, "F1"),
                Prefs.ZoomSpeed, Prefs.SpeedLo, Prefs.SpeedHi, "F1");

            if (ready)
            {
                float lo = CameraState.SaneZoomMin;
                float hi = CameraState.ZoomDefault + 5f;
                hi = Mathf.Max(hi, lo + 1f);   // a zoom-out limit above the default must not invert the range
                float cur = Mathf.Clamp(liveTgt, lo, hi);
                float next = Widgets.LiveSliderRow(x, ref y, cw, sc, "Target zoom (live)", cur, lo, hi, "F1");
                if (Mathf.Abs(next - cur) > 0.05f && !float.IsNaN(next))
                {
                    try { mgr.targetZoom = next; } catch { }
                }
            }
            y += gapSect;

            // ── ANGLE ─────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "ANGLE");
            Widgets.SwitchRow(x, ref y, cw, sc,
                ZoomLabel("Lock camera angle", CameraState.AngleDefault, "F0", "°"),
                Prefs.LockAngle);
            if (Prefs.LockAngle.Value)
                Widgets.SliderRow(x, ref y, cw, sc, "Locked angle (degrees)",
                    Prefs.Angle, Prefs.AngleLo, Prefs.AngleHi, "F0");
            y += gapSect;

            // ── PANEL ─────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "PANEL");
            Widgets.SliderRow(x, ref y, cw, sc, "Menu scale",
                Prefs.MenuScale, Prefs.MenuScaleLo, Prefs.MenuScaleHi, "F1");
            if (Widgets.ButtonRow(x, ref y, cw, sc, "Panel position", "Reset"))
            {
                _rect.x = Prefs.DefaultPanelX;
                _rect.y = Prefs.DefaultPanelY;
                SavePanelPos();
            }
            y += gapSect;

            // ── RESCUE ────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "RESCUE");
            if (ready)
            {
                if (Widgets.ButtonRow(x, ref y, cw, sc, "Camera acting up?", "Restore"))
                {
                    Prefs.ZoomMin.Value       = CameraState.ZoomMin;
                    Prefs.ZoomPerScroll.Value = CameraState.ZoomPerScroll;
                    Prefs.ZoomSpeed.Value     = CameraState.ZoomSpeed;
                    Prefs.Angle.Value         = CameraState.AngleDefault;
                    Prefs.LockAngle.Value     = false;
                    CameraState.RestoreToGame(mgr);
                    Prefs.Save();
                }
            }
            else
            {
                Theme.Text9(new Rect(x, y, cw, 22f * sc),
                    "Restore game defaults — available once a zone is loaded",
                    Theme.TextMut, Mathf.RoundToInt(9 * sc));
                y += 26f * sc;
            }

            DrawFooter(sc, w, footH);
            GUI.color = Color.white;
        }

        // Appends the captured game-original value to a row label.
        static string ZoomLabel(string label, float orig, string fmt, string suffix = "")
            => CameraState.Captured ? $"{label}  (game {orig.ToString(fmt)}{suffix})" : label;

        // ── Chrome ────────────────────────────────────────────────
        static void DrawTitleBar(float sc, float w, float titleH)
        {
            var bar = new Rect(_rect.x, _rect.y, w, titleH);
            Theme.Fill(new Rect(bar.x + 1, bar.y + 1, bar.width - 2, bar.height - 1), Theme.Surface);
            Theme.Fill(new Rect(bar.x + 1, bar.yMax - 2f, bar.width - 2, 2f), Theme.AccentDim);

            Theme.Text9(new Rect(bar.x + 10f * sc, bar.y, w * 0.6f, titleH),
                BuildInfo.DisplayName, Theme.TextHi,
                Mathf.RoundToInt(13 * sc), FontStyle.Bold, TextAnchor.MiddleLeft, serif: true);

            var titleStyle = Theme.Label(Mathf.RoundToInt(13 * sc), FontStyle.Bold, TextAnchor.MiddleLeft, true);
            float tw = titleStyle.CalcSize(new GUIContent(BuildInfo.DisplayName)).x;
            Theme.Text9(new Rect(bar.x + 10f * sc + tw + 8f * sc, bar.y + 1f * sc, 80f * sc, titleH),
                "v" + BuildInfo.Version, Theme.TextMut, Mathf.RoundToInt(8 * sc));

            float cs = 18f * sc;
            _closeRect = new Rect(bar.xMax - cs - 6f * sc, bar.y + (titleH - cs) * 0.5f, cs, cs);
            GUI.color = Color.white;
            if (GUI.Button(_closeRect, "✕", Theme.Button(Mathf.RoundToInt(10 * sc), danger: true)))
                Close();
        }

        static void DrawFooter(float sc, float w, float footH)
        {
            var f = new Rect(_rect.x + 1, _rect.yMax - footH, w - 2, footH - 1);
            Theme.Fill(new Rect(f.x + 9f * sc, f.y, w - 20f * sc, 1f), Theme.Border);
            Theme.Text9(new Rect(f.x + 9f * sc, f.y, w * 0.75f, footH),
                $"{BuildInfo.OfficialName} — {BuildInfo.Tagline}",
                Theme.TextMut, Mathf.RoundToInt(8 * sc));
            Theme.Text9(new Rect(f.x, f.y, f.width - 8f * sc, footH),
                "End closes", Theme.TextMut, Mathf.RoundToInt(8 * sc),
                FontStyle.Normal, TextAnchor.MiddleRight);
        }

        // ── Plumbing ──────────────────────────────────────────────
        static void HandleDrag()
        {
            var ev = Event.current;
            if (ev == null) return;
            var hR = new Rect(_rect.x, _rect.y, _rect.width, 30f * _sc);
            switch (ev.type)
            {
                case EventType.MouseDown when ev.button == 0
                                              && hR.Contains(ev.mousePosition)
                                              && !_closeRect.Contains(ev.mousePosition):
                    _dragging  = true;
                    _dragMoved = false;
                    _dragOff   = ev.mousePosition - new Vector2(_rect.x, _rect.y);
                    ev.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    _rect.x = ev.mousePosition.x - _dragOff.x;
                    _rect.y = ev.mousePosition.y - _dragOff.y;
                    _dragMoved = true;
                    ev.Use();
                    break;
                case EventType.MouseUp:
                    if (_dragging && _dragMoved) SavePanelPos();
                    _dragging = false;
                    break;
            }
        }

        static void SavePanelPos()
        {
            Prefs.PanelX.Value = _rect.x;
            Prefs.PanelY.Value = _rect.y;
            Prefs.Save();
        }
    }
}
