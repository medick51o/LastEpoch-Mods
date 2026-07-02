using Il2Cpp;

namespace medick_CooldownTracker
{
    // The one correct way to block input in Last Epoch: the game's own
    // EpochInputManager.forceDisableInput flag. The game routes all input
    // through its own manager, so Harmony patches on UnityEngine.Input never
    // affect it — they only break other mods (v4.x shipped seven of those;
    // v5 ships none).
    //
    // The flag is only written when our desired state changes, so we never
    // stomp another mod that set it for its own reasons.
    internal static class InputBlocker
    {
        static bool _applied;
        static int  _mgrId;   // which EpochInputManager we actually wrote to

        public static void Apply(bool want)
        {
            try
            {
                var mgr = EpochInputManager.instance;
                if (mgr == null)
                {
                    if (!want) _applied = false;   // manager gone = nothing left to restore
                    return;
                }
                // A scene change spawns a fresh manager with the flag reset —
                // "applied" only counts if it was applied to THIS instance.
                // (Andrew's report: panel open across a transition = movement
                // lock silently off until the panel was toggled again.)
                int id = mgr.GetInstanceID();
                if (want == _applied && id == _mgrId) return;
                mgr.forceDisableInput = want;
                _applied = want;
                _mgrId   = id;
            }
            catch { }
        }

        public static void Restore() => Apply(false);
    }
}
