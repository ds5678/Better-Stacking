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

        private static bool PostFixTrack { get; set; } = false;

        private static float PostfixCondition { get; set; } = 0;

        private static GearItem? PostfixStack { get; set; } = null;

        private static GearItem? PostfixGearToAdd { get; set; } = null;

        private static float PostfixConstraint { get; set; } = 0;

        [HarmonyPatch]
        internal static class PlayerManager_TryAddToExistingStackable
        {

            static MethodBase? TargetMethod()
            {
                MethodInfo? targetMethod = typeof(PlayerManager)
                    .GetMethods()
                    .FirstOrDefault(
                        m => m.Name == nameof(PlayerManager.TryAddToExistingStackable)
                        && m.ReturnType == typeof(bool)
                        && !m.IsGenericMethod
                        && m.GetParameters().Length == 4
                        );
                if (targetMethod is null)
                {
                    Implementation.LogWarning("PlayerManager.TryAddToExistingStackable not found for patch.");
                }

                return targetMethod;
            }

            /// <summary>
            /// <para>GearItem existingGearItem is an out parameter of the original method</para>
            /// <para><em>(due to changes in how we apply the override logic, we no longer need to skip the original method)</em></para>
            /// </summary>
            /// <param name="gearToAdd"></param>
            /// <param name="normalizedCondition"></param>
            /// <param name="numUnits"></param>
            /// <param name="existingGearItem"></param>
            internal static void Prefix(GearItem gearToAdd, float normalizedCondition, int numUnits, ref GearItem? existingGearItem, ref bool __runOriginal)
            {

                // if the item is ruined we (do nothing)
                if (normalizedCondition == 0)
                {
                    return;
                }

                // if we are not controlling this items stackablity (do nothing)
                if (UseDefaultStacking(gearToAdd))
                {
                    return;
                }

                // get the closest match stackable from player inventory
                GearItem targetStack = StackableItem.GetClosestMatchStackable(GameManager.GetInventoryComponent().m_Items, gearToAdd, normalizedCondition);

                // if we can't merge this item/stack (do nothing)
                if (!CanBeMerged(targetStack, gearToAdd))
                {
                    return;
                }

				//patch for lit matches breaking the stacks
				if (gearToAdd.IsLitMatch() || targetStack.IsLitMatch())
				{
					__runOriginal = false;
					existingGearItem = null;
				}


                // if the item is stackable perform the merge changed
                if (targetStack.m_StackableItem != null)
                {
                    // perform merge override logic
                    MergeIntoStack(normalizedCondition, numUnits, targetStack, gearToAdd);
                    // set the existingGearItem to our targetStack
                    existingGearItem = targetStack;
                    return;
                }

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
                    if (PostfixStack != null)
                    {
                        // correct the stack(s) conditions
                        PostfixStack.CurrentHP = PostfixCondition * PostfixStack.m_GearItemData.m_MaxHP;
                        // reset the items constraints
                        PostfixStack.m_StackableItem.m_StackConditionDifferenceConstraint = PostfixConstraint;
                    }
                    if (PostfixGearToAdd != null)
                    {
                        // correct the stack(s) conditions
                        PostfixGearToAdd.CurrentHP = PostfixCondition * PostfixGearToAdd.m_GearItemData.m_MaxHP;
                        // reset the items constraints
                        PostfixGearToAdd.m_StackableItem.m_StackConditionDifferenceConstraint = PostfixConstraint;
                    }
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

            //Implementation.Log("Merging " + gearToAdd.name + "(qty:" + numUnits + ") (cond:"+ normalizedCondition + ") into " + targetStack.name+" (cond:"+ targetStack.GetNormalizedCondition() + ")");

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