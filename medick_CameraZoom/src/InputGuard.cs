using Il2Cpp;

namespace medick_CameraZoom
{
    // The one correct way to pause input in Last Epoch: the game's own
    // EpochInputManager.forceDisableInput flag (the Terrible Cooldowns
    // technique). Without it, every slider drag on the settings panel is
    // ALSO a click-to-move command — the game reads its own input path,
    // and IMGUI event consumption means nothing to it.
    //
    // The flag is only written when our desired state changes, so we never
    // stomp another mod using it for its own reasons.
    internal static class InputGuard
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
                    if (!want) _applied = false;   // manager gone = nothing to restore
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
