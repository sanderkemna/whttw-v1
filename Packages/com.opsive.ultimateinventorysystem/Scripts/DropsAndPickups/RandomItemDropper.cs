/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.DropsAndPickups
{
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using UnityEngine;

    /// <summary>
    /// Random Item dropper. Use the Inventory as a probability table.
    /// </summary>
    public class RandomItemDropper : ItemDropper
    {
        [Tooltip("The minimum amount of item that can be dropped.")]
        [SerializeField] protected int m_MinAmount = 1;
        [Tooltip("The maximum amount of item that can be dropped.")]
        [SerializeField] protected int m_MaxAmount = 2;
        [Tooltip("An animation curve to specify the probability of the amount dropped, 0->minAmount, 1->maxAmount.")]
        [SerializeField] protected AnimationCurve m_AmountProbabilityDistribution;

        protected ItemInfoProbabilityTable m_ItemAmountProbabilityTable;

        /// <summary>
        /// Initialize the probability table.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (m_Inventory == null) { return; }

            var itemCollection = m_Inventory.GetItemCollection(m_ItemCollectionID);
            if (itemCollection == null) { return; }

            m_ItemAmountProbabilityTable = new ItemInfoProbabilityTable(
                itemCollection.GetAllItemStacks().ToListSlice());
        }

        /// <summary>
        /// Get the items to drop.
        /// </summary>
        /// <returns>returns a list of items to drop.</returns>
        protected override ListSlice<ItemInfo> GetItemsToDropInternal()
        {
            var randomItemAmounts =
                m_ItemAmountProbabilityTable.GetRandomItemInfos(m_MinAmount, m_MaxAmount, m_AmountProbabilityDistribution);
            return randomItemAmounts.ToListSlice();
        }
    }
}