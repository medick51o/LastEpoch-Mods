using Il2Cpp;

namespace medick_DeathCounter.UI
{
    // Same technique as Terrible Cooldowns / Zoom: the game's own
    // EpochInputManager.forceDisableInput, re-asserted while wanted, released
    // once and only if this mod set it. Keeps a click on the panel from also
    // walking the character there.
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
                    if (!want) _applied = false;
                    return;
                }
                if (want)
                {
                    if (!mgr.forceDisableInput)
                    {
                        mgr.forceDisableInput = true;
                        _applied = true;
                    }
                }
                else if (_applied)
                {
                    mgr.forceDisableInput = false;
                    _applied = false;
                }
            }
            catch { }
        }

        public static void Restore() => Apply(false);
    }
}
