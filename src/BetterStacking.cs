using Harmony;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BetterStacking
{
    internal class BetterStacking
    {
        private static readonly string[] STACK_MERGE = {
            "GEAR_WoodMatches",
            "GEAR_Accelerant",
            "GEAR_PackMatches",
            "GEAR_WolfPeltDried",
            "GEAR_RabbitPeltDried",
            "GEAR_LeatherHideDried",
            "GEAR_LeatherDried",
            "GEAR_GutDried",
            "GEAR_BearHideDried",
            "GEAR_BirchSaplingDried",
            "GEAR_MapleSaplingDried"
        };

        public static void OnLoad()
        {
            MakeStackable("GEAR_Accelerant");
            MakeStackable("GEAR_FlareA");
            MakeStackable("GEAR_SewingKit");
            MakeStackable("GEAR_SimpleTools");
            MakeStackable("GEAR_HighQualityTools");
            MakeStackable("GEAR_Prybar");
            MakeStackable("GEAR_Hammer");
            MakeStackable("GEAR_Hacksaw");
            MakeStackable("GEAR_CanOpener");
        }

        internal static void AddToStack(GearItem gearItem)
        {
            if (gearItem == null || gearItem.m_StackableItem == null)
            {
                return;
            }

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
                    inventory.RemoveGear(gearItem.gameObject);
                    return;
                }

                if (!useDefaultStacking && CanBeMerged(eachTargetItem, gearItem))
                {
                    MergeStack(gearItem.GetNormalizedCondition(), 1, eachTargetItem);
                    inventory.RemoveGear(gearItem.gameObject);
                    return;
                }
            }
        }

        internal static bool CanBeMerged(GearItem target, GearItem item)
        {
            if (target == null || item == null)
            {
                return false;
            }

            return CanBeMerged(target.m_FlareItem, item.m_FlareItem);
        }

        internal static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo fieldInfo = AccessTools.Field(target.GetType(), fieldName);
            if (fieldInfo != null)
            {
                return (T)fieldInfo.GetValue(target);
            }

            return default(T);
        }

        internal static void MergeStack(float normalizedCondition, int numUnits, GearItem targetStack)
        {
            int targetCount = numUnits + targetStack.m_StackableItem.m_Units;
            float targetCondition = (numUnits * normalizedCondition + targetStack.m_StackableItem.m_Units * targetStack.GetNormalizedCondition()) / targetCount;

            targetStack.m_StackableItem.m_Units = targetCount;
            targetStack.m_CurrentHP = targetCondition * targetStack.m_MaxHP;
        }

        internal static void SplitStack(GearItem gearItem)
        {
            if (gearItem == null || gearItem.m_StackableItem == null)
            {
                return;
            }

            int count = gearItem.m_StackableItem.m_Units;
            if (count <= 1)
            {
                return;
            }

            gearItem.m_StackableItem.m_Units = 1;

            GearItem clone = Utils.InstantiateGearFromPrefabName(gearItem.name);
            clone.m_StackableItem.m_Units = count - 1;
            clone.m_CurrentHP = gearItem.m_CurrentHP;

            GameManager.GetInventoryComponent().AddGear(clone.gameObject);
        }

        internal static bool UseDefaultStacking(GearItem gearItem)
        {
            if (gearItem == null)
            {
                return true;
            }

            if (STACK_MERGE.Contains(gearItem.name))
            {
                return false;
            }

            return true;
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

            Debug.Log("target.IsBurnedOut() = " + target.IsBurnedOut() + "; item.IsBurnedOut() = " + item.IsBurnedOut());
            if (target.IsBurnedOut() != item.IsBurnedOut())
            {
                return false;
            }

            return true;
        }

        private static void MakeStackable(string prefabName)
        {
            GameObject gameObject = Resources.Load(prefabName) as GameObject;

            StackableItem stackableItem = gameObject.GetComponent<StackableItem>();
            if (stackableItem == null)
            {
                stackableItem = gameObject.AddComponent<StackableItem>();
                stackableItem.m_ShareStackWithGear = new StackableItem[0];
                stackableItem.m_StackSpriteName = string.Empty;
                stackableItem.m_Units = 1;
                stackableItem.m_UnitsPerItem = 1;
            }
        }
    }
}