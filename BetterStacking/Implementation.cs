using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace BetterStacking;

internal class Implementation : MelonMod
{
    private static readonly string[] STACK_MERGE =
    {
        "GEAR_BirchSaplingDried",
        "GEAR_BearHideDried",
        "GEAR_BottleAntibiotics",
        "GEAR_BottlePainKillers",
        "GEAR_CoffeeTin",
        "GEAR_GreenTeaPackage",
        "GEAR_GutDried",
        "GEAR_LeatherDried",
        "GEAR_LeatherHideDried",
        "GEAR_MapleSaplingDried",
        "GEAR_MooseHideDried",
        "GEAR_PackMatches",
        "GEAR_RabbitPeltDried",
        "GEAR_WolfPeltDried",
        "GEAR_WoodMatches",
    };

    public override void OnInitializeMelon()
    {
        Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");

        //MakeStackable("GEAR_Accelerant");
        //MakeStackable("GEAR_CanOpener");
        //MakeStackable("GEAR_FlareA");
        //MakeStackable("GEAR_BlueFlare");
        //MakeStackable("GEAR_Hacksaw");
        //MakeStackable("GEAR_Hammer");
        //MakeStackable("GEAR_Hatchet");
        //MakeStackable("GEAR_HatchetImprovised");
        //MakeStackable("GEAR_Knife");
        //MakeStackable("GEAR_KnifeImprovised");
        //MakeStackable("GEAR_Prybar");
        //MakeStackable("GEAR_SewingKit");
        //MakeStackable("GEAR_HookAndLine");
        //MakeStackable("GEAR_SharpeningStone");
        //MakeStackable("GEAR_SimpleTools");
        //MakeStackable("GEAR_HighQualityTools");
    }

    internal static void AddToExistingStack(GearItem gearItem)
    {
        if (gearItem == null || gearItem.m_StackableItem == null)
        {
            return;
        }

        try
        {
            AddtoExistingStackWithException(gearItem);
        }
        catch (System.Exception e)
        {
            Log("Failed to merge into stack of {0}: {1}.", gearItem.name, e);
        }
    }

    internal static bool CanBeMerged(GearItem target, GearItem item)
    {
        return target != null && item != null && CanBeMerged(target.m_FlareItem, item.m_FlareItem);
    }

    internal static void Log(string message) => MelonLogger.Msg(message);
    internal static void Log(string message, params object[] parameters) => MelonLogger.Msg(message, parameters);

    internal static void MergeIntoStack(float normalizedCondition, int numUnits, GearItem targetStack)
    {
        int targetCount = numUnits + targetStack.m_StackableItem.m_Units;
        float targetCondition = (numUnits * normalizedCondition + targetStack.m_StackableItem.m_Units * targetStack.GetNormalizedCondition()) / targetCount;

        targetStack.m_StackableItem.m_Units = targetCount;
        targetStack.CurrentHP = targetCondition * targetStack.m_GearItemData.m_MaxHP;
    }

    internal static void SplitStack(GearItem gearItem)
    {
        if (gearItem == null || gearItem.m_StackableItem == null)
        {
            return;
        }

        try
        {
            SplitStackWithException(gearItem);
        }
        catch (System.Exception e)
        {
            Log("Failed to split stack of {0}: {1}.", gearItem.name, e);
        }
    }

    internal static bool UseDefaultStacking(GearItem gearItem)
    {
        return gearItem == null || !STACK_MERGE.Contains(gearItem.name);
    }

    private static void AddtoExistingStackWithException(GearItem gearItem)
    {
        bool useDefaultStacking = UseDefaultStacking(gearItem);
        Inventory inventory = GameManager.GetInventoryComponent();

        GearItem[] targetItems = inventory.GearInInventory(gearItem.name);
        foreach (GearItem eachTargetItem in targetItems)
        {
            if (eachTargetItem == gearItem)
            {
                continue;
            }

            if (useDefaultStacking && eachTargetItem.GetRoundedCondition() == gearItem.GetRoundedCondition())
            {
                eachTargetItem.m_StackableItem.m_Units++;
                inventory.DestroyGear(gearItem.gameObject);
                return;
            }

            if (!useDefaultStacking && CanBeMerged(eachTargetItem, gearItem))
            {
                MergeIntoStack(gearItem.GetNormalizedCondition(), 1, eachTargetItem);
                inventory.DestroyGear(gearItem.gameObject);
                return;
            }
        }
    }

    private static bool CanBeMerged(FlareItem target, FlareItem item)
    {
        if (target == null || item == null)
        {
            return true;
        }

        if (target.IsBurning() || item.IsBurning())
        {
            return false;
        }

        if (target.IsBurnedOut() != item.IsBurnedOut())
        {
            return false;
        }

        return true;
    }

    private static void MakeStackable(string prefabName)
    {
        GameObject gameObject = Resources.Load(prefabName).Cast<GameObject>();

        StackableItem stackableItem = gameObject.GetComponent<StackableItem>();
        if (stackableItem == null)
        {
            stackableItem = gameObject.AddComponent<StackableItem>();
            stackableItem.m_ShareStackWithGear = Array.Empty<StackableItem>();
            stackableItem.m_StackSpriteName = string.Empty;
            stackableItem.m_Units = 1;
            stackableItem.m_UnitsPerItem = 1;
        }
    }

    private static void SplitStackWithException(GearItem gearItem)
    {
        int count = gearItem.m_StackableItem.m_Units;
        if (count <= 1)
        {
            return;
        }

        gearItem.m_StackableItem.m_Units = 1;

        GearItem clone = GearItem.InstantiateGearItem(gearItem.name);
        clone.m_StackableItem.m_Units = count - 1;
        clone.CurrentHP = gearItem.CurrentHP;

        GameManager.GetInventoryComponent().AddGear(clone);
    }
}