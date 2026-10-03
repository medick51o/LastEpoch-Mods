using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using medick_DeathCounter.Core;
using UnityEngine;

namespace medick_DeathCounter.Game
{
    internal static class DeathReportHooks
    {
        public static int Installed { get; private set; }
        static readonly DeathFormatterCapture Capture = new();
        static readonly DeathMessageInbox Inbox = new();
        static string _character, _readState;
        static int _emitted = -1, _insideCause;

        public static void Install(HarmonyLib.Harmony harmony)
        {
            // Never detour ReceiveDetailedDeathInfo, UpdateDeathInfo, or
            // AppendLocalizedDamageTypes: all carry Il2Cpp Nullable<DamageType>.
            // Trampoline failures stopped the game's own death message.
            InstallOne(harmony, "Il2Cpp.DeathInformationText", "AppendLocalizedCause", 4, nameof(CausePrefix), nameof(CausePostfix));
            InstallOne(harmony, "Il2Cpp.DeathInformationText", "AppendLocalizedDamage", 4, nameof(AmountPrefix), null);
            InstallOne(harmony, "Il2Cpp.DamageTypes", "GetName", 1, null, nameof(ElementPostfix));
            InstallOne(harmony, "Il2Cpp.DeathInformationText", "UpdateText", 0, null, nameof(TextPostfix));
            InstallOne(harmony, "Il2Cpp.DeathInformationText", "OnEnable", 0, null, nameof(ScreenPostfix));
            MelonLogger.Msg($"Death details: {Installed} formatter observers available; original death-report callbacks left intact.");
        }

        static void InstallOne(HarmonyLib.Harmony harmony, string type, string name, int count, string prefix, string postfix)
        {
            var method = Refl.FindType(type)?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == count);
            if (method == null || !InteropSignaturePolicy.CanPatch(method)) return;
            HarmonyMethod Patch(string callback) => callback == null ? null
                : new HarmonyMethod(typeof(DeathReportHooks).GetMethod(callback, BindingFlags.NonPublic | BindingFlags.Static));
            try
            {
                harmony.Patch(method, prefix: Patch(prefix), postfix: Patch(postfix));
                Installed++; Dbg.Log("observing death formatter " + type + "." + name);
            }
            catch (Exception ex) { MelonLogger.Warning($"Cannot observe {name}: {ex.Message}"); }
        }

        static void CausePrefix(object[] __args, out bool __state)
        {
            __state = false;
            try
            {
                if (!PlayerProbe.HasPlayer || __args?.Length != 4) return;
                _insideCause++; __state = true;
                Capture.Cause(Refl.Ptr(__args[0]).ToInt64(), PlayerProbe.CharacterName(), Time.frameCount, Time.time,
                    Localized("GetLocalizedAttackerName", __args[3]), Localized("GetLocalizedAbilityName", __args[2]),
                    Localized("GetLocalizedAilmentName", __args[1]));
            }
            catch (Exception ex) { Dbg.Log("death cause formatter: " + ex.Message); }
        }

        static void CausePostfix(bool __state) { if (__state) _insideCause = Math.Max(0, _insideCause - 1); }

        static void AmountPrefix(object[] __args)
        {
            try
            {
                if (!PlayerProbe.HasPlayer || __args?.Length != 4
                    || __args[1] is not bool crit || __args[2] is not int damage || __args[3] is not int overkill) return;
                Capture.Amount(Refl.Ptr(__args[0]).ToInt64(), PlayerProbe.CharacterName(), Time.frameCount, Time.time, crit, damage, overkill);
            }
            catch (Exception ex) { Dbg.Log("death amount formatter: " + ex.Message); }
        }

        static void ElementPostfix(object[] __args, string __result)
        {
            try
            {
                if (_insideCause != 0 || __args?.Length != 1) return;
                if (Elements.TryParse(__args[0]?.ToString(), out var element))
                    Capture.Element(Time.frameCount, Time.time, element, __result);
            }
            catch (Exception ex) { Dbg.Log("death element formatter: " + ex.Message); }
        }

        static void TextPostfix(object __instance) { ObserveScreen(__instance, true); }
        static void ScreenPostfix(object __instance) { ObserveScreen(__instance, false); }
        static void ObserveScreen(object instance, bool fresh)
        {
            try { Poll(fresh, Refl.GetString(Refl.Get(instance, "text"), "text")); }
            catch (Exception ex) { Dbg.Log("death text observer: " + ex.Message); }
        }

        public static void Poll(bool textUpdated = false, string displayedText = null)
        {
            string character = PlayerProbe.CharacterName();
            if (string.IsNullOrWhiteSpace(character) || character == "Unknown Hero") return;
            var read = Refl.ReadStatic("Il2Cpp.DeathInformationText", "get_mostRecentDeathInfo");
            string text = displayedText ?? read.Value as string;
            if (_character != character)
            {
                _character = character; Capture.Reset(); _emitted = -1; _readState = null;
            }
            float health = PlayerProbe.CurrentHealth;
            bool window = float.IsFinite(health) && health <= 0 || DeathTracker.AwaitingDeathDetails;
            if (window && displayedText == null)
            {
                // The active label is an independent read, useful if a game
                // update changes the static report field but keeps the UI.
                object active = Refl.Static("Il2Cpp.DeathInformationText", "get_activeDeathInformationTexts");
                int count = Refl.Get(active, "Count") is int n ? Math.Min(n, 8) : 0;
                for (int i = 0; i < count; i++)
                {
                    string shown = Refl.GetString(Refl.Get(Refl.Index(active, i), "text"), "text");
                    if (!string.IsNullOrWhiteSpace(shown)) { text = shown; break; }
                }
                string state = read.Detail + "; static chars=" + ((read.Value as string)?.Length ?? 0)
                    + "; active labels=" + count + "; selected chars=" + (text?.Length ?? 0);
                if (state != _readState) { _readState = state; Dbg.Log("death message read: " + state); }
            }
            var report = Capture.Build(character, Time.time, text);
            bool newBatch = report != null && Capture.Generation != _emitted;
            // A UI redraw alone can display the preceding death. Reusing an
            // identical message requires newly captured formatter fragments.
            string ready = Inbox.Observe(character, text, Time.time, window, newBatch);
            if (ready == null) return;
            if (!newBatch) report = DeathMessageParser.Parse(ready);
            _emitted = Capture.Generation;
            DeathTracker.OnDeathDetails(report);
            Dbg.Log($"game death message: attacker={report.Killer ?? "unavailable"}, ability={report.Ability ?? "none"}, damage={report.Damage:0}, type={report.PrimaryElement ?? "unavailable"}, source={report.Source ?? (newBatch ? "formatter" : "text")}");
        }

        static string Localized(string method, object arg)
        {
            if (arg == null || Refl.Ptr(arg) == IntPtr.Zero) return null;
            try
            {
                var m = Refl.FindType("Il2Cpp.DeathInformationText")?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == method && m.GetParameters().Length == 1);
                return DeathFormatterCapture.Plain(m?.Invoke(null, new[] { arg }) as string);
            }
            catch { return null; }
        }
    }
}
