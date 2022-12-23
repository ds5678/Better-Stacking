using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Reflection;
using MelonLoader;
namespace BetterStacking
{

    internal static class Patches
    {

        internal static bool postfix_track = false;
        internal static float postfix_condition = 0;
        internal static GearItem postfix_stack = new GearItem();
        internal static GearItem postfix_geartoadd = new GearItem();
        internal static float postfix_constraint = 0;

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
                MelonLogger.Msg("PlayerManager.TryAddToExistingStackable not found for patch.");
                return null;
            }
            internal static bool Prefix(ref GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem)
            {

                existingGearItem = new GearItem();

                if (normalizedCondition == 0)
                {
                    return false;
                }

                if (Implementation.UseDefaultStacking(gearToAdd))
                {
                    return true;
                }

                GearItem targetStack = GameManager.GetInventoryComponent().GetClosestMatchStackable(gearToAdd.GearItemData, normalizedCondition);

                if (!Implementation.CanBeMerged(targetStack, gearToAdd))
                {
                    return false;
                }


                if (targetStack.m_StackableItem != null)
                {
                    Implementation.MergeIntoStack(normalizedCondition, numUnits, targetStack, gearToAdd);
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
                if (postfix_track)
                {
                    // correct the stack(s) conditions
                    postfix_stack.CurrentHP = postfix_condition * postfix_stack.m_GearItemData.m_MaxHP;
                    postfix_geartoadd.CurrentHP = postfix_condition * postfix_geartoadd.m_GearItemData.m_MaxHP;

                    // reset the items constraints
                    postfix_stack.m_StackableItem.m_StackConditionDifferenceConstraint = postfix_constraint;
                    postfix_geartoadd.m_StackableItem.m_StackConditionDifferenceConstraint = postfix_constraint;

                }

                // reset the static values to avoid any conflicts
                postfix_track = false;
                postfix_condition = 0;
                postfix_stack = new GearItem();
                postfix_geartoadd = new GearItem();
                postfix_constraint = 0;

            }
        }

    }
}