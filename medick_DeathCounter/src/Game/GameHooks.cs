using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using medick_DeathCounter.Core;
using MelonLoader;
using UnityEngine;

namespace medick_DeathCounter.Game
{
    // Three taps into the game, all resolved by NAME at startup and applied
    // one by one (per-patch degradation, same law as fog_OF_war):
    //   hit      the player's health component taking damage → HitEvent
    //   death    the player dying → DeathTracker.OnDeath (the health watch
    //            in DeathTracker is the fallback when no death hook resolves)
    //   ailment  an ailment landing on the player → DeathTracker.OnAilment
    //
    // The candidate names below are educated guesses, NOT verified against
    // this game build (no game install on the machine that wrote them). The
    // first in-hand launch with ProbeApi=true prints the real API; fix the
    // lists or add HookOverrides in the cfg. See CURRENT-WORK.md.
    internal static class GameHooks
    {
        enum Role { Hit, Death, Ailment }

        static readonly (Role role, string type, string[] methods)[] Candidates =
        {
            (Role.Hit,     "Il2Cpp.PlayerHealth", new[] { "ReceiveDamage", "TakeDamage", "Damage", "ApplyDamage", "DamageHealth", "HealthDamage" }),
            (Role.Hit,     "Il2Cpp.BaseHealth",   new[] { "ReceiveDamage", "TakeDamage", "Damage", "ApplyDamage", "DamageHealth", "HealthDamage" }),
            (Role.Hit,     "Il2Cpp.Health",       new[] { "ReceiveDamage", "TakeDamage", "Damage", "ApplyDamage" }),
            (Role.Death,   "Il2Cpp.PlayerHealth", new[] { "Die", "OnDeath", "Death", "PlayerDied", "HandleDeath" }),
            (Role.Death,   "Il2Cpp.BaseHealth",   new[] { "Die", "OnDeath", "Death", "HandleDeath" }),
            (Role.Death,   "Il2Cpp.Actor",        new[] { "Die", "OnDeath", "Death" }),
            (Role.Ailment, "Il2Cpp.Actor",        new[] { "ApplyAilment", "applyAilment", "AddAilment", "addAilment", "ReceiveAilment" }),
            (Role.Ailment, "Il2Cpp.StatBuffs",    new[] { "ApplyAilment", "applyAilment", "AddAilment", "addAilment" }),
            (Role.Ailment, "Il2Cpp.AilmentData",  new[] { "ApplyAilment", "applyAilment", "AddAilment", "addAilment", "OnApply" }),
        };

        public static int HitHooks, DeathHooks, AilmentHooks;
        static readonly HashSet<IntPtr> _notPlayer = new();

        // ── Install ──────────────────────────────────────────────
        public static void Install(HarmonyLib.Harmony harmony)
        {
            var targets = new List<(Role, MethodInfo)>();
            foreach (var (role, typeName, names) in Candidates)
            {
                var t = Refl.FindType(typeName);
                if (t == null) { Dbg.Log($"hook type {typeName} not in this build"); continue; }
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    if (names.Contains(m.Name) && !m.IsAbstract && !m.IsGenericMethodDefinition && !m.IsSpecialName)
                        targets.Add((role, m));
            }
            targets.AddRange(ParseOverrides(Prefs.HookOverrides.Value));

            var seen = new HashSet<MethodInfo>();
            foreach (var (role, m) in targets)
            {
                if (!seen.Add(m)) continue;
                string label = $"{role.ToString().ToLowerInvariant()} {m.DeclaringType?.Name}.{m.Name}({m.GetParameters().Length})";
                try
                {
                    switch (role)
                    {
                        case Role.Hit:
                            harmony.Patch(m, prefix: Hm(nameof(HitPrefix)), postfix: Hm(nameof(HitPostfix)));
                            HitHooks++; break;
                        case Role.Death:
                            harmony.Patch(m, prefix: Hm(nameof(DeathPrefix)));
                            DeathHooks++; break;
                        case Role.Ailment:
                            harmony.Patch(m, prefix: Hm(nameof(AilmentPrefix)));
                            AilmentHooks++; break;
                    }
                    Dbg.Log("hooked " + label);
                    if (Prefs.ProbeApi.Value) MelonLogger.Msg("[probe] hooked " + label);
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"hook {label} failed, skipped: {ex.Message}");
                }
            }

            if (HitHooks == 0)
                MelonLogger.Warning("no damage hook found: deaths still count (health watch), but the killer will show as unknown. Set ProbeApi=true in UserData/medick_DeathCounter.cfg and send the log.");
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
            bool mine = PlayerProbe.IsPlayerObject(go);
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
        internal sealed class HitScratch { public float Before; }

        static int      _depth;
        static int      _depthFrame;
        static HitEvent _pending;
        static int      _probeHitsLogged;

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
            catch (Exception ex) { __state = null; Dbg.Log("hit prefix: " + ex.Message); }
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

                if (known && before > 0f && after <= 0f)
                    DeathTracker.OnDeath("hook");
            }
            catch (Exception ex) { _depth = 0; _pending = null; Dbg.Log("hit postfix: " + ex.Message); }
        }

        // ── Death ────────────────────────────────────────────────
        static void DeathPrefix(object __instance)
        {
            try { if (DeathTracker.Recording && IsPlayer(__instance)) DeathTracker.OnDeath("hook"); }
            catch (Exception ex) { Dbg.Log("death prefix: " + ex.Message); }
        }

        // ── Ailment ──────────────────────────────────────────────
        static void AilmentPrefix(object __instance, object[] __args, MethodBase __originalMethod)
        {
            try
            {
                if (!DeathTracker.Recording) return;
                bool onPlayer = IsPlayer(__instance)
                    || (__args != null && __args.Any(a => a is Component c && IsPlayer(c)));
                if (!onPlayer) return;

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
