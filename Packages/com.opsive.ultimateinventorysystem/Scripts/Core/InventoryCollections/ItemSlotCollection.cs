/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Core.InventoryCollections
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Storage;
    using System;
    using System.Collections.Generic;
    using Opsive.Shared.Utility;
    using UnityEngine;

    /// <summary>
    /// Organizes the items in slots. Each slot can have multiple items.
    /// </summary>
    [Serializable]
    public class ItemSlotCollection : ItemCollection, IDatabaseSwitcher
    {
        [Tooltip("The slots that belong to the collection.")]
        [SerializeField] protected ItemSlotSet m_ItemSlotSet;
        [Tooltip("Should newly added items replace previously added items?")]
        [SerializeField] protected bool m_NewItemPriority;
        [Tooltip("Try to give the replaced Item to where the new item came from, if not the item will simply overflow.")]
        [SerializeField] protected bool m_TryGivePreviousItemToNewItemCollection;

        public ItemSlotSet ItemSlotSet => m_ItemSlotSet;

        protected ItemStack[] m_ItemsBySlot;

        public int SlotCount => m_ItemSlotSet?.ItemSlots?.Count ?? 0;

        public IReadOnlyList<ItemStack> ItemsBySlot => m_ItemsBySlot;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ItemSlotCollection() : base()
        {
        }

        /// <summary>
        /// Constructor which takes an ItemSlotSet as a parameter.
        /// </summary>
        /// <param name="itemSlotSet">The ItemSlotSet that should be initialized to the ItemCollection.</param>
        public ItemSlotCollection(ItemSlotSet itemSlotSet) : base()
        {
            m_ItemSlotSet = itemSlotSet;
        }

        /// <summary>
        /// This function should be called by a MonoBehaviour within Awake.
        /// </summary>
        /// <param name="owner">The object doing the initialization.</param>
        /// <param name="force">Force the initialization even if it was already initialized.</param>
        public override void Initialize(IInventory owner, bool force)
        {
            if (m_Initialized && !force) {
                return;
            }

            base.Initialize(owner, force);
            
            // Pre-add the itemStacks by slot. They will be added and removed from the itemstack list dynamically.
            // The AddItemInternal and RemoveItemInternal functions are overriden
            // which means the item stacks will be reused 
            // Therefore there is no guarantee that the ItemsBySlot index matches the ItemStacks indexes.
            m_ItemsBySlot = new ItemStack[m_ItemSlotSet?.ItemSlots?.Count ?? 0];
            for (int i = 0; i < m_ItemsBySlot.Length; i++) {
                var itemStack = new ItemStack();
                itemStack.Initialize(ItemAmount.None, this);
                m_ItemsBySlot[i] = itemStack;
            }

            // Do not perform any validity checks when the game isn't active. This is to prevent logs from being shown when the collection is being edited.
            if (!Application.isPlaying) {
                return;
            }

            if (m_ItemSlotSet == null) {
                Debug.LogError($"Error: The ItemSlotSet does not exist on Item Collection {Name}.");
                return;
            }

            if (m_ItemSlotSet.ItemSlots == null) {
                Debug.LogError($"Error: The slots do not exist on Item Collection {Name}.");
                return;
            }

            if ((m_ItemSlotSet as IDatabaseSwitcher).IsComponentValidForDatabase(InventorySystemManager
                    .Instance.Database) == false) {
                Debug.LogError($"Error: The item slot set {m_ItemSlotSet.name} in the item slot collection {m_Name} is using the wrong database.");
                return;
            }

            // Ensure a valid category exists for each slot.
            for (int i = 0; i < m_ItemSlotSet.ItemSlots.Count; ++i) {
                if (m_ItemSlotSet.ItemSlots[i].Category == null) {
                    Debug.LogError($"Error: A category must exist for each slot in the item slot set {m_ItemSlotSet.name}. The slot at index {i} is missing the category for the Item Collection {m_Name}.", m_Inventory?.gameObject);
                }
            }
        }

        /// <summary>
        /// Tries to get the item stack at the specified slot.
        /// </summary>
        /// <param name="slotIndex">The name of the slot to get the item at.</param>
        /// <returns>The item at the specified slot index. Can be null.</returns>
        public ItemStack GetItemStackAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_ItemsBySlot.Length) { return null; }

            return m_ItemsBySlot[slotIndex];
        }

        /// <summary>
        /// Tries to get the item at the specified slot.
        /// </summary>
        /// <param name="slotIndex">The name of the slot to get the item at.</param>
        /// <returns>The item at the specified slot index. Can be null.</returns>
        public Item GetItemAtSlot(int slotIndex)
        {
            var itemStack = GetItemStackAtSlot(slotIndex);
            return itemStack?.Item;
        }

        /// <summary>
        /// Get the item info for the slot index.
        /// </summary>
        /// <param name="slotIndex">The slot index.</param>
        /// <returns>The item info.</returns>
        public ItemInfo GetItemInfoAtSlot(int slotIndex)
        {
            if (slotIndex <= -1) { return ItemInfo.None; }

            return (ItemInfo)GetItemStackAtSlot(slotIndex);
        }

        /// <summary>
        /// Get the item info for the slot name.
        /// </summary>
        /// <param name="slotName">The slot name.</param>
        /// <returns>The item info.</returns>
        public ItemInfo GetItemInfoAtSlot(string slotName)
        {
            var slotIndex = ItemSlotSet.GetIndexOf(slotName);
            if (slotIndex == -1) { return ItemInfo.None; }

            return (ItemInfo)GetItemStackAtSlot(slotIndex);
        }

        /// <summary>
        /// Swaps an item from one slot to another. The slots need to be similar.
        /// </summary>
        /// <param name="slotIndex1">The slot that contains the item to swap.</param>
        /// <param name="slotIndex2">The slot that contains the item to swap.</param>
        /// <returns>Successfully swapped.</returns>
        public bool SwapItemSlot(int slotIndex1, int slotIndex2)
        {
            if (slotIndex1 < 0 || slotIndex1 >= m_ItemsBySlot.Length) { return false; }
            if (slotIndex2 < 0 || slotIndex2 >= m_ItemsBySlot.Length) { return false; }

            var itemSlot1 = ItemSlotSet.ItemSlots[slotIndex1];
            var itemSlot2 = ItemSlotSet.ItemSlots[slotIndex2];

            //Check that the items are allowed to be swapped.
            if (m_ItemsBySlot[slotIndex2] != null &&
                (!itemSlot1.Category.InherentlyContains(m_ItemsBySlot[slotIndex2].Item) ||
                 m_ItemsBySlot[slotIndex1] != null) &&
                !itemSlot2.Category.InherentlyContains(m_ItemsBySlot[slotIndex1].Item)) { return false; }

            var temp = m_ItemsBySlot[slotIndex1];
            m_ItemsBySlot[slotIndex1] = m_ItemsBySlot[slotIndex2];
            m_ItemsBySlot[slotIndex2] = temp;
            
            UpdateCollection();

            return true;
        }
        
        /// <summary>
        /// Add conditions, returns the itemInfo that can be added (or returns null if it cannot).
        /// </summary>
        /// <param name="itemInfo">Can this item be added.</param>
        public override ItemInfo? CanAddItem(ItemInfo itemInfo)
        {
            var targetSlot = GetTargetSlotIndex(itemInfo.Item);
            if (targetSlot == -1)
            {
                return null;
            }
            return base.CanAddItem(itemInfo);
        } 

        /// <summary>
        /// Add an item to the ItemCollection.
        /// </summary>
        /// <param name="itemInfo">The item info being added.</param>
        /// <param name="stackTarget">The item stack where the item should be added.</param>
        /// <returns>Returns the item info that was actually added.</returns>
        public override ItemInfo AddItem(ItemInfo itemInfo, ItemStack stackTarget = null)
        {
            return AddItem(itemInfo, GetTargetSlotIndex(itemInfo.Item));
        }

        /// <summary>
        /// Adds the item to the specified slot.
        /// </summary>
        /// <param name="itemInfo">The item that should be added.</param>
        /// <param name="slotName">The slot name where the item should be added to.</param>
        /// <returns>Returns the item that was actually added.</returns>
        public ItemInfo AddItem(ItemInfo itemInfo, string slotName)
        {
            var index = m_ItemSlotSet.GetIndexOf(slotName);
            if (index <= -1) { return (0, itemInfo.Item, this); }

            return AddItem(itemInfo, index);
        }

        /// <summary>
        /// Adds the item to the specified slot.
        /// </summary>
        /// <param name="itemInfo">The item that should be added.</param>
        /// <param name="slotIndex">The index of the slot that the item should be added to.</param>
        /// <returns>Returns the item that was actually added.</returns>
        public ItemInfo AddItem(ItemInfo itemInfo, int slotIndex)
        {
            var itemInfoAdded = AddItemInternal(itemInfo, slotIndex);
            
            if (itemInfoAdded.Amount < itemInfo.Amount) {
                HandleItemOverflow(itemInfo, ref itemInfoAdded);
            }
            
            return itemInfoAdded;
        }

        /// <summary>
        /// Adds the item to the specified slot.
        /// </summary>
        /// <param name="itemInfo">The item that should be added.</param>
        /// <param name="slotIndex">The index of the slot that the item should be added to.</param>
        /// <returns>Returns the item that was actually added.</returns>
        protected virtual ItemInfo AddItemInternal(ItemInfo itemInfo, int slotIndex)
        {
            var result = CanAddItem(itemInfo);
            if (result.HasValue == false) {
                return (0, itemInfo.Item, this);
            }

            var item = result.Value.Item;
            var amount = result.Value.Amount;

            var itemSlot = m_ItemSlotSet.GetSlot(slotIndex);
            if (!itemSlot.HasValue) {
                return (0, itemInfo.Item, this);
            }

            if (!m_NewItemPriority && item.IsUnique && m_ItemsBySlot[slotIndex].Item != null) {
                return (0, itemInfo.Item, this);
            }

            var setItemInfo = SetItemAmount((amount, itemInfo), slotIndex, true);
            if (setItemInfo.HasValue) {
                return setItemInfo.Value;
            } else {
                return (0, itemInfo.Item, this);
            }
        }

        /// <summary>
        /// Sets the amount for the specified item.
        /// </summary>
        /// <param name="itemInfo">The item to add.</param>
        /// <param name="slotIndex">The index of the slot that the item occupies.</param>
        /// <param name="removePreviousItem">If the slot is at its capacity, should the last item be replaced by the current item?</param>
        /// <returns>Returns the item info that was set.</returns>
        private ItemInfo? SetItemAmount(ItemInfo itemInfo, int slotIndex, bool removePreviousItem)
        {
            var itemSlot = m_ItemSlotSet.GetSlot(slotIndex);
            if (!itemSlot.HasValue) {
                return null;
            }

            var currentStack = m_ItemsBySlot[slotIndex];
            var amountToAdd = itemInfo.Amount;
            var amount = itemInfo.Amount;
            if (currentStack != null) {
                if (itemInfo.Item.StackableEquivalentTo(currentStack.Item)) {
                    // The items are the same, lets merge them.
                    amount += currentStack.Amount;
                } else if (removePreviousItem) {
                    var removedItem = RemoveItem((ItemInfo)currentStack);
                    if (removedItem.Amount > 0) {
                        if (m_TryGivePreviousItemToNewItemCollection &&
                            (itemInfo.ItemCollection?.CanAddItem(removedItem).HasValue ?? false)) {
                            itemInfo.ItemCollection.AddItem(removedItem);
                        } else {
                            var removedItemInfoAdded = ItemInfo.None;
                            HandleItemOverflow(removedItem, ref removedItemInfoAdded);
                        }
                    }
                } else {
                    return null;
                }
            }

            var itemToSet = (amount, itemInfo);
            var itemInfoAdded = AddInternal(itemToSet, currentStack, false);
            
            // Set back to the orginal added item info amount.
            itemInfoAdded = new ItemInfo(itemInfo.ItemAmount, itemInfoAdded);

            m_ItemsBySlot[slotIndex] = itemInfoAdded.ItemStack;

            NotifyAdd(itemInfo, itemInfoAdded.ItemStack);

            return itemInfoAdded;
        }
        
        /// <summary>
        /// Add an Item Amount in an organized way.
        /// </summary>
        /// <param name="itemInfo">The Item info being added to the item collection.</param>
        /// <param name="stackTarget">The item stack where you would like the item to be added (can be null).</param>
        protected override ItemInfo AddInternal(ItemInfo itemInfo, ItemStack stackTarget, bool notifyAdd = true)
        {
            if (stackTarget == null) {
                Debug.LogWarning("The ItemSlotCollection stack target cannot be null.");
                return ItemInfo.None;
            }

            var addedItemStack = stackTarget;
            stackTarget.Reset();
            stackTarget.Initialize(itemInfo.ItemAmount, this);
            if (m_ItemStacks.Contains(stackTarget) == false && stackTarget.Item != null) {
                m_ItemStacks.Add(stackTarget);
            }
            
            itemInfo.Item.AddItemCollection(this);

            if (notifyAdd) { NotifyAdd(itemInfo, addedItemStack); }

            return (ItemInfo)(itemInfo.Item, itemInfo.Amount, this, addedItemStack);
        }
        
        /// <summary>
        /// Internal method which removes an ItemAmount from the collection.
        /// </summary>
        /// <param name="itemInfo">The item info to remove.</param>
        /// <returns>Returns the number of items removed, 0 if no item was removed.</returns>
        protected override ItemInfo RemoveInternal(ItemInfo itemInfo)
        {
            var removed = 0;
            ItemStack itemStackToRemove = itemInfo.ItemStack;

            if (itemStackToRemove != null && itemStackToRemove.Item != null && itemStackToRemove.Item.ID == itemInfo.Item.ID) {
                removed = SimpleInternalItemRemove(itemInfo, removed, itemStackToRemove);
            }

            if (removed < itemInfo.Amount) {
                for (int i = m_ItemsBySlot.Length - 1; i >= 0; i--) {
                    if(m_ItemsBySlot[i].Item == null){ continue; }
                    if (m_ItemsBySlot[i].Item.ID != itemInfo.Item.ID) { continue; }

                    itemStackToRemove = m_ItemsBySlot[i];
                    removed = SimpleInternalItemRemove(itemInfo, removed, itemStackToRemove);
                    if (removed >= itemInfo.Amount) { break; }
                }
            }

            if (removed == 0) {
                return (removed, itemInfo.Item, this);
            }

            var removedItemInfo = (ItemInfo)(removed, itemInfo.Item, this, itemStackToRemove);
            
            NotifyItemRemoved(removedItemInfo);

            //Debug.Log(Name+" ItemCollection Removed item "+removedItemInfo);
            return removedItemInfo;
        }

        /// <summary>
        /// Simple remove of an item from a specific item stack.
        /// </summary>
        /// <param name="itemInfo">The item Info to remove.</param>
        /// <param name="removed">The amount already removed.</param>
        /// <param name="itemStackToRemove">The item stack to remove the amount from.</param>
        /// <returns>The amount removed (includes the amount previously removed).</returns>
        private int SimpleInternalItemRemove(ItemInfo itemInfo, int removed, ItemStack itemStackToRemove)
        {
            var remainingToRemove = itemInfo.Amount - removed;
            var newAmount = itemStackToRemove.Amount - remainingToRemove;
            if (newAmount <= 0) {
                // Remove the item from the item collection.
                itemStackToRemove.Item?.RemoveItemCollection(this);
                
                removed += itemStackToRemove.Amount;
                itemStackToRemove.Reset();
                itemStackToRemove.Initialize(ItemAmount.None, this);
                if (m_ItemStacks.Contains(itemStackToRemove)) {
                    m_ItemStacks.Remove(itemStackToRemove);
                }

                
            } else {
                removed += remainingToRemove;
                itemStackToRemove.SetAmount(newAmount);
            }

            return removed;
        }
        
        /// <summary>
        /// Removes the item at the specified slot and item index.
        /// </summary>
        /// <param name="slotName">The slot name of the item that should be removed.</param>
        /// <param name="amount">The amount to remove from that slot, -1 will remove all the amount.</param>
        /// <returns>True if the item was removed.</returns>
        public ItemInfo RemoveItemInSlot(string slotName, int amount = -1)
        {
            var index = m_ItemSlotSet.GetIndexOf(slotName);
            if (index <= -1) { return (ItemAmount.None, this); }

            return RemoveItem(index, amount);
        }

        /// <summary>
        /// Removes the item at the specified slot and item index.
        /// </summary>
        /// <param name="slotIndex">The slot index of the item that should be removed.</param>
        /// <param name="amount">The amount to remove from that slot, -1 will remove all the amount.</param>
        /// <returns>True if the item was removed.</returns>
        public ItemInfo RemoveItem(int slotIndex, int amount = -1)
        {
            if (slotIndex < 0 || slotIndex >= m_ItemsBySlot.Length) { return (ItemAmount.None, this); }

            var itemToRemove = m_ItemsBySlot[slotIndex];
            if (itemToRemove == null) { return  (ItemAmount.None, this); }
            
            var amountToRemove = amount > 0 ? amount : itemToRemove.Amount;
            var removed = RemoveInternal((itemToRemove.Item, amountToRemove, this, itemToRemove));

            m_ItemsBySlot[slotIndex] = removed.ItemStack;

            return removed;
        }

        /// <summary>
        /// Remove an item from the Item Collection.
        /// </summary>
        /// <param name="itemInfo">The item info to remove.</param>
        /// <returns>Returns true if the item was removed.</returns>
        public override ItemInfo RemoveItem(ItemInfo itemInfo)
        {
            var itemSlot = GetItemSlotIndex(itemInfo.Item);
            if (itemSlot == -1) {
                return (0, itemInfo.Item, this);
            }

            return RemoveItem(itemSlot, itemInfo.Amount);
        }

        /// <summary>
        /// Returns a slot that is suitable for the item provided.
        /// </summary>
        /// <param name="item">The item that needs a slot.</param>
        /// <returns>The desired slot. -1 indicates no slot.</returns>
        public int GetTargetSlotIndex(Item item)
        {
            if (m_ItemSlotSet == null || m_ItemSlotSet.ItemSlots == null) {
                Debug.LogWarning("Item Slot Collection is missing an Item Slot Set.");
                return -1;
            }

            var matchingCategoryUsedSlot = -1;
            var matchingCategoryEmptySlot = -1;
            for (int i = 0; i < m_ItemSlotSet.ItemSlots.Count; i++) {
                var itemSlot = m_ItemSlotSet.ItemSlots[i];
                if (!itemSlot.Category.InherentlyContains(item)) {
                    continue;
                }

                matchingCategoryUsedSlot = i;

                if (m_ItemsBySlot[i] == null || m_ItemsBySlot[i].Item == null) {
                    if (matchingCategoryEmptySlot != -1) { continue; }
                    matchingCategoryEmptySlot = i;
                    continue;
                }

                if (item.IsUnique == false && m_ItemsBySlot[i].Item.StackableEquivalentTo(item)) { return i; }

            }

            if (matchingCategoryEmptySlot != -1) { return matchingCategoryEmptySlot; }

            return matchingCategoryUsedSlot;
        }

        /// <summary>
        /// Returns the slot where the item is set.
        /// </summary>
        /// <param name="item">The item that is in the collection.</param>
        /// <returns>The slot where the item is equipped. -1 indicates no slot.</returns>
        public int GetItemSlotIndex(Item item)
        {

            var stackableEquivalentItemIndex = -1;
            for (int i = 0; i < m_ItemSlotSet.ItemSlots.Count; i++) {
                var itemSlot = m_ItemSlotSet.ItemSlots[i];
                if (itemSlot.Category != null && !itemSlot.Category.InherentlyContains(item)) {
                    continue;
                }

                if (m_ItemsBySlot[i] == null) { continue; }
                
                if (m_ItemsBySlot[i].Item == null) { continue; }

                if (m_ItemsBySlot[i].Item == item) { return i; }

                if (m_ItemsBySlot[i].Item.StackableEquivalentTo(item)) {
                    stackableEquivalentItemIndex = i;
                }
            }

            return stackableEquivalentItemIndex;
        }
        
        /// <summary>
        /// Returns the slot where the item is set.
        /// </summary>
        /// <param name="itemStack">The item that is in the collection.</param>
        /// <returns>The slot where the item is equipped. -1 indicates no slot.</returns>
        public int GetItemSlotIndex(ItemStack itemStack)
        {
            for (int i = 0; i < m_ItemsBySlot.Length; i++) {
                if (m_ItemsBySlot[i] == itemStack) { return i; }
            }

            return -1;
        }

        /// <summary>
        /// Check if the object contained by this component are part of the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>True if all the objects in the component are part of that database.</returns>
        bool IDatabaseSwitcher.IsComponentValidForDatabase(InventorySystemDatabase database)
        {
            if (database == null) { return false; }

            database.Initialize(false);

            return (m_ItemSlotSet as IDatabaseSwitcher)?.IsComponentValidForDatabase(database) ?? true;
        }

        /// <summary>
        /// Replace any object that is not in the database by an equivalent object in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>The objects that have been changed.</returns>
        ModifiedObjectWithDatabaseObjects IDatabaseSwitcher.ReplaceInventoryObjectsBySelectedDatabaseEquivalents(InventorySystemDatabase database)
        {
            if (database == null) { return null; }

            (m_ItemSlotSet as IDatabaseSwitcher).ReplaceInventoryObjectsBySelectedDatabaseEquivalents(database);

            return new UnityEngine.Object[] { m_ItemSlotSet };
        }
    }
}