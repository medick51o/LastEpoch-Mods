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
            if (want == _applied) return;
            try
            {
                var mgr = EpochInputManager.instance;
                if (mgr == null)
                {
                    if (!want) _applied = false;   // manager gone = nothing left to restore
                    return;
                }
                mgr.forceDisableInput = want;
                _applied = want;
            }
            catch { }
        }

        public static void Restore() => Apply(false);
    }
}
