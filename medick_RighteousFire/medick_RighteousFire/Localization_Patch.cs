// ================================================================
//  Localization_Patch.cs  —  medick_RighteousFire
//
//  Intercepts the game's localization system to inject display
//  text for the RIGHTEOUS FIRE belt.
// ================================================================

namespace medick_RighteousFire;

public static class LocalizationHelper
{
    public static bool TryGet(string key, out string text)
    {
        if (key == Item_RighteousFire.Get_Subtype_Name())
        {
            text = "Righteous Fire Belt"; return true;
        }
        if (key == Item_RighteousFire.Get_Unique_Name())
        {
            text = "RIGHTEOUS FIRE"; return true;
        }
        if (key == Item_RighteousFire.Get_Tooltip_0())
        {
            text = "Equipping this belt ignites you.\n" +
                   "The fire that burns within you... burns everything else too.\n\n" +
                   "Shield Charge auto-fires toward the nearest enemy.\n" +
                   "Fire Traps detonate automatically in chain explosions.\n" +
                   "Using a potion doubles your aura for 8 seconds.\n" +
                   "You are also on fire. Your regen handles it.\n\n" +
                   "— Pohx";
            return true;
        }
        if (key == Item_RighteousFire.Get_Unique_Lore())
        {
            text = "A tribute to the man who made standing in fire a legitimate strategy. " +
                   "Offline use only. EHG has seen your work. Play responsibly. ♥";
            return true;
        }

        text = null;
        return false;
    }
}

[HarmonyPatch(typeof(Localization), "GetText")]
public static class Localization_GetText_Patch
{
    public static bool Prefix(string key, ref string __result)
    {
        if (LocalizationHelper.TryGet(key, out string text))
        {
            __result = text;
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(Localization), "TryGetText")]
public static class Localization_TryGetText_Patch
{
    public static bool Prefix(string key, ref string text, ref bool __result)
    {
        if (LocalizationHelper.TryGet(key, out string value))
        {
            text     = value;
            __result = true;
            return false;
        }
        return true;
    }
}
