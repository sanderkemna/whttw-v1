/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.ItemActions
{
    using Opsive.UltimateInventorySystem.UI.Item;

    /// <summary>
    /// An interface for an item action panel.
    /// </summary>
    public interface IActionWithSlotContainer
    {
        /// <summary>
        /// Set the Item View Slots Container.
        /// </summary>
        /// <param name="itemViewSlotsContainer">The item View Slots Container.</param>
        /// <param name="itemViewSlotIndex">The index of the selected Item View Slot.</param>
        void SetViewSlotsContainer(ItemViewSlotsContainerBase itemViewSlotsContainer, int itemViewSlotIndex);
    }
}