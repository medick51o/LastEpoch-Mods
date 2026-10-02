using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using medick_DeathCounter.Core;
using medick_DeathCounter.Game;
using UnityEngine;

namespace medick_DeathCounter.UI
{
    internal static class DeathPanel
    {
        public static bool Open { get; private set; }
        public static bool MouseOver { get; private set; }
        static int _tab, _selected = -1;
        static Vector2 _scroll;
        static float _contentHeight = 560f;
        static bool _confirmReset;
        static string _status;
        static List<DeathRecord> _records = new();
        static int _knownCount = -1;
        static string _knownCharacter;
        static readonly string[] Tabs = { "Last death", "History", "Patterns", "Options" };
        public static void Toggle()
        {
            if (!PlayerProbe.HasPlayer) { Close(); return; }
            Open = !Open;
            if (Open) { _tab = 0; _selected = -1; _scroll = new Vector2(0, 0); }
        }
        public static void Close() { Open = false; MouseOver = false; _confirmReset = false; }
        static GUIStyle Body(float sc) => Theme.Label(Mathf.RoundToInt(18f * sc));
        static GUIStyle Small(float sc) => Theme.Label(Mathf.RoundToInt(16f * sc));
        static GUIStyle Button(float sc) => Theme.Button(Mathf.RoundToInt(17f * sc));
        static float Paragraph(float x, float y, float w, string text, float sc, bool muted = false) =>
            y + Theme.Para(x, y, w, text, muted ? Theme.TextMut : Theme.Text, Body(sc)) + 12f * sc;
        static float Heading(float x, float y, float w, string text, float sc)
        {
            y += 10f * sc;
            Theme.Fill(new Rect(x, y, w, 1f), Theme.BorderHi);
            y += 14f * sc;
            return y + Theme.Para(x, y, w, text, Theme.Accent, Theme.Label(Mathf.RoundToInt(19 * sc), FontStyle.Bold)) + 12f * sc;
        }
        public static void Draw()
        {
            MouseOver = false;
            if (!Open) return;
            Refresh();
            float sc = Mathf.Clamp(Prefs.PanelScale.Value, 1f, 1.4f);
            float w = Mathf.Min(820f * sc, Screen.width - 40f);
            float h = Mathf.Min(700f * sc, Screen.height - 64f);
            float x = (Screen.width - w) * 0.5f, top = (Screen.height - h) * 0.5f;
            Rect panel = new(x, top, w, h);
            var ev = Event.current;
            if (ev != null) MouseOver = panel.Contains(ev.mousePosition);
            Theme.Box(panel, Theme.Panel);
            Theme.Fill(new Rect(x, top, w, 3f), Theme.Accent);
            float pad = 24f, cx = x + pad, cw = w - 2 * pad;
            float y = top + 19f;
            Theme.Write(new Rect(cx, y, cw - 245f, 36f), "Terrible Death Counter and Log", Theme.TextHi, Theme.Label(23, FontStyle.Bold));
            if (GUI.Button(new Rect(x + w - 228f, y, 144f, 36f), "Move counter", Theme.Button(16))) { CounterHud.BeginMove(); return; }
            if (GUI.Button(new Rect(x + w - 76f, y, 52f, 36f), "Close", Theme.Button(16))) { Close(); return; }
            y += 43f;
            Theme.Write(new Rect(cx, y, cw, 28f), $"{DeathTracker.Character}   |   Counter: {DeathTracker.CharacterDeaths}   |   {_records.Count} saved deaths", Theme.Text, Theme.Label(17));
            y += 39f;
            float tabW = (cw - 24f) / 4f;
            for (int i = 0; i < Tabs.Length; i++)
            {
                Rect tr = new(cx + i * (tabW + 8f), y, tabW, 40f);
                if (GUI.Button(tr, Tabs[i], Theme.Button(17))) { _tab = i; _scroll = new Vector2(0, 0); _confirmReset = false; _status = null; }
                if (_tab == i) Theme.Fill(new Rect(tr.x, tr.yMax - 3f, tr.width, 3f), Theme.Accent);
            }
            y += 55f;
            Rect viewport = new(cx, y, cw, top + h - 76f - y);
            float contentW = cw - 24f;
            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0, 0, contentW, Mathf.Max(viewport.height, _contentHeight)), GUIStyle.none, GUIStyle.none);
            try
            {
                float used = _tab switch
                {
                    1 => DrawHistory(0, contentW, 0, sc),
                    2 => DrawPatterns(0, contentW, 0, sc),
                    3 => DrawOptions(0, contentW, 0, sc),
                    _ => DrawDeath(0, contentW, 0, sc),
                };
                _contentHeight = used + 20f;
            }
            finally { GUI.EndScrollView(); }
            y = top + h - 60f;
            Theme.Fill(new Rect(cx, y, cw, 1), Theme.BorderHi);
            Theme.Write(new Rect(cx, y + 8f, cw - 185f, 36f), $"{Prefs.PanelKeyCode}: open / close   |   Shift + {Prefs.PanelKeyCode}: show / hide counter", Theme.TextMut, Theme.Label(16));
            if (GUI.Button(new Rect(x + w - 200f, y + 8f, 176f, 36f), "Open log folder", Theme.Button(16))) OpenFolder();
        }
        static void Refresh()
        {
            int count = DeathTracker.Log?.All.Count ?? 0;
            string character = DeathTracker.Character;
            if (count == _knownCount && character == _knownCharacter) return;
            _knownCount = count;
            _knownCharacter = character;
            _records = DeathTracker.Log?.For(character).ToList() ?? new();
            _selected = _records.Count - 1;
        }
        static float DrawDeath(float x, float w, float y, float sc)
        {
            if (_records.Count == 0)
            {
                y += Theme.Para(x, y, w, "Deaths: 0", Theme.TextHi, Theme.Label(Mathf.RoundToInt(30 * sc), FontStyle.Bold)) + 16f;
                return Paragraph(x, y, w, "Your deaths will appear here with the cause, damage and time when the game provides them.", sc);
            }
            _selected = Mathf.Clamp(_selected < 0 ? _records.Count - 1 : _selected, 0, _records.Count - 1);
            var d = _records[_selected];
            Theme.Write(new Rect(x, y, w - 210f, 40f), $"Death #{d.Number}   |   {d.UtcTime.ToLocalTime():MMM d, h:mm tt}", Theme.TextMut, Small(sc));
            if (_selected > 0 && GUI.Button(new Rect(x + w - 208f, y, 100f, 38f), "Previous", Button(sc))) _selected--;
            if (_selected < _records.Count - 1 && GUI.Button(new Rect(x + w - 100f, y, 100f, 38f), "Next", Button(sc))) _selected++;
            y += 54f;
            bool cause = !string.IsNullOrWhiteSpace(d.Killer) || !string.IsNullOrWhiteSpace(d.KillingAilment) || !string.IsNullOrWhiteSpace(d.KillerAbility);
            string title = cause ? (!string.IsNullOrWhiteSpace(d.Killer) || !string.IsNullOrWhiteSpace(d.KillingAilment) ? "Killed by " + d.KillerLine() : "Killed by " + d.KillerAbility) : "Cause not captured";
            if (!cause && !string.IsNullOrWhiteSpace(d.KillingElement)) title = "Killed by " + d.KillingElement.ToLowerInvariant() + " damage";
            y += Theme.Para(x, y, w, title, Theme.TextHi, Theme.Label(Mathf.RoundToInt(28 * sc), FontStyle.Bold)) + 14f;
            string location = (string.IsNullOrWhiteSpace(d.Zone) ? "" : d.Zone + "   |   ") + $"Level {d.Level}" + (d.Hardcore ? "   |   Hardcore" : "");
            y = Paragraph(x, y, w, location, sc, true);
            if (!string.IsNullOrWhiteSpace(d.KillerAbility)) y = Paragraph(x, y, w, "Ability: " + d.KillerAbility, sc);
            if (d.KillingBlow > 0)
            {
                string element = d.KillingElement;
                if (!string.IsNullOrWhiteSpace(d.SecondaryKillingElement)) element += " / " + d.SecondaryKillingElement;
                y = Paragraph(x, y, w, $"Killing blow: {d.KillingBlow:N0} {(element ?? "").ToLowerInvariant()} damage" + (d.KillingCrit == true ? "   |   Critical strike" : ""), sc);
            }
            if (d.OverkillDamage > 0) y = Paragraph(x, y, w, $"Overkill: {d.OverkillDamage:N0} damage", sc);
            if (!string.IsNullOrWhiteSpace(d.GameDeathInfo)) y = Paragraph(x, y, w, d.GameDeathInfo, sc);
            if (!string.IsNullOrWhiteSpace(d.DetailSource)) y = Paragraph(x, y, w, "Cause supplied by the game. The full damage timeline may be unavailable.", sc, true);
            if (d.Kind == DeathKind.Unknown && !cause && string.IsNullOrWhiteSpace(d.GameDeathInfo))
                y = Paragraph(x, y, w, "This death was counted, but its cause was not captured when it happened. Updating the mod cannot recover details that were never saved.", sc, true);
            if (d.AilmentsOnYou?.Count > 0)
            {
                y = Heading(x, y, w, "Ailments", sc);
                y = Paragraph(x, y, w, string.Join(", ", d.AilmentsOnYou), sc);
            }
            if (d.Hits > 0)
            {
                y = Heading(x, y, w, $"Damage before death ({d.WindowSeconds:0}s)", sc);
                y = Paragraph(x, y, w, $"{d.WindowDamage:N0} damage across {d.Hits} recorded hits. {d.KindLabel()}", sc);
                float total = d.DamageByElement?.Sum() ?? 0f;
                if (total > 0)
                    foreach (int i in Enumerable.Range(0, Elements.Count).Where(i => d.DamageByElement[i] > 0).OrderByDescending(i => d.DamageByElement[i]))
                        y = Paragraph(x, y, w, $"{Elements.Names[i]}: {d.DamageByElement[i]:N0} ({100 * d.DamageByElement[i] / total:0}%)", sc);
            }
            if (d.Defenses?.Count > 0)
            {
                y = Heading(x, y, w, "Your defenses", sc);
                y = Paragraph(x, y, w, d.DefenseLine(), sc);
            }
            var tips = Advisor.Suggest(d, _records).Where(t => t.Key != "unknown").ToList();
            if (tips.Count > 0)
            {
                y = Heading(x, y, w, "What may help next time", sc);
                foreach (var tip in tips)
                {
                    y += Theme.Para(x, y, w, tip.Title, Theme.TextHi, Theme.Label(Mathf.RoundToInt(20 * sc), FontStyle.Bold)) + 6f;
                    y = Paragraph(x, y, w, tip.Body, sc);
                }
            }
            return y;
        }
        static float DrawHistory(float x, float w, float y, float sc)
        {
            y = Paragraph(x, y, w, "Choose a death to review its details. Your history stays saved when you reset the counter.", sc, true);
            if (_records.Count == 0) return Paragraph(x, y, w, "No saved deaths.", sc);
            for (int i = _records.Count - 1; i >= 0; i--)
            {
                var d = _records[i];
                float rowH = 92f * sc;
                if (GUI.Button(new Rect(x, y, w, rowH), "", Button(sc))) { _selected = i; _tab = 0; _scroll = new Vector2(0, 0); }
                Theme.Write(new Rect(x + 14f, y + 10f, w - 28f, 32f * sc), $"#{d.Number}   {d.UtcTime.ToLocalTime():MMM d, h:mm tt}   |   {d.Zone}", Theme.TextMut, Small(sc));
                string text = !string.IsNullOrEmpty(d.Killer) || !string.IsNullOrEmpty(d.KillingAilment) ? d.KillerLine() : d.KillerAbility ?? d.GameDeathInfo ?? "Cause not captured";
                var lines = Theme.Wrap(text, Body(sc), w - 28f);
                Theme.Write(new Rect(x + 14f, y + 45f * sc, w - 28f, 34f * sc), lines[0] + (lines.Count > 1 ? "..." : ""), Theme.TextHi, Body(sc));
                y += rowH + 10f;
            }
            return y;
        }
        static float DrawPatterns(float x, float w, float y, float sc)
        {
            var report = DeathPatterns.Build(_records);
            y = Paragraph(x, y, w, $"Across {_records.Count} saved deaths for {DeathTracker.Character}.", sc, true);
            if (_records.Count == 0) return Paragraph(x, y, w, "Patterns will appear after deaths have been recorded.", sc);
            if (report.TopKillers.Count > 0)
            {
                y = Heading(x, y, w, "Frequent killers", sc);
                foreach (var (name, count) in report.TopKillers) y = Paragraph(x, y, w, $"{name}: {count} deaths", sc);
            }
            if (report.TopAilments.Count > 0)
            {
                y = Heading(x, y, w, "Frequent ailments", sc);
                foreach (var a in report.TopAilments) y = Paragraph(x, y, w, $"{a.Name}: {a.Count} deaths", sc);
            }
            var priorities = report.Priorities.Where(p => p.Advice.Key != "unknown").ToList();
            if (priorities.Count > 0)
            {
                y = Heading(x, y, w, "Build priorities", sc);
                foreach (var p in priorities)
                {
                    y += Theme.Para(x, y, w, $"{p.Advice.Title} ({p.Deaths} deaths)", Theme.TextHi, Theme.Label(Mathf.RoundToInt(20 * sc), FontStyle.Bold)) + 6f;
                    y = Paragraph(x, y, w, p.Advice.Body, sc);
                }
            }
            else y = Paragraph(x, y, w, "There is not enough recorded cause data to suggest a build change yet.", sc, true);
            return y;
        }
        static float DrawOptions(float x, float w, float y, float sc)
        {
            y = Heading(x, y, w, "Counter position and size", sc);
            y = Paragraph(x, y, w, "Hover over the number to reveal the MOVE handle, or use Move counter below. Drag to reposition it. Your position is saved automatically.", sc);
            if (GUI.Button(new Rect(x, y, 160f, 42f), "Move counter", Button(sc))) CounterHud.BeginMove();
            if (GUI.Button(new Rect(x + 174f, y, 175f, 42f), "Reset position", Button(sc))) { Prefs.HudX.Value = 0.5f; Prefs.HudY.Value = 0.015f; Prefs.Save(); }
            y += 57f;
            y = SizeControl(x, w, y, sc, "Counter size", Prefs.HudScale.Value, 0.9f, 2f, v => { Prefs.HudScale.Value = v; Prefs.Save(); });
            y = SizeControl(x, w, y, sc, "Log text size", Prefs.PanelScale.Value, 1f, 1.4f, v => { Prefs.PanelScale.Value = v; Prefs.Save(); });
            y = Heading(x, y, w, "Tracking and display", sc);
            if (GUI.Button(new Rect(x, y, w, 42f), Prefs.ShowCounter.Value ? "Counter: visible (click to hide)" : "Counter: hidden (click to show)", Button(sc))) { Prefs.ShowCounter.Value = !Prefs.ShowCounter.Value; Prefs.Save(); }
            y += 52f;
            if (GUI.Button(new Rect(x, y, w, 42f), Prefs.Tracking.Value ? "Recording: on (click to pause)" : "Recording: paused (click to resume)", Button(sc))) { Prefs.Tracking.Value = !Prefs.Tracking.Value; Prefs.Save(); }
            y += 52f;
            if (GUI.Button(new Rect(x, y, w, 42f), Prefs.ShowDeathToast.Value ? "Death notification: on" : "Death notification: off", Button(sc))) { Prefs.ShowDeathToast.Value = !Prefs.ShowDeathToast.Value; Prefs.Save(); }
            y += 54f;
            y = Heading(x, y, w, "Death history", sc);
            y = Paragraph(x, y, w, "Reset sets this character's displayed counter and session count to zero. Saved deaths and the game's death count stay intact.", sc);
            if (!_confirmReset)
            {
                if (GUI.Button(new Rect(x, y, 210f, 42f), "Reset death counter", Button(sc))) _confirmReset = true;
                y += 56f;
            }
            else
            {
                y = Paragraph(x, y, w, "Resetting the counter does not make you a bad person. But Medick, the author, will judge you.", sc);
                if (GUI.Button(new Rect(x, y, 170f, 42f), "Reset anyway", Button(sc))) { _status = DeathTracker.ResetCounter() ? "Counter reset. All saved deaths are still in History." : "Reset could not be saved. The counter has been kept."; _confirmReset = false; }
                if (GUI.Button(new Rect(x + 184f, y, 180f, 42f), "Face my deaths", Button(sc))) _confirmReset = false;
                y += 56f;
            }
            if (GUI.Button(new Rect(x, y, 230f, 42f), "Export character log", Button(sc)))
            {
                try
                {
                    string safe = string.Concat(DeathTracker.Character.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                    string file = Path.Combine(DeathTracker.Log.Directory, safe + "-deaths-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
                    File.WriteAllLines(file, _records.Select(d => d.ToLogLine()));
                    _status = "Export saved. Open log folder to find it.";
                }
                catch (Exception) { _status = "Export could not be saved. Your existing history is unchanged."; }
            }
            y += 56f;
            if (_status != null) y = Paragraph(x, y, w, _status, sc);
            return y;
        }
        static float SizeControl(float x, float w, float y, float sc, string caption, float value, float min, float max, Action<float> change)
        {
            Theme.Write(new Rect(x, y, w - 152f, 42f), $"{caption}: {value:0.0}x", Theme.Text, Body(sc));
            if (GUI.Button(new Rect(x + w - 144f, y, 64f, 42f), "Smaller", Theme.Button(14))) change(Mathf.Clamp(value - 0.1f, min, max));
            if (GUI.Button(new Rect(x + w - 72f, y, 72f, 42f), "Larger", Theme.Button(14))) change(Mathf.Clamp(value + 0.1f, min, max));
            return y + 54f;
        }
        static void OpenFolder()
        {
            if (DeathTracker.Log == null) return;
            try { OpenFolderInterop(DeathTracker.Log.Directory); }
            catch (Exception ex) { Dbg.Log("open log folder: " + ex.Message); }
        }
        static void OpenFolderInterop(string directory)
        {
            Directory.CreateDirectory(directory);
            Application.OpenURL("file:///" + directory.Replace('\\', '/'));
        }
    }
}
