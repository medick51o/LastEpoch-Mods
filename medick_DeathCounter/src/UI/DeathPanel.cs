using System;
using System.Collections.Generic;
using System.Linq;
using medick_DeathCounter.Core;
using medick_DeathCounter.Game;
using UnityEngine;

namespace medick_DeathCounter.UI
{
    // The Last Death panel (PanelKey, default Insert, or click the counter).
    // Newest death first; ‹ › walk back through this character's history.
    //
    //   Killed by <who>                 ‹ 12 / 12 › ✕
    //   <ability> · 1,240 fire · crit
    //   3 min ago · Monolith · lvl 84 · [ONE-SHOT]
    //   AILMENTS ON YOU     [Ignite] [Shock]
    //   DAMAGE, LAST 5s     ███████ fire 72%  ██ physical 20% ...
    //   TOP THREATS         Lagon 1,240 (1 hit) ...
    //   TO SURVIVE NEXT TIME
    //     1. Cap fire resistance (75%): why ...
    //   Log: UserData/medick_DeathCounter/deaths.txt      [OPEN LOG FOLDER]
    internal static class DeathPanel
    {
        public static bool Open { get; private set; }
        public static bool MouseOver { get; private set; }

        static int _back;                 // 0 = newest, 1 = the one before ...
        static float _lastH = 420f;       // last frame's content height (one-frame lag on first open)
        static DeathRecord _tipsFor;
        static List<Advice> _tips = new();
        static List<DeathRecord> _list = new();
        static int _listCount = -1;
        static string _listChar;

        public static void Toggle()
        {
            Open = !Open;
            _back = 0;
        }

        public static void Close() { Open = false; MouseOver = false; }

