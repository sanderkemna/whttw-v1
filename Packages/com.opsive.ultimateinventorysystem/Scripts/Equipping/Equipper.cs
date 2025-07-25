/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

namespace Opsive.UltimateInventorySystem.Equipping
{
    using Opsive.Shared.Game;
    using Opsive.UltimateInventorySystem.Core;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using Opsive.UltimateInventorySystem.Core.InventoryCollections;
    using Opsive.UltimateInventorySystem.Storage;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;
    using EventHandler = Opsive.Shared.Events.EventHandler;

    /// <summary>
    /// The Equipper component is used to equip items by converting them to ItemObjects.
    /// </summary>
    public class Equipper : EquipperBase, IDatabaseSwitcher
    {
        [Tooltip("Set this option to true if the character has a bone Hierarchy.")]
        [SerializeField] protected bool m_SkinnedMeshCharacter = true;
        [Tooltip("The attribute name fo the equipment prefab (visual).")]
        [SerializeField] protected string m_EquipablePrefabAttributeName = "EquipmentPrefab";
        [Tooltip("The attribute name fo the usable item prefab (functional).")]
        [SerializeField] protected string m_UsableItemPrefabAttributeName = "UsableItemPrefab";
        [Tooltip("The item slot set used to restruct the items that can be equipped.")]
        [SerializeField] protected ItemSlotSet m_ItemSlotSet;
        [FormerlySerializedAs("m_Slots")]
        [Tooltip("The item object slots which holds the equipped item.")]
        [SerializeField] protected ItemObjectSlot[] m_ItemObjectSlots;
        [Tooltip("The main root node used to bind Skinned Mesh Renderers at runtime using the bones hiearchy.")]
        [SerializeField] protected Transform m_MainRootNode;

        protected Transform[] m_Bones;
        protected HashSet<Transform> m_BonesHashSet;
        protected Dictionary<string, Transform> m_BonesDictionary;

        public ItemSlotSet ItemSlotSet {
            get => m_ItemSlotSet;
            internal set => m_ItemSlotSet = value;
        }
        public ItemObjectSlot[] ItemObjectSlots {
            get => m_ItemObjectSlots;
            internal set => m_ItemObjectSlots = value;
        }

        /// <summary>
        /// PreInitialize.
        /// </summary>
        /// <param name="force">force initialize?</param>
        protected override void PreInitialize(bool force)
        {
            if(m_Initialized && !force){ return; }
            
            base.PreInitialize(force);
            
            m_BonesDictionary = new Dictionary<string, Transform>();
            m_BonesHashSet = new HashSet<Transform>();
            
            if (m_MainRootNode == null && m_SkinnedMeshCharacter) {
                var skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer == null) {
                    Debug.LogWarning("No Root Bone was found for the Equipper.");
                } else {
                    m_MainRootNode = skinnedMeshRenderer.rootBone;
                }
            }

            UpdateBones();
            
            ValidateSlots();
        }

        /// <summary>
        /// Update the character bones.
        /// </summary>
        /// <param name="rootNode">The new root node.</param>
        public virtual void UpdateBones(Transform rootNode)
        {
            m_MainRootNode = rootNode;
            UpdateBones();
        }
        
        /// <summary>
        /// update the bones.
        /// </summary>
        public virtual void UpdateBones()
        {
            if (m_MainRootNode == null) {
                if (m_SkinnedMeshCharacter) {
                    Debug.LogWarning("The Root Node for the Equipper cannot be null.");
                }
                return;
            }
            
            m_Bones = m_MainRootNode.GetComponentsInChildren<Transform>();
            
            m_BonesDictionary.Clear();
            m_BonesHashSet.Clear();
            for (int i = 0; i < m_Bones.Length; i++) {
                var bone = m_Bones[i];

                if (m_BonesDictionary.ContainsKey(bone.name)) {
                    Debug.LogWarning($"Bones in the '{m_MainRootNode.name}' bone hierarchy must all have unique names. The bone name '{bone.name}' is used multiple times.", bone);
                    continue;
                }
                
                m_BonesDictionary.Add(bone.name, bone);
                m_BonesHashSet.Add(bone);
            }
        }

