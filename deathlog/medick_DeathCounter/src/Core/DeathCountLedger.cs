using System.Collections.Generic;

namespace medick_DeathCounter.Core
{
    // Reconciles the game's own lifetime death counter (CharacterData.Deaths)
    // with the deaths this mod already recorded from hooks or the health watch.
    //
    // Every recorded death leaves a credit; a counter increase spends credits
    // first and only the remainder is a death nobody saw. Credits expire after
    // CreditSeconds, so a hook that fired without the counter ever moving
    // cannot swallow a later real death. A time window alone was not enough:
    // a player can sit on the death screen for minutes and the counter may
    // only move at respawn (review 2026-10-01 #4).
    public sealed class DeathCountLedger
    {
        public const double CreditSeconds = 600.0;

        int _last = -1;
        readonly Queue<double> _credits = new();
        // True when Observe already spent the newest credit against the baseline.
        bool _latestSpent;

        public void Reset()
        {
            _last = -1;
            _credits.Clear();
            _latestSpent = false;
        }

        // A death this mod recorded itself.
        public void Recorded(double now)
        {
            _credits.Enqueue(now);
            _latestSpent = false;
        }

        // Drop the newest credit when a pending death is cancelled because
        // health recovered. Older credits stay, so a real earlier death is
        // not forgotten. If Observe already spent that credit, step the
        // baseline back by one so the same counter increase is seen again.
        public bool RetractLatest()
        {
            if (_credits.Count > 0)
            {
                var kept = new Queue<double>();
                int leave = _credits.Count - 1;
                for (int i = 0; i < leave; i++) kept.Enqueue(_credits.Dequeue());
                _credits.Clear();
                while (kept.Count > 0) _credits.Enqueue(kept.Dequeue());
                _latestSpent = false;
                return true;
            }
            if (_latestSpent && _last > 0)
            {
                _last--;
                _latestSpent = false;
                return true;
            }
            return false;
        }

        // Feed the counter's current value. Returns how many deaths it shows
        // that the mod has not recorded (0 on the first read: baseline only).
        public int Observe(int count, double now)
        {
            while (_credits.Count > 0 && now - _credits.Peek() > CreditSeconds) _credits.Dequeue();
            if (count < 0) return 0;
            int before = _last;
            _last = count;
            if (before < 0 || count <= before) return 0;

            int unexplained = count - before;
            int queued = _credits.Count;
            while (unexplained > 0 && _credits.Count > 0) { _credits.Dequeue(); unexplained--; }
            // Spending every queued credit includes the newest one.
            if (queued > 0 && _credits.Count == 0) _latestSpent = true;
            return unexplained;
        }
    }
}
