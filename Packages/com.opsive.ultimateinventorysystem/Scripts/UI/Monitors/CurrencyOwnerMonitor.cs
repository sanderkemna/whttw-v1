/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Monitors
{
    using System;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Exchange;
    using Opsive.UltimateInventorySystem.UI.Currency;
    using Opsive.UltimateInventorySystem.UI.Panels;
    using UnityEngine;
    using EventHandler = Opsive.Shared.Events.EventHandler;

    /// <summary>
    /// A monitor component that listens to a currencyOwner events.
    /// </summary>
    public class CurrencyOwnerMonitor : MonoBehaviour
    {
        [Tooltip("The inventory ID to monitor, does not monitor if zero.")]
        [SerializeField] protected uint m_InventoryID = 1;
        [Tooltip("The currency owner.")]
        [SerializeField] protected CurrencyOwner m_CurrencyOwner;

        [Tooltip("The currency UI.")]
        [SerializeField] protected MultiCurrencyView m_MultiCurrencyView;

        protected bool m_IsRegisteredToCurrencyOwner;
        
        public CurrencyOwner CurrencyOwnerCurrency {
            get => m_CurrencyOwner;
            internal set => m_CurrencyOwner = value;
        }

        /// <summary>
        /// Initialize the component.
        /// </summary>
        protected virtual void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the event listener.
        /// </summary>
        protected virtual void Initialize()
        {
            EventHandler.RegisterEvent<GameObject, DisplayPanelManager>(EventNames.c_OnPanelOwnerChange_GameObjectPanelOwner_DisplayPanelManager, OnPanelOwnerChanged);
            if (m_MultiCurrencyView == null) {
                m_MultiCurrencyView = GetComponent<MultiCurrencyView>();
            }
            SetCurrencyOwner();
        }

        /// <summary>
        /// Set the currency owner.
        /// </summary>
        /// <param name="currencyOwner">The currency owner.</param>
        public void SetCurrencyOwner(CurrencyOwner currencyOwner)
        {
            if (m_CurrencyOwner == currencyOwner) { return; }
            RemoveEvent();
            m_CurrencyOwner = currencyOwner;
            SetCurrencyOwner();
        }
        
        /// <summary>
        /// Set the currencyOwner from the inventory id.
        /// </summary>
        private void SetCurrencyOwner()
        {
            RemoveEvent();
            
            if (m_CurrencyOwner == null) {
                if (m_InventoryID != 0) {
                    if (InventorySystemManager.InventoryIdentifierRegister.TryGetValue(m_InventoryID,
                            out var inventoryIdentifier)) { m_CurrencyOwner = inventoryIdentifier.CurrencyOwner; }
                }

                if (m_CurrencyOwner == null) {
                    Debug.LogWarning("The Currency Owner Monitor cannot find an  Currency Owner reference.", this);
                    return;
                }
            }

            EventHandler.RegisterEvent(m_CurrencyOwner, EventNames.c_CurrencyOwner_OnUpdate, CurrencyUpdate);
            m_IsRegisteredToCurrencyOwner = true;
            
            CurrencyUpdate(m_CurrencyOwner);
        }

        /// <summary>
        /// The panel owner changed.
        /// </summary>
        /// <param name="panelOwner">The new panel owner.</param>
        /// <param name="displayPanelManager">The display panel manager that changed.</param>
        private void OnPanelOwnerChanged(GameObject panelOwner, DisplayPanelManager displayPanelManager)
        {
            SetCurrencyOwner();
        }

        /// <summary>
        /// Remove an event listener.
        /// </summary>
        protected void RemoveEvent()
        {
            if (m_IsRegisteredToCurrencyOwner == false || m_CurrencyOwner == null) { return; }

            EventHandler.UnregisterEvent(m_CurrencyOwner, EventNames.c_CurrencyOwner_OnUpdate, CurrencyUpdate);
            m_IsRegisteredToCurrencyOwner = false;
        }

        /// <summary>
        /// Update the view when the currency changes.
        /// </summary>
        protected void CurrencyUpdate()
        {
            CurrencyUpdate(m_CurrencyOwner);
        }
        
        /// <summary>
        /// Update the view when the currency changes.
        /// </summary>
        /// <param name="currencyOwner">The currency owner.</param>
        protected void CurrencyUpdate(CurrencyOwnerBase currencyOwner)
        {
            var currencyOwnerCurrencyAmounts = currencyOwner as CurrencyOwner;

            if (currencyOwnerCurrencyAmounts == null) { return; }

            m_MultiCurrencyView.DrawCurrency(currencyOwnerCurrencyAmounts.CurrencyAmount);
        }

        /// <summary>
        /// Destroy.
        /// </summary>
        private void OnDestroy()
        {
            EventHandler.UnregisterEvent<GameObject, DisplayPanelManager>(EventNames.c_OnPanelOwnerChange_GameObjectPanelOwner_DisplayPanelManager, OnPanelOwnerChanged);
            if (m_CurrencyOwner != null) {
                EventHandler.UnregisterEvent(m_CurrencyOwner, EventNames.c_CurrencyOwner_OnUpdate, CurrencyUpdate);
            }
            
        }
    }
}