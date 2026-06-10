using UnityEngine;

namespace medick_CooldownTracker
{
    // [▼] popup with the real controller glyphs, colour-coded like the pads.
    internal static class ButtonPicker
    {
        static readonly string[] PS5Face  = { "△", "□", "○", "✕" };
        static readonly Color[]  PS5FaceC =
        {
            new(0.30f, 0.85f, 0.55f),   // △ Triangle – green
            new(0.95f, 0.48f, 0.70f),   // □ Square   – pink
            new(0.95f, 0.28f, 0.28f),   // ○ Circle   – red
            new(0.38f, 0.52f, 0.96f),   // ✕ Cross    – blue
        };
        static readonly string[] XboxFace  = { "A", "B", "X", "Y" };
        static readonly Color[]  XboxFaceC =
        {
            new(0.15f, 0.80f, 0.28f),   // A – green
            new(0.90f, 0.20f, 0.15f),   // B – red
            new(0.22f, 0.45f, 0.95f),   // X – blue
            new(0.95f, 0.78f, 0.05f),   // Y – gold
        };

        public static void Draw(int effMI)
        {
            float sc = Mathf.Clamp(Prefs.MenuScale.Value, 0.7f, 2.0f);
            var anchor = SettingsPanel.PanelRect;

            float pw = 228f * sc;
            float ph = 248f * sc;

            // Prefer the right of the settings panel, fall back left, then overlap.
            float px = anchor.xMax + 10f;
            if (px + pw > Screen.width) px = anchor.x - pw - 10f;
            if (px < 0) px = anchor.x + (anchor.width - pw) * 0.5f;
            float py = Mathf.Clamp(anchor.y, 0, Mathf.Max(0, Screen.height - ph));

            GUI.color = new Color(0.06f, 0.06f, 0.10f, 0.97f);
            GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
            Styles.DrawBorder(new Rect(px, py, pw, ph), new Color(0.30f, 0.30f, 0.40f), 1);

            Color hCol = effMI == 1
                ? new Color(0.60f, 0.34f, 0.08f, 1f)    // Xbox – dark orange
                : new Color(0.40f, 0.12f, 0.55f, 1f);   // PS5  – dark purple
            GUI.color = hCol;
            GUI.DrawTexture(new Rect(px, py, pw, 26f * sc), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string modeName = effMI == 1 ? "Xbox" : "PS5";
            GUI.Label(new Rect(px + 6, py + 4, pw - 30, 18 * sc),
                $"{modeName} buttons  —  slot {UiState.PickerSlot}",
                Styles.Get(StyleKind.CenterBold, 10, sc, TextAnchor.MiddleLeft));

            var xR = new Rect(px + pw - 22f * sc, py + 3, 20f * sc, 20f * sc);
            GUI.color = new Color(0.75f, 0.22f, 0.22f, 1f);
            GUI.DrawTexture(xR, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(xR, "✕", Styles.Get(StyleKind.Center, 10, sc));
            if (GUI.Button(xR, GUIContent.none, GUIStyle.none)) { UiState.PickerSlot = -1; return; }

            float y = py + 30f * sc;
            if (effMI == 2)
            {
                y = Group(px, y, sc, "Face",    PS5Face,                 PS5FaceC, 34);
                y = Group(px, y, sc, "Bumper",  new[] { "L1", "R1" },    null,     28);
                y = Group(px, y, sc, "Trigger", new[] { "L2", "R2" },    null,     28);
                y = Group(px, y, sc, "Stick",   new[] { "L3", "R3" },    null,     28);
                y = Group(px, y, sc, "D-Pad",   new[] { "↑", "↓", "←", "→" }, null, 28);
                Group(px, y, sc, "Other", new[] { "Opt", "Tpad" }, null, 28);
            }
            else
            {
                y = Group(px, y, sc, "Face",    XboxFace,                XboxFaceC, 34);
                y = Group(px, y, sc, "Bumper",  new[] { "LB", "RB" },    null,      28);
                y = Group(px, y, sc, "Trigger", new[] { "LT", "RT" },    null,      28);
                y = Group(px, y, sc, "Stick",   new[] { "LS", "RS" },    null,      28);
                y = Group(px, y, sc, "D-Pad",   new[] { "↑", "↓", "←", "→" }, null, 28);
                Group(px, y, sc, "Other", new[] { "Start", "Back" }, null, 28);
            }
        }

        static float Group(float px, float y, float sc,
            string groupName, string[] syms, Color[] colors, float btnSz)
        {
            float btnH   = btnSz * sc;
            float gap    = 4f * sc;
            float labW   = 54f * sc;
            float innerX = px + 6f + labW;

            GUI.color = new Color(0.60f, 0.60f, 0.65f);
            GUI.Label(new Rect(px + 6, y, labW, btnH), groupName + ":",
                Styles.Get(StyleKind.Left, 9, sc, TextAnchor.MiddleLeft));

            for (int i = 0; i < syms.Length; i++)
            {
                Color c = (colors != null && i < colors.Length)
                    ? colors[i]
                    : new Color(0.20f, 0.20f, 0.26f, 0.95f);
                var r = new Rect(innerX + i * (btnH + gap), y, btnH, btnH);
                GUI.color = c;
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.18f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), Texture2D.whiteTexture);
                GUI.color = Color.white;
                int fs = syms[i].Length <= 2 ? 12 : syms[i].Length <= 4 ? 9 : 7;
                GUI.Label(r, syms[i], Styles.Get(StyleKind.Center, fs, sc));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
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
