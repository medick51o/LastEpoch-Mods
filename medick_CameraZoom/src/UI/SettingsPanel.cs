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

        public static void Close()
        {
            Visible = false;
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

            float sc = Mathf.Clamp(Prefs.MenuScale.Value, 0.7f, 2.0f);
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
            y = ready
                ? Widgets.StatusRow(x, y, cw, sc,
                    $"live — zoom {liveCur:F1} → {liveTgt:F1} · angle {liveAngle:F1}° · game default {CameraState.ZoomDefault:F1}",
                    Theme.Ready)
                : Widgets.StatusRow(x, y, cw, sc,
                    "camera not loaded — enter a zone first", Theme.Warning);
            y += gapSect;

            // ── ZOOM ──────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "ZOOM");
            y = Widgets.SliderRow(x, y, cw, sc,
                ZoomLabel("Zoom out limit", CameraState.ZoomMin, "F0"),
                Prefs.ZoomMin, -200f, -1f, "F0");
            y = Widgets.SliderRow(x, y, cw, sc,
                ZoomLabel("Scroll sensitivity", CameraState.ZoomPerScroll, "F1"),
                Prefs.ZoomPerScroll, 0.1f, 20f, "F1");
            y = Widgets.SliderRow(x, y, cw, sc,
                ZoomLabel("Zoom speed", CameraState.ZoomSpeed, "F1"),
                Prefs.ZoomSpeed, 0.5f, 30f, "F1");

            if (ready)
            {
                float lo = CameraState.SaneZoomMin;
                float hi = CameraState.ZoomDefault + 5f;
                float cur = Mathf.Clamp(liveTgt, lo, hi);
                float next = Widgets.LiveSliderRow(x, ref y, cw, sc, "Current zoom (live)", cur, lo, hi, "F1");
                if (Mathf.Abs(next - cur) > 0.05f && !float.IsNaN(next))
                {
                    try { mgr.targetZoom = next; } catch { }
                }
            }
            y += gapSect;

            // ── ANGLE ─────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "ANGLE");
            y = Widgets.SwitchRow(x, y, cw, sc,
                Prefs.LockAngle.Value
                    ? $"Lock camera angle ({Prefs.Angle.Value:F0}°)"
                    : ZoomLabel("Lock camera angle", CameraState.AngleDefault, "F0", "°"),
                Prefs.LockAngle);
            if (Prefs.LockAngle.Value)
                y = Widgets.SliderRow(x, y, cw, sc, "Locked angle (degrees)", Prefs.Angle, 20f, 85f, "F0");
            y += gapSect;

            // ── PANEL ─────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "PANEL");
            y = Widgets.SliderRow(x, y, cw, sc, "Menu scale", Prefs.MenuScale, 0.7f, 2.0f, "F1");
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
            float sc = Mathf.Clamp(Prefs.MenuScale.Value, 0.7f, 2.0f);
            var hR = new Rect(_rect.x, _rect.y, _rect.width, 30f * sc);
            switch (ev.type)
            {
                case EventType.MouseDown when hR.Contains(ev.mousePosition)
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
