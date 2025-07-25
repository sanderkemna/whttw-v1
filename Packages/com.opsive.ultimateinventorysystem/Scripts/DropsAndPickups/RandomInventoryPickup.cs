/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.DropsAndPickups
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Interactions;
    using UnityEngine;

    /// <summary>
    /// Random Inventory pickup. Use the Inventory as a probability table.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Interactable))]
    public class RandomInventoryPickup : InventoryPickup
    {
        [Tooltip("The minimum amount of item that can be picked up.")]
        [SerializeField] protected int m_MinAmount = 1;
        [Tooltip("The maximum amount of item that can be picked up.")]
        [SerializeField] protected int m_MaxAmount = 2;
        [Tooltip("An animation curve to specify the probability of the amount dropped, 0->minAmount, 1->maxAmount.")]
        [SerializeField] protected AnimationCurve m_AmountProbabilityDistribution;

        protected ItemAmountProbabilityTable m_ItemAmountProbabilityTable;
        
        public int MinAmount { get => m_MinAmount; set => m_MinAmount = value; }
        public int MaxAmount { get => m_MaxAmount; set => m_MaxAmount = value; }
        public AnimationCurve AmountProbabilityDistribution { get => m_AmountProbabilityDistribution; set => m_AmountProbabilityDistribution = value; }


        /// <summary>
        /// Initialize the probability table.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            m_ItemAmountProbabilityTable = new ItemAmountProbabilityTable((m_Inventory.MainItemCollection.GetAllItemStacks(), 0));
        }

        /// <summary>
        /// Add a random set of item amounts to the item Collection.
        /// </summary>
        /// <param name="itemCollection">The item collection.</param>
        protected override void AddPickupToCollection(ItemCollection itemCollection)
        {

            if (m_ItemAmountProbabilityTable.Count == 0) {
                NotifyPickupFailed();
                return;
            }

            var randomItemAmounts = m_ItemAmountProbabilityTable.GetRandomItemAmounts(m_MinAmount, m_MaxAmount, m_AmountProbabilityDistribution);
            
            var atLeastOneCanBeAdded = false;
            var atLeastOneCannotBeAdded = false;
            for (int i = 0; i < randomItemAmounts.Count; i++) {
                var itemAmount = randomItemAmounts[i];
                var canAddResult =  itemCollection.CanAddItem((ItemInfo)itemAmount);
                if (canAddResult.HasValue && canAddResult.Value.Amount != 0) {
                    atLeastOneCanBeAdded = true;
                } else {
                    atLeastOneCannotBeAdded = true;
                }
            }

            if (atLeastOneCanBeAdded == false) {
                NotifyPickupFailed();
                return;
            }
            
            Shared.Events.EventHandler.ExecuteEvent(m_Inventory.gameObject, "OnItemPickupStartPickup");
            itemCollection.AddItems((randomItemAmounts, 0));
            
            if (atLeastOneCannotBeAdded) {
                NotifyPartialPickup();
            } else {
                NotifyPickupSuccess();
            }
            
            Shared.Events.EventHandler.ExecuteEvent(m_Inventory.gameObject, "OnItemPickupStopPickup");

        }
    }
}