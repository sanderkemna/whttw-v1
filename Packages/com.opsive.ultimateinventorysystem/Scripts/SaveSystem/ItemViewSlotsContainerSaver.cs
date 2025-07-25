namespace Opsive.UltimateInventorySystem.SaveSystem
{
    using System;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.UI.Item;
    using UnityEngine;

    public class ItemViewSlotsContainerSaver : SaverBase
    {
        [Serializable]
        public struct ItemStackIndexSaveData
        {
            public ItemStackSaveData ItemStackSaveData;
            public int Index;
        }
        
        /// <summary>
        /// The inventory save data.
        /// </summary>
        [System.Serializable]
        public struct ItemViewSlotsContainerSaveData
        {
            public ItemStackIndexSaveData[] ItemStackIndexSaveData;
        }
        
        [Tooltip("The inventory Grid Index Data to save.")]
        [SerializeField] protected ItemViewSlotsContainerBase m_ItemViewSlotsContainer;

        public ItemViewSlotsContainerBase ItemViewSlotsContainer
        {
            get => m_ItemViewSlotsContainer;
            set => m_ItemViewSlotsContainer = value;
        }
        
        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected override void Awake()
        {
            if (m_ItemViewSlotsContainer == null) { m_ItemViewSlotsContainer = GetComponent<ItemViewSlotsContainerBase>(); }

            base.Awake();
        }
        
        public override Serialization SerializeSaveData()
        {
            if (m_ItemViewSlotsContainer == null) { return null; }

            var itemCount = m_ItemViewSlotsContainer.GetItemCount();

            var indexerSaveData = new ItemStackIndexSaveData[itemCount];

            for (int i = 0; i < itemCount; i++) {
                indexerSaveData[i] = new ItemStackIndexSaveData()
                {
                    ItemStackSaveData = new ItemStackSaveData(m_ItemViewSlotsContainer.GetItemAt(i).ItemStack),
                    Index = i
                };
            }
            
            var saveData = new ItemViewSlotsContainerSaveData()
            {
                ItemStackIndexSaveData = indexerSaveData
            };

            return Serialization.Serialize(saveData);
        }

        public override void DeserializeAndLoadSaveData(Serialization serializedSaveData)
        {
            if (m_ItemViewSlotsContainer == null) { return; }

            var savedData = serializedSaveData.DeserializeFields(MemberVisibility.All) as ItemViewSlotsContainerSaveData?;

            if (savedData.HasValue == false) {
                return;
            }
            
            var inventory = m_ItemViewSlotsContainer.Inventory;

            if(inventory == null){ return; }

            var inventoryGridSaveData = savedData.Value;

            if (inventoryGridSaveData.ItemStackIndexSaveData == null) {
                return;
            }

            for (int i = 0; i < inventoryGridSaveData.ItemStackIndexSaveData.Length; i++) {
                var indexerData = inventoryGridSaveData.ItemStackIndexSaveData[i];
                var indexedItems = GetIndexedItems(inventory, indexerData);
                m_ItemViewSlotsContainer.AssignItemToSlot(indexedItems, i);
            }
            
            m_ItemViewSlotsContainer.ResetDraw();
        }
        
        /// <summary>
        /// Get the indexer items.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        /// <param name="itemStackIndexSaveData">The inventory grid indexer save data.</param>
        /// <returns></returns>
        private ItemInfo GetIndexedItems(Inventory inventory, ItemStackIndexSaveData itemStackIndexSaveData)
        {
            var allItems = inventory.AllItemInfos;
            for (int i = 0; i < allItems.Count; i++) {
                var itemStack = allItems[i].ItemStack;
                var itemStackIndexedSaveData = itemStackIndexSaveData;

                if (itemStackIndexedSaveData.ItemStackSaveData.Match(itemStack) == false) {
                    continue;
                }

                return (ItemInfo)itemStack;
            }

            return ItemInfo.None;
        }
    }
}