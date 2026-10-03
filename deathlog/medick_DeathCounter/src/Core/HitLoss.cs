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
            float hp = health ? Math.Max(0f, healthBefore - healthAfter) : 0f;
            float wd = ward ? Math.Max(0f, wardBefore - wardAfter) : 0f;
            // Both sides were read and nothing moved: a dodge or a full block.
            if (health && ward) return hp + wd > 0f ? hp + wd : (float?)null;
            // Health did not move and ward could not be read. Keep a positive
            // fallback so a ward-only hit is not dropped.
            if (health && !ward) return hp > 0f ? hp : (fallbackAmount > 0f ? fallbackAmount : (float?)null);
            if (!health && ward) return wd > 0f ? wd : (fallbackAmount > 0f ? fallbackAmount : (float?)null);
            return fallbackAmount > 0f ? fallbackAmount : (float?)null;
        }
    }
}
