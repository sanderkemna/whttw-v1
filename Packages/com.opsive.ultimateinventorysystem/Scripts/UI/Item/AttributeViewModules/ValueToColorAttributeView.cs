namespace Opsive.UltimateInventorySystem.UI.Item.AttributeViewModules
{
    using System;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// A Attribute View UI component that lets you bind the color of an icon to an item attribute.
    /// </summary>
    public class ValueToColorAttributeView : AttributeViewModule
    {
        [Tooltip("The icon image.")]
        [SerializeField] protected Image m_Icon;
        [Tooltip("The missing item icon sprite.")]
        [SerializeField] protected Color[] m_Colors;
        [Tooltip("The missing item icon sprite.")]
        [SerializeField] protected Color m_MissingColor;
        [Tooltip("Disable the image component if item is null.")]
        [SerializeField] protected bool m_DisableOnClear;

        
        /// <summary>
        /// Set the color.
        /// </summary>
        /// <param name="info">the attribute info.</param>
        public override void SetValue(AttributeInfo info)
        {
            if (info.Attribute == null) {
                Clear();
                return;
            }

            m_Icon.enabled = true;
            
            
            
            if (info.Attribute is Attribute<int> intAttribute) {
                var index = intAttribute.GetValue();
                if (index >= 0 && index < m_Colors.Length) {
                    m_Icon.color = m_Colors[index];
                    return;
                }
            }
            
            var value = info.Attribute.GetValueAsObject();
            if (value is Enum enumValue) {
                var integerValue = Convert.ToInt32(enumValue);
                if (integerValue >= 0 && integerValue < m_Colors.Length) {
                    m_Icon.color = m_Colors[integerValue];
                    return;
                }
            }

            m_Icon.color = m_MissingColor;
        }

        /// <summary>
        /// Clear the component.
        /// </summary>
        public override void Clear()
        {
            m_Icon.color = m_MissingColor;
            if (m_DisableOnClear) { m_Icon.enabled = false; }

        }
    }
}