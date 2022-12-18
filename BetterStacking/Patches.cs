using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Reflection;
using MelonLoader;

namespace BetterStacking
{
    internal static class Patches
    {

        [HarmonyPatch]
        internal static class PlayerManager_TryAddToExistingStackable
        {
            static MethodBase? TargetMethod()
            {
                MethodInfo[] methods = typeof(PlayerManager).GetMethods();
                foreach (MethodInfo m in methods)
                {
                    //MelonLogger.Msg(m.Name.ToString() + "|" + m.ReturnType.ToString() + "|" + m.IsGenericMethod.ToString() + "|"+ m.GetParameters().Length);
                    if (m.Name == nameof(PlayerManager.TryAddToExistingStackable) && m.ReturnType == typeof(bool) && !m.IsGenericMethod && m.GetParameters().Length == 4)
                    {
                        return m;
                    }
                }
                MelonLogger.Msg("PlayerManager.TryAddToExistingStackable not found for patch.");
                return null;
            }
            internal static bool Prefix(PlayerManager __instance, GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem)
            {

                if (normalizedCondition == 0)
                {
                    existingGearItem = null;
                    return false;
                }

                if (Implementation.UseDefaultStacking(gearToAdd))
                {
                    existingGearItem = null;
                    return true;
                }

                GearItem targetStack = GameManager.GetInventoryComponent().GetClosestMatchStackable(gearToAdd.GearItemData, normalizedCondition);
                if (!Implementation.CanBeMerged(targetStack, gearToAdd))
                {
                    existingGearItem = null;
                    return false;
                }

                if (targetStack.m_StackableItem != null)
                {
                    Implementation.MergeIntoStack(normalizedCondition, numUnits, targetStack);
                    existingGearItem = null;
                    return false;
                }
                existingGearItem = null;
                return true;

            }
        }

    }
}