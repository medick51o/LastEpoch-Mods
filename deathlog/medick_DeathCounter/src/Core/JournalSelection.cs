using System;
using System.Collections.Generic;
using System.Linq;

namespace medick_DeathCounter.Core
{
    // Presentation state only. Never mutates history or follows a newly loaded
    // character while the reader is inspecting an older death.
    public sealed class JournalSelection
    {
        public string Character { get; private set; }
        public string SelectedKey { get; private set; }
        public bool NewDeathAvailable { get; private set; }
        readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        bool _initialized;

        public void Begin(string character, IReadOnlyList<DeathRecord> records, bool newest)
        {
            if (Character != character) { Character = character; SelectedKey = null; _seen.Clear(); _initialized = false; NewDeathAvailable = false; }
            Refresh(records);
            if (newest) SelectNewest(records);
        }
        public void Refresh(IReadOnlyList<DeathRecord> records)
        {
            var scoped = (records ?? Array.Empty<DeathRecord>()).Where(d => d.Character == Character).ToList();
            if (_initialized && scoped.Any(d => !_seen.Contains(ReassessmentLog.KeyFor(d)))) NewDeathAvailable = true;
            foreach (var d in scoped) _seen.Add(ReassessmentLog.KeyFor(d));
            if (SelectedKey == null) SelectedKey = scoped.LastOrDefault() is DeathRecord last ? ReassessmentLog.KeyFor(last) : null;
            _initialized = true;
        }
        public void Select(DeathRecord death)
        {
            if (death?.Character != Character) return;
            SelectedKey = ReassessmentLog.KeyFor(death);
        }
        public void SelectNewest(IReadOnlyList<DeathRecord> records)
        {
            var last = records?.LastOrDefault(d => d.Character == Character);
            SelectedKey = last == null ? null : ReassessmentLog.KeyFor(last);
            NewDeathAvailable = false;
        }
    }
}
