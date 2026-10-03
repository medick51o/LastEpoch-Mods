using System;
using System.Reflection;
using medick_DeathCounter.Core;

namespace medick_DeathCounter.Game
{
    internal static class PlayContextProbe
    {
        const string CycleType = "Il2CppLE.Data.Cycle";

        public static PlayContext Read(object characterData, string expectedCharacter)
        {
            // Never stamp the cycle of a preview or a different loaded character.
            string name = Refl.GetString(characterData, "CharacterName", "characterName");
            if (string.IsNullOrWhiteSpace(expectedCharacter) || name?.Trim() != expectedCharacter.Trim()) return new();
            bool? offline = Refl.Get(characterData, "IsOffline") is bool value ? value : null;
            object cycle = Refl.Get(characterData, "Cycle");
            int? id = CycleId(cycle);
            string code = CycleName(cycle);
            bool effectiveKnown = false;
            string label = null;
            try
            {
                // Read the loaded service only. Do not initialize services,
                // request a cycle from the server or use the character-select tab.
                object service = Refl.Static("Il2CppLE.Services.ServiceProvider", "get_Initialized") is true
                    ? Refl.Static("Il2CppLE.Services.ServiceProvider", "get_CycleStatusService") : null;
                object status = Refl.Get(service, "Status");
                bool ready = status?.GetType().IsEnum == true && Enum.GetName(status.GetType(), status) == "Initialized";
                if (ready && code != null)
                {
                    object effective = InvokeCycle(service, "EffectiveCycle", cycle);
                    if (CycleName(effective) is string effectiveName)
                    {
                        cycle = effective;
                        id = CycleId(effective);
                        code = effectiveName;
                        effectiveKnown = true;
                    }
                    if (effectiveKnown)
                    {
                        object info = InvokeCycle(service, "GetInfo", cycle);
                        string tag = Refl.GetString(info, "LocalizationTag");
                        if (tag != null)
                        {
                            const string missing = "__medick_cycle_name_unavailable__";
                            var method = Refl.FindType("Il2Cpp.Localization")?.GetMethod("GetText", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null);
                            string localized = method?.Invoke(null, new object[] { tag, missing }) as string;
                            if (!string.IsNullOrWhiteSpace(localized) && localized != tag && localized != missing) label = localized;
                        }
                    }
                }
            }
            catch (Exception ex) { Dbg.Log("season context: " + ex.Message); }
            return PlayContext.FromGameCycle(id, code, label, effectiveKnown, offline);
        }

        static object InvokeCycle(object service, string method, object cycle) =>
            service?.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public, null, new[] { cycle.GetType() }, null)?.Invoke(service, new[] { cycle });

        static string CycleName(object value) => value?.GetType().FullName == CycleType && value.GetType().IsEnum
            ? Enum.GetName(value.GetType(), value) : null;

        static int? CycleId(object value) => value?.GetType().FullName == CycleType && value.GetType().IsEnum
            ? Convert.ToInt32(value) : null;
    }
}
