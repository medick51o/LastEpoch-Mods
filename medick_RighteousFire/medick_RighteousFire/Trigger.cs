// ================================================================
//  Trigger.cs  —  medick_RighteousFire
//
//  All on-equip proc logic for the RIGHTEOUS FIRE belt.
//
//  RF Aura        — fire damage pulse every 0.5s to nearby enemies
//  Shield Charge  — auto-fires toward nearest enemy every 2s
//  Fire Trap      — auto-cast every 3s at a nearby enemy
//  Potion burst   — doubles aura radius/damage for 8s on potion use
//
//  On first load, logs ALL available ability names so we can
//  confirm "ShieldCharge" and trap ability exact strings.
// ================================================================

namespace medick_RighteousFire;

public static class Trigger
{
    // ── Ability references ────────────────────────────────────────────
    private static Ability _shieldCharge = null;
    private static Ability _fireTrap     = null;
    private static bool    _abilitiesSearched = false;

    // ── Timers ────────────────────────────────────────────────────────
    private static DateTime _lastAura    = DateTime.Now;
    private static DateTime _lastCharge  = DateTime.Now;
    private static DateTime _lastTrap    = DateTime.Now;

    private const double AURA_CD   = 0.5;
    private const double CHARGE_CD = 2.0;
    private const double TRAP_CD   = 3.0;
    private const float  AURA_RADIUS = 12f;

    // Potion burst state
    private static bool   _burstActive   = false;
    private static DateTime _burstExpiry = DateTime.Now;
    private const double BURST_DURATION  = 8.0;

    // ── Main tick — called from Main.OnUpdate ─────────────────────────
    public static void OnUpdate()
    {
        if (!Item_RighteousFire.IsEquipped()) return;
        if (Refs.player_actor == null) return;

        if (!_abilitiesSearched) SearchAbilities();

        double now_aura   = (DateTime.Now - _lastAura).TotalSeconds;
        double now_charge = (DateTime.Now - _lastCharge).TotalSeconds;
        double now_trap   = (DateTime.Now - _lastTrap).TotalSeconds;

        float effectiveRadius = _burstActive ? AURA_RADIUS * 2f : AURA_RADIUS;

        // Expire burst
        if (_burstActive && (DateTime.Now - _burstExpiry).TotalSeconds > 0)
            _burstActive = false;

        // RF Aura
        if (now_aura >= AURA_CD)
        {
            RFAura(effectiveRadius);
            _lastAura = DateTime.Now;
        }

        // Shield Charge
        if (now_charge >= CHARGE_CD && _shieldCharge != null)
        {
            FireShieldCharge();
            _lastCharge = DateTime.Now;
        }

        // Fire Trap
        if (now_trap >= TRAP_CD && _fireTrap != null)
        {
            FireTrap();
            _lastTrap = DateTime.Now;
        }
    }

    // ── RF Aura — fire damage pulse around player ─────────────────────
    // Applies a stacking fire damage buff to all nearby enemies.
    private static void RFAura(float radius)
    {
        try
        {
            Vector3 origin = Refs.player_actor.position();
            Collider[] hits = Physics.OverlapSphere(origin, radius);

            foreach (Collider col in hits)
            {
                try
                {
                    Actor enemy = col.gameObject.GetComponent<Actor>();
                    if (enemy == null) continue;
                    if (enemy == Refs.player_actor) continue;

                    float damage = _burstActive ? 200f : 100f;

                    // Apply fire damage as a short buff that ticks damage
                    enemy.statBuffs.addBuff(
                        0.6f,           // duration — slightly longer than tick rate so it overlaps
                        SP.Damage,
                        damage,         // added flat fire damage
                        0f,
                        null,
                        AT.Fire,
                        "RF_Aura"
                    );
                }
                catch { }
            }

            // Self-burn — you are also on fire (regen sustains this)
            Refs.player_actor.statBuffs.removeBuffsWithName("RF_SelfBurn");
            Refs.player_actor.statBuffs.addBuff(
                0.6f,
                SP.Damage,
                -50f,   // negative = taking damage (self burn)
                0f,
                null,
                AT.Fire,
                "RF_SelfBurn"
            );
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] Aura failed: {ex.Message}"); }
    }

