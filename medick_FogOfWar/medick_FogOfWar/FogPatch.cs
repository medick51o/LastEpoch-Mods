// ================================================================
//  FogPatch.cs  —  medick_The_fogOFwar
//
//  Patches Il2CppLE.UI.Minimap.Minimap.Awake to control RevealRadius.
//
//  BLIND  — RevealRadius = 0, hides minimap UI (top right). Requires restart.
//  HARD   — RevealRadius = 0, minimap UI stays visible.
//  1-5    — RevealRadius updated on zone load AND attempted on-the-fly.
// ================================================================

using Il2CppLE.UI.Minimap;

namespace medick_FogOfWar;

[HarmonyPatch(typeof(Minimap), "Awake")]
public static class MinimapAwakePatch
{
    // Keep a reference so we can update on-the-fly
    internal static Minimap Instance;

    public static void Postfix(Minimap __instance)
    {
        try
        {
            Instance = __instance;

            // Capture default radius once on first load
            if (FogOfWarMod.DefaultRevealRadius < 0f && __instance.RevealRadius > 0f)
            {
                FogOfWarMod.DefaultRevealRadius = __instance.RevealRadius;
                MelonLogger.Msg($"[The_fogOFwar] Default RevealRadius captured: {FogOfWarMod.DefaultRevealRadius}");
            }

            float radius = FogOfWarMod.GetRevealRadius();
            __instance.RevealRadius = radius;
            MelonLogger.Msg($"[The_fogOFwar] RevealRadius → {radius}  (level: {FogOfWarMod.FogLevel.Value})");

            // BLIND only — hide the minimap corner UI
            // HARD leaves the minimap visible but RevealRadius stays 0
            if (FogOfWarMod.FogLevel.Value == FogOfWarMod.MapVisionLevel.BLIND)
                HideMinimapCorner(__instance);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[The_fogOFwar] MinimapAwakePatch failed: {ex.Message}");
        }
    }

    private static void HideMinimapCorner(Minimap minimap)
    {
        try
        {
            Transform t = minimap.transform.parent;
            while (t != null)
            {
                string n = t.name.ToLower();
                if (n.Contains("minimap") || n.Contains("hud") || n.Contains("corner"))
                    break;
                t = t.parent;
            }

            GameObject target = t != null ? t.gameObject : minimap.gameObject;
            target.SetActive(false);
            MelonLogger.Msg($"[The_fogOFwar] BLIND — minimap hidden: {target.name}");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[The_fogOFwar] BLIND hide failed: {ex.Message}");
        }
    }

    // Called from settings dropdown callback — tries to apply radius without a zone reload
    public static void TryApplyLive()
    {
        if (Instance == null) return;
        if (FogOfWarMod.FogLevel.Value == FogOfWarMod.MapVisionLevel.BLIND) return;

        try
        {
            float radius = FogOfWarMod.GetRevealRadius();
            Instance.RevealRadius = radius;
            MelonLogger.Msg($"[The_fogOFwar] Live update — RevealRadius → {radius}");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[The_fogOFwar] Live update failed: {ex.Message}");
        }
    }
}
