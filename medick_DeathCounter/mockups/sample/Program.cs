using System.Text.Json;
using medick_DeathCounter.Core;
// Sample fights pushed through the REAL Core: the mockup shows real analyzer/advisor/patterns output.
HitEvent H(double t, float a, string src, Element? el, string abil = null, string ail = null, bool dot = false, bool? crit = null)
{
    var h = new HitEvent { Time = t, Amount = a, Source = src, Ability = abil, Ailment = ail, IsDot = dot, Crit = crit, MaxHealth = 2400 };
    if (el is Element e) { h.ByElement = new float[Elements.Count]; h.ByElement[(int)e] = a; }
    return h;
}
var ctx = new DeathContext { Character = "Medick", CharacterClass = "Sentinel", Level = 87, Zone = "Monolith of Fate", MaxHealth = 2400, UtcNow = DateTime.UtcNow.AddMinutes(-3) };
var history = new List<DeathRecord>();
DeathRecord Fight(List<HitEvent> hits, double t, IEnumerable<string> ail = null) { var r = DeathAnalyzer.Analyze(hits, t, ctx, ail); r.Number = history.Count + 1; history.Add(r); return r; }
// older deaths for the Patterns tab
for (int i = 0; i < 4; i++) Fight(new() { H(9, 700, "Fire Imp", Element.Fire), H(9.6, 900, "Lagon", Element.Fire, "Tidal Fire"), H(10, 900, "Lagon", Element.Fire, "Tidal Fire") }, 10, new[] { "Ignite" });
for (int i = 0; i < 2; i++) Fight(new() { H(8, 600, "Bandit Brute", Element.Physical), H(9, 900, "Bandit Brute", Element.Physical), H(10, 1000, "Bandit Brute", Element.Physical) }, 10, new[] { "Armor Shred" });
Fight(Enumerable.Range(0, 20).Select(i => H(5 + i * 0.25, 130, "Plague Spider", null, null, "Poison", true)).ToList(), 10, new[] { "Poison" });
// the last death: a frozen one-shot crit
var last = Fight(new() { H(8.8, 300, "Frost Wraith", Element.Cold), H(9.4, 250, "Frost Wraith", Element.Cold, "Frost Claw"), H(10, 1900, "Rahyeh", Element.Void, "Void Rift Slam", crit: true) }, 10, new[] { "Freeze", "Shock" });
last.GameDeathInfo = "";
last.Defenses = new() { ["Res.Physical"] = 12, ["Res.Fire"] = 75, ["Res.Cold"] = 75, ["Res.Lightning"] = 52, ["Res.Necrotic"] = 60,
                        ["Res.Void"] = 38, ["Res.Poison"] = 45, ["Armor"] = 1180, ["Dodge"] = 420, ["CritAvoidance"] = 60, ["Ward"] = 0 };
var tips = Advisor.Suggest(last, history);
var pat = DeathPatterns.Build(history);
var opts = new JsonSerializerOptions { IncludeFields = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };
Console.WriteLine(JsonSerializer.Serialize(new {
    last, killerLine = last.KillerLine(), kind = last.KindLabel(), tips,
    count = history.Count, logLine = last.ToLogLine(),
    pat = new { pat.Deaths, killers = pat.TopKillers.Select(k => new { k.Name, k.Count }), shares = pat.ElementShares.Select(e => new { el = e.Element.ToString(), e.Share }),
                ailments = pat.TopAilments.Select(a => new { a.Name, a.Count }), kinds = pat.Kinds.Select(k => new { k = new DeathRecord { Kind = k.Key }.KindLabel().ToLowerInvariant(), n = k.Value }),
                prio = pat.Priorities.Select(p => new { p.Advice.Title, p.Advice.Body, p.Deaths }) }
}, opts));
