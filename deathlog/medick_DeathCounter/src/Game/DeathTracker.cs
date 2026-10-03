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
        public static ReassessmentLog Reassessments { get; private set; }
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
        static bool _unknownNameWarned;
        static bool _unreadableHealthNoted;
        static string _pendingDetection;
        static bool _pendingCredited;
        static float _pendingAt;
        static DateTime _pendingUtc;
        static bool _committing;
        static DeathDetails _details;
        static float _detailsAt = -999f;
        static string _detailsCharacter;
        static PlayContext _pendingPlayContext, _livingPlayContext;
        static PendingDeath _pendingCapture;
        static string _livingPlayCharacter;
        static float _livingPlayAt = -999f;
        static string _livingZone, _livingScene;
        static int? _livingZoneLevel;
        static string _livingClass;
        static int _livingLevel;
        static bool _livingHardcore;
        static CounterResets _resets;

        public static bool Recording => PlayerProbe.HasPlayer && !_dead;
        public static bool AwaitingDeathDetails => _pendingDetection != null || (_dead && Time.time >= _deadAt && Time.time - _deadAt <= 5f);

        public static void Init(string directory)
        {
            Log = new DeathLog(directory, MelonLogger.Warning);
            Reassessments = new ReassessmentLog(directory, MelonLogger.Warning);
            Reassessments.Load();
            _resets = new CounterResets(System.IO.Path.Combine(directory, "counter-resets.json"), MelonLogger.Warning);
            try { Log.Load(); }
            catch (Exception ex) { MelonLogger.Warning($"could not read the death log ({ex.Message}); starting fresh for this session"); }
        }

        public static void Update()
        {
            float now = Time.time;
            if (PlayerProbe.Refresh(now)) OnPlayerChanged(now);

            if (PlayerProbe.HasPlayer && now >= _nextNameCheck)
            {
                _nextNameCheck = now + 5f;
                SetCharacter(PlayerProbe.CharacterName());
            }
            DeathReportHooks.Poll();

            // The game counter and a deferred death do not need a resolved
            // player object. A missing actor must not silence them.
            FlushDeferredDeath();
            if (_pendingDetection != null && now - _pendingAt >= 0.75f)
            {
                float commitHp = PlayerProbe.CurrentHealth;
                if (DeathCommitGate.CancelBecauseAlive(_pendingDetection, commitHp))
                {
                    if (_pendingCredited) _ledger.RetractLatest();
                    _pendingCredited = false;
                    _pendingDetection = null;
                    _pendingCapture = null;
                    _pendingPlayContext = null;
                }
                else
                {
                    string detection = _pendingDetection;
                    _committing = true;
                    try { if (_pendingCapture != null) OnDeath(detection); }
                    finally { _committing = false; _pendingDetection = null; _pendingCapture = null; _pendingPlayContext = null; _pendingCredited = false; }
                }
            }
            if (now >= _nextCountCheck)
            {
                _nextCountCheck = now + 0.5f;
                PollGameDeathCount(now);
            }

            if (!PlayerProbe.HasPlayer) return;

            float hp = PlayerProbe.CurrentHealth;
            if (float.IsNaN(hp)) return;
            if (hp > 0f && !_dead && _pendingDetection == null && now - _defensesAt >= 0.5f)
                SampleDefenses(now);
            if (!_dead && _lastHp > 0f && hp <= 0f) OnDeath("health");
            else if (_dead && hp > 0f && now - _deadAt > 1f) _dead = false;   // respawned in place
            _lastHp = hp;
        }

        static void PollGameDeathCount(float now)
        {
            int count = PlayerProbe.GameDeathCount();
            if (count < 0) return;   // needs readable CharacterData, not a player object
            int unseen = _ledger.Observe(count, now);
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

        // Death screen / analytics: no owner. Not gated on HasPlayer.
        // Readable health above 0 is never a death. Unreadable health waits
        // for CharacterData.Deaths instead of guessing.
        public static void OnUnownedDeathSignal()
        {
            float hp = PlayerProbe.CurrentHealth;
            if (!float.IsNaN(hp) && hp > 0f)
            {
                Dbg.Log($"unowned death signal ignored (health {hp:0})");
                return;
            }
            if (float.IsNaN(hp))
            {
                if (!_unreadableHealthNoted)
                {
                    _unreadableHealthNoted = true;
                    MelonLogger.Msg("unowned death signal with unreadable health; waiting for the game death counter");
                }
                return;
            }
            OnDeath("hook");
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
            if (_pendingCapture == null) { _details = null; _detailsAt = -999f; }
            _defenses = null;
            _defensesAt = -999f;
            _livingPlayContext = null;
            _livingPlayCharacter = null;
            _livingPlayAt = -999f;
            _livingZone = null; _livingScene = null; _livingZoneLevel = null;
            GameHooks.OnPlayerChanged();
            _nextNameCheck = now;   // re-read the name this frame
            Dbg.Log(PlayerProbe.HasPlayer ? "player found" : "player gone");
        }

        static bool RealName(string name) =>
            !string.IsNullOrWhiteSpace(name) && name.Trim() != "Unknown Hero";

        static void SetCharacter(string name)
        {
            // A missing name, or the old "Unknown Hero" placeholder, is a
            // CharacterData hiccup. Keep the last real name and its ledger.
            if (!RealName(name) || name.Trim() == Character) return;
            name = name.Trim();
            Character = name;
            _ledger.Reset();    // re-baseline the game's counter for this character
            CharacterDeaths = _resets?.Count(name, Log?.CountFor(name) ?? 0) ?? 0;
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
            if (PlayerProbe.CurrentHealth > 0f && now - _defensesAt >= 0.5f) SampleDefenses(now);
            Dbg.Log($"hit {h.Amount:0} from {h.Source ?? "?"} {(h.Ability != null ? "(" + h.Ability + ")" : "")} {h.Ailment ?? ""} hp {h.HealthBefore:0}/{h.MaxHealth:0}");
        }

        static void SampleDefenses(float now)
        {
            _defensesAt = now;
            try { _defenses = PlayerProbe.Defenses(); }
            catch { _defenses = null; }
            string name = PlayerProbe.CharacterName()?.Trim();
            if (RealName(name))
            {
                _livingPlayContext = PlayerProbe.PlayContext(name);
                _livingPlayCharacter = name;
                _livingPlayAt = now;
                _livingZone = PlayerProbe.Zone();
                _livingScene = PlayerProbe.RawSceneId();
                _livingZoneLevel = PlayerProbe.ZoneLevel();
                _livingClass = PlayerProbe.CharacterClass();
                _livingLevel = PlayerProbe.Level();
                _livingHardcore = PlayerProbe.Hardcore();
            }
        }

        public static void OnAilment(string name)
        {
            _ailments[name] = Time.time;
            Dbg.Log("ailment on you: " + name);
        }

        public static void OnDeath(string detection)
        {
            if (_dead && !_committing) return;
            // The game counter is the safety net when no player object exists.
            // A hook still needs either a player or health we already accepted.
            if (!_committing && detection != "game" && !PlayerProbe.HasPlayer && PlayerProbe.Health == null) return;
            if (GameHooks.InPlayerHit) { _deferred ??= detection; return; }

            // A hooked "Die" must agree with health when we can read it, so a
            // mis-guessed hook can never invent deaths. Unreadable health is
            // not counted here; the game counter has to move.
            float hp = PlayerProbe.CurrentHealth;
            if (!_committing && detection == "hook" && !float.IsNaN(hp) && hp > 0f)
            {
                Dbg.Log($"death hook fired with health {hp:0}; ignored");
                return;
            }
            if (!_committing && detection == "hook" && float.IsNaN(hp))
            {
                if (!_unreadableHealthNoted)
                {
                    _unreadableHealthNoted = true;
                    MelonLogger.Msg("death hook with unreadable health; waiting for the game death counter");
                }
                return;
            }

            string raw = PlayerProbe.CharacterName();
            string name = _committing ? _pendingCapture?.Record.Character : RealName(raw) ? raw.Trim() : (RealName(Character) ? Character : null);
            if (name == null)
            {
                if (!_unknownNameWarned)
                {
                    _unknownNameWarned = true;
                    MelonLogger.Warning("death not recorded: character name is unreadable, and it will not be filed as Unknown Hero");
                }
                return;
            }

            float now = Time.time;
            // The server report can arrive after the health update. Give it a
            // short window, retaining the original death time and hit cutoff.
            if (!_committing)
            {
                if (_pendingDetection == null)
                {
                    _pendingDetection = detection;
                    FreezePending(name, hp, now, true);
                }
                else
                {
                    var merged = DeathSignalMerge.Combine(_pendingDetection, detection);
                    _pendingDetection = merged.Source;
                    if (merged.Rebuild) FreezePending(name, hp, now, false);
                }
                return;
            }
            now = _pendingAt;
            _dead = true;
            _deadAt = now;

            try
            {
                var rec = _pendingCapture.Record;
                if (_details != null && _detailsCharacter == name && Math.Abs(_detailsAt - now) <= 5f)
                    _details.Apply(rec);
                // Do not present post-death stats as the player's defenses in
                // combat. Live snapshots also work when no client hit fires.
                _defenses = null;
                _defensesAt = -999f;

                Log.Append(rec);
                _session[rec.Character] = SessionCount(rec.Character) + 1;
                string current = PlayerProbe.CharacterName();
                if (RealName(current)) SetCharacter(current);
                else if (!RealName(Character)) SetCharacter(rec.Character);
                if (Character == rec.Character)
                {
                    CharacterDeaths = _resets.Count(rec.Character, Log.CountFor(rec.Character));
                    SessionDeaths = SessionCount(rec.Character);
                }
                JustDied   = rec;
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
                _details = null;
                _pendingPlayContext = null;
                _pendingCapture = null;
            }
        }

        // First signal freezes the death time. A later signal in the same
        // window can refresh the hit list without moving that time, and it
        // does not add a second counter credit.
        static void FreezePending(string name, float hp, float now, bool fresh)
        {
            if (fresh)
            {
                _pendingAt = now;
                _pendingUtc = DateTime.UtcNow;
            }
            string detection = _pendingDetection;
            bool lateCounter = detection == "game" && (!float.IsFinite(hp) || hp > 0);
            bool living = !lateCounter && _livingPlayCharacter == name && now >= _livingPlayAt && now - _livingPlayAt <= 2f;
            _pendingPlayContext = lateCounter ? null : living && _livingPlayContext != null
                ? _livingPlayContext.Copy() : PlayerProbe.PlayContext(name);
            float max = lateCounter ? -1 : _defenses != null && now >= _defensesAt && now - _defensesAt <= 2f && _defenses.TryGetValue("MaxHealth", out float livingMax)
                ? livingMax : PlayerProbe.MaxHealth;
            var context = new DeathContext
            {
                Character = name, CharacterClass = living ? _livingClass : PlayerProbe.CharacterClass(), Level = living ? _livingLevel : PlayerProbe.Level(),
                Zone = lateCounter ? "" : living ? _livingZone : PlayerProbe.Zone(),
                RawSceneId = lateCounter ? null : living ? _livingScene : PlayerProbe.RawSceneId(),
                ZoneLevel = lateCounter ? null : living ? _livingZoneLevel : PlayerProbe.ZoneLevel(),
                Hardcore = !lateCounter && (living ? _livingHardcore : PlayerProbe.Hardcore()), PlayContext = _pendingPlayContext,
                MaxHealth = float.IsFinite(max) ? max : -1,
                Detection = detection, ModVersion = BuildInfo.Version, UtcNow = _pendingUtc,
            };
            _pendingCapture = new PendingDeath(lateCounter ? null : _hits.Since(now - 12), fresh ? now : _pendingAt, context,
                lateCounter ? null : _ailments.Where(kv => now >= kv.Value && now - kv.Value <= AilmentMemory).Select(kv => kv.Key), lateCounter ? null : _defenses, _defensesAt, !lateCounter);
            if (fresh && detection != "game" && Character == name)
            {
                _ledger.Recorded(now);
                _pendingCredited = true;
            }
        }

        public static void OnDeathDetails(DeathDetails details)
        {
            if (details == null) return;
            string name = PlayerProbe.CharacterName();
            name = RealName(name) ? name.Trim() : null;
            float health = PlayerProbe.CurrentHealth;
            bool? alive = float.IsFinite(health) ? health > 0 : null;
            double? recentAt = JustDied != null ? _deadAt : null;
            var target = DeathReportRouting.Choose(Time.time, name, _pendingCapture?.Time, _pendingCapture?.Record.Character, recentAt, JustDied?.Character, alive, _dead);
            if (target == ReportTarget.Reject) { Dbg.Log("death details could not be matched safely; ignored"); return; }
            if (target == ReportTarget.Recent)
            {
                details.Apply(JustDied);
                Log.SaveUpdated();
                return;
            }
            if (_detailsCharacter == (target == ReportTarget.Pending ? _pendingCapture.Record.Character : name)
                && Time.time >= _detailsAt && Time.time - _detailsAt <= 1f)
                details.PreserveBossFlagFrom(_details);
            _details = details;
            _detailsAt = Time.time;
            _detailsCharacter = target == ReportTarget.Pending ? _pendingCapture.Record.Character : name;
            // A later packet enriches the existing death; it never adds a count.
            if (target == ReportTarget.AwaitDeath) OnUnownedDeathSignal();
        }

        static int SessionCount(string character) => _session.TryGetValue(character, out var n) ? n : 0;

        public static bool ResetCounter()
        {
            if (!RealName(Character) || Log == null || _pendingDetection != null) return false;
            if (!_resets.Reset(Character, Log.CountFor(Character))) return false;
            CharacterDeaths = 0;
            _session[Character] = 0;
            SessionDeaths = 0;
            return true;
        }

        public static int SessionFor(string character) => SessionCount(character);
    }
}
