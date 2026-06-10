using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace medick_CooldownTracker
{
    internal class SlotData
    {
        public int    SlotIndex;
        public string GameBoundKey;     // null = still probing, "" = gave up
        public Sprite Icon;
        public Image  CooldownBar;
        public float  Fill;
        public bool   OnCooldown;
        public AbilityBarIcon Source;

        // Render cache — rebuilt only when the resolved label changes, so the
        // per-frame draw path allocates nothing.
        public string RawLabel;
        public string DisplayLabel = "";
        public bool   TwoLine;

        public float NextHotkeyRetry;
        public int   HotkeyTries;
    }

    // Thread-safe registry of action-bar slots, fed by the Harmony patches.
    // Slots whose source object has been destroyed (quit to menu, scene torn
    // down) are pruned on the next tick — nothing stale ever reaches the
    // renderer, so no more ghost icons on the login screen.
    internal static class SlotRegistry
    {
        public const int   SlotCount      = 7;   // 0-5 skills, 6 = evade
        const int   MaxHotkeyTries = 6;
        const float HotkeyRetraySec = 2f;

        static readonly List<SlotData> _slots = new();
        static readonly object _lock = new();

        public static int Count { get { lock (_lock) return _slots.Count; } }

        public static void Register(AbilityBarIcon icon)
        {
            if (icon == null) return;
            try
            {
                int idx = icon.abilityNumber;
                var img = icon.icon;
                var s = new SlotData
                {
                    SlotIndex    = idx,
                    GameBoundKey = HotkeyReader.TryRead(icon),
                    Icon         = img != null ? img.sprite : null,
                    CooldownBar  = icon.cooldownBar,
                    OnCooldown   = icon.cooldownBarActive,
                    Source       = icon,
                };
                RefreshLabel(s);
                lock (_lock)
                {
                    _slots.RemoveAll(x => x.SlotIndex == idx);
                    int at = _slots.FindIndex(x => x.SlotIndex > idx);
                    if (at < 0) _slots.Add(s); else _slots.Insert(at, s);
                }
                Dbg.Log($"slot #{idx} registered (key={s.GameBoundKey ?? "?"})");
            }
            catch (Exception ex) { MelonLogger.Warning("RegisterSlot: " + ex.Message); }
        }

        public static void SetCooldown(AbilityBarIcon icon, bool on)
        {
            if (icon == null) return;
            try
            {
                int idx = icon.abilityNumber;
                lock (_lock)
                    foreach (var s in _slots)
                        if (s.SlotIndex == idx)
                        { s.OnCooldown = on; if (!on) s.Fill = 0f; break; }
            }
            catch { }
        }

        // 20 Hz heartbeat: refresh fill state, retry hotkey probe with backoff,
        // rebuild label caches, and prune slots whose Unity objects died.
        public static void Tick(float now)
        {
            lock (_lock)
            {
                for (int i = _slots.Count - 1; i >= 0; i--)
                {
                    var s = _slots[i];
                    bool alive;
                    try { alive = s.Source != null; }   // Unity-overloaded ==, true null for destroyed objects
                    catch { alive = false; }
                    if (!alive) { _slots.RemoveAt(i); continue; }

                    try
                    {
                        if (s.Icon == null)
                        { var img = s.Source.icon; if (img != null) s.Icon = img.sprite; }
                        if (s.CooldownBar == null) s.CooldownBar = s.Source.cooldownBar;

                        if (s.CooldownBar != null)
                        {
                            s.Fill       = s.CooldownBar.fillAmount;
                            s.OnCooldown = s.Source.cooldownBarActive;
                            if (!s.OnCooldown && s.Fill > 0.01f)  s.OnCooldown = true;
                            if (s.OnCooldown  && s.Fill < 0.005f) s.OnCooldown = false;
                        }

                        if (s.GameBoundKey == null && now >= s.NextHotkeyRetry)
                        {
                            var k = HotkeyReader.TryRead(s.Source);
                            if (k != null)                            s.GameBoundKey = k;
                            else if (++s.HotkeyTries >= MaxHotkeyTries) s.GameBoundKey = "";
                            else                                      s.NextHotkeyRetry = now + HotkeyRetraySec;
                        }

                        RefreshLabel(s);
                    }
                    catch { _slots.RemoveAt(i); }   // dead Il2Cpp reference mid-read
                }
            }
        }

        internal static void RefreshLabel(SlotData s)
        {
            string lbl = ButtonLabels.Resolve(s.SlotIndex, s.GameBoundKey);
            if (lbl == s.RawLabel) return;
            s.RawLabel = lbl;
            int sp = lbl.IndexOf(' ');
            s.TwoLine = sp > 0 && sp < lbl.Length - 1;
            s.DisplayLabel = s.TwoLine
                ? lbl.Substring(0, sp) + "\n" + lbl.Substring(sp + 1)
                : lbl;
        }

        public static void SnapshotAll(List<SlotData> buf)
        {
            buf.Clear();
            lock (_lock)
                foreach (var s in _slots) buf.Add(s);
        }

        // Only slots the renderer should draw: enabled, on cooldown, visible fill.
        public static void SnapshotActive(List<SlotData> buf)
        {
            buf.Clear();
            lock (_lock)
                foreach (var s in _slots)
                    if (s.OnCooldown && s.Fill > 0.005f && Prefs.IsSlotEnabled(s.SlotIndex))
                        buf.Add(s);
        }
    }

    internal static class Dbg
    {
        public static void Log(string msg)
        {
            if (Prefs.DebugLog != null && Prefs.DebugLog.Value)
                MelonLogger.Msg("[debug] " + msg);
        }
    }
}
