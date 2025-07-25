namespace Opsive.UltimateInventorySystem.UI.Item.ItemViewModules
{
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Item View component that shows if an item is part of an Item View Slot Container.
    /// </summary>
    public class PartOfOtherContainerItemView : ItemViewModule
    {
        [Tooltip("The Display Panel Manager ID containing the panel to search for.")]
        [SerializeField] protected uint m_DisplayPanelManagerID = 1;
        [Tooltip("The name of the other Item View Slot Container Panel.")]
        [SerializeField] protected string m_OtherContainerPanelName = "ItemHotbar";
        [Tooltip("The objects that will be set active if the item is part of that other container.")]
        [SerializeField] protected GameObject[] m_ActiveIfContained;
        [Tooltip("The objects that will be set inactive if the item is part of that other container.")]
        [SerializeField] protected GameObject[] m_DisabledIfContained;

        protected ItemViewSlotsContainerBase m_OtherItemViewSlotsContainer;

        public string OtherContainerPanelName
        {
            get => m_OtherContainerPanelName;
            set => m_OtherContainerPanelName = value;
        }

        public GameObject[] ActiveIfEquipped { get => m_ActiveIfContained; set => m_ActiveIfContained = value; }
        public GameObject[] DisabledIfContained { get => m_DisabledIfContained; set => m_DisabledIfContained = value; }

        public ItemViewSlotsContainerBase OtherItemViewSlotsContainer
        {
            get => m_OtherItemViewSlotsContainer;
            set => m_OtherItemViewSlotsContainer = value;
        }
        
        /// <summary>
        /// Set the value.
        /// </summary>
        /// <param name="info">The item info.</param>
        public override void SetValue(ItemInfo info)
        {
            if (info.Item == null || info.Item.IsInitialized == false) {
                Clear();
                return;
            }

            if (TryGetOtherItemViewSlotsContainer(out var otherItemViewSlotContainer) == false) {
                Clear();
                return;
            }

            if (!otherItemViewSlotContainer.ContainsItem(info)) {
                Clear();
                return;
            }

            EnableGameObjects(m_ActiveIfContained, true);
            EnableGameObjects(m_DisabledIfContained, false);
        }

        protected virtual bool TryGetOtherItemViewSlotsContainer(out ItemViewSlotsContainerBase itemViewSlotsContainer)
        {
            if (m_OtherItemViewSlotsContainer != null) {
                itemViewSlotsContainer = m_OtherItemViewSlotsContainer;
                return true;
            }

            itemViewSlotsContainer = InventorySystemManager.GetItemViewSlotContainer<ItemViewSlotsContainerBase>(
                m_DisplayPanelManagerID, m_OtherContainerPanelName);
            if (itemViewSlotsContainer != null) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Enable or disable the game objects.
        /// </summary>
        /// <param name="gameObjects">The gameobjects to enable or disable.</param>
        /// <param name="enable">Enable or Disable the Gameobjects?</param>
        protected void EnableGameObjects(GameObject[] gameObjects, bool enable)
        {
            if(gameObjects == null){ return; }

            for (int i = 0; i < gameObjects.Length; i++) {
                var gameObject = gameObjects[i];
                if(gameObject == null || gameObject.activeSelf == enable){ continue; }
                gameObject.SetActive(enable);
            }
        }

        /// <summary>
        /// Clear the value.
        /// </summary>
        public override void Clear()
        {
            EnableGameObjects(m_ActiveIfContained, false);
            EnableGameObjects(m_DisabledIfContained, true);
        }
    }
}