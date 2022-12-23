using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Reflection;
namespace BetterStacking
{

    internal static class Patches
    {

        internal static bool PostFixTrack { get; set; } = false ;
        internal static float PostfixCondition { get; set; }  = 0;
        internal static GearItem? PostfixStack { get; set; }  = null;
        internal static GearItem? PostfixGearToAdd { get; set; }  = null;
        internal static float PostfixConstraint { get; set; }  = 0;

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
                MelonLogger.Warning("PlayerManager.TryAddToExistingStackable not found for patch.");
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

                if (!CanBeMerged(targetStack, gearToAdd))
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

        internal static bool CanBeMerged(GearItem target, GearItem item)
        {
            return target != null && item != null && CanBeMerged(target.m_FlareItem, item.m_FlareItem);
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

        internal static void ResetPostfixParams()
        {
            PostFixTrack = false;
            PostfixCondition = 0;
            PostfixStack = null;
            PostfixGearToAdd = null;
            PostfixConstraint = 0;
        }

    }
}