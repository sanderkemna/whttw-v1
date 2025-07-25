/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers
{
    using System;
    using Opsive.UltimateInventorySystem.UI.Item;
    using System.Collections.Generic;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using UnityEngine;

    /// <summary>
    /// Inventory grid index data is used to keep track of the position of items within grids. 
    /// </summary>
    public class InventoryGridIndexData : MonoBehaviour
    {
        [Header("InventoryGrids with ItemInfoGrid ID -1 are stored by reference, the rest are stored by ID.")]
        
        [Tooltip("The inventory that the index data is attached to.")]
        [SerializeField] protected Inventory m_Inventory;
        
        protected Dictionary<(int gridID, int tabID), InventoryGridIndexer> m_GridIDTabIndexedItems;
        protected Dictionary<(InventoryGrid grid, int tabID), InventoryGridIndexer> m_GridTabIndexedItems;

        protected bool m_IsInitialized = false;
        
        public Inventory Inventory
        {
            get
            {
                if (m_IsInitialized == false) { Initialize(false); }
                return m_Inventory;
            }
            set => m_Inventory = value;
        }

        public Dictionary<(int gridID, int tabID), InventoryGridIndexer> GridIDTabIndexedItems
        {
            get => m_GridIDTabIndexedItems;
            set => m_GridIDTabIndexedItems = value;
        }

        public Dictionary<(InventoryGrid grid, int tabID), InventoryGridIndexer> GridTabIndexedItems
        {
            get => m_GridTabIndexedItems; 
            set => m_GridTabIndexedItems = value;
        }

        /// <summary>
        /// Awake.
        /// </summary>
        protected virtual void Awake()
        {
            Initialize(false);
        }

        /// <summary>
        /// Initialize the component.
        /// </summary>
        /// <param name="force">Force the component to reinitialize.</param>
        public virtual void Initialize(bool force)
        {
            if(m_IsInitialized && !force){ return;}
            
            if (m_Inventory == null) { m_Inventory = GetComponent<Inventory>(); }

            if (m_GridIDTabIndexedItems == null) {
                m_GridIDTabIndexedItems = new Dictionary<(int, int), InventoryGridIndexer>();
            }

            if (m_GridTabIndexedItems == null) {
                m_GridTabIndexedItems = new Dictionary<(InventoryGrid grid, int tabID), InventoryGridIndexer>();
            }

            m_IsInitialized = true;
        }

        /// <summary>
        /// Set the grid index data from an inventory grid.
        /// </summary>
        /// <param name="inventoryGrid">The inventory grid.</param>
        public virtual void SetGridIndexData(InventoryGrid inventoryGrid)
        {
            if (inventoryGrid == null) { return; }

            InventoryGridIndexer value = null;
            var tabId = inventoryGrid.TabID;
            var gridId = inventoryGrid.GridID;
            
            if (gridId == -1) {
                if (m_GridTabIndexedItems == null) {
                    m_GridTabIndexedItems = new Dictionary<(InventoryGrid, int), InventoryGridIndexer>();
                }

                if (m_GridTabIndexedItems.TryGetValue((inventoryGrid, tabId), out value)) {
                    value.Copy(inventoryGrid.InventoryGridIndexer);
                } else {
                    var newValue = new InventoryGridIndexer();
                    newValue.Copy(inventoryGrid.InventoryGridIndexer);
                    m_GridTabIndexedItems[(inventoryGrid, tabId)] = newValue;
                }
                return;
            }

            if (m_GridIDTabIndexedItems == null) {
                m_GridIDTabIndexedItems = new Dictionary<(int, int), InventoryGridIndexer>();
            }

            if (m_GridIDTabIndexedItems.TryGetValue((gridId, tabId), out value)) {
                value.Copy(inventoryGrid.InventoryGridIndexer);
            } else {
                var newValue = new InventoryGridIndexer();
                newValue.Copy(inventoryGrid.InventoryGridIndexer);
                m_GridIDTabIndexedItems[(gridId, tabId)] = newValue;
            }
        }

        /// <summary>
        /// Get the grid indexer for the inventory grid.
        /// </summary>
        /// <param name="inventoryGrid">The inventory grid.</param>
        /// <returns>The grid indexer.</returns>
        public virtual InventoryGridIndexer GetGridIndexer(InventoryGrid inventoryGrid)
        {
            if (inventoryGrid == null) { return null; }

            InventoryGridIndexer value = null;

            if (inventoryGrid.GridID == -1) {
                if (m_GridTabIndexedItems == null) { return null; }
                if (m_GridTabIndexedItems.TryGetValue((inventoryGrid, inventoryGrid.TabID), out value)) {
                    return value;
                }
                return null;
            }

            if (m_GridIDTabIndexedItems == null) { return null; }
            if (m_GridIDTabIndexedItems.TryGetValue((inventoryGrid.GridID, inventoryGrid.TabID), out value)) {
                return value;
            }

            return null;
        }
    }
}