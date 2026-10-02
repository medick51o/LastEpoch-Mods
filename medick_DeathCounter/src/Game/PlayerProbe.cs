using System;
using System.Collections.Generic;
using System.Linq;
using medick_DeathCounter.Core;
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

        static float _nextRefresh;
        static string _lastDiag;
        static float _unresolvedSince = -1f;

        // A resolved object with readable character data. Char-select previews
        // have a model and no CharacterData, so the counter and the panel stay down.
        public static bool HasPlayer { get; private set; }

        // Re-resolve twice a second, including while health is still missing.
        // Returns true when the player object (or whether we are tracking) changed.
        public static bool Refresh(float now)
        {
            if (now < _nextRefresh) return false;
            _nextRefresh = now + 0.5f;

            Component actor = null, lph = null;
            GameObject go = null;
            string path = null;
            var actorRead = new Refl.StaticRead { Detail = "not asked" };
            var healthRead = new Refl.StaticRead { Detail = "not asked" };
            var multiRead = new Refl.StaticRead { Detail = "not asked" };
            var playerRead = new Refl.StaticRead { Detail = "not asked" };
            try
            {
                // 1. Proven actor. 2. Proven health, its actor, that actor's object.
                // Unproven names stay behind those two. Health is kept even when
                // the object route fails, and a missing health is tried again
                // next pass (this method is not skipped while Health is null).
                actorRead = Refl.ReadStatic("Il2Cpp.PlayerFinder", "getPlayerActor");
                healthRead = Refl.ReadStatic("Il2Cpp.PlayerFinder", "getLocalPlayerHealth");
                actor = actorRead.Value as Component;
                lph = healthRead.Value as Component;
                if (actor != null) path = "getPlayerActor";
                else if (lph != null)
                {
                    if (Refl.Get(lph, "actor", "Actor") is Component fromHealth) actor = fromHealth;
                    path = "getLocalPlayerHealth";
                }
                if (actor != null)
                {
                    try { go = actor.gameObject; } catch { }
                }
                if (go == null)
                {
                    multiRead = Refl.ReadStatic("Il2Cpp.PlayerFinder", "getLocalPlayerInMultiplayer");
                    if (multiRead.Value is Component local)
                    {
                        try { go = local.gameObject; path ??= "getLocalPlayerInMultiplayer"; } catch { }
                    }
                    if (go == null)
                    {
                        playerRead = Refl.ReadStatic("Il2Cpp.PlayerFinder", "getPlayer");
                        if (playerRead.Value is GameObject g) { go = g; path ??= "getPlayer"; }
                    }
                }
                if (actor == null && go != null)
                    actor = Refl.GetComponent(go, ActorType) ?? Refl.GetComponentInChildren(go, ActorType);
                if (lph == null && go != null)
                {
                    foreach (var name in HealthTypes)
                    {
                        var t = Refl.FindType(name);
                        lph = Refl.GetComponent(go, t) ?? Refl.GetComponentInChildren(go, t);
                        if (lph != null) { path ??= "children"; break; }
                    }
                }
                if (actor != null)
                {
                    try { var anchored = actor.gameObject; if (anchored != null) go = anchored; } catch { }
                }
                else if (go == null && lph != null)
                {
                    try { go = lph.gameObject; path ??= "getLocalPlayerHealth"; } catch { }
                }
            }
            catch (Exception ex)
            {
                Diag("player resolve failed: " + ex.Message, true);
                return false;
            }

            IntPtr ptr = Refl.Ptr(go);
            bool data = !string.IsNullOrWhiteSpace(CharacterName());
            // Character data is what separates a loaded character from a menu
            // preview. Health alone is enough to track when that data is readable.
            bool present = data && (ptr != IntPtr.Zero || lph != null);
            // Health can show up a pass later. Adopting it must not look like a
            // new player (that clears the hit buffer).
            bool objectChanged = ptr != ObjectPtr || present != HasPlayer;
            if (!objectChanged)
            {
                if (lph != null && (Health == null || Refl.Ptr(lph) != HealthPtr))
                    AdoptHealth(lph);
                if (!present) ReportWaiting(now, data || lph != null, Failure(actorRead, healthRead, multiRead, playerRead, data));
                return false;
            }

            if (ptr != ObjectPtr)
            {
                Health = null;
                HealthPtr = IntPtr.Zero;
            }
            if (lph != null) AdoptHealth(lph);
            Actor = actor;
            Object = go;
            ObjectPtr = ptr;
            HasPlayer = present;
            if (present) _unresolvedSince = -1f;
            if (present)
                Diag($"player resolved via {path ?? "none"}: object={go?.name ?? "none"}, actor={actor?.GetType().FullName ?? "none"}, health={Health?.GetType().FullName ?? "none"}, character=yes", false);
            else
                ReportWaiting(now, data || lph != null, Failure(actorRead, healthRead, multiRead, playerRead, data));
            return true;
        }

        // The hit hook calls this with the component it saw take player damage.
        public static void AdoptHealth(Component c)
        {
            if (c == null) return;
            IntPtr p = Refl.Ptr(c);
            bool changed = Health == null || p != HealthPtr;
            Health = c;
            HealthPtr = p;
            if (changed)
                MelonLoader.MelonLogger.Msg($"player health adopted: {c.GetType().FullName}");
        }

        public static void Reset()
        {
            Actor = null; Object = null; ObjectPtr = IntPtr.Zero;
            Health = null; HealthPtr = IntPtr.Zero;
            HasPlayer = false;
            _nextRefresh = 0f;
        }

        static string Failure(Refl.StaticRead actor, Refl.StaticRead health, Refl.StaticRead multi, Refl.StaticRead player, bool data)
        {
            string why = !data && (actor.Value != null || health.Value != null)
                ? "player object seen without character data (menu or preview); not tracking. "
                : "";
            return why + "player not resolved:"
                + $" getPlayerActor={actor.Detail}"
                + $" getLocalPlayerHealth={health.Detail}"
                + $" getLocalPlayerInMultiplayer={multi.Detail}"
                + $" getPlayer={player.Detail}";
        }

                static void ReportWaiting(float now, bool playerEvidence, string detail)
        {
            if (!playerEvidence) _unresolvedSince = -1f;
            else if (_unresolvedSince < 0f) _unresolvedSince = now;
            if (playerEvidence && now - _unresolvedSince >= 30f)
                Diag("Death tracking is not active yet: character detection has not completed. " + detail, true);
            else
            {
                // Menus and normal loading are expected; diagnostic details are
                // available only when the owner enables DebugLog.
                string line = "Waiting for your character to load (normal during menus/loading). " + detail;
                if (line == _lastDiag) return;
                _lastDiag = line;
                Dbg.Log(line);
            }
        }

        static void Diag(string line, bool warning)
        {
            if (string.IsNullOrEmpty(line) || line == _lastDiag) return;
            _lastDiag = line;
            if (warning) MelonLoader.MelonLogger.Warning(line);
            else MelonLoader.MelonLogger.Msg(line);
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
        // Null when CharacterData has no name. Callers keep the last real name
        // instead of stamping the shared "Unknown Hero" placeholder.
        public static string CharacterName() =>
            Refl.GetString(CharacterData(), "CharacterName", "characterName")
            ?? Refl.GetString(Refl.Get(Refl.Static("Il2Cpp.PlayerFinder", "getLocalPlayerInMultiplayer"), "localPlayerInfo"), "characterName", "CharacterName");

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

        public static PlayContext PlayContext(string expectedCharacter) => PlayContextProbe.Read(CharacterData(), expectedCharacter);

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
        // stats are stored as 75 or 0.75 is unknown: DefenseSnapshot decides
        // from evidence or reports nothing, and the raw values are logged once
        // for the first launch.
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
            ("ReducedBonusCritDamage", new[] { "reducedBonusDamageTakenFromCrits" }),
            ("StunAvoidance", new[] { "stunAvoidance" }),
        };
        static bool _defensesLogged;

        public static bool TryCurrentDefenses(string expectedCharacter, out Dictionary<string, float> stats, out string reason)
        {
            stats = null;
            reason = "Load this character and finish respawning before checking current stats.";
            try
            {
                if (!HasPlayer || CurrentHealth <= 0 || !float.IsFinite(CurrentHealth) || CharacterName()?.Trim() != expectedCharacter) return false;
                var read = Defenses();
                if (!HasPlayer || CurrentHealth <= 0 || !float.IsFinite(CurrentHealth) || CharacterName()?.Trim() != expectedCharacter) return false;
                if (read == null || read.Count == 0) { reason = "Current defenses could not be read. Try again once the character has loaded."; return false; }
                stats = read;
                reason = null;
                return true;
            }
            catch { reason = "Current stats are temporarily unavailable. Try again once the character has loaded."; return false; }
        }

        public static Dictionary<string, float> Defenses()
        {
            var prot = Refl.Static("Il2Cpp.PlayerFinder", "getLocalPlayerPrecalculatedStatsHolder")
                ?? Refl.Get(Actor, "protection") ?? Refl.GetComponent(Object, Refl.FindType("Il2Cpp.ProtectionClass"));
            if (prot == null) return null;

            var raw = new Dictionary<string, float>();
            foreach (var (key, names) in ResFields.Concat(PercentFields))
                if (Refl.TryFloat(Refl.Get(prot, names), out var v)) raw[key] = v;

            // Scale (75 vs 0.75) and the cap live in Core/DefenseSnapshot,
            // where they are unit-tested; unknown scale = no percent stats.
            if (Refl.TryFloat(Refl.Call(prot, "getArmour") ?? Refl.Get(prot, "armour", "armor") ?? Refl.Call(prot, "armourForCharacterSheet"), out var armor)) raw["Armor"] = armor;
            if (Refl.TryFloat(Refl.Get(prot, "dodgeRating"), out var dodge)) raw["Dodge"] = dodge;
            if (Refl.TryFloat(Refl.Get(prot, "blockProtection"), out var blockEffect)) raw["BlockEffectiveness"] = blockEffect;
            if (Refl.TryFloat(Refl.Get(Refl.Static("Il2Cpp.DifficultyManager", "get_instance"), "areaLevel"), out var area) && area > 0f) raw["AreaLevel"] = area;
            if (Refl.TryFloat(Refl.Call(prot, "getEnduranceThreshold") ?? Refl.Get(prot, "enduranceThreshold"), out var et)) raw["EnduranceThreshold"] = et;
            if (Refl.TryFloat(Refl.Get(Refl.Static("Il2Cpp.PlayerFinder", "getLocalPlayerWardHolder"), "CurrentWard", "currentWard") ?? Refl.Get(prot, "CurrentWard", "currentWard"), out var ward)) raw["Ward"] = ward;
            float max = MaxHealth;
            if (!float.IsNaN(max)) raw["MaxHealth"] = max;
            var def = DefenseSnapshot.Normalize(raw);

            if (!_defensesLogged && (Prefs.ProbeApi.Value || Prefs.DebugLog.Value))
            {
                _defensesLogged = true;
                MelonLoader.MelonLogger.Msg($"[probe] defences raw: {string.Join(", ", raw.Select(kv => $"{kv.Key}={kv.Value}"))}; scale {DefenseSnapshot.Scale(raw)?.ToString() ?? "unknown"}; stored: {string.Join(", ", def.Select(kv => $"{kv.Key}={kv.Value:0.##}"))}");
            }
            return def.Count > 0 ? def : null;
        }

        public static string Zone()
        {
            try { return Refl.CleanName(SceneManager.GetActiveScene().name) ?? ""; }
            catch { return ""; }
        }

        public static string RawSceneId()
        {
            try { return SceneManager.GetActiveScene().name; }
            catch { return null; }
        }
        public static int? ZoneLevel()
        {
            return Refl.Static("Il2Cpp.ZoneInfoManager", "get_ZoneLevel") is int level && level > 0 ? level : null;
        }

        static object CharacterData() => Refl.Static("Il2Cpp.PlayerFinder", "getPlayerData")
            ?? Refl.Get(Refl.Static("Il2Cpp.PlayerFinder", "getPlayerDataTracker"), "charData")
            ?? Refl.Get(Refl.Get(Refl.Static("Il2Cpp.PlayerFinder", "getLocalTreeData"), "dataTracker"), "charData");
    }
}



