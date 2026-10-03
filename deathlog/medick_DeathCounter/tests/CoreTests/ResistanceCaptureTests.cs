using System;
using System.Collections.Generic;
using System.Linq;
using medick_DeathCounter.Core;

static partial class Program
{
    static readonly string RahyehMessage = "You have been slain by a Void Blast from Rahyeh, The Black Sun\n"
        + "Killing Blow Damage Type: Void\nKilling Blow Damage: <color=#ff0000>2609</color> - Overkill Damage: 2015";

    static void Test_MessageInbox_ReportBeforeHealthIsRetainedAndDeliveredOnce()
    {
        var inbox = new DeathMessageInbox();
        Eq(null, inbox.Observe("Hero", "older report", 0, false));
        Eq(null, inbox.Observe("Hero", RahyehMessage, 10, false));
        Eq(null, inbox.Observe("Hero", RahyehMessage, 10.1, false));
        Eq(RahyehMessage, inbox.Observe("Hero", RahyehMessage, 10.2, true));
        Eq(null, inbox.Observe("Hero", RahyehMessage, 10.3, true));
    }
    static void Test_MessageInbox_StaleAndCharacterSwitchCannotBorrowAReport()
    {
        var inbox = new DeathMessageInbox();
        inbox.Observe("Hero", "old", 0, false);
        inbox.Observe("Hero", "new", 1, false);
        Eq(null, inbox.Observe("Hero", "new", 7, true));
        Eq(null, inbox.Observe("Other", "new", 8, true));
        Eq(null, inbox.Observe("Other", "new", 8.1, true));
    }
    static void Test_MessageInbox_IdenticalReportRequiresAFreshEvent()
    {
        var inbox = new DeathMessageInbox();
        inbox.Observe("Hero", "old", 0, false);
        Eq(null, inbox.Observe("Hero", "old", 1, true));
        Eq("old", inbox.Observe("Hero", "old", 1.1, true, true));
        Eq(null, inbox.Observe("Hero", "old", 1.2, true));
    }
    static void Test_MessageInbox_UnsupportedTimeCannotExtendPendingReport()
    {
        var inbox = new DeathMessageInbox();
        inbox.Observe("Hero", "old", 10, false);
        inbox.Observe("Hero", "new", 11, false);
        Eq(null, inbox.Observe("Hero", "new", double.NaN, true));
        Eq(null, inbox.Observe("Hero", "new", 9, true));
    }
    static void Test_MessageParser_ActualRahyehScreenPersistsCauseTypeNumbersAndColors()
    {
        var report = DeathMessageParser.Parse(RahyehMessage);
        Eq("Rahyeh, The Black Sun", report.Killer); Eq("Void Blast", report.Ability);
        Eq("Void", report.PrimaryElement); Eq(2609f, report.Damage); Eq(2015f, report.Overkill);
        Eq(false, report.TextOnly); Eq(null, report.Crit); Eq(null, report.IsBossFight);
        var death = new DeathRecord(); report.Apply(death);
        Eq("Void", death.KillingElement); Eq(2609f, death.KillingBlow); Eq(2015f, death.OverkillDamage);
        Eq("game death message (parsed)", death.DetailSource);
        True(death.GameDeathInfoRich.Contains("<color"), "original colors retained");
        True(!death.GameDeathInfo.Contains("<color"), "plain text retained");
    }
    static void Test_MessageParser_ColorBossAbilityAndUnsupportedLanguageCannotInventDamage()
    {
        foreach (string text in new[] { "<color=purple>Void Blast from Rahyeh</color>", "Killed by Fire Dragon: 100", "Dégâts du coup fatal : Vide" })
        {
            var report = DeathMessageParser.Parse(text);
            Eq(null, report.PrimaryElement); Eq(0f, report.Damage); Eq(true, report.TextOnly);
            True(!string.IsNullOrWhiteSpace(report.Text), "unparsed message retained");
        }
    }
    static void Test_MessageParser_ConflictingOrOverflowingFieldsStayTextOnly()
    {
        foreach (string text in new[] { RahyehMessage + "\nKilling Blow Damage Type: Fire",
            RahyehMessage.Replace("2609", "99999999999999"), RahyehMessage.Replace("2609", "2,60"),
            RahyehMessage.Replace("Void\nKilling", "Shadow\nKilling") })
        {
            var report = DeathMessageParser.Parse(text);
            Eq(true, report.TextOnly); Eq(null, report.PrimaryElement);
        }
    }
    static void Test_MessageParser_MixedExplicitTypesAndGroupedNumbers()
    {
        var report = DeathMessageParser.Parse(RahyehMessage.Replace("Type: Void", "Types: Void and Physical").Replace("2609", "2,609"));
        Eq("Void", report.PrimaryElement); Eq("Physical", report.SecondaryElement); Eq(2609f, report.Damage);
    }
    static Dictionary<string, float> ExampleResistances() => new()
    {
        ["Res.Cold"] = 75, ["ResUncapped.Cold"] = 112,
        ["Res.Void"] = 2, ["ResUncapped.Void"] = 2,
        ["Res.Fire"] = 29, ["Res.Physical"] = 2,
    };
    static void Test_ResistanceCards_CappedFirstEffectiveBeforeTotalRelevantGapNext()
    {
        var rows = ResistanceReview.Build(ExampleResistances(), new DeathRecord { KillingElement = "Void", ZoneLevel = 70 });
        Eq(Element.Cold, rows[0].Element); Eq(75f, rows[0].Effective); Eq(112f, rows[0].Total);
        Eq(Element.Void, rows[1].Element); Eq(73f, rows[1].Gap);
        True(rows[1].LargeRelevantGap && rows[1].KillingType, "captured void gap highlighted");
        True(Math.Abs(rows[1].ExtraDamageAtArea.Value - (1.68f / .95f - 1f)) < .0001f, "area-specific gap math");
    }
    static void Test_ResistanceCards_UnknownIsNotZeroAndBossCannotSupplyRelevance()
    {
        var rows = ResistanceReview.Build(ExampleResistances(), new DeathRecord { Killer = "Rahyeh", ZoneLevel = 70 });
        True(rows.All(r => !r.Relevant && !r.LargeRelevantGap), "no damage inferred from boss");
        Eq(null, rows.Single(r => r.Element == Element.Lightning).Effective);
    }
    static void Test_ResistanceCards_UnknownAreaAndSmallGapDoNotClaimLargeDanger()
    {
        var rows = ResistanceReview.Build(ExampleResistances(), new DeathRecord { KillingElement = "Void" });
        var v = rows.Single(r => r.Element == Element.Void);
        Eq(null, v.ExtraDamageAtArea); Eq(false, v.LargeRelevantGap);
        rows = ResistanceReview.Build(new Dictionary<string, float> { ["Res.Void"] = 74.9f }, new DeathRecord { KillingElement = "Void", ZoneLevel = 70 });
        Eq(false, rows.Single(r => r.Element == Element.Void).Capped);
        Eq(false, rows.Single(r => r.Element == Element.Void).LargeRelevantGap);
    }
    static void Test_ResistanceCards_ReassessmentPreservesBothSnapshots()
    {
        var original = ExampleResistances();
        var current = new Dictionary<string, float>(original) { ["Res.Void"] = 75, ["ResUncapped.Void"] = 90 };
        var death = new DeathRecord { Defenses = original, KillingElement = "Void", ZoneLevel = 70 };
        var row = ResistanceReview.Build(current, death, original).Single(r => r.Element == Element.Void);
        Eq(75f, row.Effective); Eq(2f, row.Previous); Eq(90f, row.Total); Eq(2f, death.Defenses["Res.Void"]);
        Eq(true, row.Capped); Eq(false, row.LargeRelevantGap);
    }
    static void Test_Advice_ReassessmentUsesFrozenZoneWhenOnlineStatsOmitArea()
    {
        var death = new DeathRecord { Character = "Hero", Defenses = ExampleResistances(),
            KillingElement = "Void", ZoneLevel = 70, DefenseSnapshotAgeSeconds = .2f };
        True(AdviceRules.FreshDefense(death, "AreaLevel", out float area), "frozen zone is readable");
        Eq(70f, area);
        var assessment = CurrentAssessment.Build(death, "Hero", ExampleResistances());
        Eq(70f, assessment.CurrentStats["AreaLevel"]);
        True(assessment.Actions.Any(a => a.Key == "res_Void" && a.Body.Contains("area level 70")), "reassessment retains original area");
        Eq(false, death.Defenses.ContainsKey("AreaLevel")); // original unchanged
    }
}