        /// <summary>
        /// Validate the slots by checking the Slot Set.
        /// </summary>
        public virtual void ValidateSlots()
        {
            if (m_ItemObjectSlots == null) { m_ItemObjectSlots = new ItemObjectSlot[0]; }

            var needsRefresh = false;

            if (m_ItemSlotSet == null || m_ItemSlotSet.ItemSlots == null) { return; }

            if (m_ItemObjectSlots.Length != m_ItemSlotSet.ItemSlots.Count) {
                needsRefresh = true;
            } else {
                for (int i = 0; i < m_ItemObjectSlots.Length; i++) {
                    if (m_ItemObjectSlots[i].Name == m_ItemSlotSet.ItemSlots[i].Name &&
                       m_ItemObjectSlots[i].Category == m_ItemSlotSet.ItemSlots[i].Category) { continue; }

                    needsRefresh = true;
                    break;
                }
            }

            if (needsRefresh != true) { return; }

            Array.Resize(ref m_ItemObjectSlots, m_ItemSlotSet.ItemSlots.Count);

            for (int i = 0; i < m_ItemSlotSet.ItemSlots.Count; i++) {
                var itemSlot = m_ItemSlotSet.ItemSlots[i];

                bool foundMatch = false;
                for (int j = 0; j < m_ItemObjectSlots.Length; j++) {
                    if (m_ItemObjectSlots[j] == null || itemSlot.Name != m_ItemObjectSlots[j].Name) { continue; }

                    m_ItemObjectSlots[j] = new ItemObjectSlot(itemSlot.Name, itemSlot.Category, m_ItemObjectSlots[j]);
                    foundMatch = true;

                    if (i != j) {
                        var temp = m_ItemObjectSlots[i];
                        m_ItemObjectSlots[i] = m_ItemObjectSlots[j];
                        m_ItemObjectSlots[j] = temp;
                    }

                    break;
                }

                if (!foundMatch) {
                    m_ItemObjectSlots[i] = new ItemObjectSlot(itemSlot.Name, itemSlot.Category, false, null, null);
                }
            }
        }

        /// <summary>
        /// Equip an item to a specific slot.
        /// </summary>
        /// <param name="item">The item to equip.</param>
        /// <param name="index">The slot to equip to.</param>
        /// <returns>True if equipped successfully.</returns>
        public override bool Equip(Item item, int index)
        {
            var result = CreateItemObjectForSlot(item, index);
            
            base.Equip(item, index);

            return result;
        }
        
        /// <summary>
        /// UnEquip the item at the slot.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The slot.</param>
        public override void UnEquip(Item item, int index)
        {
            var itemObject = m_ItemObjectSlots[index].ItemObject;

            m_ItemObjectSlots[index].SetItemObject(null);

            if (itemObject != null)
            {
                if (ObjectPoolBase.IsPooledObject(itemObject.gameObject)) {
                    ReturnItemObjectToPool(itemObject);
                } else {
                    Destroy(itemObject.gameObject);
                }
            }

            base.UnEquip(item, index);
        }
        
        /// <summary>
        /// Get the item Object by index.
        /// </summary>
        /// <param name="slotIndex">The slot index.</param>
        /// <returns>The item Object at the index specified.</returns>
        public override ItemObject GetEquippedItemObject(int slotIndex)
        {
            if (m_ItemObjectSlots == null || slotIndex < 0 || slotIndex > m_ItemObjectSlots.Length ) {
                return null;
            }

            var slot = m_ItemObjectSlots[slotIndex];
            if (slot == null) {
                return null;
            }
            
            return slot.ItemObject;
        }

        /// <summary>
        /// Create an ItemObject for the item in the slot specified.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The slot index.</param>
        /// <returns>True if the item was successfully created.</returns>
        protected virtual bool CreateItemObjectForSlot(Item item, int index)
        {
            var slot = m_ItemObjectSlots[index];

            if (slot.Category != null && slot.Category.InherentlyContains(item) == false) { return false; }

            var itemObject = CreateItemObject(item);

            if (itemObject == null) { return false; }

            slot.SetItemObject(itemObject);

            if (slot.IsSkinnedEquipment) { SkinItemObject(itemObject, slot); } else { PositionItemObject(itemObject, slot); }

            return true;
        }

