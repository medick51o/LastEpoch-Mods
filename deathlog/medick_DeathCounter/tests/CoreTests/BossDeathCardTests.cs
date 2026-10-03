using System;
using System.Linq;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_BossCard_KillingMoveSurfacesOnDeathWithMatchingTip()
    {
        var death = new DeathRecord { Killer = "Harbinger of Pride", KillerAbility = "Spear Beam", KillingElement = "Fire", IsBossFight = true };
        var lines = BossFieldNotes.ForDeath(death, out var boss, out var move);
        Eq("harbinger-pride", boss.Id);
        Eq("Spear Beam", move.Name);
        True(lines[0].StartsWith("Spear Beam is reported as Fire damage", StringComparison.Ordinal), lines[0]);
        True(lines.Any(l => l.Contains("Spear Beam") && l.Contains("Move sideways")), "authored tip for the killing move is shown");
        True(!lines.Any(l => l.Contains("Trust the capture")), "matching capture needs no caveat");
    }
    static void Test_BossCard_GameLabelWithParenthesesMatchesGuideMove()
    {
        var death = new DeathRecord { Killer = "Harbinger of Pride", KillerAbility = "Melee Attack (Portal Spear)", KillingElement = "Lightning", IsBossFight = true };
        var lines = BossFieldNotes.ForDeath(death, out _, out var move);
        Eq("Portal Spear", move?.Name);
        True(lines.Any(l => l.Contains("Your capture recorded Lightning damage")), "capture outranks guide typing");
        var julra = new DeathRecord { Killer = "Julra", KillerAbility = "Lightning Bolt", IsBossFight = true };
        Eq("Summon Pillars (Lightning Bolt)", BossGuideMoves.KillingMove(julra)?.Name);
    }
    static void Test_BossCard_NeverGuessesBossOrMove()
    {
        Eq(0, BossFieldNotes.ForDeath(new DeathRecord { Killer = "Harbinger of Pride", KillerAbility = "Spear Beam", IsBossFight = false }, out var none, out _).Count);
        Eq(null, none);
        Eq(0, BossFieldNotes.ForDeath(new DeathRecord { Killer = "Aberroth", KillerAbility = "Void Beam", IsBossFight = true }, out var ambiguous, out _).Count);
        Eq(null, ambiguous);
        var unknown = BossFieldNotes.ForDeath(new DeathRecord { Killer = "Harbinger of Pride", KillerAbility = "Mystery Swipe", IsBossFight = true }, out var pride, out var noMove);
        Eq("harbinger-pride", pride.Id); Eq(null, noMove);
        True(unknown.Single().Contains("not in the move list"), "unknown ability stays unknown");
        Eq(null, BossGuideMoves.KillingMove(new DeathRecord { Killer = "Harbinger of Pride", IsBossFight = true }));
    }
    static void Test_BossCard_GuidePriorityOrdersPreparationWithoutAddingTypes()
    {
        var shaman = BossCatalog.All.Single(p => p.Id == "volcanic-shaman");
        Eq("Fire,Necrotic", string.Join(",", BossFieldNotes.PriorityResistances(shaman).Select(Elements.Name)));
        True(BossFieldNotes.Preparation(shaman)[0].StartsWith("Cap Fire and Necrotic resistance first.", StringComparison.Ordinal), BossFieldNotes.Preparation(shaman)[0]);
        var formosus = BossCatalog.All.Single(p => p.Id == "frost-lich-formosus");
        Eq("Cold, Necrotic and Lightning", BossFieldNotes.Preparation(formosus)[0].Substring(4, "Cold, Necrotic and Lightning".Length));
        var herald = BossCatalog.All.Single(p => p.Id == "herald-of-oblivion");
        True(BossFieldNotes.Preparation(herald)[0].Contains("Void and Physical"), "six-type pinnacle gets a usable order");
        var rahyeh = BossCatalog.All.Single(p => p.Id == "rahyeh-the-black-sun");
        True(BossFieldNotes.Preparation(rahyeh)[0].StartsWith("Fix gaps", StringComparison.Ordinal), "single-type fight keeps the normal note");
        foreach (var boss in BossCatalog.All)
        {
            True(BossFieldNotes.PriorityResistances(boss).All(e => boss.DamageTypes.Contains(e)), boss.Id + " priority stays inside coverage");
            True(BossFieldNotes.Preparation(boss).Count <= 3, boss.Id);
        }
        Eq(0, BossFieldNotes.PriorityResistances(BossCatalog.All.Single(p => p.Id == "morditas")).Count);
    }
    static void Test_BossCard_CopyIsPlainForEveryGuideMove()
    {
        foreach (var boss in BossCatalog.All)
            foreach (var move in BossGuideMoves.For(boss.Id))
            {
                var death = new DeathRecord { Killer = boss.Aliases.FirstOrDefault(), KillerAbility = move.Name, IsBossFight = true };
                foreach (string line in BossFieldNotes.ForDeath(death, out _, out _))
                    True(!line.Contains("--") && !line.Contains("Maxroll") && !line.Contains("survive") && !line.Contains("guarantee"), boss.Id + ": " + line);
            }
    }
}
