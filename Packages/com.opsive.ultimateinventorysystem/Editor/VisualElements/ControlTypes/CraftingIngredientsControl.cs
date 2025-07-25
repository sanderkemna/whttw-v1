/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Editor.VisualElements.ControlTypes
{
    using Opsive.Shared.Editor.UIElements.Controls;
    using Opsive.UltimateInventorySystem.Crafting;
    using Opsive.UltimateInventorySystem.Crafting.IngredientsTypes;
    using System;
    using System.Reflection;
    using UnityEngine.UIElements;

    /// <summary>
    /// The control for crafting ingredients.
    /// </summary>
    [ControlType(typeof(CraftingIngredients))]
    public class CraftingIngredientsControl : CraftingInOutBaseControl
    {
        protected CraftingIngredients m_Ingredients;

        protected override string[] TabNames => new string[]
        {
            "Item Categories",
            "Item Definitions",
            "Items",
            "Others"
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

            m_Ingredients = value as CraftingIngredients;

            m_TabContents.Insert(m_TabContents.Count - 1, () => new ItemCategoryAmountsView(
                  m_Ingredients.ItemCategoryAmounts, m_Database,
                  (x) =>
                  {
                      m_OnChangeEvent?.Invoke(x);
                      return true;
                  }));

            m_TabContents.Insert(m_TabContents.Count - 1, () => new ItemDefinitionAmountsView(
                  m_Ingredients.ItemDefinitionAmounts, m_Database,
                  (x) =>
                  {
                      m_OnChangeEvent?.Invoke(x);
                      return true;
                  }));

            m_TabContents.Insert(m_TabContents.Count - 1, () => new ItemAmountsView(
                  m_Ingredients.ItemAmounts, m_Database,
                  (x) =>
                  {
                      m_OnChangeEvent?.Invoke(x);
                      return true;
                  }));

            HandleSelection(0);
            return container;
        }
    }

    /// <summary>
    /// The control for crafting ingredients with currency.
    /// </summary>
    [ControlType(typeof(CraftingIngredientsWithCurrency))]
    public class CraftingIngredientsWithCurrencyControl : CraftingIngredientsControl
    {

        protected override string[] TabNames => new string[]
        {
            "Item Categories",
            "Item Definitions",
            "Items",
            "Currencies",
            "Others"
        };

        /// <summary>
        /// Returns the control that should be used for the specified ControlType.
        /// </summary>
        /// <param name="input">The input to the control.</param>
        /// <returns>The created control.</returns>
        protected override VisualElement GetControl(TypeControlInput input)
        {
            var container = base.GetControl(input);
            if (container == null) { return null; }

            var ingredients = m_Ingredients as CraftingIngredientsWithCurrency;
            if (ingredients == null) {
                return container;
            }

            m_TabContents.Insert(m_TabContents.Count - 1, () => new CurrencyAmountsView(
                  ingredients.CurrencyAmounts, m_Database,
                  (x) =>
                  {
                      m_OnChangeEvent?.Invoke(x);
                      return true;
                  }));

            HandleSelection(0);
            return container;
        }
    }
}