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
        Reported,        // game's killing-blow report; no complete hit timeline
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
        public string Id { get; set; } // stable link for reassessments; old records use a derived key
        public int      Number         { get; set; }   // this character's Nth death
        public DateTime UtcTime        { get; set; }
        public string   Character      { get; set; }
        public string   CharacterClass { get; set; }
        public int      Level          { get; set; }
        public string   Zone           { get; set; }
        public string   RawSceneId     { get; set; }
        public int?     ZoneLevel      { get; set; }
        public bool     Hardcore       { get; set; }
        public PlayContext PlayContext { get; set; } // realm when this death happened, not today's realm

        public string   Killer         { get; set; }
        public string   KillerAbility  { get; set; }
        public string   KillingAilment { get; set; }
        public string   KillingElement { get; set; }
        public float    KillingBlow    { get; set; }
        public string   SecondaryKillingElement { get; set; }
        public float    OverkillDamage { get; set; }
        public string   DetailSource   { get; set; }
        public bool?    KillingCrit    { get; set; }
        public bool?    IsBossFight    { get; set; }
        public string   IrregularSource { get; set; } // enum name only; no inferred hazard
        public float    MaxHealth      { get; set; }
        // Pool and loss the death kind was judged against. 0 means an older record
        // with no split. Timeline WindowDamage stays health plus ward.
        public float    ClassPool      { get; set; }
        public float    ClassLoss      { get; set; }
        // Negative when that part of the killing hit was not read.
        public float    KillingHealthLoss { get; set; } = -1f;
        public float    KillingWardLoss   { get; set; } = -1f;

        public DeathKind Kind          { get; set; }
        public float    WindowSeconds  { get; set; }
        public float    WindowDamage   { get; set; }
        public float    DotDamage      { get; set; }
        public float[]  DamageByElement { get; set; } = new float[Elements.Count];
        public float[]  DotByElement    { get; set; } = new float[Elements.Count];   // the DoT part of DamageByElement
        public List<SourceShare> TopSources { get; set; } = new();
        public List<string> AilmentsOnYou { get; set; } = new();
        public int      Hits           { get; set; }

        // Your defences at the moment of death, when the game could be read:
        // "Res.Fire" (effective %, after the cap), "Armor", "CritAvoidance",
        // "Dodge", "Block", "Endurance", "EnduranceThreshold", "Ward",
        // "MaxHealth", "StunAvoidance". Null or missing keys = unknown.
        public Dictionary<string, float> Defenses { get; set; }
        public float? DefenseSnapshotAgeSeconds { get; set; }

        // Loaded history may be older, hand-edited or partly written. Readers
        // index the element arrays by Elements.Count and enumerate the lists,
        // so a null list or a short array would throw while drawing or saving.
        // A malformed array becomes unknown (null), never invented zeros.
        public DeathRecord Normalize()
        {
            AilmentsOnYou ??= new();
            AilmentsOnYou.RemoveAll(string.IsNullOrWhiteSpace);
            TopSources ??= new();
            TopSources.RemoveAll(s => s == null);
            static float[] Valid(float[] a) => a != null && a.Length == Elements.Count && Array.TrueForAll(a, v => float.IsFinite(v) && v >= 0f) ? a : null;
            DamageByElement = Valid(DamageByElement);
            DotByElement = DamageByElement == null ? null : Valid(DotByElement);
            return this;
        }

        public bool TryDefense(string key, out float v)
        {
            v = 0f;
            return Defenses != null && Defenses.TryGetValue(key, out v) && float.IsFinite(v);
        }

        // "Fire 41%  ·  Cold 75% (cap)  ·  ...  ·  Armor 1,200  ·  Ward 120 (after hit)"
        public string DefenseLine()
        {
            var parts = new List<string>();
            foreach (var n in Elements.Names)
                if (TryDefense("Res." + n, out float r))
                    parts.Add($"{n} {r:0}%{(r >= DefenseSnapshot.ResCap - 0.5f ? " (cap)" : "")}");
            if (TryDefense("Armor", out float a))          parts.Add($"Armor {a:N0}");
            if (TryDefense("Dodge", out float dg))         parts.Add($"Dodge {dg:N0}");
            if (TryDefense("Block", out float b))          parts.Add($"Block {b:0}%");
            if (TryDefense("BlockEffectiveness", out float be)) parts.Add($"Block effectiveness {be:N0}");
            if (TryDefense("Endurance", out float e))      parts.Add($"Endurance {e:0}%");
            if (TryDefense("CritAvoidance", out float c))  parts.Add($"Crit avoid {c:0}%");
            if (TryDefense("ReducedBonusCritDamage", out float cb)) parts.Add($"Reduced crit bonus {cb:0}%");
            if (TryDefense("AreaLevel", out float area))   parts.Add($"Area level {area:0}");
            if (TryDefense("Ward", out float w))           parts.Add($"Ward {w:N0}" + (DefenseSnapshotAgeSeconds.HasValue ? " (snapshot)" : " (after hit)"));
            return string.Join("  ·  ", parts);
        }

        public string   GameDeathInfo  { get; set; }   // the game's own death text (ProtectionClass.deathInformation), when readable
        public string   GameDeathInfoRich { get; set; } // original colors, rendered at our own readable font size
        public string   Detection      { get; set; }   // "hook" | "health" | "game", for bug reports
        public string   ModVersion     { get; set; }

        // Headline for the death card and History. Timeline captures store ""
        // (not null) for a missing ability or a hit kill, so blank fields must
        // fall through instead of producing an empty headline.
        public string CauseTitle()
        {
            if (!string.IsNullOrWhiteSpace(KillerAbility)) return KillerAbility.Trim();
            if (!string.IsNullOrWhiteSpace(KillingAilment)) return KillingAilment.Trim();
            return !string.IsNullOrWhiteSpace(Killer) ? "Killed by " + Killer.Trim() : "Cause not recorded";
        }

        // True when the headline names an ability or ailment, so the attacker
        // still needs its own line.
        public bool CauseTitleNamesAttack() => !string.IsNullOrWhiteSpace(KillerAbility) || !string.IsNullOrWhiteSpace(KillingAilment);

        public string KillingAilmentLabel() => string.IsNullOrWhiteSpace(KillingAilment) ? "Not recorded" : KillingAilment.Trim();

        // Shown when ward, not health, was most of the killing hit.
        public string WardDominatedLine()
        {
            if (KillingHealthLoss < 0f || KillingWardLoss < 0f || KillingWardLoss <= KillingHealthLoss) return null;
            return "Ward took more of the killing hit than health did. The death kind is based on health loss.";
        }

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
            DeathKind.Reported       => "GAME DEATH REPORT",
            _                        => "UNKNOWN",
        };

        public string PlayContextLabel() => PlayContext?.Label() ?? "Season / Legacy unknown";

        public string LocationLabel() =>
            (!string.IsNullOrWhiteSpace(RawSceneId) ? "Scene: " + RawSceneId : !string.IsNullOrWhiteSpace(Zone) ? Zone : "Zone unavailable")
            + (ZoneLevel > 0 ? $" (area level {ZoneLevel})" : "");

        // Plain-text line for deaths.txt.
        public string ToLogLine()
        {
            string when  = UtcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            string blow  = KillingBlow > 0 ? $" for {KillingBlow:0}" : "";
            string elem  = string.IsNullOrEmpty(KillingElement) ? "" : $" {KillingElement.ToLowerInvariant()}";
            string abil  = string.IsNullOrEmpty(KillerAbility) ? "" : $" ({KillerAbility})";
            string ail   = AilmentsOnYou?.Count > 0 ? $" | ailments: {string.Join(", ", AilmentsOnYou)}" : "";
            string lvl   = (Level > 0 ? $" lvl {Level}" : "") + (Hardcore ? " HC" : "");
            return $"{when} | {Character}{lvl} death #{Number} | {PlayContextLabel()} | {LocationLabel()} | {BossCatalog.EncounterLabel(this)} | killed by {KillerLine()}{abil}{blow}{elem} | {KindLabel()}{ail}";
        }
    }
}
