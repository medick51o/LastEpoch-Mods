using System.Collections.Generic;
using UnityEngine;

namespace medick_CooldownTracker
{
    // The Home-key settings panel — Terrible Cooldowns design system.
    // Screen position persists across sessions (PanelX/PanelY prefs):
    // drag it once, it opens there forever.
    internal static class SettingsPanel
    {
        static Rect    _rect = new(Prefs.DefaultPanelX, Prefs.DefaultPanelY, 400, 30);
        static bool    _posLoaded;
        static bool    _dragging;
        static bool    _dragMoved;
        static Vector2 _dragOff;
        static Vector2 _scroll;
        static Rect    _closeRect;
        static readonly List<SlotData> _rows = new();

        public static Rect PanelRect => _rect;

        static readonly string[] ModeLabels   = { "Auto", "Keyboard", "Xbox", "PS5" };
        static readonly string[] LayoutLabels = { "Auto", "Xbox", "PS5" };

        public static Color ModeDot(int effMI) =>
            effMI == 1 ? Theme.XboxGreen : effMI == 2 ? Theme.PsBlue : Theme.TextMut;

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
            float w  = 400f * sc;
            int   effMI = ButtonLabels.GetModeIndex();

            // ── Height budget ─────────────────────────────────────
            float titleH  = 30f * sc;
            float pad     = 10f * sc;
            float hdrH    = 20f * sc;                       // SectionHeader advance
            float segH    = 24f * sc + 6f * sc;
            float autoH   = Prefs.InputMode.Value == 0
                ? 20f * sc + 24f * sc                       // status row + layout override row
                : 0f;
            float sliders = 5 * 26f * sc + 26f * sc            // 5 sliders + Move row
                + (UiState.MoveIcons ? 20f * sc : 0f);         // hint while moving
            float behave  = 2 * 26f * sc;
            float gapSect = 8f * sc;
            int   slotCount = SlotRegistry.Count;
            float rowH    = 42f * sc, rowGap = 4f * sc;
            float listH   = slotCount == 0
                ? 40f * sc
                : Mathf.Min(slotCount * (rowH + rowGap), 7 * (rowH + rowGap));
            float footH   = 22f * sc;

            float total = titleH + pad
                + hdrH + segH + autoH + gapSect
                + hdrH + sliders + gapSect
                + hdrH + behave + gapSect
                + hdrH + listH
                + pad * 0.5f + footH;

            _rect.width  = w;
            _rect.height = total;
            _rect.x = Mathf.Clamp(_rect.x, 0, Mathf.Max(0, Screen.width  - w));
            _rect.y = Mathf.Clamp(_rect.y, 0, Mathf.Max(0, Screen.height - total));

            // ── Chrome ────────────────────────────────────────────
            GUI.color = Color.white;
            Theme.Box(_rect, Theme.Panel);

            DrawTitleBar(sc, w, titleH);

            float x = _rect.x + pad;
            float y = _rect.y + titleH + pad * 0.7f;
            float cw = w - pad * 2f;

            // ── INPUT ─────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "INPUT");
            int newMode = Widgets.Segmented(new Rect(x, y, cw, 24f * sc),
                Prefs.InputMode.Value, ModeLabels, sc);
            if (newMode != Prefs.InputMode.Value)
            { Prefs.InputMode.Value = newMode; UiState.PickerSlot = -1; }
            y += 24f * sc + 6f * sc;

            if (Prefs.InputMode.Value == 0)
            {
                y = InputTracker.IsControllerActive
                    ? Widgets.StatusRow(x, y, cw, sc,
                        $"Controller detected ({(InputTracker.DetectedLayout == CtrlLayout.PlayStation ? "PS5" : "Xbox")})",
                        InputTracker.DetectedLayout == CtrlLayout.PlayStation ? Theme.PsBlue : Theme.XboxGreen)
                    : Widgets.StatusRow(x, y, cw, sc, "Keyboard / Mouse", Theme.Ready);

                float labW = 110f * sc;
                Theme.Text9(new Rect(x, y, labW, 20f * sc), "Layout override",
                    Theme.TextMut, Mathf.RoundToInt(9 * sc));
                int newLay = Widgets.Segmented(new Rect(x + labW, y, cw - labW, 20f * sc),
                    Prefs.CtrlLayout.Value, LayoutLabels, sc);
                if (newLay != Prefs.CtrlLayout.Value)
                { Prefs.CtrlLayout.Value = newLay; UiState.PickerSlot = -1; }
                y += 24f * sc;
            }
            y += gapSect;

