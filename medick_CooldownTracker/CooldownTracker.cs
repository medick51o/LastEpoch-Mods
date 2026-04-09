// ================================================================
//  medick_CooldownTracker  v4.4
//  Home key = toggle settings
//
//  v4.4 fixes
//  ─────────────────────────────────────────────────────────────
//  • Backspace / Delete now work in label text fields
//  • Movement lock now uses EpochInputManager.forceDisableInput
//    — the game's own input manager flag, used by every working
//    Last Epoch mod (Unity Input patches don't intercept the game's
//    custom input system)
//  • Game hotkeys (inventory, skills) no longer fire while typing
//    in a label field — same EpochInputManager mechanism
//
//  v4.3: corrected default button arrays; movement lock toggle
//  v4.2: [▼] Button Picker for Xbox AND PS5 modes
//  v4.1: per-mode custom labels (KB / Xbox / PS5 fully independent)
//  v4.1: Slot 6 (evade/dodge) fully supported & customisable
//
//  Button defaults (slots 0-6)
//  ─────────────────────────────────────────────────────────────
//  Slot:      0    1    2    3    4    5    6(evade)
//  Xbox:      X    Y    RB   LT   L    RT   B
//  PS5:       □    △    R1   L2   L3   R2   ○
//  Keyboard:  Q    W    E    R    RMB  T    Space
// ================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

