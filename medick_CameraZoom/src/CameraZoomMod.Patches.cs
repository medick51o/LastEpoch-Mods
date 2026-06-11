using HarmonyLib;
using Il2Cpp;

namespace medick_CameraZoom
{
    // Harmony patches live nested inside the partial MelonMod class —
    // MelonLoader's auto-discovery reliably finds them there (top-level
    // patch classes have silently failed to register in other LE mods).
    public partial class CameraZoomMod
    {
        // Capture + apply the moment each scene's camera spins up, so the
        // extended zoom range is live before the first scroll.
        [HarmonyPatch(typeof(CameraManager), "Start")]
        private static class Patch_CameraManagerStart
        {
            [HarmonyPostfix]
            public static void Postfix(CameraManager __instance)
            {
                if (__instance == null) return;
                bool ok = CameraState.TryCapture(__instance);
                CameraState.Apply(__instance);
                Dbg.Log(ok
                    ? "CameraManager.Start — settings applied for new scene"
                    : "CameraManager.Start — capture not ready, will retry in OnUpdate");
            }
        }
    }
}
