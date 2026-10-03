using System.Linq;
using medick_DeathCounter.Core;

// Pins from the boss fact-check that current Core types can already answer.
// Encounter grades and new roster profiles stay in the research JSON.
static partial class Program
{
    static void Test_Catalog_FactcheckRemovesUnsupportedStarterTips()
    {
        foreach (var profile in BossCatalog.Starter)
        {
            foreach (var tip in profile.Tips)
            {
                True(!tip.Contains("--"), profile.Id + " tip has a double dash");
                True(!tip.Contains("before it detonates"), profile.Id);
                True(!tip.Contains("safe timing"), profile.Id);
                True(!tip.Contains("leave the landing telegraph"), profile.Id);
                True(!tip.Contains("avoid its telegraph"), profile.Id);
                True(!tip.Contains("keep a clear route"), profile.Id);
                True(!tip.Contains("Campaign and Monolith variants may differ"), profile.Id);
            }
            True(!profile.Mechanics.Contains("Campaign and Monolith variants may differ"), profile.Id);
        }

        Eq(0, BossCatalog.Starter.Single(p => p.Id == "lagon").Tips.Count);
        Eq(0, BossCatalog.Starter.Single(p => p.Id == "emperor-corpses").Tips.Count);
        Eq(0, BossCatalog.Starter.Single(p => p.Id == "heorot").Tips.Count);
        Eq(0, BossCatalog.Starter.Single(p => p.Id == "julra").Tips.Count);
        var hatred = BossCatalog.Starter.Single(p => p.Id == "harbinger-hatred");
        Eq(1, hatred.Tips.Count);
        Eq("A Harbinger imitation is not interchangeable with the original boss's full move set.", hatred.Tips[0]);
        True(hatred.Mechanics.Contains("Void Rahyeh Dive Bomb"), "the named attack stays a mechanics claim");
        Eq(0, hatred.DamageTypes.Count);
        Eq(0, BossCatalog.Starter.Single(p => p.Id == "lagon").DamageTypes.Count);
        True(!BossCatalogSchema.AbilityNameEstablishesDamage("Void Rahyeh Dive Bomb", "Void"), "ability title");
        Eq(null, BossCatalog.Match(new DeathRecord { IsBossFight = true, Killer = "Harbinger of Hatred's Void Rahyeh" }));
        Eq("harbinger-hatred", BossCatalog.Match(new DeathRecord { IsBossFight = true, Killer = "Harbinger of Hatred" }).Id);
    }

    static void Test_Math_Lane5ResistanceAndPoolPins()
    {
        const float tol = 0.0001f;
        float fireGap = MitigationMath.ResistanceRatio(41, 75, 0, 75);
        Near(1f / 1.34f, fireGap, "L5-M1", tol);
        float newDamage = 1700f * fireGap;
        Near(1268.6567f, newDamage, "L5-M1 scaled hit", 0.05f);
        True(newDamage > 1000f * 1.10f, "scaled hit stays above the 10 percent band around remaining health");

        Near(1f, MitigationMath.ResistanceRatio(75, 95, 0, 75), "L5-M2 overcap without shred", tol);

        var endurance = MitigationMath.PreviewEndurance(2200, 400, 400, 20, 60, DamageMeaning.PostMitigationWithWard);
        Near(0.8181818182f, endurance.Ratio, "L5-M3", tol);
        Near(1800f, endurance.NewDamage, "L5-M3 new damage", 0.1f);
        Near(1800f, endurance.RemainingEffectiveHealth, "L5-M3 remaining", 0.1f);
        True(endurance.Sentence().Contains("may or may not"), endurance.Sentence());

        var absorbed = MitigationMath.PreviewPoolIncrease(2200, 400, 500, DamageMeaning.PostMitigationWithWard);
        Eq(true, absorbed.Survived);
        True(absorbed.Sentence().Contains("would have absorbed this exact hit"), absorbed.Sentence());
        Eq(false, MitigationMath.PreviewPoolIncrease(2200, 400, 200, DamageMeaning.PostMitigationWithWard).Survived);

        float voidGap = MitigationMath.ResistanceRatio(40, 75, 0, 75);
        Near(1f / 1.35f, voidGap, "L5-M5", tol);
        Eq(26, (int)System.Math.Round((1f - voidGap) * 100f));
        var noAmount = MitigationMath.PreviewResistance(float.NaN, float.NaN, 40, 75, 0, 100, DamageMeaning.PostMitigationWithWard);
        True(float.IsNaN(noAmount.NewDamage), "a Harbinger element with no recorded amount has no new damage");

        Near(0f, MitigationMath.EffectiveResistance(95, 20, 75), "L5-M6 overcap", tol);
        Near(-20f, MitigationMath.EffectiveResistance(75, 20, 75), "L5-M6 at cap", tol);
        Eq(10, MitigationMath.PlayerResShredMaxStacks);

        var dot = MitigationMath.PreviewArmor(300, 40, 2763, 2763, 100, false, true, DamageMeaning.PostMitigationWithWard);
        True(float.IsNaN(dot.Ratio), "L5-M7");
        True(dot.Note != null && dot.Note.Contains("damage over time"), dot.Note ?? "");
    }
}