        /// <summary>
        /// Position the item object after it was spawned.
        /// </summary>
        /// <param name="itemObject">The item object to place.</param>
        /// <param name="slot">The slot in which to place it.</param>
        protected virtual  void PositionItemObject(ItemObject itemObject, ItemObjectSlot slot)
        {
            var itemTransform = itemObject.transform;
            itemTransform.SetParent(slot.Transform);

            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localScale = Vector3.one;
        }

        /// <summary>
        /// Skin an item object with a skinned mesh renderer to the character.
        /// </summary>
        /// <param name="itemObject">The item object ot skin.</param>
        /// <param name="slot">The slot to skin the item to.</param>
        protected virtual  void SkinItemObject(ItemObject itemObject, ItemObjectSlot slot)
        {
            var itemTransform = itemObject.transform;

            var parentTransform = slot.Transform == null  || m_BonesHashSet.Contains(slot.Transform) ? m_MainRootNode.parent : slot.Transform;
            
            itemTransform.SetParent(parentTransform);
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
            itemTransform.localScale = Vector3.one;
            
            SkinnedMeshRenderer[] equipmentSkinnedMeshRenderers = itemObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            for (var i = 0; i < equipmentSkinnedMeshRenderers.Length; i++) {
                SkinnedMeshRenderer equipmentSkinnedMeshRenderer = equipmentSkinnedMeshRenderers[i];

                // The item might be already linked to the character bones if it was previously spawned,
                // considering items are pooled in a sub pool linked to the Character.
                var equipmentRootNode = equipmentSkinnedMeshRenderer.rootBone;
                if (m_BonesHashSet.Contains(equipmentRootNode)) {
                    continue;
                }
                
                var newRootNode = GetMatchingSubRootNode(equipmentRootNode);
                var newBones = GetMatchingSubNodes(newRootNode, equipmentSkinnedMeshRenderer.bones);

                // Remove the old bones to clean up the hierarchy.
                if (equipmentRootNode != null) {
                    
                    //The Equipment Root Node might not match the master root node, search for it in the parents.
                    var node = equipmentRootNode;
                    var foundMatch = false;
                    while (node != null && node != itemTransform) {
                        if (node.name == m_MainRootNode.name) {
                            foundMatch = true;
                            break;
                        }
                        
                        node = node.parent;
                    }

                    if (foundMatch) {
                        GameObject.Destroy(node.gameObject);
                    } else {
                        GameObject.Destroy(equipmentRootNode.gameObject);
                    }
                }
                
                equipmentSkinnedMeshRenderer.rootBone = newRootNode;
                equipmentSkinnedMeshRenderer.bones = newBones;
            }
        }

        /// <summary>
        /// Get the matching sub root node.
        /// </summary>
        /// <param name="otherRootBone">The other root node.</param>
        /// <returns>The new matching root node.</returns>
        protected virtual Transform GetMatchingSubRootNode(Transform otherRootBone)
        {
            if (otherRootBone == null) {
                Debug.LogWarning("The equipment root node is null, the system will try to bind the equipment to the master root node.", gameObject);
                return m_MainRootNode;
            }

            if (m_BonesDictionary.TryGetValue(otherRootBone.name, out var subRootNode)) {
                return subRootNode;
            }
            
            Debug.LogError("No Matching root node found for the equipment. Looking for "+otherRootBone.name, gameObject);
            
            return m_MainRootNode;
        }

        /// <summary>
        /// Get the matching sub nodes within the new bone hiearchy.
        /// </summary>
        /// <param name="newRootNode">The new root node.</param>
        /// <param name="originalItemBones">The original bones.</param>
        /// <returns>The new bones.</returns>
        protected virtual Transform[] GetMatchingSubNodes(Transform newRootNode, Transform[] originalItemBones)
        {
            for (int i = 0; i < originalItemBones.Length; i++) {
                var originalItemBone = originalItemBones[i];
                if(m_BonesDictionary.TryGetValue(originalItemBone.name, out var newBone)) {
                    originalItemBones[i] = newBone;
                }
            }

            return originalItemBones;
        }

