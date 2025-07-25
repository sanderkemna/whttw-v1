/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.UI.Menus.Crafting
{
    using System;
    using System.Collections.Generic;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Crafting;
    using Opsive.UltimateInventorySystem.UI.Grid;
    using Opsive.UltimateInventorySystem.UI.Panels;
    using Opsive.UltimateInventorySystem.UI.Panels.Crafting;
    using Opsive.UltimateInventorySystem.UI.Panels.ItemViewSlotContainers;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable]
    public class BoolUnityEvent : UnityEvent<bool> { }

    public abstract class CraftingMenuBase : InventoryPanelBinding
    {
        [Tooltip("The crafter has the list of available recipes to craft.")]
        [SerializeField] protected internal Crafter m_Crafter;
        [Tooltip("The recipes display.")]
        [SerializeField] protected internal CraftingRecipeGrid m_CraftingRecipeGrid;
        [Tooltip("The selected recipe panel.")]
        [SerializeField] protected internal RecipePanel m_RecipePanel;
        [Tooltip("An Event called when the crafting is done.")]
        [SerializeField] protected bool m_BindFilterSortersToGrid = true;
        [Tooltip("An Event called when the crafting is done.")]
        [SerializeField] protected bool m_UseGridFilterSorter = true;
        [Tooltip("An Event called when the crafting is done.")]
        [SerializeField] protected bool m_UseTabControlTabData = true;
        [Tooltip("The selected recipe panel.")]
        [SerializeField] protected CraftingRecipeFilterSorterBase[] m_FiltersAndSorters;
        [Tooltip("Draw recipes on open.")]
        [SerializeField] protected bool m_DrawRecipesOnOpen = true;
        [Tooltip("An Event called when the crafting is done.")]
        [SerializeField] protected BoolUnityEvent m_OnCraftComplete;
        
        protected CraftingRecipe m_SelectedRecipe;

        public Crafter Crafter => m_Crafter;
        public CraftingRecipe SelectedRecipe => m_SelectedRecipe;

        protected MultiCraftingRecipeFilterSorter m_MultiFilterSorters = new MultiCraftingRecipeFilterSorter();
        
        public MultiCraftingRecipeFilterSorter MultiFilterSorters => m_MultiFilterSorters;

        public override void Initialize(DisplayPanel display, bool force)
        {
            var wasInitialized = m_IsInitialized;
            if (wasInitialized && !force) { return; }
            base.Initialize(display, force);

            if (wasInitialized == false) {
                //Only do it once even if forced.
                if (m_Inventory == null) {
                    m_Inventory = GameObject.FindWithTag("Player")?.GetComponent<Inventory>();
                }

                if (m_Crafter != null) { m_Crafter.Initialize(false); }
                
                m_CraftingRecipeGrid.SetParentPanel(m_DisplayPanel);
                m_CraftingRecipeGrid.Initialize(false);

                m_CraftingRecipeGrid.OnElementSelected += CraftingRecipeSelected;
                m_CraftingRecipeGrid.OnEmptySelected += (x) => CraftingRecipeSelected(null, x);
                m_CraftingRecipeGrid.OnElementClicked += CraftingRecipeClicked;

                for (int i = 0; i < m_FiltersAndSorters.Length; i++) {
                    m_MultiFilterSorters.GridFilters.Add(m_FiltersAndSorters[i]);
                }

                if (m_UseGridFilterSorter) {
                    m_MultiFilterSorters.GridFilters.Add(m_CraftingRecipeGrid.FilterSorter);
                }

                if (m_BindFilterSortersToGrid) {
                    m_CraftingRecipeGrid.BindGridFilterSorter(m_MultiFilterSorters);
                }

                if (m_UseTabControlTabData) {
                    var tabControl = m_CraftingRecipeGrid.TabControl;

                    if (tabControl != null) {
                        tabControl.Initialize(false);
                        tabControl.OnTabChange += HandleTabChange;

                        for (int i = 0; i < tabControl.TabCount; i++) {
                            var tab = tabControl.TabToggles[i];
                            var craftingTabData = tab.GetComponent<CraftingTabData>();
                            if (craftingTabData != null) {
                                craftingTabData.Initialize(false);
                            }
                        }

                        HandleTabChange(-1, tabControl.TabIndex, false);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the crafter.
        /// </summary>
        /// <param name="crafter">The crafter to set.</param>
        public virtual void SetCrafter(Crafter crafter)
        {
            m_Crafter = crafter;
            m_Crafter.Initialize(false);
            DrawRecipes();
        }

        /// <summary>
        /// Refresh the display.
        /// </summary>
        public virtual void DrawRecipes()
        {
            m_CraftingRecipeGrid.SetElements(m_Crafter.GetRecipes());
            m_CraftingRecipeGrid.Draw();
        }
        
        /// <summary>
        /// Set the inventory.
        /// </summary>
        protected override void OnInventoryBound()
        {
            m_RecipePanel.SetInventory(m_Inventory);
        }

        /// <summary>
        /// Handle the On Open event.
        /// </summary>
        public override void OnOpen()
        {
            base.OnOpen();

            m_RecipePanel.SetInventory(m_Inventory);

            if (m_DrawRecipesOnOpen) {
                var tabControl = m_CraftingRecipeGrid.TabControl;
                if (tabControl != null) {
                    HandleTabChange(-1, tabControl.TabIndex, true);
                } else {
                    DrawRecipes();
                }
            }

            m_CraftingRecipeGrid.SelectButton(0);
        }

        /// <summary>
        /// Handle a tab change.
        /// </summary>
        /// <param name="previousIndex">The previous tab index.</param>
        /// <param name="newIndex">The new tab index.</param>
        private void HandleTabChange(int previousIndex, int newIndex)
        {
            HandleTabChange(previousIndex, newIndex, true);
        }

        /// <summary>
        /// Handle the tab change.
        /// </summary>
        /// <param name="previousIndex">The previous tab index.</param>
        /// <param name="newIndex">The new tab index.</param>
        /// <param name="draw">Should the recipes be drawn?</param>
        protected virtual void HandleTabChange(int previousIndex, int newIndex, bool draw)
        {
            if (previousIndex == newIndex) { return; }

            var tabToggles = m_CraftingRecipeGrid.TabControl.TabToggles;

            if (previousIndex >= 0 && previousIndex < tabToggles.Count) {
                var previousTabData = tabToggles[previousIndex]?.GetComponent<CraftingTabData>();
                if (previousTabData != null && previousTabData.CraftingFilter != null) {
                    m_MultiFilterSorters.GridFilters.Remove(previousTabData.CraftingFilter);
                }
            }

            var craftingTabData = m_CraftingRecipeGrid.TabControl.CurrentTab.GetComponent<CraftingTabData>();

            if (craftingTabData == null) {
                Debug.LogWarning("The selected tab is either null or does not have an CraftingTabData", gameObject);
                return;
            }

            if (craftingTabData.CraftingFilter != null && m_MultiFilterSorters.GridFilters.Contains(craftingTabData.CraftingFilter) == false) {
                m_MultiFilterSorters.GridFilters.Add(craftingTabData.CraftingFilter);
            }

            if (draw) {
                DrawRecipes();
            }
        }

        /// <summary>
        /// update when the crafting amount changes.
        /// </summary>
        /// <param name="amount">The new amount.</param>
        public virtual void CraftingAmountChanged(int amount)
        {
            m_RecipePanel.SetQuantity(amount);
            m_RecipePanel.Refresh();
        }

        /// <summary>
        /// A recipe is selected.
        /// </summary>
        /// <param name="recipe">The recipe.</param>
        /// <param name="index">The index.</param>
        public virtual void CraftingRecipeSelected(CraftingRecipe recipe, int index)
        {
            m_RecipePanel.SetRecipe(recipe);

            if (m_SelectedRecipe == recipe) { return; }

            m_SelectedRecipe = recipe;
            CraftingAmountChanged(1);
        }

        /// <summary>
        /// Recipe is clicked.
        /// </summary>
        /// <param name="recipe">The recipe.</param>
        /// <param name="index">The index.</param>
        public virtual void CraftingRecipeClicked(CraftingRecipe recipe, int index)
        {
            m_RecipePanel.SetRecipe(recipe);

            if (m_SelectedRecipe == recipe) { return; }

            m_SelectedRecipe = recipe;
            CraftingAmountChanged(1);
        }

        /// <summary>
        /// Wait for the player to select a quantity.
        /// </summary>
        /// <returns>The task.</returns>
        public virtual void CraftSelectedQuantity()
        {
            var quantity = m_RecipePanel.Quantity;

            DoCraft(quantity);
        }

        /// <summary>
        /// Do craft the item.
        /// </summary>
        /// <param name="quantity">The quantity to craft.</param>
        public virtual void DoCraft(int quantity)
        {
            if (quantity >= 1) {
                var result = m_Crafter.Processor.Craft(m_SelectedRecipe, m_Inventory, quantity);
                OnCraftComplete(result, m_SelectedRecipe, m_Inventory, quantity);
            }

            DrawRecipes();
            m_RecipePanel.SetQuantity(1);
            m_RecipePanel.Refresh();
        }

        /// <summary>
        /// When the Craft has been complete send an event.
        /// </summary>
        /// <param name="result">The crafting Result.</param>
        /// <param name="selectedRecipe">The selected Recipe.</param>
        /// <param name="inventory">The Inventory where the item was added.</param>
        /// <param name="quantity">The quantity crafted.</param>
        public virtual void OnCraftComplete(CraftingResult result, CraftingRecipe selectedRecipe, Inventory inventory, int quantity)
        {
            m_OnCraftComplete.Invoke(result.Success);
        }
        
        [Serializable]
        public class MultiCraftingRecipeFilterSorter : IFilterSorter<CraftingRecipe>
        {
            [Tooltip("A list of frig filters and sorters.")]
            [SerializeField] protected List<IFilterSorter<CraftingRecipe>> m_GridFilters = new List<IFilterSorter<CraftingRecipe>>();

            public List<IFilterSorter<CraftingRecipe>> GridFilters => m_GridFilters;

            /// <summary>
            /// Filter the list of item infos.
            /// </summary>
            /// <param name="input">The input.</param>
            /// <param name="outputPooledArray">Reference to the output.</param>
            /// <returns>The list slice of filter.</returns>
            public ListSlice<CraftingRecipe> Filter(ListSlice<CraftingRecipe> input, ref CraftingRecipe[] outputPooledArray)
            {
                var list = input;
                for (int i = 0; i < m_GridFilters.Count; i++) {
                    if(m_GridFilters[i] == null || m_GridFilters[i] == this){ continue; }
                    list = m_GridFilters[i].Filter(list, ref outputPooledArray);
                }

                return list;
            }
        }
    }
}