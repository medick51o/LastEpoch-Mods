using System.Collections.Generic;
using UnityEngine;

namespace medick_CooldownTracker
{
    // Draws the floating cooldown icons above the player.
    //
    // Anchor rule: if there is no living player to anchor to, draw NOTHING.
    // (v4.x guessed a fallback screen position when the anchor was missing,
    // which is exactly why ghost icons appeared on the login screen.)
    //
    // Move mode: while the settings panel is open and Move is engaged, every
    // enabled slot renders as a draggable preview cluster — drag it where you
    // want the icons to float and the Offset X/Y prefs (and sliders) follow.
    internal static class OverheadRenderer
    {
        static readonly List<SlotData> _buf = new();
        static bool    _dragging;
        static Vector2 _dragStart;
        static float   _startOX, _startOY;

        public static void Draw()
        {
            var cam = Camera.main;
            if (cam == null) return;

            bool moveMode = UiState.MoveIcons && UiState.ShowSettings;
            if (moveMode) SlotRegistry.SnapshotEnabled(_buf);
            else          SlotRegistry.SnapshotActive(_buf);
            if (_buf.Count == 0) { _dragging = false; return; }
            if (!TryGetPlayerAnchor(cam, out Vector2 anchor)) { _dragging = false; return; }

            float sz    = Mathf.Clamp(Prefs.Size.Value, 32f, 120f);
            float gap   = 8f;
            float alpha = Mathf.Clamp01(Prefs.Alpha.Value);
            if (moveMode) alpha = Mathf.Max(alpha, 0.9f);

            float totalW  = _buf.Count * (sz + gap) - gap;
            float startX  = anchor.x + Prefs.OffsetX.Value - totalW * 0.5f;
            float startY  = anchor.y + Prefs.OffsetY.Value - sz * 0.5f;
            var   cluster = new Rect(startX, startY, totalW, sz);

            if (moveMode) HandleMoveDrag(cluster);

            for (int i = 0; i < _buf.Count; i++)
                DrawIcon(new Rect(startX + i * (sz + gap), startY, sz, sz), _buf[i], alpha, moveMode);

            if (moveMode)
            {
                var hit = Theme.Pad(cluster, 8f);
                Theme.DrawBorder(hit, Theme.Accent, 2f);
                Theme.Text9(new Rect(hit.x - 60f, hit.y - 24f, hit.width + 120f, 20f),
                    "drag to reposition — press Lock when done",
                    Theme.Accent, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
        }

        static void HandleMoveDrag(Rect cluster)
        {
            var ev = Event.current;
            if (ev == null) return;
            var hit = Theme.Pad(cluster, 8f);
            switch (ev.type)
            {
                case EventType.MouseDown when hit.Contains(ev.mousePosition)
                                              && !SettingsPanel.PanelRect.Contains(ev.mousePosition):
                    _dragging  = true;
                    _dragStart = ev.mousePosition;
                    _startOX   = Prefs.OffsetX.Value;
                    _startOY   = Prefs.OffsetY.Value;
                    ev.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    var d = ev.mousePosition - _dragStart;
                    Prefs.OffsetX.Value = Mathf.Clamp(_startOX + d.x, -500f, 500f);
                    Prefs.OffsetY.Value = Mathf.Clamp(_startOY + d.y, -600f, 200f);
                    ev.Use();
                    break;
                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    Prefs.Save();
                    break;
            }
        }

        static bool TryGetPlayerAnchor(Camera cam, out Vector2 gui)
        {
            gui = default;
            foreach (var s in _buf)
            {
                try
                {
                    var src = s.Source;
                    if (src == null) continue;          // Unity-overloaded null check
                    var player = src.player;
                    if (player == null) continue;
                    var sp = cam.WorldToScreenPoint(player.transform.position);
                    if (sp.z <= 0f) continue;
                    gui = new Vector2(sp.x, Screen.height - sp.y);
                    return true;
                }
                catch { }                                // collected Il2Cpp reference
            }
            return false;
        }

        static void DrawIcon(Rect r, SlotData s, float alpha, bool preview)
        {
            // Backplate
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.55f);
            GUI.DrawTexture(Theme.Pad(r, 3), Texture2D.whiteTexture);

            // Skill sprite (or dim placeholder)
            GUI.color = new Color(1f, 1f, 1f, alpha);
            bool drewSprite = false;
            try
            {
                if (s.Icon != null && s.Icon.texture != null)
                { Theme.DrawSprite(r, s.Icon); drewSprite = true; }
            }
            catch { }
            if (!drewSprite)
            {
                GUI.color = new Color(0.30f, 0.30f, 0.35f, alpha);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
            }

            // Cooldown sweep (top-down fill) — not in preview, icons there are "ready"
            bool cooling = !preview && s.OnCooldown && s.Fill > 0.005f;
            if (cooling)
            {
                GUI.color = new Color(0f, 0f, 0f, alpha * 0.70f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * s.Fill), Texture2D.whiteTexture);
            }

            // Border: orange while cooling, green when nearly ready
            bool nearReady = !cooling || s.Fill < 0.10f;
            Theme.DrawBorder(r, nearReady
                ? new Color(0.25f, 1f, 0.35f, alpha)
                : new Color(1f, 0.55f, 0.1f, alpha * 0.85f), 2);

            // Label strip ("Flame Ward" renders stacked as Flame / Ward)
            float lh = s.TwoLine
                ? Mathf.Max(28f, r.height * 0.42f)
                : Mathf.Max(15f, r.height * 0.23f);
            Rect lr = new(r.x, r.yMax - lh, r.width, lh);
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.80f);
            GUI.DrawTexture(lr, Texture2D.whiteTexture);

            Color lblCol = nearReady
                ? new Color(0.3f, 1f, 0.5f, alpha)
                : new Color(1f, 0.92f, 0.55f, alpha);
            int fontSize = Mathf.Max(8, Mathf.RoundToInt(r.height * (s.TwoLine ? 0.13f : 0.20f)));
            GUI.color = lblCol;
            GUI.Label(lr, s.DisplayLabel, Theme.IconLabel(fontSize, lblCol));
            GUI.color = Color.white;
        }
    }
}