[assembly: MelonInfo(typeof(medick_CooldownTracker.CooldownTrackerMod),
    "medick_CooldownTracker", "4.4.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_CooldownTracker
{
    internal enum CtrlLayout { Xbox, PlayStation }

    // ── Live input detector ───────────────────────────────────────
    internal static class InputTracker
    {
        static float    _lastCtrl  = -999f;
        static float    _lastKb    = -999f;
        static float    _namesNext = 0f;
        static string[] _joyNames  = Array.Empty<string>();

        public static bool       IsControllerActive { get; private set; }
        public static CtrlLayout DetectedLayout     { get; private set; } = CtrlLayout.Xbox;

        const int JoyFirst = (int)KeyCode.JoystickButton0;
        const int JoyLast  = (int)KeyCode.JoystickButton19;

        public static void Update()
        {
            float t = Time.time;
            if (t >= _namesNext)
            {
                _namesNext = t + 5f;
                try   { _joyNames = Input.GetJoystickNames() ?? Array.Empty<string>(); }
                catch { _joyNames = Array.Empty<string>(); }

                DetectedLayout = CtrlLayout.Xbox;
                foreach (var n in _joyNames)
                    if (n != null &&
                        (n.IndexOf("DualSense",           StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("DualShock",           StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("PlayStation",         StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Wireless Controller", StringComparison.OrdinalIgnoreCase) >= 0))
                    { DetectedLayout = CtrlLayout.PlayStation; break; }

                if (_lastCtrl < -990f && _lastKb < -990f)
                    IsControllerActive = _joyNames.Any(n => !string.IsNullOrEmpty(n));
            }

            if (Input.anyKeyDown)
            {
                bool joyBtn = false;
                for (int k = JoyFirst; k <= JoyLast; k++)
                    if (Input.GetKeyDown((KeyCode)k)) { joyBtn = true; break; }
                if (joyBtn) { _lastCtrl = t; IsControllerActive = true;  }
                else        { _lastKb   = t; IsControllerActive = false; }
            }
            try
            {
                if (Mathf.Abs(Input.GetAxisRaw("Mouse X")) > 0.01f ||
                    Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.01f)
                { _lastKb = t; if (_lastCtrl < t - 1f) IsControllerActive = false; }
            }
            catch { }
        }
    }

    // ── Button label tables ───────────────────────────────────────
    internal static class ButtonLabels
    {
        // 7 entries: slots 0-5 (skills) + slot 6 (evade/dodge)
        public static readonly string[] Xbox        = { "X",  "Y",  "RB", "LT", "L",  "RT", "B"     };
        public static readonly string[] PlayStation = { "□",  "△",  "R1", "L2", "L3", "R2", "○"     };
        public static readonly string[] Keyboard    = { "Q",  "W",  "E",  "R",  "RMB","T",  "Space" };

        // modeIdx: 0=Keyboard  1=Xbox  2=PlayStation
        public static int GetModeIndex(int inputModePref, int ctrlLayoutPref)
        {
            switch (inputModePref)
            {
                case 1: return 0;
                case 2: return 1;
                case 3: return 2;
                default:
                    if (!InputTracker.IsControllerActive) return 0;
                    CtrlLayout lay =
                        ctrlLayoutPref == 1 ? CtrlLayout.Xbox :
                        ctrlLayoutPref == 2 ? CtrlLayout.PlayStation :
                        InputTracker.DetectedLayout;
                    return lay == CtrlLayout.PlayStation ? 2 : 1;
            }
        }

        static string[] TableFor(int mi) =>
            mi == 2 ? PlayStation : mi == 1 ? Xbox : Keyboard;

        public static string Default(int idx, int inputModePref, int ctrlLayoutPref)
        {
            var arr = TableFor(GetModeIndex(inputModePref, ctrlLayoutPref));
            return (idx >= 0 && idx < arr.Length) ? arr[idx] : $"#{idx}";
        }

        public static string Resolve(int idx, int inputModePref, int ctrlLayoutPref,
            string gameBound, MelonPreferences_Entry<string>[][] customs)
        {
            int mi = GetModeIndex(inputModePref, ctrlLayoutPref);
            string custom = "";
            if (customs != null && mi < customs.Length && customs[mi] != null
                && idx < customs[mi].Length)
                custom = customs[mi][idx]?.Value ?? "";
            if (!string.IsNullOrEmpty(custom)) return custom;
            if (mi == 0 && !string.IsNullOrEmpty(gameBound)) return gameBound;
            return Default(idx, inputModePref, ctrlLayoutPref);
        }
    }

    // ── Game keybind reader ───────────────────────────────────────
    internal static class HotkeyReader
    {
        public static string TryRead(AbilityBarIcon icon)
        {
            if (icon == null) return null;
            try
            {
                var texts = icon.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                { var s = t?.text?.Trim(); if (IsHotkeyLike(s)) return s; }

                var all = icon.GetComponentsInChildren<Component>(true);
                foreach (var comp in all)
                {
                    if (comp == null) continue;
                    try
                    {
                        var n = comp.GetIl2CppType()?.Name ?? "";
                        if (!n.Contains("Text") && !n.Contains("TMP") && !n.Contains("Label")) continue;
                        var p = comp.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                        if (p == null) continue;
                        var v = p.GetValue(comp)?.ToString()?.Trim();
                        if (IsHotkeyLike(v)) return v;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
        static bool IsHotkeyLike(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 1 || s.Length > 6) return false;
            if (s.All(char.IsDigit)) return false;
            if (s.Contains("%") || s.Contains(":") || s.Contains(".")) return false;
            return true;
        }
    }

    // ── Per-slot data ─────────────────────────────────────────────
    internal class SlotData
    {
        public int    SlotIndex;
        public string GameBoundKey;
        public Sprite Icon;
        public Image  CooldownBar;
        public float  Fill;
        public bool   OnCooldown;
        public bool   TrackEnabled = true;
        public AbilityBarIcon Source;
    }

    // ── Mod ───────────────────────────────────────────────────────
    public class CooldownTrackerMod : MelonMod
    {
        // ── Preferences ───────────────────────────────────────────
        internal static MelonPreferences_Entry<float> PrefAlpha;
        internal static MelonPreferences_Entry<float> PrefSize;
        internal static MelonPreferences_Entry<float> PrefOffsetX;
        internal static MelonPreferences_Entry<float> PrefOffsetY;
        internal static MelonPreferences_Entry<float> PrefMenuScale;
        internal static MelonPreferences_Entry<int>   PrefInputMode;   // 0=Auto 1=KB 2=Xbox 3=PS5
        internal static MelonPreferences_Entry<int>   PrefCtrlLayout;  // 0=Auto 1=Xbox 2=PS5
        internal static MelonPreferences_Entry<bool>  PrefLockInput;   // block movement while settings open

        internal static bool BlockMovementInputs =>
            _showSettings && (PrefLockInput?.Value ?? false);

        // Per-mode custom labels  [modeIdx 0-2][slotIdx 0-6]
        // 0=Keyboard  1=Xbox  2=PlayStation  — completely independent
        internal static readonly MelonPreferences_Entry<string>[][] PrefSlotLabels =
        {
            new MelonPreferences_Entry<string>[7],  // Keyboard
            new MelonPreferences_Entry<string>[7],  // Xbox
            new MelonPreferences_Entry<string>[7],  // PlayStation
        };
        static readonly string[] ModePrefPfx  = { "Kb", "Xb", "Ps" };
        static readonly string[] ModeDispName = { "Keyboard", "Xbox", "PS5" };

        // ── Slot registry ─────────────────────────────────────────
        internal static readonly List<SlotData> Slots = new();
        static readonly object _lock = new();

        // ── GUI state ─────────────────────────────────────────────
        static bool    _showSettings;
        static Rect    _settRect = new(20, 60, 380, 30);
        static bool    _dragging;
        static Vector2 _dragOff;
        static bool    _stylesReady;
        internal static bool _textFieldActive;  // true when any label text field has keyboard focus
        static GUIStyle _lbl, _bold, _left, _small, _sectionHdr, _dimText, _textField;
        static Vector2  _scroll;

        // ── Button picker state ───────────────────────────────────
        static int _pickerSlot = -1;   // -1 = closed

        // PS5 face button symbols + display colours
        static readonly string[] PS5Face   = { "△",  "□",  "○",  "✕"  };
        static readonly Color[]  PS5FaceC  =
        {
            new Color(0.30f, 0.85f, 0.55f),  // △ Triangle – green
            new Color(0.95f, 0.48f, 0.70f),  // □ Square   – pink
            new Color(0.95f, 0.28f, 0.28f),  // ○ Circle   – red
            new Color(0.38f, 0.52f, 0.96f),  // ✕ Cross    – blue
        };
        // Xbox face button symbols + display colours
        static readonly string[] XboxFace  = { "A",  "B",  "X",  "Y"  };
        static readonly Color[]  XboxFaceC =
        {
            new Color(0.15f, 0.80f, 0.28f),  // A – green
            new Color(0.90f, 0.20f, 0.15f),  // B – red
            new Color(0.22f, 0.45f, 0.95f),  // X – blue
            new Color(0.95f, 0.78f, 0.05f),  // Y – gold
        };

        // Tick
        float _tick;
        const float TickRate = 0.05f;

        // ─────────────────────────────────────────────────────────
        public override void OnInitializeMelon()
        {
            var cat = MelonPreferences.CreateCategory("medick_CooldownTracker");
            PrefAlpha      = cat.CreateEntry("Alpha",      0.92f, "Icon opacity");
            PrefSize       = cat.CreateEntry("Size",       64f,   "Icon size px");
            PrefOffsetX    = cat.CreateEntry("OffsetX",    0f,    "Horizontal screen offset");
            PrefOffsetY    = cat.CreateEntry("OffsetY",    -160f, "Vertical screen offset");
            PrefMenuScale  = cat.CreateEntry("MenuScale",  1.0f,  "Settings panel scale");
            PrefInputMode  = cat.CreateEntry("InputMode",  0,     "0=Auto 1=KB 2=Xbox 3=PS5");
            PrefCtrlLayout = cat.CreateEntry("CtrlLayout", 0,     "0=Auto 1=Xbox 2=PS5");
            PrefLockInput  = cat.CreateEntry("LockInputWhenOpen", false, "Block movement inputs while settings open");
            for (int m = 0; m < 3; m++)
                for (int i = 0; i < 7; i++)
                    PrefSlotLabels[m][i] = cat.CreateEntry(
                        $"SlotLabel{ModePrefPfx[m]}{i}", "",
                        $"Custom label [{ModeDispName[m]}] slot {i}");
            MelonLogger.Msg("medick_CooldownTracker v4.4  |  Home = settings");
        }

        // ── Slot registration ─────────────────────────────────────
        internal static void RegisterSlot(AbilityBarIcon icon)
        {
            if (icon == null) return;
            try
            {
                int    idx = icon.abilityNumber;
                var    img = icon.icon;
                string gk  = HotkeyReader.TryRead(icon);
                lock (_lock)
                {
                    Slots.RemoveAll(s => s.SlotIndex == idx);
                    Slots.Add(new SlotData
                    {
                        SlotIndex    = idx,
                        GameBoundKey = gk,
                        Icon         = img?.sprite,
                        CooldownBar  = icon.cooldownBar,
                        OnCooldown   = icon.cooldownBarActive,
                        Source       = icon,
                    });
                    Slots.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
                }
                MelonLogger.Msg($"[CDT] Slot #{idx} | gk={gk ?? "n/a"} | icon={img?.sprite?.name ?? "none"}");
            }
            catch (Exception ex) { MelonLogger.Warning("[CDT] RegisterSlot: " + ex.Message); }
        }

        internal static void OnActivate(AbilityBarIcon icon)
        {
            if (icon == null) return;
            lock (_lock)
            {
                var s = Slots.FirstOrDefault(x => x.SlotIndex == icon.abilityNumber);
                if (s != null) s.OnCooldown = true;
            }
        }
        internal static void OnDeactivate(AbilityBarIcon icon)
        {
            if (icon == null) return;
            lock (_lock)
            {
                var s = Slots.FirstOrDefault(x => x.SlotIndex == icon.abilityNumber);
                if (s != null) { s.OnCooldown = false; s.Fill = 0f; }
            }
        }

        static string GetEffectiveLabel(int idx, SlotData s = null) =>
            ButtonLabels.Resolve(idx, PrefInputMode.Value, PrefCtrlLayout.Value,
                s?.GameBoundKey, PrefSlotLabels);

        // ── Helpers: safe custom label read/write ─────────────────
        static string SafeCustom(int mi, int idx)
        {
            if (mi < 0 || mi >= PrefSlotLabels.Length) return "";
            var row = PrefSlotLabels[mi];
            if (row == null || idx < 0 || idx >= row.Length) return "";
            return row[idx]?.Value ?? "";
        }
        static void SetCustom(int mi, int idx, string val)
        {
            if (mi < 0 || mi >= PrefSlotLabels.Length) return;
            var row = PrefSlotLabels[mi];
            if (row == null || idx < 0 || idx >= row.Length) return;
            if (row[idx] != null) row[idx].Value = val;
        }

        // ── OnUpdate ──────────────────────────────────────────────
        public override void OnUpdate()
        {
            InputTracker.Update();

            // When a label text field is focused, do NOT let Home toggle the panel
            // (prevents the Home key itself also triggering the toggle while typing)
            if (!_textFieldActive && Input.GetKeyDown(KeyCode.Home))
            {
                _showSettings = !_showSettings;
                if (!_showSettings)
                {
                    _pickerSlot = -1;
                    _textFieldActive = false;
                    // Always restore game input when the panel closes
                    try { var m = EpochInputManager.instance; if (m != null) m.forceDisableInput = false; } catch { }
                }
            }

            // Block game input via Last Epoch's own input manager.
            // EpochInputManager.forceDisableInput is the flag every working LE mod uses
            // — Unity Input.* patches don't intercept the game's custom input system.
            // We block when: (a) movement lock is on while panel is open, OR
            //                (b) a label text field has keyboard focus (stops hotkeys firing while typing)
            bool shouldBlock = BlockMovementInputs || _textFieldActive;
            try
            {
                var mgr = EpochInputManager.instance;
                if (mgr != null) mgr.forceDisableInput = shouldBlock;
            }
            catch { }

            _tick += Time.deltaTime;
            if (_tick < TickRate) return;
            _tick = 0f;
            lock (_lock)
            {
                foreach (var s in Slots)
                {
                    if (s.Icon == null && s.Source != null)        s.Icon        = s.Source.icon?.sprite;
                    if (s.CooldownBar == null && s.Source != null) s.CooldownBar = s.Source.cooldownBar;
                    if (s.CooldownBar != null)
                    {
                        s.Fill = s.CooldownBar.fillAmount;
                        if (s.Source != null) s.OnCooldown = s.Source.cooldownBarActive;
                        if (!s.OnCooldown && s.Fill > 0.01f)  s.OnCooldown = true;
                        if (s.OnCooldown  && s.Fill < 0.005f) s.OnCooldown = false;
                    }
                    if (s.GameBoundKey == null && s.Source != null)
                        s.GameBoundKey = HotkeyReader.TryRead(s.Source);
                }
            }
        }

        // ── OnGUI ─────────────────────────────────────────────────
        public override void OnGUI()
        {
            if (!_stylesReady) BuildStyles();
            DrawOverheadIcons();
            if (_showSettings)
            {
                DrawSettings();
                // Picker is drawn after (on top of) the main panel
                if (_pickerSlot >= 0)
                {
                    float sc   = Mathf.Clamp(PrefMenuScale.Value, 0.7f, 2.0f);
                    int   effMI = ButtonLabels.GetModeIndex(PrefInputMode.Value, PrefCtrlLayout.Value);
                    if (effMI == 0) _pickerSlot = -1;
                    else DrawPickerPanel(sc, effMI);
                }
            }
        }

        static void BuildStyles()
        {
            _stylesReady = true;
            _lbl  = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            _lbl.normal.textColor = Color.white;
            _bold = new GUIStyle(_lbl)  { fontStyle = FontStyle.Bold };
            _left = new GUIStyle(_lbl)  { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            _left.normal.textColor = Color.white;
            _small = new GUIStyle(_lbl) { fontSize = 10 };
            _small.normal.textColor = new Color(0.75f, 0.75f, 0.80f);
            _sectionHdr = new GUIStyle(_left) { fontStyle = FontStyle.Bold };
            _sectionHdr.normal.textColor = new Color(0.75f, 0.85f, 1f);
            _dimText = new GUIStyle(_left) { fontSize = 11 };
            _dimText.normal.textColor = new Color(0.50f, 0.50f, 0.55f);
            _textField = new GUIStyle(GUI.skin.textField) { fontSize = 11 };
            _textField.normal.textColor = Color.white;
        }

        // ─────────────────────────────────────────────────────────
        //  Floating overhead icons
        // ─────────────────────────────────────────────────────────
        static void DrawOverheadIcons()
        {
            if (Camera.main == null) return;
            List<SlotData> active;
            lock (_lock)
                active = Slots.Where(s => s.TrackEnabled && s.OnCooldown && s.Fill > 0.005f)
                              .OrderBy(s => s.SlotIndex).ToList();
            if (active.Count == 0) return;
            Vector2 anchor = GetPlayerScreenPos(active);
            if (anchor == Vector2.zero) return;

            float sz    = Mathf.Clamp(PrefSize.Value, 32f, 120f);
            float gap   = 8f;
            float alpha = Mathf.Clamp01(PrefAlpha.Value);
            float totalW = active.Count * (sz + gap) - gap;
            float startX = anchor.x + PrefOffsetX.Value - totalW * 0.5f;
            float startY = anchor.y + PrefOffsetY.Value - sz * 0.5f;
            for (int i = 0; i < active.Count; i++)
                DrawIcon(new Rect(startX + i * (sz + gap), startY, sz, sz), active[i], alpha);
        }

        static Vector2 GetPlayerScreenPos(List<SlotData> slots)
        {
            foreach (var s in slots)
            {
                try
                {
                    if (s.Source?.player != null)
                    {
                        var sp = Camera.main.WorldToScreenPoint(s.Source.player.transform.position);
                        if (sp.z > 0f) return new Vector2(sp.x, Screen.height - sp.y);
                    }
                }
                catch { }
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.65f);
        }

        static void DrawIcon(Rect r, SlotData s, float alpha)
        {
            string label = GetEffectiveLabel(s.SlotIndex, s);
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.55f);
            GUI.DrawTexture(Pad(r, 3), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            if (s.Icon != null && s.Icon.texture != null) DrawSprite(r, s.Icon);
            else { GUI.color = new Color(0.30f, 0.30f, 0.35f, alpha); GUI.DrawTexture(r, Texture2D.whiteTexture); }
            if (s.Fill > 0.005f)
            {
                GUI.color = new Color(0f, 0f, 0f, alpha * 0.70f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * s.Fill), Texture2D.whiteTexture);
            }
            bool nr = s.Fill < 0.10f;
            DrawBorder(r, nr ? new Color(0.25f, 1f, 0.35f, alpha) : new Color(1f, 0.55f, 0.1f, alpha * 0.85f), 2);
            // Two-line display: split on first space so "Flame Ward" → "Flame\nWard"
            int   spIdx     = label.IndexOf(' ');
            bool  twoLine   = spIdx > 0 && spIdx < label.Length - 1;
            string dispLbl  = twoLine
                ? label.Substring(0, spIdx) + "\n" + label.Substring(spIdx + 1)
                : label;
            float lh = twoLine
                ? Mathf.Max(28f, r.height * 0.42f)
                : Mathf.Max(15f, r.height * 0.23f);
            Rect lr = new(r.x, r.yMax - lh, r.width, lh);
            GUI.color = new Color(0f, 0f, 0f, alpha * 0.80f); GUI.DrawTexture(lr, Texture2D.whiteTexture);
            GUI.color = nr ? new Color(0.3f, 1f, 0.5f, alpha) : new Color(1f, 0.92f, 0.55f, alpha);
            int  fSize   = Mathf.Max(8, Mathf.RoundToInt(r.height * (twoLine ? 0.13f : 0.20f)));
            var  lblSt   = new GUIStyle(_lbl) { fontSize = fSize, wordWrap = false };
            lblSt.normal.textColor = GUI.color;
            GUI.Label(lr, dispLbl, lblSt);
            GUI.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────
        //  Settings panel
        // ─────────────────────────────────────────────────────────
        static void DrawSettings()
        {
            HandleDrag();
            float sc  = Mathf.Clamp(PrefMenuScale.Value, 0.7f, 2.0f);
            float w   = 380f * sc;
            float hdr = 26f  * sc;
            int   effMI      = ButtonLabels.GetModeIndex(PrefInputMode.Value, PrefCtrlLayout.Value);
            string editingFor = ModeDispName[effMI];

            float inputSect = 20f * sc + 28f * sc + (PrefInputMode.Value == 0 ? 48f * sc : 0f);
            float sliders   = 5 * 36f * sc;
            float lockRow   = 28f * sc;   // movement lock toggle row
            float sep       = 10f * sc;
            int slotCount; lock (_lock) slotCount = Slots.Count;
            float slotHdr   = 22f * sc;
            float slotListH = Mathf.Min(slotCount * 46f * sc + 8f, 320f * sc);
            float total = hdr + sep + inputSect + sep + sliders + lockRow + sep + slotHdr + slotListH + 10f;

            _settRect.width  = w;  _settRect.height = total;
            _settRect.x = Mathf.Clamp(_settRect.x, 0, Screen.width  - w);
            _settRect.y = Mathf.Clamp(_settRect.y, 0, Screen.height - total);

            GUI.color = new Color(0.06f, 0.06f, 0.10f, 0.96f);
            GUI.DrawTexture(_settRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.13f, 0.42f, 0.78f, 1f);
            GUI.DrawTexture(new Rect(_settRect.x, _settRect.y, w, hdr), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var sBold  = S(_bold,       13, sc);
            var sSmall = S(_small,      10, sc, TextAnchor.MiddleRight);
            var sLeft  = S(_left,       12, sc);
            var sSHdr  = S(_sectionHdr, 12, sc);
            var sDim   = S(_dimText,    10, sc);
            var sTF    = new GUIStyle(_textField) { fontSize = Mathf.RoundToInt(11 * sc) };

            GUI.Label(new Rect(_settRect.x + 8, _settRect.y + 3, w - 80, hdr - 4), "◈ Cooldown Tracker  v4.4", sBold);
            GUI.Label(new Rect(_settRect.x, _settRect.y + 3, w - 6, hdr - 4), "[Home] close", sSmall);

            float y  = _settRect.y + hdr + sep;
            float lx = _settRect.x + 10f;
            float lw = w - 20f;

            // ── INPUT MODE ────────────────────────────────────────
            GUI.color = new Color(0.75f, 0.85f, 1f);
            GUI.Label(new Rect(lx, y, lw, 18 * sc), "INPUT MODE", sSHdr);
            y += 20f * sc;
            string[] mLbl = { "Auto", "Keyboard", "Xbox", "PS5" };
            Color[]  mCol = { new Color(0.20f,0.60f,0.28f,1f), new Color(0.25f,0.45f,0.70f,1f),
                               new Color(0.65f,0.38f,0.10f,1f), new Color(0.45f,0.15f,0.60f,1f) };
            float bw = (lw - 3f * sc) / 4f;
            for (int i = 0; i < 4; i++)
            {
                bool act = PrefInputMode.Value == i;
                var r = new Rect(lx + i * (bw + sc), y, bw, 24f * sc);
                GUI.color = act ? mCol[i] : new Color(0.14f, 0.14f, 0.18f, 0.95f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = act ? Color.white : new Color(0.50f, 0.50f, 0.55f);
                GUI.Label(r, mLbl[i], S(_lbl, 11, sc));
                GUI.color = Color.white;
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { PrefInputMode.Value = i; _pickerSlot = -1; }
            }
            y += 28f * sc;

            if (PrefInputMode.Value == 0)
            {
                string stTxt; Color stCol;
                if (InputTracker.IsControllerActive)
                { stTxt = $"● Controller detected  ({(InputTracker.DetectedLayout == CtrlLayout.PlayStation ? "PS5" : "Xbox")})"; stCol = new Color(0.40f,1.00f,0.50f); }
                else
                { stTxt = "● Keyboard / Mouse"; stCol = new Color(0.55f,0.80f,1.00f); }
                GUI.color = stCol;
                GUI.Label(new Rect(lx, y, lw, 18 * sc), stTxt, S(_left, 11, sc));
                y += 20f * sc;

                float labW = 108f * sc;
                GUI.color = new Color(0.60f, 0.60f, 0.65f);
                GUI.Label(new Rect(lx, y, labW, 20 * sc), "Layout override:", S(_left, 10, sc));
                string[] lo = { "Auto", "Xbox", "PS5" };
                Color[]  lc = { new Color(0.22f,0.22f,0.28f,1f), new Color(0.65f,0.38f,0.10f,1f), new Color(0.45f,0.15f,0.60f,1f) };
                float lbw = (lw - labW - 2f * sc) / 3f;
                for (int i = 0; i < 3; i++)
                {
                    bool act = PrefCtrlLayout.Value == i;
                    var lr = new Rect(lx + labW + i * (lbw + sc), y, lbw, 20f * sc);
                    GUI.color = act ? lc[i] : new Color(0.14f, 0.14f, 0.18f, 0.95f);
                    GUI.DrawTexture(lr, Texture2D.whiteTexture);
                    GUI.color = act ? Color.white : new Color(0.50f, 0.50f, 0.55f);
                    GUI.Label(lr, lo[i], S(_lbl, 10, sc));
                    GUI.color = Color.white;
                    if (GUI.Button(lr, GUIContent.none, GUIStyle.none)) { PrefCtrlLayout.Value = i; _pickerSlot = -1; }
                }
                y += 28f * sc;
            }

            Sep(lx, ref y, lw, sc);

            // ── SLIDERS ───────────────────────────────────────────
            y = SliderRow(lx, y, lw, sc, sLeft, "Icon Opacity",            ref PrefAlpha,     0.05f,  1f,   "F2");
            y = SliderRow(lx, y, lw, sc, sLeft, "Icon Size (px)",          ref PrefSize,      32f,  120f,   "F0");
            y = SliderRow(lx, y, lw, sc, sLeft, "Offset X (← / →)",       ref PrefOffsetX, -500f,  500f,   "F0");
            y = SliderRow(lx, y, lw, sc, sLeft, "Offset Y (↑ neg = up)",  ref PrefOffsetY, -600f,  200f,   "F0");
            y = SliderRow(lx, y, lw, sc, sLeft, "Menu Size",               ref PrefMenuScale, 0.7f,   2.0f, "F1");

            // ── MOVEMENT LOCK ─────────────────────────────────────
            {
                float togSz  = 22f * sc;
                float rowH2  = 24f * sc;
                float midY   = y + (rowH2 - togSz) * 0.5f;
                bool  locked = PrefLockInput.Value;
                // Coloured badge behind the toggle label
                Color lkCol = locked
                    ? new Color(0.75f, 0.28f, 0.28f, 1f)   // red  = active
                    : new Color(0.15f, 0.15f, 0.20f, 0.90f); // dark = off
                GUI.color = lkCol;
                GUI.DrawTexture(new Rect(lx, y, lw, rowH2), Texture2D.whiteTexture);
                GUI.color = Color.white;
                string lockTxt = locked
                    ? "🔒 Block movement while menu is open  (ON)"
                    : "🔓 Block movement while menu is open  (OFF)";
                GUI.Label(new Rect(lx + 4, y, lw - togSz - 8, rowH2), lockTxt, S(_left, 10, sc));
                bool newLock = GUI.Toggle(
                    new Rect(lx + lw - togSz - 2, midY, togSz, togSz),
                    locked, "");
                if (newLock != locked) PrefLockInput.Value = newLock;
                y += rowH2 + 4f * sc;
            }

            Sep(lx, ref y, lw, sc);

            // ── SKILLS header ─────────────────────────────────────
            GUI.color = new Color(0.75f, 0.85f, 1f);
            GUI.Label(new Rect(lx, y, lw * 0.40f, 20 * sc), "SKILLS", sSHdr);

            Color badgeCol = effMI == 0 ? new Color(0.25f,0.45f,0.70f,1f)
                           : effMI == 1 ? new Color(0.65f,0.38f,0.10f,1f)
                                        : new Color(0.45f,0.15f,0.60f,1f);
            float badgeW = 75f * sc;
            Rect  badgeR = new(_settRect.x + w - 10 - badgeW, y + 1, badgeW, 18 * sc);
            GUI.color = badgeCol; GUI.DrawTexture(badgeR, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(badgeR, $"editing: {editingFor}", S(_small, 9, sc));

            // Column sub-header
            float iSz   = 30f * sc;
            float lblCW = 46f * sc;
            float tfSt  = iSz + lblCW + 10f * sc;     // text field starts here (within scroll)
            float togW  = 22f * sc;
            bool  hasPicker = effMI >= 1;
            float picBW = hasPicker ? 24f * sc : 0f;  // picker button width

            GUI.color = new Color(0.50f, 0.50f, 0.55f);
            GUI.Label(new Rect(lx + tfSt, y, lw - tfSt - picBW - togW - 12f, 18 * sc),
                      hasPicker
                          ? $"Custom Label  ({editingFor} — use [▼] to pick)"
                          : "Custom Label  (empty = auto-default)",
                      S(_small, 9, sc, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(lx + lw - togW - 4, y, togW + 4, 18 * sc),
                      "On", S(_small, 9, sc, TextAnchor.MiddleCenter));
            y += 22f * sc;

            // ── Scrollable slot list ───────────────────────────────
            List<SlotData> snap;
            lock (_lock) snap = Slots.ToList();

            if (snap.Count == 0)
            {
                GUI.color = new Color(0.50f, 0.50f, 0.55f);
                GUI.Label(new Rect(lx, y, lw, 24 * sc), "No slots found — load into a zone first.", sDim);
                GUI.color = Color.white; return;
            }

            float scrollH = _settRect.yMax - y - 6f;
            float rowH    = 46f * sc;
            var   vRect   = new Rect(0, 0, lw - 16f, snap.Count * rowH + 4f);
            _scroll = GUI.BeginScrollView(new Rect(lx, y, lw, scrollH), _scroll, vRect);
            float vw = vRect.width;
            float ry = 2f;

            foreach (var s in snap)
            {
                int idx = s.SlotIndex;

                // Icon
                if (s.Icon != null) { GUI.color = Color.white; DrawSprite(new Rect(2, ry + 4, iSz, iSz), s.Icon); }
                else { GUI.color = new Color(0.20f, 0.20f, 0.24f); GUI.DrawTexture(new Rect(2, ry + 4, iSz, iSz), Texture2D.whiteTexture); }
                if (s.OnCooldown && s.Fill > 0f)
                {
                    GUI.color = new Color(0.20f, 0.80f, 1f, 0.70f);
                    GUI.DrawTexture(new Rect(2, ry + 4 + iSz, iSz * (1f - s.Fill), 2.5f * sc), Texture2D.whiteTexture);
                }

                // Effective label
                float lbX = iSz + 8;
                string eff = GetEffectiveLabel(idx, s);
                var lbSt = S(_bold, 12, sc); lbSt.normal.textColor = s.OnCooldown ? new Color(1f,0.65f,0.15f) : Color.white;
                GUI.color = Color.white;
                GUI.Label(new Rect(lbX, ry + 2, lblCW, rowH * 0.5f), eff, lbSt);
                if (s.OnCooldown)
                {
                    var pSt = S(_left, 10, sc); pSt.normal.textColor = new Color(0.9f, 0.55f, 0.55f);
                    GUI.Label(new Rect(lbX, ry + rowH * 0.5f, lblCW, rowH * 0.5f - 2), $"{(int)(s.Fill * 100)}%", pSt);
                }
                if (idx == 6)
                {
                    var evSt = S(_small, 8, sc); evSt.normal.textColor = new Color(0.55f, 0.75f, 0.95f);
                    GUI.Label(new Rect(lbX, ry + rowH - 13f * sc, lblCW, 13f * sc), "evade", evSt);
                }
                string curCustom = SafeCustom(effMI, idx);
                if (!string.IsNullOrEmpty(s.GameBoundKey) && string.IsNullOrEmpty(curCustom) && effMI == 0)
                {
                    var hSt = S(_small, 9, sc); hSt.normal.textColor = new Color(0.35f,0.85f,0.45f,0.85f);
                    GUI.Label(new Rect(lbX, ry + rowH - 13f * sc, lblCW, 13f * sc), $"↑{s.GameBoundKey}", hSt);
                }

                // ── Text field (per-mode) ─────────────────────────
                float tfX = iSz + lblCW + 10f * sc;
                // Width leaves room for picker button + toggle
                float tfW = vw - tfX - (hasPicker ? picBW + 3f * sc : 0f) - togW - 8f * sc;
                float tfY = ry + (rowH - 22f * sc) * 0.5f;

                if (string.IsNullOrEmpty(curCustom))
                {
                    var ph = S(_small, 10, sc); ph.normal.textColor = new Color(0.35f, 0.35f, 0.40f);
                    GUI.Label(new Rect(tfX + 3, tfY + 1, tfW - 4, 20f * sc), "auto", ph);
                }
                GUI.color = Color.white;
                string nxt = GUI.TextField(new Rect(tfX, tfY, tfW, 22f * sc), curCustom, 20, sTF);
                if (nxt != curCustom) SetCustom(effMI, idx, nxt);

                if (!string.IsNullOrEmpty(curCustom))
                {
                    var clR = new Rect(tfX + tfW - 18f * sc, tfY + 1, 17f * sc, 20f * sc);
                    GUI.color = new Color(0.80f, 0.28f, 0.28f, 0.90f); GUI.DrawTexture(clR, Texture2D.whiteTexture);
                    GUI.color = Color.white; GUI.Label(clR, "✕", S(_lbl, 10, sc));
                    if (GUI.Button(clR, GUIContent.none, GUIStyle.none)) { SetCustom(effMI, idx, ""); if (_pickerSlot == idx) _pickerSlot = -1; }
                }

                // ── [▼] Picker button (Xbox / PS5 only) ───────────
                if (hasPicker)
                {
                    bool picOpen = _pickerSlot == idx;
                    var  picR   = new Rect(tfX + tfW + 3f * sc, tfY, picBW, 22f * sc);
                    GUI.color = picOpen ? badgeCol : new Color(0.20f, 0.20f, 0.26f, 0.95f);
                    GUI.DrawTexture(picR, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(picR, "▼", S(_lbl, 10, sc));
                    if (GUI.Button(picR, GUIContent.none, GUIStyle.none))
                        _pickerSlot = picOpen ? -1 : idx;
                }

                // ── Toggle ────────────────────────────────────────
                GUI.color = Color.white;
                s.TrackEnabled = GUI.Toggle(
                    new Rect(vw - togW - 1, ry + rowH * 0.5f - 11f * sc, togW, togW),
                    s.TrackEnabled, "");

                ry += rowH;
            }
            // Track whether any text field has keyboard focus so we can suppress game hotkeys
            _textFieldActive = _showSettings && GUIUtility.keyboardControl != 0;
            GUI.EndScrollView();
            GUI.color = Color.white;
        }

        // ─────────────────────────────────────────────────────────
        //  Button Picker Panel (drawn outside the scroll view)
        // ─────────────────────────────────────────────────────────
        static void DrawPickerPanel(float sc, int effMI)
        {
            // Panel dimensions
            float pw  = 228f * sc;
            float ph  = 248f * sc;   // enough for 1 face row (34px) + 5 rows (28px) + header + padding

            // Position: prefer to the right of the settings panel
            float px = _settRect.xMax + 10f;
            if (px + pw > Screen.width) px = _settRect.x - pw - 10f;
            if (px < 0) px = _settRect.x + (_settRect.width - pw) * 0.5f;
            float py = Mathf.Clamp(_settRect.y, 0, Screen.height - ph);

            // Background
            GUI.color = new Color(0.06f, 0.06f, 0.10f, 0.97f);
            GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
            // Border
            DrawBorder(new Rect(px, py, pw, ph), new Color(0.30f, 0.30f, 0.40f), 1);

            // Header
            Color hCol = effMI == 1
                ? new Color(0.60f, 0.34f, 0.08f, 1f)   // Xbox – dark orange
                : new Color(0.40f, 0.12f, 0.55f, 1f);   // PS5  – dark purple
            GUI.color = hCol;
            GUI.DrawTexture(new Rect(px, py, pw, 26f * sc), Texture2D.whiteTexture);
            GUI.color = Color.white;
            string modeName = effMI == 1 ? "Xbox" : "PS5";
            GUI.Label(new Rect(px + 6, py + 4, pw - 30, 18 * sc),
                      $"{modeName} buttons  —  slot {_pickerSlot}", S(_bold, 10, sc));

            // Close button
            var xR = new Rect(px + pw - 22f * sc, py + 3, 20f * sc, 20f * sc);
            GUI.color = new Color(0.75f, 0.22f, 0.22f, 1f); GUI.DrawTexture(xR, Texture2D.whiteTexture);
            GUI.color = Color.white; GUI.Label(xR, "✕", S(_lbl, 10, sc));
            if (GUI.Button(xR, GUIContent.none, GUIStyle.none)) { _pickerSlot = -1; return; }

            float y = py + 30f * sc;

            if (effMI == 2) // ── PS5 ──────────────────────────────
            {
                y = PickerGroup(px, y, pw, sc, "Face",    PS5Face,               PS5FaceC, 34);
                y = PickerGroup(px, y, pw, sc, "Bumper",  new[]{"L1","R1"},      null,      28);
                y = PickerGroup(px, y, pw, sc, "Trigger", new[]{"L2","R2"},      null,      28);
                y = PickerGroup(px, y, pw, sc, "Stick",   new[]{"L3","R3"},      null,      28);
                y = PickerGroup(px, y, pw, sc, "D-Pad",   new[]{"↑","↓","←","→"}, null,   28);
                y = PickerGroup(px, y, pw, sc, "Other",   new[]{"Opt","Tpad"},   null,      28);
            }
            else             // ── Xbox ─────────────────────────────
            {
                y = PickerGroup(px, y, pw, sc, "Face",    XboxFace,              XboxFaceC, 34);
                y = PickerGroup(px, y, pw, sc, "Bumper",  new[]{"LB","RB"},      null,      28);
                y = PickerGroup(px, y, pw, sc, "Trigger", new[]{"LT","RT"},      null,      28);
                y = PickerGroup(px, y, pw, sc, "Stick",   new[]{"LS","RS"},      null,      28);
                y = PickerGroup(px, y, pw, sc, "D-Pad",   new[]{"↑","↓","←","→"}, null,   28);
                y = PickerGroup(px, y, pw, sc, "Other",   new[]{"Start","Back"}, null,      28);
            }
        }

        // Draws one row of picker buttons; returns next Y
        static float PickerGroup(float px, float y, float pw, float sc,
            string groupName, string[] syms, Color[] colors, float btnSz)
        {
            float btnH   = btnSz * sc;
            float gap    = 4f * sc;
            float labW   = 54f * sc;
            float innerX = px + 6f + labW;

            // Group label
            GUI.color = new Color(0.60f, 0.60f, 0.65f);
            GUI.Label(new Rect(px + 6, y, labW, btnH), groupName + ":", S(_left, 9, sc, TextAnchor.MiddleLeft));

            // Buttons
            for (int i = 0; i < syms.Length; i++)
            {
                Color c = (colors != null && i < colors.Length)
                    ? colors[i]
                    : new Color(0.20f, 0.20f, 0.26f, 0.95f);
                var r = new Rect(innerX + i * (btnH + gap), y, btnH, btnH);
                GUI.color = c;
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                // Subtle highlight border
                GUI.color = new Color(1f, 1f, 1f, 0.18f);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1), Texture2D.whiteTexture);
                // Label — smaller font for longer strings
                GUI.color = Color.white;
                int fs = syms[i].Length <= 2 ? 12 : syms[i].Length <= 4 ? 9 : 7;
                GUI.Label(r, syms[i], S(_lbl, fs, sc));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    int mi = ButtonLabels.GetModeIndex(PrefInputMode.Value, PrefCtrlLayout.Value);
                    SetCustom(mi, _pickerSlot, syms[i]);
                    _pickerSlot = -1;
                }
            }
            GUI.color = Color.white;
            return y + btnH + gap;
        }

        // ─────────────────────────────────────────────────────────
        //  Drawing helpers
        // ─────────────────────────────────────────────────────────
        static GUIStyle S(GUIStyle src, int baseSize, float sc, TextAnchor? anchor = null)
        {
            var st = new GUIStyle(src) { fontSize = Mathf.RoundToInt(baseSize * sc) };
            if (anchor.HasValue) st.alignment = anchor.Value;
            return st;
        }
        static void Sep(float lx, ref float y, float lw, float sc)
        {
            GUI.color = new Color(0.28f, 0.28f, 0.38f, 0.90f);
            GUI.DrawTexture(new Rect(lx, y, lw, 1), Texture2D.whiteTexture);
            y += 9f * sc; GUI.color = Color.white;
        }
        static float SliderRow(float x, float y, float w, float sc, GUIStyle lSt,
            string label, ref MelonPreferences_Entry<float> pref, float lo, float hi, string fmt)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, 18 * sc), $"{label}: {pref.Value.ToString(fmt)}", lSt);
            y += 18f * sc;
            pref.Value = GUI.HorizontalSlider(new Rect(x, y, w, 14 * sc), pref.Value, lo, hi);
            y += 18f * sc;
            return y;
        }
        static void HandleDrag()
        {
            var ev = Event.current; if (ev == null) return;
            float sc = Mathf.Clamp(PrefMenuScale.Value, 0.7f, 2.0f);
            var hR = new Rect(_settRect.x, _settRect.y, _settRect.width, 26f * sc);
            switch (ev.type)
            {
                case EventType.MouseDown when hR.Contains(ev.mousePosition):
                    _dragging = true; _dragOff = ev.mousePosition - new Vector2(_settRect.x, _settRect.y); ev.Use(); break;
                case EventType.MouseDrag when _dragging:
                    _settRect.x = ev.mousePosition.x - _dragOff.x; _settRect.y = ev.mousePosition.y - _dragOff.y; ev.Use(); break;
                case EventType.MouseUp: _dragging = false; break;
            }
        }
        static void DrawSprite(Rect r, Sprite s)
        {
            var tex = s.texture; var sr = s.rect;
            GUI.DrawTextureWithTexCoords(r, tex,
                new Rect(sr.x / tex.width, sr.y / tex.height, sr.width / tex.width, sr.height / tex.height));
        }
        static void DrawBorder(Rect r, Color c, float bw)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x,        r.y,         r.width, bw),        Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,        r.yMax - bw, r.width, bw),        Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,        r.y,         bw,      r.height),  Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - bw, r.y,        bw,      r.height),  Texture2D.whiteTexture);
        }
        static Rect Pad(Rect r, float d) => new(r.x - d, r.y - d, r.width + d * 2, r.height + d * 2);
    }

    // ── Harmony patches ───────────────────────────────────────────

    [HarmonyPatch(typeof(AbilityBarIcon), "Awake")]
    internal static class Patch_Awake
    {
        [HarmonyPostfix]
        public static void Postfix(AbilityBarIcon __instance) =>
            CooldownTrackerMod.RegisterSlot(__instance);
    }
    [HarmonyPatch(typeof(AbilityBarIcon), "Start")]
    internal static class Patch_Start
    {
        [HarmonyPostfix]
        public static void Postfix(AbilityBarIcon __instance) =>
            CooldownTrackerMod.RegisterSlot(__instance);
    }
    [HarmonyPatch(typeof(AbilityBarIcon), "activateCooldownBar")]
    internal static class Patch_Activate
    {
        [HarmonyPostfix]
        public static void Postfix(AbilityBarIcon __instance) =>
            CooldownTrackerMod.OnActivate(__instance);
    }
    [HarmonyPatch(typeof(AbilityBarIcon), "deactivateCooldownBar")]
    internal static class Patch_Deactivate
    {
        [HarmonyPostfix]
        public static void Postfix(AbilityBarIcon __instance) =>
            CooldownTrackerMod.OnDeactivate(__instance);
    }

    // ── Keyboard-suppression patches (block game hotkeys while typing in a label field) ──

    [HarmonyPatch(typeof(Input), "GetKeyDown", typeof(KeyCode))]
    internal static class Patch_InputGetKeyDown
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!CooldownTrackerMod._textFieldActive) return true;
            __result = false; return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKey", typeof(KeyCode))]
    internal static class Patch_InputGetKey
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!CooldownTrackerMod._textFieldActive) return true;
            __result = false; return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetKeyUp", typeof(KeyCode))]
    internal static class Patch_InputGetKeyUp
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!CooldownTrackerMod._textFieldActive) return true;
            __result = false; return false;
        }
    }

    // ── Movement-lock patches (block movement while settings panel is open) ──

    [HarmonyPatch(typeof(Input), "GetAxis")]
    internal static class Patch_InputGetAxis
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            if (!CooldownTrackerMod.BlockMovementInputs) return true;
            __result = 0f; return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetAxisRaw")]
    internal static class Patch_InputGetAxisRaw
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            if (!CooldownTrackerMod.BlockMovementInputs) return true;
            __result = 0f; return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButton")]
    internal static class Patch_InputGetMouseButton
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!CooldownTrackerMod.BlockMovementInputs) return true;
            __result = false; return false;
        }
    }

    [HarmonyPatch(typeof(Input), "GetMouseButtonDown")]
    internal static class Patch_InputGetMouseButtonDown
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (!CooldownTrackerMod.BlockMovementInputs) return true;
            __result = false; return false;
        }
    }
}
