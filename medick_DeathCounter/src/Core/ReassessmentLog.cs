using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace medick_DeathCounter.Core
{
    public sealed class SavedAdvice
    {
        public string Key { get; set; }
        public string Group { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public Advice ToAdvice() => new() { Key = Key, Group = Group, Title = Title, Body = Body };
    }
    public sealed class AssessmentSnapshot
    {
        public string Message { get; set; }
        public List<string> Changes { get; set; } = new();
        public List<SavedAdvice> Actions { get; set; } = new();
        public ReadinessRating Rating { get; set; }
        public static AssessmentSnapshot From(CurrentAssessment result) => new()
        {
            Message = result.Message, Changes = result.Changes.ToList(),
            Actions = result.Actions.Select(a => new SavedAdvice { Key = a.Key, Group = a.Group, Title = a.Title, Body = a.Body }).ToList(),
            Rating = result.Rating,
        };
    }
    public sealed class ReassessmentUpdate
    {
        public int Schema { get; set; } = 1;
        public string Id { get; set; }
        public string DeathKey { get; set; }
        public string PreviousUpdateId { get; set; }
        public DateTime UtcTime { get; set; }
        public string Algorithm { get; set; } = "defense-progress-v1";
        public DeathRecord OriginalCapture { get; set; }
        public Dictionary<string, float> CurrentStats { get; set; }
        public AssessmentSnapshot Assessment { get; set; }
        public List<string> SincePrevious { get; set; } = new();
    }

    // Separate history: these records never enter deaths.jsonl, the death count
    // or Patterns. Every successful click creates a new immutable update.
    public sealed class ReassessmentLog
    {
        public const string FileName = "reassessments.jsonl";
        internal static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };
        readonly string _path;
        readonly Action<string> _warn;
        readonly List<ReassessmentUpdate> _all = new();
        readonly List<string> _unreadable = new();
        bool _loaded;
        public int Revision { get; private set; }
        public IReadOnlyList<ReassessmentUpdate> All => _all;
        public ReassessmentLog(string directory, Action<string> warn = null) { _path = Path.Combine(directory, FileName); _warn = warn ?? (_ => { }); }

        public static string KeyFor(DeathRecord death)
        {
            if (death == null) return null;
            if (!string.IsNullOrWhiteSpace(death.Id)) return "id:" + death.Id;
            // Late killer details and current realm are deliberately excluded.
            string source = JsonSerializer.Serialize(new { death.Character, death.Number, Ticks = death.UtcTime.Ticks });
            return "legacy:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
        }
        public IEnumerable<ReassessmentUpdate> For(DeathRecord death)
        {
            string key = KeyFor(death);
            return _all.Where(u => u.DeathKey == key);
        }
        public void Load()
        {
            _loaded = false;
            var next = new List<ReassessmentUpdate>();
            var unreadable = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                if (File.Exists(_path))
                    foreach (var line in File.ReadLines(_path))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var update = JsonSerializer.Deserialize<ReassessmentUpdate>(line, Json);
                            if (update?.Schema == 1 && !string.IsNullOrWhiteSpace(update.Id) && !string.IsNullOrWhiteSpace(update.DeathKey)
                                && update.OriginalCapture != null && update.DeathKey == KeyFor(update.OriginalCapture) && update.CurrentStats != null
                                && update.Assessment?.Rating != null && update.Assessment.Changes != null && update.Assessment.Actions != null
                                && update.Assessment.Actions.All(a => a != null && !string.IsNullOrWhiteSpace(a.Key) && a.Title != null && a.Body != null)
                                && ids.Add(update.Id)) next.Add(update);
                            else unreadable.Add(line);
                        }
                        catch { unreadable.Add(line); }
                    }
                _all.Clear(); _all.AddRange(next);
                _unreadable.Clear(); _unreadable.AddRange(unreadable);
                _loaded = true; Revision++;
                if (unreadable.Count > 0) _warn($"{unreadable.Count} unreadable reassessment line(s) skipped and retained on disk");
            }
            catch (Exception ex) { _warn("could not read reassessments: " + ex.Message); }
        }
        public bool Save(DeathRecord death, string character, IReadOnlyDictionary<string, float> stats, CurrentAssessment result, ReassessmentUpdate previous, out ReassessmentUpdate saved)
        {
            saved = null;
            if (!_loaded || result?.Available != true || death == null || death.Character != character?.Trim()) return false;
            string key = KeyFor(death);
            if (previous != null && (previous.DeathKey != key || !_all.Any(u => u.Id == previous.Id))) return false;
            try
            {
                var update = new ReassessmentUpdate
                {
                    Id = Guid.NewGuid().ToString("N"), DeathKey = key, PreviousUpdateId = previous?.Id,
                    UtcTime = DateTime.UtcNow, OriginalCapture = Clone(death),
                    CurrentStats = CurrentAssessment.ReadableStats(stats),
                    Assessment = Clone(AssessmentSnapshot.From(result)),
                };
                if (previous != null)
                    foreach (var kv in update.CurrentStats.Where(kv => kv.Key != "AreaLevel" && previous.CurrentStats.TryGetValue(kv.Key, out float old) && old != kv.Value).OrderBy(kv => kv.Key))
                        update.SincePrevious.Add($"{StatLabel(kv.Key)}: {previous.CurrentStats[kv.Key]:0.##} → {kv.Value:0.##}" + (kv.Key.StartsWith("Res.") || kv.Key.StartsWith("ResUncapped.") || kv.Key == "CritAvoidance" || kv.Key == "ReducedBonusCritDamage" || kv.Key == "Block" || kv.Key == "Endurance" ? " percent" : ""));
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                // Rewrite atomically to prevent a prior torn tail swallowing a
                // fresh appended update; retain unknown/corrupt lines verbatim.
                Write(_all.Append(update));
                _all.Add(update); Revision++;
                saved = update;
                return true;
            }
            catch (Exception ex) { _warn("could not save reassessment: " + ex.Message); return false; }
        }
        public bool Delete(string id)
        {
            if (!_loaded || !_all.Any(u => u.Id == id)) return false;
            return Keep(_all.Where(u => u.Id != id).ToList());
        }
        public bool DeleteFor(DeathRecord death)
        {
            if (!_loaded || death == null || !For(death).Any()) return false;
            return Keep(_all.Where(u => u.DeathKey != KeyFor(death)).ToList());
        }
        bool Keep(List<ReassessmentUpdate> next)
        {
            var removed = _all.Where(u => !next.Any(n => n.Id == u.Id)).Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
            // Retained duplicate lines must not resurrect a deleted update on
            // the next load. Unrelated unsupported or corrupt data stays intact.
            var retained = _unreadable.Where(line => !HasDeletedId(line, removed)).ToList();
            try { Write(next, retained); _all.Clear(); _all.AddRange(next); _unreadable.Clear(); _unreadable.AddRange(retained); Revision++; return true; }
            catch (Exception ex) { _warn("could not delete reassessment: " + ex.Message); return false; }
        }
        static bool HasDeletedId(string line, HashSet<string> removed)
        {
            try
            {
                using var json = JsonDocument.Parse(line);
                return json.RootElement.TryGetProperty("Id", out var id) && id.ValueKind == JsonValueKind.String && removed.Contains(id.GetString());
            }
            catch { return false; }
        }
        void Write(IEnumerable<ReassessmentUpdate> updates, IEnumerable<string> retained = null)
        {
            using (var stream = new FileStream(_path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                    foreach (string line in updates.Select(u => JsonSerializer.Serialize(u, Json)).Concat(retained ?? _unreadable)) writer.WriteLine(line);
                stream.Flush(true);
            }
            File.Move(_path + ".tmp", _path, true);
        }
        static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json);
        static string StatLabel(string key)
        {
            if (key.StartsWith("Res.")) return key.Substring(4) + " resistance";
            if (key.StartsWith("ResUncapped.")) return "Uncapped " + key.Substring(12).ToLowerInvariant() + " resistance";
            return key switch
            {
                "MaxHealth" => "Max health", "Ward" => "Ward (instant reading)", "CritAvoidance" => "Crit avoidance",
                "ReducedBonusCritDamage" => "Reduced crit bonus damage", "Block" => "Block chance", "BlockEffectiveness" => "Block effectiveness",
                "EnduranceThreshold" => "Endurance threshold", "StunAvoidance" => "Stun avoidance", _ => key,
            };
        }
    }
}
