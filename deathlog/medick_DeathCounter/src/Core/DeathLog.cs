using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace medick_DeathCounter.Core
{
    // JSONL is authoritative; deaths.txt is a rebuildable human-readable mirror.
    // Save complete snapshots atomically, retaining opaque lines and unknown fields.
    public sealed class DeathLog
    {
        public const string JsonFile = "deaths.jsonl";
        public const string TextFile = "deaths.txt";
        static readonly JsonSerializerOptions Json = new()
        {
            Converters = { new JsonStringEnumConverter() },
        };
        static readonly UTF8Encoding Utf8 = new(false, true);
        sealed class Entry
        {
            public DeathRecord Record;
            public string Raw, Snapshot;
        }
        readonly string _dir;
        readonly Action<string> _warn;
        readonly List<DeathRecord> _all = new();
        readonly List<Entry> _entries = new();
        readonly HashSet<DeathRecord> _saved = new();
        byte[] _disk;
        bool _attemptedLoad, _loaded;
        string _loadError, _writeError, _textError;
        int _unreadable;

        public DeathLog(string directory, Action<string> warn = null)
        {
            _dir = directory;
            _warn = warn ?? (_ => { });
        }
        public string Directory => _dir;
        public string JsonPath => Path.Combine(_dir, JsonFile);
        public string TextPath => Path.Combine(_dir, TextFile);
        public IReadOnlyList<DeathRecord> All => _all;
        public int UnsavedCount => _all.Count(r => !_saved.Contains(r));
        public int SavedCountFor(string character) => For(character).Count(r => _saved.Contains(r));
        public string PersistenceWarning => _loadError ?? _writeError ?? _textError
            ?? (_unreadable > 0 ? $"{_unreadable} unreadable history line(s) are preserved but not included in the count." : null);

        // A failed read never replaces the previous in-memory view and never
        // authorizes overwriting a file from an incomplete session history.
        public void Load()
        {
            if (UnsavedCount > 0)
            {
                _warn("Reload skipped: unsaved deaths are still in memory.");
                return;
            }
            _attemptedLoad = true;
            _loaded = false;
            try
            {
                byte[] disk = ReadDisk();
                var entries = new List<Entry>();
                int bad = 0;
                if (disk != null)
                {
                    string text = Utf8.GetString(disk).TrimStart('\uFEFF');
                    using var reader = new StringReader(text);
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var entry = new Entry { Raw = line };
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            try
                            {
                                using var document = JsonDocument.Parse(line);
                                var root = document.RootElement;
                                // Unsupported versions remain opaque; older records have no Schema.
                                bool supported = root.ValueKind == JsonValueKind.Object
                                    && (!root.TryGetProperty("Schema", out var schema) || schema.TryGetInt32(out int version) && version == 1);
                                var record = supported ? JsonSerializer.Deserialize<DeathRecord>(line, Json) : null;
                                if (record == null || string.IsNullOrWhiteSpace(record.Character)) bad++;
                                else
                                {
                                    entry.Record = record.Normalize();
                                    entry.Snapshot = JsonSerializer.Serialize(record, Json);
                                }
                            }
                            catch { bad++; }
                        }
                        entries.Add(entry);
                    }
                }
                _entries.Clear(); _entries.AddRange(entries);
                _all.Clear(); _all.AddRange(entries.Where(e => e.Record != null).Select(e => e.Record));
                _saved.Clear(); foreach (var record in _all) _saved.Add(record);
                _disk = disk; _unreadable = bad; _loaded = true;
                _loadError = null; _writeError = null;
                if (bad > 0) _warn($"{bad} unreadable line(s) in {JsonFile} were skipped and retained");
            }
            catch (Exception ex)
            {
                _loadError = "Death history could not be read. Disk writes are blocked to protect it; new deaths remain in memory. Export them before closing the game.";
                _warn(_loadError + " " + ex.Message);
            }
        }
        byte[] ReadDisk()
        {
            try { return File.ReadAllBytes(JsonPath); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }
        public IEnumerable<DeathRecord> For(string character) =>
            _all.Where(r => string.Equals(r.Character, character, StringComparison.Ordinal));
        public int CountFor(string character) => For(character).Count();
        public DeathRecord LastFor(string character) => For(character).LastOrDefault();

        // Recovery exports include the full captured facts, including unsaved
        // session deaths. Never overwrite an existing file or the primary log.
        public void ExportCharacter(string character, string path)
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using (var writer = new StreamWriter(stream, Utf8, 4096, true))
                foreach (var entry in _entries.Where(e => e.Record != null && e.Record.Character == character))
                {
                    string snapshot = JsonSerializer.Serialize(entry.Record, Json);
                    writer.WriteLine(entry.Raw == null ? snapshot : MergeChanges(entry.Raw, entry.Snapshot, snapshot));
                }
            stream.Flush(true);
        }

        public bool Append(DeathRecord record)
        {
            if (!_attemptedLoad) Load();
            record.Number = Math.Max(CountFor(record.Character), For(record.Character).Select(r => r.Number).DefaultIfEmpty().Max()) + 1;
            if (string.IsNullOrWhiteSpace(record.Id)) record.Id = Guid.NewGuid().ToString("N");
            _all.Add(record);
            _entries.Add(new Entry { Record = record });
            return SaveUpdated();
        }

        // A successful return means the JSON is committed. A text-mirror error
        // is reported separately and must not label an already saved death lost.
        public bool SaveUpdated()
        {
            if (!_attemptedLoad) Load();
            if (!_loaded) return false;
            try
            {
                System.IO.Directory.CreateDirectory(_dir);
                byte[] current = ReadDisk();
                if ((_disk == null) != (current == null) || _disk != null && !_disk.SequenceEqual(current))
                    throw new IOException("History changed on disk after loading; refusing to overwrite it.");
                var snapshots = _entries.Select(e => e.Record == null ? null : JsonSerializer.Serialize(e.Record, Json)).ToArray();
                var lines = _entries.Select((e, i) => e.Record == null ? e.Raw
                    : e.Raw == null ? snapshots[i] : MergeChanges(e.Raw, e.Snapshot, snapshots[i])).ToArray();
                byte[] next = Utf8.GetBytes(string.Join(Environment.NewLine, lines) + (lines.Length > 0 ? Environment.NewLine : ""));
                if (current == null || !current.SequenceEqual(next))
                {
                    WriteFlushed(JsonPath + ".tmp", next);
                    if (current != null)
                    {
                        // Never overwrite the original pre-rewrite recovery copy.
                        // Rolling .bak remains useful for the latest successful save.
                        if (!File.Exists(JsonPath + ".recovery")) File.Copy(JsonPath, JsonPath + ".recovery", false);
                        File.Copy(JsonPath, JsonPath + ".bak", true);
                    }
                    File.Move(JsonPath + ".tmp", JsonPath, true);
                }
                _disk = next;
                for (int i = 0; i < _entries.Count; i++)
                {
                    _entries[i].Raw = lines[i]; _entries[i].Snapshot = snapshots[i];
                }
                _saved.Clear(); foreach (var record in _all) _saved.Add(record);
                _writeError = null;
            }
            catch (Exception ex)
            {
                string warning = "Some deaths or updated details could not be saved. They remain in memory; export the log before closing the game.";
                if (_writeError != warning) _warn(warning + " " + ex.Message);
                _writeError = warning;
                return false;
            }
            try
            {
                WriteFlushed(TextPath + ".tmp", Utf8.GetBytes(string.Join(Environment.NewLine, _all.Select(r => r.ToLogLine()))
                    + (_all.Count > 0 ? Environment.NewLine : "")));
                File.Move(TextPath + ".tmp", TextPath, true);
                _textError = null;
            }
            catch (Exception ex)
            {
                string warning = "Death history is saved, but the readable text copy could not be refreshed.";
                if (_textError != warning) _warn(warning + " " + ex.Message);
                _textError = warning;
            }
            return true;
        }
        static void WriteFlushed(string path, byte[] bytes)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        internal static string TryMerge(string raw, string before, string after) => MergeChanges(raw, before, after);
        static string MergeChanges(string raw, string before, string after)
        {
            if (before == after) return raw;
            try
            {
                if (JsonNode.Parse(raw) is not JsonObject target) return raw;
                ApplyChanges(target, JsonNode.Parse(before).AsObject(), JsonNode.Parse(after).AsObject());
                return target.ToJsonString();
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                // A duplicate key or other broken line stays as stored text.
                // One bad line must not block the rest of the save.
                return raw;
            }
        }
        static void ApplyChanges(JsonObject target, JsonObject before, JsonObject after)
        {
            foreach (var property in after)
            {
                before.TryGetPropertyValue(property.Key, out var old);
                if (old?.ToJsonString() == property.Value?.ToJsonString()) continue;
                if (old is JsonObject oldObject && property.Value is JsonObject newObject && target[property.Key] is JsonObject targetObject)
                    ApplyChanges(targetObject, oldObject, newObject);
                else target[property.Key] = property.Value == null ? null : JsonNode.Parse(property.Value.ToJsonString());
            }
            foreach (var property in before)
                if (!after.ContainsKey(property.Key)) target.Remove(property.Key);
        }
    }
}
