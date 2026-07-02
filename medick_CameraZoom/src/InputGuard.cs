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
            try
            {
                var mgr = EpochInputManager.instance;
                if (mgr == null)
                {
                    if (!want) _applied = false;   // manager gone = nothing to restore
                    return;
                }
                if (want)
                {
                    // Re-assert while blocking: a scene change spawns a fresh
                    // manager with the flag reset, and a sibling mod's guard
                    // (Terrible Cooldowns runs the same technique) can flip it
                    // under us. Trusting our own last write is how input locks
                    // silently die; one Il2Cpp read per frame is cheap.
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
