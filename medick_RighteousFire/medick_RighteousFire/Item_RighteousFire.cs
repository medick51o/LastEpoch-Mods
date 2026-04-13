// ================================================================
//  Item_RighteousFire.cs  —  medick_RighteousFire
//
//  Defines and registers the RIGHTEOUS FIRE unique belt.
//
//  Slot      : Belt (base_type = 2)
//  Level req : 1  (equippable immediately on any character)
//  Legendary : 4 LP
//
//  Passive stats:
//    +500 Health
//    +150 Health Regen/s
//    +75% Fire Resistance
//    +200% Increased Fire Damage
//    +80% Movement Speed
//    +40 Throwing Speed (for trap feel)
//
//  On-equip procs (handled in Trigger.cs):
//    RF Aura   — fire pulse every 0.5s to nearby enemies
//    Shield Charge — auto-fires toward nearest enemy every 2s
//    Fire Trap — auto-cast every 3s, chains fire explosions
//    Potion    — aura burst for 8s
// ================================================================

namespace medick_RighteousFire;

public static class Item_RighteousFire
{
    // ── IDs — pick values well away from known mods ──────────────────
    public static readonly byte   base_type = 2;    // belt
    public static readonly int    base_id   = 20;   // unused base subtype
    public static readonly ushort unique_id = 510;  // unused unique ID

    // ── Registration state ────────────────────────────────────────────
    public static bool AddedToBasicList  = false;
    public static bool AddedToUniqueList = false;
    public static bool AddedToDictionary = false;
    public static bool IsFullyRegistered => AddedToBasicList && AddedToUniqueList && AddedToDictionary;

    // ── Localization keys (matched in Localization_Patch.cs) ──────────
    public static string Get_Subtype_Name()  => $"Item_SubType_Name_{base_type}_{base_id}";
    public static string Get_Unique_Name()   => $"Unique_Name_{unique_id}";
    public static string Get_Unique_Lore()   => $"Unique_Lore_{unique_id}";
    public static string Get_Tooltip_0()     => $"Unique_Tooltip_0_{unique_id}";

    // ── Registration entry point ──────────────────────────────────────
    public static void TryRegister()
    {
        if (!Refs.IsReady) { Refs.Refresh(); return; }

        if (!AddedToBasicList)  AddToBasicList();
        if (!AddedToUniqueList) AddToUniqueList();
        if (!AddedToDictionary) AddToDictionary();
    }

    // ── Step 1: register base subtype ─────────────────────────────────
    private static void AddToBasicList()
    {
        try
        {
            Refs.item_list.EquippableItems[base_type].subItems.Add(BasicItem());
            AddedToBasicList = true;
            MelonLogger.Msg("[RighteousFire] Base subtype registered.");
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] BasicList failed: {ex.Message}"); }
    }

    private static ItemList.EquipmentItem BasicItem()
    {
        return new ItemList.EquipmentItem
        {
            classRequirement    = ItemList.ClassRequirement.None,
            subClassRequirement = ItemList.SubClassRequirement.None,
            implicits           = Implicits(),
            cannotDrop          = true,
            itemTags            = ItemLocationTag.None,
            levelRequirement    = 1,
            name                = Get_Subtype_Name(),
            subTypeID           = base_id
        };
    }

    private static Il2CppSystem.Collections.Generic.List<ItemList.EquipmentImplicit> Implicits()
    {
        var list = new Il2CppSystem.Collections.Generic.List<ItemList.EquipmentImplicit>();
        list.Add(new ItemList.EquipmentImplicit
        {
            property       = SP.Health,
            type           = BaseStats.ModType.ADDED,
            tags           = AT.None,
            implicitValue  = 500,
            implicitMaxValue = 500,
            specialTag     = 0
        });
        return list;
    }

    // ── Step 2: register unique entry ─────────────────────────────────
    private static void AddToUniqueList()
    {
        try
        {
            UniqueList.getUnique(0); // force-initialize singleton
            Refs.unique_list.uniques.Add(UniqueItem());
            AddedToUniqueList = true;
            MelonLogger.Msg("[RighteousFire] Unique entry registered.");
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] UniqueList failed: {ex.Message}"); }
    }

    private static UniqueList.Entry UniqueItem()
    {
        return new UniqueList.Entry
        {
            name            = Get_Unique_Name(),
            displayName     = Get_Unique_Name(),
            uniqueID        = unique_id,
            isSetItem       = false,
            setID           = 0,

            // Level 1 — equippable on any fresh character
            overrideLevelRequirement = true,
            levelRequirement         = 1,

            // 4 LP — max legendary potential
            legendaryType = UniqueList.LegendaryType.LegendaryPotential,
            overrideEffectiveLevelForLegendaryPotential = true,
            effectiveLevelForLegendaryPotential         = 100, // high level = max LP rolls

            canDropRandomly = true,
            rerollChance    = 1,
            itemModelType   = UniqueList.ItemModelType.Unique,
            subTypeForIM    = 0,
            baseType        = base_type,
            subTypes        = SubTypes(),
            mods            = Mods(),
            tooltipDescriptions = TooltipDescriptions(),
            tooltipEntries  = TooltipEntries(),
            loreText        = Get_Unique_Lore(),
            oldSubTypeID    = 0,
            oldUniqueID     = 0
        };
    }

    private static Il2CppSystem.Collections.Generic.List<byte> SubTypes()
    {
        var list = new Il2CppSystem.Collections.Generic.List<byte>();
        list.Add((byte)base_id);
        return list;
    }

