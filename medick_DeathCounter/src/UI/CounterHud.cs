using medick_DeathCounter.Game;
using UnityEngine;

namespace medick_DeathCounter.UI
{
    // The always-on pill: "DEATHS 12". Red-flashes for a few seconds after a
    // death with a "killed by" line under it.
    //   click        open / close the Last Death panel
    //   shift-drag   move it (position saved as a screen fraction, so it
    //                stays put across resolutions)
    //
    // Anchor rule (Terrible Cooldowns law): no player, draw nothing. No ghost
    // counter on the login or character select screens.
    internal static class CounterHud
    {
        const float FreshSeconds = 8f;

        public static bool MouseOver { get; private set; }
        static bool    _dragging;
        static Vector2 _grab;

        public static void Draw()
        {
            MouseOver = false;
            if (!Prefs.ShowCounter.Value || !PlayerProbe.HasPlayer) { _dragging = false; return; }

            float sc = Prefs.HudScale.Value;
            float since = Time.unscaledTime - DeathTracker.JustDiedAt;
            bool fresh = since >= 0f && since < FreshSeconds && DeathTracker.JustDied != null;

            var lblSt = Theme.Label(Mathf.RoundToInt(10 * sc), FontStyle.Bold);
            var numSt = Theme.Label(Mathf.RoundToInt(17 * sc), FontStyle.Bold, TextAnchor.MiddleLeft, serif: true);
            var subSt = Theme.Label(Mathf.RoundToInt(9 * sc));

            string lbl = Prefs.Tracking.Value ? "DEATHS" : "DEATHS (paused)";
            string num = DeathTracker.CharacterDeaths.ToString();
            int sess = DeathTracker.SessionDeaths;
            string sub = sess > 0 ? $"+{sess} this session" : null;

            float pad = 10f * sc, gap = 7f * sc, h = 28f * sc;
            float w = pad + Theme.Width(lbl, lblSt) + gap + Theme.Width(num, numSt)
                    + (sub != null ? gap + Theme.Width(sub, subSt) : 0f) + pad;
            float x = Mathf.Clamp(Prefs.HudX.Value * Screen.width - w * 0.5f, 0f, Screen.width - w);
            float y = Mathf.Clamp(Prefs.HudY.Value * Screen.height, 0f, Screen.height - h);
            var r = new Rect(x, y, w, h);

            HandleInput(r);

            Theme.Box(r, Theme.Panel);
            if (fresh)
            {
                // Fade from full blood to dim across the fresh window.
                float k = 1f - since / FreshSeconds;
                Theme.DrawBorder(r, Color.Lerp(Theme.Border, Theme.Blood, 0.35f + 0.65f * k * (0.6f + 0.4f * Mathf.Sin(since * 8f))), Mathf.Max(1f, 2f * sc));
            }
            else if (MouseOver)
                Theme.DrawBorder(r, Theme.AccentDim, 1f);

            float cx = r.x + pad;
            float lw = Theme.Width(lbl, lblSt);
            Theme.Write(new Rect(cx, r.y, lw + 2, h), lbl, Theme.TextMut, lblSt);
            cx += lw + gap;
            float nw = Theme.Width(num, numSt);
            Theme.Write(new Rect(cx, r.y, nw + 2, h), num, fresh ? Theme.Blood : Theme.TextHi, numSt);
            cx += nw + gap;
            if (sub != null) Theme.Write(new Rect(cx, r.y, Theme.Width(sub, subSt) + 2, h), sub, Theme.TextMut, subSt);

            if (fresh && Prefs.ShowDeathToast.Value)
            {
                var d = DeathTracker.JustDied;
                string toast = $"Killed by {d.KillerLine()}  ·  {Prefs.PanelKeyCode} for details";
                var tSt = Theme.Label(Mathf.RoundToInt(11 * sc), FontStyle.Bold, TextAnchor.MiddleCenter);
                float tw = Theme.Width(toast, tSt) + 20f * sc;
                var tr = new Rect(r.center.x - tw * 0.5f, r.yMax + 4f * sc, tw, 22f * sc);
                Theme.Box(tr, Theme.Panel);
                Theme.Write(tr, toast, Theme.TextHi, tSt);
            }
        }

        static void HandleInput(Rect r)
        {
            var ev = Event.current;
            if (ev == null) return;
            MouseOver = r.Contains(ev.mousePosition) || _dragging;

            switch (ev.type)
            {
                case EventType.MouseDown when ev.button == 0 && r.Contains(ev.mousePosition):
                    if (ev.shift) { _dragging = true; _grab = ev.mousePosition - r.center; }
                    else DeathPanel.Toggle();
                    ev.Use();
                    break;
                case EventType.MouseDrag when _dragging:
                    var c = ev.mousePosition - _grab;
                    Prefs.HudX.Value = Mathf.Clamp01(c.x / Mathf.Max(1, Screen.width));
                    Prefs.HudY.Value = Mathf.Clamp01((c.y - r.height * 0.5f) / Mathf.Max(1, Screen.height));
                    ev.Use();
                    break;
                case EventType.MouseUp when _dragging:
                    _dragging = false;
                    Prefs.Save();
                    ev.Use();
                    break;
            }
        }
    }
}
