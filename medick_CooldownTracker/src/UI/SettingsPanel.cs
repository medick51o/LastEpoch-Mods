using System.Collections.Generic;
using UnityEngine;

namespace medick_CooldownTracker
{
    // The Home-key settings panel. Its screen position persists across
    // sessions (PanelX/PanelY prefs) — drag it once, it stays there.
    internal static class SettingsPanel
    {
        static Rect    _rect = new(Prefs.DefaultPanelX, Prefs.DefaultPanelY, 380, 30);
        static bool    _posLoaded;
        static bool    _dragging;
        static bool    _dragMoved;
        static Vector2 _dragOff;
        static Vector2 _scroll;
        static readonly List<SlotData> _rows = new();

        static readonly string HeaderTitle =
            $"◈ {BuildInfo.DisplayName}  v{BuildInfo.Version}";

        public static Rect PanelRect => _rect;

        static readonly string[] ModeBtnLabels = { "Auto", "Keyboard", "Xbox", "PS5" };
        static readonly Color[]  ModeBtnColors =
        {
            new(0.20f, 0.60f, 0.28f, 1f),   // Auto     – green
            new(0.25f, 0.45f, 0.70f, 1f),   // Keyboard – blue
            new(0.65f, 0.38f, 0.10f, 1f),   // Xbox     – orange
            new(0.45f, 0.15f, 0.60f, 1f),   // PS5      – purple
        };

        public static Color ModeBadgeColor(int effMI) =>
            effMI == 0 ? ModeBtnColors[1] : effMI == 1 ? ModeBtnColors[2] : ModeBtnColors[3];

