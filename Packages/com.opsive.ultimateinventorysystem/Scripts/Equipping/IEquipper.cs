/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Equipping
{
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;

    /// <summary>
    /// Equipper interface. 
    /// </summary>
    public interface IEquipper
    {
        /// <summary>
        /// Equip an item.
        /// </summary>
        /// <param name="item">The item to equip.</param>
        /// <returns>True if the item was equipped.</returns>
        bool Equip(Item item);

        /// <summary>
        /// Equip an item to a specific slot.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The slot index.</param>
        /// <returns>True if the item was equipped.</returns>
        bool Equip(Item item, int index);

        /// <summary>
        /// Check if an item is equipped.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the item is equipped.</returns>
        bool IsEquipped(Item item);

        /// <summary>
        /// Check if an item slot is occupied.
        /// </summary>
        /// <param name="index">The slot index to check.</param>
        /// <returns>True if the item slot is occupied.</returns>
        bool IsEquipped(int index);

        /// <summary>
        /// Get the equipped item at the slot specified.
        /// </summary>
        /// <param name="index">The slot index.</param>
        /// <returns>The item equipped.</returns>
        Item GetEquippedItem(int index);

        /// <summary>
        /// Get the equipment stats.
        /// </summary>
        /// <param name="attributeName">The equipment stat name.</param>
        /// <returns>The equipment stat.</returns>
        int GetEquipmentStatInt(string attributeName);

        /// <summary>
        /// Get a preview stat total by simulating adding a new item.
        /// </summary>
        /// <param name="attributeName">The attribute.</param>
        /// <param name="itemPreview">The item to preview.</param>
        /// <returns>The total attribute value.</returns>
        int GetEquipmentStatPreviewAdd(string attributeName, Item itemPreview);

        /// <summary>
        /// Preview the attribute stat by simulating removing an item.
        /// </summary>
        /// <param name="attributeName">The attribute.</param>
        /// <param name="itemPreview">The item to preview remove.</param>
        /// <returns>The total attribute value.</returns>
        int GetEquipmentStatPreviewRemove(string attributeName, Item itemPreview);

        /// <summary>
        /// UnEquip an item.
        /// </summary>
        /// <param name="item">The item to unequip.</param>
        void UnEquip(Item item);

        /// <summary>
        /// UnEquip the item at the slot.
        /// </summary>
        /// <param name="index">The slot.</param>
        void UnEquip(int index);

        /// <summary>
        /// Get the ItemObject assigned to the slot index.
        /// </summary>
        /// <param name="index">The slot index where the item object is assigned.</param>
        /// <returns>The itemObject at the slot index provided</returns>
        ItemObject GetEquippedItemObject(int index);
    }
}