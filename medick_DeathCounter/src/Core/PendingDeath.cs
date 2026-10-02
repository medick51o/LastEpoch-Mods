using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Freeze before any loading/respawn clears the live actor and buffers.
    // The only later change is a report whose timing/character can be matched.
    public sealed class PendingDeath
    {
        public double Time { get; }
        public DeathRecord Record { get; }
        public PendingDeath(IReadOnlyList<HitEvent> hits, double time, DeathContext context, IEnumerable<string> ailments, Dictionary<string, float> defenses, double defenseAt, bool liveContext = true)
        {
            Time = time;
            if (!liveContext)
            {
                // A counter increase after respawn proves a death, not where
                // it happened or what today's town stats were during combat.
                context = new DeathContext { Character = context.Character, CharacterClass = context.CharacterClass,
                    Level = context.Level, UtcNow = context.UtcNow, ModVersion = context.ModVersion, Detection = context.Detection };
                hits = null; ailments = null; defenses = null;
            }
            Record = DeathAnalyzer.Analyze(hits, time, context, ailments);
            double age = time - defenseAt;
            if (double.IsFinite(age) && age >= 0 && age <= 2 && defenses != null)
            {
                Record.Defenses = defenses.Where(kv => float.IsFinite(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value);
                Record.DefenseSnapshotAgeSeconds = (float)age;
            }
        }
    }

    public enum ReportTarget { Reject, Pending, Recent, AwaitDeath }
    public static class DeathReportRouting
    {
        public const double ReportWindow = 5;
        public static ReportTarget Choose(double now, string name, double? pendingAt, string pendingCharacter, double? recentAt, string recentCharacter, bool? alive, bool recentStillDead = true)
        {
            if (!double.IsFinite(now)) return ReportTarget.Reject;
            bool Fresh(double? at) => at.HasValue && double.IsFinite(at.Value) && now >= at.Value && now - at.Value <= ReportWindow;
            bool Matches(string expected) => !string.IsNullOrWhiteSpace(expected) && (string.IsNullOrWhiteSpace(name) || name.Trim() == expected);
            if (Fresh(pendingAt) && Matches(pendingCharacter))
            {
                // Neither report signature contains a death sequence ID. If a
                // second death overlaps the first report window, don't guess.
                if (Fresh(recentAt) && recentCharacter == pendingCharacter && recentAt < pendingAt) return ReportTarget.Reject;
                return ReportTarget.Pending;
            }
            if (pendingAt.HasValue) return ReportTarget.Reject;
            if (Fresh(recentAt) && Matches(recentCharacter)) return recentStillDead || alive == true ? ReportTarget.Recent : ReportTarget.Reject;
            return alive == false && !string.IsNullOrWhiteSpace(name) ? ReportTarget.AwaitDeath : ReportTarget.Reject;
        }
    }
}
