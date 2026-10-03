using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using medick_DeathCounter.Core;
using MelonLoader;
using UnityEngine;

namespace medick_DeathCounter.Game
{
    // Taps into the game, all resolved by NAME at startup and applied one
    // by one (per-patch degradation, same law as fog_OF_war):
    //   hit       damage landing on the player → HitEvent
    //   death     a player-owned component dying → DeathTracker.OnDeath
    //   anydeath  a death signal with no owner to check (the death screen,
    //             analytics) → only counts when the player's health reads <= 0
    //   ailment   an ailment landing on the player → DeathTracker.OnAilment
    // Plus two hook-free signals in DeathTracker: the health watch, and the
    // game's own CharacterData.Deaths counter.
    //
    // Names from docs/RESEARCH-game-api.md: public Last Epoch mods (RCInet
    // LastEpoch_Mods for LE 1.4, le-pandora, Fallen_LE_Mods) plus a 2023
    // Il2CppInspector dump. The damage chain is
    //   DamageStatsHolder.applyDamage(Actor)            attacker's side
    //   → ProtectionClass.ApplyDamage(DamageStats, DamageSource, float,
    //         Actor attacker, bool) : HitEvents        the player's side
    //   → BaseHealth.HealthDamage(float)
    // and DoT ticks take the same path with the ActiveAilment as the
    // DamageSource and DamageStats.isHit == false. Not yet run in game: the
    // first launch with ProbeApi=true confirms them; HookOverrides in the
    // cfg adds more without a rebuild. See CURRENT-WORK.md.
    internal static class GameHooks
    {
        enum Role { Hit, Death, AnyDeath, Ailment }

        static readonly string[] HealthHitMethods = { "HealthDamage", "HealthDamageCull", "HealthDamagePercent", "CurrentHealthPercentDamage" };

        static readonly (Role role, string type, string[] methods)[] Candidates =
        {
            (Role.Hit,      "Il2Cpp.ProtectionClass",  new[] { "ApplyDamage" }),
            (Role.Hit,      "Il2Cpp.BaseHealth",       HealthHitMethods),
            (Role.Hit,      "Il2Cpp.PlayerHealth",     HealthHitMethods),
            (Role.Hit,      "Il2Cpp.UnitHealth",       HealthHitMethods),
            (Role.Death,    "Il2Cpp.Dying",            new[] { "die" }),
            (Role.Death,    "Il2Cpp.ActorSync",        new[] { "receiveDeath" }),
            (Role.Death,    "Il2Cpp.ActorVisuals",     new[] { "Die" }),
            (Role.AnyDeath, "Il2Cpp.DeathScreen",      new[] { "toggle" }),
            (Role.AnyDeath, "Il2Cpp.AnalyticsManager", new[] { "PlayerDeath" }),
            (Role.Ailment,  "Il2Cpp.AilmentReceiver",  new[] { "ApplyAilment", "ApplyAilmentWithDamageStats", "ApplyStackOfAilment",
                                                               "ApplyStacksOfAilment", "ApplyStackOfAilmentForDuration" }),
        };

        public static int HitHooks, DeathHooks, AilmentHooks;   // AnyDeath counts as a death hook
        static readonly HashSet<IntPtr> _notPlayer = new();
        static readonly HashSet<string> _hooked = new();
        static readonly (string type, string method)[] DeathReport =
        {
            ("Il2Cpp.Dying", "die"),
            ("Il2Cpp.ActorSync", "receiveDeath"),
            ("Il2Cpp.ActorVisuals", "Die"),
            ("Il2Cpp.DeathScreen", "toggle"),
            ("Il2Cpp.AnalyticsManager", "PlayerDeath"),
        };

