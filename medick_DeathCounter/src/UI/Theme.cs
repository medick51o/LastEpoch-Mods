using System.Collections.Generic;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using medick_DeathCounter.Core;
using UnityEngine;

namespace medick_DeathCounter.UI
{
    // The Terrible family palette (shared with Terrible Cooldowns): dark
    // Last-Epoch-native surfaces, one gold accent, plus a blood red for the
    // counter and one colour per damage type. Must be initialised from inside
    // OnGUI: GUI.skin is invalid elsewhere.
    internal static class Theme
    {
        public static readonly Color Bg        = Hex(0x10141C);
        public static readonly Color Surface   = Hex(0x161A24);
        public static readonly Color SurfaceHi = Hex(0x202637);
        public static readonly Color Inset     = Hex(0x10131B);
        public static readonly Color Border    = Hex(0x2A3040);
        public static readonly Color BorderHi  = Hex(0x3C4456);
        public static readonly Color Accent    = Hex(0xC9A653);   // LE gold
        public static readonly Color AccentDim = Hex(0xC9A653);
        public static readonly Color TextHi    = Hex(0xEDE6D4);
        public static readonly Color Text      = Hex(0xE0E4EB);
        public static readonly Color TextMut   = Hex(0xB8C1CF);
        public static readonly Color Blood     = Hex(0xC23B3B);
        public static readonly Color BloodDim  = Hex(0x6E2226);

        static readonly Color[] ElementColors =
        {
            Hex(0xC8B9A6),   // Physical
            Hex(0xFF625E),   // Fire
            Hex(0x6FB7E8),   // Cold
            Hex(0xE8D04A),   // Lightning
            Hex(0x4FB0A0),   // Necrotic
            Hex(0x9B6BD6),   // Void
            Hex(0x7BC043),   // Poison
        };
        public static Color ElementColor(Element e) => ElementColors[(int)e];
        public static Color DamageColor(string element) => Elements.TryParse(element, out var el) ? ElementColor(el) : Text;

        static Color Hex(int rgb, float a = 1f) => new(
            ((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        static bool _ready;
        public static GUIStyle Panel, Card, Chip;
        static GUIStyle _btn, _label;
        static Font _serif;
        static readonly Dictionary<(int, FontStyle, TextAnchor, bool), GUIStyle> _labels = new();
        static readonly Dictionary<int, GUIStyle> _buttons = new();

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;
            try { _serif = Font.CreateDynamicFontFromOSFont("Georgia", 14); } catch { _serif = null; }

            Panel = BoxStyle(Bg, Border);
            Card  = BoxStyle(Surface, Border);
            Chip  = BoxStyle(Inset, BorderHi);

            _btn = BoxStyle(Surface, Border);
            _btn.alignment = TextAnchor.MiddleCenter;
            _btn.normal.textColor  = Text;
            _btn.hover.background  = Bordered(SurfaceHi, BorderHi);
            _btn.hover.textColor   = TextHi;
            _btn.active.background = Bordered(Inset, AccentDim);
            _btn.active.textColor  = TextHi;

            _label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(0, 0, 0, 0) };
            _label.normal.textColor = Color.white;   // tinted with GUI.color at draw time
        }

        static Texture2D Bordered(Color fill, Color border)
        {
            var t = new Texture2D(3, 3, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 3; y++)
                    t.SetPixel(x, y, x == 1 && y == 1 ? fill : border);
            t.Apply();
            return t;
        }

        static GUIStyle BoxStyle(Color fill, Color border)
        {
            var st = new GUIStyle { border = new RectOffset(1, 1, 1, 1) };
            st.normal.background = Bordered(fill, border);
            return st;
        }

        public static GUIStyle Label(int size, FontStyle fs = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft,
            bool serif = false)
        {
            var key = (size, fs, anchor, serif && _serif != null);
            if (_labels.TryGetValue(key, out var st)) return st;
            st = new GUIStyle(_label) { fontSize = size, fontStyle = fs, alignment = anchor };
            if (serif && _serif != null) st.font = _serif;
            _labels[key] = st;
            return st;
        }

        public static GUIStyle Button(int size)
        {
            if (_buttons.TryGetValue(size, out var st)) return st;
            st = new GUIStyle(_btn) { fontSize = size };
            _buttons[size] = st;
            return st;
        }

        // Every Unity call in this mod also appears in a shipped Terrible mod's
        // DLL (built against the game's own interop assemblies): IL2CPP strips
        // members the game never uses, and interop turns fields into
        // properties. So no GUIContent.none (a field in plain Unity), no
        // GUIStyle.CalcHeight / word wrap (unproven), no Color.Lerp or
        // Rect.center. See the no-game build notes in CURRENT-WORK.md.
        static GUIContent _empty;

        public static void Box(Rect r, GUIStyle style) => GUI.Label(r, _empty ??= new GUIContent(""), style);

        public static Color Mix(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, 1f);
        }

