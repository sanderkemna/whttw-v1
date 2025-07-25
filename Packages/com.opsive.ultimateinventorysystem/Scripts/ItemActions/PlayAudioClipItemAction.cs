namespace Opsive.UltimateInventorySystem.ItemActions
{
    using System;
    using Opsive.Shared.Audio;
    using Opsive.UltimateInventorySystem.Core.DataStructures;
    using UnityEngine;

    /// <summary>
    /// Plays an AudioClip when the effect starts.
    /// </summary>
    [Serializable]
    public class PlayAudioClipItemAction : ItemAction
    {
        [Tooltip("A set of AudioClips that can be played when the effect is started.")]
        [SerializeField] protected AudioClipSet m_AudioClipSet = new AudioClipSet();

        public AudioClipSet AudioClipSet { get { return m_AudioClipSet; } set { m_AudioClipSet = value; } }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PlayAudioClipItemAction()
        {
            m_Name = "Play Audio";
        }

        /// <summary>
        /// Check if the action can be invoked.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        /// <returns>True if the action can be invoked.</returns>
        protected override bool CanInvokeInternal(ItemInfo itemInfo, ItemUser itemUser)
        {
            return true;
        }

        /// <summary>
        /// Invoke the action.
        /// </summary>
        /// <param name="itemInfo">The item.</param>
        /// <param name="itemUser">The item user (can be null).</param>
        protected override void InvokeActionInternal(ItemInfo itemInfo, ItemUser itemUser)
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