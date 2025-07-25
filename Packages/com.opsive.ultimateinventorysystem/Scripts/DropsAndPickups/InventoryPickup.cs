/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.DropsAndPickups
{
    using System.Collections.Generic;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Interactions;
    using UnityEngine;

    /// <summary>
    /// A pickup that uses the inventory component.
    /// </summary>
    [RequireComponent(typeof(Inventory))]
    [RequireComponent(typeof(Interactable))]
    public class InventoryPickup : PickupBase
    {
        [Tooltip("The item collection where the items should be added when picking up the item.")]
        [SerializeField] protected ItemCollectionID m_AddToItemCollection = ItemCollectionPurpose.Main;
        [Tooltip("Remove Items from pickup before picking them up.")]
        [SerializeField] protected bool m_RemovePickedUpItems;
        [Tooltip("Remove Items from pickup before picking them up.")]
        [SerializeField] protected bool m_PickupDuplicateItems;

        protected Inventory m_Inventory;
        protected List<ItemStack> m_CachedItems = new List<ItemStack>();

        public Inventory Inventory => m_Inventory;

        public ItemCollectionID AddToItemCollection
        {
            get => m_AddToItemCollection;
            set => m_AddToItemCollection = value;
        }

        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected void Awake()
        {
            m_Inventory = GetComponent<Inventory>();
        }

        /// <summary>
        /// The the inventory content to the interactors inventory.
        /// </summary>
        protected override void OnInteractInternal(IInteractor interactor)
        {
            if (!(interactor is IInteractorWithInventory interactorWithInventory)) { return; }
            
            var itemCollection = interactorWithInventory.Inventory.GetItemCollection(m_AddToItemCollection);

            if (itemCollection == null) {
                itemCollection = interactorWithInventory.Inventory.MainItemCollection;
            }

            AddPickupToCollection(itemCollection);
        }

        /// <summary>
        /// Add the pickup to the collection specified.
        /// </summary>
        /// <param name="itemCollection">The item Collection.</param>
        protected virtual void AddPickupToCollection(ItemCollection itemCollection)
        {
            var pickupCollection = m_Inventory.MainItemCollection;
            var pickupItems = pickupCollection.GetAllItemStacks();
            
            m_CachedItems.Clear();
            m_CachedItems.AddRange(pickupItems);
            
            Shared.Events.EventHandler.ExecuteEvent(m_Inventory.gameObject, "OnItemPickupStartPickup");
            
            var atLeastOneCanBeAdded = false;
            var atLeastOneCannotBeAdded = false;
            for (int i = 0; i < m_CachedItems.Count; i++) {
                var itemStack = m_CachedItems[i];
                var canAddResult =  itemCollection.CanAddItem((ItemInfo)itemStack);
                if (canAddResult.HasValue && canAddResult.Value.Amount != 0) {
                    atLeastOneCanBeAdded = true;

                    if (itemStack.Amount != canAddResult.Value.Amount) {
                        atLeastOneCannotBeAdded = true;
                    }

                    if (m_RemovePickedUpItems) {
                        canAddResult = pickupCollection.RemoveItem((ItemInfo)itemStack);
                    }else if (m_PickupDuplicateItems == false) {
                        // Pickup the original and then add a duplicate in the pickup.
                        canAddResult = pickupCollection.RemoveItem((ItemInfo)itemStack);
                        var duplicateItem = InventorySystemManager.CreateItem(canAddResult.Value.Item);
                        var itemInfo = new ItemInfo((duplicateItem, canAddResult.Value.Amount), canAddResult.Value);
                        pickupCollection.AddItem(itemInfo);
                    }

                    if (m_PickupDuplicateItems) {
                        var duplicateItem = InventorySystemManager.CreateItem(canAddResult.Value.Item);
                        var itemInfo = new ItemInfo((duplicateItem, canAddResult.Value.Amount), canAddResult.Value);
                        itemCollection.AddItem(itemInfo);
                    } else {
                        itemCollection.AddItem(canAddResult.Value);
                    }
                } else {
                    atLeastOneCannotBeAdded = true;
                }
            }

            if (atLeastOneCanBeAdded == false) {
                NotifyPickupFailed();
                Shared.Events.EventHandler.ExecuteEvent(m_Inventory.gameObject, "OnItemPickupStopPickup");
                return;
            }
            

            if (atLeastOneCannotBeAdded) {
                NotifyPartialPickup();
            } else {
                NotifyPickupSuccess();
            }
            
            Shared.Events.EventHandler.ExecuteEvent(m_Inventory.gameObject, "OnItemPickupStopPickup");
        }
    }
}