using System;

namespace medick_DeathCounter.Core
{
    // Authoritative killing-blow information. This is not a damage timeline:
    // never manufacture hits, percentages, or a one-shot classification from it.
    public sealed class DeathDetails
    {
        public string Killer, Ability, Ailment, PrimaryElement, SecondaryElement, Text, RichText;
        public float Damage, Overkill;
        public bool Crit;

        public void Apply(DeathRecord record)
        {
            if (record == null) return;
            if (!string.IsNullOrWhiteSpace(Killer)) record.Killer = Killer;
            if (!string.IsNullOrWhiteSpace(Ability)) record.KillerAbility = Ability;
            if (!string.IsNullOrWhiteSpace(Ailment)) record.KillingAilment = Ailment;
            if (!string.IsNullOrWhiteSpace(PrimaryElement)) record.KillingElement = PrimaryElement;
            record.SecondaryKillingElement = SecondaryElement;
            if (float.IsFinite(Damage) && Damage > 0f) record.KillingBlow = Damage;
            if (float.IsFinite(Overkill) && Overkill >= 0f) record.OverkillDamage = Overkill;
            record.KillingCrit = Crit;
            if (!string.IsNullOrWhiteSpace(Text)) record.GameDeathInfo = Text;
            if (!string.IsNullOrWhiteSpace(RichText)) record.GameDeathInfoRich = RichText;
            record.DetailSource = "game death report";
            if (record.Kind == DeathKind.Unknown) record.Kind = DeathKind.Reported;
            if (!string.IsNullOrWhiteSpace(Ailment) && !record.AilmentsOnYou.Contains(Ailment))
                record.AilmentsOnYou.Add(Ailment);
        }
    }
}