            // ── DISPLAY ───────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "DISPLAY");
            y = Widgets.SliderRow(x, y, cw, sc, "Icon opacity",      Prefs.Alpha,     0.05f,   1f, "F2");
            y = Widgets.SliderRow(x, y, cw, sc, "Icon size",         Prefs.Size,      32f,   120f, "F0");
            y = Widgets.SliderRow(x, y, cw, sc, "Horizontal offset", Prefs.OffsetX, -500f,  500f, "F0");
            y = Widgets.SliderRow(x, y, cw, sc, "Vertical offset",   Prefs.OffsetY, -600f,  200f, "F0");

            // Move mode: drag the live icon cluster instead of doing slider math.
            {
                float mvH = 22f * sc;
                Theme.Text9(new Rect(x, y, cw - 80f * sc, mvH), "Icon position",
                    Theme.Text, Mathf.RoundToInt(10 * sc));
                GUI.color = Color.white;
                if (GUI.Button(new Rect(x + cw - 76f * sc, y + 1f * sc, 76f * sc, mvH - 2f * sc),
                        UiState.MoveIcons ? "Lock" : "Move",
                        Theme.Button(Mathf.RoundToInt(9 * sc), selected: UiState.MoveIcons)))
                {
                    UiState.MoveIcons = !UiState.MoveIcons;
                    if (!UiState.MoveIcons) Prefs.Save();   // just locked in a position
                }
                y += mvH + 4f * sc;
                if (UiState.MoveIcons)
                    y = Widgets.StatusRow(x, y, cw, sc,
                        "drag the icon cluster in the game view — sliders follow", Theme.Accent);
            }

            y = Widgets.SliderRow(x, y, cw, sc, "Menu scale",        Prefs.MenuScale, 0.7f,  2.0f, "F1");
            y += gapSect;

            // ── BEHAVIOR ──────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "BEHAVIOR");
            y = Widgets.SwitchRow(x, y, cw, sc, "Block movement while menu is open", Prefs.LockInput);
            if (Widgets.ButtonRow(x, ref y, cw, sc, "Panel position", "Reset"))
            {
                _rect.x = Prefs.DefaultPanelX;
                _rect.y = Prefs.DefaultPanelY;
                SavePanelPos();
            }
            y += gapSect;

            // ── SKILLS ────────────────────────────────────────────
            Widgets.SectionHeader(x, ref y, cw, sc, "SKILLS",
                $"editing: {Prefs.ModeDispName[effMI]}", ModeDot(effMI));

            SlotRegistry.SnapshotAll(_rows);
            if (_rows.Count == 0)
            {
                var er = new Rect(x, y, cw, 36f * sc);
                GUI.color = Color.white;
                Theme.Box(er, Theme.Card);
                Theme.Text9(er, "Waiting for your skill bar — load into a zone.",
                    Theme.TextMut, Mathf.RoundToInt(10 * sc), FontStyle.Normal, TextAnchor.MiddleCenter);
                UiState.TextFieldActive = false;
                DrawFooter(sc, w, footH);
                return;
            }

            DrawSlotList(x, y, cw, sc, effMI, rowH, rowGap, listH);

            // Suppress game hotkeys while a label field has keyboard focus.
            UiState.TextFieldActive = UiState.ShowSettings && GUIUtility.keyboardControl != 0;

            DrawFooter(sc, w, footH);
            GUI.color = Color.white;
        }

