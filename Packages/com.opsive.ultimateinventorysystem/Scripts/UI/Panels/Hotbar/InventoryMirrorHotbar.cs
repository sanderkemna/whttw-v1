/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Panels.Hotbar
{
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.UI.Item;
    using UnityEngine;

    /// <summary>
    /// A component used to mirror the first few slots of an Inventory Grid.
    /// </summary>
    public class InventoryMirrorHotbar : ItemHotbar
    {
        [Tooltip("The Inventory Grid ID to Mirror")]
        [SerializeField] protected int m_InventoryGridID;
        [Tooltip("The Inventory Grid to Mirror")]
        [SerializeField] protected InventoryGrid m_InventoryGrid;
        [Tooltip("The Inventory Grid index at which to start Mirroring the items")]
        [SerializeField] protected int m_StartIndex = 0;
        [Tooltip("Prevent the items from being moved from the hotbar to the grid")]
        [SerializeField] protected bool m_PreventMoveToGrid;
        [Tooltip("Prevent the items from being moved from the grid to the hotbar")]
        [SerializeField] protected bool m_PreventMoveFromGrid;

        public InventoryGrid InventoryGrid
        {
            get => m_InventoryGrid;
            set { m_InventoryGrid = value; }
        }
        
        public int InventoryGridID
        {
            get => m_InventoryGridID;
            set { m_InventoryGridID = value; }
        }

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="force">Force initialize.</param>
        public override void Initialize(bool force)
        {
            base.Initialize(force);

            if (m_InventoryGrid == null) {
                Debug.LogError("The Inventory Mirror Hotbar requires an Inventory Grid to work, please assign one in the Inspector.");
                return;
            }
        }

        /// <summary>
        /// A new Inventory was bound to the container.
        /// </summary>
        protected override void OnInventoryBound()
        {
            base.OnInventoryBound();
            RegisterEvent(true);
        }

        /// <summary>
        /// The inventory was unbound from the container.
        /// </summary>
        protected override void OnInventoryUnbound()
        {
            base.OnInventoryUnbound();
            RegisterEvent(false);
        }

        /// <summary>
        /// Register or unregister the event.
        /// </summary>
        /// <param name="register">Register the event?</param>
        public void RegisterEvent(bool register)
        {
            Shared.Events.EventHandler.RegisterUnregisterEvent<InventoryGrid, ListSlice<ItemInfo>, int>(register,
                m_Inventory.gameObject,
                EventNames.c_InventoryGameObject_OnInventoryGridUpdate_InventoryGrid_ListSliceOfItemInfos_GridID,
                OnInventoryGridUpdate);
        }

        /// <summary>
        /// An Inventory Grid has updated, if the id match refresh.
        /// </summary>
        /// <param name="inventoryGrid">The InventoryGrid that executed the event.</param>
        /// <param name="itemInfoListSlice"></param>
        /// <param name="id"></param>
        private void OnInventoryGridUpdate(InventoryGrid inventoryGrid, ListSlice<ItemInfo> itemInfoListSlice, int id)
        {
            if ( id != -1 && (id == m_InventoryGrid.GridID || id == m_InventoryGridID) )
            {
                m_InventoryGrid = inventoryGrid;
                Draw();
            } else if (inventoryGrid == m_InventoryGrid) {
                Draw();
            }
        }

        /// <summary>
        /// Draw the Item Hotbar and refresh all the views.
        /// </summary>
        protected override void RefreshItemSlotInfos()
        {
            //Draw the Inventory Grid to update the indexes of the item infos
            var itemInfoListSliceInGrid = m_InventoryGrid.FilterAndSortItemInfos(false);
            
            var slots = m_ItemViewSlots;
            for (int i = 0; i < m_ItemViewSlots.Length; i++) {
                var infoIndex = i + m_StartIndex;
                if (infoIndex < 0 || infoIndex >= itemInfoListSliceInGrid.Count) {
                    slots[i].SetItemInfo(ItemInfo.None);
                    continue;
                }

                slots[i].SetItemInfo(itemInfoListSliceInGrid[infoIndex]);
            }
        }

        /// <summary>
        /// Can the item be moved from one index to another.
        /// </summary>
        /// <param name="sourceIndex">The source index.</param>
        /// <param name="destinationIndex">The destination index.</param>
        /// <returns>True if the item can be moved.</returns>
        public override bool CanMoveItem(int sourceIndex, int destinationIndex)
        {
            return m_InventoryGrid.CanMoveItem(sourceIndex-m_StartIndex, SlotIndexToGridIndex(destinationIndex));
        }

        /// <summary>
        /// Move the item from one index to another.
        /// </summary>
        /// <param name="sourceIndex">The source index.</param>
        /// <param name="destinationIndex">The destination index.</param>
        public override void MoveItem(int sourceIndex, int destinationIndex)
        {
            m_InventoryGrid.MoveItem(sourceIndex-m_StartIndex, SlotIndexToGridIndex(destinationIndex));
        }

        /// <summary>
        /// Due to the nature of the item hotbar, it cannot give items.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="slotIndex">The slot index.</param>
        /// <returns>False.</returns>
        public override bool CanGiveItem(ItemInfo itemInfo, int slotIndex)
        {
            if (m_PreventMoveToGrid && ReferenceEquals(itemInfo.Inventory, m_InventoryGrid.Inventory)) {
                return false;
            }
            
            return m_InventoryGrid.CanGiveItem(itemInfo, SlotIndexToGridIndex(slotIndex));
        }

        /// <summary>
        /// Can the item be added to the index?
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <param name="index">The index.</param>
        /// <returns>True if the item can be added.</returns>
        public override bool CanAddItem(ItemInfo itemInfo, int index)
        {
            if (m_PreventMoveFromGrid && ReferenceEquals(itemInfo.Inventory, m_InventoryGrid.Inventory)) {
                return false;
            }
            return m_InventoryGrid.CanAddItem(itemInfo, SlotIndexToGridIndex(index));
        }

        /// <summary>
        /// Add the item to the slot.
        /// </summary>
        /// <param name="itemInfo">The item info to add.</param>
        /// <param name="index">The index to add the item to.</param>
        /// <returns>The item info that was actually added.</returns>
        public override ItemInfo AddItem(ItemInfo itemInfo, int index)
        {
            return m_InventoryGrid.AddItem(itemInfo, index-m_StartIndex);
        }

        /// <summary>
        /// Remove the item from an index.
        /// </summary>
        /// <param name="itemInfo">The item info to remove.</param>
        /// <param name="index">The index to remove the item from.</param>
        /// <returns>Returns the item info removed.</returns>
        public override ItemInfo RemoveItem(ItemInfo itemInfo, int index)
        {
            return m_InventoryGrid.RemoveItem(itemInfo, index-m_StartIndex);
        }

        /// <summary>
        /// Unassign an item to a slot.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        public override void UnassignItemFromSlots(ItemInfo itemInfo)
        {
            m_InventoryGrid.UnassignItemFromSlots(itemInfo);
        }

        /// <summary>
        /// Get the matching slot index to the grid index
        /// </summary>
        /// <param name="slotIndex">The slot index.</param>
        /// <returns>The matching grid index.</returns>
        public virtual int SlotIndexToGridIndex(int slotIndex)
        {
            return slotIndex - m_StartIndex;
        }
    }
}