using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json.Nodes;
using medick_DeathCounter.Core;

static partial class Program
{
    static DeathRecord JournalDeath(string character, int number) => new() { Character = character, Number = number, Id = Guid.NewGuid().ToString("N"), UtcTime = DateTime.UtcNow };
    static void Test_Journal_NewDeathDoesNotStealSelectedContext()
    {
        var first = JournalDeath("A", 1); var second = JournalDeath("A", 2);
        var state = new JournalSelection(); state.Begin("A", new[] { first }, true);
        state.Refresh(new[] { first, second });
        Eq(ReassessmentLog.KeyFor(first), state.SelectedKey); True(state.NewDeathAvailable, "new death has an explicit navigation offer");
        state.SelectNewest(new[] { first, second }); Eq(ReassessmentLog.KeyFor(second), state.SelectedKey); True(!state.NewDeathAvailable, "explicit view acknowledges banner");
    }
    static void Test_Journal_CharacterScopeStaysFrozenWhileBrowsing()
    {
        var first = JournalDeath("A", 1); var other = JournalDeath("B", 1);
        var state = new JournalSelection(); state.Begin("A", new[] { first }, true);
        state.Refresh(new[] { first, other }); state.Select(other);
        Eq("A", state.Character); Eq(ReassessmentLog.KeyFor(first), state.SelectedKey); True(!state.NewDeathAvailable, "another character does not trigger current history banner");
    }
    static void Test_Journal_LateDetailEnrichmentRetainsDeathIdentity()
    {
        var first = JournalDeath("A", 1); first.Id = null;
        var state = new JournalSelection(); state.Begin("A", new[] { first }, true); string key = state.SelectedKey;
        first.Killer = "Rahyeh"; first.KillingElement = "Void";
        state.Refresh(new[] { first }); Eq(key, state.SelectedKey); True(!state.NewDeathAvailable, "enrichment is not another death");
    }
    static void Test_Journal_ReopenRetainsOlderSelectionWhenNotLastDeath()
    {
        var first = JournalDeath("A", 1); var second = JournalDeath("A", 2);
        var state = new JournalSelection(); state.Begin("A", new[] { first, second }, true); state.Select(first);
        state.Begin("A", new[] { first, second }, false); Eq(ReassessmentLog.KeyFor(first), state.SelectedKey);
        state.Refresh(new[] { first, second, JournalDeath("A", 3) }); True(state.NewDeathAvailable, "old character banner exists");
        state.Begin("B", Array.Empty<DeathRecord>(), false); Eq(null, state.SelectedKey); Eq("B", state.Character); True(!state.NewDeathAvailable, "old character banner clears");
    }
    static void Test_MenuFocus_CloseClickIsBlockedUntilReleased()
    {
        var gate = new MenuInputGate(); gate.Update(true, false); True(gate.Blocked, "open claims focus");
        for (int i = 0; i < 30; i++) gate.Update(false, true);
        True(gate.Blocked, "closing click cannot leak while held");
        gate.Update(false, false); True(gate.Blocked, "release frame is swallowed");
        gate.Update(false, false); True(!gate.Blocked, "neutral game controls return");
    }
    static void Test_MenuFocus_ReopenDuringReleaseKeepsOwnership()
    {
        var gate = new MenuInputGate(); gate.Update(true, false); gate.Update(false, false); gate.Update(true, false);
        gate.Update(false, false); True(gate.Blocked, "reopen starts a fresh release boundary");
        gate.Clear(); True(!gate.Blocked, "scene teardown cannot leave a stale gate");
    }
    static void Test_Check_DeletedParentKeepsFrozenComparisonAcrossRestart()
    {
        InAssessmentDirectory(dir =>
        {
            var death = HeavyFire(41); var updates = new ReassessmentLog(dir); updates.Load();
            var parent = SaveUpdate(updates, death, NewGear(60, 1400));
            var child = SaveUpdate(updates, death, NewGear(75, 1800), parent);
            Eq(60f, child.PreviousStats["Res.Fire"]); Eq(parent.UtcTime, child.PreviousUtcTime.Value);
            parent.CurrentStats["Res.Fire"] = 1; Eq(60f, child.PreviousStats["Res.Fire"]);
            True(updates.Delete(parent.Id), "delete parent only");
            var reload = new ReassessmentLog(dir); reload.Load(); var restored = reload.For(death).Single();
            Eq(child.Id, restored.Id); Eq(60f, restored.PreviousStats["Res.Fire"]); Eq(75f, restored.CurrentStats["Res.Fire"]);
            Eq(41f, restored.OriginalCapture.Defenses["Res.Fire"]);
        });
    }
    static void Test_Check_LegacyCheckWithoutFrozenParentFieldsStillLoads()
    {
        InAssessmentDirectory(dir =>
        {
            var death = HeavyFire(41); var updates = new ReassessmentLog(dir); updates.Load();
            var saved = SaveUpdate(updates, death, NewGear());
            string path = Path.Combine(dir, ReassessmentLog.FileName);
            var json = JsonNode.Parse(File.ReadAllText(path)).AsObject();
            json.Remove("PreviousStats"); json.Remove("PreviousUtcTime");
            File.WriteAllText(path, json.ToJsonString() + "\n");
            var reload = new ReassessmentLog(dir); reload.Load(); var loaded = reload.For(death).Single();
            Eq(saved.Id, loaded.Id); Eq(saved.Assessment.Rating.Grade, loaded.Assessment.Rating.Grade);
            Eq(null, loaded.PreviousStats); Eq(null, loaded.PreviousUtcTime);
        });
    }
    static void Test_BossLabels_TenTimelineHarbingersHaveParentAndTimeline()
    {
        string[] ids = { "defilement", "pride", "hatred", "war", "chaos", "treason", "destruction", "fear", "tyranny", "ash" };
        foreach (string id in ids)
        {
            var boss = BossCatalog.All.Single(b => b.Id == "harbinger-" + id);
            True(BossContextLabels.Timeline(boss) != null, id + " timeline");
            True(BossContextLabels.Label(boss).StartsWith("Spawns after ", StringComparison.Ordinal), id + " parent");
        }
    }
    static void Test_BossLabels_LagonCampaignIsNotMonolith()
    {
        var campaign = BossCatalog.All.Single(b => b.Id == "lagon-campaign");
        var monolith = BossCatalog.All.Single(b => b.Id == "lagon-monolith");
        Eq(null, BossContextLabels.Timeline(campaign)); Eq("Campaign encounter", BossContextLabels.Label(campaign));
        Eq("Ending the Storm", BossContextLabels.Timeline(monolith));
        True(BossContextLabels.Label(BossCatalog.All.Single(b => b.Id == "harbinger-chaos")).Contains("Lagon"), "Chaos follows Lagon");
    }
    static void Test_BossLabels_UnknownParentDoesNotInventAssociation()
    {
        var unknown = BossCatalog.All.Single(b => b.Id == "harbinger-cruelty");
        Eq("Follow-up boss not mapped", BossContextLabels.Label(unknown));
        Eq(null, BossContextLabels.Timeline(unknown));
    }
}