        public static void Draw()
        {
            MouseOver = false;
            if (!Open) return;

            RefreshList();
            float sc = Prefs.HudScale.Value;
            float w = Mathf.Min(480f * sc, Screen.width - 32f);
            float pad = 14f * sc;
            float x = (Screen.width - w) * 0.5f;
            float top = Mathf.Max(16f, Screen.height * 0.12f);
            var panel = new Rect(x, top, w, _lastH);

            var ev = Event.current;
            if (ev != null) MouseOver = panel.Contains(ev.mousePosition);

            Theme.Box(panel, Theme.Panel);
            Theme.Fill(new Rect(panel.x, panel.y, panel.width, 2f * sc), Theme.BloodDim);

            float cx = x + pad, cw = w - pad * 2f, y = top + pad;

            // ── Header ───────────────────────────────────────
            var hdr = Theme.Label(Mathf.RoundToInt(9 * sc), FontStyle.Bold);
            Theme.Write(new Rect(cx, y, cw, 14f * sc), $"{BuildInfo.DisplayName.ToUpperInvariant()}  ·  {DeathTracker.Character}", Theme.AccentDim, hdr);

            float bs = 20f * sc;
            var btn = Theme.Button(Mathf.RoundToInt(11 * sc));
            if (GUI.Button(new Rect(cx + cw - bs, y - 3f * sc, bs, bs), "✕", btn)) { Close(); return; }

            int n = _list.Count;
            if (n > 0)
            {
                int idx = n - 1 - Mathf.Clamp(_back, 0, n - 1);
                var navSt = Theme.Label(Mathf.RoundToInt(10 * sc), FontStyle.Normal, TextAnchor.MiddleCenter);
                string pos = $"{idx + 1} / {n}";
                float pw = Theme.Width(pos, navSt) + 10f * sc;
                float nx = cx + cw - bs - 8f * sc - bs - pw - bs;
                NavButton(new Rect(nx, y - 3f * sc, bs, bs), "‹", _back < n - 1, btn, navSt, () => _back++);
                Theme.Write(new Rect(nx + bs, y - 3f * sc, pw, bs), pos, Theme.TextMut, navSt);
                NavButton(new Rect(nx + bs + pw, y - 3f * sc, bs, bs), "›", _back > 0, btn, navSt, () => _back--);
            }
            y += 24f * sc;

            if (n == 0)
            {
                var st = Theme.Label(Mathf.RoundToInt(15 * sc), FontStyle.Bold, TextAnchor.MiddleLeft, serif: true);
                string msg = Prefs.Tracking.Value ? "No deaths yet. Keep it that way." : "Death tracking is paused (Tracking = false in the cfg).";
                y += Theme.Para(cx, y, cw, msg, Theme.TextHi, st) + 8f * sc;
                y = Footer(cx, cw, y, sc);
                _lastH = y - top + pad;
                return;
            }

            var d = _list[n - 1 - Mathf.Clamp(_back, 0, n - 1)];
            if (!ReferenceEquals(d, _tipsFor))
            {
                _tipsFor = d;
                _tips = Advisor.Suggest(d, _list);
            }

            // ── Who ──────────────────────────────────────────
            var titleSt = Theme.Label(Mathf.RoundToInt(19 * sc), FontStyle.Bold, TextAnchor.MiddleLeft, serif: true);
            string title = d.Kind == DeathKind.Unknown && string.IsNullOrEmpty(d.Killer) ? "Killed by something unseen" : $"Killed by {d.KillerLine()}";
            y += Theme.Para(cx, y, cw, title, Theme.TextHi, titleSt) + 2f * sc;

            var how = new List<string>();
            if (!string.IsNullOrEmpty(d.KillerAbility)) how.Add(d.KillerAbility);
            if (d.KillingBlow > 0f)
                how.Add($"{d.KillingBlow:N0}{(string.IsNullOrEmpty(d.KillingElement) ? "" : " " + d.KillingElement.ToLowerInvariant())} damage"
                        + (d.MaxHealth > 0f ? $" ({d.KillingBlow / d.MaxHealth * 100f:0}% of your life)" : ""));
            if (d.KillingCrit == true) how.Add("critical strike");
            var bodySt = Theme.Label(Mathf.RoundToInt(11 * sc));
            if (how.Count > 0)
                y += Theme.Para(cx, y, cw, string.Join("  ·  ", how), Theme.Text, bodySt) + 2f * sc;
            if (!string.IsNullOrWhiteSpace(d.GameDeathInfo))
                y += Theme.Para(cx, y, cw, "Game says: " + d.GameDeathInfo.Trim(), Theme.TextMut, bodySt) + 2f * sc;

            // Meta line + kind chip
            var metaSt = Theme.Label(Mathf.RoundToInt(10 * sc));
            var meta = new List<string> { Ago(d.UtcTime) };
            if (!string.IsNullOrEmpty(d.Zone)) meta.Add(d.Zone);
            if (d.Level > 0) meta.Add(string.IsNullOrEmpty(d.CharacterClass) ? $"lvl {d.Level}" : $"lvl {d.Level} {d.CharacterClass}");
            string metaText = string.Join("  ·  ", meta);
            float mw = Theme.Width(metaText, metaSt);
            Theme.Write(new Rect(cx, y, mw + 2, 18f * sc), metaText, Theme.TextMut, metaSt);
            float kw = Chip(cx + mw + 10f * sc, y, d.KindLabel(), d.Kind == DeathKind.Unknown ? Theme.TextMut : Theme.Blood, sc);
            if (d.Hardcore) Chip(cx + mw + 16f * sc + kw, y, "HARDCORE", Theme.Blood, sc);
            y += 26f * sc;

            // ── Ailments ─────────────────────────────────────
            if (d.AilmentsOnYou != null && d.AilmentsOnYou.Count > 0)
            {
                y = Section(cx, cw, y, sc, "AILMENTS ON YOU");
                float ax = cx;
                foreach (var a in d.AilmentsOnYou)
                {
                    var info = Ailments.ByName(a);
                    Color c = info?.Element is Element el ? Theme.ElementColor(el) : Theme.Text;
                    float cwid = Chip(ax, y, a, c, sc);
                    ax += cwid + 6f * sc;
                    if (ax > cx + cw - 60f * sc) { ax = cx; y += 22f * sc; }
                }
                y += 26f * sc;
            }

            // ── Damage mix ───────────────────────────────────
            float total = d.DamageByElement?.Sum() ?? 0f;
            if (total > 0f)
            {
                y = Section(cx, cw, y, sc, $"DAMAGE TAKEN, LAST {d.WindowSeconds:0}s  ·  {d.WindowDamage:N0} in {d.Hits} hit{(d.Hits == 1 ? "" : "s")}");
                var barSt = Theme.Label(Mathf.RoundToInt(10 * sc));
                float labelW = 78f * sc, barH = 10f * sc;
                foreach (var (el, v) in Enumerable.Range(0, Elements.Count)
                             .Select(i => ((Element)i, d.DamageByElement[i]))
                             .Where(t => t.Item2 / total >= 0.01f)
                             .OrderByDescending(t => t.Item2))
                {
                    float share = v / total;
                    Theme.Write(new Rect(cx, y, labelW, 16f * sc), Elements.Name(el), Theme.Text, barSt);
                    var track = new Rect(cx + labelW, y + (16f * sc - barH) * 0.5f, cw - labelW - 44f * sc, barH);
                    Theme.Fill(track, Theme.Inset);
                    Theme.Fill(new Rect(track.x, track.y, track.width * share, barH), Theme.ElementColor(el));
                    Theme.Write(new Rect(track.xMax + 6f * sc, y, 40f * sc, 16f * sc), $"{share * 100f:0}%", Theme.TextMut, barSt);
                    y += 18f * sc;
                }
                y += 6f * sc;
            }

            // ── Threats ──────────────────────────────────────
            if (d.TopSources != null && d.TopSources.Count > 0)
            {
                y = Section(cx, cw, y, sc, "TOP THREATS");
                var tSt = Theme.Label(Mathf.RoundToInt(11 * sc));
                foreach (var s in d.TopSources)
                {
                    string line = $"{s.Name}   {s.Amount:N0} damage, {s.Hits} hit{(s.Hits == 1 ? "" : "s")}";
                    Theme.Write(new Rect(cx, y, cw, 17f * sc), line, Theme.Text, tSt);
                    y += 17f * sc;
                }
                y += 6f * sc;
            }

            // ── Advice ───────────────────────────────────────
            if (_tips.Count > 0)
            {
                y = Section(cx, cw, y, sc, "TO SURVIVE NEXT TIME");
                var tipTitle = Theme.Label(Mathf.RoundToInt(12 * sc), FontStyle.Bold);
                var tipBody  = Theme.Label(Mathf.RoundToInt(10 * sc));
                float numW = 18f * sc;
                for (int i = 0; i < _tips.Count; i++)
                {
                    var t = _tips[i];
                    Theme.Write(new Rect(cx, y, numW, Theme.LineHeight(tipTitle)), $"{i + 1}.", Theme.Accent, tipTitle);
                    y += Theme.Para(cx + numW, y, cw - numW, t.Title, Theme.TextHi, tipTitle);
                    y += Theme.Para(cx + numW, y, cw - numW, t.Body, Theme.TextMut, tipBody) + 7f * sc;
                }
            }

            y = Footer(cx, cw, y + 2f * sc, sc);
            _lastH = y - top + pad;
        }

