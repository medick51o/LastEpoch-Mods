using System.Collections.Generic;
using UnityEngine;

namespace medick_CooldownTracker
{
    internal enum StyleKind { Center, CenterBold, Left, Small, SectionHeader, Dim, TextField }

    // GUIStyle factory with a lookup cache so the panel and overhead icons
    // never allocate styles per frame (v4.x created ~a dozen per frame).
    // Must be initialised from inside OnGUI — GUI.skin is invalid elsewhere.
    internal static class Styles
    {
        static bool _ready;
        static GUIStyle _center, _bold, _left, _small, _sectionHdr, _dim, _textField, _iconLabel;
        static readonly Dictionary<(StyleKind kind, int size, TextAnchor anchor), GUIStyle> _cache = new();

        public static void Ensure()
        {
            if (_ready) return;
            _ready = true;

            _center = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            _center.normal.textColor = Color.white;
            _bold = new GUIStyle(_center) { fontStyle = FontStyle.Bold };
            _left = new GUIStyle(_center) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            _left.normal.textColor = Color.white;
            _small = new GUIStyle(_center) { fontSize = 10 };
            _small.normal.textColor = new Color(0.75f, 0.75f, 0.80f);
            _sectionHdr = new GUIStyle(_left) { fontStyle = FontStyle.Bold };
            _sectionHdr.normal.textColor = new Color(0.75f, 0.85f, 1f);
            _dim = new GUIStyle(_left) { fontSize = 11 };
            _dim.normal.textColor = new Color(0.50f, 0.50f, 0.55f);
            _textField = new GUIStyle(GUI.skin.textField) { fontSize = 11 };
            _textField.normal.textColor = Color.white;
            _iconLabel = new GUIStyle(_center) { wordWrap = false };
        }

        static GUIStyle Base(StyleKind k) => k switch
        {
            StyleKind.CenterBold    => _bold,
            StyleKind.Left          => _left,
            StyleKind.Small         => _small,
            StyleKind.SectionHeader => _sectionHdr,
            StyleKind.Dim           => _dim,
            StyleKind.TextField     => _textField,
            _                       => _center,
        };

        // baseSize is pre-scale; the cache key is the final font size, so a
        // changed Menu Size slider naturally resolves to new entries.
        public static GUIStyle Get(StyleKind kind, int baseSize, float scale, TextAnchor? anchor = null)
        {
            var src = Base(kind);
            int size = Mathf.RoundToInt(baseSize * scale);
            var key = (kind, size, anchor ?? src.alignment);
            if (_cache.TryGetValue(key, out var st)) return st;
            st = new GUIStyle(src) { fontSize = size };
            if (anchor.HasValue) st.alignment = anchor.Value;
            _cache[key] = st;
            return st;
        }

        // Single mutable style reused for every overhead icon label.
        public static GUIStyle IconLabel(int fontSize, Color color)
        {
            _iconLabel.fontSize = fontSize;
            _iconLabel.normal.textColor = color;
            return _iconLabel;
        }

        // ── Shared primitive drawing helpers ─────────────────────────
        public static void DrawSprite(Rect r, Sprite s)
        {
            var tex = s.texture; var sr = s.rect;
            GUI.DrawTextureWithTexCoords(r, tex,
                new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height));
        }

        public static void DrawBorder(Rect r, Color c, float bw)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x,         r.y,         r.width, bw),       Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,         r.yMax - bw, r.width, bw),       Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,         r.y,         bw,      r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - bw, r.y,         bw,      r.height), Texture2D.whiteTexture);
        }

        public static Rect Pad(Rect r, float d) => new(r.x - d, r.y - d, r.width + d * 2, r.height + d * 2);
    }
}
