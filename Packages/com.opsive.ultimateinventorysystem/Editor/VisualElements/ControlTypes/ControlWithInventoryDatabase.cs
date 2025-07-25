/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Editor.VisualElements.ControlTypes
{
    using Opsive.Shared.Editor.UIElements.Controls.Types;
    using Opsive.UltimateInventorySystem.Storage;
    using System;
    using System.Reflection;
    using Opsive.Shared.Editor.Managers;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Editor.Managers;
    using Opsive.UltimateInventorySystem.Editor.Styles;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    /// <summary>
    /// The base control for inventory object amounts.
    /// </summary>
    public abstract class ControlWithInventoryDatabase : TypeControlBase
    {
        protected InventorySystemDatabase m_Database;
        protected bool m_BoolUserData = true;

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
            
            ExtractUserDataAndUseCustomControl(input.UserData);

            var container = new VisualElement();
            container.styleSheets.Add(Shared.Editor.Utility.EditorUtility.LoadAsset<StyleSheet>("e70f56fae2d84394b861a2013cb384d0")); // Shared stylesheet.
            container.styleSheets.Add(CommonStyles.StyleSheet);
            container.styleSheets.Add(ManagerStyles.StyleSheet);
            container.styleSheets.Add(ControlTypeStyles.StyleSheet);
            container.styleSheets.Add(InventoryManagerStyles.StyleSheet);
            container.styleSheets.Add(InventoryManagerStyles.StyleSheet);
            
            //TODO this used to be field == null, but it was limiting bindings, so I changed it to value ==null, please double check there aren't any issues
            //TODO There is an issue because null Scriptable objects appeared as "NULL" instead of a field, so Completely commenting out that part
            /*if (value == null) {
                var text = value == null ? "NULL" : value.ToString();
                container.Add(new Label(text));
                return container;
            }*/

            if (m_Database == null) {
                Debug.LogWarning($"The Type Control {GetType().Name} requires the Inventory Database to work properly. But it is missing for object {unityObject}, please contact Opsive.");
            }

            var visualElement = CreateCustomControlVisualElement(value, onChangeEvent, field);
            container.Add(visualElement);

            return container;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userData"></param>
        /// <returns>Returns false if the user Data says not to use the custom control.</returns>
        protected virtual void ExtractUserDataAndUseCustomControl(object userData)
        {
            if (userData is object[] objArray) {
                for (int i = 0; i < objArray.Length; i++) {
                    if (objArray[i] is bool b) {
                        if (b == false) {
                            m_BoolUserData = false;
                        }
                    }

                    if (objArray[i] is InventorySystemDatabase database) {
                        m_Database = database;
                    }
                }
            } else if (userData is InventorySystemDatabase database) {
                m_Database = database;
            }

            if (m_Database == null) {
                if (Application.isPlaying == false || InventorySystemManager.IsNull) {
                   
#if UNITY_2023_1_OR_NEWER
                     m_Database = Object.FindFirstObjectByType<InventorySystemManager>()?.Database;
#else
                    m_Database = Object.FindObjectOfType<InventorySystemManager>()?.Database;
#endif
                } else {
                    m_Database = InventorySystemManager.Instance.Database;
                }
            
                if (m_Database == null) {
                    if (Application.isPlaying == false) {
                        m_Database = InventoryMainWindow.InventorySystemDatabase;
                    }
                }
            }
        }

        /// <summary>
        /// Creates an ObjectAmountView for the correct object type.
        /// </summary>
        /// <param name="value">The start value.</param>
        /// <param name="onChangeEvent">The onChangeEvent.</param>
        /// <param name="field">The field info</param>
        /// <returns>The new ObjectAmountsView.</returns>
        public abstract VisualElement CreateCustomControlVisualElement(object value, Func<object, bool> onChangeEvent,
            FieldInfo field);
    }
}