        static void RefreshList()
        {
            var log = DeathTracker.Log;
            if (log == null) { _list = new List<DeathRecord>(); return; }
            string who = DeathTracker.Character;
            int count = log.All.Count;
            if (count == _listCount && who == _listChar) return;
            _listCount = count;
            _listChar = who;
            _list = log.For(who).ToList();
            _back = 0;   // a new death (or another character) jumps back to the newest
        }

        static float Section(float x, float w, float y, float sc, string title)
        {
            var st = Theme.Label(Mathf.RoundToInt(9 * sc), FontStyle.Bold);
            float tw = Theme.Width(title, st);
            Theme.Write(new Rect(x, y, tw + 2, 14f * sc), title, Theme.AccentDim, st);
            if (x + tw + 8f * sc < x + w)
                Theme.Fill(new Rect(x + tw + 8f * sc, y + 7f * sc, w - tw - 8f * sc, 1f), Theme.Border);
            return y + 18f * sc;
        }

        static float Chip(float x, float y, string text, Color c, float sc)
        {
            var st = Theme.Label(Mathf.RoundToInt(9 * sc), FontStyle.Bold, TextAnchor.MiddleCenter);
            float w = Theme.Width(text, st) + 14f * sc;
            var r = new Rect(x, y, w, 18f * sc);
            Theme.Box(r, Theme.Chip);
            Theme.Fill(new Rect(r.x, r.y, 2f * sc, r.height), c);
            Theme.Write(r, text, c, st);
            return w;
        }

        static float Footer(float x, float w, float y, float sc)
        {
            Theme.Fill(new Rect(x, y, w, 1f), Theme.Border);
            y += 6f * sc;
            var st = Theme.Label(Mathf.RoundToInt(9 * sc));
            string path = DeathTracker.Log != null ? $"Log: UserData/medick_DeathCounter/{DeathLog.TextFile}" : "";
            Theme.Write(new Rect(x, y, w - 130f * sc, 20f * sc), path, Theme.TextMut, st);
            if (DeathTracker.Log != null &&
                GUI.Button(new Rect(x + w - 124f * sc, y, 124f * sc, 20f * sc), "OPEN LOG FOLDER", Theme.Button(Mathf.RoundToInt(9 * sc))))
            {
                // Separate method: if Application.OpenURL was stripped from the
                // game, the JIT failure lands inside this try, not in Draw.
                try { OpenFolder(DeathTracker.Log.Directory); }
                catch { }
            }
            return y + 24f * sc;
        }

        static void OpenFolder(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            Application.OpenURL("file:///" + dir.Replace('\\', '/'));
        }

        // A disabled nav arrow is plain dim text, not a greyed-out button
        // (GUI.enabled is not in any shipped Terrible mod).
        static void NavButton(Rect r, string glyph, bool active, GUIStyle btn, GUIStyle textSt, Action onClick)
        {
            if (!active) { Theme.Write(r, glyph, Theme.Border, textSt); return; }
            if (GUI.Button(r, glyph, btn)) onClick();
        }

        static string Ago(DateTime utc)
        {
            var dt = DateTime.UtcNow - utc;
            if (dt.TotalSeconds < 60) return "just now";
            if (dt.TotalMinutes < 60) return $"{(int)dt.TotalMinutes} min ago";
            if (dt.TotalHours < 24)   return $"{(int)dt.TotalHours} h ago";
            return utc.ToLocalTime().ToString("MMM d, HH:mm");
        }
    }
}
