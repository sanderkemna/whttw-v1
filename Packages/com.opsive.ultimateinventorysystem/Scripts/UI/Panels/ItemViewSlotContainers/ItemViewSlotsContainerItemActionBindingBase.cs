/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers
{
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.ItemActions;
    using Opsive.UltimateInventorySystem.UI.Item;
    using Opsive.UltimateInventorySystem.UI.Panels.ActionPanels;
    using System;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.UI.Item.ItemViewModules;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Base class to add actions to the item view slots container.
    /// </summary>
    public abstract class ItemViewSlotsContainerItemActionBindingBase : ItemViewSlotsContainerBinding
    {
        public event Action OnItemUserAssigned;
        
        [Tooltip("The item user. Defaults to the Inventory Item User if null.")]
        [SerializeField] protected ItemUser m_ItemUser;
        [Tooltip("The action panel will open when clicking an item, displaying the actions that can be used on it. Can be null.")]
        [SerializeField] internal ItemActionPanel m_ActionPanel;
        [Tooltip("Draw the Item Action Panel again when an item view slot container is redrawn?")]
        [SerializeField] protected bool m_RefreshActionPanelOnContainerDraw = false;
        [Tooltip("Draw the Item Action Panel again when an item view slot container is redrawn?")]
        [SerializeField] protected bool m_OpenActionPanelOnContainerDraw = false;
        [Tooltip("If after the Item Action was invoked, the previous slot is empty, try selecting the first slot.")]
        [SerializeField] protected bool m_IfReturnSelectEmptyTrySelectSlot0 = false;
        [Tooltip("Use the item on click.")]
        [SerializeField] protected bool m_UseItemOnClick = true;
        [Tooltip("Which item action to use when triggered, -1 will use all item actions.")]
        [SerializeField] protected int m_UseItemActionIndex = -1;
        [Tooltip("Allow item actions to be called on empty slots.")]
        [SerializeField] protected bool m_DisableActionOnEmptySlots;
        [Tooltip("Prevent opening the Item Action Panel when no actions are available for the item.")]
        [SerializeField] protected bool m_PreventOpenWhenNoAction;
        [Tooltip("Set the Item User to the Inventory Item User.")] 
        [SerializeField] protected bool m_AutoSetItemUser = true;
        [Tooltip("Invoke on cannot open panel.")]
        [SerializeField] protected UnityEvent m_OnCannotOpenPanel;
        [Tooltip("Invoked on action canceled.")]
        [SerializeField] protected UnityEvent m_OnActionCanceled;
        [Tooltip("Invoked on action invoked.")]
        [SerializeField] protected UnityEvent m_OnActionInvoked;

        protected ListSlice<ItemAction> m_ItemActionListSlice;
        protected ItemViewSlot m_SelectedSlot;
        protected ItemInfo m_SelectedItemInfo;
        protected bool m_WaitingForActionFromPanel;

        public ItemUser ItemUser => m_ItemUser;
        public ItemActionPanel ItemActionPanel => m_ActionPanel;

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <param name="force">Force Initialize.</param>
        public override void Initialize(bool force)
        {
            if (m_IsInitialized && force == false) { return; }
            base.Initialize(force);

            if (m_ActionPanel != null) { m_ActionPanel.Close(); }

            m_ItemActionListSlice = InitializeItemActionList();
        }
        
        
        /// <summary>
        /// Handle the slot container being drawn event.
        /// </summary>
        protected virtual void HandleOnSlotContainerDraw()
        {
            if (m_OpenActionPanelOnContainerDraw == false && m_RefreshActionPanelOnContainerDraw == false) { return; }
            
            if (m_ItemViewSlotsContainer == null) {
                CloseItemAction(false);
                return;
            }
                
            var selectedSlot = m_ItemViewSlotsContainer.GetSelectedSlot();
            if (selectedSlot == null || selectedSlot.ItemInfo == ItemInfo.None) {
                CloseItemAction(false);
                return;
            }
            
            m_SelectedSlot = selectedSlot;
            m_SelectedItemInfo = selectedSlot.ItemInfo;

            if (m_OpenActionPanelOnContainerDraw) {
                OpenItemAction(m_SelectedSlot.ItemInfo, m_SelectedSlot.Index);
            }else if (m_RefreshActionPanelOnContainerDraw) {
                TryRefreshAndAssignItemActionsToItemActionPanel(m_SelectedSlot.ItemInfo, m_SelectedSlot.Index, out var slot);
            }
            
           
        }

        /// <summary>
        /// Create the Item Action List Slice.
        /// </summary>
        /// <returns>The List slice of item actions.</returns>
        protected abstract ListSlice<ItemAction> InitializeItemActionList();

        /// <summary>
        /// Refresh the item actions.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        protected abstract void RefreshItemActions(ItemInfo itemInfo);

        /// <summary>
        /// On bind.
        /// </summary>
        protected override void OnBindItemViewSlotContainer()
        {
            OnInventoryChanged(m_ItemViewSlotsContainer.Inventory);
            
            m_ItemViewSlotsContainer.OnBindInventory += OnInventoryChanged;
            m_ItemViewSlotsContainer.OnResetDraw += ResetDraw;
            m_ItemViewSlotsContainer.OnDraw += HandleOnSlotContainerDraw;
            m_ItemViewSlotsContainer.OnItemViewSlotClicked += HandleItemClicked;
            if (m_ActionPanel != null) {
                m_ActionPanel.OnAfterAnyItemActionInvoke += HandleItemActionInvokedFromPanel;
                m_ActionPanel.OnOpen += HandleActionPanelOpened;
                m_ActionPanel.OnClose += HandleActionPanelClosed;
            }

            HandleOnSlotContainerDraw();
        }

        /// <summary>
        /// Update the Item User when a new Inventory is set.
        /// </summary>
        /// <param name="newInventory"></param>
        protected virtual void OnInventoryChanged(Inventory newInventory)
        {
            if (m_AutoSetItemUser) {
                SetItemUser(m_ItemViewSlotsContainer.Inventory?.ItemUser);
            }
        }

        /// <summary>
        /// Reset the state of the Item Action binding.
        /// </summary>
        protected virtual void ResetDraw()
        {
            CloseItemAction(false);
        }

        /// <summary>
        /// On UnBind.
        /// </summary>
        protected override void OnUnbindItemViewSlotContainer()
        {
            m_ItemViewSlotsContainer.OnBindInventory -= OnInventoryChanged;
            m_ItemViewSlotsContainer.OnResetDraw -= ResetDraw;
            m_ItemViewSlotsContainer.OnDraw -= HandleOnSlotContainerDraw;
            m_ItemViewSlotsContainer.OnItemViewSlotClicked -= HandleItemClicked;
            if (m_ActionPanel != null) {
                m_ActionPanel.OnAfterAnyItemActionInvoke -= HandleItemActionInvokedFromPanel;
                m_ActionPanel.OnOpen -= HandleActionPanelOpened;
                m_ActionPanel.OnClose -= HandleActionPanelClosed;
            }
        }

        /// <summary>
        /// Set the item user.
        /// </summary>
        /// <param name="itemUser">The item user.</param>
        public virtual void SetItemUser(ItemUser itemUser)
        {
            m_ItemUser = itemUser;
            OnItemUserAssigned?.Invoke();
        }

        /// <summary>
        /// Handle the item clicked.
        /// </summary>
        /// <param name="eventdata">The slot event data.</param>
        protected virtual void HandleItemClicked(ItemViewSlotEventData eventdata)
        {
            if (m_UseItemOnClick == false) { return; }

            TriggerItemAction();
        }

        /// <summary>
        /// Handle the item action being invoked.
        /// </summary>
        /// <param name="itemActionIndex">The index of the item action.</param>
        protected virtual void HandleItemActionInvokedFromPanel(int itemActionIndex)
        {
            m_WaitingForActionFromPanel = false;
            NotifyItemActionInvoked(m_SelectedSlot, m_SelectedItemInfo, itemActionIndex);
        }
        
        protected virtual void HandleActionPanelClosed()
        {
            var itemViewSlot = m_SelectedSlot;
            if(itemViewSlot == null){ return; }

            if (m_WaitingForActionFromPanel) {
                m_WaitingForActionFromPanel = false;

                NotifyItemActionCanceled(itemViewSlot);
            }
            
            for (int i = 0; i < itemViewSlot.ItemView.Modules.Count; i++) {
                if (itemViewSlot.ItemView.Modules[i] is IItemActionBindingItemViewModule module) {
                    module.OnCloseItemActionPanel(this);
                }
            }
        }

        private void NotifyItemActionCanceled(ItemViewSlot itemViewSlot)
        {
            m_OnActionCanceled?.Invoke();

            for (int i = 0; i < itemViewSlot.ItemView.Modules.Count; i++) {
                if (itemViewSlot.ItemView.Modules[i] is IItemActionBindingItemViewModule module) {
                    module.OnCancelItemActionPanel(this);
                }
            }
        }

        protected virtual void HandleActionPanelOpened()
        {
            m_WaitingForActionFromPanel = true;
            var itemViewSlot = m_SelectedSlot;
            if(itemViewSlot == null){ return; }
            
            for (int i = 0; i < itemViewSlot.ItemView.Modules.Count; i++) {
                if (itemViewSlot.ItemView.Modules[i] is IItemActionBindingItemViewModule module) {
                    module.OnOpenItemActionPanel(this);
                }
            }
        }

        protected virtual void NotifyItemActionInvoked(ItemViewSlot itemViewSlot, ItemInfo itemInfo, int itemActionIndex)
        {
            m_OnActionInvoked?.Invoke();
            
            m_ItemViewSlotsContainer.ForceDraw();
            
            if(m_IfReturnSelectEmptyTrySelectSlot0 && itemViewSlot != null && itemViewSlot.Index != 0 && itemViewSlot.ItemInfo == ItemInfo.None) {
                m_ItemViewSlotsContainer.SelectSlot(0);
                m_ItemViewSlotsContainer.Draw();
                return;
            }
            
            if (itemActionIndex < 0 || itemActionIndex >= m_ItemActionListSlice.Count) { return; }

            var itemAction = m_ItemActionListSlice[itemActionIndex];
            
            for (int i = 0; i < itemViewSlot.ItemView.Modules.Count; i++) {
                if (itemViewSlot.ItemView.Modules[i] is IItemActionBindingItemViewModule module) {
                    module.OnItemActionInvoked(this, itemActionIndex, itemAction);
                }
            }
        }

        /// <summary>
        /// Trigger the item action for the selected slot..
        /// </summary>
        public virtual void TriggerItemAction()
        {
            TriggerItemAction(m_ItemViewSlotsContainer.GetSelectedSlot());
        }

        /// <summary>
        /// Trigger the item action on the slot of the index provided.
        /// </summary>
        /// <param name="slotIndex">The item slot index.</param>
        public virtual void TriggerItemAction(int slotIndex)
        {
            var slotCount = m_ItemViewSlotsContainer.GetItemViewSlotCount();
            if (slotIndex < 0 && slotIndex >= slotCount || slotIndex >= m_ItemViewSlotsContainer.ItemViewSlots.Count) {
                Debug.LogWarning("The slot index you are trying to use is out of range " + slotIndex + " / " + slotCount);
                return;
            }

            TriggerItemAction(m_ItemViewSlotsContainer.ItemViewSlots[slotIndex]);
        }

        /// <summary>
        /// Trigger the item action of the item view slot.
        /// </summary>
        /// <param name="itemViewSlot">The item view slot.</param>
        public virtual void TriggerItemAction(ItemViewSlot itemViewSlot)
        {
            if (itemViewSlot == null) { return; }

            m_SelectedSlot = itemViewSlot;
            m_SelectedItemInfo = itemViewSlot.ItemInfo;

            if (m_ActionPanel != null) {
                OpenItemAction(itemViewSlot.ItemInfo, itemViewSlot.Index);
                return;
            }

            if (m_UseItemActionIndex == -1) {
                UseAllItemActions(itemViewSlot.Index);
            } else {
                UseItemAction(itemViewSlot.Index, m_UseItemActionIndex);
            }
        }

        /// <summary>
        /// Can the item use the action.
        /// </summary>
        /// <param name="itemSlotIndex">The item slot index.</param>
        /// <returns>True if the item can use the action.</returns>
        public virtual bool CanItemUseAction(int itemSlotIndex)
        {
            if (itemSlotIndex < 0 && itemSlotIndex >= m_ItemViewSlotsContainer.GetItemViewSlotCount()) { return false; }

            var itemInfo = m_ItemViewSlotsContainer.ItemViewSlots[itemSlotIndex].ItemInfo;

            if (m_DisableActionOnEmptySlots && (itemInfo.Item == null || itemInfo.Amount <= 0)) { return false; }

            return true;
        }

        /// <summary>
        /// Use an item from the hot bar.
        /// </summary>
        /// <param name="itemSlotIndex">The item slot index of the item to use.</param>
        /// <param name="itemUser">The item user.</param>
        public virtual void UseAllItemActions(int itemSlotIndex)
        {
            if (CanItemUseAction(itemSlotIndex) == false) {
                return;
            }
            
            var itemInfo = m_ItemViewSlotsContainer.ItemViewSlots[itemSlotIndex].ItemInfo;
            RefreshItemActions(itemInfo);

            for (int i = 0; i < m_ItemActionListSlice.Count; i++) {
                InvokeActionInternal(itemSlotIndex, i);
            }
        }

        /// <summary>
        /// Use an item from the container.
        /// </summary>
        /// <param name="itemActionIndex">The item action index.</param>
        public virtual void UseItemActionOnSelectedSlot(int itemActionIndex)
        {
            var selectedSlot = m_ItemViewSlotsContainer.GetSelectedSlot();
            if (selectedSlot == null) { return; }

            UseItemAction(selectedSlot.Index, itemActionIndex);
        }
        
        /// <summary>
        /// Use an item from container.
        /// </summary>
        /// <param name="itemActionName">The item action name.</param>
        public virtual void UseItemActionOnSelectedSlot(string itemActionName)
        {
            var selectedSlot = m_ItemViewSlotsContainer.GetSelectedSlot();
            if (selectedSlot == null) { return; }

            UseItemAction(selectedSlot.Index, itemActionName);
        }

        /// <summary>
        /// Use an item from the hot bar.
        /// </summary>
        /// <param name="itemSlotIndex">The item slot index of the item to use.</param>
        /// <param name="itemActionIndex">The item action index.</param>
        public virtual void UseItemAction(int itemSlotIndex, int itemActionIndex)
        {
            if (CanItemUseAction(itemSlotIndex) == false) {
                return;
            }
            
            var itemInfo = m_ItemViewSlotsContainer.ItemViewSlots[itemSlotIndex].ItemInfo;
            RefreshItemActions(itemInfo);

            InvokeActionInternal(itemSlotIndex, itemActionIndex);
        }
        
        /// <summary>
        /// Use an item from the hot bar.
        /// </summary>
        /// <param name="itemSlotIndex">The item slot index of the item to use.</param>
        /// <param name="itemActionName">The item action name.</param>
        public virtual void UseItemAction(int itemSlotIndex, string itemActionName)
        {
            if (CanItemUseAction(itemSlotIndex) == false) {
                return;
            }
            
            var itemInfo = m_ItemViewSlotsContainer.ItemViewSlots[itemSlotIndex].ItemInfo;
            RefreshItemActions(itemInfo);

            var itemActionIndex = -1;
            for (int i = 0; i < m_ItemActionListSlice.Count; i++) {
                var itemAction = m_ItemActionListSlice[i];
                if(itemAction == null){ continue; }
                
                if (itemAction.Name == itemActionName) {
                    itemActionIndex = i;
                    break;
                }
            }

            InvokeActionInternal(itemSlotIndex, itemActionIndex);
        }

        /// <summary>
        /// Use an item from the hot bar.
        /// </summary>
        /// <param name="itemSlotIndex">The item slot index of the item to use.</param>
        /// <param name="itemActionIndex">The item action index.</param>
        protected virtual void InvokeActionInternal(int itemSlotIndex, int itemActionIndex)
        {
            if (itemActionIndex < 0 || itemActionIndex >= m_ItemActionListSlice.Count) { return; }

            var itemAction = m_ItemActionListSlice[itemActionIndex];

            if (itemAction == null) { return; }

            var itemViewSlot = m_ItemViewSlotsContainer.ItemViewSlots[itemSlotIndex];
            var itemInfo = itemViewSlot.ItemInfo;

            itemAction.Initialize(false);

            if (itemAction is IActionWithPanel actionWithPanel) {
                actionWithPanel.SetParentPanel(m_ItemViewSlotsContainer.Panel, itemViewSlot, m_ItemViewSlotsContainer.transform);
            }

            if (itemAction is IActionWithSlotContainer actionViewSlotsContainer) {
                actionViewSlotsContainer.SetViewSlotsContainer(m_ItemViewSlotsContainer, itemSlotIndex);
            }

            itemAction.InvokeAction(itemInfo, m_ItemUser);

            NotifyItemActionInvoked(itemViewSlot, itemInfo, itemActionIndex);
        }

        /// <summary>
        /// Open the item action panel.
        /// </summary>
        /// <param name="itemInfo">The item info selected.</param>
        /// <param name="index">The index.</param>
        protected virtual void OpenItemAction(ItemInfo itemInfo, int index)
        {
            if (TryRefreshAndAssignItemActionsToItemActionPanel(itemInfo, index, out var itemViewSlot) == false) {
                return;
            }

            m_ActionPanel.Open(m_ItemViewSlotsContainer.Panel, itemViewSlot);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemInfo"></param>
        /// <param name="index"></param>
        /// <param name="itemViewSlot"></param>
        /// <returns></returns>
        protected virtual bool TryRefreshAndAssignItemActionsToItemActionPanel(ItemInfo itemInfo, int index, out ItemViewSlot itemViewSlot)
        {
            itemViewSlot = m_ItemViewSlotsContainer.GetItemViewSlot(index);

            // Must refresh actions to check if the item has item actions.
            RefreshItemActions(itemInfo);

            if (CannotOpenItemActionPanel(itemInfo)) {
                NotifyCannotOpenPanel(itemViewSlot);

                return false;
            }

            m_ActionPanel.AssignActions(m_ItemActionListSlice, itemInfo, m_ItemUser, m_ItemViewSlotsContainer, index);

            if (m_ItemViewSlotsContainer.Panel == null) {
                Debug.LogWarning(
                    $"The ItemViewSlotContainer '{m_ItemViewSlotsContainer}' does not have a parent display panel.");
            }

            return true;
        }

        protected virtual void NotifyCannotOpenPanel(ItemViewSlot itemViewSlot)
        {
            m_OnCannotOpenPanel?.Invoke();

            for (int i = 0; i < itemViewSlot.ItemView.Modules.Count; i++) {
                if (itemViewSlot.ItemView.Modules[i] is IItemActionBindingItemViewModule module) {
                    module.OnCannotOpenItemActionPanel(this);
                }
            }
        }

        /// <summary>
        /// Can the item action panel be opened.
        /// </summary>
        /// <param name="itemInfo">The item info.</param>
        /// <returns>True.</returns>
        protected virtual bool CannotOpenItemActionPanel(ItemInfo itemInfo)
        {
            if (m_ActionPanel == null) { return true; }

            if (m_DisableActionOnEmptySlots && (itemInfo.Item == null || itemInfo.Amount <= 0)) { return true; }

            if (m_PreventOpenWhenNoAction && m_ItemActionListSlice.Count == 0) { return true;}

            return false;
        }

        /// <summary>
        /// Close the item action panel.
        /// </summary>
        public virtual void CloseItemAction(bool selectPrevious)
        {
            if (m_ActionPanel == null) { return; }

            m_ActionPanel.Close(selectPrevious);
        }
    }
}