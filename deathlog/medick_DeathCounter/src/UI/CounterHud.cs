using System;
using medick_DeathCounter.Core;
using medick_DeathCounter.Game;
using UnityEngine;

namespace medick_DeathCounter.UI
{
    internal static class CounterHud
    {
        public static bool MouseOver { get; private set; }
        public static bool Moving { get; private set; }
        public static bool Dragging => _dragging;
        static bool _dragging, _hovered;
        static Vector2 _grab;
        static Rect _hint, _lastRect;
        static bool _hasRect;
        public static bool PointerOver()
        {
            if (!PlayerProbe.HasPlayer || (!Prefs.ShowCounter.Value && !Moving) || !_hasRect || !PointerVisible()) return false;
            var pos = Input.mousePosition;
            var mouse = new Vector2(pos.x, Screen.height - pos.y);
            return _lastRect.Contains(mouse) || (_hovered && _hint.Contains(mouse)) || _dragging;
        }
        public static void BeginMove()
        {
            Moving = true;
            InputBlocker.TakeFocus();
            Prefs.ShowCounter.Value = true;
            DeathPanel.Close();
        }
        public static void EndMove()
        {
            Moving = false;
            _dragging = false;
            Prefs.Save();
        }
        static bool PointerVisible()
        {
            // A hidden controller cursor can retain its last mouse position.
            // It must not expand the HUD or block controller movement.
            var visible = Refl.Static("UnityEngine.Cursor", "visible");
            if (visible is bool yes) return yes;
            return Refl.Static("Il2Cpp.EpochInputManager", "get_IsControllerActive") is not true;
        }
        public static void Draw()
        {
            MouseOver = false;
            if ((!Prefs.ShowCounter.Value && !Moving) || !PlayerProbe.HasPlayer)
            { _dragging = false; _hovered = false; return; }
            float sc = Mathf.Clamp(Prefs.HudScale.Value, 0.9f, 2f);
            var number = Theme.Label(Mathf.RoundToInt(20 * sc), FontStyle.Bold, TextAnchor.MiddleCenter);
            var label = Theme.Label(Mathf.RoundToInt(14 * sc), FontStyle.Bold);
            var small = Theme.Label(Mathf.RoundToInt(14 * sc));
            string count = DeathTracker.CharacterDeaths.ToString();
            float handleW = 40f * sc;
            float idleW = Mathf.Max(34f * sc, Theme.Width(count, number) + 18f * sc);
            float expandedW = handleW + Theme.Width("Death counter", label) + Theme.Width(count, number) + 36f * sc;
            float h = 32f * sc;
            bool pointer = PointerVisible();
            if (!pointer && Moving) EndMove();
            var ev = Event.current;
            Rect previous = Position(_hovered || Moving || _dragging ? expandedW : idleW, h);
            bool hover = pointer && ev != null && (previous.Contains(ev.mousePosition) || (_hovered && _hint.Contains(ev.mousePosition)));
            _hovered = hover;
            bool active = Moving || _dragging || hover;
            Rect rect = Position(active ? expandedW : idleW, h);
            if (pointer && !DeathPanel.Open) HandleInput(rect, handleW);
            else _dragging = false;
            rect = Position(active ? expandedW : idleW, h);
            _lastRect = rect; _hasRect = true;
            GUI.color = Theme.WithAlpha(Color.white, active ? 1f : 0.12f);
            Theme.Box(rect, Theme.Panel);
            GUI.color = Color.white;
            if (active)
            {
                Theme.Fill(new Rect(rect.x, rect.y, handleW, h), Theme.SurfaceHi);
                Theme.Write(new Rect(rect.x, rect.y, handleW, h), "MOVE", Theme.TextHi, Theme.Label(Mathf.RoundToInt(12 * sc), FontStyle.Bold, TextAnchor.MiddleCenter));
                Theme.Write(new Rect(rect.x + handleW + 10f * sc, rect.y, Theme.Width("Death counter", label) + 3f, h), "Death counter", Theme.TextHi, label);
                Theme.Write(new Rect(rect.xMax - Theme.Width(count, number) - 18f * sc, rect.y, Theme.Width(count, number) + 8f * sc, h), count, Theme.Accent, number);
            }
            else
            {
                // Idle: only the number draws attention. The faint label has
                // no panel, handle, session text or extra interactive footprint.
                var faint = Theme.Label(Mathf.RoundToInt(12 * sc), FontStyle.Normal, TextAnchor.MiddleRight);
                float labelW = Theme.Width("deaths", faint) + 6f * sc;
                float lx = rect.x - labelW;
                if (lx < 0f) lx = rect.xMax + 2f;
                lx = Mathf.Clamp(lx, 0f, Mathf.Max(0f, Screen.width - labelW));
                Theme.Write(new Rect(lx, rect.y, labelW, h), "deaths", Theme.WithAlpha(Theme.Text, 0.12f), faint);
                Theme.Write(rect, count, Theme.TextHi, number);
            }
            if (Moving || _dragging) Theme.DrawBorder(rect, Theme.Accent, 2f);
            if (active)
            {
                string help = Moving ? "Drag the counter to position it." : $"Drag MOVE to reposition. Click for the log. +{DeathTracker.SessionDeaths} this session.";
                float helpW = Mathf.Min(Screen.width - 16f, Theme.Width(help, small) + 24f * sc + (Moving ? 82f * sc : 0f));
                float textW = helpW - (Moving ? 102f * sc : 24f * sc);
                float hintH = Theme.Wrap(help, small, textW).Count * (Theme.LineHeight(small) + 4f) + 14f * sc;
                hintH = Mathf.Max(38f * sc, hintH);
                float hx = Mathf.Clamp(rect.x + (rect.width - helpW) * 0.5f, 8f, Mathf.Max(8f, Screen.width - helpW - 8f));
                float hy = rect.yMax; // touching keeps hover stable when moving to the help
                if (hy + hintH > Screen.height - 8f) hy = rect.y - hintH;
                _hint = new Rect(hx, hy, helpW, hintH);
                Theme.Box(_hint, Theme.Panel);
                Theme.Para(hx + 12f * sc, hy + 7f * sc, textW, help, Theme.TextHi, small);
                if (pointer && ev != null && _hint.Contains(ev.mousePosition)) MouseOver = true;
                if (Moving && GUI.Button(new Rect(_hint.xMax - 78f * sc, hy + 4f * sc, 72f * sc, hintH - 8f * sc), "Done", Theme.Button(Mathf.RoundToInt(14 * sc))))
                { EndMove(); DeathPanel.Toggle(); }
            }
            else if (Prefs.ShowDeathToast.Value && DeathTracker.JustDied?.Character == DeathTracker.Character && Time.unscaledTime - DeathTracker.JustDiedAt < 8f)
            {
                var death = DeathTracker.JustDied;
                string step = ToastStep(death);
                string next = step == null ? "" : $" Next: {step}.";
                string toast = string.IsNullOrEmpty(death.Killer) && string.IsNullOrEmpty(death.KillingAilment)
                    ? $"Death recorded.{next} {Prefs.PanelKeyCode} opens the log."
                    : $"Killed by {death.KillerLine()}.{next} {Prefs.PanelKeyCode} for details.";
                float tw = Mathf.Min(Screen.width - 16f, Theme.Width(toast, small) + 24f * sc);
                float tx = Mathf.Clamp(rect.x + (rect.width - tw) * 0.5f, 8f, Mathf.Max(8f, Screen.width - tw - 8f));
                float th = Theme.Wrap(toast, small, tw - 24f * sc).Count * (Theme.LineHeight(small) + 4f) + 14f * sc;
                float ty = rect.yMax + 5f * sc;
                if (ty + th > Screen.height - 8f) ty = rect.y - th - 5f * sc;
                Theme.Box(new Rect(tx, ty, tw, th), Theme.Panel);
                Theme.Para(tx + 12f * sc, ty + 7f * sc, tw - 24f * sc, toast, Theme.TextHi, small);
            }
        }
        // Late game reports can enrich JustDied after the toast appears, so
        // the cached step follows the same signature the log uses.
        static DeathRecord _toastFor;
        static int _toastSignature;
        static string _toastStep;
        static string ToastStep(DeathRecord death)
        {
            int signature = DeathPanel.Signature(death);
            if (!ReferenceEquals(death, _toastFor) || signature != _toastSignature)
            {
                _toastFor = death; _toastSignature = signature;
                try { _toastStep = Advisor.ToastStep(death); }
                catch (Exception ex) { _toastStep = null; Dbg.Log("toast advice: " + ex.Message); }
            }
            return _toastStep;
        }
        static Rect Position(float w, float h) => new(
            Mathf.Clamp(Prefs.HudX.Value * Screen.width - w * 0.5f, 8f, Mathf.Max(8f, Screen.width - w - 8f)),
            Mathf.Clamp(Prefs.HudY.Value * Screen.height, 8f, Mathf.Max(8f, Screen.height - h - 8f)), w, h);
        static void HandleInput(Rect r, float handleW)
        {
            var ev = Event.current;
            if (ev == null) return;
            MouseOver = r.Contains(ev.mousePosition) || _dragging;
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                if (Moving || shift || ev.mousePosition.x < r.x + handleW)
                { _dragging = true; _grab = ev.mousePosition - new Vector2(r.x + r.width * 0.5f, r.y); }
                else { InputBlocker.TakeFocus(); DeathPanel.Toggle(); }
                ev.Use();
            }
            else if (ev.type == EventType.MouseDrag && _dragging)
            {
                Vector2 at = ev.mousePosition - _grab;
                float x = Mathf.Clamp(at.x - r.width * 0.5f, 8f, Mathf.Max(8f, Screen.width - r.width - 8f));
                float y = Mathf.Clamp(at.y, 8f, Mathf.Max(8f, Screen.height - r.height - 8f));
                Prefs.HudX.Value = (x + r.width * 0.5f) / Mathf.Max(1, Screen.width);
                Prefs.HudY.Value = y / Mathf.Max(1, Screen.height);
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp && _dragging)
            { _dragging = false; Prefs.Save(); ev.Use(); }
        }
    }
}
