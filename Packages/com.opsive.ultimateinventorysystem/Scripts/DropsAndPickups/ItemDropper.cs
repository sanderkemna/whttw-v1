/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.DropsAndPickups
{
    using System.Collections.Generic;
    using Opsive.Shared.Game;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;

    /// <summary>
    /// An item dropper component.
    /// </summary>
    public class ItemDropper : Dropper
    {
        [Tooltip("The inventory with the items to drop.")]
        [SerializeField] protected Inventory m_Inventory;
        [Tooltip("The itemCollection within the inventory with the items to drop.")]
        [SerializeField]
        protected ItemCollectionID m_ItemCollectionID
            = new ItemCollectionID("ItemDrops", ItemCollectionPurpose.Drop);
        [Tooltip("If true the dropper will drop copies, if not the dropper will drop the original once.")]
        [SerializeField] protected bool m_DropCopies = true;
        [Tooltip("If the item dropped are unique, should they be split in multiple item infos, instead of one with higher amount.")]
        [SerializeField] protected bool m_SplitUniqueItems = true;

        // Item pickup or Inventory pickup
        protected bool m_IsItemPickup;
        protected ItemInfo[] m_CachedItemInfos;
        protected List<ItemInfo> m_CachedItemInfoList;

        public Inventory Inventory
        {
            get => m_Inventory;
            set => m_Inventory = value;
        }

        public ItemCollectionID ItemCollectionID
        {
            get => m_ItemCollectionID;
            set => m_ItemCollectionID = value;
        }

        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected virtual void Awake()
        {
            m_CachedItemInfos = new ItemInfo[0];
            m_CachedItemInfoList = new List<ItemInfo>();
            
            if (m_PickUpPrefab == null) {
                Debug.LogError("The PickUp Prefab is null, a Prefab with the scripts Item Pickup or Inventory Pickup must be specified.",gameObject);
                return;
            }

            if (m_PickUpPrefab.GetComponent<ItemPickup>() != null) {
                m_IsItemPickup = true;
                return;
            }

            if (m_PickUpPrefab.GetComponent<InventoryPickup>() != null) {
                m_IsItemPickup = false;
                return;
            }

            Debug.LogError("The PickUp Prefab is missing an Item Pickup or Inventory Pickup component.");
        }

        /// <summary>
        /// Drop the item.
        /// </summary>
        public override void Drop()
        {
            var itemsToDrop = GetItemsToDrop();

            DropItemsInternal(itemsToDrop);
        }

        /// <summary>
        /// Get the items to drop.
        /// </summary>
        /// <returns>returns a list of items to drop.</returns>
        public virtual ListSlice<ItemInfo> GetItemsToDrop()
        {
            var itemsToDrop = GetItemsToDropInternal();
            
            //Clear the list of item infos.
            m_CachedItemInfoList.Clear();
            
            for (int i = 0; i < itemsToDrop.Count; i++) {
                var itemInfo = itemsToDrop[i];
                if (itemInfo.Item == null || itemInfo.Item.IsInitialized == false) {
                    continue;
                }
                
                //If the item is common, no need to copy or split it.
                if ( itemInfo.Item.IsUnique == false) {
                    m_CachedItemInfoList.Add(itemInfo);
                    continue;
                }

                if (m_DropCopies) {
                    if (m_SplitUniqueItems) {
                        for (int j = 0; j < itemInfo.Amount; j++) {
                            var itemCopy = InventorySystemManager.CreateItem(itemInfo.Item);
                            m_CachedItemInfoList.Add( new ItemInfo(1, itemCopy) );
                        }
                    } else {
                        var itemCopy = InventorySystemManager.CreateItem(itemInfo.Item);
                        m_CachedItemInfoList.Add( new ItemInfo(itemInfo.Amount, itemCopy) );
                    }
                } else {
                    if (m_SplitUniqueItems) {
                        for (int j = 0; j < itemInfo.Amount; j++) {
                            m_CachedItemInfoList.Add( (1, itemInfo) );
                        }
                    } else {
                        m_CachedItemInfoList.Add( itemInfo );
                    }
                }
            }
            
            return m_CachedItemInfoList;
        }
        
        /// <summary>
        /// Get the items to drop.
        /// </summary>
        /// <returns>returns a list of items to drop.</returns>
        protected virtual ListSlice<ItemInfo> GetItemsToDropInternal()
        {
            var collection = m_Inventory.GetItemCollection(m_ItemCollectionID);
            var itemsToDrop = collection.GetAllItemInfos(ref m_CachedItemInfos);

            return itemsToDrop;
        }

        /// <summary>
        /// Drop an inventory pickup.
        /// </summary>
        /// <param name="itemList">The item collection to drop.</param>
        protected virtual void DropItemsInternal(ListSlice<ItemInfo> itemList)
        {
            if (m_IsItemPickup) {
                for (int i = 0; i < itemList.Count; i++) {
                    DropItemPickup(itemList[i]);
                }
            } else {
                DropInventoryPickup(itemList);
            }
        }

        /// <summary>
        /// Drop an inventory pickup.
        /// </summary>
        /// <param name="itemList">The item collection to drop.</param>
        public virtual void DropInventoryPickup(ListSlice<ItemInfo> itemList)
        {
            var inventoryPickup = ObjectPool.Instantiate(m_PickUpPrefab,
                m_DropTransform.position + DropOffset(),
                Quaternion.identity).GetComponent<InventoryPickup>();

            if (inventoryPickup == null) {
                Debug.LogWarning("The pickup prefab specified in the item dropper does not have an itemPickup component or a inventoryItemPickup component");
                return;
            }

            inventoryPickup.Inventory.MainItemCollection.RemoveAll();
            inventoryPickup.Inventory.MainItemCollection.AddItems(itemList);
        }

        /// <summary>
        /// Drop an item pickup.
        /// </summary>
        /// <param name="itemInfo">The item amount to drop.</param>
        public virtual void DropItemPickup(ItemInfo itemInfo)
        {
            var itemPickup = ObjectPool.Instantiate(m_PickUpPrefab,
                m_DropTransform.position + DropOffset(), Quaternion.identity).GetComponent<ItemPickup>();

            if (itemPickup == null) {
                Debug.LogWarning(
                    "The pickup prefab specified in the item dropper does not have an itemPickup component or a inventoryItemPickup component");
                return;
            }

            var itemObject = itemPickup.ItemObject;
            if (m_DropCopies) {
                var itemCopy = InventorySystemManager.CreateItem(itemInfo.Item);
                itemObject.SetItem((ItemInfo)(itemInfo.Amount, itemCopy));
            } else {
                itemObject.SetItem(itemInfo);
            }

        }
    }
}
