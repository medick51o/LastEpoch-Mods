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
                if (want)
                {
                    // Re-assert while locking: a scene change spawns a fresh
                    // manager with the flag reset (Andrew's report: panel open
                    // across a transition = lock silently off), and a sibling
                    // mod's guard (Terrible Zoom runs the same technique) can
                    // flip the shared flag under us. Trusting our own last
                    // write is how input locks silently die.
                    if (!mgr.forceDisableInput) mgr.forceDisableInput = true;
                    _applied = true;
                }
                else if (_applied)
                {
                    mgr.forceDisableInput = false;   // release once
                    _applied = false;
                }
            }
            catch { }
        }

        public static void Restore() => Apply(false);
    }
}
