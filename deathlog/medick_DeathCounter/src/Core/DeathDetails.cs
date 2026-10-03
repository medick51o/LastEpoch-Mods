using System;

namespace medick_DeathCounter.Core
{
    // Authoritative killing-blow information. This is not a damage timeline:
    // never manufacture hits, percentages, or a one-shot classification from it.
    public sealed class DeathDetails
    {
        public string Killer, Ability, Ailment, PrimaryElement, SecondaryElement, Text, RichText;
        public float Damage, Overkill;
        public bool? Crit, IsBossFight;
        public string IrregularSource;
        public bool TextOnly;
        public string Source;

        public void PreserveBossFlagFrom(DeathDetails previous)
        {
            // The nine-argument local report follows the ten-argument network
            // report. Preserve its extra flag only for the very same blow.
            if (previous == null || IsBossFight.HasValue || !previous.IsBossFight.HasValue) return;
            if (Killer == previous.Killer && Ability == previous.Ability && Ailment == previous.Ailment
                && PrimaryElement == previous.PrimaryElement && SecondaryElement == previous.SecondaryElement
                && Damage == previous.Damage && Overkill == previous.Overkill && Crit == previous.Crit
                && IrregularSource == previous.IrregularSource)
                IsBossFight = previous.IsBossFight;
        }

        public void Apply(DeathRecord record)
        {
            if (record == null) return;
            if (!string.IsNullOrWhiteSpace(Killer)) record.Killer = Killer;
            if (!string.IsNullOrWhiteSpace(Ability)) record.KillerAbility = Ability;
            if (!string.IsNullOrWhiteSpace(Ailment)) record.KillingAilment = Ailment;
            if (!string.IsNullOrWhiteSpace(PrimaryElement)) record.KillingElement = PrimaryElement;
            if (!TextOnly) record.SecondaryKillingElement = SecondaryElement;
            if (float.IsFinite(Damage) && Damage > 0f) record.KillingBlow = Damage;
            if (!TextOnly && float.IsFinite(Overkill) && Overkill >= 0f) record.OverkillDamage = Overkill;
            if (Crit.HasValue) record.KillingCrit = Crit;
            if (IsBossFight.HasValue) record.IsBossFight = IsBossFight;
            if (!string.IsNullOrWhiteSpace(IrregularSource)) record.IrregularSource = IrregularSource;
            if (!string.IsNullOrWhiteSpace(Text)) record.GameDeathInfo = Text;
            if (!string.IsNullOrWhiteSpace(RichText)) record.GameDeathInfoRich = RichText;
            if (!TextOnly || string.IsNullOrWhiteSpace(record.DetailSource))
                record.DetailSource = Source ?? (TextOnly ? "game death message" : "game death report");
            if (record.Kind == DeathKind.Unknown ||
                (record.Kind == DeathKind.OneShot && record.MaxHealth <= 0f && record.Hits <= 1))
                record.Kind = DeathKind.Reported;
            record.AilmentsOnYou ??= new();
            if (!string.IsNullOrWhiteSpace(Ailment) && !record.AilmentsOnYou.Contains(Ailment))
                record.AilmentsOnYou.Add(Ailment);
        }
    }
}
