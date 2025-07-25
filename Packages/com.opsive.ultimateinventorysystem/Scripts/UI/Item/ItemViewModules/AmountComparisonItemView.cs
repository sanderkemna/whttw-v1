/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Item.ItemViewModules
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.UI.CompoundElements;
    using Opsive.UltimateInventorySystem.UI.Views;
    using UnityEngine;
    using Text = Opsive.Shared.UI.Text;

    /// <summary>
    /// An Item View component that compares the item amount with an amount from an inventory.
    /// </summary>
    public class AmountComparisonItemView : ItemViewModule, IInventoryDependent
    {
        [Tooltip("The amount text.")]
        [SerializeField] protected Text m_AmountText;
        [Tooltip("The text color if the inventory amount is bigger or equal to the item amount.")]
        [SerializeField] protected Color m_PositiveColor = Color.green;
        [Tooltip("The text color if the inventory amount is lower then the item amount.")]
        [SerializeField] protected Color m_NegativeColor = Color.red;
        [Tooltip("The itemCollections to check into. If none is specified the Inventory is used")]
        [SerializeField] protected string[] m_ItemCollectionNames;

        public Inventory Inventory { get; set; }

        /// <summary>
        /// Set the value.
        /// </summary>
        /// <param name="info">The item info.</param>
        public override void SetValue(ItemInfo info)
        {
            if (Inventory == null) { Inventory = info.Inventory as Inventory; }
            if (Inventory == null) {
                Debug.LogWarning("Inventory is missing from component.");
                Clear();
                return;
            }

            var inventoryAmount = 0;
            if (m_ItemCollectionNames == null || m_ItemCollectionNames.Length == 0) {
                inventoryAmount = Inventory.GetItemAmount(info.Item.ItemDefinition, false, false);   
            } else {
                for (int i = 0; i < m_ItemCollectionNames.Length; i++) {
                    var itemCollection = Inventory.GetItemCollection(m_ItemCollectionNames[i]);
                    if(itemCollection == null) { continue; }
                    
                    inventoryAmount += itemCollection.GetItemAmount(info.Item.ItemDefinition, false, false);
                }
            }

            m_AmountText.text = $"{inventoryAmount}/{info.Amount}";

            m_AmountText.color = inventoryAmount - info.Amount >= 0 ? m_PositiveColor : m_NegativeColor;
        }

        /// <summary>
        /// Clear the value.
        /// </summary>
        public override void Clear()
        {
            m_AmountText.text = "";
        }
    }
}