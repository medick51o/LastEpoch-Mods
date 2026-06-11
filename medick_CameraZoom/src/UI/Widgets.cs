using MelonLoader;
using UnityEngine;

namespace medick_CameraZoom
{
    // Reusable controls for the settings UI. All sizes are design units
    // multiplied by the current panel scale (sc).
    internal static class Widgets
    {
        // Small-caps gold section title + hairline to the right edge.
        public static void SectionHeader(float x, ref float y, float w, float sc, string title)
        {
            float h = 14f * sc;
            var titleStyle = Theme.Label(Mathf.RoundToInt(9 * sc), FontStyle.Bold);
            float titleW = titleStyle.CalcSize(new GUIContent(title)).x;
            Theme.Text9(new Rect(x, y, titleW + 2, h), title, Theme.AccentDim,
                Mathf.RoundToInt(9 * sc), FontStyle.Bold);
            float lineStart = x + titleW + 8f * sc;
            if (x + w > lineStart)
                Theme.Fill(new Rect(lineStart, y + h * 0.5f, x + w - lineStart, 1f), Theme.Border);
            y += 20f * sc;
        }

        // Modern two-state switch; returns the (possibly toggled) value.
        public static bool Switch(Rect r, bool value, float sc)
        {
            GUI.color = Color.white;
            Theme.Box(r, value ? Theme.SwitchOn : Theme.SwitchOff);
            float inset = 3f * sc;
            float k = r.height - inset * 2f;
            var knob = value
                ? new Rect(r.xMax - k - inset, r.y + inset, k, k)
                : new Rect(r.x + inset,        r.y + inset, k, k);
            Theme.Fill(knob, value ? Theme.TextDark : Theme.TextMut);
            return GUI.Button(r, GUIContent.none, GUIStyle.none) ? !value : value;
        }

        // Slider with custom groove/fill/thumb (no default Unity skin).
        public static float Slider(Rect r, float value, float lo, float hi, float sc)
        {
            GUI.color = Color.white;
            float grooveH = Mathf.Max(3f, 3f * sc);
            var groove = new Rect(r.x, r.y + (r.height - grooveH) * 0.5f, r.width, grooveH);
            Theme.Box(groove, Theme.InsetBox);
            float t = Mathf.InverseLerp(lo, hi, value);
            if (t > 0.001f)
                Theme.Fill(new Rect(groove.x + 1, groove.y + 1,
                    (groove.width - 2) * t, grooveH - 2), Theme.AccentDim);
            return GUI.HorizontalSlider(r, value, lo, hi, GUIStyle.none, Theme.SliderThumb(sc));
        }

        // "Label ........ [slider] [value]" row; writes the pref.
        // All row helpers advance the layout cursor via `ref y`.
        public static void SliderRow(float x, ref float y, float w, float sc,
            string label, MelonPreferences_Entry<float> pref, float lo, float hi, string fmt)
        {
            float v = LiveSliderRow(x, ref y, w, sc, label, pref.Value, lo, hi, fmt);
            if (v != pref.Value) pref.Value = v;
        }

        // Same row shape for values that are not preference-backed
        // (e.g. the live target zoom). Returns the new value, advances y.
        public static float LiveSliderRow(float x, ref float y, float w, float sc,
            string label, float value, float lo, float hi, string fmt)
        {
            float rowH  = 22f * sc;
            // Family default is 0.40 (CooldownTracker); widened here so the
            // "(game N)" suffixed labels from SettingsPanel.ZoomLabel fit.
            float labW  = w * 0.46f;
            float chipW = 44f * sc;

            Theme.Text9(new Rect(x, y, labW, rowH), label, Theme.Text, Mathf.RoundToInt(10 * sc));

            var chip = new Rect(x + w - chipW, y + 2f * sc, chipW, rowH - 4f * sc);
            GUI.color = Color.white;
            Theme.Box(chip, Theme.InsetBox);
            Theme.Text9(chip, value.ToString(fmt), Theme.Accent,
                Mathf.RoundToInt(9 * sc), FontStyle.Normal, TextAnchor.MiddleCenter);

            var sr = new Rect(x + labW + 6f * sc, y, w - labW - chipW - 14f * sc, rowH);
            float result = Slider(sr, value, lo, hi, sc);
            y += rowH + 4f * sc;
            return result;
        }

        // "Label ............................ [switch]" row.
        public static void SwitchRow(float x, ref float y, float w, float sc,
            string label, MelonPreferences_Entry<bool> pref)
        {
            float rowH = 22f * sc;
            Theme.Text9(new Rect(x, y, w - 44f * sc, rowH), label, Theme.Text, Mathf.RoundToInt(10 * sc));
            float sw = 34f * sc, sh = 16f * sc;
            bool next = Switch(new Rect(x + w - sw, y + (rowH - sh) * 0.5f, sw, sh), pref.Value, sc);
            if (next != pref.Value) pref.Value = next;
            y += rowH + 4f * sc;
        }

        // "Label ............................ [button]" row; returns clicked.
        public static bool ButtonRow(float x, ref float y, float w, float sc,
            string label, string buttonText, bool selected = false)
        {
            float rowH = 22f * sc;
            Theme.Text9(new Rect(x, y, w - 80f * sc, rowH), label, Theme.Text, Mathf.RoundToInt(10 * sc));
            GUI.color = Color.white;
            bool clicked = GUI.Button(new Rect(x + w - 76f * sc, y + 1f * sc, 76f * sc, rowH - 2f * sc),
                buttonText, Theme.Button(Mathf.RoundToInt(9 * sc), selected));
            y += rowH + 4f * sc;
            return clicked;
        }

        // Status line with a coloured dot.
        public static void StatusRow(float x, ref float y, float w, float sc, string text, Color dotColor)
        {
            float h = 16f * sc;
            float d = 6f * sc;
            Theme.Fill(new Rect(x + 1, y + (h - d) * 0.5f, d, d), dotColor);
            Theme.Text9(new Rect(x + d + 6f * sc, y, w - d - 6f * sc, h), text,
                Theme.TextMut, Mathf.RoundToInt(9 * sc));
            y += h + 4f * sc;
        }
    }
}
