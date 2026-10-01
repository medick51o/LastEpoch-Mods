using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace medick_DeathCounter.Game
{
    // The local player: actor, health component, and "who am I" details.
    // PlayerFinder.getPlayerActor() is proven (Righteous Fire) but called by
    // name, like everything else here: the mod references no game assembly,
    // so it cannot fail to load over which Il2CppLE*.dll a type lives in.
    internal static class PlayerProbe
    {
        // docs/RESEARCH-game-api.md: PlayerHealth : BaseHealth (UnitHealth for
        // monsters), fields currentHealth / maxHealth (maxHealth was an int in
        // 2023; Refl.TryFloat takes either). PlayerFinder.getLocalPlayerHealth()
        // returns it directly.
        public static readonly string[] HealthTypes = { "Il2Cpp.PlayerHealth", "Il2Cpp.BaseHealth", "Il2Cpp.UnitHealth" };
        static readonly string[] CurrentHealthNames = { "currentHealth", "CurrentHealth" };
        static readonly string[] MaxHealthNames     = { "maxHealth", "MaxHealth" };

        public static Component  Actor   { get; private set; }   // Il2Cpp.Actor
        public static Type       ActorType => Refl.FindType("Il2Cpp.Actor");
        public static GameObject Object  { get; private set; }
        public static IntPtr     ObjectPtr { get; private set; }
        public static Component  Health  { get; private set; }
        public static IntPtr     HealthPtr { get; private set; }

        static System.Reflection.MethodInfo _getPlayerActor;
        static float _nextRefresh;
        static bool  _healthMissingWarned;

        public static bool HasPlayer => ObjectPtr != IntPtr.Zero;

        // Re-resolve twice a second (menus included: no per-frame lookups).
        // Returns true when the player object changed (zone load, respawn, new character).
        public static bool Refresh(float now)
        {
            if (now < _nextRefresh) return false;
            _nextRefresh = now + 0.5f;

            Component actor = null;
            try
            {
                _getPlayerActor ??= Refl.FindType("Il2Cpp.PlayerFinder")?.GetMethod("getPlayerActor", Type.EmptyTypes);
                actor = _getPlayerActor?.Invoke(null, null) as Component;
            }
            catch { }
            GameObject go = null;
            try { go = actor != null ? actor.gameObject : null; } catch { }
            IntPtr ptr = Refl.Ptr(go);
            if (ptr == ObjectPtr) return false;

            Actor = actor; Object = go; ObjectPtr = ptr;
            Health = null; HealthPtr = IntPtr.Zero;
            if (go != null)
            {
                if (Refl.Static("Il2Cpp.PlayerFinder", "getLocalPlayerHealth") is Component lph && IsPlayerObject(SafeGo(lph)))
                    AdoptHealth(lph);
                else foreach (var name in HealthTypes)
                {
                    var c = Refl.GetComponent(go, Refl.FindType(name));
                    if (c == null) continue;
                    AdoptHealth(c);
                    break;
                }
                if (Health == null && !_healthMissingWarned)
                {
                    _healthMissingWarned = true;
                    Dbg.Log("no health component found by name yet; the hit hook will adopt it on the first hit");
                }
            }
            return true;
        }

        // The hit hook calls this with the component it saw take player damage.
        public static void AdoptHealth(Component c)
        {
            Health = c;
            HealthPtr = Refl.Ptr(c);
            Dbg.Log($"player health component: {c?.GetType().FullName}");
        }

        public static void Reset()
        {
            Actor = null; Object = null; ObjectPtr = IntPtr.Zero;
            Health = null; HealthPtr = IntPtr.Zero;
            _nextRefresh = 0f;
        }

        // NaN = unknown (no health component, or the member names are wrong).
        // Never -1: health really does go negative on a killing blow.
        public static float CurrentHealth => Refl.GetFloat(Health, float.NaN, CurrentHealthNames);
        public static float MaxHealth     => Refl.GetFloat(Health, float.NaN, MaxHealthNames);

        public static bool IsPlayerObject(GameObject go) => go != null && Refl.Ptr(go) == ObjectPtr && ObjectPtr != IntPtr.Zero;

        static GameObject SafeGo(Component c)
        {
            try { return c != null ? c.gameObject : null; } catch { return null; }
        }

        // ── Who and where ────────────────────────────────────────
        // Il2CppLE.Data.CharacterData via PlayerFinder.getPlayerData():
        // CharacterName, Level, CharacterClass (int index), Deaths, Hardcore.
        public static string CharacterName() =>
            Refl.GetString(CharacterData(), "CharacterName", "characterName") ?? "Unknown Hero";

        public static string CharacterClass()
        {
            var v = Refl.Get(CharacterData(), "CharacterClass", "characterClass");
            if (v == null) return "";
            if (Refl.TryFloat(v, out var idx))
            {
                // CharacterClassList.instance.classes[i].className
                var list = Refl.Get(Refl.Static("Il2Cpp.CharacterClassList", "instance"), "classes");
                return Refl.GetString(Refl.Index(list, (int)idx), "className") ?? "";
            }
            return Refl.GetString(v, "className") ?? v.ToString();
        }

        public static int Level()
        {
            if (Refl.TryFloat(Refl.Get(CharacterData(), "Level", "level"), out var f)) return (int)f;
            var exp = Refl.Static("Il2Cpp.PlayerFinder", "getExperienceTracker")
                      ?? Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ExperienceTracker"));
            if (Refl.TryFloat(Refl.Get(exp, "CurrentLevel", "currentLevel"), out f)) return (int)f;
            return 0;
        }

        public static bool Hardcore() => Refl.Get(CharacterData(), "Hardcore", "hardcore") is bool b && b;

        // The game's own lifetime death count for this character, or -1.
        public static int GameDeathCount() =>
            Refl.TryFloat(Refl.Get(CharacterData(), "Deaths", "deaths"), out var f) ? (int)f : -1;

        // ProtectionClass.deathInformation.deathInfo: the game's own death text,
        // when it has one. Probed, not trusted: shown beside our analysis.
        public static string GameDeathInfo()
        {
            var prot = Refl.Get(Actor, "protection") ?? Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ProtectionClass"));
            return Refl.GetString(Refl.Get(prot, "deathInformation"), "deathInfo") ?? "";
        }

        // ── Your defences (docs/RESEARCH-player-stats.md) ────────
        // Final values are plain fields on the player's ProtectionClass
        // (2023 dump names; actor + component lookup confirmed for LE 1.4).
        // Resistances are "uncapped"; the cap is a hard 75%. Whether percent
        // stats are stored as 75 or 0.75 is unknown, so the resistances decide
        // the scale and the raw values are logged once for the first launch.
        static readonly (string key, string[] names)[] ResFields =
        {
            ("Res.Physical",  new[] { "uncappedPhysicalResistance" }),
            ("Res.Fire",      new[] { "uncappedFireResistance" }),
            ("Res.Cold",      new[] { "uncappedColdResistance" }),
            ("Res.Lightning", new[] { "uncappedLightningResistance" }),
            ("Res.Necrotic",  new[] { "uncappedNecroticResistance" }),
            ("Res.Void",      new[] { "uncappedVoidResistance" }),
            ("Res.Poison",    new[] { "uncappedPoisonResistance" }),
        };
        static readonly (string key, string[] names)[] PercentFields =
        {
            ("Block",         new[] { "blockChance" }),
            ("Endurance",     new[] { "endurance" }),
            ("CritAvoidance", new[] { "critAvoidance" }),
            ("StunAvoidance", new[] { "stunAvoidance" }),
        };
        static bool _defensesLogged;

        public static Dictionary<string, float> Defenses()
        {
            var prot = Refl.Get(Actor, "protection") ?? Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ProtectionClass"));
            if (prot == null) return null;

            var raw = new Dictionary<string, float>();
            foreach (var (key, names) in ResFields.Concat(PercentFields))
                if (Refl.TryFloat(Refl.Get(prot, names), out var v)) raw[key] = v;

            // Scale from the resistances: any |res| > 1.5 means percent units,
            // all small and some nonzero means fractions; no signal = unknown,
            // and then percent stats are left out rather than guessed.
            var res = raw.Where(kv => kv.Key.StartsWith("Res.")).Select(kv => kv.Value).ToList();
            float scale = res.Any(v => Math.Abs(v) > 1.5f) ? 1f : res.Any(v => v != 0f) ? 100f : float.NaN;

            var def = new Dictionary<string, float>();
            if (!float.IsNaN(scale))
                foreach (var kv in raw)
                    def[kv.Key] = kv.Key.StartsWith("Res.") ? Math.Min(kv.Value * scale, 75f) : kv.Value * scale;

            if (Refl.TryFloat(Refl.Get(prot, "armour", "armor") ?? Refl.Call(prot, "armourForCharacterSheet"), out var armor)) def["Armor"] = armor;
            if (Refl.TryFloat(Refl.Get(prot, "dodgeRating"), out var dodge)) def["Dodge"] = dodge;
            if (Refl.TryFloat(Refl.Get(prot, "enduranceThreshold"), out var et)) def["EnduranceThreshold"] = et;
            if (Refl.TryFloat(Refl.Get(prot, "CurrentWard", "currentWard"), out var ward)) def["Ward"] = ward;
            float max = MaxHealth;
            if (!float.IsNaN(max)) def["MaxHealth"] = max;

            if (!_defensesLogged && (Prefs.ProbeApi.Value || Prefs.DebugLog.Value))
            {
                _defensesLogged = true;
                MelonLoader.MelonLogger.Msg($"[probe] defences raw: {string.Join(", ", raw.Select(kv => $"{kv.Key}={kv.Value}"))}; scale {scale}; stored: {string.Join(", ", def.Select(kv => $"{kv.Key}={kv.Value:0.##}"))}");
            }
            return def.Count > 0 ? def : null;
        }

        public static string Zone()
        {
            try { return Refl.CleanName(SceneManager.GetActiveScene().name) ?? ""; }
            catch { return ""; }
        }

        static object CharacterData() => Refl.Static("Il2Cpp.PlayerFinder", "getPlayerData");
    }
}
