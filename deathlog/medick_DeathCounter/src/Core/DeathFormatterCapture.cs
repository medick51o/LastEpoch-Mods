using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace medick_DeathCounter.Core
{
    // Correlates the simple formatter helpers for one message. Never infer
    // damage types from boss names, ability names, colors or prose.
    public sealed class DeathFormatterCapture
    {
        static readonly Regex Tags = new("<[^>]*>", RegexOptions.Compiled);
        readonly List<(Element Element, string Label)> _elements = new();
        long _builder;
        int _frame;
        double _at;
        string _character, _killer, _ability, _ailment;
        bool _cause, _amount, _ambiguous;
        int _damage, _overkill;
        bool _crit;
        public int Generation { get; private set; }
        public bool Complete => _cause && _amount && !_ambiguous;

        public void Reset()
        {
            _builder = 0; _character = null; _cause = _amount = _ambiguous = false;
            _elements.Clear(); _killer = _ability = _ailment = null;
            _damage = _overkill = 0; Generation++;
        }

        bool Begin(long builder, string character, int frame, double now)
        {
            if (builder == 0 || string.IsNullOrWhiteSpace(character) || !double.IsFinite(now)) return false;
            if (_builder != builder || _character != character || _frame != frame)
            {
                Reset(); _builder = builder; _character = character; _frame = frame; _at = now;
            }
            return true;
        }

        public void Cause(long builder, string character, int frame, double now, string killer, string ability, string ailment)
        {
            if (!Begin(builder, character, frame, now)) return;
            if (_cause) _ambiguous = true;
            _cause = true; _killer = Plain(killer); _ability = Plain(ability); _ailment = Plain(ailment);
        }

        public void Amount(long builder, string character, int frame, double now, bool crit, int damage, int overkill)
        {
            if (!Begin(builder, character, frame, now)) return;
            if (_amount || damage < 0 || overkill < 0) _ambiguous = true;
            _amount = true; _crit = crit; _damage = damage; _overkill = overkill;
        }

        public void Element(int frame, double now, Element element, string localizedLabel)
        {
            if (_builder == 0 || frame != _frame || now < _at || now - _at > 1 || string.IsNullOrWhiteSpace(localizedLabel)) return;
            string label = Plain(localizedLabel);
            if (string.IsNullOrWhiteSpace(label)) return;
            if (!_elements.Any(e => e.Element == element)) _elements.Add((element, label));
            if (_elements.Count > 2) _ambiguous = true;
        }

        public bool Fresh(string character, double now) => _builder != 0 && _character == character
            && now >= _at && now - _at <= 1;

        public DeathDetails Build(string character, double now, string formatted)
        {
            if (!Fresh(character, now) || !Complete || string.IsNullOrWhiteSpace(formatted)) return null;
            string text = Plain(formatted);
            string[] names = new[] { _killer, _ability, _ailment }.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();
            if (names.Length > 0 && !names.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase))) return null;
            // Do not promote a secondary element to primary when only one of
            // two localized labels matches the message.
            bool typed = _elements.Count > 0 && _elements.All(e => text.Contains(e.Label, StringComparison.OrdinalIgnoreCase));
            string Present(string value) => !string.IsNullOrWhiteSpace(value) && text.Contains(value, StringComparison.OrdinalIgnoreCase) ? value : null;
            return new DeathDetails
            {
                Killer = Present(_killer), Ability = Present(_ability), Ailment = Present(_ailment),
                PrimaryElement = typed ? Elements.Name(_elements[0].Element) : null,
                SecondaryElement = typed && _elements.Count > 1 ? Elements.Name(_elements[1].Element) : null,
                Damage = _damage, Overkill = _overkill, Crit = _crit,
                Text = text, RichText = formatted,
            };
        }

        public static string Plain(string text) => string.IsNullOrWhiteSpace(text) ? null : Tags.Replace(text, "").Trim();
    }
}