    private static Il2CppSystem.Collections.Generic.List<UniqueItemMod> Mods()
    {
        var list = new Il2CppSystem.Collections.Generic.List<UniqueItemMod>();

        // +500 Health
        list.Add(new UniqueItemMod { canRoll = false, property = SP.Health,
            tags = AT.None, type = BaseStats.ModType.ADDED,
            value = 500f, maxValue = 500f, hideInTooltip = false });

        // +150 Health Regen per second
        list.Add(new UniqueItemMod { canRoll = false, property = SP.HealthRegen,
            tags = AT.None, type = BaseStats.ModType.ADDED,
            value = 150f, maxValue = 150f, hideInTooltip = false });

        // +75% Fire Resistance
        list.Add(new UniqueItemMod { canRoll = false, property = SP.FireResistance,
            tags = AT.Fire, type = BaseStats.ModType.INCREASED,
            value = 0.75f, maxValue = 0.75f, hideInTooltip = false });

        // +200% Increased Fire Damage
        list.Add(new UniqueItemMod { canRoll = false, property = SP.Damage,
            tags = AT.Fire, type = BaseStats.ModType.INCREASED,
            value = 2.0f, maxValue = 2.0f, hideInTooltip = false });

        // +80% Movement Speed (resolved at runtime to avoid enum name mismatch)
        list.Add(new UniqueItemMod { canRoll = false, property = SafeSP("MovementSpeed", SP.Damage),
            tags = AT.None, type = BaseStats.ModType.INCREASED,
            value = 0.8f, maxValue = 0.8f, hideInTooltip = false });

        // +40% Attack Speed (proxy for throwing speed until correct SP found)
        list.Add(new UniqueItemMod { canRoll = false, property = SP.AttackSpeed,
            tags = AT.None, type = BaseStats.ModType.INCREASED,
            value = 0.4f, maxValue = 0.4f, hideInTooltip = false });

        return list;
    }

    private static Il2CppSystem.Collections.Generic.List<ItemTooltipDescription> TooltipDescriptions()
    {
        // ItemTooltipDescription fields verified at runtime — empty for now,
        // description text is injected via localization key in Localization_Patch.cs
        return new Il2CppSystem.Collections.Generic.List<ItemTooltipDescription>();
    }

    private static Il2CppSystem.Collections.Generic.List<UniqueModDisplayListEntry> TooltipEntries()
    {
        var list = new Il2CppSystem.Collections.Generic.List<UniqueModDisplayListEntry>();
        list.Add(new UniqueModDisplayListEntry(0));   // Health
        list.Add(new UniqueModDisplayListEntry(1));   // Health Regen
        list.Add(new UniqueModDisplayListEntry(2));   // Fire Resistance
        list.Add(new UniqueModDisplayListEntry(3));   // Fire Damage
        list.Add(new UniqueModDisplayListEntry(4));   // Movement Speed
        list.Add(new UniqueModDisplayListEntry(5));   // Attack Speed
        list.Add(new UniqueModDisplayListEntry(128)); // Tooltip description text
        return list;
    }

    // ── Step 3: add to lookup dictionary ─────────────────────────────
    private static void AddToDictionary()
    {
        try
        {
            UniqueList.Entry found = null;
            foreach (UniqueList.Entry entry in Refs.unique_list.uniques)
            {
                if (entry.uniqueID == unique_id && entry.name == Get_Unique_Name())
                {
                    found = entry;
                    break;
                }
            }
            if (found == null) return;

            Refs.unique_list.entryDictionary.Add(unique_id, found);
            AddedToDictionary = true;
            MelonLogger.Msg("[RighteousFire] Dictionary entry added. Item fully registered — go find it, Exile.");
            DumpBeltUniques();
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] Dictionary failed: {ex.Message}"); }
    }

    // ── Debug dump — logs all belt uniques so we can find Immolator's Oblation ID ─
    private static bool _dumpDone = false;
    public static void DumpBeltUniques()
    {
        if (_dumpDone) return;
        _dumpDone = true;
        try
        {
            MelonLogger.Msg("[RighteousFire] ── BELT UNIQUE DUMP ──────────────────────");
            foreach (UniqueList.Entry e in Refs.unique_list.uniques)
            {
                if (e.baseType == base_type)  // belt slot
                    MelonLogger.Msg($"[RighteousFire]  uniqueID={e.uniqueID,4}  subTypeForIM={e.subTypeForIM,3}  modelType={e.itemModelType}  name='{e.name}'");
            }
            MelonLogger.Msg("[RighteousFire] ── END BELT DUMP ─────────────────────────");
        }
        catch (Exception ex) { MelonLogger.Warning($"[RighteousFire] Dump failed: {ex.Message}"); }
    }

    // ── Equipped check ────────────────────────────────────────────────
    public static bool IsEquipped()
    {
        try
        {
            return Refs.player_actor != null &&
                   Refs.player_actor.itemContainersManager.hasUniqueEquipped(unique_id);
        }
        catch { return false; }
    }

    // ── Runtime SP resolver — safe fallback if enum name doesn't exist ─
    private static SP SafeSP(string name, SP fallback)
    {
        try { return (SP)(int)Enum.Parse(typeof(SP), name); }
        catch { MelonLogger.Warning($"[RighteousFire] SP.{name} not found, using fallback"); return fallback; }
    }
}
