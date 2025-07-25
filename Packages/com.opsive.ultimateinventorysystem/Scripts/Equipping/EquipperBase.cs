/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Equipping
{
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Utility;
    using UnityEngine;
    using EventHandler = Opsive.Shared.Events.EventHandler;

    /// <summary>
    /// The Equipper component is used to equip items by converting them to ItemObjects.
    /// </summary>
    public class EquipperBase : MonoBehaviour, IEquipper
    {
        [Tooltip("The equippers inventory.")]
        [SerializeField] protected Inventory m_Inventory;
        [Tooltip("The equippers itemCollection within the inventory.")]
        [SerializeField]
        protected ItemCollectionID m_EquipmentItemCollectionID = new ItemCollectionID("Equipped", ItemCollectionPurpose.Equipped);

        protected ItemSlotCollection m_EquipmentItemCollection;
        protected bool m_Initialized;

        /// <summary>
        /// Initialize in awake.
        /// </summary>
        protected virtual void Awake()
        {
            PreInitialize(false);
        }

        /// <summary>
        /// PreInitialize.
        /// </summary>
        /// <param name="force">force initialize?</param>
        protected virtual void PreInitialize(bool force)
        {
            if(m_Initialized && !force){ return; }

            m_Initialized = true;
        }

        /// <summary>
        /// Initialize the Equiper.
        /// </summary>
        protected virtual void Start()
        {
            if (m_Inventory == null) { m_Inventory = GetComponent<Inventory>(); }
            m_EquipmentItemCollection = m_Inventory.GetItemCollection(m_EquipmentItemCollectionID) as ItemSlotCollection;

            if (m_EquipmentItemCollection == null) {
                Debug.LogWarning("Your inventory does not have an equipment Item Collection.");
                return;
            }

            EventHandler.RegisterEvent<ItemInfo, ItemStack>(m_Inventory, EventNames.c_Inventory_OnAdd_ItemInfo_ItemStack, OnAddedItemToInventory);
            EventHandler.RegisterEvent<ItemInfo>(m_Inventory, EventNames.c_Inventory_OnRemove_ItemInfo, OnRemovedItemFromInventory);

            for (int i = 0; i < m_EquipmentItemCollection.SlotCount; i++) {
                var item = m_EquipmentItemCollection.GetItemAtSlot(i);
                if(item == null){ continue; }

                Equip(item, i);
            }
        }

        /// <summary>
        /// Make sure to unregister the listener on Destroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            EventHandler.UnregisterEvent<ItemInfo, ItemStack>(m_Inventory, EventNames.c_Inventory_OnAdd_ItemInfo_ItemStack, OnAddedItemToInventory);
            EventHandler.UnregisterEvent<ItemInfo>(m_Inventory, EventNames.c_Inventory_OnRemove_ItemInfo, OnRemovedItemFromInventory);
        }

        /// <summary>
        /// Equip item that was added to the equipment collection.
        /// </summary>
        /// <param name="originItemInfo">The origin Item info.</param>
        /// /// <param name="addedItemStack">The added item stack.</param>
        protected virtual void OnAddedItemToInventory(ItemInfo originItemInfo, ItemStack addedItemStack)
        {
            if (addedItemStack == null) { return; }
            if (addedItemStack.ItemCollection == m_EquipmentItemCollection) {
                var index = m_EquipmentItemCollection.GetItemSlotIndex(addedItemStack);
                Equip(addedItemStack.Item, index);
            }
        }

        /// <summary>
        /// Unequip an item that was removed from the equipment collection.
        /// </summary>
        /// <param name="removedItemInfo">The removed Item info.</param>
        protected virtual void OnRemovedItemFromInventory(ItemInfo removedItemInfo)
        {
            if (removedItemInfo.ItemCollection == m_EquipmentItemCollection) {
                UnEquip(removedItemInfo.Item, m_EquipmentItemCollection.GetItemSlotIndex(removedItemInfo.ItemStack));
            }
        }

        /// <summary>
        /// Equip an item.
        /// </summary>
        /// <param name="item">The item to equip.</param>
        /// <returns>True if the item was equipped.</returns>
        public bool Equip(Item item)
        {
            var slotIndex = m_EquipmentItemCollection.GetItemSlotIndex(item);
            return Equip(item, slotIndex);
        }

        /// <summary>
        /// Equip an item to a specific slot.
        /// </summary>
        /// <param name="item">The item to equip.</param>
        /// <param name="index">The slot to equip to.</param>
        /// <returns>True if equipped successfully.</returns>
        public virtual bool Equip(Item item, int index)
        {
            EventHandler.ExecuteEvent<Item,int>(this, EventNames.c_Equipper_OnEquipped_Item_Index, item, index);
            EventHandler.ExecuteEvent(this, EventNames.c_Equipper_OnChange);

            return true;
        }

        /// <summary>
        /// Check if the item is equipped.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if equipped.</returns>
        public bool IsEquipped(Item item)
        {
            if (item == null) { return false; }

            return m_EquipmentItemCollection.HasItem(item);
        }

        /// <summary>
        /// Check if the slot has an item equipped already.
        /// </summary>
        /// <param name="index">The slot.</param>
        /// <returns>True if an item is equipped in that slot.</returns>
        public bool IsEquipped(int index)
        {
            return m_EquipmentItemCollection.GetItemAtSlot(index) != null;
        }

        /// <summary>
        /// Get the item Object by index.
        /// </summary>
        /// <param name="slotIndex">The slot index.</param>
        /// <returns>The item Object at the index specified.</returns>
        public virtual ItemObject GetEquippedItemObject(int slotIndex)
        {
            Debug.LogWarning("Equipper Base does not spawn ItemObjects, please overrride");
            return null;
        }

        /// <summary>
        /// Get the item equipped in the slot provided.
        /// </summary>
        /// <param name="index">The slot.</param>
        /// <returns>The item equipped in that slot.</returns>
        public virtual Item GetEquippedItem(int index)
        {
            return m_EquipmentItemCollection.GetItemAtSlot(index);
        }

        /// <summary>
        /// Get the item equipped in the slot provided.
        /// </summary>
        /// <param name="slotName">The slot name.</param>
        /// <returns>The item equipped in that slot.</returns>
        public virtual Item GetEquippedItem(string slotName)
        {
            var itemInfo = m_EquipmentItemCollection.GetItemInfoAtSlot(slotName);

            return itemInfo.Item;
        }

        /// <summary>
        /// Get the Equipment stats by retrieving the total value of the attribute.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <returns>The total amount for the attribute.</returns>
        public virtual int GetEquipmentStatInt(string attributeName)
        {
            return (int)GetEquipmentStatFloat(attributeName);
        }

        /// <summary>
        /// Get the Equipment stats by retrieving the total value of the attribute.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <returns>The total amount for the attribute.</returns>
        public virtual float GetEquipmentStatFloat(string attributeName)
        {
            if (m_EquipmentItemCollection == null) {
                return 0;
            }
            return m_EquipmentItemCollection.GetFloatSum(attributeName);
        }

        /// <summary>
        /// Get a preview stat total by simulating adding a new item.
        /// </summary>
        /// <param name="attributeName">The attribute.</param>
        /// <param name="itemPreview">The item to preview.</param>
        /// <returns>The total attribute value.</returns>
        public int GetEquipmentStatPreviewAdd(string attributeName, Item itemPreview)
        {
            var targetSlot = m_EquipmentItemCollection.GetTargetSlotIndex(itemPreview);
            
            var stat = 0f;
            for (int i = 0; i < m_EquipmentItemCollection.SlotCount; i++) {

                // Get the item or the one to preview.
                var item = targetSlot == i ? 
                    itemPreview
                    : m_EquipmentItemCollection.GetItemAtSlot(i);
                
                if(item == null){ continue; }

                stat += AttributeUtility.GetFloatSum(attributeName, item);
            }

            return (int)stat;
        }

        /// <summary>
        /// Preview the attribute stat by simulating removing an item.
        /// </summary>
        /// <param name="attributeName">The attribute.</param>
        /// <param name="itemPreview">The item to preview remove.</param>
        /// <returns>The total attribute value.</returns>
        public int GetEquipmentStatPreviewRemove(string attributeName, Item itemPreview)
        {
            var ignoreSlot = m_EquipmentItemCollection.GetItemSlotIndex(itemPreview);
            
            var stat = 0f;
            for (int i = 0; i < m_EquipmentItemCollection.SlotCount; i++) {

                // Get the item or the one to preview.
                var item = ignoreSlot == i ? 
                    null
                    : m_EquipmentItemCollection.GetItemAtSlot(i);
                
                if(item == null){ continue; }

                stat += AttributeUtility.GetFloatSum(attributeName, item);
            }

            return (int)stat;
        }

        /// <summary>
        /// UnEquip an item.
        /// </summary>
        /// <param name="item">The item to unequip.</param>
        public virtual void UnEquip(Item item)
        {
            var index = m_EquipmentItemCollection.GetItemSlotIndex(item);
            UnEquip(item, index);
        }

        /// <summary>
        /// UnEquip the item at the slot.
        /// </summary>
        /// <param name="index">The slot.</param>
        public virtual void UnEquip(int index)
        {
            var item = m_EquipmentItemCollection.GetItemAtSlot(index);
            UnEquip(item, index);
        }

        /// <summary>
        /// UnEquip the item at the slot.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The slot.</param>
        public virtual void UnEquip(Item item, int index)
        {
            EventHandler.ExecuteEvent<Item,int>(this, EventNames.c_Equipper_OnEquipped_Item_Index, item, index);
            EventHandler.ExecuteEvent(this, EventNames.c_Equipper_OnChange);
        }
    }
}