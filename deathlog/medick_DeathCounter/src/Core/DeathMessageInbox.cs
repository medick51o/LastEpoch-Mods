using System;

namespace medick_DeathCounter.Core
{
    // Reports can reach the client before the health update. Keep that new
    // report briefly instead of replacing the baseline while the player lives.
    // A persistent old message needs a new UI/formatter event to be reused.
    public sealed class DeathMessageInbox
    {
        const double Lifetime = 5;
        string _character, _baseline, _pending;
        double _at;

        public string Observe(string character, string text, double now, bool deathWindow, bool freshEvent = false)
        {
            if (string.IsNullOrWhiteSpace(character) || !double.IsFinite(now)) return null;
            if (_character != character)
            {
                _character = character; _baseline = text; _pending = null;
                return null; // loading a character is not a new report
            }
            if (_pending != null && (now < _at || now - _at > Lifetime))
            {
                _baseline = _pending; _pending = null;
            }
            if (string.IsNullOrWhiteSpace(text))
            {
                if (!deathWindow) _baseline = text;
                return null;
            }
            if (freshEvent || (text != _baseline && text != _pending))
            {
                _pending = text; _at = now;
            }
            if (!deathWindow || _pending == null) return null;
            string ready = _pending;
            _baseline = ready; _pending = null;
            return ready;
        }
    }
}