    // ── Shield Charge — auto-fires toward nearest enemy ───────────────
    private static void FireShieldCharge()
    {
        try
        {
            Actor target = FindNearestEnemy(30f);
            if (target == null) return;

            float backup = _shieldCharge.manaCost;
            _shieldCharge.manaCost = 0f;
            _shieldCharge.castAtTargetFromConstructorAfterDelay(
                Refs.player_actor.abilityObjectConstructor,
                Vector3.zero,
                target.position(),
                0,
                UseType.Indirect
            );
            _shieldCharge.manaCost = backup;
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] ShieldCharge failed: {ex.Message}"); }
    }

    // ── Fire Trap — auto-cast at a nearby enemy ───────────────────────
    private static void FireTrap()
    {
        try
        {
            Actor target = FindNearestEnemy(25f);
            if (target == null) return;

            float backup = _fireTrap.manaCost;
            _fireTrap.manaCost = 0f;
            _fireTrap.castAtTargetFromConstructorAfterDelay(
                Refs.player_actor.abilityObjectConstructor,
                Vector3.zero,
                target.position(),
                0,
                UseType.Indirect
            );
            _fireTrap.manaCost = backup;
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] FireTrap failed: {ex.Message}"); }
    }

    // ── Potion burst trigger (called from PotionPatch) ────────────────
    public static void OnPotionUsed()
    {
        _burstActive = true;
        _burstExpiry = DateTime.Now.AddSeconds(BURST_DURATION);
        MelonLogger.Msg("[RighteousFire] POTION BURST — aura doubled for 8s. Go.");
    }

    // ── Ability search — runs once, logs all names for debugging ──────
    private static void SearchAbilities()
    {
        _abilitiesSearched = true;
        try
        {
            // Candidate names — tuned after first test run
            string[] chargeNames = { "ShieldCharge", "Shield Charge", "ShieldRush", "Shield Rush", "Lunge" };
            string[] trapNames   = { "FireTrap", "Fire Trap", "ForgeStrike", "Forge Strike",
                                     "VolcanicOrb", "Volcanic Orb", "Combustion", "Firestarter" };

            MelonLogger.Msg("[RighteousFire] Scanning abilities...");

            foreach (Ability ab in Resources.FindObjectsOfTypeAll<Ability>())
            {
                if (ab == null) continue;

                string n = ab.abilityName ?? ab.name ?? "";

                // Log everything so we can find the right names
                if (n.ToLower().Contains("fire") || n.ToLower().Contains("shield") ||
                    n.ToLower().Contains("charge") || n.ToLower().Contains("trap") ||
                    n.ToLower().Contains("forge") || n.ToLower().Contains("lunge") ||
                    n.ToLower().Contains("volcanic") || n.ToLower().Contains("rush"))
                {
                    MelonLogger.Msg($"[RighteousFire] Found ability: '{n}' (name='{ab.name}')");
                }

                // Try to match Shield Charge
                if (_shieldCharge == null)
                    foreach (string s in chargeNames)
                        if (n.Equals(s, StringComparison.OrdinalIgnoreCase)) { _shieldCharge = ab; break; }

                // Try to match Fire Trap
                if (_fireTrap == null)
                    foreach (string s in trapNames)
                        if (n.Equals(s, StringComparison.OrdinalIgnoreCase)) { _fireTrap = ab; break; }
            }

            MelonLogger.Msg($"[RighteousFire] Shield Charge: {(_shieldCharge != null ? "FOUND" : "NOT FOUND")}");
            MelonLogger.Msg($"[RighteousFire] Fire Trap:     {(_fireTrap != null ? "FOUND" : "NOT FOUND")}");
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] Ability search failed: {ex.Message}"); }
    }

    // ── Find nearest enemy within radius ─────────────────────────────
    private static Actor FindNearestEnemy(float radius)
    {
        Actor nearest = null;
        float nearestDist = float.MaxValue;
        Vector3 origin = Refs.player_actor.position();

        try
        {
            Collider[] hits = Physics.OverlapSphere(origin, radius);
            foreach (Collider col in hits)
            {
                try
                {
                    Actor a = col.gameObject.GetComponent<Actor>();
                    if (a == null || a == Refs.player_actor) continue;
                    float d = Vector3.Distance(origin, a.position());
                    if (d < nearestDist) { nearestDist = d; nearest = a; }
                }
                catch { }
            }
        }
        catch { }

        return nearest;
    }
}
