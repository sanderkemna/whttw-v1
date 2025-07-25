/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Item.DragAndDrop
{
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Interface for Item View Slot Drop Hover Selectable, used to preview that a drop could happen.
    /// </summary>
    public interface IItemViewSlotDropHoverSelectable
    {
        /// <summary>
        /// Select the slot with a drop handler.
        /// </summary>
        /// <param name="dropHandler">The drop handler.</param>
        void SelectWith(ItemViewDropHandler dropHandler);

        /// <summary>
        /// Deselect the slot with a drop handler.
        /// </summary>
        /// <param name="dropHandler">The drop handler.</param>
        void DeselectWith(ItemViewDropHandler dropHandler);
    }

    /// <summary>
    /// The Item View Slot Drop Handler Stream Data goes through the conditions and actions.
    /// </summary>
    public class ItemViewSlotDropHandlerStreamData
    {
        public ItemViewSlotEventData DragSlotEventData { get; set; }
        public ItemViewSlotsContainerBase SourceContainer { get; set; }
        public ItemViewSlot SourceItemViewSlot { get; set; }
        public ItemInfo SourceItemInfo { get; set; }
        public int SourceSlotIndexOffset { get; set; }
        public int SourceSlotIndex { get; set; }
        public int SourceIndex => SourceSlotIndex + SourceSlotIndexOffset;

        public ItemViewSlotEventData DropSlotEventData { get; set; }
        public ItemViewSlotsContainerBase DestinationContainer { get; set; }
        public ItemViewSlot DestinationItemViewSlot { get; set; }
        public ItemInfo DestinationItemInfo { get; set; }

        public int DestinationSlotIndex { get; set; }
        public int DestinationSourceSlotIndexOffset { get; set; }
        public int DestinationIndex => DestinationSlotIndex + DestinationSourceSlotIndexOffset;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public ItemViewSlotDropHandlerStreamData()
        {
        }

        /// <summary>
        /// Reset the stream data.
        /// </summary>
        /// <param name="sourceItemViewSlot">The source item view slot.</param>
        /// <param name="dragSlotEventData">The drag slot event data.</param>
        /// <param name="dropSlotEventData">The drop slot event data.</param>
        public virtual void Reset(ItemViewSlot sourceItemViewSlot, ItemViewSlotEventData dragSlotEventData, ItemViewSlotEventData dropSlotEventData)
        {
            DragSlotEventData = dragSlotEventData;
            SourceContainer = dragSlotEventData.ItemViewSlotsContainer;
            SourceItemViewSlot = sourceItemViewSlot;
            SourceItemInfo = dragSlotEventData.ItemInfo;
            SourceSlotIndex = dragSlotEventData.SlotIndex;
            SourceSlotIndexOffset = dragSlotEventData.SlotIndexOffset;

            DropSlotEventData = dropSlotEventData;
            DestinationContainer = dropSlotEventData.ItemViewSlotsContainer;
            DestinationItemViewSlot = dropSlotEventData.ItemViewSlot;
            DestinationItemInfo = dropSlotEventData.ItemInfo;
            DestinationSlotIndex = dropSlotEventData.SlotIndex;
            DestinationSourceSlotIndexOffset = dropSlotEventData.SlotIndexOffset;
        }
    }

    /// <summary>
    /// The item view drop handler.
    /// </summary>
    public class ItemViewDropHandler : MonoBehaviour
    {
        [Tooltip("This field shows the index of the last condition that past.")]
        [SerializeField] protected int m_DebugPassedConditionIndex;
        [Tooltip("The cursor Manager ID, used to get the Cursor Manager from anywhere in the scene.")]
        [SerializeField] protected uint m_CursorManagerID = 1;
        [Tooltip("The item view cursor manager.")]
        [SerializeField] protected ItemViewSlotCursorManager m_ItemViewSlotCursorManager;
        [Tooltip("Should the item be dropped on the slot under the mouse or on the last selected ItemView which can be selected by keyboard or by code?")]
        [SerializeField] protected bool m_DropOnLastSelectedView;
        [Tooltip("This event is invoked when an item view slot is selected and can be dropped on.")]
        [SerializeField] protected UnityEvent m_OnSelectCanDropEvent;
        [Tooltip("This event is invoked when an item view slot is selected and cannot be dropped on.")]
        [SerializeField] protected UnityEvent m_OnSelectCannotDropEvent;
        [Tooltip("This event is invoked when an item was dropped successfully.")]
        [SerializeField] protected UnityEvent m_OnDropSuccessEvent;
        [Tooltip("This event is invoked when an item failed to drop successfully.")]
        [SerializeField] protected UnityEvent m_OnDropFailEvent;

        [Tooltip("The Item View Slot Drop Action Set.")]
        [SerializeField] internal ItemViewSlotDropActionSet m_ItemViewSlotDropActionSet;

        protected ItemViewSlotsContainerBase m_ViewSlotsContainer;
        protected bool m_IsInitialized = false;
        protected ItemViewSlotDropHandlerStreamData m_StreamData;
        protected ItemViewSlotEventData m_DropSlotEventData;
        protected Vector2 m_MoveItemShapeOffset;
        protected int m_LastPassedConditionIndex;

        public uint CursorManagerID => m_CursorManagerID;
        public ItemViewSlotCursorManager SlotCursorManager => m_ItemViewSlotCursorManager;
        public ItemViewSlotDropHandlerStreamData StreamData => m_StreamData;

        public ItemViewSlotsContainerBase SourceContainer => m_ItemViewSlotCursorManager.SourceSlotEventData.ItemViewSlotsContainer;

        public ItemViewSlot SourceItemViewSlot => m_ItemViewSlotCursorManager.SourceItemViewSlot;

        public ItemInfo SourceItemInfo => m_ItemViewSlotCursorManager.SourceSlotEventData.ItemInfo;

        public int SourceIndex => m_ItemViewSlotCursorManager.SourceSlotEventData.ItemIndex;

        public ItemViewSlotEventData DropSlotEventData => m_DropSlotEventData;

        public ItemView DestinationItemView => m_DropSlotEventData?.ItemView;

        public ItemInfo DestinationItemInfo => DestinationItemView?.CurrentValue ?? (0, null, null);

        public ItemViewSlotsContainerBase DestinationContainer => m_DropSlotEventData?.ItemViewSlotsContainer;

        public int DestinationIndex => m_DropSlotEventData?.ItemIndex ?? -1;

        public ItemViewSlotDropActionSet ItemViewSlotDropActionSet {
            get => m_ItemViewSlotDropActionSet;
            set => m_ItemViewSlotDropActionSet = value;
        }

        public ItemViewSlotsContainerBase ViewSlotsContainer {
            get => m_ViewSlotsContainer;
            set => m_ViewSlotsContainer = value;
        }

        public int LastPassedConditionIndex
        {
            get => m_LastPassedConditionIndex;
        }
        
        public UnityEvent OnSelectCanDropEvent => m_OnSelectCanDropEvent;
        public UnityEvent OnSelectCannotDropEvent => m_OnSelectCannotDropEvent;
        public UnityEvent OnDropSuccessEvent => m_OnDropSuccessEvent;
        public UnityEvent OnDropFailEvent => m_OnDropFailEvent;

        /// <summary>
        /// Initialize.
        /// </summary>
        protected virtual void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the grid.
        /// </summary>
        public virtual void Initialize()
        {
            if (m_IsInitialized) { return; }

            if (m_ItemViewSlotCursorManager == null) {
                m_ItemViewSlotCursorManager = InventorySystemManager.GetItemViewSlotCursorManager(m_CursorManagerID);
                if (m_ItemViewSlotCursorManager == null) {
                    m_ItemViewSlotCursorManager = GetComponentInParent<ItemViewSlotCursorManager>();
                    if (m_ItemViewSlotCursorManager == null) {
                        Debug.LogWarning("The item view cursor manager is missing, please add one on your canvas.");
                    }
                }
            }
            
            Shared.Events.EventHandler.RegisterEvent(m_ItemViewSlotCursorManager.gameObject,
                EventNames.c_ItemViewSlotCursorManagerGameobject_StartMove, HandleOnMoveStart);

            if (m_ItemViewSlotDropActionSet != null) {
                m_ItemViewSlotDropActionSet.Initialize(false);
            }

            //Listen to the destination container events.
            m_ViewSlotsContainer = GetComponent<ItemViewSlotsContainerBase>();

            if (m_ViewSlotsContainer != null) {
                m_ViewSlotsContainer.OnItemViewSlotDropE += HandleItemViewSlotDrop;
                m_ViewSlotsContainer.OnItemViewSlotClicked += HandleItemViewSlotClicked;

                m_ViewSlotsContainer.OnItemViewSlotSelected += ItemViewSlotSelected;
                m_ViewSlotsContainer.OnItemViewSlotDeselected += ItemViewSlotDeselected;
            }

            m_StreamData = new ItemViewSlotDropHandlerStreamData();

            m_IsInitialized = true;
        }

        /// <summary>
        /// Move started, prepare the drop.
        /// </summary>
        protected virtual void HandleOnMoveStart()
        {
            var itemViewSlot = SourceItemViewSlot;
            
            var basePosition = itemViewSlot.transform.position;
            var viewPosition = basePosition;
            for (int i = 0; i < itemViewSlot.ItemView.Modules.Count; i++) {
                var module = itemViewSlot.ItemView.Modules[i];
                if (module is ItemShapeItemView itemShapeItemView) {
                    viewPosition = itemShapeItemView.ForegroundItemView.View.transform.position;
                }
            }

            m_MoveItemShapeOffset = basePosition - viewPosition;
        }

        /// <summary>
        /// The item view slot was clicked.
        /// </summary>
        /// <param name="sloteventdata">The slot event data.</param>
        protected virtual void HandleItemViewSlotClicked(ItemViewSlotEventData sloteventdata)
        {
            //Drop on click when using the MoveCursor.
            if (m_ItemViewSlotCursorManager.IsMovingItemView == false) { return; }
            
            HandleItemViewSlotDrop(sloteventdata);
        }

        /// <summary>
        /// Handle the Item View Slot Drop.
        /// </summary>
        /// <param name="dropSlotEventData">The drop slot event data.</param>
        public virtual void HandleItemViewSlotDrop(ItemViewSlotEventData dropSlotEventData)
        {
            var dragEventData = m_ItemViewSlotCursorManager.SourceSlotEventData;

            if (dragEventData == null) {
                Debug.LogWarning("dragEventData == null");
                return;
            }
            
            //Only drop if the drag and drop pointer ID is the same.
            if (dragEventData.PointerID != dropSlotEventData.PointerID) {
                return;
            }

            if (m_DropOnLastSelectedView) {
                var previousItemViewSlotContainer = m_DropSlotEventData.ItemViewSlotsContainer;
                var previousIndex = m_DropSlotEventData.SlotIndex;
                m_DropSlotEventData = dropSlotEventData;
                m_DropSlotEventData.SetValues(previousItemViewSlotContainer, previousIndex);
            } else {
                m_DropSlotEventData = dropSlotEventData;
            }

            m_StreamData.Reset(SourceItemViewSlot, dragEventData, m_DropSlotEventData);

            m_ItemViewSlotCursorManager.BeforeDrop();
            HandleItemViewSlotDropInternal();
            m_ItemViewSlotCursorManager.RemoveItemView();
        }

        /// <summary>
        /// Handle the Drop.
        /// </summary>
        protected virtual void HandleItemViewSlotDropInternal()
        {
            if (m_ItemViewSlotDropActionSet == null) {
                InvokeOnDropFail();
                return;
            }
            
            m_LastPassedConditionIndex = m_ItemViewSlotDropActionSet.HandleItemViewSlotDrop(this);
            if (m_LastPassedConditionIndex == -1) {
                InvokeOnDropFail();
            } else {
                InvokeOnDropSuccess();
            }

            if (SourceContainer != null) {
                SourceContainer.Draw();
            }

            if (DestinationContainer != null && SourceContainer != DestinationContainer) {
                DestinationContainer.Draw();
            }
        }

        /// <summary>
        /// An Item View slot was selected.
        /// </summary>
        /// <param name="eventdata">The event data.</param>
        public virtual void ItemViewSlotSelected(ItemViewSlotEventData eventdata)
        {
            if (m_ItemViewSlotCursorManager.IsMovingItemView == false) { return; }
            
            m_DropSlotEventData = eventdata;
            m_StreamData.Reset(SourceItemViewSlot, m_ItemViewSlotCursorManager.SourceSlotEventData, eventdata);

            m_LastPassedConditionIndex = m_ItemViewSlotDropActionSet.GetFirstPassingConditionIndex(this);
            m_DebugPassedConditionIndex = m_LastPassedConditionIndex;

            if (m_LastPassedConditionIndex == -1) {
                InvokeOnSelectCannotDrop();
            } else {
                InvokedOnSelectCanDrop();
            }

            var selectedItemView = eventdata.ItemView;

            // Select the item view.
            for (int i = 0; i < selectedItemView.Modules.Count; i++) {
                if (selectedItemView.Modules[i] is IItemViewSlotDropHoverSelectable module) {
                    module.SelectWith(this);
                }
            }
            
            //Move the item view when you are not using the drag handler.
            if (m_ItemViewSlotCursorManager.UsingDrag == false) {
                var basePosition = eventdata.ItemViewSlot.transform.position;
                var viewPosition = new Vector2(basePosition.x, basePosition.y) - m_MoveItemShapeOffset;
            
                m_ItemViewSlotCursorManager.SetPosition(viewPosition);
            }
        }

        /// <summary>
        /// An item View slot was deselected.
        /// </summary>
        /// <param name="slotEventData">The event data.</param>
        public virtual void ItemViewSlotDeselected(ItemViewSlotEventData slotEventData)
        {
            var itemView = slotEventData.ItemView;
            if (m_ItemViewSlotCursorManager.IsMovingItemView == false) { return; }

            for (int i = 0; i < itemView.Modules.Count; i++) {
                if (itemView.Modules[i] is IItemViewSlotDropHoverSelectable module) {
                    module.DeselectWith(this);
                }
            }
        }
        
        /// <summary>
        /// Invoke an event when on select an item view which can be dropped on.
        /// </summary>
        protected virtual void InvokedOnSelectCanDrop()
        {
            m_OnSelectCanDropEvent?.Invoke();
        }

        /// <summary>
        /// Invoke an event when on select an item view which cannot be dropped on.
        /// </summary>
        protected virtual void InvokeOnSelectCannotDrop()
        {
            m_OnSelectCannotDropEvent?.Invoke();
        }
        
        /// <summary>
        /// Invoke an event when the item failed to pass the condition to drop.
        /// </summary>
        protected virtual void InvokeOnDropFail()
        {
            m_OnDropFailEvent?.Invoke();
        }

        /// <summary>
        /// Invoke an event when the item successfully dropped.
        /// </summary>
        protected virtual void InvokeOnDropSuccess()
        {
            m_OnDropSuccessEvent?.Invoke();
        }
    }
}