        /// <summary>
        /// Get the item Object slot by name.
        /// </summary>
        /// <param name="slotName">The slot name.</param>
        /// <returns>The item Object slot.</returns>
        public virtual ItemObjectSlot GetItemObjectSlot(string slotName)
        {
            for (int i = 0; i < m_ItemObjectSlots.Length; i++) {
                if (m_ItemObjectSlots[i].Name == slotName) {
                    return m_ItemObjectSlots[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Return the Item Object to the pool.
        /// </summary>
        /// <param name="itemObject">The itemObject to return.</param>
        protected virtual void ReturnItemObjectToPool(ItemObject itemObject)
        {
            // The skinned mesh bones will stay linked to the character, since the pool is a sub the item will never to bound to other characters.
            
            // The itemObject could have children that are pooled object such as the equipment model.
            for (int i = itemObject.transform.childCount - 1; i >= 0; i--) {
                var child = itemObject.transform.GetChild(i);
                if ((ObjectPoolBase.IsPooledObject(child.gameObject) == false)) { continue; }

                ObjectPoolBase.Destroy(child.gameObject);
            }

            ObjectPoolBase.Destroy(itemObject.gameObject);
        }

        /// <summary>
        /// Create an item Object from a pool.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The ItemObject.</returns>
        public virtual ItemObject CreateItemObject(Item item)
        {
            if (item.TryGetAttributeValue(m_EquipablePrefabAttributeName, out GameObject itemPrefab) == false) {
                Debug.LogError($"Prefab Attribute is undefined for Attribute {m_EquipablePrefabAttributeName}.", gameObject);
                return null;
            }

            if (itemPrefab == null) {
                Debug.LogError($"Prefab Attribute value is null for Attribute {m_EquipablePrefabAttributeName}.", gameObject);
                return null;
            }

            if (item.TryGetAttributeValue(m_UsableItemPrefabAttributeName, out GameObject usablePrefab) == false) {
                return CreateItemObjectInternal(item, itemPrefab);
            }

            var usableItemGameObject = CreateItemObjectInternal(item, usablePrefab);

            if (usableItemGameObject == null) {
                Debug.LogError($"The Usable Item GameObject is null for Attribute {m_UsableItemPrefabAttributeName}.");
                return null;
            }

            var characterID = gameObject.GetInstanceID();
            var equipmentGameObject = ObjectPoolBase.Instantiate(itemPrefab, characterID, usableItemGameObject.transform);
            equipmentGameObject.transform.localPosition = Vector3.zero;
            equipmentGameObject.transform.localRotation = Quaternion.identity;
            equipmentGameObject.transform.localScale = itemPrefab.transform.localScale;

            return usableItemGameObject;
        }

        /// <summary>
        /// Create an ItemObject.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemPrefab">The ItemObjectPrefab.</param>
        /// <returns>The item Object.</returns>
        protected virtual ItemObject CreateItemObjectInternal(Item item, GameObject itemPrefab)
        {
            if (itemPrefab == null) {
                Debug.LogWarning("The item prefab is null.");
                return null;
            }

            var characterID = gameObject.GetInstanceID();
            var itemGameObject = ObjectPoolBase.Instantiate(itemPrefab, characterID);

            var itemObject = itemGameObject.GetComponent<ItemObject>();
            if (itemObject == null) {
                itemObject = itemGameObject.AddComponent<ItemObject>();
            }

            itemObject.SetItem(item);
            return itemObject;
        }

        /// <summary>
        /// Check if the object contained by this component are part of the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>True if all the objects in the component are part of that database.</returns>
        bool IDatabaseSwitcher.IsComponentValidForDatabase(InventorySystemDatabase database)
        {
            if (database == null) { return false; }

            return (m_ItemSlotSet as IDatabaseSwitcher)?.IsComponentValidForDatabase(database) ?? true;
        }

        /// <summary>
        /// Replace any object that is not in the database by an equivalent object in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns>The objects that have been changed.</returns>
        ModifiedObjectWithDatabaseObjects IDatabaseSwitcher.ReplaceInventoryObjectsBySelectedDatabaseEquivalents(InventorySystemDatabase database)
        {
            if (database == null) { return null; }

            (m_ItemSlotSet as IDatabaseSwitcher)?.ReplaceInventoryObjectsBySelectedDatabaseEquivalents(database);

            return new UnityEngine.Object[] { m_ItemSlotSet };
        }
    }
}