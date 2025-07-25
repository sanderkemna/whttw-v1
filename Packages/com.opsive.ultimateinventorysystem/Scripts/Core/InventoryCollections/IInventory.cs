/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Core.InventoryCollections
{
    using Opsive.Shared.Utility;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Exchange;
    using Opsive.UltimateInventorySystem.ItemActions;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface for the inventory.
    /// </summary>
    public interface IInventory
    {
        /// <summary>
        /// Get the game object attached to the inventory.
        /// </summary>
        GameObject gameObject { get; }

        /// <summary>
        /// Get the item user attached to the inventory.
        /// </summary>
        ItemUser ItemUser { get; }

        /// <summary>
        /// Get the main itemCollection.
        /// </summary>
        ItemCollection MainItemCollection { get; }

        /// <summary>
        /// Get the currency owner.
        /// </summary>
        /// <typeparam name="T">The currency owner type.</typeparam>
        /// <returns>The currency owner found.</returns>
        ICurrencyOwner<T> GetCurrencyComponent<T>();

        /// <summary>
        /// The item Collections.
        /// </summary>
        IReadOnlyList<ItemCollection> ItemCollectionsReadOnly { get; }

        /// <summary>
        /// Can the item be added to the item
        /// </summary>
        /// <param name="itemInfo">The item info to add.</param>
        /// <param name="receivingCollection">The receiving item Collection.</param>
        /// <returns>The itemInfo that can be added (can be null).</returns>
        ItemInfo? CanAddItem(ItemInfo itemInfo, ItemCollection receivingCollection);

        /// <summary>
        /// Can the item be removed from the itemCollection.
        /// </summary>
        /// <param name="itemInfo">The item info to remove (contains the itemCollection).</param>
        /// <returns>The item info that can be removed (can be null).</returns>
        ItemInfo? CanRemoveItem(ItemInfo itemInfo);

        /// <summary>
        /// Add an item to the inventory.
        /// </summary>
        /// <param name="itemInfo">The amount of item being added.</param>
        /// <param name="stackTarget">The item stack where the item should be added.</param>
        /// <returns>Returns the number of items added, 0 if no item was added.</returns>
        ItemInfo AddItem(ItemInfo itemInfo, ItemStack stackTarget = null);

        /// <summary>
        /// Remove an item from the inventory.
        /// </summary>
        /// <param name="itemInfo">The amount of item to remove.</param>
        /// <returns>Returns true if the item was removed correctly.</returns>
        ItemInfo RemoveItem(ItemInfo itemInfo);

        /// <summary>
        /// Get the item collection with the ID specified.
        /// </summary>
        /// <param name="itemCollectionID">The item Collection ID.</param>
        /// <returns>The item collection.</returns>
        ItemCollection GetItemCollection(ItemCollectionID itemCollectionID);

        /// <summary>
        /// Get the first item info for the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The itemInfo.</returns>
        ItemInfo? GetItemInfo(Item item);

        /// <summary>
        /// Returns the first itemInfo that matches the description.
        /// </summary>
        /// <param name="itemDefinition">The Item Definition to retrieve the amount of.</param>
        /// <param name="checkInherently">Take into account the children of the item definition.</param>
        /// <returns>The first item info found in the inventory.</returns>
        ItemInfo? GetItemInfo(ItemDefinition itemDefinition, bool checkInherently = false);

        /// <summary>
        /// Returns the first itemInfo that matches the description.
        /// </summary>
        /// <param name="itemCategory">The Item Category to retrieve the amount of.</param>
        /// <param name="checkInherently">Take into account the children of the item category.</param>
        /// <returns>The first item info found in the inventory.</returns>
        ItemInfo? GetItemInfo(ItemCategory itemCategory, bool checkInherently = false);

        /// <summary>
        /// Get a filtered and sorted set of item infos in the inventory.
        /// </summary>
        /// <param name="itemInfos">The array holding the resulting itemInfos.</param>
        /// <param name="filterParam">The filter parameter.</param>
        /// <param name="filterFunc">The filter function.</param>
        /// <param name="startIndex">The start index for the result.</param>
        /// <typeparam name="T">The filter parameter type.</typeparam>
        /// <returns>The list slice of the item infos.</returns>
        ListSlice<ItemInfo> GetItemInfos<T>(ref ItemInfo[] itemInfos, T filterParam,
            Func<ItemInfo, T, bool> filterFunc, int startIndex = 0);

        /// <summary>
        /// Get a filtered and sorted set of item infos in the inventory.
        /// </summary>
        /// <param name="itemInfos">The array holding the resulting itemInfos.</param>
        /// <param name="filterParam">The filter parameter.</param>
        /// <param name="filterFunc">The filter function.</param>
        /// <param name="sortComparer">The sort comparer.</param>
        /// <param name="startIndex">The start index for the result.</param>
        /// <typeparam name="T">The filter parameter type.</typeparam>
        /// <returns>The list slice of the item infos.</returns>
        ListSlice<ItemInfo> GetItemInfos<T>(ref ItemInfo[] itemInfos, T filterParam,
            Func<ItemInfo, T, bool> filterFunc, Comparer<ItemInfo> sortComparer, int startIndex = 0);

        /// <summary>
        /// Does the inventory have this list of items.
        /// </summary>
        /// <param name="itemList">The item list.</param>
        /// <returns>Returns true if the inventory has all the items in the list.</returns>
        bool HasItemList(ListSlice<ItemInfo> itemList);

        /// <summary>
        /// Determines if the Item Collection contains the item.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <param name="similarItem">Find an itemInfo with that value equivalent item or just one similar?</param>
        /// <returns>Returns true if the amount of items in the collection is equal or bigger than the amount specified.</returns>
        bool HasItem(Item item, bool similarItem = true);

        /// <summary>
        /// Determines if the Item Collection contains the item.
        /// </summary>
        /// <param name="itemAmount">The item to check.</param>
        /// <param name="similarItem">Find an itemInfo with that value equivalent item or just one similar?</param>
        /// <returns>Returns true if the amount of items in the collection is equal or bigger than the amount specified.</returns>
        bool HasItem(ItemAmount itemAmount, bool similarItem = true);

        /// <summary>
        /// Check if the inventory has at least the item amount specified.
        /// </summary>
        /// <param name="itemInfo">The item info to check.</param>
        /// <param name="similarItem">Find an itemInfo with that value equivalent item or just one similar?</param>
        /// <returns>True if the inventory has at least that amount.</returns>
        bool HasItem(ItemInfo itemInfo, bool similarItem = true);

        /// <summary>
        /// Check if the inventory has at least the item amount specified.
        /// </summary>
        /// <param name="itemDefinitionAmount">The item info to check.</param>
        /// <returns>True if the inventory has at least that amount.</returns>
        bool HasItem(ItemDefinitionAmount itemDefinitionAmount);

        /// <summary>
        /// Determines if the Item Collection contains an item with the Item Definition.
        /// </summary>
        /// <param name="itemDefinition">The itemDefinition of the item to check.</param>
        /// <param name="checkInherently">Take into account the children of the Item Definition.</param>
        /// <param name="countStacks">If true count the stacks number not the stacks amounts.</param>
        /// <returns>Returns true if the amount of items with the Item Definition in the collection is equal or bigger than the amount specified.</returns>
        bool HasItem(ItemDefinitionAmount itemDefinitionAmount, bool checkInherently,
            bool countStacks = false);

        /// <summary>
        /// Determines if the Item Collection contains an item with the exact same category provided (does NOT check the category children).
        /// </summary>
        /// <param name="categoryAmount">The category amount of the items being checked.</param>
        /// <param name="checkInherently">Take into account the children of the item category.</param>
        /// <param name="countStacks">If true count the stacks number not the stacks amounts.</param>
        /// <returns>Returns true if the amount of items with the Item Category in the collection is equal or bigger than the amount specified.</returns>
        bool HasItem(ItemCategoryAmount categoryAmount, bool checkInherently, bool countStacks = false);

        /// <summary>
        /// Checks if the inventory contains a list of Item Amounts.
        /// </summary>
        /// <param name="itemAmounts">The Item amounts list to check.</param>
        /// <returns>Returns true if the collection has ALL the item amounts in the list.</returns>
        bool HasItemList(ListSlice<ItemAmount> itemAmounts);

        /// <summary>
        /// Return the amount of item in the inventory.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <param name="similarItem">Find an itemInfo with that value equivalent item or just one similar?</param>
        /// <returns>The amount of that item in the inventory.</returns>
        int GetItemAmount(Item item, bool similarItem = true);

        /// <summary>
        /// Returns the number of items in the collection with the specified Item Definition.
        /// </summary>
        /// <param name="itemDefinition">The Item Definition to retrieve the amount of.</param>
        /// <param name="checkInherently">Take into account the children of the Item Definition.</param>
        /// <param name="unique">Should count unique items or amounts of items.</param>
        /// <returns>The number of items in the collection with the specified Item Definition.</returns>
        int GetItemAmount(ItemDefinition itemDefinition, bool checkInherently = false,
            bool unique = false);

        /// <summary>
        /// Returns the number of items in the collection with the specified Item Definition.
        /// </summary>
        /// <param name="itemCategory">The Item Category to retrieve the amount of.</param>
        /// <param name="checkInherently">Take into account the children of the item category.</param>
        /// <param name="unique">Should count unique items or amounts of items.</param>
        /// <returns>The number of items in the collection with the specified Item Category.</returns>
        int GetItemAmount(ItemCategory itemCategory, bool checkInherently = false, bool unique = false);

        /// <summary>
        /// Get any combination of item amounts be setting your own filter
        /// </summary>
        /// <param name="itemInfos">Reference to the array of item amounts. Can be resized up.</param>
        /// <param name="filterFunc">A function that will be used to filter the result, evaluating to true means it will be part of the result.</param>
        /// <param name="startIndex">The start index, the items will be added to the itemInfos array from that start index.</param>
        /// <returns>The list slice.</returns>
        ListSlice<ItemInfo> GetItemInfos(ref ItemInfo[] itemInfos, Func<ItemInfo, bool> filterFunc,
            int startIndex = 0);

        /// <summary>
        /// Get the sum of the attributes as an integer. Includes Int and Float attribute values.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="perAmount"> multiply the sum by the item amount in each slot.</param>
        /// <returns>Returns the sum of the attribute values with the attribute name.</returns>
        float GetFloatSum(string attributeName, bool perAmount = false);

        /// <summary>
        /// Get the sum of the attributes as an float. Includes Int and Float attribute values.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// /// <param name="perAmount"> multiply the sum by the item amount in each slot.</param>
        /// <returns>Returns the sum of the attribute values with the attribute name.</returns>
        int GetIntSum(string attributeName, bool perAmount = false);

        /// <summary>
        /// Send an event that the inventory changed.
        /// </summary>
        /// <param name="updateInventoryCache">Update the inventory cache if true.</param>
        void UpdateInventory(bool updateInventoryCache = true, bool force = false);

        /// <summary>
        /// Update the inventory item amounts list cache.
        /// </summary>
        void UpdateCachedInventory();
    }
}