using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace medick_DeathCounter.Core
{
    // A display baseline per character. Resetting never deletes death history.
    public sealed class CounterResets
    {
        readonly string _path;
        readonly Action<string> _warn;
        public const string Unavailable = "counter baselines unavailable";
        Dictionary<string, int> _baselines = new();
        bool _broken;
        bool _hasBaselines;
        public bool Broken => _broken;
        public bool HasBaselines => _hasBaselines;
        public CounterResets(string path, Action<string> warn = null)
        {
            _path = path;
            _warn = warn ?? (_ => { });
            TryRead(false);
        }
        public int Count(string character, int recorded)
        {
            int baseline = _baselines.TryGetValue(character ?? "", out int n) ? n : 0;
            return Math.Max(0, recorded - Math.Max(0, baseline));
        }
        // The number to show. A torn file with no earlier baselines must not
        // display the full saved count as if the resets were zero.
        public string Label(string character, int recorded)
            => _broken && !_hasBaselines ? Unavailable : Count(character, recorded).ToString();
        public bool Reset(string character, int recorded)
        {
            if (string.IsNullOrWhiteSpace(character)) return false;
            if (!TryReread()) return false;
            var next = new Dictionary<string, int>(_baselines) { [character] = Math.Max(0, recorded) };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                // Flush before the rename, as DeathLog does, so a crash cannot
                // leave an empty reset file in place of the old baselines.
                using (var stream = new FileStream(_path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(next);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                File.Move(_path + ".tmp", _path, true);
                _baselines = next;
                _hasBaselines = true;
                _broken = false;
                return true;
            }
            catch (Exception ex) { _warn("could not save counter reset: " + ex.Message); return false; }
        }
        // A torn file blocks Reset until a later read succeeds, so a new
        // baseline cannot replace Hero and Alt with a single new name.
        bool TryReread() => _broken ? TryRead(true) : true;
        // keepExisting: a failed read leaves the last good baselines in place.
        bool TryRead(bool keepExisting)
        {
            try
            {
                if (!File.Exists(_path))
                {
                    _broken = false;
                    _baselines = new();
                    _hasBaselines = false;
                    return true;
                }
                var next = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(_path)) ?? new();
                _baselines = next;
                _hasBaselines = true;
                _broken = false;
                return true;
            }
            catch (Exception ex)
            {
                _broken = true;
                if (!keepExisting || !_hasBaselines) _baselines = new();
                _warn("could not read counter resets: " + ex.Message);
                return false;
            }
        }
        // Re-read the file. A torn read keeps the last good baselines.
        public bool Refresh() => TryRead(true);
    }
}
