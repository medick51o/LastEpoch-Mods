using System;

namespace medick_DeathCounter.Core
{
    // Outermost hit size. Health lost plus ward lost when both reads exist.
    // A ward-only hit is kept. A dodge or a full block (nothing lost) is not.
    public static class HitLoss
    {
        public static float? Amount(float healthBefore, float healthAfter, float wardBefore, float wardAfter, float fallbackAmount)
        {
            bool health = float.IsFinite(healthBefore) && float.IsFinite(healthAfter);
            bool ward = float.IsFinite(wardBefore) && float.IsFinite(wardAfter);
            if (health || ward)
            {
                float hp = health ? Math.Max(0f, healthBefore - healthAfter) : 0f;
                float wd = ward ? Math.Max(0f, wardBefore - wardAfter) : 0f;
                float total = hp + wd;
                return total > 0f ? total : (float?)null;
            }
            return fallbackAmount > 0f ? fallbackAmount : (float?)null;
        }
    }
}