        // ── Install ──────────────────────────────────────────────
        public static void Install(HarmonyLib.Harmony harmony)
        {
            var targets = new List<(Role, MethodInfo)>();
            foreach (var (role, typeName, names) in Candidates)
            {
                var t = Refl.FindType(typeName);
                if (t == null) { Dbg.Log($"hook type {typeName} not in this build"); continue; }
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    if (names.Contains(m.Name) && !m.IsAbstract && !m.IsGenericMethodDefinition && !m.IsSpecialName)
                        targets.Add((role, m));
            }
            targets.AddRange(ParseOverrides(Prefs.HookOverrides.Value));

            var seen = new HashSet<MethodInfo>();
            foreach (var (role, m) in targets)
            {
                if (!seen.Add(m)) continue;
                string label = $"{role.ToString().ToLowerInvariant()} {m.DeclaringType?.Name}.{m.Name}({m.GetParameters().Length})";
                if (!InteropSignaturePolicy.CanPatch(m))
                {
                    Dbg.Log("skipped nullable interop signature " + label);
                    continue;
                }
                try
                {
                    switch (role)
                    {
                        case Role.Hit:
                            PatchHit(harmony, m);
                            HitHooks++; break;
                        case Role.Death:
                            harmony.Patch(m, prefix: Hm(nameof(DeathPrefix)));
                            DeathHooks++; break;
                        case Role.AnyDeath:
                            harmony.Patch(m, postfix: Hm(nameof(AnyDeathPostfix)));
                            DeathHooks++; break;
                        case Role.Ailment:
                            harmony.Patch(m, prefix: Hm(nameof(AilmentPrefix)));
                            AilmentHooks++; break;
                    }
                    _hooked.Add(m.DeclaringType?.FullName + "." + m.Name);
                    _hooked.Add(m.DeclaringType?.Name + "." + m.Name);
                    Dbg.Log("hooked " + label);
                    if (Prefs.ProbeApi.Value) MelonLogger.Msg("[probe] hooked " + label);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"hook {label} failed, skipped: {ex.Message}");
                }
            }

            var bits = new List<string>();
            foreach (var (type, method) in DeathReport)
            {
                string shortType = type.Substring(type.LastIndexOf('.') + 1);
                bool found = _hooked.Contains(type + "." + method) || _hooked.Contains(shortType + "." + method);
                bits.Add(shortType + "." + method + (found ? " found" : " missing"));
            }
            Dbg.Log("death hooks: " + string.Join(", ", bits));
            DeathReportHooks.Install(harmony);

            if (HitHooks == 0 && DeathReportHooks.Installed == 0)
                MelonLogger.Warning("no damage hook found: deaths still count (health watch), but the killer will show as unknown. Set ProbeApi=true in UserData/medick_DeathCounter.cfg and send the log.");
        }

        // ApplyDamage returns HitEvents flags (crit, freeze, stun, kill): read
        // them through __result when the method returns HitEvents. If Harmony
        // refuses the boxed result for this signature, fall back to the plain
        // postfix and lose only the flags.
        static void PatchHit(HarmonyLib.Harmony harmony, MethodInfo m)
        {
            if (m.ReturnType.Name == "HitEvents")
            {
                try
                {
                    harmony.Patch(m, prefix: Hm(nameof(HitPrefix)), postfix: Hm(nameof(HitPostfixWithFlags)));
                    return;
                }
                catch (Exception ex) { Dbg.Log($"HitEvents postfix refused on {m.Name} ({ex.Message}); plain postfix"); }
            }
            harmony.Patch(m, prefix: Hm(nameof(HitPrefix)), postfix: Hm(nameof(HitPostfix)));
        }

