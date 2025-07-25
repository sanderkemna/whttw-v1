namespace Opsive.UltimateInventorySystem.ItemActions
{
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;

    /// <summary>
    /// Item action used to Move an item from one Inventory to another use the Inventory Identifier ID. It can be used to equip items too.
    /// </summary>
    [System.Serializable]
    public class MoveToInventoryItemAction : ItemAction
    {
        [Tooltip("The first original Inventory, specified by and Inventory Identifier ID.")]
        [SerializeField] protected uint m_TargetInventoryID = 1;
        [Tooltip("The second item collection.")]
        [SerializeField] protected ItemCollectionID m_TargetCollectionID = new ItemCollectionID(null, ItemCollectionPurpose.Equipped);

        protected ItemCollection m_TargetItemCollection;
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public MoveToInventoryItemAction()
        { }

        /// <summary>
        /// Can the item action be invoked.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        /// <returns>True if it can be invoked.</returns>
        protected override bool CanInvokeInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var item = itemInfo.Item;
            var inventory = itemInfo.Inventory;

            if (inventory == null) {
                return false;
            }
            
            var targetInventory = InventorySystemManager.GetInventoryIdentifier(m_TargetInventoryID)?.Inventory;
            if (targetInventory == null) {
                Debug.LogWarning($"Target Inventory not found with IDs {m_TargetInventoryID}");
                return false;
            }

            var targetItemCollection = targetInventory.GetItemCollection(m_TargetCollectionID);

            if (targetItemCollection == null) {
                Debug.LogWarning($"Target ItemCollection not found in Inventory '{targetInventory}' with ItemCollection ID {m_TargetCollectionID}");
                return false;
            }

            m_TargetItemCollection = targetItemCollection;

            return true;
        }

        /// <summary>
        /// Move an item from one collection to another.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            var item = itemInfo.Item;
            var inventory = itemInfo.Inventory;

            //This action used to give the item one way.
            //The action now removes the item, before it adds it to the other collection to allow restrictions to work properly

            var originalCollection = itemInfo.ItemCollection ?? inventory.MainItemCollection;
            var destinationCollection = m_TargetItemCollection;
            
            var originalItem = originalCollection.RemoveItem(itemInfo);
            var movedItemInfo = ItemInfo.None;
            
            if (destinationCollection is ItemSlotCollection itemSlotCollection) {
                var slotIndex = itemSlotCollection.GetTargetSlotIndex(item);
                if (slotIndex != -1) {
                    var previousItemInSlot = itemSlotCollection.GetItemInfoAtSlot(slotIndex);

                    if (previousItemInSlot.Item != null) {
                        //If the previous item is stackable don't remove it.
                        if (previousItemInSlot.Item.StackableEquivalentTo(originalItem.Item)) {
                            previousItemInSlot = ItemInfo.None;
                        } else {
                            previousItemInSlot = itemSlotCollection.RemoveItem(slotIndex); 
                        }
                    }
            
                    movedItemInfo = itemSlotCollection.AddItem(originalItem, slotIndex);

                    if (previousItemInSlot.Item != null) {
                        originalCollection.AddItem(previousItemInSlot);
                    }
                }
            } else {
                movedItemInfo = destinationCollection.AddItem(originalItem);
            }

            //Not all the item was added, return the items to the default collection.
            if (movedItemInfo.Amount != originalItem.Amount) {
                var amountToReturn = originalItem.Amount - movedItemInfo.Amount;
                originalCollection.AddItem((ItemInfo) (amountToReturn, originalItem));
            }
            
        }
    }
}