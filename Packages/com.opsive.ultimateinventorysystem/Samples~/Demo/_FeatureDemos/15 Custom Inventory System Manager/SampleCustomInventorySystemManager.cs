using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Opsive.UltimateInventorySystem.Samples
{
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.AttributeSystem;

    /// <summary>
    /// IMPORTANT: make sure to set the script execution order of this script to the same as the base InventorySystemManager
    /// In the sample we override the InventorySystemManager to override the item factory to set custom attribute values to mutable items.
    /// </summary>
    public class SampleCustomInventorySystemManager : InventorySystemManager
    {
        protected override void CreateInventorySystemFactory()
        {
            m_Factory = new MyInventorySystemFactory(this);
        }
    }

    public class MyInventorySystemFactory : InventorySystemFactory
    {
        public MyInventorySystemFactory(IInventorySystemManager manager) : base(manager)
        {
        }

        public override void InvokeOnItemCreated(Item item)
        {
            base.InvokeOnItemCreated(item);
            if (!item.IsMutable || !item.IsUnique) {
                return;
            }

            // Don't change the default item.
            if (item == item.ItemDefinition.DefaultItem) {
                return;
            }

            // Gets the "Attack" attribute and set a random value -5,+5 the default value.
            if (item.TryGetAttribute("Attack", out AttributeBase attribute,false,false)) {
                
                
                if (attribute is Attribute<int> intAttribute) {
                    var defaultValue = intAttribute.GetValue();
                    
                    intAttribute.SetOverrideValue(Random.Range(defaultValue-5,defaultValue+6));
                }
            }
        }
    }
}

