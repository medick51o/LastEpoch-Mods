using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using medick_DeathCounter.Core;
using medick_DeathCounter.Game;
using UnityEngine;

namespace medick_DeathCounter.UI
{
    // Native UI dispatch and gameplay input are separate from IMGUI Event.Use.
    // Suppress their input dispatch only; never pause simulation or mutate a
    // shared forceDisableInput flag held by another screen/mod.
    internal static class InputBlocker
    {
        static readonly MenuInputGate Gate = new();
        static readonly CounterPressLatch Press = new();
        static readonly HashSet<string> Installed = new();
        static HarmonyLib.Harmony _harmony;
        static float _nextInstall;
        static PropertyInfo _cursor;
        static bool _revealedCursor, _cursorWarned;
        static void CursorFocus(bool ownsInput)
        {
            try
            {
                _cursor ??= Refl.FindType("UnityEngine.Cursor")?.GetProperty("visible", BindingFlags.Public | BindingFlags.Static);
                if (_cursor == null) return;
                if (ownsInput && !_revealedCursor && _cursor.GetValue(null) is false)
                { _cursor.SetValue(null, true); _revealedCursor = true; }
                else if (!ownsInput && _revealedCursor)
                {
                    // Let keyboard/mouse detection retain its cursor. Only
                    // restore a controller cursor that this menu revealed.
                    if (Refl.Static("Il2Cpp.EpochInputManager", "get_IsControllerActive") is true && _cursor.GetValue(null) is true)
                        _cursor.SetValue(null, false);
                    _revealedCursor = false;
                }
            }
            catch (Exception ex) { if (!_cursorWarned) { Dbg.Log("menu cursor: " + ex.Message); _cursorWarned = true; } _revealedCursor = false; }
        }
        public static bool GameplayReady { get; private set; }
        public static bool NativeUiReady { get; private set; }
        public static string Status => GameplayReady && NativeUiReady
            ? "Menu focus hooks ready. Native click-through testing is still required."
            : "Menu input protection is incomplete. Close the log before using game controls.";
        public static void Install(HarmonyLib.Harmony harmony) { _harmony = harmony; TryInstall(); }
        static void TryInstall()
        {
            if (_harmony == null) return;
            _nextInstall = Time.unscaledTime + 5f;
            Patch("Il2Cpp.EpochInputManager", "Update", true);
            Patch("Il2CppRewired.Integration.UnityUI.RewiredStandaloneInputModule", "Process", false);
            Patch("UnityEngine.EventSystems.StandaloneInputModule", "Process", false);
            Patch("UnityEngine.InputSystem.UI.InputSystemUIInputModule", "Process", false);
        }
        static void Patch(string typeName, string methodName, bool gameplay)
        {
            string key = typeName + "." + methodName;
            if (Installed.Contains(key)) return;
            var type = Refl.FindType(typeName);
            var method = type?.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (method == null || method.ReturnType != typeof(void)) return;
            try
            {
                _harmony.Patch(method, prefix: new HarmonyMethod(typeof(InputBlocker).GetMethod(nameof(AllowNativeInput),
                    BindingFlags.Static | BindingFlags.NonPublic)));
                Installed.Add(key);
                if (gameplay) GameplayReady = true;
                else if (typeName == "Il2CppRewired.Integration.UnityUI.RewiredStandaloneInputModule") NativeUiReady = true;
                Dbg.Log("menu focus hook: " + key);
            }
            catch (Exception ex)
            {
                // Log once per target, without a silent permanent type miss.
                Installed.Add(key);
                Dbg.Log("menu focus hook unavailable: " + key + ": " + ex.Message);
            }
        }
        static bool MouseDown(int button) => Input.GetKeyDown(button == 0 ? KeyCode.Mouse0 : button == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2);
        static bool MouseButton(int button) => Input.GetKey(button == 0 ? KeyCode.Mouse0 : button == 1 ? KeyCode.Mouse1 : KeyCode.Mouse2);
        static bool CounterPress() => Press.Update(CounterHud.PointerOver(),
            MouseDown(0), MouseDown(1), MouseDown(2), MouseButton(0), MouseButton(1), MouseButton(2));
        static bool AllowNativeInput()
        {
            // Native Update/Process may run before Melon's OnUpdate. Protect
            // the opening frame as well as the steady open state. Hovering
            // the counter does not pause gameplay.
            bool opening = PlayerProbe.HasPlayer && Input.GetKeyDown(Prefs.PanelKeyCode)
                && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)
                && !DeathPanel.Open;
            bool press = CounterPress();
            return !MenuInputPolicy.BlockGameplay(Gate.Blocked, DeathPanel.Open, CounterHud.Dragging, opening, press, press);
        }
        public static void Update()
        {
            if ((!GameplayReady || !NativeUiReady) && Time.unscaledTime >= _nextInstall) TryInstall();
            bool press = CounterPress();
            CursorFocus(DeathPanel.Open || CounterHud.Moving);
            Gate.Update(DeathPanel.Open || CounterHud.Dragging || press,
                press || Input.GetKey(Prefs.PanelKeyCode) || Input.GetKey(KeyCode.Escape));
        }
        public static void TakeFocus() { Gate.Update(true, false); CursorFocus(true); }
        public static void Restore() { Gate.Clear(); Press.Clear(); CursorFocus(false); }
    }
}
