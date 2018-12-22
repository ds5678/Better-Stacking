using Harmony;
using UnityEngine;

namespace BetterStacking
{
    [HarmonyPatch(typeof(FlareItem), "Ignite", new System.Type[] { typeof(string) })]
    internal class FlareItem_Ignite
    {
        private static void Postfix(FlareItem __instance)
        {
            GearItem gearItem = __instance.GetComponent<GearItem>();
            if (gearItem == null || gearItem.m_StackableItem == null)
            {
                return;
            }

            Implementation.SplitStack(gearItem);
            Object.Destroy(gearItem.m_StackableItem);
        }
    }

    [HarmonyPatch(typeof(GearItem), "DegradeOnUse")]
    internal class GearItem_DegradeOnUse
    {
        private static void Postfix(GearItem __instance)
        {
            Implementation.AddToExistingStack(__instance);
        }

        private static void Prefix(GearItem __instance)
        {
            Implementation.SplitStack(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "InstantiateItemInPlayerInventory", new System.Type[] { typeof(GearItem), typeof(int) })]
    internal class InstantiateItemInPlayerInventory
    {
        private static void Postfix(ref GearItem __result)
        {
            __result = null;
        }
    }

    [HarmonyPatch(typeof(Lock), "OnForceLockComplete")]
    internal class Lock_OnForceLockComplete
    {
        private static void Postfix(Lock __instance)
        {
            GearItem m_GearUsedToForceLock = Implementation.GetFieldValue<GearItem>(__instance, "m_GearUsedToForceLock");
            Implementation.AddToExistingStack(m_GearUsedToForceLock);
        }

        private static void Prefix(Lock __instance)
        {
            GearItem m_GearUsedToForceLock = Implementation.GetFieldValue<GearItem>(__instance, "m_GearUsedToForceLock");
            Implementation.SplitStack(m_GearUsedToForceLock);
        }
    }

    [HarmonyPatch(typeof(Panel_Crafting), "DegradeToolUsedForCrafting")]
    internal class Panel_Crafting_DegradeToolUsedForCrafting
    {
        private static void Postfix(Panel_Crafting __instance)
        {
            GearItem gearItem = __instance.GetSelectedTool()?.GetComponent<GearItem>();
            Implementation.AddToExistingStack(gearItem);
        }

        private static void Prefix(Panel_Crafting __instance)
        {
            GearItem gearItem = __instance.GetSelectedTool()?.GetComponent<GearItem>();
            Implementation.SplitStack(gearItem);
        }
    }

    [HarmonyPatch(typeof(Panel_IceFishingHoleClear), "OnBreakIceComplete")]
    internal class Panel_IceFishingHoleClear_OnBreakIceComplete
    {
        private static void Postfix(Panel_IceFishingHoleClear __instance)
        {
            GearItem gearItem = Implementation.GetFieldValue<GearItem>(__instance, "m_ToolUsed");
            Implementation.AddToExistingStack(gearItem);
        }

        private static void Prefix(Panel_IceFishingHoleClear __instance)
        {
            GearItem gearItem = Implementation.GetFieldValue<GearItem>(__instance, "m_ToolUsed");
            Implementation.SplitStack(gearItem);
        }
    }

    [HarmonyPatch(typeof(Panel_Inventory_Examine), "RepairSuccessful")]
    internal class Panel_Inventory_Examine_RepairSuccessful
    {
        private static void Postfix(Panel_Inventory_Examine __instance)
        {
            Implementation.AddToExistingStack(__instance.m_GearItem);
        }

        private static void Prefix(Panel_Inventory_Examine __instance)
        {
            Implementation.SplitStack(__instance.m_GearItem);
        }
    }

    [HarmonyPatch(typeof(Panel_Inventory_Examine), "SharpenSuccessful")]
    internal class Panel_Inventory_Examine_SharpenSuccessful
    {
        private static void Postfix(Panel_Inventory_Examine __instance)
        {
            Implementation.AddToExistingStack(__instance.m_GearItem);
        }

        private static void Prefix(Panel_Inventory_Examine __instance)
        {
            Implementation.SplitStack(__instance.m_GearItem);
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "AddToExistingStackable", new System.Type[] { typeof(string), typeof(float), typeof(int), typeof(GearItem) })]
    internal class PlayerManager_AddToExistingStackable
    {
        private static bool Prefix(PlayerManager __instance, ref GearItem __result, string itemName, float normalizedCondition, int numUnits, GearItem gearToAdd)
        {
            if (normalizedCondition == 0)
            {
                __result = null;
                return false;
            }

            if (Implementation.UseDefaultStacking(gearToAdd))
            {
                return true;
            }

            GearItem targetStack = GameManager.GetInventoryComponent().GetClosestMatchStackable(itemName, normalizedCondition);
            if (!Implementation.CanBeMerged(targetStack, gearToAdd))
            {
                __result = null;
                return false;
            }

            Implementation.MergeIntoStack(normalizedCondition, numUnits, targetStack);
            __result = targetStack;
            return false;
        }
    }
}