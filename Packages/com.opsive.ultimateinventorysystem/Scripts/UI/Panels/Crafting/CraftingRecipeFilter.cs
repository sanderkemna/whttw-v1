namespace Opsive.UltimateInventorySystem.UI.Panels.Crafting
{
    using System.Collections.Generic;
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Crafting;
    using Opsive.UltimateInventorySystem.UI.Grid;
    using UnityEngine;

    public class CraftingRecipeFilter : CraftingRecipeFilterBase
    {
        [SerializeField] protected List<CraftingRecipe> m_ShowRecipes;
        [SerializeField] protected List<CraftingRecipe> m_HideRecipes;
        public override bool Filter(CraftingRecipe recipe)
        {
            var show = m_ShowRecipes.Count == 0;
            for(int i = 0; i < m_ShowRecipes.Count; i++)
            {
                if (m_ShowRecipes[i].ID != recipe.ID) { continue; }
                show = true;
                break;
            }
            if (show == false) { return false; }

            for (int i = 0; i < m_HideRecipes.Count; i++)
            {
                if (m_HideRecipes[i].ID == recipe.ID) { return false; }
            }

            return true;
        }
    }
    
    public abstract class CraftingRecipeFilterSorterBase : FilterSorter<CraftingRecipe>
    {
        public override string ToString()
        {
            return GetType().Name;
        }
    }

    public abstract class CraftingRecipeFilterBase : CraftingRecipeFilterSorterBase
    {
        public abstract bool Filter(CraftingRecipe recipe);

        public override ListSlice<CraftingRecipe> Filter(ListSlice<CraftingRecipe> input, ref CraftingRecipe[] outputPooledArray)
        {
            TypeUtility.ResizeIfNecessary(ref outputPooledArray, input.Count);

            var count = 0;
            for (int i = 0; i < input.Count; i++)
            {
                if (Filter(input[i]) == false) { continue; }

                outputPooledArray[count] = input[i];
                count++;
            }

            return (outputPooledArray, 0, count);
        }

        public override bool CanContain(CraftingRecipe input)
        {
            return Filter(input);
        }
    }
}