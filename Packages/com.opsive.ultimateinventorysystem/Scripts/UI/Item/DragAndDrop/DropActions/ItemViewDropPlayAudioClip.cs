namespace Opsive.UltimateInventorySystem.UI.Item.DragAndDrop.DropActions
{
    using System;
    using Opsive.Shared.Audio;
    using UnityEngine;

    /// <summary>
    /// Plays an AudioClip when the effect starts.
    /// </summary>
    [Serializable]
    public class ItemViewDropPlayAudioClip : ItemViewDropAction
    {
        [Tooltip("A set of AudioClips that can be played when the effect is started.")]
        [SerializeField] protected AudioClipSet m_AudioClipSet = new AudioClipSet();

        public AudioClipSet AudioClipSet { get { return m_AudioClipSet; } set { m_AudioClipSet = value; } }

        /// <summary>
        /// Drop Action.
        /// </summary>
        /// <param name="itemViewDropHandler">The Item View Drop Handler.</param>
        public override void Drop(ItemViewDropHandler itemViewDropHandler)
        {            
            m_AudioClipSet.PlayAudioClip(GetPlayGameObject());

        }

        /// <summary>
        /// Get the game object on which to play the audio clip.
        /// </summary>
        /// <returns>The game object on which to play the audio.</returns>
        protected virtual GameObject GetPlayGameObject()
        {
            return null;
        }
    }
}