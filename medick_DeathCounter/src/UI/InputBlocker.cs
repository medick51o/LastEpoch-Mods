using System.Reflection;
using medick_DeathCounter.Game;

namespace medick_DeathCounter.UI
{
    // Same technique as Terrible Cooldowns / Zoom: the game's own
    // EpochInputManager.forceDisableInput, re-asserted while wanted, released
    // once and only if this mod set it. Keeps a click on the panel from also
    // walking the character there. Reached by name (see PlayerProbe for why).
    internal static class InputBlocker
    {
        static bool _applied;
        static bool _resolved;
        static PropertyInfo _instance, _flag;

        static object Manager()
        {
            if (!_resolved)
            {
                _resolved = true;
                var t = Refl.FindType("Il2Cpp.EpochInputManager");
                _instance = t?.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                _flag     = t?.GetProperty("forceDisableInput", BindingFlags.Public | BindingFlags.Instance);
                if (_instance == null || _flag == null) Dbg.Log("EpochInputManager not found; clicks on the panel will reach the game");
            }
            return _instance?.GetValue(null);
        }

        public static void Apply(bool want)
        {
            try
            {
                var mgr = Manager();
                if (mgr == null || _flag == null)
                {
                    if (!want) _applied = false;
                    return;
                }
                if (want)
                {
                    if (!(bool)_flag.GetValue(mgr))
                    {
                        _flag.SetValue(mgr, true);
                        _applied = true;
                    }
                }
                else if (_applied)
                {
                    _flag.SetValue(mgr, false);
                    _applied = false;
                }
            }
            catch { }
        }

        public static void Restore() => Apply(false);
    }
}
