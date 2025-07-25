/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Panels.ActionPanels
{
    using Opsive.UltimateInventorySystem.UI.CompoundElements;
    using Opsive.UltimateInventorySystem.UI.Grid;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// A base class to create asynchronous actions in a panel.
    /// </summary>
    /// <typeparam name="T">The type of the action parameter.</typeparam>
    public abstract class AsyncFuncActionsPanel<T> : DisplayPanel
    {
        [Tooltip("The action button prefab. Requires an ActionButton component.")]
        [SerializeField] protected GameObject m_ActionButtonPrefab;
        [Tooltip("The parent for the buttons.")]
        [SerializeField] protected Transform m_ButtonParent;
        [Tooltip("Add a cancel option to close the panel.")]
        [SerializeField] protected bool m_AddCancelOption = true;
        [Tooltip("Select the first button on open")]
        [SerializeField] protected bool m_SelectFirstButtonOnOpen = true;

        protected List<ActionButton> m_ItemActionButtons = new List<ActionButton>();
        protected LayoutGroupNavigation m_LayoutNavigation;
        protected IList<AsyncFuncAction<T>> m_Actions;

        protected bool m_WaitForInput = true;
        protected T m_ValueToReturn;
        protected bool m_Canceled;

        public bool Canceled => m_Canceled;
        public bool AddCancelOption { get => m_AddCancelOption; set => m_AddCancelOption = value; }

        public T ValueToReturn => m_ValueToReturn;
        public List<ActionButton> ItemActionButtons => m_ItemActionButtons;
        public LayoutGroupNavigation LayoutNavigation => m_LayoutNavigation;
        public IList<AsyncFuncAction<T>> Actions => m_Actions;

        /// <summary>
        /// Wait for a return value before processing it.
        /// </summary>
        /// <returns>Returns the task.</returns>
        public virtual async Task<T> WaitForReturnedValueAsync()
        {
            while (m_WaitForInput) {

                if (m_IsOpen == false) {
                    return await Task.FromCanceled<T>(CancellationToken.None);
                }

                await Task.Yield();
            }

            return m_ValueToReturn;
        }

        /// <summary>
        /// Open the panel and setup the buttons.
        /// </summary>
        protected override void OpenInternal()
        {
            base.OpenInternal();

            if (m_ButtonParent == null) { m_ButtonParent = transform; }

            if (m_LayoutNavigation == null) {
                m_LayoutNavigation = m_ButtonParent.GetComponent<LayoutGroupNavigation>();
            }

            m_WaitForInput = true;
            
            
            var actionCount = m_Actions.Count;

            for (int i = 0; i < actionCount; i++) {
                var actionButton = AddButtonAtIndex(i);

                var localIndex = i;

                actionButton.SetButtonName(m_Actions[i].Name);
                actionButton.SetButtonAction(() => ButtonClicked(localIndex));
            }

            if (m_AddCancelOption) {
                var actionButton = AddButtonAtIndex(actionCount);

                actionButton.SetButtonName("Cancel");
                actionButton.SetButtonAction(() => ButtonClicked(-1));

                actionCount += 1;
            }

            for (int i = actionCount; i < m_ButtonParent.childCount; i++) {
                m_ButtonParent.GetChild(i).gameObject.SetActive(false);
            }

            if (m_SelectFirstButtonOnOpen && m_ItemActionButtons!= null && m_ItemActionButtons.Count > 0) {
                m_ItemActionButtons[0].Select();
            }

            if (m_LayoutNavigation != null) {
                m_LayoutNavigation.RefreshNavigation();
            } else {
                Debug.LogWarning("The action panel is missing a UILayoutGroupNavigation component on the buttons parent.");
            }
        }
        
        /// <summary>
        /// Add the button to a specific index.
        /// </summary>
        /// <param name="i">The index to add the button to.</param>
        /// <returns>The Action button. created.</returns>
        private ActionButton AddButtonAtIndex(int i)
        {
            if (m_ButtonParent.childCount <= i) { Instantiate(m_ActionButtonPrefab, m_ButtonParent); }

            if (m_ItemActionButtons.Count <= i) {
                m_ItemActionButtons.Add(m_ButtonParent.GetChild(i).GetComponent<ActionButton>());
            }

            m_ItemActionButtons[i].gameObject.SetActive(true);
            return m_ItemActionButtons[i];
        }

        /// <summary>
        /// Process a button being clicked.
        /// </summary>
        /// <param name="index">The index of the button clicked.</param>
        protected virtual void ButtonClicked(int index)
        {
            if (index < 0 || index >= m_Actions.Count) {
                m_ValueToReturn = GetCancelValue();
                m_Canceled = true;
            }else{
                m_ValueToReturn = m_Actions[index].Func.Invoke();
                m_Canceled = false;
            }
            Close();
            m_WaitForInput = false;
        }

        protected abstract T GetCancelValue();

        /// <summary>
        /// Assign the asynchronous actions to the buttons.
        /// </summary>
        /// <param name="funcActions">The list of actions.</param>
        public void AssignActions(IList<AsyncFuncAction<T>> funcActions)
        {
            m_Actions = funcActions;
        }
    }
}