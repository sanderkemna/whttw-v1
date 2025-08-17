using AdvancedPeopleSystem;
using Opsive.Shared.Events;
using Opsive.UltimateInventorySystem.Core;
using Opsive.UltimateInventorySystem.Core.AttributeSystem;
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

    protected override void Awake() {
        base.Awake();
    }

    private void OnEnable() {
        EventHandler.RegisterEvent<Item, int>(
            this,
            EventNames.c_Equipper_OnEquipped_Item_Index,
            OnEquipmentHasChanged
        );
    }

    /// <summary>
    /// After Equipped has changed, check if it was a wearable, if yes call the ChangeCloths method 
    /// to physically change the cloths.
    /// </summary>
    private void OnEquipmentHasChanged(Item item, int index) {
        // Check if the item is part of a category
        var WearableCategory = InventorySystemManager.GetItemCategory("Wearable");
        if (!WearableCategory.InherentlyContains(item)) {
            return;
        } else {
            ChangeCloths(item, index);
        }
    }

    /// <summary>
    /// Calls the CharacterCustomization API to change the cloths.
    /// TODO: add fields for the type and index, then make it possible to swap the old cloths to drop
    /// </summary>
    private void ChangeCloths(Item item, int index) {
        if (character == null) {
            Debug.LogError("You didnt drag the reference to CharacterCustomization yo...");
            return;
        }
        if (!item.HasAttribute(AdvancedPeopleSystemTypeAttributeName)) {
            Debug.LogError("Couldnt find the attribute for CharacterElementType...");
            return;
        }
        if (!item.HasAttribute(AdvancedPeopleSystemIndexAttributeName)) {
            Debug.LogError("Couldnt find the attribute for int index...");
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

    private void OnDisable() {
        EventHandler.UnregisterEvent<Item, int>(
            this,
            EventNames.c_Equipper_OnEquipped_Item_Index,
            OnEquipmentHasChanged
        );
    }
}
