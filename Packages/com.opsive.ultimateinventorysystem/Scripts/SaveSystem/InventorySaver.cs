/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.SaveSystem
{
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using System.Collections.Generic;
    using Opsive.Shared.Events;
    using UnityEngine;

    /// <summary>
    /// A saver component that saves the content of an inventory.
    /// </summary>
    public class InventorySaver : SaverBase
    {
        //A higher priority (-1 > 1) will be saved first.
        public override int SavePriority => -100;
        //A higher priority (-1 > 1) will be loaded first.
        public override int LoadPriority => -100;
        
        /// <summary>
        /// The inventory save data.
        /// </summary>
        [System.Serializable]
        public struct InventorySaveData
        {
            public IDAmountSaveData[][] ItemIDAmountsPerCollection;
        }

        [Tooltip("The inventory.")]
        [SerializeField] protected Inventory m_Inventory;
        [Tooltip("Is the save data added to the loadout of does it overwrite it.")]
        [SerializeField] protected bool m_Additive;

        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected override void Awake()
        {
            if (m_Inventory == null) { m_Inventory = GetComponent<Inventory>(); }
            base.Awake();
        }

        /// <summary>
        /// Serialize the save data.
        /// </summary>
        /// <returns>The serialized data.</returns>
        public override Serialization SerializeSaveData()
        {
            if (m_Inventory == null) { return null; }
            
            EventHandler.ExecuteEvent<Inventory>(EventNames.c_WillStartSaving_Inventory,m_Inventory);

            var itemCollectionCount = m_Inventory.GetItemCollectionCount();
            var newItemAmountsArray = new IDAmountSaveData[itemCollectionCount][];
            var listItemIDs = new List<uint>();

            for (int i = 0; i < itemCollectionCount; i++) {

                var itemCollection = m_Inventory.GetItemCollection(i);

                IReadOnlyList<ItemStack> allItemStacks;
                // Get the items by slot to save the correct index
                if (itemCollection is ItemSlotCollection itemSlotCollection) {
                    allItemStacks = itemSlotCollection.ItemsBySlot;
                } else {
                    allItemStacks = itemCollection.GetAllItemStacks();
                }

                var itemAmounts = new IDAmountSaveData[allItemStacks?.Count ?? 0];
                for (int j = 0; j < itemAmounts.Length; j++) {

                    var itemStack = allItemStacks[j];

                    if (itemStack?.Item == null) {
                        itemAmounts[j] = new IDAmountSaveData() {
                            ID = 0,
                            Amount = 0
                        };
                        continue;
                    }
                    
                    itemAmounts[j] = new IDAmountSaveData() {
                        ID = itemStack.Item.ID,
                        Amount = itemStack.Amount
                    };

                    listItemIDs.Add(itemStack.Item.ID);
                }

                newItemAmountsArray[i] = itemAmounts;
            }

            SaveSystemManager.InventorySystemManagerItemSaver.SetItemsToSave(FullKey, listItemIDs);

            var saveData = new InventorySaveData {
                ItemIDAmountsPerCollection = newItemAmountsArray
            };
            
            EventHandler.ExecuteEvent<Inventory,InventorySaveData>(EventNames.c_SavingComplete_Inventory_InventorySaveData,m_Inventory, saveData);

            return Serialization.Serialize(saveData);
        }

        /// <summary>
        /// Deserialize and load the save data.
        /// </summary>
        /// <param name="serializedSaveData">The serialized save data.</param>
        public override void DeserializeAndLoadSaveData(Serialization serializedSaveData)
        {
            if (m_Inventory == null) { return; }
            
            var savedData = serializedSaveData.DeserializeFields(MemberVisibility.All) as InventorySaveData?;
            
            EventHandler.ExecuteEvent<Inventory,InventorySaveData?>(EventNames.c_WillStartLoadingSave_Inventory_NullableInventorySaveData,m_Inventory, savedData);

            if (!m_Additive) {
                var itemCollectionCount = m_Inventory.GetItemCollectionCount();

                for (int i = 0; i < itemCollectionCount; i++) {
                    m_Inventory.GetItemCollection(i).RemoveAll();
                }
            }

            if (savedData.HasValue == false) {
                return;
            }

            var inventorySaveData = savedData.Value;

            if (inventorySaveData.ItemIDAmountsPerCollection == null) { return; }

            EventHandler.ExecuteEvent(m_Inventory.gameObject, EventNames.c_InventoryGameObject_InventoryMonitorListen_Bool, false);


            for (int i = 0; i < inventorySaveData.ItemIDAmountsPerCollection.Length; i++) {
                var itemIDAmounts = inventorySaveData.ItemIDAmountsPerCollection[i];
                var itemAmounts = new ItemAmount[itemIDAmounts.Length];
                for (int j = 0; j < itemIDAmounts.Length; j++) {
                    var idAmountSaveData = itemIDAmounts[j];
                    // The id can be 0 for item slot collections.
                    if(idAmountSaveData.ID == 0){ continue;}
                    
                    if (InventorySystemManager.ItemRegister.TryGetValue(idAmountSaveData.ID, out var item) == false) {
                        Debug.LogWarning($"Saved Item ID {idAmountSaveData.ID} could not be retrieved from the Inventory System Manager.");
                        continue;
                    }
                    itemAmounts[j] = new ItemAmount(item, idAmountSaveData.Amount);
                }

                var itemCollection = m_Inventory.GetItemCollection(i);
                if (itemCollection == null) {
                    Debug.LogWarning("Item Collection from save data is missing in the scene.");
                } else {
                    if (itemCollection is ItemSlotCollection itemSlotCollection) {
                        for (int j = 0; j < itemAmounts.Length; j++) {
                            var itemAmount = itemAmounts[j];
                            if(itemAmount.Item == null){ continue; }
                            itemSlotCollection.AddItem(new ItemInfo(itemAmount), j);
                        }
                    } else {
                        itemCollection.AddItems(itemAmounts);
                    }
                    
                }
            }
            
            EventHandler.ExecuteEvent(m_Inventory.gameObject, EventNames.c_InventoryGameObject_InventoryMonitorListen_Bool, true);

            EventHandler.ExecuteEvent<Inventory,InventorySaveData>(EventNames.c_LoadingSaveComplete_Inventory_InventorySaveData,m_Inventory, savedData.Value);

        }
    }
}