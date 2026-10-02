using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using MelonLoader;
using medick_DeathCounter.Core;

namespace medick_DeathCounter.Game
{
    internal static class DeathReportHooks
    {
        public static int Installed { get; private set; }
        static readonly Regex Tags = new("<[^>]*>", RegexOptions.Compiled);

        public static void Install(HarmonyLib.Harmony harmony)
        {
            // UpdateDeathInfo is the local death screen's shared online/offline
            // report. ReceiveDetailedDeathInfo is a separately validated fallback.
            InstallOne(harmony, "Il2Cpp.DeathInformationText", "UpdateDeathInfo", 9, nameof(LocalReportPostfix));
            InstallOne(harmony, "Il2Cpp.PlayerActorSync", "ReceiveDetailedDeathInfo", 10, nameof(NetworkReportPostfix));
            MelonLogger.Msg($"Death details: {Installed} game-report hooks available.");
        }

        static void InstallOne(HarmonyLib.Harmony harmony, string type, string name, int count, string postfix)
        {
            var method = Refl.FindType(type)?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == count);
            if (method == null) return;
            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(DeathReportHooks).GetMethod(postfix, BindingFlags.NonPublic | BindingFlags.Static)));
                Installed++;
                Dbg.Log("hooked death report " + type + "." + name);
            }
            catch (Exception ex) { MelonLogger.Warning($"Cannot read {name}: {ex.Message}"); }
        }

        static void LocalReportPostfix(object[] __args)
        {
            try { Capture(__args, true); }
            catch (Exception ex) { Dbg.Log("local death report: " + ex.Message); }
        }

        static void NetworkReportPostfix(object __instance, object[] __args)
        {
            try
            {
                var local = Refl.Static("Il2Cpp.PlayerFinder", "getLocalActorSync");
                IntPtr p = Refl.Ptr(local);
                if (p == IntPtr.Zero || p != Refl.Ptr(__instance)) return;
                Capture(__args, false);
            }
            catch (Exception ex) { Dbg.Log("network death report: " + ex.Message); }
        }

        static void Capture(object[] args, bool textUpdated)
        {
            if (!PlayerProbe.HasPlayer || !Prefs.Tracking.Value || args == null || args.Length < 9) return;
            string formatted = ReportText(args, textUpdated);
            var report = new DeathDetails
            {
                PrimaryElement = ElementName(args[0]),
                SecondaryElement = Refl.Get(args[1], "HasValue", "hasValue") is true ? ElementName(Refl.Get(args[1], "Value", "value")) : null,
                Crit = args[2] is bool crit && crit,
                Damage = Refl.TryFloat(args[3], out float damage) ? damage : 0f,
                Overkill = Refl.TryFloat(args[4], out float overkill) ? overkill : 0f,
                Ailment = Localized("GetLocalizedAilmentName", args[5]),
                Ability = Localized("GetLocalizedAbilityName", args[6]),
                Killer = Localized("GetLocalizedAttackerName", args[7]),
                Text = Plain(formatted),
                RichText = formatted,
            };
            DeathTracker.OnDeathDetails(report);
            Dbg.Log($"game death report: attacker={report.Killer ?? "unavailable"}, ability={report.Ability ?? "none"}, ailment={report.Ailment ?? "none"}, damage={report.Damage:0}, type={report.PrimaryElement}");
        }

        static string ElementName(object value) =>
            Elements.TryParse(value?.ToString(), out var el) ? Elements.Name(el) : null;

        static string ReportText(object[] args, bool textUpdated)
        {
            try
            {
                var m = Refl.FindType("Il2Cpp.DeathInformationText")?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "ComposeLocalizedDeathInfo" && m.GetParameters().Length == 9);
                string text = m?.Invoke(null, args.Take(9).ToArray()) as string;
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch { }
            // Only the shared UI update proves the static text belongs to this
            // death. Never attach the previous death's text to a network packet.
            return textUpdated ? Refl.Static("Il2Cpp.DeathInformationText", "get_mostRecentDeathInfo") as string : null;
        }

        static string Localized(string method, object arg)
        {
            if (arg == null || Refl.Ptr(arg) == IntPtr.Zero) return null;
            try
            {
                var m = Refl.FindType("Il2Cpp.DeathInformationText")?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == method && m.GetParameters().Length == 1);
                var text = Plain(m?.Invoke(null, new[] { arg }) as string);
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch { }
            return Plain(Refl.GetString(arg, "displayName", "abilityName", "ailmentName") ?? Refl.UnityName(arg));
        }

        static string Plain(string text) => string.IsNullOrWhiteSpace(text) ? null : Tags.Replace(text, "").Trim();
    }
}

