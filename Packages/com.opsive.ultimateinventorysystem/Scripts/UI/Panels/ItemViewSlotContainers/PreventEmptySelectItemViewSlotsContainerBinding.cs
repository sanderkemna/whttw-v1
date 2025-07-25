namespace Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers
{
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.UI.Item;
    using UnityEngine;

    /// <summary>
    /// Bind the item description to an Item View Slot Container to show the description of the selected item.
    /// </summary>
    public class PreventEmptySelectItemViewSlotsContainerBinding : ItemViewSlotsContainerBinding
    {
        [Tooltip("Check if the slot is empty on select.")]
        [SerializeField] protected bool m_CheckOnSelect = false;
        [Tooltip("Check if the slot is empty on Click.")]
        [SerializeField] protected bool m_CheckOnClick = false;
        [Tooltip("This is the slot that will be selected if an empty ItemViewSlot is clicked.")]
        [SerializeField] protected int m_DefaultSlotIndex = 0;
        [Tooltip("Redraw the container on select.")]
        [SerializeField] protected bool m_RedrawOnSelect = false;

        private int m_PreviousSelectedSlotIndex = -1;
        
        /// <summary>
        /// On bind.
        /// </summary>
        protected override void OnBindItemViewSlotContainer()
        {
            m_ItemViewSlotsContainer.OnItemViewSlotSelected += HandleOnSelect;
            m_ItemViewSlotsContainer.OnItemViewSlotClicked += HandleOnClicked;
        }

        /// <summary>
        /// On unbind.
        /// </summary>
        protected override void OnUnbindItemViewSlotContainer()
        {
            m_ItemViewSlotsContainer.OnItemViewSlotSelected -= HandleOnSelect;
            m_ItemViewSlotsContainer.OnItemViewSlotClicked -= HandleOnClicked;
        }

        protected virtual void HandleOnSelect(ItemViewSlotEventData sloteventdata)
        {
            if(m_CheckOnSelect == false){ return; }
            SelectPreviousSlotIfSlotIsEmpty(sloteventdata);
        }

        protected virtual void HandleOnClicked(ItemViewSlotEventData sloteventdata)
        {
            if(m_CheckOnClick == false){ return; }
            SelectPreviousSlotIfSlotIsEmpty(sloteventdata);
        }

        private void SelectPreviousSlotIfSlotIsEmpty(ItemViewSlotEventData sloteventdata)
        {
            if (m_PreviousSelectedSlotIndex < 0) {
                m_PreviousSelectedSlotIndex = m_DefaultSlotIndex;
            }

            var selectedSlot = sloteventdata?.SlotIndex ?? m_ItemViewSlotsContainer.GetSelectedSlot()?.Index ?? -1;
            if ( sloteventdata == null || sloteventdata.ItemInfo == ItemInfo.None) {
                if (selectedSlot != m_PreviousSelectedSlotIndex && selectedSlot != m_DefaultSlotIndex) {

                    var previousSlot = m_ItemViewSlotsContainer.GetItemViewSlot(m_PreviousSelectedSlotIndex);
                    if (previousSlot == null) {
                        m_ItemViewSlotsContainer.SelectSlot(m_DefaultSlotIndex);
                        if (m_RedrawOnSelect) {
                            m_ItemViewSlotsContainer.ForceDraw();
                        }
                        return;
                    }
                    
                    m_ItemViewSlotsContainer.SelectSlot(m_PreviousSelectedSlotIndex);
                    if (m_RedrawOnSelect) {
                        m_ItemViewSlotsContainer.ForceDraw();
                    }
                    return;
                }
            }

            m_PreviousSelectedSlotIndex = sloteventdata.SlotIndex;
        }
    }
}