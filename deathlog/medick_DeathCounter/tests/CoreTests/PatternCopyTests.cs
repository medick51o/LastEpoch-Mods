using System;
using System.Collections.Generic;
using System.Linq;
using medick_DeathCounter.Core;

static partial class Program
{
    static void Test_PatternCopy_TitlesDoNotQuoteOneDeathsNumber()
    {
        var log = new List<DeathRecord> { AdviceDeath("Fire", 41), AdviceDeath("Fire", 60), AdviceDeath("Fire", 52) };
        for (int i = 0; i < log.Count; i++) { log[i].Number = i + 1; log[i].UtcTime = DateTime.UtcNow.AddMinutes(i); }
        var p = DeathPatterns.Build(log);
        var fire = p.Priorities.Single(x => x.Advice.Key == "res_Fire");
        Eq("Cap fire resistance", fire.Advice.Title);
        True(p.Priorities.All(x => !x.Advice.Title.Contains("you had") && !x.Advice.Title.Contains("%")), "pattern titles are not one death's reading");
        Eq("Raise endurance", Advisor.PatternTitle(new Advice { Title = "Raise endurance: you had 20%" }));
        Eq("Recover between hits", Advisor.PatternTitle(new Advice { Title = "Recover between hits" }));
    }
    static void Test_PatternCopy_MatchesPerDeathAdviceAndSpelling()
    {
        foreach (string key in new[] { "armor", "cc_shock", "shred_armor", "shred_res", "cc_stun", "cc_slow", "endurance", "avoid" })
        {
            string body = Advisor.PatternBody(new Advice { Key = key, Body = "" }, 2, 5);
            True(body.Contains("2 of 5 deaths"), key);
            True(!body.Contains("defence") && !body.Contains("--") && !body.Contains("Kill shredders") && !body.Contains("Overcap"), key + ": " + body);
        }
        True(Advisor.PatternBody(new Advice { Key = "armor" }, 2, 5).Contains("physical resistance"), "armor pattern keeps the separate resistance layer");
        True(Advisor.PatternBody(new Advice { Key = "cc_shock" }, 2, 5).Contains("above 75%"), "shock pattern explains why extra resistance helps");
    }
}
