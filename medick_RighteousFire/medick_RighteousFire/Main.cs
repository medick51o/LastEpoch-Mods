// ================================================================
//  Main.cs  —  medick_RighteousFire  v1.0.0
//
//  "The fire that burns within you... burns everything else too."
//                                                        — Pohx
//
//  A tribute belt that brings the Righteous Fire fantasy to
//  Last Epoch. Equip it. Charge. Watch things explode.
//  Offline only. Play responsibly.
// ================================================================

[assembly: MelonInfo(typeof(medick_RighteousFire.RighteousFireMod),
    "medick_RighteousFire", "1.0.0", "medick")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]

namespace medick_RighteousFire;

public class RighteousFireMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("[RighteousFire] v1.0.0 — 'The fire that burns within you... burns everything else too.' — Pohx");
        MelonLogger.Msg("[RighteousFire] OFFLINE ONLY. Play responsibly.");
    }

    public override void OnUpdate()
    {
        // Phase 1: register item until complete
        if (!Item_RighteousFire.IsFullyRegistered)
        {
            Item_RighteousFire.TryRegister();
            return;
        }

        // Phase 2: run procs every frame
        Trigger.OnUpdate();
    }
}