        static HarmonyMethod Hm(string name) =>
            new(typeof(GameHooks).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));

        // "hit:Il2Cpp.BaseHealth.ReceiveDamage; death:Il2Cpp.PlayerHealth.Die"
        // Every overload of the named method is hooked.
        static IEnumerable<(Role, MethodInfo)> ParseOverrides(string spec)
        {
            if (string.IsNullOrWhiteSpace(spec)) yield break;
            foreach (var raw in spec.Split(';'))
            {
                var part = raw.Trim();
                if (part.Length == 0) continue;
                int colon = part.IndexOf(':');
                int dot   = part.LastIndexOf('.');
                if (colon < 1 || dot <= colon + 1 || !Enum.TryParse(part.Substring(0, colon).Trim(), true, out Role role))
                {
                    MelonLogger.Warning($"HookOverrides: cannot read '{part}' (want role:Namespace.Type.Method)");
                    continue;
                }
                string typeName = part.Substring(colon + 1, dot - colon - 1).Trim();
                string method   = part.Substring(dot + 1).Trim();
                var t = Refl.FindType(typeName);
                var found = t?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => m.Name == method && !m.IsAbstract && !m.IsGenericMethodDefinition).ToList();
                if (found == null || found.Count == 0)
                {
                    MelonLogger.Warning($"HookOverrides: {typeName}.{method} not found");
                    continue;
                }
                foreach (var m in found) yield return (role, m);
            }
        }

        public static void OnPlayerChanged() => _notPlayer.Clear();

        // ── Is this the local player? ────────────────────────────
        // Damage hooks fire for every monster too, so the miss path must be
        // cheap: one pointer compare for the known player component, one set
        // lookup for known strangers, one interop call for first sightings.
        static bool IsPlayer(object inst)
        {
            IntPtr p = Refl.Ptr(inst);
            if (p == IntPtr.Zero || !PlayerProbe.HasPlayer) return false;
            if (p == PlayerProbe.HealthPtr || p == Refl.Ptr(PlayerProbe.Actor)) return true;
            if (_notPlayer.Contains(p)) return false;

            GameObject go = null;
            try { go = inst is Component c ? c.gameObject : inst as GameObject; } catch { }
            bool mine = PlayerProbe.IsPlayerObject(go)
                // A component on a child object (visuals) still names its owner.
                || (PlayerProbe.Actor != null && Refl.Ptr(Refl.Get(inst, "actor", "Actor", "owner")) == Refl.Ptr(PlayerProbe.Actor));
            if (!mine)
            {
                if (_notPlayer.Count > 4096) _notPlayer.Clear();
                _notPlayer.Add(p);
            }
            else if (PlayerProbe.Health == null && inst is Component hc && hc.GetType().Name.Contains("Health"))
                PlayerProbe.AdoptHealth(hc);
            return mine;
        }

        // ── Hit ──────────────────────────────────────────────────
        // Prefix reads health before and the call's arguments; the outermost
        // postfix turns "health before - health after" into the real damage
        // taken. Nested hooked calls (ReceiveDamage → Damage) merge into one
        // HitEvent instead of counting twice.
        internal sealed class HitScratch { public float Before; public bool Kill; }

        // True while a player hit is being processed (between the outermost
        // prefix and postfix). A death that fires inside it waits for the
        // killing blow to be recorded first (review 2026-10-01 #2).
        public static bool InPlayerHit => _depth > 0 && _depthFrame == Time.frameCount;

        static int      _depth;
        static int      _depthFrame;
        static HitEvent _pending;
        static int      _probeHitsLogged;

        // Note (review #9): asking for __args makes Harmony box an argument
        // array on every hooked call, monsters included. Accepted: hits are
        // tens to hundreds per second and the arrays are small and short-lived.
        static void HitPrefix(object __instance, object[] __args, MethodBase __originalMethod, out HitScratch __state)
        {
            __state = null;
            try
            {
                if (!DeathTracker.Recording || !IsPlayer(__instance)) return;

                int frame = Time.frameCount;
                if (_depth > 0 && frame != _depthFrame) { _depth = 0; _pending = null; }   // a throwing original never ran our postfix
                _depthFrame = frame;

                __state = new HitScratch { Before = PlayerProbe.CurrentHealth };
                if (_depth++ == 0) _pending = new HitEvent();
                ArgReader.Read(__args, __originalMethod, _pending);

                if (Prefs.ProbeApi.Value && _probeHitsLogged < 10)
                {
                    _probeHitsLogged++;
                    MelonLogger.Msg($"[probe] player hit via {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}: " +
                        string.Join(", ", (__args ?? Array.Empty<object>()).Select(a => a == null ? "null" : $"{a.GetType().Name}={Short(a)}")));
                }
            }
            catch (Exception ex)
            {
                if (__state != null && _depth > 0 && --_depth == 0) _pending = null;   // roll back our own ++ (review #6)
                __state = null;
                Dbg.Log("hit prefix: " + ex.Message);
            }
        }

        // HitEvents [Flags]: None 0, Hit 1, Crit 2, Kill 4, Freeze 8, Stun 16, Block 32, MeleeHit 64
        const int FlagHit = 1, FlagCrit = 2, FlagKill = 4, FlagFreeze = 8, FlagStun = 16;

        static void HitPostfixWithFlags(HitScratch __state, object __result)
        {
            if (__state != null && _pending != null && __result != null)
            {
                try
                {
                    int f = Convert.ToInt32(__result);
                    if ((f & FlagCrit) != 0) _pending.Crit = true;
                    else if ((f & FlagHit) != 0 && _pending.Crit == null) _pending.Crit = false;
                    if ((f & FlagFreeze) != 0) DeathTracker.OnAilment("Freeze");
                    if ((f & FlagStun) != 0) DeathTracker.OnAilment("Stun");
                    __state.Kill = (f & FlagKill) != 0;
                }
                catch { }
            }
            HitPostfix(__state);
        }

        static void HitPostfix(HitScratch __state)
        {
            if (__state == null) return;
            try
            {
                if (--_depth > 0) return;
                _depth = 0;
                var h = _pending; _pending = null;
                if (h == null) return;

                float before = __state.Before, after = PlayerProbe.CurrentHealth;
                bool known = !float.IsNaN(before) && !float.IsNaN(after);
                if (known)
                {
                    float lost = before - after;
                    if (lost <= 0f) return;            // dodged, blocked, or ward ate it
                    h.Amount = lost;
                }
                if (h.Amount <= 0f) return;

                float max = PlayerProbe.MaxHealth;
                h.Time         = Time.time;
                h.HealthBefore = float.IsNaN(before) ? -1f : before;
                h.MaxHealth    = float.IsNaN(max) ? -1f : max;
                if (!string.IsNullOrEmpty(h.Ailment) && Ailments.ByName(h.Ailment)?.IsDot == true && h.ByElement == null)
                    h.IsDot = true;
                DeathTracker.OnHit(h);

                if ((known && before > 0f && after <= 0f) || __state.Kill)
                    DeathTracker.OnDeath("hook");   // OnDeath re-checks health, so a stray Kill flag cannot invent a death
                DeathTracker.FlushDeferredDeath();
            }
            catch (Exception ex) { _depth = 0; _pending = null; Dbg.Log("hit postfix: " + ex.Message); }
            finally { if (_depth == 0) DeathTracker.FlushDeferredDeath(); }
        }

        // ── Death ────────────────────────────────────────────────
        static void DeathPrefix(object __instance)
        {
            try { if (DeathTracker.Recording && IsPlayer(__instance)) DeathTracker.OnDeath("hook"); }
            catch (Exception ex) { Dbg.Log("death prefix: " + ex.Message); }
        }

        // The death screen opening, analytics' PlayerDeath: no owner to check,
        // so they only count when the player's health agrees.
        static void AnyDeathPostfix()
        {
            try { DeathTracker.OnUnownedDeathSignal(); }
            catch (Exception ex) { Dbg.Log("any-death postfix: " + ex.Message); }
        }

        // ── Ailment ──────────────────────────────────────────────
        static void AilmentPrefix(object __instance, object[] __args, MethodBase __originalMethod)
        {
            try
            {
                if (!DeathTracker.Recording) return;
                // The receiver decides who it is on. Never scan the args: the
                // player is the CREATOR of every ailment it puts on monsters
                // (review 2026-10-01 #3).
                if (!IsPlayer(__instance)) return;

                string name = ArgReader.AilmentName(__args) ?? Ailments.Find(__originalMethod.Name)?.Name;
                if (name != null) DeathTracker.OnAilment(name);
            }
            catch (Exception ex) { Dbg.Log("ailment prefix: " + ex.Message); }
        }

        static string Short(object a)
        {
            try
            {
                string s = a is UnityEngine.Object u ? u.name : a.ToString();
                return s != null && s.Length > 40 ? s.Substring(0, 40) : s;
            }
            catch { return "?"; }
        }

        // ── ProbeApi ─────────────────────────────────────────────
        // One-time dump of everything that looks health/death/ailment shaped,
        // so the candidate lists above can be corrected from a single log.
        public static void DumpApi()
        {
            string[] words = { "Health", "Death", "Dying", "Ailment", "DamageStats", "DamageType", "PlayerFinder", "CharacterData", "ExperienceTracker", "ActorDisplay" };
            int types = 0;
            MelonLogger.Msg("[probe] ===== Terrible Deaths API probe =====");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] all;
                try { all = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { all = e.Types.Where(x => x != null).ToArray(); }
                catch { continue; }
                foreach (var t in all.Where(t => t.Namespace == "Il2Cpp" && words.Any(w => t.Name.Contains(w))).OrderBy(t => t.Name))
                {
                    if (++types > 80) { MelonLogger.Msg("[probe] (type cap reached)"); return; }
                    MelonLogger.Msg($"[probe] type {t.FullName} : {t.BaseType?.Name}");
                    if (t.IsEnum) { MelonLogger.Msg("  values " + string.Join(", ", Enum.GetNames(t))); continue; }
                    foreach (var line in Refl.Describe(t).Take(80)) MelonLogger.Msg("[probe]" + line);
                }
            }
            MelonLogger.Msg($"[probe] ===== {types} types =====");
        }
    }
}
