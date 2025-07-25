/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.ItemActions
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.UI.Panels.Hotbar;
    using UnityEngine;

    /// <summary>
    /// Unassign an Item from the hotbar without removing it from the Inventory.
    /// </summary>
    [System.Serializable]
    public class UnAssignHotbarItemAction : ItemViewSlotsContainerItemAction
    {
        /// <summary>
        /// Internal Invoke, is used by the default InvokeAction.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            if (m_ItemViewSlotsContainer is ItemHotbar itemHotbar) {
                itemHotbar.UnAssignSlot(m_ItemViewSlotIndex);
            } else {
                Debug.LogWarning("The Item is not in a hotbar, so it cannot be unassigned from the slot");
            }
        }
    }
}