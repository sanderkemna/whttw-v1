namespace Opsive.UltimateInventorySystem.UI.Item.AttributeViewModules
{
    using System;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using UnityEngine;

    /// <summary>
    /// An attribute UI component used to disable the AttributeView depending on the value.
    /// </summary>
    public class ShowHideAttributeView : AttributeViewModule
    {
        [Serializable]
        public enum CompareOperator
        {
            NotEqual,
            Equal,
            BiggerThan,
            SmallerThan,
            BiggerThanOrEqual,
            SmallerThanOrEqual
        }
        
        [Tooltip("Enable these gameobjects if the condition passes.")]
        [SerializeField] protected GameObject[] m_Show;
        [Tooltip("Disable these gameobjects if the condition passes.")]
        [SerializeField] protected GameObject[] m_Hide;
        [Tooltip("The Comparision Operator.")]
        [SerializeField] protected CompareOperator m_CompareOperator;
        [Tooltip("The Compare value.")]
        [SerializeField] protected float m_CompareValue;
        [Tooltip("Pass the condition if the value is null.")]
        [SerializeField] protected bool m_PassIfNull;

        /// <summary>
        /// Set the text.
        /// </summary>
        /// <param name="info">the attribute info.</param>
        public override void SetValue(AttributeInfo info)
        {
            if (info.Attribute == null) {
                Clear();
                return;
            }

            var value = info.Attribute.GetValueAsObject();

            if (!m_PassIfNull && (value == null || string.IsNullOrEmpty(value.ToString()))) {
                Clear();
                return;
            }

            var valueAsFloat = Convert.ToSingle(value);
            Debug.Log(info.Attribute.Name + " " +valueAsFloat);

            var pass = false;
            switch (m_CompareOperator) {
                case CompareOperator.NotEqual:
                    pass = valueAsFloat != m_CompareValue;
                    break;
                case CompareOperator.Equal:
                    pass = valueAsFloat == m_CompareValue;
                    break;
                case CompareOperator.BiggerThan:
                    pass = valueAsFloat > m_CompareValue;
                    break;
                case CompareOperator.SmallerThan:
                    pass = valueAsFloat < m_CompareValue;
                    break;
                case CompareOperator.BiggerThanOrEqual:
                    pass = valueAsFloat >= m_CompareValue;
                    break;
                case CompareOperator.SmallerThanOrEqual:
                    pass = valueAsFloat <= m_CompareValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            ActivateGameObjects(m_Show, pass);
            ActivateGameObjects(m_Hide, !pass);
        }

        /// <summary>
        /// Clear the box.
        /// </summary>
        public override void Clear()
        {
            ActivateGameObjects(m_Show, false);
            ActivateGameObjects(m_Hide, true);
        }

        /// <summary>
        /// Activate the gameobject.
        /// </summary>
        /// <param name="gameObjects">The gameobjects to activate.</param>
        /// <param name="activate">Activate or Desactivate?</param>
        public void ActivateGameObjects(GameObject[] gameObjects, bool activate)
        {
            if(gameObjects == null){ return; }
            for (int i = 0; i < gameObjects.Length; i++) {
                if(gameObjects[i] == null){ continue; }
                gameObjects[i].SetActive(activate);
            }
        }
    }
}