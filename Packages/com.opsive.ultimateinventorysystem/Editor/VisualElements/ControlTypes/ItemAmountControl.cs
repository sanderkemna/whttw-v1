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
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Storage;
    using System;
    using System.Reflection;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    /// <summary>
    /// The control type for an item amount.
    /// </summary>
    [ControlType(typeof(ItemAmount))]
    public class ItemAmountControl : TypeControlBase
    {
        protected InventorySystemDatabase m_Database;
        protected VisualElement m_ItemAmountFieldContainer;
        protected IntegerField m_IntegerField;
        protected ItemField m_ItemField;

        public override bool UseLabel => true;

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
            var arrayIndex = input.ArrayIndex;
            
            if (userData is object[] objArray) {
                for (int i = 0; i < objArray.Length; i++) {
                    if (objArray[i] is bool b) {
                        if (b == false) { return null; }
                    }
                    if (objArray[i] is InventorySystemDatabase database) { m_Database = database; }
                }
            } else if (userData is InventorySystemDatabase database) {
                m_Database = database;
            }

            var itemAmount = (ItemAmount)value;

            m_ItemAmountFieldContainer = new VisualElement();

            m_IntegerField = new IntegerField("Amount");
            m_IntegerField.SetValueWithoutNotify(itemAmount.Amount);
            m_IntegerField.RegisterValueChangedCallback(evt =>
            {
                if (!(onChangeEvent?.Invoke(new ItemAmount(m_ItemField.Value, m_IntegerField.value)) ?? false)) {
                    m_IntegerField.SetValueWithoutNotify(evt.newValue);
                }
            });
            m_ItemAmountFieldContainer.Add(m_IntegerField);

            m_ItemField = new ItemField(m_Database);
            m_ItemField.Refresh(itemAmount.Item);
            // Ensure the control is kept up to date as the value changes.
            if (field != null) {
                Action<object> onBindingUpdateEvent = (object newValue) =>
                {
                    var newItemAmount = (ItemAmount)newValue;
                    m_IntegerField.SetValueWithoutNotify(newItemAmount.Amount);
                    m_ItemField.Refresh(newItemAmount.Item);
                };
                m_ItemField.RegisterCallback<AttachToPanelEvent>(c =>
                {
                    BindingUpdater.AddBinding(field, arrayIndex, target, onBindingUpdateEvent);
                });
                m_ItemField.RegisterCallback<DetachFromPanelEvent>(c =>
                {
                    BindingUpdater.RemoveBinding(onBindingUpdateEvent);
                });
            }
            m_ItemField.OnValueChanged += () =>
            {
                if (!(onChangeEvent?.Invoke(new ItemAmount(m_ItemField.Value, m_IntegerField.value)) ?? false)) {
                    m_ItemField.Refresh(value as Item);
                }
            };
            m_ItemAmountFieldContainer.Add(m_ItemField);

            return m_ItemAmountFieldContainer;
        }
    }
}