        public static void Draw()
        {
            if (!_posLoaded)
            {
                _posLoaded = true;
                _rect.x = Prefs.PanelX.Value;
                _rect.y = Prefs.PanelY.Value;
            }

            HandleDrag();

            float sc  = Mathf.Clamp(Prefs.MenuScale.Value, 0.7f, 2.0f);
            float w   = 380f * sc;
            float hdr = 26f  * sc;
            int   effMI = ButtonLabels.GetModeIndex();
            string editingFor = Prefs.ModeDispName[effMI];

            // ── Height budget ─────────────────────────────────────
            float inputSect = 20f * sc + 28f * sc + (Prefs.InputMode.Value == 0 ? 48f * sc : 0f);
            float sliders   = 5 * 36f * sc;
            float lockRow   = 28f * sc;
            float resetRow  = 24f * sc;
            float sep       = 10f * sc;
            int   slotCount = SlotRegistry.Count;
            float slotHdr   = 22f * sc;
            float slotListH = slotCount == 0
                ? 28f * sc
                : Mathf.Min(slotCount * 46f * sc + 8f, 320f * sc);
            float total = hdr + sep + inputSect + sep + sliders + lockRow + resetRow + sep + slotHdr + slotListH + 10f;

            _rect.width  = w;
            _rect.height = total;
            _rect.x = Mathf.Clamp(_rect.x, 0, Mathf.Max(0, Screen.width  - w));
            _rect.y = Mathf.Clamp(_rect.y, 0, Mathf.Max(0, Screen.height - total));

            // ── Chrome ────────────────────────────────────────────
            GUI.color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            GUI.DrawTexture(_rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.13f, 0.42f, 0.78f, 1f);
            GUI.DrawTexture(new Rect(_rect.x, _rect.y, w, hdr), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(_rect.x + 8, _rect.y + 3, w - 80, hdr - 4),
                HeaderTitle, Styles.Get(StyleKind.CenterBold, 13, sc, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(_rect.x, _rect.y + 3, w - 6, hdr - 4),
                "[Home] close", Styles.Get(StyleKind.Small, 10, sc, TextAnchor.MiddleRight));

            float y  = _rect.y + hdr + sep;
            float lx = _rect.x + 10f;
            float lw = w - 20f;

            y = DrawInputModeSection(lx, y, lw, sc);
            DrawSep(lx, ref y, lw, sc);

            // ── Sliders ───────────────────────────────────────────
            y = SliderRow(lx, y, lw, sc, "Icon Opacity",           Prefs.Alpha,     0.05f,   1f, "F2");
            y = SliderRow(lx, y, lw, sc, "Icon Size (px)",         Prefs.Size,      32f,   120f, "F0");
            y = SliderRow(lx, y, lw, sc, "Offset X (← / →)",      Prefs.OffsetX, -500f,  500f, "F0");
            y = SliderRow(lx, y, lw, sc, "Offset Y (↑ neg = up)", Prefs.OffsetY, -600f,  200f, "F0");
            y = SliderRow(lx, y, lw, sc, "Menu Size",              Prefs.MenuScale, 0.7f,  2.0f, "F1");

            y = DrawLockRow(lx, y, lw, sc);
            y = DrawResetRow(lx, y, lw, sc);
            DrawSep(lx, ref y, lw, sc);

            // ── Skills list ───────────────────────────────────────
            GUI.color = new Color(0.75f, 0.85f, 1f);
            GUI.Label(new Rect(lx, y, lw * 0.40f, 20 * sc), "SKILLS",
                Styles.Get(StyleKind.SectionHeader, 12, sc));

            Color badgeCol = ModeBadgeColor(effMI);
            float badgeW = 75f * sc;
            Rect  badgeR = new(_rect.x + w - 10 - badgeW, y + 1, badgeW, 18 * sc);
            GUI.color = badgeCol; GUI.DrawTexture(badgeR, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(badgeR, $"editing: {editingFor}", Styles.Get(StyleKind.Small, 9, sc));

            float iSz   = 30f * sc;
            float lblCW = 46f * sc;
            float tfSt  = iSz + lblCW + 10f * sc;
            float togW  = 22f * sc;
            bool  hasPicker = effMI >= 1;
            float picBW = hasPicker ? 24f * sc : 0f;

            GUI.color = new Color(0.50f, 0.50f, 0.55f);
            GUI.Label(new Rect(lx + tfSt, y, lw - tfSt - picBW - togW - 12f, 18 * sc),
                hasPicker
                    ? $"Custom Label  ({editingFor} — use [▼] to pick)"
                    : "Custom Label  (empty = auto-default)",
                Styles.Get(StyleKind.Small, 9, sc, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(lx + lw - togW - 4, y, togW + 4, 18 * sc), "On",
                Styles.Get(StyleKind.Small, 9, sc, TextAnchor.MiddleCenter));
            y += 22f * sc;

            SlotRegistry.SnapshotAll(_rows);
            if (_rows.Count == 0)
            {
                GUI.color = new Color(0.50f, 0.50f, 0.55f);
                GUI.Label(new Rect(lx, y, lw, 24 * sc),
                    "Waiting for your skill bar — load into a zone.",
                    Styles.Get(StyleKind.Dim, 11, sc));
                GUI.color = Color.white;
                UiState.TextFieldActive = false;
                return;
            }

            DrawSlotRows(lx, y, lw, sc, effMI, iSz, lblCW, togW, hasPicker, picBW, badgeCol);

            // Suppress game hotkeys while a label field has keyboard focus.
            UiState.TextFieldActive = UiState.ShowSettings && GUIUtility.keyboardControl != 0;
            GUI.color = Color.white;
        }

        // ── Sections ──────────────────────────────────────────────
        static float DrawInputModeSection(float lx, float y, float lw, float sc)
        {
            GUI.color = new Color(0.75f, 0.85f, 1f);
            GUI.Label(new Rect(lx, y, lw, 18 * sc), "INPUT MODE",
                Styles.Get(StyleKind.SectionHeader, 12, sc));
            y += 20f * sc;

            float bw = (lw - 3f * sc) / 4f;
            for (int i = 0; i < 4; i++)
            {
                bool act = Prefs.InputMode.Value == i;
                var r = new Rect(lx + i * (bw + sc), y, bw, 24f * sc);
                GUI.color = act ? ModeBtnColors[i] : new Color(0.14f, 0.14f, 0.18f, 0.95f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = act ? Color.white : new Color(0.50f, 0.50f, 0.55f);
                GUI.Label(r, ModeBtnLabels[i], Styles.Get(StyleKind.Center, 11, sc));
                GUI.color = Color.white;
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                { Prefs.InputMode.Value = i; UiState.PickerSlot = -1; }
            }
            y += 28f * sc;

            if (Prefs.InputMode.Value == 0)
            {
                string stTxt; Color stCol;
                if (InputTracker.IsControllerActive)
                {
                    stTxt = $"● Controller detected  ({(InputTracker.DetectedLayout == CtrlLayout.PlayStation ? "PS5" : "Xbox")})";
                    stCol = new Color(0.40f, 1.00f, 0.50f);
                }
                else
                {
                    stTxt = "● Keyboard / Mouse";
                    stCol = new Color(0.55f, 0.80f, 1.00f);
                }
                GUI.color = stCol;
                GUI.Label(new Rect(lx, y, lw, 18 * sc), stTxt, Styles.Get(StyleKind.Left, 11, sc));
                y += 20f * sc;

                float labW = 108f * sc;
                GUI.color = new Color(0.60f, 0.60f, 0.65f);
                GUI.Label(new Rect(lx, y, labW, 20 * sc), "Layout override:",
                    Styles.Get(StyleKind.Left, 10, sc));
                string[] lo = { "Auto", "Xbox", "PS5" };
                Color[]  lc = { new(0.22f, 0.22f, 0.28f, 1f), ModeBtnColors[2], ModeBtnColors[3] };
                float lbw = (lw - labW - 2f * sc) / 3f;
                for (int i = 0; i < 3; i++)
                {
                    bool act = Prefs.CtrlLayout.Value == i;
                    var lr = new Rect(lx + labW + i * (lbw + sc), y, lbw, 20f * sc);
                    GUI.color = act ? lc[i] : new Color(0.14f, 0.14f, 0.18f, 0.95f);
                    GUI.DrawTexture(lr, Texture2D.whiteTexture);
                    GUI.color = act ? Color.white : new Color(0.50f, 0.50f, 0.55f);
                    GUI.Label(lr, lo[i], Styles.Get(StyleKind.Center, 10, sc));
                    GUI.color = Color.white;
                    if (GUI.Button(lr, GUIContent.none, GUIStyle.none))
                    { Prefs.CtrlLayout.Value = i; UiState.PickerSlot = -1; }
                }
                y += 28f * sc;
            }
            return y;
        }

        static float DrawLockRow(float lx, float y, float lw, float sc)
        {
            float togSz = 22f * sc;
            float rowH  = 24f * sc;
            bool  locked = Prefs.LockInput.Value;
            GUI.color = locked
                ? new Color(0.75f, 0.28f, 0.28f, 1f)
                : new Color(0.15f, 0.15f, 0.20f, 0.90f);
            GUI.DrawTexture(new Rect(lx, y, lw, rowH), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(lx + 4, y, lw - togSz - 8, rowH),
                locked ? "Block movement while menu is open  (ON)"
                       : "Block movement while menu is open  (OFF)",
                Styles.Get(StyleKind.Left, 10, sc));
            bool newLock = GUI.Toggle(
                new Rect(lx + lw - togSz - 2, y + (rowH - togSz) * 0.5f, togSz, togSz),
                locked, "");
            if (newLock != locked) Prefs.LockInput.Value = newLock;
            return y + rowH + 4f * sc;
        }

        static float DrawResetRow(float lx, float y, float lw, float sc)
        {
            float rowH = 20f * sc;
            var r = new Rect(lx, y, lw, rowH);
            GUI.color = new Color(0.15f, 0.15f, 0.20f, 0.90f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.65f, 0.65f, 0.72f);
            GUI.Label(r, "Reset panel position", Styles.Get(StyleKind.Center, 10, sc));
            GUI.color = Color.white;
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                _rect.x = Prefs.DefaultPanelX;
                _rect.y = Prefs.DefaultPanelY;
                SavePanelPos();
            }
            return y + rowH + 4f * sc;
        }

        static void DrawSlotRows(float lx, float y, float lw, float sc, int effMI,
            float iSz, float lblCW, float togW, bool hasPicker, float picBW, Color badgeCol)
        {
            float scrollH = _rect.yMax - y - 6f;
            float rowH    = 46f * sc;
            var   vRect   = new Rect(0, 0, lw - 16f, _rows.Count * rowH + 4f);
            _scroll = GUI.BeginScrollView(new Rect(lx, y, lw, scrollH), _scroll, vRect);
            float vw = vRect.width;
            float ry = 2f;

            foreach (var s in _rows)
            {
                int idx = s.SlotIndex;

                // Icon preview + progress underline
                bool drew = false;
                try
                {
                    if (s.Icon != null)
                    { GUI.color = Color.white; Styles.DrawSprite(new Rect(2, ry + 4, iSz, iSz), s.Icon); drew = true; }
                }
                catch { }
                if (!drew)
                {
                    GUI.color = new Color(0.20f, 0.20f, 0.24f);
                    GUI.DrawTexture(new Rect(2, ry + 4, iSz, iSz), Texture2D.whiteTexture);
                }
                if (s.OnCooldown && s.Fill > 0f)
                {
                    GUI.color = new Color(0.20f, 0.80f, 1f, 0.70f);
                    GUI.DrawTexture(new Rect(2, ry + 4 + iSz, iSz * (1f - s.Fill), 2.5f * sc), Texture2D.whiteTexture);
                }

                // Effective label + status
                float lbX = iSz + 8;
                var lbSt = Styles.Get(StyleKind.CenterBold, 12, sc);
                GUI.color = s.OnCooldown ? new Color(1f, 0.65f, 0.15f) : Color.white;
                GUI.Label(new Rect(lbX, ry + 2, lblCW, rowH * 0.5f), s.RawLabel ?? "", lbSt);
                GUI.color = Color.white;
                if (s.OnCooldown)
                {
                    GUI.color = new Color(0.9f, 0.55f, 0.55f);
                    GUI.Label(new Rect(lbX, ry + rowH * 0.5f, lblCW, rowH * 0.5f - 2),
                        $"{(int)(s.Fill * 100)}%", Styles.Get(StyleKind.Left, 10, sc));
                    GUI.color = Color.white;
                }
                if (idx == 6)
                {
                    GUI.color = new Color(0.55f, 0.75f, 0.95f);
                    GUI.Label(new Rect(lbX, ry + rowH - 13f * sc, lblCW, 13f * sc), "evade",
                        Styles.Get(StyleKind.Small, 8, sc));
                    GUI.color = Color.white;
                }
                string curCustom = Prefs.CustomLabel(effMI, idx);
                if (!string.IsNullOrEmpty(s.GameBoundKey) && string.IsNullOrEmpty(curCustom) && effMI == 0)
                {
                    GUI.color = new Color(0.35f, 0.85f, 0.45f, 0.85f);
                    GUI.Label(new Rect(lbX, ry + rowH - 13f * sc, lblCW, 13f * sc),
                        $"↑{s.GameBoundKey}", Styles.Get(StyleKind.Small, 9, sc));
                    GUI.color = Color.white;
                }

                // Custom label field (per input mode)
                float tfX = iSz + lblCW + 10f * sc;
                float tfW = vw - tfX - (hasPicker ? picBW + 3f * sc : 0f) - togW - 8f * sc;
                float tfY = ry + (rowH - 22f * sc) * 0.5f;

                if (string.IsNullOrEmpty(curCustom))
                {
                    GUI.color = new Color(0.35f, 0.35f, 0.40f);
                    GUI.Label(new Rect(tfX + 3, tfY + 1, tfW - 4, 20f * sc), "auto",
                        Styles.Get(StyleKind.Small, 10, sc));
                }
                GUI.color = Color.white;
                string next = GUI.TextField(new Rect(tfX, tfY, tfW, 22f * sc), curCustom, 20,
                    Styles.Get(StyleKind.TextField, 11, sc));
                if (next != curCustom) Prefs.SetCustomLabel(effMI, idx, next);

                if (!string.IsNullOrEmpty(curCustom))
                {
                    var clR = new Rect(tfX + tfW - 18f * sc, tfY + 1, 17f * sc, 20f * sc);
                    GUI.color = new Color(0.80f, 0.28f, 0.28f, 0.90f);
                    GUI.DrawTexture(clR, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(clR, "✕", Styles.Get(StyleKind.Center, 10, sc));
                    if (GUI.Button(clR, GUIContent.none, GUIStyle.none))
                    {
                        Prefs.SetCustomLabel(effMI, idx, "");
                        if (UiState.PickerSlot == idx) UiState.PickerSlot = -1;
                    }
                }

                // [▼] button picker (controller modes only)
                if (hasPicker)
                {
                    bool picOpen = UiState.PickerSlot == idx;
                    var  picR    = new Rect(tfX + tfW + 3f * sc, tfY, picBW, 22f * sc);
                    GUI.color = picOpen ? badgeCol : new Color(0.20f, 0.20f, 0.26f, 0.95f);
                    GUI.DrawTexture(picR, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(picR, "▼", Styles.Get(StyleKind.Center, 10, sc));
                    if (GUI.Button(picR, GUIContent.none, GUIStyle.none))
                        UiState.PickerSlot = picOpen ? -1 : idx;
                }

                // Per-slot enable (persists across sessions)
                GUI.color = Color.white;
                bool en  = Prefs.IsSlotEnabled(idx);
                bool nen = GUI.Toggle(
                    new Rect(vw - togW - 1, ry + rowH * 0.5f - 11f * sc, togW, togW), en, "");
                if (nen != en) Prefs.SetSlotEnabled(idx, nen);

                ry += rowH;
            }
            GUI.EndScrollView();
        }

        // ── Plumbing ──────────────────────────────────────────────
        static void DrawSep(float lx, ref float y, float lw, float sc)
        {
            GUI.color = new Color(0.28f, 0.28f, 0.38f, 0.90f);
            GUI.DrawTexture(new Rect(lx, y, lw, 1), Texture2D.whiteTexture);
            y += 9f * sc;
            GUI.color = Color.white;
        }

        static float SliderRow(float x, float y, float w, float sc,
            string label, MelonLoader.MelonPreferences_Entry<float> pref, float lo, float hi, string fmt)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, 18 * sc), $"{label}: {pref.Value.ToString(fmt)}",
                Styles.Get(StyleKind.Left, 12, sc));
            y += 18f * sc;
            pref.Value = GUI.HorizontalSlider(new Rect(x, y, w, 14 * sc), pref.Value, lo, hi);
            return y + 18f * sc;
        }

        static void HandleDrag()
        {
            var ev = Event.current;
            if (ev == null) return;
            float sc = Mathf.Clamp(Prefs.MenuScale.Value, 0.7f, 2.0f);
            var hR = new Rect(_rect.x, _rect.y, _rect.width, 26f * sc);
            switch (ev.type)
            {
                case EventType.MouseDown when hR.Contains(ev.mousePosition):
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
