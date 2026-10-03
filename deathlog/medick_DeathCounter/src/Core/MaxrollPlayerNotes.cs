using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace medick_DeathCounter.Core
{
    // Short reworded Maxroll community-guide lines. Not the guide's own sentences.
    public static class MaxrollPlayerNotes
    {
        public const string Attribution = "Maxroll (community guide)";
        public const int LineWordCap = 22;
        static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        static readonly Dictionary<string, string> _resists;
        static readonly Dictionary<string, MoveNote> _moves;
        public static string LoadError { get; }

        static MaxrollPlayerNotes()
        {
            try
            {
                using var stream = typeof(MaxrollPlayerNotes).Assembly.GetManifestResourceStream("medick_DeathCounter.MaxrollPlayerNotes.json");
                if (stream == null) throw new InvalidDataException("Embedded Maxroll player notes are missing.");
                using var reader = new StreamReader(stream);
                var file = JsonSerializer.Deserialize<NotesFile>(reader.ReadToEnd(), Json);
                _resists = file?.Resists ?? new Dictionary<string, string>(StringComparer.Ordinal);
                _moves = file?.Moves ?? new Dictionary<string, MoveNote>(StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                LoadError = ex.Message;
                _resists = new Dictionary<string, string>(StringComparer.Ordinal);
                _moves = new Dictionary<string, MoveNote>(StringComparer.Ordinal);
            }
        }

        public static int MoveCount => _moves.Count;
        public static int ResistCount => _resists.Count;
        public static bool TryMove(string moveId, out string spot, out string avoid, out bool danger, out bool oneShot)
        {
            spot = avoid = null; danger = oneShot = false;
            if (moveId == null || !_moves.TryGetValue(moveId, out var note) || note == null) return false;
            spot = note.Spot; avoid = note.Avoid; danger = note.Danger; oneShot = note.OneShot;
            return true;
        }
        public static string Resists(string encounterId)
        {
            if (encounterId == null || !_resists.TryGetValue(encounterId, out var text) || string.IsNullOrWhiteSpace(text)) return null;
            return Attribution + ": " + text.Trim();
        }
        public static IReadOnlyList<string> MoveLines(string moveId)
        {
            if (!TryMove(moveId, out var spot, out var avoid, out bool danger, out bool oneShot)) return Array.Empty<string>();
            var lines = new List<string> { Attribution };
            if (oneShot) lines.Add("The guide marks this as a one-shot.");
            else if (danger) lines.Add("The guide marks this as dangerous.");
            if (!string.IsNullOrWhiteSpace(spot)) lines.Add("How to spot it: " + spot.Trim());
            if (!string.IsNullOrWhiteSpace(avoid)) lines.Add("How to avoid it: " + avoid.Trim());
            return lines;
        }

        sealed class NotesFile
        {
            public Dictionary<string, string> Resists { get; set; }
            public Dictionary<string, MoveNote> Moves { get; set; }
        }
        sealed class MoveNote
        {
            public string Spot { get; set; }
            public string Avoid { get; set; }
            public bool Danger { get; set; }
            public bool OneShot { get; set; }
        }
    }
}
