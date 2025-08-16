using AdvancedPeopleSystem;
using Opsive.UltimateInventorySystem.Core.DataStructures;
using Opsive.UltimateInventorySystem.Equipping;
using UnityEngine;

public class CharacterEquipper : EquipperBase {

    [SerializeField]
    [Tooltip("Required -> This is the Advanced People System reference coming from the visuals character.")]
    private CharacterCustomization character;

    protected override void Awake() {
        base.Awake();
    }

    protected override void OnAddedItemToInventory(ItemInfo originItemInfo, ItemStack addedItemStack) {
        if (addedItemStack == null) { return; }
        if (addedItemStack.ItemCollection == m_EquipmentItemCollection) {
            var index = m_EquipmentItemCollection.GetItemSlotIndex(addedItemStack);
            Debug.Log("I Equip!");
            Equip(addedItemStack.Item, index);
            ChangeCloths();
        }
    }

    /// <summary>
    /// Calls the CharacterCustomization API to change the cloths.
    /// TODO: add fields for the type and index, then make it possible to swap the old cloths to drop
    /// </summary>
    private void ChangeCloths() {
        if (character == null) {
            Debug.LogError("You didnt drag the reference to CharacterCustomization yo...");
            return;
        }
        var lastShirtIndex = character.Settings.shirtsPresets.Count - 1;
        character.SetElementByIndex(CharacterElementType.Shirt, 8); //Set shirt element by Id
    }
}
