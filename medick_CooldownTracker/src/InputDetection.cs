using System;
using System.Linq;
using System.Reflection;
using Il2Cpp;
using UnityEngine;
using UnityEngine.UI;

namespace medick_CooldownTracker
{
    internal enum CtrlLayout { Xbox, PlayStation }

    // Watches raw device activity to decide whether the player is currently on
    // keyboard/mouse or a controller, and which controller family it is.
    // Reads UnityEngine.Input directly — safe, because v5 ships no Input patches.
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

                // Before any real input has been seen, presence of a pad decides.
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

    // Default button names per input mode, and resolution of the label that
    // actually gets drawn for a slot (custom > game-bound key > default).
    internal static class ButtonLabels
    {
        // 7 entries: slots 0-5 (skills) + slot 6 (evade/dodge)
        public static readonly string[] Xbox        = { "X",  "Y",  "RB", "LT", "L",  "RT", "B"     };
        public static readonly string[] PlayStation = { "□",  "△",  "R1", "L2", "L3", "R2", "○"     };
        public static readonly string[] Keyboard    = { "Q",  "W",  "E",  "R",  "RMB","T",  "Space" };

        // modeIdx: 0=Keyboard  1=Xbox  2=PlayStation
        public static int GetModeIndex()
        {
            switch (Prefs.InputMode.Value)
            {
                case 1: return 0;
                case 2: return 1;
                case 3: return 2;
                default:
                    if (!InputTracker.IsControllerActive) return 0;
                    CtrlLayout lay =
                        Prefs.CtrlLayout.Value == 1 ? CtrlLayout.Xbox :
                        Prefs.CtrlLayout.Value == 2 ? CtrlLayout.PlayStation :
                        InputTracker.DetectedLayout;
                    return lay == CtrlLayout.PlayStation ? 2 : 1;
            }
        }

        static string[] TableFor(int mi) =>
            mi == 2 ? PlayStation : mi == 1 ? Xbox : Keyboard;

        public static string Resolve(int idx, string gameBound)
        {
            int mi = GetModeIndex();
            string custom = Prefs.CustomLabel(mi, idx);
            if (!string.IsNullOrEmpty(custom)) return custom;
            if (mi == 0 && !string.IsNullOrEmpty(gameBound)) return gameBound;
            var arr = TableFor(mi);
            return (uint)idx < (uint)arr.Length ? arr[idx] : "#" + idx;
        }
    }

    // Best-effort read of the key the game itself shows on an action bar slot.
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
}
