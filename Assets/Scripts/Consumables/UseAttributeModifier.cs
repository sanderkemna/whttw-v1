
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Items.Actions;
using Opsive.UltimateCharacterController.Items.Actions.Modules;
using Opsive.UltimateCharacterController.Traits;
using System;
using UnityEngine;
using Attribute = Opsive.UltimateCharacterController.Traits.Attribute;
using EventHandler = Opsive.Shared.Events.EventHandler;

/// <summary>
/// A module used to modify an attribute when and/or while an item is used.
/// It can be used to prevent the item from being used if the attribute is invalid.
/// It also enables or disables a GameObjects depending if the item is being used or not.
/// This is not a toggle like the original one, but will run every time.
/// </summary>
[Serializable]
public class UseAttributeModifier : BasicUsableActionModule {
    [Tooltip("Prevent the item from being used if the Attribute is not valid?")]
    [SerializeField] protected bool m_PreventUseIfAttributeNotValid = true;
    [Tooltip("The attribute modifier which is active while the item is being used.")]
    [SerializeField] protected AttributeModifier m_UseModifier = new AttributeModifier("Battery", 0, Attribute.AutoUpdateValue.Decrease);

    public bool PreventUseIfAttributeNotValid { get => m_PreventUseIfAttributeNotValid; set => m_PreventUseIfAttributeNotValid = value; }
    public AttributeModifier UseModifier { get => m_UseModifier; set => m_UseModifier = value; }
    /// <summary>
    /// This will show the attribute amount in the object binding dropdown, by making it a property instead of the current existing field.
    /// https://opsive.com/support/documentation/ultimate-inventory-system/item-objects/item-binding/
    /// </summary>
    public float AttributeAmount { get => m_UseModifier.Amount; set => m_UseModifier.Amount = value; }

    /// <summary>
    /// Initialize the module.
    /// </summary>
    protected override void InitializeInternal() {
        base.InitializeInternal();

        if (m_UseModifier != null) {
            if (m_UseModifier.Initialize(GameObject)) {
                EventHandler.RegisterEvent(m_UseModifier.Attribute, "OnAttributeReachedDestinationValue", OnAttributeEmpty);
            }
        }
    }

    /// <summary>
    /// When the attribute becomes empty toggle the object off.
    /// </summary>
    protected virtual void OnAttributeEmpty() {
        ChangeAttributeValue(false);
    }

    /// <summary>
    /// Can the item be used?
    /// </summary>
    /// <param name="useAbility">A reference to the ability using the item.</param>
    /// <param name="abilityState">The state of the Use ability when calling CanUseItem.</param>
    /// <returns>True if the item can be used.</returns>
    public override bool CanStartUseItem(Use useAbility, UsableAction.UseAbilityState abilityState) {
        var baseValue = base.CanStartUseItem(useAbility, abilityState);
        if (baseValue == false) {
            return false;
        }

        // The object can't be used if there is no attribute amount left.
        if (m_PreventUseIfAttributeNotValid && m_UseModifier != null && !m_UseModifier.IsValid()) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Use the item.
    /// </summary>
    public override void UseItem() {
        base.UseItem();

        // Only toggle if attribute is positive.
        if (m_UseModifier?.Attribute != null && m_UseModifier.Attribute.Value <= 0) {
            return;
        }

        ChangeAttributeValue(true);
    }

    /// <summary>
    /// Toggle the attribute value on or off.
    /// </summary>
    /// <param name="on">Should the attribute changing be turned on?</param>
    public virtual void ChangeAttributeValue(bool on) {
        if (m_UseModifier != null) {
            m_UseModifier.EnableModifier(on);
        }
    }

    /// <summary>
    /// The item will start unequipping.
    /// </summary>
    public override void StartUnequip() {
        base.StartUnequip();
        ChangeAttributeValue(false);
    }

    /// <summary>
    /// The object has been destroyed.
    /// </summary>
    public override void OnDestroy() {
        base.OnDestroy();

        if (m_UseModifier != null && m_UseModifier.Attribute != null) {
            EventHandler.UnregisterEvent(m_UseModifier.Attribute, "OnAttributeReachedDestinationValue", OnAttributeEmpty);
        }
    }
}