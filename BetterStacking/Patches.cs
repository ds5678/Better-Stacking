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

                //MelonLogger.Msg("PlayerManager.TryAddToExistingStackable | " + gearToAdd.name);
                //MelonLogger.Msg("PlayerManager.TryAddToExistingStackable | " + normalizedCondition);
                //MelonLogger.Msg("PlayerManager.TryAddToExistingStackable | " + numUnits);

                if (normalizedCondition == 0)
                {
                    //MelonLogger.Msg("normalizedCondition == 0 ");
                    existingGearItem = null;
                    return false;
                }

                if (Implementation.UseDefaultStacking(gearToAdd))
                {
                    //MelonLogger.Msg("UseDefaultStacking");
                    existingGearItem = null;
                    return true;
                }

                GearItem targetStack = GameManager.GetInventoryComponent().GetClosestMatchStackable(gearToAdd.GearItemData, normalizedCondition);
                if (!Implementation.CanBeMerged(targetStack, gearToAdd))
                {
                    //MelonLogger.Msg("CanBeMerged NOT | "+ targetStack.name + " | "+gearToAdd.name);
                    existingGearItem = null;
                    return false;
                }

                //MelonLogger.Msg("CanBeMerged | " + targetStack.name + " | " + gearToAdd.name);
                // should only destroy if we have an existing stack in inventory
                if (targetStack.m_StackableItem != null)
                {
                    //MelonLogger.Msg("existing stack | " + targetStack.name + " | " + gearToAdd.name);
                    //MelonLogger.Msg("CanBeMerged item | " + targetStack.m_StackableItem + " | " + gearToAdd.m_StackableItem);
                    //MelonLogger.Msg("CanBeMerged counts | " + targetStack.m_StackableItem.m_Units + " | " + gearToAdd.m_StackableItem.m_Units);
                    Implementation.MergeIntoStack(normalizedCondition, numUnits, targetStack);
                    GameManager.GetInventoryComponent().DestroyGear(gearToAdd.gameObject);
                    existingGearItem = null;
                    return false;
                }
                existingGearItem = null;
                return true;

            }
        }

    }
}