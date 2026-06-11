// ================================================================
//  TerribleTooltipsMod.cs — entry point + lifecycle.
//
//  HarmonyDontPatchAll + per-feature TryPatch: if a game update breaks
//  one native signature, that ONE feature degrades with a warning and
//  everything else keeps working. (The family standard since fog_OF_war.)
//
//  Logging contract: one startup line. Everything else is Dbg-gated or
//  a degradation warning. (Aaron's House gets its ♥ lines — canon.)
// ================================================================

[assembly: MelonInfo(typeof(medick_Terrible_Tooltips.TerribleTooltipsMod),
    medick_Terrible_Tooltips.BuildInfo.Name,
    medick_Terrible_Tooltips.BuildInfo.Version,
    medick_Terrible_Tooltips.BuildInfo.Author)]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]
[assembly: HarmonyDontPatchAll]

namespace medick_Terrible_Tooltips;

public class TerribleTooltipsMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Prefs.Init();
        int ok = ApplyPatches();
        MelonLogger.Msg($"{BuildInfo.OfficialName} v{BuildInfo.Version} loaded ({ok}/{s_patchCount} patches).");
    }

    public override void OnUpdate()
    {
        GroundLabels.OnUpdate();
        FilterRuleTooltip.MonitorUpdate();
    }

    public override void OnLateUpdate()
    {
        TooltipRecolor.OnLateUpdate();
    }

    public override void OnApplicationQuit()
    {
        Prefs.Save();
    }

    // ── Per-feature patching ──────────────────────────────────────────
    private static int s_patchCount;

    private int ApplyPatches()
    {
        int ok = 0;

        // Tooltip text pipeline (stage 1: bracket injection)
        ok += TryPatch(typeof(AffixInjector.Patch_AffixFormatter),    "tooltip affix grades");
        ok += TryPatch(typeof(AffixInjector.Patch_UniqueFormatter),   "unique/legendary grades");
        ok += TryPatch(typeof(AffixInjector.Patch_ImplicitFormatter), "implicit grades");

        // Tooltip text pipeline (stage 2: recolor)
        ok += TryPatch(typeof(TooltipRecolor.Patch_UpdateLayout),     "tooltip recolor");

        // Ground labels
        ok += TryPatch(typeof(GroundLabels.Patch_GroundLabel),        "ground labels");

        // Filter rule capture
        ok += TryPatch(typeof(FilterRuleTooltip.Patch_SetAsItemTooltip),   "filter rule (item hover)");
        ok += TryPatch(typeof(FilterRuleTooltip.Patch_SetAsGroundTooltip), "filter rule (ground hover)");

        // Settings panel
        ok += TryPatch(typeof(SettingsUi.Patch_SettingsPanel),        "settings panel");

        return ok;
    }

    private int TryPatch(Type patchClass, string feature)
    {
        s_patchCount++;
        try
        {
            new PatchClassProcessor(HarmonyInstance, patchClass).Patch();
            return 1;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"'{feature}' disabled — game update likely changed a signature ({ex.Message}). Everything else still works.");
            return 0;
        }
    }
}
