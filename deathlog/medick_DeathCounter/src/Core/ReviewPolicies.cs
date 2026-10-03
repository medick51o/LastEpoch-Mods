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

    // A later death signal may fill an empty capture. It must not replace a
    // capture that already has hits or defenses with an empty one. After the
    // live buffers are cleared, the frozen copy is the one to read.
    public readonly struct CaptureChoice
    {
        public bool KeepExisting { get; }
        public bool UseFrozen { get; }
        public CaptureChoice(bool keepExisting, bool useFrozen)
        {
            KeepExisting = keepExisting;
            UseFrozen = useFrozen;
        }
    }

    public static class CaptureRefresh
    {
        public static CaptureChoice Choose(bool fresh, bool liveHas, bool frozenHas, bool existingHas)
        {
            if (liveHas) return new CaptureChoice(false, false);
            if (frozenHas) return new CaptureChoice(false, true);
            if (!fresh && existingHas) return new CaptureChoice(true, false);
            return new CaptureChoice(false, false);
        }
    }

    // A press counts only for the button that went down while the pointer was
    // on the counter. It clears on release, and it does not count a button
    // that was already held on entry or still held after the pointer leaves.
    public sealed class CounterPressLatch
    {
        int _armed;
        public void Clear() => _armed = 0;
        public bool Update(bool pointerOver, bool down0, bool down1, bool down2, bool held0, bool held1, bool held2)
        {
            if (!held0) _armed &= ~1;
            if (!held1) _armed &= ~2;
            if (!held2) _armed &= ~4;
            if (!pointerOver)
            {
                _armed = 0;
                return false;
            }
            if (down0) _armed |= 1;
            if (down1) _armed |= 2;
            if (down2) _armed |= 4;
            return _armed != 0;
        }
    }

    // Expanded boss attacks: killing move first, then a short page.
    public static class BossAttackPages
    {
        public const int Size = 4;
        public static List<string> Order(IReadOnlyList<string> moveIds, string killingMoveId)
        {
            var list = new List<string>();
            if (moveIds == null) return list;
            if (!string.IsNullOrEmpty(killingMoveId))
                foreach (var id in moveIds)
                    if (id == killingMoveId) { list.Add(id); break; }
            foreach (var id in moveIds)
                if (list.Count == 0 || id != list[0]) list.Add(id);
            return list;
        }
        public static int Count(int moves) => moves <= 0 ? 1 : (moves + Size - 1) / Size;
        public static int Clamp(int page, int moves)
        {
            int last = Count(moves) - 1;
            if (page < 0) return 0;
            return page > last ? last : page;
        }
        public static List<string> Page(IReadOnlyList<string> ordered, int page)
        {
            var list = new List<string>();
            if (ordered == null || ordered.Count == 0) return list;
            page = Clamp(page, ordered.Count);
            int start = page * Size;
            for (int i = start; i < ordered.Count && list.Count < Size; i++) list.Add(ordered[i]);
            return list;
        }
    }

    // Measured block heights, keyed by the caller (boss id, page, width).
    public sealed class HeightCache
    {
        readonly Dictionary<string, float> _heights = new(StringComparer.Ordinal);
        public int Count => _heights.Count;
        public bool TryGet(string key, out float height)
        {
            height = 0f;
            return key != null && _heights.TryGetValue(key, out height);
        }
        public void Store(string key, float height)
        {
            if (string.IsNullOrEmpty(key) || !float.IsFinite(height) || height <= 0f) return;
            _heights[key] = height;
        }
    }
}
