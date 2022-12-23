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




    internal static void MergeIntoStack(float normalizedCondition, int numUnits, GearItem targetStack, GearItem gearToAdd)
    {

        // check for console added items
        if (uConsole.IsOn())
        {

            // normalizedCondition is always 1 here when added via console, this gets changed later in CONSOLE_gear_add
            // so we recalculate it from the console params (or game logic) to be used in the below calculations

            // NO condition specified (only 2 params)
            if (uConsole.GetNumParameters() == 2)
            {
                // calc default/random condition as per game logic
                gearToAdd.RollGearCondition(false);
                normalizedCondition = gearToAdd.GetNormalizedCondition();
            }

            // condition WAS specified (3rd param)
            if (uConsole.GetNumParameters() == 3)
            {
                // set the PostFixTrack to enable the CONSOLE_gear_add.postfix logic
                Patches.PostFixTrack = true;
                // calc condition based on console params
                float consoleCondition = Mathf.Clamp(float.Parse(uConsole.m_Argv[3]), 0, 100) / 100f;
                // apply the new condition and override normalizedCondition with the new value
                gearToAdd.CurrentHP = gearToAdd.m_GearItemData.m_MaxHP * consoleCondition;
                normalizedCondition = gearToAdd.GetNormalizedCondition();

            }

        }

        int targetCount = numUnits + targetStack.m_StackableItem.m_Units;
        float targetCondition = (numUnits * normalizedCondition + targetStack.m_StackableItem.m_Units * targetStack.GetNormalizedCondition()) / targetCount;

        // set static variables for the postfox patch
        if (Patches.PostFixTrack == true)
        {
            Patches.PostfixCondition = targetCondition;
            Patches.PostfixStack = targetStack;
            Patches.PostfixGearToAdd = gearToAdd;
            Patches.PostfixConstraint = targetStack.m_StackableItem.m_StackConditionDifferenceConstraint;
        }

        // convince the game logic these stacks can merge between 0% -> 100% condition
        // the game keeps the existing stack condition %
        // (avoids having to destroy anything, we let the game do it all)
        targetStack.m_StackableItem.m_StackConditionDifferenceConstraint = 100f;
        gearToAdd.m_StackableItem.m_StackConditionDifferenceConstraint = 100f;

        // change the target stack to the new calculated condition
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
            MelonLogger.Warning("Failed to split stack of {0}: {1}.", gearItem.name, e);
        }
    }

    internal static bool UseDefaultStacking(GearItem gearItem)
    {
        return gearItem == null || !STACK_MERGE.Contains(gearItem.name);
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