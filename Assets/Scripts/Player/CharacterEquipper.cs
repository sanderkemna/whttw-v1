using AdvancedPeopleSystem;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.AttributeSystem;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Equipping;
using UnityEngine;

public class CharacterEquipper : EquipperBase {

    [SerializeField]
    [Tooltip("Required -> This is the Advanced People System reference coming from the visuals character.")]
    private CharacterCustomization character;

    [SerializeField]
    [Tooltip("Required -> The attribute name as used in the UIS for the CharacterElementType for Advanced People System.")]
    private string AdvancedPeopleSystemTypeAttributeName = "AdvancedPeopleSystem-Type";

    [SerializeField]
    [Tooltip("Required -> The attribute name as used in the UIS for the index for Advanced People System.")]
    private string AdvancedPeopleSystemIndexAttributeName = "AdvancedPeopleSystem-Index";

    protected override void Start() {
        base.Start();

        if (character == null) {
            Debug.LogError("You didnt drag the reference to CharacterCustomization yo...");
            return;
        }
    }

    protected override void OnAddedItemToInventory(ItemInfo originItemInfo, ItemStack addedItemStack) {
        if (addedItemStack == null) { return; }
        if (addedItemStack.ItemCollection == m_EquipmentItemCollection) {
            if (ItemHasWearablesCategory(addedItemStack.Item)) {
                SetCloths(addedItemStack.Item);
            }
        }
    }

    protected override void OnRemovedItemFromInventory(ItemInfo removedItemInfo) {
        base.OnRemovedItemFromInventory(removedItemInfo);
        if (ItemHasWearablesCategory(removedItemInfo.Item)) {
            ResetCloths(removedItemInfo.Item);
        }
    }

    /// <summary>
    /// Check if it is a wearable.
    /// </summary>
    private bool ItemHasWearablesCategory(Item item) {
        var WearableCategory = InventorySystemManager.GetItemCategory("Wearable");
        if (!WearableCategory.InherentlyContains(item)) {
            return false;
        } else {
            return true;
        }
    }

    /// <summary>
    /// Calls the CharacterCustomization API to set the cloths, depending on type and cloth index.
    /// </summary>
    private void SetCloths(Item item) {
        if (!item.HasAttribute(AdvancedPeopleSystemTypeAttributeName)) {
            Debug.LogError($"Couldnt find the attribute for CharacterElementType in {item}...");
            return;
        }
        if (!item.HasAttribute(AdvancedPeopleSystemIndexAttributeName)) {
            Debug.LogError($"Couldnt find the attribute for int index in {item}...");
            return;
        }

        // get the CharacterElementType of the item
        var characterElementTypeAttribute = item.GetAttribute<Attribute<CharacterElementType>>(AdvancedPeopleSystemTypeAttributeName);
        CharacterElementType characterElementType = characterElementTypeAttribute.GetValue();

        // get the index int of the item
        var itemIndexAttribute = item.GetAttribute<Attribute<int>>(AdvancedPeopleSystemIndexAttributeName);
        int itemIndex = itemIndexAttribute.GetValue();

        character.SetElementByIndex(characterElementType, itemIndex); //Set shirt element by Id
    }

    /// <summary>
    /// Calls the CharacterCustomization API to reset the cloths CharacterElementType.
    /// </summary>
    private void ResetCloths(Item item) {
        if (!item.HasAttribute(AdvancedPeopleSystemTypeAttributeName)) {
            Debug.LogError($"Couldnt find the attribute for CharacterElementType in {item}...");
            return;
        }

        // get the CharacterElementType of the item
        var characterElementTypeAttribute = item.GetAttribute<Attribute<CharacterElementType>>(AdvancedPeopleSystemTypeAttributeName);
        CharacterElementType characterElementType = characterElementTypeAttribute.GetValue();

        character.ClearElement(characterElementType); //clear character element
    }
}
