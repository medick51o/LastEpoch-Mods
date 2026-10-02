using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace medick_DeathCounter.Core
{
    // Every death, forever, on disk:
    //   deaths.jsonl  one DeathRecord per line, the source of truth
    //   deaths.txt    one readable line per death, for humans
    // Append-only: a crash mid-write can only damage the last line, and a bad
    // line is skipped on load instead of losing the whole history.
    public sealed class DeathLog
    {
        public const string JsonFile = "deaths.jsonl";
        public const string TextFile = "deaths.txt";

        static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() },
        };

        readonly string _dir;
        readonly Action<string> _warn;
        readonly List<DeathRecord> _all = new();
        bool _writeWarned;

        public DeathLog(string directory, Action<string> warn = null)
        {
            _dir  = directory;
            _warn = warn ?? (_ => { });
        }

        public string Directory  => _dir;
        public string JsonPath   => Path.Combine(_dir, JsonFile);
        public string TextPath   => Path.Combine(_dir, TextFile);
        public IReadOnlyList<DeathRecord> All => _all;

        public void Load()
        {
            _all.Clear();
            if (!File.Exists(JsonPath)) return;
            int bad = 0;
            foreach (var line in File.ReadLines(JsonPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var r = JsonSerializer.Deserialize<DeathRecord>(line, Json);
                    if (r != null) _all.Add(r);
                    else bad++;
                }
                catch { bad++; }
            }
            if (bad > 0) _warn($"{bad} unreadable line(s) in {JsonFile} were skipped");
        }

        public IEnumerable<DeathRecord> For(string character) =>
            _all.Where(r => string.Equals(r.Character, character, StringComparison.Ordinal));

        public int CountFor(string character) => For(character).Count();

        public DeathRecord LastFor(string character) => For(character).LastOrDefault();

        // Late server details update a numbered record without a second death.
        // Replace each file atomically; keep the previous JSON as a recovery copy.
        public bool SaveUpdated()
        {
            try
            {
                System.IO.Directory.CreateDirectory(_dir);
                File.WriteAllLines(JsonPath + ".tmp", _all.Select(r => JsonSerializer.Serialize(r, Json)));
                File.WriteAllLines(TextPath + ".tmp", _all.Select(r => r.ToLogLine()));
                if (File.Exists(JsonPath)) File.Copy(JsonPath, JsonPath + ".bak", true);
                File.Move(JsonPath + ".tmp", JsonPath, true);
                File.Move(TextPath + ".tmp", TextPath, true);
                return true;
            }
            catch (Exception ex) { _warn("could not save updated death details: " + ex.Message); return false; }
        }

        // Numbers the record as this character's next death, keeps it in
        // memory, and appends it to both files. Returns false if the disk
        // write failed (the death still counts for this session).
        public bool Append(DeathRecord r)
        {
            r.Number = CountFor(r.Character) + 1;
            if (string.IsNullOrWhiteSpace(r.Id)) r.Id = Guid.NewGuid().ToString("N");
            _all.Add(r);
            try
            {
                System.IO.Directory.CreateDirectory(_dir);
                File.AppendAllText(JsonPath, JsonSerializer.Serialize(r, Json) + Environment.NewLine);
                File.AppendAllText(TextPath, r.ToLogLine() + Environment.NewLine);
                return true;
            }
            catch (Exception ex)
            {
                if (!_writeWarned)
                {
                    _writeWarned = true;
                    _warn($"could not write the death log ({ex.Message}); deaths still count this session");
                }
                return false;
            }
        }
    }
}
