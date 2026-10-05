using System;

namespace medick_Terrible_Tooltips;

// A failed native cleanup or diagnostic must not stop subsequent steps.
internal static class CleanupStep
{
    internal static void Run(Action step, Action<Exception> warn)
    {
        try { step(); }
        catch (Exception ex)
        {
            try { warn(ex); }
            catch (Exception) { /* Cleanup must also survive logger teardown. */ }
        }
    }
}