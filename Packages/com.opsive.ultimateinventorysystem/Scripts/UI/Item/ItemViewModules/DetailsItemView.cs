/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Item.ItemViewModules
{
    using Opsive.Shared.UI;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using UnityEngine;

    /// <summary>
    /// A Item View mostly used for debugging items, it shows the same information showed in the database.
    /// Mutable, Unique, ID, ItemDefinition ID, ItemCategoryID, etc...
    /// </summary>
    public class DetailsItemView : ItemViewModule
    {
        [Tooltip("The text component where the details will be written")]
        [SerializeField] protected Text m_Text;
        
        /// <summary>
        /// Set the item info.
        /// </summary>
        /// <param name="info">The item info.</param>
        public override void SetValue(ItemInfo info)
        {
            if (info.Item == null || info.Item.IsInitialized == false) {
                Clear();
                return;
            }

            var details = 
                $"Item Category: {info.Item.Category} ({info.Item.Category.ID})\n"+
                $"Mutable: {info.Item.IsMutable}\n"+
                $"Unique: {info.Item.IsUnique}\n"+
                $"Item Definition: {info.Item.ItemDefinition} ({info.Item.ItemDefinition.ID})\n" +
                $"Default Item: {info.Item.ItemDefinition.DefaultItem}\n"+
                $"This Item: {info.Item}\n";

            m_Text.text = details;
        }

        /// <summary>
        /// Clear the component.
        /// </summary>
        public override void Clear()
        {
            m_Text.text = "";
        }
    }
}