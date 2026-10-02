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
        Dictionary<string, int> _baselines = new();
        public CounterResets(string path, Action<string> warn = null)
        {
            _path = path;
            _warn = warn ?? (_ => { });
            try
            {
                if (File.Exists(path)) _baselines = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path)) ?? new();
            }
            catch (Exception ex) { _warn("could not read counter resets: " + ex.Message); }
        }
        public int Count(string character, int recorded) => Math.Max(0, recorded - Math.Max(0, _baselines.TryGetValue(character, out int n) ? n : 0));
        public bool Reset(string character, int recorded)
        {
            if (string.IsNullOrWhiteSpace(character)) return false;
            var next = new Dictionary<string, int>(_baselines) { [character] = Math.Max(0, recorded) };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path));
                File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(next));
                File.Move(_path + ".tmp", _path, true);
                _baselines = next;
                return true;
            }
            catch (Exception ex) { _warn("could not save counter reset: " + ex.Message); return false; }
        }
    }
}