        // ── Chrome pieces ─────────────────────────────────────────
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
                "Home closes", Theme.TextMut, Mathf.RoundToInt(8 * sc),
                FontStyle.Normal, TextAnchor.MiddleRight);
        }

        internal static void Close()
        {
            UiState.ShowSettings    = false;
            UiState.PickerSlot      = -1;
            UiState.TextFieldActive = false;
            UiState.MoveIcons       = false;
            InputBlocker.Restore();
            Prefs.Save();
        }

        // ── Skill rows ────────────────────────────────────────────
        static void DrawSlotList(float x, float y, float cw, float sc, int effMI,
            float rowH, float rowGap, float listH)
        {
            float contentH = _rows.Count * (rowH + rowGap);
            bool  scrolls  = contentH > listH + 1f;
            var   outer    = new Rect(x, y, cw, listH);
            var   inner    = new Rect(0, 0, cw - (scrolls ? 8f * sc : 0f), contentH);

            _scroll = GUI.BeginScrollView(outer, _scroll, inner, GUIStyle.none, GUIStyle.none);
            float ry = 0f;
            foreach (var s in _rows)
            {
                DrawSlotRow(new Rect(0, ry, inner.width, rowH), s, sc, effMI);
                ry += rowH + rowGap;
            }
            GUI.EndScrollView();

            if (scrolls)   // slim scroll indicator, wheel does the work
            {
                float track = listH;
                float thumbH = Mathf.Max(16f, track * (listH / contentH));
                float tY = y + (track - thumbH) * Mathf.Clamp01(_scroll.y / (contentH - listH));
                Theme.Fill(new Rect(x + cw - 3f, tY, 3f, thumbH), Theme.AccentDim);
            }
        }

        static void DrawSlotRow(Rect r, SlotData s, float sc, int effMI)
        {
            GUI.color = Color.white;
            Theme.Box(r, Theme.Card);

            // Icon + cooldown progress underline
            float iSz = r.height - 12f * sc;
            var ir = new Rect(r.x + 6f * sc, r.y + 5f * sc, iSz, iSz);
            bool drew = false;
            try
            {
                if (s.Icon != null)
                { Theme.DrawSprite(ir, s.Icon); drew = true; }
            }
            catch { }
            if (!drew) Theme.Fill(ir, Theme.Inset);
            Theme.DrawBorder(ir, Theme.Border, 1f);
            if (s.OnCooldown && s.Fill > 0f)
                Theme.Fill(new Rect(ir.x, ir.yMax + 1f, ir.width * (1f - s.Fill), 2f * sc), Theme.Cooling);

            // Key label + sub-hint
            float lbX = ir.xMax + 8f * sc;
            float lbW = 44f * sc;
            Theme.Text9(new Rect(lbX, r.y + 4f * sc, lbW, r.height * 0.5f),
                s.RawLabel ?? "", s.OnCooldown ? Theme.Cooling : Theme.TextHi,
                Mathf.RoundToInt(11 * sc), FontStyle.Bold);

            string hint = s.OnCooldown ? $"{(int)(s.Fill * 100)}%"
                       : s.SlotIndex == 6 ? "evade"
                       : (!string.IsNullOrEmpty(s.GameBoundKey) && effMI == 0) ? $"↑{s.GameBoundKey}"
                       : null;
            if (hint != null)
                Theme.Text9(new Rect(lbX, r.y + r.height * 0.5f, lbW, r.height * 0.5f - 4f * sc),
                    hint, s.OnCooldown ? Theme.Cooling : Theme.TextMut, Mathf.RoundToInt(8 * sc));

            // Custom label field
            bool  hasPicker = effMI >= 1;
            float swW = 34f * sc, swH = 16f * sc;
            float pkW = hasPicker ? 24f * sc : 0f;
            float tfX = lbX + lbW + 6f * sc;
            float tfW = r.xMax - tfX - pkW - swW - 20f * sc;
            float tfH = 22f * sc;
            float tfY = r.y + (r.height - tfH) * 0.5f;
            string cur = Prefs.CustomLabel(effMI, s.SlotIndex);

            GUI.color = Color.white;
            string next = GUI.TextField(new Rect(tfX, tfY, tfW, tfH), cur, 20,
                Theme.TextField(Mathf.RoundToInt(10 * sc)));
            if (next != cur) Prefs.SetCustomLabel(effMI, s.SlotIndex, next);

            if (string.IsNullOrEmpty(cur))
                Theme.Text9(new Rect(tfX + 7f * sc, tfY, tfW - 10f * sc, tfH), "auto",
                    Theme.TextMut, Mathf.RoundToInt(9 * sc));
            else
            {
                float xs = 15f * sc;
                GUI.color = Color.white;
                if (GUI.Button(new Rect(tfX + tfW - xs - 3f * sc, tfY + (tfH - xs) * 0.5f, xs, xs),
                        "✕", Theme.Button(Mathf.RoundToInt(8 * sc), danger: true)))
                {
                    Prefs.SetCustomLabel(effMI, s.SlotIndex, "");
                    if (UiState.PickerSlot == s.SlotIndex) UiState.PickerSlot = -1;
                }
            }

            // [▼] glyph picker (controller modes only)
            if (hasPicker)
            {
                bool open = UiState.PickerSlot == s.SlotIndex;
                GUI.color = Color.white;
                if (GUI.Button(new Rect(tfX + tfW + 4f * sc, tfY, 20f * sc, tfH), "▼",
                        Theme.Button(Mathf.RoundToInt(9 * sc), selected: open)))
                    UiState.PickerSlot = open ? -1 : s.SlotIndex;
            }

            // Per-slot enable switch (persists)
            bool en  = Prefs.IsSlotEnabled(s.SlotIndex);
            bool nen = Widgets.Switch(
                new Rect(r.xMax - swW - 8f * sc, r.y + (r.height - swH) * 0.5f, swW, swH), en);
            if (nen != en) Prefs.SetSlotEnabled(s.SlotIndex, nen);
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
