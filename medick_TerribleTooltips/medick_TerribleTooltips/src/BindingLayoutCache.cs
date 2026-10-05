namespace medick_Terrible_Tooltips;

// Single-thread confined. T is an immutable, caller-owned snapshot.
// No Unity objects, native invocation, composition, or replay-success contract.
public sealed class BindingLayoutCache<T> where T : class
{
    private sealed class Entry
    {
        public readonly long Epoch;
        public T Snapshot;
        public float HiddenSince = float.NaN;
        public bool Expired;
        public Entry(long epoch) => Epoch = epoch;
    }
    private readonly Dictionary<int, Entry> entries = new();
    private long sequence;

    // Call BEFORE native SetAs*. Replaces the binding and drops old args.
    public long BeginBinding(int instanceId)
    {
        long epoch = checked(sequence + 1); // Fail closed before mutation.
        sequence = epoch;
        entries[instanceId] = new Entry(epoch);
        return epoch;
    }

    // Ordinary recolor-only tooltips need not have a FilterRuleTooltip owner.
    // Capture prefix uses this to initialize a first observed binding.
    public long CaptureCurrentEpoch(int instanceId)
        => entries.TryGetValue(instanceId, out var e)
            ? e.Epoch : BeginBinding(instanceId);

    public bool TryCapture(int instanceId, long epoch, T snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!entries.TryGetValue(instanceId, out var e) || e.Epoch != epoch || e.Expired)
            return false;
        e.Snapshot = snapshot;
        return true;
    }

    // Lookup does not create a binding. Caller supplies its expected epoch.
    public bool TryGet(int instanceId, long epoch, out T snapshot)
    {
        snapshot = null;
        if (!entries.TryGetValue(instanceId, out var e) || e.Epoch != epoch || e.Expired)
            return false;
        snapshot = e.Snapshot;
        return snapshot != null;
    }

    public bool TryGetCurrentEpoch(int instanceId, out long epoch)
    {
        epoch = 0;
        if (!entries.TryGetValue(instanceId, out var e) || e.Expired) return false;
        epoch = e.Epoch;
        return true;
    }

    // Shared hidden grace for all owners/pruners. Expiry is sticky until rebind.
    // No native calls or hidden-panel replay. Invalid clock fails closed.
    public bool Retain(int instanceId, long epoch, bool visible, float now, float grace)
    {
        if (!entries.TryGetValue(instanceId, out var e) || e.Epoch != epoch || e.Expired)
            return false;
        if (!float.IsFinite(now) || !float.IsFinite(grace) || grace <= 0)
        { e.Expired = true; return false; }
        if (!float.IsNaN(e.HiddenSince) && (now < e.HiddenSince || now - e.HiddenSince >= grace))
        { e.Expired = true; return false; }
        if (visible) { e.HiddenSince = float.NaN; return true; }
        if (float.IsNaN(e.HiddenSince)) e.HiddenSince = now;
        if (now < e.HiddenSince || now - e.HiddenSince >= grace)
        { e.Expired = true; return false; }
        return true;
    }

    public bool Forget(int instanceId) => entries.Remove(instanceId);
    // Do not reset sequence: old in-flight captures must fail after reuse.
    public void Clear() => entries.Clear();
}
