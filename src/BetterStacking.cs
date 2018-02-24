using Harmony;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BetterStacking
{
    internal class BetterStacking
    {
        private static readonly string[] STACK_MERGE = {
            "GEAR_Accelerant",
            "GEAR_BirchSaplingDried",
            "GEAR_BearHideDried",
            "GEAR_GutDried",
            "GEAR_LeatherDried",
            "GEAR_LeatherHideDried",
            "GEAR_MapleSaplingDried",
            "GEAR_PackMatches",
            "GEAR_RabbitPeltDried",
            "GEAR_WolfPeltDried",
            "GEAR_WoodMatches",
        };

        public static void OnLoad()
        {
            Debug.Log("[Better-Stacking]: Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            MakeStackable("GEAR_Accelerant");
            MakeStackable("GEAR_CanOpener");
            MakeStackable("GEAR_FlareA");
            MakeStackable("GEAR_Hacksaw");
            MakeStackable("GEAR_Hammer");
            MakeStackable("GEAR_Hatchet");
            MakeStackable("GEAR_HatchetImprovised");
            MakeStackable("GEAR_HighQualityTools");
            MakeStackable("GEAR_Knife");
            MakeStackable("GEAR_KnifeImprovised");
            MakeStackable("GEAR_Prybar");
            MakeStackable("GEAR_SewingKit");
            MakeStackable("GEAR_SimpleTools");
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
                Debug.LogException(e);
                HUDMessage.AddMessage("[Better-Stacking]: Failed to merge into stack of " + gearItem.name + ".");
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

        internal static void MergeIntoStack(float normalizedCondition, int numUnits, GearItem targetStack)
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

            try
            {
                SplitStackWithException(gearItem);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                HUDMessage.AddMessage("[Better-Stacking]: Failed to split stack of " + gearItem.name + ".");
            }
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
                    inventory.RemoveGear(gearItem.gameObject);
                    return;
                }

                if (!useDefaultStacking && CanBeMerged(eachTargetItem, gearItem))
                {
                    MergeIntoStack(gearItem.GetNormalizedCondition(), 1, eachTargetItem);
                    inventory.RemoveGear(gearItem.gameObject);
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

        private static void SplitStackWithException(GearItem gearItem)
        {
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
    }
}