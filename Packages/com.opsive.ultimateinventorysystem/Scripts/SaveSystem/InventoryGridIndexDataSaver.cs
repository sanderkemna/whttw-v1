namespace Opsive.UltimateInventorySystem.SaveSystem
{
    using System;
    using System.Collections.Generic;
    using Opsive.Shared.Game;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers;
    using UnityEngine;

    /// <summary>
    /// A component used to save the Inventory Grid Index Data.
    /// </summary>
    public class InventoryGridIndexDataSaver : SaverBase
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
        public struct InventoryGridTabIndexerSaveData
        {
            public int GridID;
            public int TabID;
            public ItemStackIndexSaveData[] ItemStackIndexSaveDatas;
        }
        
        /// <summary>
        /// The inventory save data.
        /// </summary>
        [System.Serializable]
        public struct InventoryGridIndexDataSaveData
        {
            public InventoryGridTabIndexerSaveData[] IndexerSaveData;
        }

        [Header("InventoryGrids with ItemInfoGrid ID -1 will NOT be saved.")]
        [Tooltip("The inventory Grid Index Data to save.")]
        [SerializeField] protected InventoryGridIndexData m_InventoryGridIndexData;

        public InventoryGridIndexData InventoryGridIndexData
        {
            get => m_InventoryGridIndexData;
            set => m_InventoryGridIndexData = value;
        }
        
        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected override void Awake()
        {
            if (m_InventoryGridIndexData == null) { m_InventoryGridIndexData = GetComponent<InventoryGridIndexData>(); }

            base.Awake();
        }

        /// <summary>
        /// Serialize the save data.
        /// </summary>
        /// <returns>The serialized data.</returns>
        public override Serialization SerializeSaveData()
        {
            if (m_InventoryGridIndexData == null) { return null; }
            m_InventoryGridIndexData.Initialize(false);

            var gridIdTabIndexedItems = m_InventoryGridIndexData.GridIDTabIndexedItems;

            var indexerSaveData = new InventoryGridTabIndexerSaveData[gridIdTabIndexedItems.Count];

            var count = 0;
            foreach (var keyValuePair in gridIdTabIndexedItems) {
                var (gridId, tabId) = keyValuePair.Key;
                var inventoryGridIndexer = keyValuePair.Value;

                var inventoryGridTabIndexerSaveData = new InventoryGridTabIndexerSaveData()
                {
                    GridID = gridId,
                    TabID = tabId,
                    ItemStackIndexSaveDatas = GetIndexerSaveData(inventoryGridIndexer)
                };

                indexerSaveData[count] = inventoryGridTabIndexerSaveData;
                count++;
            }

            var saveData = new InventoryGridIndexDataSaveData()
            {
                IndexerSaveData = indexerSaveData
            };

            return Serialization.Serialize(saveData);
        }

        /// <summary>
        /// Get the Indexer save data.
        /// </summary>
        /// <param name="inventoryGridIndexer">The inventory grid indexer.</param>
        /// <returns>The save data for the inventory grid indexer.</returns>
        private ItemStackIndexSaveData[] GetIndexerSaveData(InventoryGridIndexer inventoryGridIndexer)
        {
            var indexedItems = inventoryGridIndexer.IndexedItems;
            var itemStackIndexSaveDatas = new ItemStackIndexSaveData[indexedItems.Count];

            int count = 0;
            foreach (var keyValuePair in indexedItems) {
                var itemStack = keyValuePair.Key;
                var index = keyValuePair.Value.index;

                itemStackIndexSaveDatas[count] = new ItemStackIndexSaveData()
                {
                    ItemStackSaveData = new ItemStackSaveData(itemStack),
                    Index = index
                };
                count++;
            }

            return itemStackIndexSaveDatas;
        }

        /// <summary>
        /// Deserialize and load the save data.
        /// </summary>
        /// <param name="serializedSaveData">The serialized save data.</param>
        public override void DeserializeAndLoadSaveData(Serialization serializedSaveData)
        {
            if (m_InventoryGridIndexData == null) { return; }
            m_InventoryGridIndexData.Initialize(false);

            var savedData = serializedSaveData.DeserializeFields(MemberVisibility.All) as InventoryGridIndexDataSaveData?;

            if (savedData.HasValue == false) {
                return;
            }
            
            var inventory = m_InventoryGridIndexData.Inventory;

            if(inventory == null){ return; }

            var inventoryGridSaveData = savedData.Value;

            if (inventoryGridSaveData.IndexerSaveData == null) {
                return;
            }

            var gridIDTabIndexedItems = new Dictionary<(int, int), InventoryGridIndexer>();
            for (int i = 0; i < inventoryGridSaveData.IndexerSaveData.Length; i++) {
                var indexerData = inventoryGridSaveData.IndexerSaveData[i];
                var indexedItems = GetIndexedItems(inventory, indexerData.ItemStackIndexSaveDatas);
                var inventoryGridIndexer = new InventoryGridIndexer(); 
                inventoryGridIndexer.SetIndexItems(indexedItems);

                gridIDTabIndexedItems[(indexerData.GridID, indexerData.TabID)] = inventoryGridIndexer;
            }
            
            m_InventoryGridIndexData.GridIDTabIndexedItems = gridIDTabIndexedItems;
            
            inventory.UpdateInventory(true, true);
        }

        /// <summary>
        /// Get the indexer items.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        /// <param name="itemStackIndexSaveDatas">The inventory grid indexer save data.</param>
        /// <returns></returns>
        private Dictionary<ItemStack, (int index, ItemInfo itemInfo)> GetIndexedItems(Inventory inventory, ItemStackIndexSaveData[] itemStackIndexSaveDatas)
        {
            if (itemStackIndexSaveDatas == null) {
                return null;
            }
            var indexedItems = new Dictionary<ItemStack, (int index, ItemInfo itemInfo)>();

            var allItems = inventory.AllItemInfos;
            for (int i = 0; i < allItems.Count; i++) {
                var itemStack = allItems[i].ItemStack;
                for (int j = 0; j < itemStackIndexSaveDatas.Length; j++) {
                    var itemStackIndexedSaveData = itemStackIndexSaveDatas[j];

                    if (itemStackIndexedSaveData.ItemStackSaveData.Match(itemStack) == false) {
                        continue;
                    }

                    if (indexedItems.ContainsKey(itemStack)) {
                        continue;
                    }

                    indexedItems[itemStack] = (itemStackIndexedSaveData.Index, (ItemInfo)itemStack);
                }
            }

            return indexedItems;
        }
    }
}