using System;
using medick_DeathCounter.Core;

static partial class Program
{
    static DeathFormatterCapture VoidFormatter()
    {
        var buffer = new DeathFormatterCapture();
        buffer.Cause(42, "Hero", 100, 10, "Rahyeh", "Dive Bomb", null);
        buffer.Element(100, 10, Element.Void, "Void");
        buffer.Amount(42, "Hero", 100, 10, false, 2000, 500);
        return buffer;
    }
    static void Test_Formatter_SameMessageCapturesCauseElementDamageAndOriginalText()
    {
        var report = VoidFormatter().Build("Hero", 10.1, "Rahyeh: Dive Bomb. <color=#aa44ff>Void</color> 2000 (500 overkill)");
        Eq("Rahyeh", report.Killer); Eq("Dive Bomb", report.Ability);
        Eq("Void", report.PrimaryElement); Eq(null, report.SecondaryElement);
        Eq(2000f, report.Damage); Eq(500f, report.Overkill); Eq(false, report.Crit);
        True(report.Text.Contains("Void") && !report.Text.Contains("<color"), "plain text preserved without markup");
        True(report.RichText.Contains("<color"), "original game coloring preserved");
        Eq(null, report.IsBossFight); // no unsafe callback flag, no guessed boss classification
        Eq("Void", VoidFormatter().Build("Hero", 10.1, "Rahyeh Dive Bomb deals void damage").PrimaryElement);
    }
    static void Test_Formatter_AmountMayPrecedeCauseWithoutLosingCorrelation()
    {
        var buffer = new DeathFormatterCapture();
        buffer.Amount(42, "Hero", 100, 10, true, 100, 0);
        buffer.Element(100, 10, Element.Fire, "Fire");
        buffer.Cause(42, "Hero", 100, 10, "Enemy", null, "Ignite");
        var report = buffer.Build("Hero", 10.1, "Enemy Ignite Fire 100");
        Eq("Fire", report.PrimaryElement); Eq("Ignite", report.Ailment); Eq(true, report.Crit);
    }
    static void Test_Formatter_AbilityAndBossNamesNeverEstablishAnElement()
    {
        var buffer = new DeathFormatterCapture();
        buffer.Cause(42, "Hero", 100, 10, "Void Bird", "Void Fireball", null);
        buffer.Amount(42, "Hero", 100, 10, false, 100, 0);
        var report = buffer.Build("Hero", 10, "Void Bird used Void Fireball 100");
        Eq(null, report.PrimaryElement); Eq("Void Bird", report.Killer);
    }
    static void Test_Formatter_StaleOtherCharacterAndTimeReversalAreRejected()
    {
        var buffer = VoidFormatter();
        Eq(null, buffer.Build("Other Hero", 10.1, "Rahyeh Dive Bomb Void"));
        Eq(null, buffer.Build("Hero", 11.01, "Rahyeh Dive Bomb Void"));
        Eq(null, buffer.Build("Hero", 9.9, "Rahyeh Dive Bomb Void"));
    }
    static void Test_Formatter_DifferentBuilderOrFrameCannotCombineFragments()
    {
        var buffer = VoidFormatter();
        buffer.Amount(43, "Hero", 100, 10, false, 3000, 100);
        Eq(null, buffer.Build("Hero", 10, "Rahyeh Dive Bomb Void 3000"));
        buffer = VoidFormatter();
        buffer.Amount(42, "Hero", 101, 10.1, false, 3000, 100);
        Eq(null, buffer.Build("Hero", 10.1, "Rahyeh Dive Bomb Void 3000"));
    }
    static void Test_Formatter_DuplicateHelpersAndMoreThanTwoElementsAreAmbiguous()
    {
        var buffer = VoidFormatter();
        buffer.Cause(42, "Hero", 100, 10, "Other Enemy", null, null);
        Eq(null, buffer.Build("Hero", 10, "Other Enemy Void"));
        buffer = VoidFormatter();
        buffer.Element(100, 10, Element.Fire, "Fire");
        buffer.Element(100, 10, Element.Cold, "Cold");
        Eq(null, buffer.Build("Hero", 10, "Rahyeh Dive Bomb Void Fire Cold"));
    }
    static void Test_Formatter_MissingOrMismatchedTextCannotInventDetails()
    {
        var buffer = VoidFormatter();
        Eq(null, buffer.Build("Hero", 10, null));
        Eq(null, buffer.Build("Hero", 10, "Some other death message"));
    }
    static void Test_Formatter_LocalizedLabelsKeepCanonicalTypesAndSecondaryOrder()
    {
        var buffer = VoidFormatter();
        buffer.Element(100, 10, Element.Fire, "Feuer");
        var report = buffer.Build("Hero", 10, "Rahyeh Dive Bomb Void Feuer");
        Eq("Void", report.PrimaryElement); Eq("Fire", report.SecondaryElement);
        report = buffer.Build("Hero", 10, "Rahyeh Dive Bomb Feuer");
        Eq(null, report.PrimaryElement); Eq(null, report.SecondaryElement);
    }
    static void Test_Formatter_ResetAndInvalidPointerDoNotReuseOldReports()
    {
        var buffer = VoidFormatter(); int generation = buffer.Generation;
        buffer.Reset(); True(buffer.Generation > generation, "old generation invalidated");
        buffer.Cause(0, "Hero", 100, 10, "Rahyeh", null, null);
        buffer.Amount(0, "Hero", 100, 10, false, 100, 0);
        Eq(null, buffer.Build("Hero", 10, "Rahyeh Void"));
    }
    static void Test_Formatter_TextFallbackCannotEraseKnownTypedDetails()
    {
        var death = new DeathRecord { KillingElement = "Void", SecondaryKillingElement = "Physical", OverkillDamage = 500, DetailSource = "game death report" };
        new DeathDetails { TextOnly = true, Text = "Game message", RichText = "Game message" }.Apply(death);
        Eq("Void", death.KillingElement); Eq("Physical", death.SecondaryKillingElement);
        Eq(500f, death.OverkillDamage); Eq("game death report", death.DetailSource);
        Eq("Game message", death.GameDeathInfo);
    }
    static void SafeSignature(bool crit, int damage, string text) { }
    static void UnsafeSignature(Il2CppSystem.Nullable<int> value) { }
    static void UnsafeReference(ref Il2CppSystem.Nullable<int> value) { }
    static Il2CppSystem.Nullable<int> UnsafeReturn() => null;
    static void Test_InteropSignatures_RejectNativeNullableParametersRefsAndReturns()
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
        True(InteropSignaturePolicy.CanPatch(typeof(Program).GetMethod(nameof(SafeSignature), flags)), "simple formatter values accepted");
        foreach (string name in new[] { nameof(UnsafeSignature), nameof(UnsafeReference), nameof(UnsafeReturn) })
            True(!InteropSignaturePolicy.CanPatch(typeof(Program).GetMethod(name, flags)), "nullable signature skipped before detour: " + name);
    }
}

// Game-free type-name fixture; the real IL2CPP type is resolved at runtime.
namespace Il2CppSystem { public class Nullable<T> { } }
