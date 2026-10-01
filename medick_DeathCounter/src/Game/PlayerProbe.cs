using System;
using System.Linq;
using Il2Cpp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace medick_DeathCounter.Game
{
    // The local player: actor, health component, and "who am I" details.
    // PlayerFinder.getPlayerActor() is proven (Righteous Fire); everything
    // past the Actor is looked up by name, see Refl.
    internal static class PlayerProbe
    {
        // Candidate health component types, most specific first.
        public static readonly string[] HealthTypes = { "Il2Cpp.PlayerHealth", "Il2Cpp.BaseHealth", "Il2Cpp.Health" };
        static readonly string[] CurrentHealthNames = { "currentHealth", "CurrentHealth", "health", "Health", "currentHP" };
        static readonly string[] MaxHealthNames     = { "maxHealth", "MaxHealth", "maximumHealth", "healthMax", "maxHP" };

        public static Actor      Actor   { get; private set; }
        public static GameObject Object  { get; private set; }
        public static IntPtr     ObjectPtr { get; private set; }
        public static Component  Health  { get; private set; }
        public static IntPtr     HealthPtr { get; private set; }

        static float _nextRefresh;
        static bool  _healthMissingWarned;

        public static bool HasPlayer => ObjectPtr != IntPtr.Zero;

        // Re-resolve twice a second (menus included: no per-frame lookups).
        // Returns true when the player object changed (zone load, respawn, new character).
        public static bool Refresh(float now)
        {
            if (now < _nextRefresh) return false;
            _nextRefresh = now + 0.5f;

            Actor actor = null;
            try { actor = PlayerFinder.getPlayerActor(); } catch { }
            GameObject go = null;
            try { go = actor != null ? actor.gameObject : null; } catch { }
            IntPtr ptr = Refl.Ptr(go);
            if (ptr == ObjectPtr) return false;

            Actor = actor; Object = go; ObjectPtr = ptr;
            Health = null; HealthPtr = IntPtr.Zero;
            if (go != null)
            {
                foreach (var name in HealthTypes)
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

        // ── Who and where ────────────────────────────────────────
        public static string CharacterName()
        {
            var data = CharacterData();
            return Refl.GetString(data, "CharacterName", "characterName", "charName", "Name", "name")
                ?? Refl.GetString(Actor, "characterName", "CharacterName")
                ?? "Unknown Hero";
        }

        public static string CharacterClass()
        {
            var data = CharacterData();
            var v = Refl.Get(data, "CharacterClass", "characterClass", "classID", "CharacterClassID", "chosenClass");
            if (v == null) return "";
            return Refl.GetString(v, "className", "ClassName", "name") ?? v.ToString();
        }

        public static int Level()
        {
            var data = CharacterData();
            if (Refl.TryFloat(Refl.Get(data, "Level", "level", "CharacterLevel", "characterLevel"), out var f)) return (int)f;
            var exp = Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ExperienceTracker"));
            if (Refl.TryFloat(Refl.Get(exp, "CurrentLevel", "currentLevel", "Level", "level"), out f)) return (int)f;
            if (Refl.TryFloat(Refl.Get(Actor, "level", "Level"), out f)) return (int)f;
            return 0;
        }

        public static string Zone()
        {
            try { return Refl.CleanName(SceneManager.GetActiveScene().name) ?? ""; }
            catch { return ""; }
        }

        // The character's saved data, through whichever PlayerFinder static
        // returns something "CharacterData"-shaped. The candidate list is
        // built once; the call itself happens every few seconds at most.
        static System.Reflection.MethodInfo[] _dataGetters;

        static object CharacterData()
        {
            _dataGetters ??= Refl.FindType("Il2Cpp.PlayerFinder")?
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(m => m.GetParameters().Length == 0 && m.ReturnType.Name.Contains("CharacterData"))
                .ToArray() ?? Array.Empty<System.Reflection.MethodInfo>();
            foreach (var m in _dataGetters)
            {
                try { var v = m.Invoke(null, null); if (v != null) return v; } catch { }
            }
            return null;
        }
    }
}
