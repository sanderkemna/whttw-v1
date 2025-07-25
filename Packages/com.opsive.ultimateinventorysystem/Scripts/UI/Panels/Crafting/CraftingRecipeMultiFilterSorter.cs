namespace Opsive.UltimateInventorySystem.UI.Panels.Crafting
{
    using System.Collections.Generic;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Crafting;
    using UnityEngine;

    public class CraftingRecipeMultiFilterSorter : CraftingRecipeFilterSorterBase
    {
        [Tooltip("A list of frig filters and sorters.")]
        [SerializeField] internal List<CraftingRecipeFilterSorterBase> m_GridFilters;

        public List<CraftingRecipeFilterSorterBase> GridFilters => m_GridFilters;

        /// <summary>
        /// Filter the list of item infos.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="outputPooledArray">Reference to the output.</param>
        /// <returns>The list slice of filter.</returns>
        public override ListSlice<CraftingRecipe> Filter(ListSlice<CraftingRecipe> input, ref CraftingRecipe[] outputPooledArray)
        {
            var list = input;
            for (int i = 0; i < m_GridFilters.Count; i++) {
                if(m_GridFilters[i] == this){ continue; }
                list = m_GridFilters[i].Filter(list, ref outputPooledArray);
            }

            return list;
        }

        /// <summary>
        /// Can the input be contained.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>True if can be contained.</returns>
        public override bool CanContain(CraftingRecipe input)
        {
            for (int i = 0; i < m_GridFilters.Count; i++) {
                if(m_GridFilters[i] == this){ continue; }
                if (m_GridFilters[i].CanContain(input)) { continue; }

                return false;
            }

            return true;
        }
    }
}