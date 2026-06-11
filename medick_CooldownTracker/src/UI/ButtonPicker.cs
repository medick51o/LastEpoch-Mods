using UnityEngine;

namespace medick_CooldownTracker
{
    // [▼] popup with the real controller glyphs — keycap chips on the
    // Terrible Cooldowns design system; face buttons keep their real-world
    // pad colours (that's semantics, not decoration).
    internal static class ButtonPicker
    {
        public static void Draw(int effMI)
        {
            float sc = Mathf.Clamp(Prefs.MenuScale.Value, 0.7f, 2.0f);
            var anchor = SettingsPanel.PanelRect;

            float pw = 228f * sc;
            float ph = 224f * sc;

            // Prefer the right of the settings panel, fall back left, then overlap.
            float px = anchor.xMax + 10f;
            if (px + pw > Screen.width) px = anchor.x - pw - 10f;
            if (px < 0) px = anchor.x + (anchor.width - pw) * 0.5f;
            float py = Mathf.Clamp(anchor.y, 0, Mathf.Max(0, Screen.height - ph));
            var panel = new Rect(px, py, pw, ph);

            GUI.color = Color.white;
            Theme.Box(panel, Theme.Panel);

            // Title bar
            float th = 24f * sc;
            Theme.Fill(new Rect(px + 1, py + 1, pw - 2, th - 1), Theme.Surface);
            Theme.Fill(new Rect(px + 1, py + th - 2f, pw - 2, 2f), Theme.AccentDim);
            float d = 6f * sc;
            Theme.Fill(new Rect(px + 8f * sc, py + (th - d) * 0.5f, d, d), SettingsPanel.ModeDot(effMI));
            Theme.Text9(new Rect(px + 8f * sc + d + 6f * sc, py, pw * 0.8f, th),
                $"{(effMI == 1 ? "Xbox" : "PS5")} glyphs — slot {UiState.PickerSlot}",
                Theme.TextHi, Mathf.RoundToInt(10 * sc), FontStyle.Bold);

            float cs = 16f * sc;
            GUI.color = Color.white;
            if (GUI.Button(new Rect(px + pw - cs - 5f * sc, py + (th - cs) * 0.5f, cs, cs),
                    "✕", Theme.Button(Mathf.RoundToInt(8 * sc), danger: true)))
            { UiState.PickerSlot = -1; return; }

            float y = py + th + 7f * sc;
            if (effMI == 2)
            {
                y = Group(px, y, pw, sc, "Face",    ButtonLabels.PsFaceSyms, ButtonLabels.PsFaceCols, 30);
                y = Group(px, y, pw, sc, "Bumper",  new[] { "L1", "R1" },  null,     24);
                y = Group(px, y, pw, sc, "Trigger", new[] { "L2", "R2" },  null,     24);
                y = Group(px, y, pw, sc, "Stick",   new[] { "L3", "R3" },  null,     24);
                y = Group(px, y, pw, sc, "D-Pad",   new[] { "↑", "↓", "←", "→" }, null, 24);
                Group(px, y, pw, sc, "Other", new[] { "Opt", "Tpad" }, null, 24);
            }
            else
            {
                y = Group(px, y, pw, sc, "Face",    ButtonLabels.XboxFaceSyms, ButtonLabels.XboxFaceCols, 30);
                y = Group(px, y, pw, sc, "Bumper",  new[] { "LB", "RB" },  null,      24);
                y = Group(px, y, pw, sc, "Trigger", new[] { "LT", "RT" },  null,      24);
                y = Group(px, y, pw, sc, "Stick",   new[] { "LS", "RS" },  null,      24);
                y = Group(px, y, pw, sc, "D-Pad",   new[] { "↑", "↓", "←", "→" }, null, 24);
                Group(px, y, pw, sc, "Other", new[] { "Start", "Back" }, null, 24);
            }
        }

        static float Group(float px, float y, float pw, float sc,
            string groupName, string[] syms, Color[] colors, float btnSz)
        {
            float btnH   = btnSz * sc;
            float gap    = 4f * sc;
            float labW   = 50f * sc;
            float innerX = px + 8f * sc + labW;

            Theme.Text9(new Rect(px + 8f * sc, y, labW, btnH), groupName,
                Theme.TextMut, Mathf.RoundToInt(9 * sc));

            for (int i = 0; i < syms.Length; i++)
            {
                var r = new Rect(innerX + i * (btnH + gap), y, btnH, btnH);
                GUI.color = Color.white;
                bool clicked = GUI.Button(r, GUIContent.none, Theme.Button(10));
                int fs = syms[i].Length <= 2 ? 12 : syms[i].Length <= 4 ? 9 : 7;
                Theme.Text9(r, syms[i],
                    colors != null && i < colors.Length ? colors[i] : Theme.TextHi,
                    Mathf.RoundToInt(fs * sc), FontStyle.Bold, TextAnchor.MiddleCenter);
                if (clicked)
                {
                    Prefs.SetCustomLabel(ButtonLabels.GetModeIndex(), UiState.PickerSlot, syms[i]);
                    UiState.PickerSlot = -1;
                }
            }
            GUI.color = Color.white;
            return y + btnH + gap;
        }
    }
}
