/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Editor.VisualElements.ControlTypes
{
    using Opsive.Shared.Editor.UIElements.Controls.Types;
    using Opsive.UltimateInventorySystem.Editor.Managers;
    using Opsive.UltimateInventorySystem.Editor.Styles;
    using System;
    using System.Reflection;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    /// <summary>
    /// The base class for unity object controls a preview.
    /// </summary>
    public class UnityObjectControlWithPreview : UnityObjectControl
    {
        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            var field = input.Field;
            var value = input.Value;
            
            if (field != null) {
                return base.GetControl(input);
            }

            var obj = (Object)value;
            var objectView = new VisualElement();
            var objectPreview = new VisualElement();

            if (obj == null) { objectView.Add(new Label("None")); } else {
                objectView.Add(new Label(obj.name));
                ManagerUtility.ObjectPreview(objectPreview, obj);
                objectView.Add(objectPreview);
                objectView.AddToClassList(ControlTypeStyles.ObjectControlView);
            }

            return objectView;
        }
    }
}