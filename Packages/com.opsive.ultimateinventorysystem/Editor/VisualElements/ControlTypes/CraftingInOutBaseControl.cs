/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Editor.VisualElements.ControlTypes
{
    using Opsive.Shared.Editor.UIElements;
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.Shared.Editor.UIElements.Controls.Types;
    using Opsive.Shared.Editor.Managers;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Storage;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// The crafting input and output base control.
    /// </summary>
    public abstract class CraftingInOutBaseControl : TypeControlBase
    {
        protected VisualElement m_TabContent;
        protected Func<object, bool> m_OnChangeEvent;
        protected List<Func<VisualElement>> m_TabContents;
        protected VisualElement m_OtherContent;
        protected InventorySystemDatabase m_Database;

        protected abstract string[] TabNames { get; }
        protected List<Func<VisualElement>> TabContents => m_TabContents;

        public override bool UseLabel => false;

        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            var unityObject = input.UnityObject;
            var field = input.Field;
            var value = input.Value;
            var onChangeEvent = input.OnChangeEvent;
            var userData = input.UserData;
            var target = input.Target;
            
            if (userData is object[] objArray) {
                for (int i = 0; i < objArray.Length; i++) {
                    if (objArray[i] is bool b) {
                        if (b == false) { return null; }
                    }
                    if (objArray[i] is InventorySystemDatabase database) { m_Database = database; }
                }
            } else if (userData is bool b) {
                if (b == false) { return null; }
            }

            if (m_Database == null) {
                Debug.LogWarning("Database is null in custom control type.");
            }

            var container = new VisualElement();

            m_OnChangeEvent = onChangeEvent;

            container.name = "box";
            container.AddToClassList(ManagerStyles.BoxBackground);

            var tabToolbar = new TabToolbar(TabNames, 0, HandleSelection);
            container.Add(tabToolbar);

            m_TabContent = new VisualElement();
            m_OtherContent = new VisualElement();
            container.Add(m_TabContent);

            m_TabContents = new List<Func<VisualElement>>();
            m_TabContents.Add(() =>
            {
                m_OtherContent.Clear();
                FieldInspectorView.AddFields(unityObject,
                    target, MemberVisibility.Public, m_OtherContent, 
                    (object obj) =>
                    {
                        onChangeEvent?.Invoke(obj);
                    }, null, null, true, null, LabelControl.WidthAdjustment.None, null, new object[] { false, m_Database });
                return m_OtherContent;
            });

            tabToolbar.Selected = 0;
            HandleSelection(tabToolbar.Selected);

            return container;
        }

        /// <summary>
        /// Show the correct tab content.
        /// </summary>
        /// <param name="index">The tab index.</param>
        protected void HandleSelection(int index)
        {
            m_TabContent.Clear();
            if (index < 0 || index >= m_TabContents.Count) { return; }

            var newContent = TabContents[index].Invoke();
            m_TabContent.Add(newContent);
        }
    }
}