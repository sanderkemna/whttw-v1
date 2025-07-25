/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Editor.VisualElements.ControlTypes
{
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.UltimateInventorySystem.Crafting;
    using System;
    using System.Reflection;
    using UnityEngine.UIElements;

    /// <summary>
    /// The control for crafting output control.
    /// </summary>
    [ControlType(typeof(CraftingOutput))]
    public class CraftingOutputControl : CraftingInOutBaseControl
    {
        protected CraftingOutput m_Output;

        protected override string[] TabNames => new string[]
        {
            "Outputs",
            "Other"
        };

        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            var value = input.Value;
            
            var container = base.GetControl(input);
            if (container == null) { return null; }

            m_Output = value as CraftingOutput;

            m_TabContents.Insert(m_TabContents.Count - 1, () =>
              {
                  return new ItemAmountsView(
                      m_Output.ItemAmounts, m_Database,
                      (x) =>
                      {
                          m_OnChangeEvent?.Invoke(x);
                          return true;
                      });
              });

            HandleSelection(0);
            return container;
        }
    }
}