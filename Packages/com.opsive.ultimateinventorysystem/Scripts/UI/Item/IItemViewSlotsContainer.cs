/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Item
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public delegate void ItemViewSlotEventHandler(ItemViewSlotEventData slotEventData);

    /// <summary>
    /// Item View Slot Event Data for submit, select, and other event types without mouse/pointer.
    /// </summary>
    public class ItemViewSlotEventData
    {
        protected int m_PointerID;
        protected ItemViewSlotsContainerBase m_ItemViewSlotsContainer;
        protected ItemViewSlot m_ItemViewSlot;
        protected ItemInfo m_ItemInfo;
        protected int m_SlotIndex;
        protected int m_SlotIndexOffset;

        public virtual int PointerID => m_PointerID;
        public ItemViewSlotsContainerBase ItemViewSlotsContainer => m_ItemViewSlotsContainer;
        public ItemViewSlot ItemViewSlot => m_ItemViewSlot;
        public ItemView ItemView => m_ItemViewSlot.ItemView;
        public ItemInfo ItemInfo => m_ItemInfo;
        public int SlotIndex => m_SlotIndex;
        public int SlotIndexOffset => m_SlotIndexOffset;
        
        public int ItemIndex => SlotIndex + SlotIndexOffset;

        /// <summary>
        /// Reset the values to default.
        /// </summary>
        public virtual void Reset()
        {
            m_PointerID = int.MinValue; // -1,-2, -3 are used for mouse clicks, positive numbers are used for touch
            m_ItemViewSlotsContainer = null;
            m_ItemViewSlot = null;
            m_SlotIndex = -1;
            m_SlotIndexOffset = -1;
            m_ItemInfo = ItemInfo.None;
        }

        /// <summary>
        /// Set the event data values.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="index"></param>
        public void SetValues(ItemViewSlotsContainerBase container, int index)
        {
            Reset();
            m_PointerID = -4; // -1,-2, -3 are used for mouse clicks, positive numbers are used for touch
            m_ItemViewSlotsContainer = container;
            m_SlotIndex = index;
            m_SlotIndexOffset = m_ItemViewSlotsContainer.SlotIndexOffset;
            m_ItemViewSlot = ItemViewSlotsContainer.GetItemViewSlot(index);
            m_ItemInfo = ItemView?.CurrentValue ?? ItemInfo.None;
        }

        /// <summary>
        /// Set the event data values.
        /// </summary>
        /// <param name="itemViewSlot"></param>
        public void SetValues(ItemViewSlot itemViewSlot)
        {
            Reset();
            m_PointerID = -4; // -1,-2, -3 are used for mouse clicks, positive numbers are used for touch
            m_ItemViewSlotsContainer = null;
            m_SlotIndex = 0;
            m_SlotIndexOffset = 0;
            m_ItemViewSlot = itemViewSlot;
            m_ItemInfo = ItemView?.CurrentValue ?? ItemInfo.None;
        }
    }

    public delegate void ItemViewSlotPointerEventHandler(ItemViewSlotPointerEventData eventData);

    /// <summary>
    /// Item View Slot Event Data for click, select, drag and other types of events.
    /// </summary>
    public class ItemViewSlotPointerEventData : ItemViewSlotEventData
    {
        public PointerEventData PointerEventData { get; set; }
        public override int PointerID => PointerEventData?.pointerId ?? m_PointerID;

        /// <summary>
        /// Reset the event data.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            PointerEventData = null;
        }
    }
}