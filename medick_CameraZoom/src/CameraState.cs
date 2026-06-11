using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace medick_CameraZoom
{
    // Owns everything written to or read from the game's CameraManager.
    //
    // Last Epoch zoom facts (verified by Il2CppLE reflection): CameraManager
    // owns all camera state — zoomMin / zoomDefault / zoomPerScroll /
    // zoomSpeed / targetZoom / currentZoom / cameraAngleMin/Max/Default.
    // There is NO zoomMax field, and writing Camera.main.fieldOfView is
    // useless (CameraManager overrides it every frame).
    //
    // v2 safety rules (the grizwad fixes — "camera stuck under ground,
    // restart was the only fix" can no longer happen):
    //  1. Originals latch ONLY after every value reads back finite — a
    //     partial capture retries next frame instead of freezing NaNs in.
    //  2. Nothing non-finite is ever written to the camera, and the live
    //     zoom/angle are NaN-healed every frame (a poisoned lerp can never
    //     strand the camera again).
    //  3. Unlocking the angle restores the game's REAL captured min/max —
    //     not a fabricated ±25° range.
    internal static class CameraState
    {
        public static bool  Captured { get; private set; }
        public static bool  CaptureStalled { get; private set; }
        public static float ZoomMin, ZoomDefault, ZoomPerScroll, ZoomSpeed;
        public static float AngleDefault, AngleMin, AngleMax;

        const int StallFrames = 300;   // ~5 s of failed reads before we call it stalled
        static int _failStreak;

        public static bool TryCapture(CameraManager mgr)
        {
            if (Captured) return true;
            if (mgr == null) return false;
            string failReason;
            try
            {
                float zm   = mgr.zoomMin,            zd   = mgr.zoomDefault;
                float zps  = mgr.zoomPerScroll,      zs   = mgr.zoomSpeed;
                float ad   = mgr.cameraAngleDefault;
                float amin = mgr.cameraAngleMin,     amax = mgr.cameraAngleMax;
                if (Finite(zm) && Finite(zd) && Finite(zps) && Finite(zs)
                    && Finite(ad) && Finite(amin) && Finite(amax))
                {
                    ZoomMin = zm; ZoomDefault = zd; ZoomPerScroll = zps; ZoomSpeed = zs;
                    AngleDefault = ad; AngleMin = amin; AngleMax = amax;
                    Captured = true;                    // latch only after a fully clean read
                    CaptureStalled = false;
                    _failStreak = 0;
                    MelonLogger.Msg(
                        $"originals captured — zoomMin {zm:F1}, zoomDefault {zd:F1}, " +
                        $"perScroll {zps:F1}, speed {zs:F1}, angle {ad:F1}° [{amin:F1}..{amax:F1}]");
                    return true;
                }
                failReason = "non-finite camera values";
            }
            catch (System.Exception ex) { failReason = ex.Message; }

            // Failed with a live manager: surface a stall instead of silently
            // idling forever — the diagnosability gap that hid v1.x's bug class.
            if (++_failStreak == StallFrames)
            {
                CaptureStalled = true;
                MelonLogger.Warning(
                    $"camera values not readable after {StallFrames} attempts ({failReason}) — " +
                    "mod is idle; a game update may have changed CameraManager");
            }
            return false;
        }

        // Applies preferences to the camera. Compare-then-write against the
        // LIVE values, so per-scene resets self-heal without stomping the
        // manager (or another camera mod) every frame.
        public static void Apply(CameraManager mgr)
        {
            if (mgr == null || !Captured) return;       // never write before a clean capture
            try
            {
                float zoomMin = SaneZoomMin;
                float perScrl = Sane(Prefs.ZoomPerScroll.Value, Prefs.PerScrollLo, Prefs.PerScrollHi, Prefs.PerScrollDefault);
                float speed   = Sane(Prefs.ZoomSpeed.Value,     Prefs.SpeedLo,     Prefs.SpeedHi,     Prefs.SpeedDefault);

                if (Differs(mgr.zoomMin, zoomMin))             mgr.zoomMin       = zoomMin;
                if (Differs(mgr.zoomPerScroll, perScrl))       mgr.zoomPerScroll = perScrl;
                if (Differs(mgr.zoomSpeed, speed))             mgr.zoomSpeed     = speed;

                if (Prefs.LockAngle.Value)
                {
                    float a = Sane(Prefs.Angle.Value, Prefs.AngleLo, Prefs.AngleHi, AngleDefault);
                    if (Differs(mgr.cameraAngleDefault, a)) mgr.cameraAngleDefault = a;
                    if (Differs(mgr.cameraAngleMin, a))     mgr.cameraAngleMin     = a;
                    if (Differs(mgr.cameraAngleMax, a))     mgr.cameraAngleMax     = a;
                }
                else
                {
                    if (Differs(mgr.cameraAngleDefault, AngleDefault)) mgr.cameraAngleDefault = AngleDefault;
                    if (Differs(mgr.cameraAngleMin, AngleMin))         mgr.cameraAngleMin     = AngleMin;
                    if (Differs(mgr.cameraAngleMax, AngleMax))         mgr.cameraAngleMax     = AngleMax;
                }

                // NaN self-heal + range enforcement on the live zoom.
                float tgt = mgr.targetZoom, cur = mgr.currentZoom;
                if (!Finite(tgt) || !Finite(cur))
                {
                    mgr.targetZoom  = ZoomDefault;
                    mgr.currentZoom = ZoomDefault;
                    MelonLogger.Warning("camera zoom was NaN — healed back to game default");
                }
                else if (tgt < zoomMin)
                    mgr.targetZoom = zoomMin;
            }
            catch { }
        }

        // Writes every captured original back and recentres the zoom — the
        // in-mod answer to "restarting was the only fix".
        public static void RestoreToGame(CameraManager mgr)
        {
            if (mgr == null || !Captured) return;
            try
            {
                mgr.zoomMin            = ZoomMin;
                mgr.zoomPerScroll      = ZoomPerScroll;
                mgr.zoomSpeed          = ZoomSpeed;
                mgr.cameraAngleDefault = AngleDefault;
                mgr.cameraAngleMin     = AngleMin;
                mgr.cameraAngleMax     = AngleMax;
                mgr.resetZoom();
            }
            catch { }
        }

        public static float SaneZoomMin =>
            Sane(Prefs.ZoomMin.Value, Prefs.ZoomMinLo, Prefs.ZoomMinHi, Prefs.ZoomMinDefault);

        static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        static bool Differs(float live, float want) => !(Mathf.Abs(live - want) < 0.001f);

        static float Sane(float v, float lo, float hi, float fallback)
            => Finite(v) ? Mathf.Clamp(v, lo, hi) : fallback;
    }
}
