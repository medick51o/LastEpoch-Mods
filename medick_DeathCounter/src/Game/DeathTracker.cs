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
    // changes to one whose health reads > 0 (respawn, zone load, character
    // switch). Each recorded death credits the game counter (DeathCountLedger),
    // so the counter catching up later is never a second death.
    internal static class DeathTracker
    {
        const double AilmentMemory  = 6.0;   // ailments seen this recently count as "on you"

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
        static readonly DeathCountLedger _ledger = new();   // reconciles CharacterData.Deaths with our records
        static string _deferred;                            // a death signal that fired mid-hit, see FlushDeferredDeath

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

            FlushDeferredDeath();
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
            int unseen = _ledger.Observe(PlayerProbe.GameDeathCount(), now);
            if (unseen <= 0 || _dead) return;
            Dbg.Log($"game death counter shows {unseen} death(s) no hook or health signal saw");
            OnDeath("game");
        }

        // A death signal that fired INSIDE the killing hit (die() runs within
        // ApplyDamage → HealthDamage) waits until the hit is in the buffer;
        // otherwise a one-shot is recorded with no killer (review #2). The
        // outermost hit postfix flushes it; Update flushes a stray one.
        public static void FlushDeferredDeath()
        {
            if (_deferred == null || GameHooks.InPlayerHit) return;
            var d = _deferred;
            _deferred = null;
            OnDeath(d);
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
            // Re-arm only for a living player (review #5): an actor flicker or
            // swap while dead must not let the closing death screen count again.
            // Unreadable health cannot prove anything either way, so it re-arms.
            // A "player gone" blip (loading, respawn) never re-arms (re-review #1);
            // unreadable health re-arms only well after the death.
            _deferred = null;   // a deferred death belongs to the old player (re-review #5)
            if (PlayerProbe.HasPlayer)
            {
                float hp = PlayerProbe.CurrentHealth;
                if (hp > 0f || (float.IsNaN(hp) && now - _deadAt > 10f)) _dead = false;
            }
            _lastHp = float.NaN;
            _hits.Clear();
            _ailments.Clear();
            _defenses = null;
            _defensesAt = -999f;
            GameHooks.OnPlayerChanged();
            _nextNameCheck = now;   // re-read the name this frame
            Dbg.Log(PlayerProbe.HasPlayer ? "player found" : "player gone");
        }

        static void SetCharacter(string name)
        {
            // The fallback name means CharacterData was momentarily unreadable,
            // not that the character changed: keep the ledger (re-review #4).
            if (name == Character || name == "Unknown Hero" && !string.IsNullOrEmpty(Character)) return;
            Character = name;
            _ledger.Reset();    // re-baseline the game's counter for this character
            CharacterDeaths = Log?.CountFor(name) ?? 0;
            SessionDeaths = SessionCount(name);
        }

        // Defences are snapshotted while you are being hit (at most once a
        // second), not at death: dying may clear buffs and ward first.
        static Dictionary<string, float> _defenses;
        static float _defensesAt = -999f;

        public static void OnHit(HitEvent h)
        {
            _hits.Add(h);
            float now = Time.time;
            if (now - _defensesAt >= 1f)
            {
                _defensesAt = now;
                try { _defenses = PlayerProbe.Defenses(); } catch { _defenses = null; }   // never carry an old life's snapshot (review #5)
            }
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
            if (GameHooks.InPlayerHit) { _deferred ??= detection; return; }

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
                if (now - _defensesAt > 15f) { try { _defenses = PlayerProbe.Defenses(); } catch { } }   // no recent hit: read now
                rec.Defenses = _defenses;
                _defenses = null;
                _defensesAt = -999f;

                Log.Append(rec);
                SetCharacter(rec.Character);
                CharacterDeaths = Log.CountFor(rec.Character);
                _session[rec.Character] = SessionCount(rec.Character) + 1;
                SessionDeaths = SessionCount(rec.Character);
                JustDied   = rec;
                if (detection != "game") _ledger.Recorded(now);
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
