using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace medick_DeathCounter.Core
{
    // Commit the pending death only when health still says the player is dead.
    // A late game-counter death can land after respawn, so health above zero
    // does not cancel that path.
    public static class DeathCommitGate
    {
        public static bool CancelBecauseAlive(string detection, float health)
            => detection != "game" && float.IsFinite(health) && health > 0f;
    }

    public readonly struct SignalMerge
    {
        public string Source { get; }
        public bool Rebuild { get; }
        public SignalMerge(string source, bool rebuild) { Source = source; Rebuild = rebuild; }
    }

    // Extra death signals inside the pending window join the same capture.
    // A counter echo does not replace a live capture. A live signal can fill
    // in a counter-only pending death. The first live source and its time stay.
    public static class DeathSignalMerge
    {
        public static SignalMerge Combine(string pending, string incoming)
        {
            if (string.IsNullOrEmpty(pending)) return new SignalMerge(incoming, true);
            if (incoming == "game" && pending != "game") return new SignalMerge(pending, false);
            if (pending == "game" && incoming != "game") return new SignalMerge(incoming, true);
            return new SignalMerge(pending, true);
        }
    }

    // Gameplay input pauses for an open log, a counter drag, or a held click
    // on the counter. Hovering the counter does not pause it.
    public static class MenuInputPolicy
    {
        public static bool BlockGameplay(bool gateBlocked, bool logOpen, bool dragging, bool openingKey, bool pointerOverCounter, bool mouseHeld)
            => gateBlocked || logOpen || dragging || openingKey || (pointerOverCounter && mouseHeld);
    }

    public static class BossNotesFocus
    {
        public static bool ShowBrowser(bool matchedBoss) => !matchedBoss;
    }

    public static class HistoryPages
    {
        public const int Size = 6;
        public static int Count(int rows) => rows <= 0 ? 1 : (rows + Size - 1) / Size;
        public static int Clamp(int page, int rows)
        {
            int last = Count(rows) - 1;
            if (page < 0) return 0;
            return page > last ? last : page;
        }
        // Indexes into an oldest-first list, newest row first.
        public static int[] NewestFirst(int rows, int page)
        {
            page = Clamp(page, rows);
            int start = rows - 1 - page * Size;
            var list = new List<int>();
            for (int i = start; i >= 0 && list.Count < Size; i--) list.Add(i);
            return list.ToArray();
        }
    }

    public static class BoundedCache
    {
        public const int AdviceCap = 48;
        // Dictionary reuses a removed slot, so key order is not insertion order.
        static readonly ConditionalWeakTable<object, object> _order = new();
        public static void Put<TKey, TValue>(Dictionary<TKey, TValue> map, TKey key, TValue value, int cap)
        {
            if (map == null || cap < 1) return;
            if (!_order.TryGetValue(map, out object boxed))
            {
                boxed = new List<TKey>();
                _order.Add(map, boxed);
            }
            var order = (List<TKey>)boxed;
            order.Remove(key);
            map.Remove(key);
            while (map.Count >= cap && order.Count > 0)
            {
                TKey oldest = order[0];
                order.RemoveAt(0);
                map.Remove(oldest);
            }
            order.Add(key);
            map[key] = value;
        }
    }

    // One cached next-step card: same death text, width, and scale.
    public sealed class ViewMemo
    {
        int _a = int.MinValue, _b = int.MinValue, _c = int.MinValue;
        public bool Matches(int a, int b, int c) => _a == a && _b == b && _c == c;
        public void Remember(int a, int b, int c) { _a = a; _b = b; _c = c; }
    }
}
