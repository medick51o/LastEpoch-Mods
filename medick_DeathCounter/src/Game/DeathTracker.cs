using System;
using System.Collections.Generic;
using System.Linq;
using medick_DeathCounter.Core;
using MelonLoader;
using UnityEngine;

namespace medick_DeathCounter.Game
{
    // Owns "did the player just die, and what did it": the rolling hit buffer,
    // recent ailments, death detection, and writing the record.
    //
    // Independent death signals, first one wins, one record per death:
    //   hook    Dying.die / ActorSync.receiveDeath / ActorVisuals.Die on the
    //           player, a hit taking health to 0, or an unowned signal (death
    //           screen, analytics) while health reads <= 0
    //   health  this Update loop seeing health cross from > 0 to <= 0
    //   game    the game's own CharacterData.Deaths going up (works even if
    //           every hook name is wrong; also the online-play fallback)
    // The latch re-arms when health is back above 0 or the player object
    // changes (respawn, zone load, character switch). A Deaths increase
    // within GameCountGrace of a recorded death is that same death.
    internal static class DeathTracker
    {
        const double AilmentMemory  = 6.0;   // ailments seen this recently count as "on you"
        const float  GameCountGrace = 60f;   // seconds a Deaths++ may lag the death we already recorded

        public static DeathLog Log { get; private set; }
        public static string   Character { get; private set; } = "";
        public static int      CharacterDeaths { get; private set; }
        public static int      SessionDeaths { get; private set; }
        public static DeathRecord JustDied { get; private set; }
        public static float    JustDiedAt { get; private set; } = -999f;

        static readonly HitBuffer _hits = new(12.0);
        static readonly Dictionary<string, double> _ailments = new();
        static readonly Dictionary<string, int> _session = new();
        static bool  _dead;
        static float _deadAt;
        static float _lastHp = float.NaN;
        static float _nextNameCheck;
        static float _nextCountCheck;
        static int   _gameDeaths = -1;       // last CharacterData.Deaths seen for Character
        static float _recordedAt = -999f;

        public static bool Recording => Prefs.Tracking.Value && PlayerProbe.HasPlayer && !_dead;

        public static void Init(string directory)
        {
            Log = new DeathLog(directory, MelonLogger.Warning);
            try { Log.Load(); }
            catch (Exception ex) { MelonLogger.Warning($"could not read the death log ({ex.Message}); starting fresh for this session"); }
        }

        public static void Update()
        {
            float now = Time.time;
            if (PlayerProbe.Refresh(now)) OnPlayerChanged(now);
            if (!PlayerProbe.HasPlayer) return;

            if (now >= _nextNameCheck)
            {
                _nextNameCheck = now + 5f;
                SetCharacter(PlayerProbe.CharacterName());
            }

            if (now >= _nextCountCheck)
            {
                _nextCountCheck = now + 0.5f;
                PollGameDeathCount(now);
            }

            float hp = PlayerProbe.CurrentHealth;
            if (float.IsNaN(hp)) return;
            if (!_dead && _lastHp > 0f && hp <= 0f) OnDeath("health");
            else if (_dead && hp > 0f && now - _deadAt > 1f) _dead = false;   // respawned in place
            _lastHp = hp;
        }

        static void PollGameDeathCount(float now)
        {
            int n = PlayerProbe.GameDeathCount();
            if (n < 0) return;
            int before = _gameDeaths;
            _gameDeaths = n;
            if (before < 0 || n <= before) return;               // first read, or no change
            if (_dead || now - _recordedAt < GameCountGrace) return;   // the death already recorded
            Dbg.Log($"game death count {before} -> {n} with no hook or health signal");
            OnDeath("game");
        }

        // Death screen / analytics: no owner, so only when health agrees.
        public static void OnUnownedDeathSignal()
        {
            float hp = PlayerProbe.CurrentHealth;
            if (!float.IsNaN(hp) && hp <= 0f) OnDeath("hook");
            else Dbg.Log($"unowned death signal ignored (health {hp})");
        }

        static void OnPlayerChanged(float now)
        {
            _dead = false;
            _lastHp = float.NaN;
            _hits.Clear();
            _ailments.Clear();
            GameHooks.OnPlayerChanged();
            _nextNameCheck = now;   // re-read the name this frame
            Dbg.Log(PlayerProbe.HasPlayer ? "player found" : "player gone");
        }

        static void SetCharacter(string name)
        {
            if (name == Character) return;
            Character = name;
            _gameDeaths = -1;   // re-baseline the game's counter for this character
            CharacterDeaths = Log?.CountFor(name) ?? 0;
            SessionDeaths = SessionCount(name);
        }

        public static void OnHit(HitEvent h)
        {
            _hits.Add(h);
            Dbg.Log($"hit {h.Amount:0} from {h.Source ?? "?"} {(h.Ability != null ? "(" + h.Ability + ")" : "")} {h.Ailment ?? ""} hp {h.HealthBefore:0}/{h.MaxHealth:0}");
        }

        public static void OnAilment(string name)
        {
            _ailments[name] = Time.time;
            Dbg.Log("ailment on you: " + name);
        }

        public static void OnDeath(string detection)
        {
            if (_dead || !Prefs.Tracking.Value || !PlayerProbe.HasPlayer) return;

            // A hooked "Die" must agree with health when we can read it, so a
            // mis-guessed hook can never invent deaths.
            float hp = PlayerProbe.CurrentHealth;
            if (detection == "hook" && !float.IsNaN(hp) && hp > 0f)
            {
                Dbg.Log($"death hook fired with health {hp:0}; ignored");
                return;
            }

            float now = Time.time;
            _dead = true;
            _deadAt = now;

            try
            {
                float max = PlayerProbe.MaxHealth;
                var ctx = new DeathContext
                {
                    Character      = PlayerProbe.CharacterName(),
                    CharacterClass = PlayerProbe.CharacterClass(),
                    Level          = PlayerProbe.Level(),
                    Zone           = PlayerProbe.Zone(),
                    Hardcore       = PlayerProbe.Hardcore(),
                    GameDeathInfo  = PlayerProbe.GameDeathInfo(),
                    MaxHealth      = float.IsNaN(max) ? -1f : max,
                    Detection      = detection,
                    ModVersion     = BuildInfo.Version,
                    UtcNow         = DateTime.UtcNow,
                };
                var ailments = _ailments.Where(kv => now - kv.Value <= AilmentMemory).Select(kv => kv.Key).ToList();
                var rec = DeathAnalyzer.Analyze(_hits.Since(now - 12.0), now, ctx, ailments);

                Log.Append(rec);
                SetCharacter(rec.Character);
                CharacterDeaths = Log.CountFor(rec.Character);
                _session[rec.Character] = SessionCount(rec.Character) + 1;
                SessionDeaths = SessionCount(rec.Character);
                JustDied   = rec;
                _recordedAt = now;
                int gd = PlayerProbe.GameDeathCount();
                if (gd >= 0) _gameDeaths = gd;
                JustDiedAt = Time.unscaledTime;

                MelonLogger.Msg("death: " + rec.ToLogLine());
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("could not record this death: " + ex.Message);
            }
            finally
            {
                _hits.Clear();
                _ailments.Clear();
            }
        }

        static int SessionCount(string character) => _session.TryGetValue(character, out var n) ? n : 0;

        public static int SessionFor(string character) => SessionCount(character);
    }
}
