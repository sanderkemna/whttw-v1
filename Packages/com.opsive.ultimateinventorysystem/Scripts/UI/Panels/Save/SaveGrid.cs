/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Panels.Save
{
    using System;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.ItemActions;
    using Opsive.UltimateInventorySystem.SaveSystem;
    using Opsive.UltimateInventorySystem.UI.Grid;
    using Opsive.UltimateInventorySystem.UI.Panels.ActionPanels;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// The Grid UI used for the save menu.
    /// </summary>
    public class SaveGrid : GridGeneric<SaveDataInfo>
    {
        [Serializable]
        public struct OptionalSaveEvent
        {
            [Serializable]
            public class UnityIntEvent : UnityEvent<int> { }
            
            public bool UseOptionalEvent;
            public UnityIntEvent Event;
        }


        [Tooltip("The action panel.")]
        [SerializeField] protected SettableActionPanel m_ActionPanel;
        [Tooltip("The confirmation pop up.")]
        [SerializeField] protected ConfirmationPopUp m_ConfirmationPopUp;
        [Header("Enabling Optional events cancels the default function. If enabled you must add a call to the relevant Save, Load, Delete function available on this component.")]
        [Tooltip("Custom Save Function.")] 
        [SerializeField] protected OptionalSaveEvent m_OptionalSaveEvent;
        [Tooltip("Custom Load Function.")] 
        [SerializeField] protected OptionalSaveEvent m_OptionalLoadEvent;
        [Tooltip("Custom Delete Function.")] 
        [SerializeField] protected OptionalSaveEvent m_OptionalDeleteEvent;

        protected ResizableArray<SaveDataInfo> m_SaveDatas;
        protected SettableActionElement[] m_SettableActions;

        private int m_SelectedIndex;
        private SaveDataInfo m_SelectedSaveData;

        /// <summary>
        /// Initialize the components.
        /// </summary>
        public override void Initialize(bool force)
        {
            if (m_IsInitialized && force == false) { return; }

            base.Initialize(force);

            m_SaveDatas = new ResizableArray<SaveDataInfo>();

            m_SettableActions = new SettableActionElement[3];
            m_SettableActions[0] = new SettableActionElement("Save",
                () =>
                {
                    if (m_SelectedIndex > SaveSystemManager.MaxSaves) {
                        Debug.LogWarning($"The max saves data '{SaveSystemManager.MaxSaves}' is smaller than the selected index {m_SelectedIndex}. Please set a higher max saves amount.");
                        return;
                    }

                    if (m_SaveDatas[m_SelectedIndex].MetaData == null) {
                        if (m_OptionalSaveEvent.UseOptionalEvent) {
                            m_OptionalSaveEvent.Event.Invoke(m_SelectedIndex);
                        } else {
                            Save(m_SelectedIndex);
                        }
                        
                        Refresh();
                        return;
                    }

                    m_ConfirmationPopUp.SetTitle("Are you sure you want to overwrite this save file?");
                    m_ConfirmationPopUp.SetConfirmAction(() =>
                    {
                        if (m_OptionalSaveEvent.UseOptionalEvent) {
                            m_OptionalSaveEvent.Event.Invoke(m_SelectedIndex);
                        } else {
                            Save(m_SelectedIndex);
                        }
                        
                        Refresh();
                    });
                    m_ConfirmationPopUp.Open(m_ParentPanel, m_GridEventSystem.GetSelectedButton());

                }, () => true);
            m_SettableActions[1] = new SettableActionElement("Load",
                () =>
                {
                    m_ConfirmationPopUp.SetTitle("Are you sure you want to load this file?");
                    m_ConfirmationPopUp.SetConfirmAction(
                        () =>
                        {
                            if (m_OptionalLoadEvent.UseOptionalEvent) {
                                m_OptionalLoadEvent.Event.Invoke(m_SelectedIndex);
                            } else {
                                Load(m_SelectedIndex);
                            }
                            
                            Refresh();
                        });
                    m_ConfirmationPopUp.Open(m_ParentPanel, m_GridEventSystem.GetSelectedButton());
                }, () => m_SelectedSaveData.MetaData != null);
            m_SettableActions[2] = new SettableActionElement("Delete",
                () =>
                {
                    m_ConfirmationPopUp.SetTitle("Are you sure you want to delete this save file?");
                    m_ConfirmationPopUp.SetConfirmAction(
                        () =>
                        {
                            if (m_OptionalDeleteEvent.UseOptionalEvent) {
                                m_OptionalDeleteEvent.Event.Invoke(m_SelectedIndex);
                            } else {
                                Delete(m_SelectedIndex);
                            }
                            
                            Refresh();
                        });
                    m_ConfirmationPopUp.Open(m_ParentPanel, m_GridEventSystem.GetSelectedButton());
                }, () => m_SelectedSaveData.MetaData != null);

            m_ActionPanel.AssignActions(m_SettableActions);

            if (m_ActionPanel != null) {
                m_ActionPanel.Close();
            }

            OnElementClicked += OnSaveElementButtonClick;
            OnEmptyClicked += OnEmptyButtonClicked;
        }

        /// <summary>
        /// Save the game data in the save index.
        /// </summary>
        /// <param name="index">The index where to save the game data.</param>
        public virtual void Save(int index)
        {
            SaveSystemManager.Save(index);
        }
        
        /// <summary>
        /// Load the save data fom the index.
        /// </summary>
        /// <param name="index">The index from which to load the data.</param>
        public virtual void Load(int index)
        {
            SaveSystemManager.Load(index);
        }
        
        /// <summary>
        /// Delete the save file at the index.
        /// </summary>
        /// <param name="index">The index of the save file to delete.</param>
        public virtual void Delete(int index)
        {
            SaveSystemManager.DeleteSave(index);
        }

        /// <summary>
        /// Refresh the view.
        /// </summary>
        public override void Refresh()
        {
            m_SaveDatas.Clear();
            m_SaveDatas.AddRange(SaveSystemManager.GetSaves());
            if (m_SaveDatas.Count < SaveSystemManager.MaxSaves) {
                for (int i = m_SaveDatas.Count; i < SaveSystemManager.MaxSaves; i++) {
                    m_SaveDatas.Add(null);
                }
            }
            SetElements(m_SaveDatas);

            base.Refresh();
        }

        /// <summary>
        /// Click a button in the grid.
        /// </summary>
        /// <param name="saveDataInfo">The save data.</param>
        /// <param name="index">The index.</param>
        private void OnSaveElementButtonClick(SaveDataInfo saveDataInfo, int index)
        {
            m_SelectedIndex = index + m_StartIndex;
            m_SelectedSaveData = saveDataInfo;
            m_ActionPanel.Open(m_ParentPanel, GetButton(index));
        }

        /// <summary>
        /// Click an empty button in the grid.
        /// </summary>
        /// <param name="index">The index.</param>
        private void OnEmptyButtonClicked(int index)
        {
            m_SelectedIndex = index;
            m_SelectedSaveData = SaveDataInfo.None;
            m_ActionPanel.Open(m_ParentPanel, GetButton(index));
        }
    }
}
