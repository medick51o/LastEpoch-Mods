using System.Collections.Generic;
using UnityEngine;

namespace medick_CameraZoom
{
    // The "Terrible" family design system — one dark Last-Epoch-native
    // palette, one gold accent, 9-sliced bordered surfaces, cached styles.
    // Shared visual language with Terrible Cooldowns/Tooltips/Inventory.
    //
    // Must be initialised from inside OnGUI — GUI.skin is invalid elsewhere.
    internal static class Theme
    {
        // ── Palette ───────────────────────────────────────────────
        public static readonly Color Bg        = Hex(0x0C0E13, 0.97f);
        public static readonly Color Surface   = Hex(0x161A24);
        public static readonly Color SurfaceHi = Hex(0x202637);
        public static readonly Color Inset     = Hex(0x10131B);
        public static readonly Color Border    = Hex(0x2A3040);
        public static readonly Color BorderHi  = Hex(0x3C4456);
        public static readonly Color Accent    = Hex(0xC9A653);   // LE gold
        public static readonly Color AccentDim = Hex(0x8A7339);
        public static readonly Color TextHi    = Hex(0xEDE6D4);
        public static readonly Color Text      = Hex(0xC6C2B6);
        public static readonly Color TextMut   = Hex(0x807D8C);
        public static readonly Color TextDark  = Hex(0x14110A);
        public static readonly Color Ready     = Hex(0x59C97B);
        public static readonly Color Warning   = Hex(0xD98E3B);
        public static readonly Color Danger    = Hex(0xB5484D);

        static Color Hex(int rgb, float a = 1f) => new(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8)  & 0xFF) / 255f,
            ( rgb        & 0xFF) / 255f, a);

        // ── Textures & core styles ────────────────────────────────
        static bool _ready;
        public static GUIStyle Panel, Card, InsetBox, SwitchOn, SwitchOff;
        static GUIStyle _btn, _btnSelected, _btnDanger, _label;
        static GUIStyle _sliderThumb;
        static Font _serif;
        static readonly Dictionary<(int size, FontStyle fs, TextAnchor anchor, bool serif), GUIStyle> _labels = new();
        static readonly Dictionary<(int size, int kind), GUIStyle> _buttons = new();

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;

            try { _serif = Font.CreateDynamicFontFromOSFont("Georgia", 14); }
            catch { _serif = null; }

            Panel     = BoxStyle(Bg,      Border);
            Card      = BoxStyle(Surface, Border);
            InsetBox  = BoxStyle(Inset,   Border);
            SwitchOn  = BoxStyle(Accent,  AccentDim);
            SwitchOff = BoxStyle(Inset,   Border);

            _btn = BoxStyle(Surface, Border);
            _btn.alignment = TextAnchor.MiddleCenter;
            _btn.normal.textColor = Text;
            _btn.hover.background  = Bordered(SurfaceHi, BorderHi);
            _btn.hover.textColor   = TextHi;
            _btn.active.background = Bordered(Inset, AccentDim);
            _btn.active.textColor  = TextHi;

            _btnSelected = BoxStyle(Accent, AccentDim);
            _btnSelected.alignment = TextAnchor.MiddleCenter;
            _btnSelected.normal.textColor = TextDark;
            _btnSelected.hover.background = Bordered(Accent, Accent);
            _btnSelected.hover.textColor  = TextDark;

            _btnDanger = BoxStyle(Surface, Border);
            _btnDanger.alignment = TextAnchor.MiddleCenter;
            _btnDanger.normal.textColor = TextMut;
            _btnDanger.hover.background = Bordered(Danger, Danger);
            _btnDanger.hover.textColor  = TextHi;

            _label = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(0, 0, 0, 0),
            };
            _label.normal.textColor = Color.white;   // tinted via GUI.color at draw time

            _sliderThumb = new GUIStyle();
            _sliderThumb.normal.background = Bordered(Accent, AccentDim);
            _sliderThumb.hover.background  = Bordered(Accent, Accent);
            _sliderThumb.border = new RectOffset(1, 1, 1, 1);
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

        // ── Cached accessors ──────────────────────────────────────
        // Text colour is applied with GUI.color at draw time (multiplies the
        // style's white), so one style serves every palette role.
        public static GUIStyle Label(int fontSize, FontStyle fs = FontStyle.Normal,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool serif = false)
        {
            var key = (fontSize, fs, anchor, serif && _serif != null);
            if (_labels.TryGetValue(key, out var st)) return st;
            st = new GUIStyle(_label) { fontSize = fontSize, fontStyle = fs, alignment = anchor };
            if (serif && _serif != null) st.font = _serif;
            _labels[key] = st;
            return st;
        }

        public static GUIStyle Button(int fontSize, bool selected = false, bool danger = false)
        {
            int kind = danger ? 2 : selected ? 1 : 0;
            var key = (fontSize, kind);
            if (_buttons.TryGetValue(key, out var st)) return st;
            st = new GUIStyle(danger ? _btnDanger : selected ? _btnSelected : _btn) { fontSize = fontSize };
            _buttons[key] = st;
            return st;
        }

        public static GUIStyle SliderThumb(float sc)
        {
            _sliderThumb.fixedWidth  = Mathf.Round(10f * sc);
            _sliderThumb.fixedHeight = Mathf.Round(14f * sc);
            return _sliderThumb;
        }

        // ── Primitive draw helpers ────────────────────────────────
        public static void Box(Rect r, GUIStyle style) => GUI.Label(r, GUIContent.none, style);

        public static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        public static void Text9(Rect r, string text, Color c, int size,
            FontStyle fs = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleLeft, bool serif = false)
        {
            GUI.color = c;
            GUI.Label(r, text, Label(size, fs, anchor, serif));
            GUI.color = Color.white;
        }
    }
}
