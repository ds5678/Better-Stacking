using HarmonyLib;
using Il2Cpp;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace BetterStacking
{

    internal static class Patches
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

        internal static bool PostFixTrack { get; private set; } = false;

        internal static float PostfixCondition { get; private set; } = 0;

        internal static GearItem? PostfixStack { get; private set; } = null;

        internal static GearItem? PostfixGearToAdd { get; private set; } = null;

        internal static float PostfixConstraint { get; private set; } = 0;

        [HarmonyPatch]
        internal static class PlayerManager_TryAddToExistingStackable
        {

            static MethodBase? TargetMethod()
            {
                MethodInfo[] methods = typeof(PlayerManager).GetMethods();
                foreach (MethodInfo m in methods)
                {
                    if (m.Name == nameof(PlayerManager.TryAddToExistingStackable) && m.ReturnType == typeof(bool) && !m.IsGenericMethod && m.GetParameters().Length == 4)
                    {
                        return m;
                    }
                }
                Implementation.LogWarning("PlayerManager.TryAddToExistingStackable not found for patch.");
                return null;
            }

            internal static bool Prefix(ref GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem)
            {
                existingGearItem = new GearItem();

                if (normalizedCondition == 0)
                {
                    return false;
                }

                if (UseDefaultStacking(gearToAdd))
                {
                    return true;
                }

                GearItem targetStack = GameManager.GetInventoryComponent().GetClosestMatchStackable(gearToAdd.GearItemData, normalizedCondition);

                if (!CanBeMerged(targetStack, gearToAdd))
                {
                    return false;
                }

                if (targetStack.m_StackableItem != null)
                {
                    MergeIntoStack(normalizedCondition, numUnits, targetStack, gearToAdd);
                    existingGearItem = targetStack;
                    return true;
                }

                return false;
            }

        }

        [HarmonyPatch(typeof(ConsoleManager), nameof(ConsoleManager.CONSOLE_gear_add))]
        internal class ConsoleManager_CONSOLE_gear_add
        {

            private static void Postfix()
            {
                // are we tracking a postfix patch ?
                if (PostFixTrack)
                {
                    // correct the stack(s) conditions
                    PostfixStack.CurrentHP = PostfixCondition * PostfixStack.m_GearItemData.m_MaxHP;
                    PostfixGearToAdd.CurrentHP = PostfixCondition * PostfixGearToAdd.m_GearItemData.m_MaxHP;

                    // reset the items constraints
                    PostfixStack.m_StackableItem.m_StackConditionDifferenceConstraint = PostfixConstraint;
                    PostfixGearToAdd.m_StackableItem.m_StackConditionDifferenceConstraint = PostfixConstraint;

                }

                // reset the static values to avoid any conflicts
                ResetPostfixParams();
            }

        }

        internal static bool CanBeMerged([NotNullWhen(true)] GearItem? target, [NotNullWhen(true)] GearItem? item)
        {
            return target != null && item != null;
        }

        internal static void ResetPostfixParams()
        {
            PostFixTrack = false;
            PostfixCondition = 0;
            PostfixStack = null;
            PostfixGearToAdd = null;
            PostfixConstraint = 0;
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

        internal static bool UseDefaultStacking(GearItem? gearItem)
        {
            return gearItem == null || !STACK_MERGE.Contains(gearItem.name);
        }

    }
}