        // Greedy word wrap measured with CalcSize, cached per text/style/width.
        // Keyed on the style object itself: styles come from the caches above,
        // so the reference is stable (and GUIStyle getters are unproven).
        static readonly Dictionary<(string, GUIStyle, float), List<string>> _wraps = new();

        public static List<string> Wrap(string text, GUIStyle st, float width)
        {
            var key = (text ?? "", st, Mathf.Round(width));
            if (_wraps.TryGetValue(key, out var lines)) return lines;
            if (_wraps.Count > 512) _wraps.Clear();
            lines = new List<string>();
            string line = "";
            foreach (var word in (text ?? "").Split(' '))
            {
                string attempt = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Width(attempt, st) > width) { lines.Add(line); line = word; }
                else line = attempt;
            }
            if (line.Length > 0 || lines.Count == 0) lines.Add(line);
            _wraps[key] = lines;
            return lines;
        }

        public static float LineHeight(GUIStyle st) => Size("Ag", st).y;

        // Draws text wrapped to width; returns the height used.
        public static float Para(float x, float y, float width, string text, Color c, GUIStyle st)
        {
            float lh = LineHeight(st) + 4f;
            var lines = Wrap(text, st, width);
            for (int i = 0; i < lines.Count; i++)
                Write(new Rect(x, y + i * lh, width, lh), lines[i], c, st);
            return lines.Count * lh;
        }

        static readonly Regex Tokens = new("\\r\\n|\\n|[^\\S\\r\\n]+|[^\\s]+", RegexOptions.Compiled);
        public static float GamePara(float x, float y, float width, string text, GUIStyle st)
        {
            float at = 0, row = 0, lh = LineHeight(st) + 4f;
            foreach (var run in DeathText.Parse(text))
            {
                Color color = GameColor(run.Color);
                foreach (Match match in Tokens.Matches(run.Text))
                {
                    string word = match.Value;
                    if (word.Contains('\n')) { at = 0; row += lh; continue; }
                    float ww = Width(word, st);
                    if (at > 0 && at + ww > width) { at = 0; row += lh; }
                    if (at == 0 && string.IsNullOrWhiteSpace(word)) continue;
                    Write(new Rect(x + at, y + row, ww + 1f, lh), word, color, st);
                    at += ww;
                }
            }
            return row + lh;
        }
        static Color GameColor(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Text;
            if (raw.StartsWith("#", StringComparison.Ordinal) && uint.TryParse(raw.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint rgb))
            {
                if (raw.Length == 9) rgb >>= 8; // preserve color, keep text fully readable
                return Hex((int)rgb);
            }
            return raw.ToLowerInvariant() switch
            {
                "red" => Hex(0xFF5555), "white" => Color.white,
                "yellow" => Hex(0xFFFF00), "orange" => Hex(0xFFA500),
                "green" => Hex(0x00FF00), "blue" => Hex(0x5599FF),
                "purple" => Hex(0xAA77EE), _ => Text,
            };
        }

        public static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void Write(Rect r, string text, Color c, GUIStyle st)
        {
            GUI.color = c;
            GUI.Label(r, text, st);
            GUI.color = Color.white;
        }

        static Vector2 Size(string text, GUIStyle st) => st.CalcSize(new GUIContent(text));
        public static float Width(string text, GUIStyle st) => Size(text, st).x;

        public static void DrawBorder(Rect r, Color c, float bw)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, bw), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.yMax - bw, r.width, bw), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, bw, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - bw, r.y, bw, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);
    }
}
