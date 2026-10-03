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
        static readonly JournalSelection Selection = new();
        sealed class Disclosure { public bool Details, Why, Checks, Raw; }
        static readonly Dictionary<string, Disclosure> Disclosures = new();
        static readonly Dictionary<string, Vector2> ScrollPositions = new();
        static readonly Dictionary<string, float> ContentHeights = new();
        static string _scrollContext;
        static int _knownCount = -1;
        static string _knownScope;
        static readonly Dictionary<DeathRecord, (int Signature, List<Advice> Tips)> AdviceCache = new();
        static readonly ViewMemo _stepMemo = new();
        static List<(string Text, GUIStyle Style, Color Color)> _stepLines;
        static float _stepMeasure;
        static bool _stepGap;
        static int _historyPage;
        static int _patternSignature;
        static PatternReport _patternReport;
        internal static int Signature(DeathRecord d) => HashCode.Combine(d.Killer, d.KillerAbility, d.KillingElement, d.SecondaryKillingElement,
            d.KillingAilment, d.Kind, d.KillingBlow, HashCode.Combine(d.OverkillDamage, d.KillingCrit, d.Hits, d.WindowDamage, d.DetailSource));
        static List<Advice> Tips(DeathRecord d)
        {
            int signature = HashCode.Combine(Signature(d), _records.Count);
            if (AdviceCache.TryGetValue(d, out var cached) && cached.Signature == signature) return cached.Tips;
            var tips = Advisor.Show(d, _records);
            BoundedCache.Put(AdviceCache, d, (signature, tips), BoundedCache.AdviceCap);
            return tips;
        }
        static bool _checking, _showAbout;
        static string ViewCharacter => Selection.Character ?? DeathTracker.Character;
        static Disclosure For(DeathRecord death)
        {
            string key = ReassessmentLog.KeyFor(death);
            if (!Disclosures.TryGetValue(key, out var state)) Disclosures[key] = state = new();
            return state;
        }
        public static bool DismissConfirmation()
        {
            if (_confirmReset) { _confirmReset = false; return true; }
            if (_confirmAssessmentDelete) { _confirmAssessmentDelete = false; return true; }
            return false;
        }
        static Vector2 _scroll;
        static bool _dragScroll, _showRawReport;
        static float _scrollGrab;
        static float _contentHeight = 560f;
        static bool _confirmReset;
        static string _status;
        static List<DeathRecord> _records = new();
        static CurrentAssessment _assessment;
        static string _assessmentError;
        static DateTime _assessmentTime;
        static string _savedUpdateId;
        static bool _confirmAssessmentDelete, _deleteAllAssessments;
        static int _updatePage;
        static int _bossIndex;
        static int _bossPage;
        static string _bossGroup = "all", _shownBossId;
        static bool _showBossMoves, _showContributions, _showBossBrowser, _showBossSources;
        static int _bossMovePage;
        static readonly HeightCache _bossMoveHeights = new();
        static EncounterRecord _bossResearch;
        static IReadOnlyList<string> _bossPreparation, _bossSources;
        static readonly string[] Tabs = { "Last death", "History", "Patterns", "Boss notes", "Settings" };
        public static void Toggle()
        {
            if (!PlayerProbe.HasPlayer) { Close(); return; }
            if (Open) { Close(); return; }
            Open = true; _dragScroll = false;
            if (Selection.Character != DeathTracker.Character) ClearAssessment();
            _records = DeathTracker.Log?.For(DeathTracker.Character).ToList() ?? new();
            Selection.Begin(DeathTracker.Character, _records, _tab == 0);
            SyncSelection();
            InputBlocker.TakeFocus();
        }
        public static void Close()
        {
            Open = false; MouseOver = false; _dragScroll = false;
            _confirmReset = false; _confirmAssessmentDelete = false;
        }
        static void SyncSelection()
        {
            _selected = _records.FindIndex(d => ReassessmentLog.KeyFor(d) == Selection.SelectedKey);
        }
        static void SelectDeath(DeathRecord death)
        {
            Selection.Select(death); SyncSelection(); ClearAssessment();
        }
        static void SelectNewest()
        {
            Selection.SelectNewest(_records); SyncSelection(); ClearAssessment();
        }
        static void ClearAssessment()
        {
            _assessment = null; _assessmentError = null; _savedUpdateId = null;
            _updatePage = 0; _confirmAssessmentDelete = false;
        }
        static ReassessmentUpdate SelectedUpdate(DeathRecord death) => DeathTracker.Reassessments?.For(death).FirstOrDefault(u => u.Id == _savedUpdateId);
        static string CheckUnavailable(DeathRecord death)
        {
            if (!PlayerProbe.HasPlayer || PlayerProbe.CharacterName()?.Trim() != death.Character) return $"Load {death.Character} to check these changes.";
            if (PlayerProbe.CurrentHealth <= 0) return "Available after respawn on this character.";
            if (!float.IsFinite(PlayerProbe.CurrentHealth)) return "Current defenses are not available yet. Try again after entering an area.";
            return null;
        }
        static float AssessmentButton(DeathRecord d, float x, float y, float w, float sc)
        {
            string unavailable = CheckUnavailable(d);
            if (ActionButton(x, ref y, w, "Assess my current gear", sc, !_checking && unavailable == null)) RunCheck(d);
            y = Paragraph(x, y, w, unavailable ?? "Come back after upgrading your gear, then press Assess my current gear to run the numbers and see how your defenses stack up against this death. Each assessment saves separately.", sc, true);
            if (_assessmentError != null) y = Paragraph(x, y, w, _assessmentError, sc);
            return y;
        }
        static void RunCheck(DeathRecord death, bool fromSelected = false)
        {
            if (_checking || CheckUnavailable(death) != null) return;
            _checking = true;
            var previous = fromSelected ? SelectedUpdate(death) : null;
            _assessmentError = null;
            try
            {
                if (!PlayerProbe.TryCurrentDefenses(death.Character, out var stats, out var reason))
                { _assessmentError = reason; return; }
                _assessment = CurrentAssessment.Build(death, PlayerProbe.CharacterName(), stats, _records);
                _assessmentTime = DateTime.Now;
                if (!_assessment.Available) { _assessmentError = _assessment.Message; return; }
                if (DeathTracker.Reassessments?.Save(death, PlayerProbe.CharacterName(), stats, _assessment, previous, out var saved) == true)
                {
                    _savedUpdateId = saved.Id; _assessment = null;
                    For(death).Checks = false; _confirmAssessmentDelete = false;
                }
                else _assessmentError = "This check could not be saved. Your existing checks are unchanged. Assess my current gear retries with a fresh reading.";
            }
            catch (Exception ex) { _assessmentError = "This check could not be saved. Your existing checks are unchanged."; Dbg.Log("gear check: " + ex.Message); }
            finally { _checking = false; }
        }
        static float UpdatePicker(DeathRecord death, float x, float y, float w, float sc)
        {
            var updates = DeathTracker.Reassessments?.For(death).Reverse().ToList() ?? new();
            if (updates.Count == 0) return Paragraph(x, y, w, "No saved checks yet.", sc, true);
            _updatePage = Math.Clamp(_updatePage, 0, (updates.Count - 1) / 5);
            foreach (var update in updates.Skip(_updatePage * 5).Take(5))
            {
                string label = $"{update.UtcTime.ToLocalTime():MMM d, yyyy · h:mm:ss tt} · Grade {update.Assessment.Rating.Grade} · {update.Assessment.Rating.Verdict}";
                if (ActionButton(x, ref y, w, label + (update.Id == _savedUpdateId ? " · Selected" : ""), sc))
                {
                    _savedUpdateId = update.Id == _savedUpdateId ? null : update.Id;
                    _assessment = null; _assessmentError = null; _confirmAssessmentDelete = false;
                }
            }
            if (updates.Count > 5)
            {
                if (ActionButton(x, ref y, w, $"Newer checks · page {_updatePage + 1} of {(updates.Count + 4) / 5}", sc, _updatePage > 0)) _updatePage--;
                if (ActionButton(x, ref y, w, "Older checks", sc, (_updatePage + 1) * 5 < updates.Count)) _updatePage++;
            }
            if (ActionButton(x, ref y, w, $"Delete all {updates.Count} saved checks for this death", sc)) { _confirmAssessmentDelete = true; _deleteAllAssessments = true; }
            if (_confirmAssessmentDelete)
            {
                y = Heading(x, y, w, _deleteAllAssessments ? $"Delete all saved checks for death #{death.Number}?" : "Delete this saved check?", sc);
                y = Paragraph(x, y, w, _deleteAllAssessments
                    ? $"{updates.Count} checks · {death.Character} · Death {death.UtcTime.ToLocalTime():MMM d, yyyy h:mm tt}. The original death stays saved."
                    : "The original death stays saved. Other checks, including later checks based on this one, stay intact.", sc);
                if (ActionButton(x, ref y, w, _deleteAllAssessments ? "Keep checks" : "Keep check", sc)) _confirmAssessmentDelete = false;
                if (ActionButton(x, ref y, w, _deleteAllAssessments ? $"Delete {updates.Count} checks" : "Delete check", sc))
                {
                    bool removed = _deleteAllAssessments ? DeathTracker.Reassessments?.DeleteFor(death) == true : DeathTracker.Reassessments?.Delete(_savedUpdateId) == true;
                    if (removed) { ClearAssessment(); _assessmentError = "Saved check deletion completed. The original death is unchanged."; }
                    else { _assessmentError = "The change could not be saved. Everything has been kept."; _confirmAssessmentDelete = false; }
                }
            }
            return y;
        }
        static float DrawAssessmentSummary(DeathRecord death, float x, float y, float w, float sc)
        {
            var saved = SelectedUpdate(death);
            var snapshot = saved?.Assessment;
            if (snapshot?.Rating == null) return y;
            var rating = snapshot.Rating;
            y = Heading(x, y, w, $"Current gear: {rating.Verdict}", sc);
            y = Paragraph(x, y, w, rating.Reason, sc);
            foreach (var tip in snapshot.Actions.Take(Advisor.MaxShown))
            {
                y += Theme.Para(x, y, w, tip.Title, Theme.TextHi, Theme.Label(Mathf.RoundToInt(20 * sc), FontStyle.Bold)) + 6f;
                y = Paragraph(x, y, w, tip.Body, sc);
            }
            y = Paragraph(x, y, w, $"Saved {saved.UtcTime.ToLocalTime():MMM d, h:mm tt} · Grade {rating.Grade} measures defense progress, not fight odds.", sc, true);
            return y;
        }
        static float DrawAssessment(DeathRecord death, float x, float y, float w, float sc)
        {
            var saved = SelectedUpdate(death);
            var snapshot = saved?.Assessment;
            if (snapshot == null) return y;
            y = Heading(x, y, w, "Saved gear check", sc);
            y = Paragraph(x, y, w, $"Stats captured at {saved.UtcTime.ToLocalTime():MMM d, h:mm:ss tt}. Reassess after changing gear or buffs.", sc, true);
            var currentStats = saved?.CurrentStats ?? _assessment?.CurrentStats;
            if (currentStats != null)
            {
                y = Heading(x, y, w, "At death / This check / Resistance cap", sc);
                var capture = saved?.OriginalCapture ?? death;
                y = DrawResistances(capture, currentStats, capture.Defenses, x, y, w, sc);
            }
            if (saved?.PreviousStats != null)
                y = DrawCheckComparison(saved, x, y, w, sc);
            foreach (string change in snapshot.Changes)
                if (currentStats == null || !Elements.Names.Any(n => change.StartsWith(n + " resistance:", StringComparison.Ordinal)))
                    y = Paragraph(x, y, w, change, sc);
            if (saved != null)
            {
                y = Paragraph(x, y, w, "Saved calculation: " + saved.Algorithm + ". This grade describes defense progress, not a chance to survive the fight.", sc, true);
                if (!string.IsNullOrWhiteSpace(saved.PreviousUpdateId))
                {
                    var parent = DeathTracker.Reassessments.For(death).FirstOrDefault(u => u.Id == saved.PreviousUpdateId);
                    y = Paragraph(x, y, w, parent == null ? "Previous check was deleted. Its saved comparison remains intact."
                        : $"Previous check · {parent.UtcTime.ToLocalTime():MMM d, yyyy h:mm:ss tt}", sc, true);
                }
                if (ActionButton(x, ref y, w, "Check again", sc, CheckUnavailable(death) == null)) RunCheck(death, true);
                if (CheckUnavailable(death) is string reason) y = Paragraph(x, y, w, reason, sc, true);
                if (ActionButton(x, ref y, w, "Delete this check", sc)) { _confirmAssessmentDelete = true; _deleteAllAssessments = false; }
            }
            if (saved?.SincePrevious?.Count > 0)
            {
                y = Heading(x, y, w, "Changes since the update you reassessed", sc);
                foreach (string change in saved.SincePrevious) y = Paragraph(x, y, w, change, sc);
            }
            return y;
        }
        static float DrawCheckComparison(ReassessmentUpdate saved, float x, float y, float w, float sc)
        {
            y = Heading(x, y, w, "At death / Previous check / This check", sc);
            y = Paragraph(x, y, w, $"Previous check · {saved.PreviousUtcTime?.ToLocalTime():MMM d, yyyy h:mm:ss tt}. Frozen comparison; resistance cap is 75%.", sc, true);
            foreach (string key in saved.CurrentStats.Keys.Union(saved.PreviousStats.Keys).Where(k => k != "AreaLevel").OrderBy(k => k))
            {
                bool percent = key.StartsWith("Res.") || key.StartsWith("ResUncapped.") || key == "Block" || key == "Endurance" || key == "CritAvoidance" || key == "ReducedBonusCritDamage";
                string Value(IReadOnlyDictionary<string, float> values) => values != null && values.TryGetValue(key, out float v) && float.IsFinite(v) ? v.ToString("0.#") + (percent ? "%" : "") : "Not recorded";
                y = Paragraph(x, y, w, key.Replace("ResUncapped.", "Total ").Replace("Res.", "") + ": "
                    + Value(saved.OriginalCapture.Defenses) + " / " + Value(saved.PreviousStats) + " / " + Value(saved.CurrentStats), sc);
            }
            return y;
        }
        static GUIStyle Body(float sc) => Theme.Label(Mathf.RoundToInt(18f * sc));
        static GUIStyle Small(float sc) => Theme.Label(Mathf.RoundToInt(16f * sc));
        static GUIStyle Button(float sc) => Theme.Button(Mathf.RoundToInt(17f * sc));
        static float Paragraph(float x, float y, float w, string text, float sc, bool muted = false) =>
            y + Theme.Para(x, y, w, text, muted ? Theme.TextMut : Theme.Text, Body(sc)) + 12f * sc;

        static float DrawResistances(DeathRecord death, IReadOnlyDictionary<string, float> stats,
            IReadOnlyDictionary<string, float> previous, float x, float y, float w, float sc)
        {
            var readings = ResistanceReview.Build(stats, death, previous);
            int cols = Math.Clamp((int)((w + 12f * sc) / (177f * sc)), 1, 4);
            float gap = 10f * sc, cardW = (w - gap * (cols - 1)) / cols;
            float totalH = readings.Max(row => Theme.Wrap(row.Total.HasValue && row.Total > row.Effective + 0.01f ? $"{row.Total:0.#}% total"
                : row.Capped ? "75% cap reached" : row.Gap.HasValue ? $"{row.Gap:0.#} points to cap" : "Not recorded", Small(sc), cardW - 24f * sc).Count * (Theme.LineHeight(Small(sc)) + 4f));
            float cardH = 98f * sc + totalH + (previous != null ? 32f * sc : 0f);
            y = Paragraph(x, y, w, "Effective resistance · 75% cap · Capped resistances first", sc, true);
            for (int i = 0; i < readings.Count; i++)
            {
                var row = readings[i];
                float cx = x + i % cols * (cardW + gap), cy = y + i / cols * (cardH + gap);
                var rect = new Rect(cx, cy, cardW, cardH);
                Theme.Box(rect, Theme.Card);
                Color color = Theme.ResistanceColor(row.Element);
                Theme.Fill(new Rect(cx, cy, 3f * sc, cardH), color);
                if (row.Relevant && row.Gap > 0) Theme.DrawBorder(rect, color, sc);
                Theme.Write(new Rect(cx + 12f * sc, cy + 8f * sc, cardW - 24f * sc, 24f * sc),
                    Elements.Name(row.Element).ToUpperInvariant(), color, Small(sc));
                Theme.Write(new Rect(cx + 12f * sc, cy + 33f * sc, cardW - 24f * sc, 34f * sc),
                    row.Effective.HasValue ? $"{row.Effective:0.#}%" : "—", Theme.TextHi,
                    Theme.Label(Mathf.RoundToInt(28f * sc), FontStyle.Bold));
                string total = row.Total.HasValue && row.Total > row.Effective + 0.01f ? $"{row.Total:0.#}% total"
                    : row.Capped ? "75% cap reached" : row.Gap.HasValue ? $"{row.Gap:0.#} points to cap" : "Not recorded";
                Theme.Para(cx + 12f * sc, cy + 69f * sc, cardW - 24f * sc, total, Theme.TextMut, Small(sc));
                string status = row.Capped ? "Cap reached" : row.KillingType ? "Killing damage" : row.Relevant ? "Recorded damage" : row.Effective.HasValue ? "Below cap" : "Not recorded";
                bool warning = row.Gap > 0 && row.Relevant;
                if (warning) Theme.DownArrow(cx + 12f * sc, cy + 72f * sc + totalH, sc, Theme.Blood);
                Theme.Write(new Rect(cx + (warning ? 32f : 12f) * sc, cy + 70f * sc + totalH,
                    cardW - (warning ? 40f : 24f) * sc, 24f * sc), status, warning ? Theme.Blood : Theme.TextMut, Small(sc));
                if (previous != null)
                    Theme.Write(new Rect(cx + 12f * sc, cy + 96f * sc + totalH, cardW - 24f * sc, 22f * sc),
                        row.Previous.HasValue ? $"At death: {row.Previous:0.#}%" : "At death: not recorded", Theme.TextMut, Small(sc));
            }
            y += (readings.Count + cols - 1) / cols * (cardH + gap);
            if (previous == null && !readings.Any(r => r.Relevant))
                y = Paragraph(x, y, w, "Damage type was not recorded. These gaps cannot yet be linked to this death.", sc, true);
            return y;
        }

        static float DrawDefenseStats(IReadOnlyDictionary<string, float> stats, float x, float y, float w, float sc)
        {
            var fields = new (string Key, string Label, bool Percent)[]
            {
                ("MaxHealth", "Max health", false), ("Ward", "Ward at snapshot", false),
                ("Armor", "Armor", false), ("Dodge", "Dodge rating", false),
                ("Endurance", "Endurance", true), ("EnduranceThreshold", "Endurance threshold", false),
                ("CritAvoidance", "Crit avoidance", true), ("ReducedBonusCritDamage", "Reduced crit bonus", true),
                ("Block", "Block chance", true), ("BlockEffectiveness", "Block effectiveness", false),
            }.Where(f => stats.TryGetValue(f.Key, out float value) && float.IsFinite(value)).ToArray();
            int cols = Math.Clamp((int)((w + 12f * sc) / (230f * sc)), 1, 3);
            float gap = 10f * sc, cellW = (w - gap * (cols - 1)) / cols, cellH = 66f * sc;
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i]; float cx = x + i % cols * (cellW + gap), cy = y + i / cols * (cellH + gap);
                Theme.Box(new Rect(cx, cy, cellW, cellH), Theme.Chip);
                Theme.Write(new Rect(cx + 10f * sc, cy + 6f * sc, cellW - 20f * sc, 24f * sc), f.Label, Theme.TextMut, Small(sc));
                Theme.Write(new Rect(cx + 10f * sc, cy + 32f * sc, cellW - 20f * sc, 28f * sc),
                    stats[f.Key].ToString(f.Percent ? "0.#" : "N0") + (f.Percent ? "%" : ""), Theme.TextHi, Body(sc));
            }
            return y + (fields.Length + cols - 1) / cols * (cellH + gap);
        }
        static float Heading(float x, float y, float w, string text, float sc)
        {
            y += 16f * sc;
            Theme.Fill(new Rect(x, y, w, 1f), Theme.BorderHi);
            y += 14f * sc;
            return y + Theme.Para(x, y, w, text, Theme.Accent, Theme.Label(Mathf.RoundToInt(20 * sc), FontStyle.Bold)) + 12f * sc;
        }
        public static void Draw()
        {
            MouseOver = false;
            if (!Open) return;
            Refresh();
            float sc = Mathf.Clamp(Prefs.PanelScale.Value, 1f, 1.4f);
            float w = Mathf.Min(920f, Screen.width - 48f), h = Mathf.Min(740f, Screen.height - 48f);
            if (w < 200f || h < 250f) return;
            float x = (Screen.width - w) * 0.5f, top = (Screen.height - h) * 0.5f;
            Rect panel = new(x, top, w, h);
            var ev = Event.current;
            if (ev != null) MouseOver = panel.Contains(ev.mousePosition);
            Theme.Box(panel, Theme.Panel);
            Theme.Fill(new Rect(x, top, w, 2f), Theme.Accent);
            float pad = 24f, cx = x + pad, cw = w - 2 * pad, y = top + 18f;
            Theme.Write(new Rect(cx, y, cw - 100f, 24f * sc), "TERRIBLE", Theme.Accent, Small(sc));
            if (GUI.Button(new Rect(x + w - 100f, y, 76f, 40f * sc), "Close", Button(sc))) { Close(); return; }
            y += 25f * sc;
            float headingW = Theme.Width("Death log", Theme.Label(Mathf.RoundToInt(26f * sc), FontStyle.Bold));
            Theme.Write(new Rect(cx, y, headingW + 4f, 36f * sc), "Death log", Theme.TextHi, Theme.Label(Mathf.RoundToInt(26f * sc), FontStyle.Bold));
            float characterX = cx + headingW + 20f;
            float characterW = cw - headingW - 120f;
            if (characterW > 100f) Theme.Para(characterX, y + 5f, characterW, ViewCharacter, Theme.TextMut, Small(sc));
            else y += Theme.Para(cx, y + 40f * sc, cw, ViewCharacter, Theme.TextMut, Small(sc));
            y += 50f * sc;
            float tx = cx, navBottom = y + 42f * sc;
            for (int i = 0; i < Tabs.Length; i++)
            {
                float tw = Theme.Width(Tabs[i], Button(sc)) + 28f * sc;
                if (tx > cx && tx + tw > cx + cw) { tx = cx; y += 48f * sc; navBottom = y + 42f * sc; }
                Rect tr = new(tx, y, Mathf.Min(tw, cw), 42f * sc);
                if (GUI.Button(tr, Tabs[i], Button(sc)))
                {
                    _tab = i; _confirmReset = false; _confirmAssessmentDelete = false;
                    if (i == 0) SelectNewest();
                }
                if (_tab == i) Theme.Fill(new Rect(tr.x, tr.yMax - 2f, tr.width, 2f), Theme.Accent);
                tx += tw + 8f;
            }
            y = navBottom + 20f;
            float footerH = Theme.Wrap($"{Prefs.PanelKeyCode}: open / close log    Shift + {Prefs.PanelKeyCode}: show / hide counter", Small(sc), cw).Count
                * (Theme.LineHeight(Small(sc)) + 4f) + 20f;
            Rect viewport = new(cx, y, cw, Mathf.Max(40f, top + h - footerH - 12f - y));
            string context = ViewCharacter + ":" + _tab + (_tab == 0 ? ":" + Selection.SelectedKey : "");
            if (_scrollContext != context)
            {
                if (_scrollContext != null) { ScrollPositions[_scrollContext] = _scroll; ContentHeights[_scrollContext] = _contentHeight; }
                _scrollContext = context;
                _scroll = ScrollPositions.TryGetValue(context, out var previousScroll) ? previousScroll : new Vector2(0, 0);
                _contentHeight = ContentHeights.TryGetValue(context, out float previousHeight) ? previousHeight : viewport.height;
            }
            float contentW = cw - 24f;
            _scroll = GUI.BeginScrollView(viewport, _scroll, new Rect(0, 0, contentW, Mathf.Max(viewport.height, _contentHeight)), GUIStyle.none, GUIStyle.none);
            try
            {
                float start = 0f;
                if (!InputBlocker.GameplayReady || !InputBlocker.NativeUiReady) start = Paragraph(0, start, contentW, InputBlocker.Status, sc);
                string warning = DeathTracker.Log?.PersistenceWarning;
                if (warning != null)
                {
                    start = Paragraph(0, start, contentW, warning, sc);
                    if (DeathTracker.Log.UnsavedCount > 0) start = Paragraph(0, start, contentW, "This death is in memory but has not saved to disk. Use Settings > History & files to export it before closing the game.", sc);
                    if (ActionButton(0, ref start, contentW, "Retry history save", sc)) DeathTracker.Log.SaveUpdated();
                }
                if (Selection.NewDeathAvailable)
                {
                    if (ActionButton(0, ref start, contentW, "New death recorded · View", sc)) { SelectNewest(); _tab = 0; }
                }
                _contentHeight = (_tab switch
                {
                    1 => DrawHistory(0, contentW, start, sc),
                    2 => DrawPatterns(0, contentW, start, sc),
                    3 => DrawBossTips(0, contentW, start, sc),
                    4 => DrawOptions(0, contentW, start, sc),
                    _ => DrawDeath(0, contentW, start, sc),
                }) + 20f;
            }
            finally { GUI.EndScrollView(); }
            _scroll.y = Mathf.Clamp(_scroll.y, 0f, Mathf.Max(0f, _contentHeight - viewport.height));
            DrawScrollBar(viewport);
            float fy = top + h - footerH;
            Theme.Fill(new Rect(cx, fy, cw, 1f), Theme.Border);
            Theme.Para(cx, fy + 10f, cw, $"{Prefs.PanelKeyCode}: open / close log    Shift + {Prefs.PanelKeyCode}: show / hide counter", Theme.TextMut, Small(sc));
            // IMGUI consumption is additional protection; native UI dispatch
            // is independently gated by InputBlocker before it can click Respawn.
            if (ev != null && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag || ev.type == EventType.ScrollWheel)) ev.Use();
        }
        static bool ActionButton(float x, ref float y, float w, string label, float sc, bool enabled = true)
        {
            var labelStyle = Theme.Label(Mathf.RoundToInt(17f * sc));
            float bh = Theme.Wrap(label, labelStyle, Mathf.Max(40f, w - 24f * sc)).Count * (Theme.LineHeight(labelStyle) + 4f) + 18f * sc;
            bh = Mathf.Max(42f * sc, bh);
            Rect r = new(x, y, w, bh);
            bool clicked = GUI.Button(r, "", Button(sc));
            bool primary = label == "Assess my current gear" && enabled;
            if (primary) Theme.Fill(r, Theme.Accent);
            Theme.Para(x + 12f * sc, y + 9f * sc, w - 24f * sc, label, primary ? Theme.Bg : enabled ? Theme.TextHi : Theme.TextMut, labelStyle);
            y += bh + 12f * sc;
            return enabled && clicked;
        }
        static bool DisclosureButton(float x, ref float y, float w, string label, bool open, float sc)
        {
            Theme.Fill(new Rect(x, y, w, 1f), Theme.Border);
            y += 8f;
            return ActionButton(x, ref y, w, label + (open ? "  −" : "  +"), sc);
        }
        static void DrawScrollBar(Rect viewport)
        {
            float maximum = Mathf.Max(0f, _contentHeight - viewport.height);
            if (maximum <= 0f || viewport.height <= 0f) { _dragScroll = false; return; }
            float thumbHeight = Mathf.Min(viewport.height, Mathf.Max(32f, viewport.height * viewport.height / _contentHeight));
            float travel = viewport.height - thumbHeight;
            Rect track = new(viewport.xMax - 16f, viewport.y, 16f, viewport.height);
            Rect thumb = new(track.x, track.y + Mathf.Clamp(_scroll.y, 0f, maximum) / maximum * travel, track.width, thumbHeight);
            Theme.Fill(track, Theme.SurfaceHi);
            Theme.Fill(thumb, _dragScroll ? Theme.TextHi : Theme.Accent);
            var ev = Event.current;
            if (ev == null) return;
            if (ev.type == EventType.MouseDown && ev.button == 0 && track.Contains(ev.mousePosition))
            {
                _dragScroll = true;
                _scrollGrab = thumb.Contains(ev.mousePosition) ? ev.mousePosition.y - thumb.y : thumbHeight * 0.5f;
                _scroll.y = travel > 0f ? Mathf.Clamp((ev.mousePosition.y - track.y - _scrollGrab) / travel, 0f, 1f) * maximum : 0f;
                ev.Use();
            }
            else if (ev.type == EventType.MouseDrag && _dragScroll)
            {
                _scroll.y = travel > 0f ? Mathf.Clamp((ev.mousePosition.y - track.y - _scrollGrab) / travel, 0f, 1f) * maximum : 0f;
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp && _dragScroll) { _dragScroll = false; ev.Use(); }
            if (_dragScroll) MouseOver = true;
        }
        static void Refresh()
        {
            int count = DeathTracker.Log?.All.Count ?? 0;
            if (_knownScope != ViewCharacter) _historyPage = 0;
            if (_knownCount == count && _knownScope == ViewCharacter) return;
            _knownCount = count; _knownScope = ViewCharacter;
            _records = DeathTracker.Log?.For(ViewCharacter).ToList() ?? new();
            Selection.Refresh(_records); SyncSelection();
        }
        static float DrawDeath(float x, float w, float y, float sc)
        {
            if (_records.Count == 0)
            {
                y = Heading(x, y, w, "No deaths recorded yet", sc);
                return Paragraph(x, y, w, "Deaths will appear here automatically while you play.", sc);
            }
            if (_selected < 0 || _selected >= _records.Count) return Paragraph(x, y, w, "Choose a death from History.", sc);
            var d = _records[_selected];
            var state = For(d);
            Theme.Para(x, y, w - 150f * sc, $"Death #{d.Number} · {d.UtcTime.ToLocalTime():MMM d · h:mm tt}", Theme.TextMut, Small(sc));
            if (GUI.Button(new Rect(x + w - 140f * sc, y, 140f * sc, 38f * sc), "All deaths", Button(sc))) _tab = 1;
            y += 54f * sc;
            y = AssessmentButton(d, x, y, w, sc);
            y = DrawAssessmentSummary(d, x, y, w, sc);
            var tips = Tips(d);
            bool showingAssessment = SelectedUpdate(d) != null;
            bool split = !showingAssessment && w >= 780f * sc;
            float leftW = split ? (w - 24f * sc) * 0.52f : w;
            float causeBottom = DrawCause(d, x, y, leftW, sc);
            if (split)
            {
                float ax = x + leftW + 24f * sc;
                y = Mathf.Max(causeBottom, DrawNextSteps(d, tips, state, ax, y, w - leftW - 24f * sc, sc));
            }
            else y = showingAssessment ? causeBottom + 12f * sc : DrawNextSteps(d, tips, state, x, causeBottom + 12f * sc, w, sc);
            y = DrawBossCard(d, x, y, w, sc);
            y = Heading(x, y, w, "Defenses at death", sc);
            y = DrawResistances(d, d.Defenses, null, x, y, w, sc);
            if (d.DefenseSnapshotAgeSeconds is float age) y = Paragraph(x, y, w, $"Snapshot taken {age:0.0}s before death. Buffs may change between snapshots.", sc, true);
            if (DisclosureButton(x, ref y, w, $"Assessment details & saved checks ({DeathTracker.Reassessments?.For(d).Count() ?? 0})", state.Checks, sc)) state.Checks = !state.Checks;
            if (state.Checks)
            {
                y = UpdatePicker(d, x, y, w, sc);
                y = DrawAssessment(d, x, y, w, sc);
            }
            if (DisclosureButton(x, ref y, w, "Death details", state.Details, sc)) state.Details = !state.Details;
            if (state.Details) { _showRawReport = state.Raw; y = DrawDeathDetails(d, x, y, w, sc); state.Raw = _showRawReport; }
            return y;
        }
        // The matching boss note, shown while the player reads this death.
        // IMGUI draws several times per frame, so the lookup is cached per
        // record and refreshed when a late report changes the signature.
        static DeathRecord _bossCardFor;
        static int _bossCardSignature;
        static (IReadOnlyList<string> Lines, BossProfile Boss, BossGuideMove Move) _bossCard;
        static float DrawBossCard(DeathRecord d, float x, float y, float w, float sc)
        {
            int signature = HashCode.Combine(Signature(d), d.IsBossFight);
            if (!ReferenceEquals(d, _bossCardFor) || signature != _bossCardSignature)
            {
                _bossCardFor = d; _bossCardSignature = signature;
                var found = BossFieldNotes.ForDeath(d, out var b, out var m);
                _bossCard = (found, b, m);
            }
            var (lines, boss, move) = _bossCard;
            if (boss == null) return y;
            y = Heading(x, y, w, move != null ? $"Boss attack: {move.Name}" : $"Boss notes: {boss.Name}", sc);
            foreach (string line in lines) y = Paragraph(x, y, w, line, sc);
            if (ActionButton(x, ref y, w, "Notes for " + boss.Name, sc)) OpenBossNotes(boss);
            return y;
        }
        static void OpenBossNotes(BossProfile boss)
        {
            if (boss != null)
            {
                _bossIndex = BossCatalog.All.ToList().IndexOf(boss);
                _bossGroup = "all"; _bossPage = Math.Max(0, _bossIndex) / 6;
            }
            else
            {
                _showBossBrowser = BossNotesFocus.ShowBrowser(false);
                _bossPage = 0;
            }
            _tab = 3; _scroll = new Vector2(0, 0);
        }
        static float DrawCause(DeathRecord d, float x, float y, float w, float sc)
        {
            string types = string.Join(" / ", new[] { d.KillingElement, d.SecondaryKillingElement }.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct());
            if (!string.IsNullOrEmpty(types)) y += Theme.Para(x, y, w, types + " damage", Theme.DamageColor(d.KillingElement), Body(sc)) + 12f * sc;
            string title = d.CauseTitle();
            y += Theme.Para(x, y, w, title, Theme.TextHi, Theme.Label(Mathf.RoundToInt(31f * sc), FontStyle.Bold)) + 12f * sc;
            if (!string.IsNullOrWhiteSpace(d.Killer) && d.CauseTitleNamesAttack()) y = Paragraph(x, y, w, d.Killer, sc);
            string area = !string.IsNullOrWhiteSpace(d.Zone) && d.Zone != d.RawSceneId ? d.Zone : d.ZoneLevel > 0 ? $"Area level {d.ZoneLevel}" : "Area not recorded";
            y = Paragraph(x, y, w, area, sc, true);
            bool reported = d.KillingBlow > 0;
            if (reported)
            {
                y = Paragraph(x, y, w, "Reported damage     Overkill", sc, true);
                y += Theme.Para(x, y, w, $"{d.KillingBlow:N0}     {d.OverkillDamage:N0}", Theme.TextHi, Theme.Label(Mathf.RoundToInt(24f * sc), FontStyle.Bold)) + 12f * sc;
            }
            return y;
        }
        static float DrawNextSteps(DeathRecord d, List<Advice> tips, Disclosure state, float x, float y, float w, float sc)
        {
            float start = y, inset = 18f * sc, ix = x + inset, iw = w - 2 * inset;
            int bodyHash = 0;
            foreach (var tip in tips) bodyHash = HashCode.Combine(bodyHash, tip.Key, tip.Title, tip.Body);
            int memoA = Signature(d), memoB = bodyHash, memoC = HashCode.Combine(Mathf.RoundToInt(w), Mathf.RoundToInt(sc * 1000f));
            List<(string Text, GUIStyle Style, Color Color)> lines;
            bool gapShown;
            float total;
            if (_stepMemo.Matches(memoA, memoB, memoC) && _stepLines != null)
            {
                lines = _stepLines; gapShown = _stepGap; total = _stepMeasure;
            }
            else
            {
                lines = new List<(string Text, GUIStyle Style, Color Color)>();
                lines.Add(("NEXT STEP", Small(sc), Theme.Accent));
                gapShown = false;
                if (tips.Count == 0) lines.Add(("No specific gear change is supported by this record.", Body(sc), Theme.TextHi));
                else
                {
                    var first = tips[0];
                    var gap = ResistanceReview.Build(d.Defenses, d).FirstOrDefault(r => first.Key == "res_" + Elements.Name(r.Element) && r.Gap > 0
                        && AdviceRules.FreshDefense(d, "Res." + Elements.Name(r.Element), out _));
                    lines.Add((gap != null ? "Close the " + Elements.Name(gap.Element) + " resistance gap" : first.Title,
                        Theme.Label(Mathf.RoundToInt(20f * sc), FontStyle.Bold), Theme.TextHi));
                    gapShown = gap != null;
                    if (gap != null)
                    {
                        lines.Add(($"{gap.Effective:0.#}% → 75% · {gap.Gap:0.#} points below cap", Body(sc), Theme.Text));
                        string exact = AdviceRules.ExactHitCardLine(first.Body);
                        lines.Add((exact ?? $"{Elements.Name(gap.Element)} was recorded in this death. Closing this gap may help; it does not guarantee surviving the attack.", Body(sc), Theme.TextMut));
                    }
                    else lines.Add((first.Body, Body(sc), Theme.Text));
                }
                total = 2 * inset + lines.Sum(l => Theme.Wrap(l.Text, l.Style, iw).Count * (Theme.LineHeight(l.Style) + 4f) + 10f * sc);
                _stepLines = lines; _stepGap = gapShown; _stepMeasure = total;
                _stepMemo.Remember(memoA, memoB, memoC);
            }
            float disclosureH = gapShown ? 48f * sc : 0f;
            Theme.Box(new Rect(x, y, w, total + disclosureH), Theme.Card);
            Theme.Fill(new Rect(x, y, 3f, total + disclosureH), Theme.Accent);
            y += inset;
            foreach (var line in lines) y += Theme.Para(ix, y, iw, line.Text, line.Color, line.Style) + 10f * sc;
            if (tips.Count > 0)
            {
                // Without a resistance summary the card already shows the full
                // explanation, so the button would only repeat it.
                if (gapShown)
                {
                    if (ActionButton(ix, ref y, iw, state.Why ? "Hide explanation" : "Why this recommendation?", sc)) state.Why = !state.Why;
                    if (state.Why) y = Paragraph(x, y, w, tips[0].Body, sc);
                }
                foreach (var tip in tips.Skip(1))
                {
                    y = Heading(x, y, w, tip.Title, sc);
                    y = Paragraph(x, y, w, tip.Body, sc);
                }
            }
            return Mathf.Max(start + total, y) + 12f * sc;
        }
        static float DrawDeathDetails(DeathRecord d, float x, float y, float w, float sc)
        {
            if (d.Detection == "game" && string.IsNullOrWhiteSpace(d.RawSceneId) && string.IsNullOrWhiteSpace(d.Zone))
                y = Paragraph(x, y, w, "The game counter confirmed this death after its live context was unavailable. Your current location and defenses have not been substituted for the missing death snapshot.", sc, true);
            y = Paragraph(x, y, w, $"Captured {d.UtcTime.ToLocalTime():yyyy-MM-dd h:mm:ss tt} · {d.Character} · Level {d.Level}", sc, true);
            y = Paragraph(x, y, w, d.LocationLabel() + " · " + d.PlayContextLabel(), sc, true);
            y = Paragraph(x, y, w, "Critical killing blow: " + (d.KillingCrit.HasValue ? d.KillingCrit.Value ? "Yes" : "No" : "Not recorded")
                + " · Boss encounter: " + (d.IsBossFight.HasValue ? d.IsBossFight.Value ? "Yes" : "No" : "Not recorded"), sc, true);
            y = Paragraph(x, y, w, "Killing ailment: " + d.KillingAilmentLabel(), sc, true);
            y = Paragraph(x, y, w, BossCatalog.EncounterLabel(d), sc, true);
            if (d.IsBossFight == true || BossCatalog.AttackerGuide(d) != null)
            {
                var boss = BossCatalog.AttackerGuide(d);
                                if (ActionButton(x, ref y, w, boss == null ? "Browse boss notes" : "Notes for " + boss.Name, sc)) OpenBossNotes(boss);
            }
            if (!string.IsNullOrWhiteSpace(d.GameDeathInfoRich) || !string.IsNullOrWhiteSpace(d.GameDeathInfo))
            {
                if (DisclosureButton(x, ref y, w, "Original game report", _showRawReport, sc)) _showRawReport = !_showRawReport;
                if (_showRawReport)
                {
                    if (!string.IsNullOrWhiteSpace(d.GameDeathInfoRich)) y += Theme.GamePara(x, y, w, d.GameDeathInfoRich, Body(sc)) + 12f * sc;
                    else y = Paragraph(x, y, w, d.GameDeathInfo, sc);
                }
            }
            if (!string.IsNullOrWhiteSpace(d.DetailSource)) y = Paragraph(x, y, w, "Cause supplied by the game. The full damage timeline may be unavailable.", sc, true);
            if (d.Kind == DeathKind.Unknown && string.IsNullOrWhiteSpace(d.Killer) && string.IsNullOrWhiteSpace(d.KillerAbility) && string.IsNullOrWhiteSpace(d.KillingAilment) && string.IsNullOrWhiteSpace(d.GameDeathInfo))
                y = Paragraph(x, y, w, "This death was counted, but its cause was not captured when it happened. Updating the mod cannot recover details that were never saved.", sc, true);
            if (d.AilmentsOnYou?.Count > 0)
            {
                y = Heading(x, y, w, "Ailments", sc);
                foreach (var name in d.AilmentsOnYou)
                    y = Paragraph(x, y, w, Ailments.CardLine(name), sc);
            }
            if (d.Hits > 0)
            {
                y = Heading(x, y, w, $"Recorded health loss ({d.WindowSeconds:0}s)", sc);
                y = Paragraph(x, y, w, $"{d.WindowDamage:N0} health lost across {d.Hits} recorded events. Recorded loss is health plus ward when both could be read. A hit that only broke ward is kept.", sc);
                if (d.WardDominatedLine() is string wardNote) y = Paragraph(x, y, w, wardNote, sc);
                // An old or hand-edited record can carry a short array.
                float total = d.DamageByElement?.Length == Elements.Count ? d.DamageByElement.Sum() : 0f;
                if (total > 0)
                {
                    y = Paragraph(x, y, w, "Type attribution uses the incoming pre-mitigation damage mix. These are not measured damage amounts after each resistance.", sc, true);
                    foreach (int i in Enumerable.Range(0, Elements.Count).Where(i => d.DamageByElement[i] > 0).OrderByDescending(i => d.DamageByElement[i]))
                        y += Theme.Para(x, y, w, $"{Elements.Names[i]}: {d.DamageByElement[i]:N0} ({100 * d.DamageByElement[i] / total:0}%)", Theme.ElementColor((Element)i), Body(sc)) + 12f * sc;
                }
            }
            if (d.Hits <= 0) y = Paragraph(x, y, w, "A full hit timeline was not recorded.", sc, true);
            if (d.Defenses?.Count > 0) { y = Heading(x, y, w, "Other defenses at death", sc); y = DrawDefenseStats(d.Defenses, x, y, w, sc); }
            y = Paragraph(x, y, w, "Capture source: " + (d.DetailSource ?? d.Detection ?? "Not recorded"), sc, true);
            y += Theme.Para(x, y, w, "Capture confidence: " + Advisor.Confidence(d) + ".", Theme.TextMut, Small(sc)) + 12f * sc;
            return y;
        }
        static float DrawHistory(float x, float w, float y, float sc)
        {
            int saved = DeathTracker.Log?.SavedCountFor(ViewCharacter) ?? 0;
            y = Paragraph(x, y, w, $"{ViewCharacter} · {saved} saved deaths. Resetting the displayed counter keeps this history.", sc, true);
            if (_records.Count == 0) return Paragraph(x, y, w, "No deaths recorded yet. Deaths will appear here automatically while you play.", sc);
            _historyPage = HistoryPages.Clamp(_historyPage, _records.Count);
            int pages = HistoryPages.Count(_records.Count);
            if (pages > 1) y = Paragraph(x, y, w, $"Page {_historyPage + 1} of {pages}", sc, true);
            foreach (int i in HistoryPages.NewestFirst(_records.Count, _historyPage))
            {
                var death = _records[i];
                string cause = death.CauseTitle();
                string detail = string.Join(" · ", new[] { death.Killer, death.KillingElement }.Where(t => !string.IsNullOrWhiteSpace(t)));
                string label = $"#{death.Number} · {death.UtcTime.ToLocalTime():MMM d, yyyy · h:mm tt}\n{cause}" + (detail.Length > 0 ? "\n" + detail : "");
                if (ActionButton(x, ref y, w, label, sc)) { SelectDeath(death); _tab = 0; }
            }
            if (pages > 1)
            {
                float half = (w - 12f * sc) / 2f, ly = y, ry = y;
                if (ActionButton(x, ref ly, half, "Previous", sc, _historyPage > 0)) _historyPage--;
                if (ActionButton(x + half + 12f * sc, ref ry, half, "Next", sc, _historyPage < pages - 1)) _historyPage++;
                y = Mathf.Max(ly, ry);
            }
            return y;
        }
        static float DrawPatterns(float x, float w, float y, float sc)
        {
            int signature = HashCode.Combine(ViewCharacter, _records.Count);
            foreach (var death in _records.TakeLast(DeathPatterns.DefaultWindow)) signature = HashCode.Combine(signature, Signature(death));
            if (_patternReport == null || signature != _patternSignature)
            { _patternReport = DeathPatterns.Build(_records); _patternSignature = signature; }
            var report = _patternReport;
            y = Paragraph(x, y, w, $"Across {report.Deaths} recent deaths for {ViewCharacter} · {_records.Count} total in history.", sc, true);
            if (_records.Count == 0) return Paragraph(x, y, w, "Patterns will appear after deaths have been recorded.", sc);
            var recent = _records.TakeLast(report.Deaths).ToList();
            int known = recent.Count(d => !string.IsNullOrWhiteSpace(d.Killer) || !string.IsNullOrWhiteSpace(d.KillingAilment) || !string.IsNullOrWhiteSpace(d.KillerAbility) || !string.IsNullOrWhiteSpace(d.KillingElement));
            y = Heading(x, y, w, "Evidence coverage", sc);
            y = Paragraph(x, y, w, $"{known} of {report.Deaths} deaths have a recorded cause. {report.Deaths - known} causes were not recorded. Saved gear checks are not deaths.", sc);
            if (!report.TopKillers.Any(k => k.Count > 1)) y = Paragraph(x, y, w, "No repeated killer is established by this history yet.", sc, true);
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
        static float DrawBossTips(float x, float w, float y, float sc)
        {
            y = Heading(x, y, w, "Boss notes", sc);
            y = Paragraph(x, y, w, "Experimental · Still gathering encounter data", sc, true);
                        _bossIndex = Math.Clamp(_bossIndex, 0, BossCatalog.All.Count - 1);
            if (DisclosureButton(x, ref y, w, "Browse encounters", _showBossBrowser, sc)) _showBossBrowser = !_showBossBrowser;
            if (_showBossBrowser)
            {
            string[] groups = { "all", "boss", "harbinger", "dungeon" };
            string[] groupNames = { "All", "Bosses", "Harbingers", "Dungeons" };
            bool groupChanged = false;
            int filterCols = w >= 600f * sc ? 4 : 2;
            float filterW = (w - 8f * sc * (filterCols - 1)) / filterCols;
            float filterY = y, filterBottom = y;
            for (int i = 0; i < groups.Length; i++)
            {
                float atY = filterY + (i / filterCols) * 60f * sc;
                if (ActionButton(x + i % filterCols * (filterW + 8f * sc), ref atY, filterW,
                    (_bossGroup == groups[i] ? "• " : "") + groupNames[i], sc))
                { groupChanged = _bossGroup != groups[i]; _bossGroup = groups[i]; _bossPage = 0; }
                filterBottom = Mathf.Max(filterBottom, atY);
            }
            y = filterBottom;
            var choices = BossCatalog.All.Select((p, index) => (Profile: p, Index: index))
                .Where(p => _bossGroup == "all" || p.Profile.BrowseGroups.Contains(_bossGroup)).ToList();
            if (groupChanged && choices.Count > 0) _bossIndex = choices[0].Index;
            int pages = Math.Max(1, (choices.Count + 5) / 6);
            _bossPage = Math.Clamp(_bossPage, 0, pages - 1);
            y = Paragraph(x, y, w, $"{choices.Count} encounters · Page {_bossPage + 1} of {pages}", sc, true);
            float half = (w - 12f * sc) / 2f, ly = y, ry = y;
            if (ActionButton(x, ref ly, half, "Previous", sc, _bossPage > 0)) _bossPage--;
            if (ActionButton(x + half + 12f * sc, ref ry, half, "Next", sc, _bossPage < pages - 1)) _bossPage++;
            y = Mathf.Max(ly, ry);
            foreach (var choice in choices.Skip(_bossPage * 6).Take(6))
            {
                string context = BossContextLabels.Label(choice.Profile);
                string label = (_bossIndex == choice.Index ? "Selected: " : "") + BossBrowseName(choice.Profile)
                    + (context == null ? "" : "\n(" + context + ")");
                if (ActionButton(x, ref y, w, label, sc)) { _bossIndex = choice.Index; _showBossBrowser = false; }
            }
            }
            var boss = BossCatalog.All[_bossIndex];
            if (_shownBossId != boss.Id)
            {
                _shownBossId = boss.Id; _bossResearch = boss.Encounter; _bossPreparation = BossFieldNotes.Preparation(boss); _bossSources = BossFieldNotes.SourceLines(boss);
                _showBossMoves = false; _bossMovePage = 0;
            }
            y = Heading(x, y, w, BossBrowseName(boss), sc);
            if (BossContextLabels.Label(boss) is string bossContext) y = Paragraph(x, y, w, "(" + bossContext + ")", sc, true);
            y = Heading(x, y, w, boss.Id == "shade-of-orobyss" ? "Possible variant damage" : "Damage to prepare for", sc);
            var priority = BossFieldNotes.PriorityResistances(boss);
            bool ranked = priority.Count > 0 && (priority.Count < boss.DamageTypes.Count || boss.Id == "majasa-phase-1");
            for (int i = 0; i < boss.DamageTypes.Count; i++)
            {
                var element = boss.DamageTypes[i];
                Theme.Write(new Rect(x + (i % 3) * (w / 3), y, w / 3, 32f * sc), Elements.Name(element) + (ranked && priority.Contains(element) ? " (main)" : ""), Theme.ElementColor(element), Body(sc));
                if (i % 3 == 2 || i == boss.DamageTypes.Count - 1) y += 38f * sc;
            }
            string guideResists = MaxrollPlayerNotes.Resists(boss.Id);
            if (!string.IsNullOrEmpty(guideResists)) y = Paragraph(x, y, w, guideResists, sc);
            y = Heading(x, y, w, "Before you pull", sc);
            foreach (var note in _bossPreparation) y = Paragraph(x, y, w, note, sc);
            y = Heading(x, y, w, "Quick fight plan", sc);
            if (boss.Tips.Count == 0) y = Paragraph(x, y, w, "Fight-specific tips are still being researched for this profile. Check Last death for advice supported by your own captured defenses.", sc, true);
            else
            {
                foreach (var tip in boss.Tips) y = Paragraph(x, y, w, tip, sc);
            }
            var death = _selected >= 0 && _selected < _records.Count ? _records[_selected] : null;
            if (death != null && BossCatalog.AttackerGuide(death)?.Id == boss.Id)
            {
                y = Heading(x, y, w, $"Related capture · Death #{death.Number}", sc);
                foreach (var element in BossCatalog.ObservedTypes(death))
                    y += Theme.Para(x, y, w, "Recorded killing damage: " + Elements.Name(element), Theme.ElementColor(element), Body(sc)) + 12f * sc;
                y = Paragraph(x, y, w, "Last death shows the defenses captured during this attempt.", sc, true);
            }
            var conflicts = (_bossResearch?.Claims ?? new List<ClaimRecord>()).Where(c => c.Id.Contains(".conflict.")).ToList();
            if (conflicts.Count > 0) y = Paragraph(x, y, w, "Some attack details still need verification. This is not complete damage coverage.", sc, true);
            var moves = BossGuideMoves.For(boss.Id);
            if (moves.Count > 0)
            {
                if (GUI.Button(new Rect(x, y, w, 38f * sc), _showBossMoves ? "Hide attack details" : $"Attack damage and ailments ({moves.Count})", Button(sc))) _showBossMoves = !_showBossMoves;
                y += 46f * sc;
                if (_showBossMoves)
                {
                    var deathMove = death != null && BossCatalog.AttackerGuide(death)?.Id == boss.Id ? BossGuideMoves.KillingMove(death) : null;
                    var orderedIds = BossAttackPages.Order(moves.Select(m => m.Id).ToList(), deathMove?.Id);
                    var byId = moves.ToDictionary(m => m.Id, m => m, StringComparer.Ordinal);
                    _bossMovePage = BossAttackPages.Clamp(_bossMovePage, orderedIds.Count);
                    int movePages = BossAttackPages.Count(orderedIds.Count);
                    string heightKey = boss.Id + ":" + _bossMovePage + ":" + (int)w + ":" + sc.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ":" + (deathMove?.Id ?? "");
                    y = Paragraph(x, y, w, "Reported attack facts. Missing types, ailments and hit/DoT details remain unknown. These do not replace your captured killing blow.", sc, true);
                    if (movePages > 1) y = Paragraph(x, y, w, $"Page {_bossMovePage + 1} of {movePages}. The killing move is first when this death has one.", sc, true);
                    float pageTop = y;
                    if (movePages > 1)
                    {
                        float half = (w - 12f * sc) / 2f, ly = y, ry = y;
                        if (ActionButton(x, ref ly, half, "Previous attacks", sc, _bossMovePage > 0)) _bossMovePage--;
                        if (ActionButton(x + half + 12f * sc, ref ry, half, "Next attacks", sc, _bossMovePage < movePages - 1)) _bossMovePage++;
                        y = Mathf.Max(ly, ry);
                        pageTop = y;
                    }
                    bool attributed = false;
                    foreach (var id in BossAttackPages.Page(orderedIds, _bossMovePage))
                    {
                        if (!byId.TryGetValue(id, out var move)) continue;
                        y = Heading(x, y, w, move.Id == deathMove?.Id ? "Killing move: " + move.Name : move.Name, sc);
                        foreach (var element in move.Elements)
                            y += Theme.Para(x, y, w, "Reported damage: " + Elements.Name(element), Theme.ElementColor(element), Body(sc)) + 8f * sc;
                        if (move.Elements.Count == 0) y = Paragraph(x, y, w, "Damage type not mapped.", sc, true);
                        if (move.Ailments.Count > 0) y = Paragraph(x, y, w, "Reported ailments: " + string.Join(", ", move.Ailments), sc, true);
                        if (move.Delivery != "unknown") y = Paragraph(x, y, w, "Reported delivery: " + (move.Delivery == "dot" ? "damage over time" : move.Delivery), sc, true);
                        foreach (string line in MaxrollPlayerNotes.MoveLines(move.Id, !attributed))
                        {
                            if (line == MaxrollPlayerNotes.Attribution) attributed = true;
                            y = Paragraph(x, y, w, line, sc);
                        }
                    }
                    if (!_bossMoveHeights.TryGet(heightKey, out _)) _bossMoveHeights.Store(heightKey, Math.Max(1f, y - pageTop));
                }
            }
            var sources = _bossSources ?? Array.Empty<string>();
            if (sources.Count > 0)
            {
                if (DisclosureButton(x, ref y, w, $"Sources ({sources.Count})", _showBossSources, sc)) _showBossSources = !_showBossSources;
                if (_showBossSources)
                {
                    y = Paragraph(x, y, w, "These notes summarize the references below. Community guides are not verified game data, and listing them does not imply affiliation or endorsement.", sc, true);
                    foreach (string line in sources) y = Paragraph(x, y, w, line, sc, true);
                }
            }
            if (DisclosureButton(x, ref y, w, "Contribute encounter data", _showContributions, sc)) _showContributions = !_showContributions;
            if (_showContributions)
            {
                y = Paragraph(x, y, w, "Help improve these experimental notes with captured damage, encounter details or corrections.", sc, true);
                if (ActionButton(x, ref y, w, "Contribute on GitHub", sc))
                    try { Application.OpenURL("https://github.com/medick51o/LastEpoch-Mods"); }
                    catch (Exception ex) { Dbg.Log("contribution page: " + ex.Message); }
            }
            return y;
        }
        static string BossBrowseName(BossProfile boss)
        {
            if (boss.Id == "lagon-campaign") return boss.Name + " (campaign)";
            if (boss.Id == "lagon-monolith") return boss.Name + " (monolith)";
            if (boss.Id == "majasa-phase-1") return boss.Name + " (phase 1)";
            if (boss.Id == "majasa-phase-2") return boss.Name + " (phase 2)";
            if (boss.Id == "admiral-harton-summit") return boss.Name + " (summit variant)";
            if (boss.Id == "aberroth-invisible-add") return boss.Name + " (unidentified add)";
            return boss.Name;
        }
        static float DrawOptions(float x, float w, float y, float sc)
        {
            y = Paragraph(x, y, w, "Deaths are recorded automatically. Display settings do not stop the log.", sc, true);
            if (ActionButton(x, ref y, w, Prefs.ShowCounter.Value ? "Show counter: On" : "Show counter: Off", sc)) { Prefs.ShowCounter.Value = !Prefs.ShowCounter.Value; Prefs.Save(); }
            if (ActionButton(x, ref y, w, Prefs.ShowDeathToast.Value ? "Death notification: On" : "Death notification: Off", sc)) { Prefs.ShowDeathToast.Value = !Prefs.ShowDeathToast.Value; Prefs.Save(); }
            y = Heading(x, y, w, "Counter position", sc);
            y = Paragraph(x, y, w, "Hover over the number to reveal MOVE, then drag. You can also enter move mode below.", sc, true);
            if (ActionButton(x, ref y, w, "Move counter", sc)) CounterHud.BeginMove();
            if (ActionButton(x, ref y, w, "Reset counter position", sc)) { Prefs.HudX.Value = 0.5f; Prefs.HudY.Value = 0.015f; Prefs.Save(); }
            y = SizeControl(x, w, y, sc, "Counter size", Prefs.HudScale.Value, 0.9f, 2f, v => { Prefs.HudScale.Value = v; Prefs.Save(); });
            y = SizeControl(x, w, y, sc, "Text size", Prefs.PanelScale.Value, 1f, 1.4f, v => { Prefs.PanelScale.Value = v; Prefs.Save(); });
            y = Heading(x, y, w, "Reset counter", sc);
            bool sameCharacter = ViewCharacter == DeathTracker.Character;
            y = Paragraph(x, y, w, "Reset changes only this character's displayed and session counter. All saved deaths and the game's count stay intact.", sc);
            if (!sameCharacter) y = Paragraph(x, y, w, $"Load {ViewCharacter} to reset this counter.", sc, true);
            if (!_confirmReset)
            {
                if (ActionButton(x, ref y, w, "Reset counter", sc, sameCharacter)) _confirmReset = true;
            }
            else
            {
                y = Paragraph(x, y, w, "Resetting the counter does not make you a bad person. But Medick, the author, will judge you.", sc);
                if (ActionButton(x, ref y, w, "Keep counter", sc)) _confirmReset = false;
                if (ActionButton(x, ref y, w, "Reset counter", sc, sameCharacter))
                {
                    _status = DeathTracker.ResetCounter() ? "Counter reset. All saved deaths are still in History." : "Reset could not be saved. The counter has been kept.";
                    _confirmReset = false;
                }
            }
            y = Heading(x, y, w, "History & files", sc);
            if (ActionButton(x, ref y, w, "Open log folder", sc)) OpenFolder();
            if (ActionButton(x, ref y, w, "Export this character's history", sc))
            {
                try
                {
                    string safe = string.Concat(ViewCharacter.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                    string file = Path.Combine(DeathTracker.Log.Directory, safe + "-deaths-" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff"));
                    DeathTracker.Log.ExportCharacter(ViewCharacter, file + ".jsonl");
                    try
                    {
                        File.WriteAllLines(file + ".txt", _records.Select(d => d.ToLogLine()));
                        _status = "Full JSON history and readable text exported, including unsaved session deaths. Open log folder to find them.";
                    }
                    catch { _status = "Full JSON history exported, including unsaved session deaths. The readable text export failed."; }
                }
                catch { _status = "Export could not be saved. Your existing history is unchanged."; }
            }
            if (_status != null) y = Paragraph(x, y, w, _status, sc);
            if (DisclosureButton(x, ref y, w, "About & troubleshooting", _showAbout, sc)) _showAbout = !_showAbout;
            if (_showAbout)
            {
                y = Paragraph(x, y, w, BuildInfo.DisplayName + " · Experimental v" + BuildInfo.Version, sc);
                y = Paragraph(x, y, w, "Medick death log. Cause details depend on what the game reports; missing fields stay unknown.", sc, true);
                y = Paragraph(x, y, w, InputBlocker.Status, sc, true);
                y = Paragraph(x, y, w, "For troubleshooting, include MelonLoader/Latest.log and the selected death's capture details. Saved history and gear checks are in the log folder.", sc, true);
            }
            return y;
        }
        static float SizeControl(float x, float w, float y, float sc, string caption, float value, float min, float max, Action<float> change)
        {
            y = Paragraph(x, y, w, $"{caption}: {value:0.0}x", sc);
            float half = (w - 12f * sc) / 2f, leftY = y, rightY = y;
            if (ActionButton(x, ref leftY, half, "Smaller", sc, value > min)) change(Mathf.Clamp(value - 0.1f, min, max));
            if (ActionButton(x + half + 12f * sc, ref rightY, half, "Larger", sc, value < max)) change(Mathf.Clamp(value + 0.1f, min, max));
            return Mathf.Max(leftY, rightY);
        }
        static void OpenFolder()
        {
            if (DeathTracker.Log == null) return;
            try { OpenFolderInterop(DeathTracker.Log.Directory); }
            catch (Exception ex) { Dbg.Log("open log folder: " + ex.Message); }
        }
        static Color AdviceColor(Advice tip)
        {
            if (tip.Key.StartsWith("res_", StringComparison.Ordinal)) return Theme.DamageColor(tip.Key.Substring(4));
            if (tip.Key.StartsWith("dot_", StringComparison.Ordinal) && Ailments.ByName(tip.Key.Substring(4))?.Element is Element el) return Theme.ElementColor(el);
            return Theme.TextHi;
        }
        static void OpenFolderInterop(string directory)
        {
            Directory.CreateDirectory(directory);
            Application.OpenURL("file:///" + directory.Replace('\\', '/'));
        }
    }
}
