using System;
using System.Collections.Generic;

namespace medick_DeathCounter.Core
{
    public enum DeathKind
    {
        Unknown,         // no hits seen (hooks missing, or killed by something we cannot see)
        OneShot,         // a single hit took most of your life
        Burst,           // several hits inside ~2 seconds
        DamageOverTime,  // ailments / DoT did most of the work
        Attrition,       // worn down over several seconds
    }

    public sealed class SourceShare
    {
        public string Name   { get; set; }
        public float  Amount { get; set; }
        public int    Hits   { get; set; }
    }

    // One death, as written to deaths.jsonl. Raw facts only: suggestions are
    // computed when the death is viewed, so better advice reaches old deaths too.
    public sealed class DeathRecord
    {
        public int      Number         { get; set; }   // this character's Nth death
        public DateTime UtcTime        { get; set; }
        public string   Character      { get; set; }
        public string   CharacterClass { get; set; }
        public int      Level          { get; set; }
        public string   Zone           { get; set; }
        public bool     Hardcore       { get; set; }

        public string   Killer         { get; set; }
        public string   KillerAbility  { get; set; }
        public string   KillingAilment { get; set; }
        public string   KillingElement { get; set; }
        public float    KillingBlow    { get; set; }
        public bool?    KillingCrit    { get; set; }
        public float    MaxHealth      { get; set; }

        public DeathKind Kind          { get; set; }
        public float    WindowSeconds  { get; set; }
        public float    WindowDamage   { get; set; }
        public float    DotDamage      { get; set; }
        public float[]  DamageByElement { get; set; } = new float[Elements.Count];
        public float[]  DotByElement    { get; set; } = new float[Elements.Count];   // the DoT part of DamageByElement
        public List<SourceShare> TopSources { get; set; } = new();
        public List<string> AilmentsOnYou { get; set; } = new();
        public int      Hits           { get; set; }

        public string   GameDeathInfo  { get; set; }   // the game's own death text (ProtectionClass.deathInformation), when readable
        public string   Detection      { get; set; }   // "hook" | "health" | "game", for bug reports
        public string   ModVersion     { get; set; }

        public string KillerLine()
        {
            string who = string.IsNullOrEmpty(Killer) ? "something unseen" : Killer;
            if (!string.IsNullOrEmpty(KillingAilment) && !string.IsNullOrEmpty(Killer))
                return $"{who}'s {KillingAilment}";
            if (!string.IsNullOrEmpty(KillingAilment)) return KillingAilment;
            return who;
        }

        public string KindLabel() => Kind switch
        {
            DeathKind.OneShot        => "ONE-SHOT",
            DeathKind.Burst          => "BURST",
            DeathKind.DamageOverTime => "DAMAGE OVER TIME",
            DeathKind.Attrition      => "WORN DOWN",
            _                        => "UNKNOWN",
        };

        // Plain-text line for deaths.txt.
        public string ToLogLine()
        {
            string when  = UtcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string blow  = KillingBlow > 0 ? $" for {KillingBlow:0}" : "";
            string elem  = string.IsNullOrEmpty(KillingElement) ? "" : $" {KillingElement.ToLowerInvariant()}";
            string abil  = string.IsNullOrEmpty(KillerAbility) ? "" : $" ({KillerAbility})";
            string ail   = AilmentsOnYou.Count > 0 ? $" | ailments: {string.Join(", ", AilmentsOnYou)}" : "";
            string lvl   = (Level > 0 ? $" lvl {Level}" : "") + (Hardcore ? " HC" : "");
            return $"{when} | {Character}{lvl} death #{Number} | {Zone} | killed by {KillerLine()}{abil}{blow}{elem} | {KindLabel()}{ail}";
        }
    }
}
