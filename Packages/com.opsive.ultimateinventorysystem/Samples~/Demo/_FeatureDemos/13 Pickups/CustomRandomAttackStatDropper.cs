/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Demo.FeatureDemos
{
    using System.Collections.Generic;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.DropsAndPickups;
    using UnityEngine;

    /// <summary>
    /// Custom Random Dropper which give random and attack stats.
    /// </summary>
    public class CustomRandomAttackStatDropper : RandomItemDropper
    {
        [SerializeField] protected AnimationCurve m_RandomAttackMultiplierDistribution;

        protected const string AttackAttributeName = "Attack";

        /// <summary>
        /// Drop a random set of item amounts.
        /// </summary>
        public override void Drop()
        {
            //Here we get a random list of items from the probability table.
            var itemsToDrop = GetItemsToDrop();

            AssignRandomStats(itemsToDrop);

            DropItemsInternal(itemsToDrop);
        }

        /// <summary>
        /// This is the function in which we will assign our custom random stats.
        /// </summary>
        /// <param name="itemAmounts">The item list to which we'll assign the random stats.</param>
        protected virtual void AssignRandomStats(ListSlice<ItemInfo> itemAmounts)
        {
            for (int i = 0; i < itemAmounts.Count; i++) {
                var item = itemAmounts[i].Item;
                if(item == null){ continue; }
            
                // If the item does not have attack then ignore it.
                if (item.HasAttribute(AttackAttributeName) == false) {
                    continue;
                }

                //Get a random attack multiplier.
                var randomAttackMultiplier = m_RandomAttackMultiplierDistribution.Evaluate(Random.value);
                
                //Get the default attack value from the item definition default item attack attribute
                var baseAttackAttribute = item.ItemDefinition.DefaultItem.GetAttribute<Attribute<int>>(AttackAttributeName);

                var randomAttackValue = Mathf.RoundToInt(baseAttackAttribute.GetValue() * randomAttackMultiplier);
                
                //Assign the new attack attribute.
                var attackAttribute = item.GetAttribute<Attribute<int>>(AttackAttributeName);
                attackAttribute.SetOverrideValue(randomAttackValue);
            }
        }
    }
}