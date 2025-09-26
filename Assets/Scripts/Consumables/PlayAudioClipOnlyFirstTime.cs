
using Opsive.Shared.Audio;
using Opsive.UltimateCharacterController.Items.Actions.Effect;
using System;
using UnityEngine;

/// <summary>
/// Plays an AudioClip when the effect starts, but only the first time.
/// </summary>
[Serializable]
public class PlayAudioClipFirstTime : ItemEffect {
    [Tooltip("A set of AudioClips that can be played when the effect is started.")]
    [SerializeField] protected AudioClipSet m_AudioClipSet = new AudioClipSet();

    public AudioClipSet AudioClipSet { get { return m_AudioClipSet; } set { m_AudioClipSet = value; } }

    private bool ItemHasBeenUsed = false;

    /// <summary>
    /// Can the effect be started? Will only start the first time, then it wont anymore
    /// </summary>
    /// <returns>True if the effect can be started.</returns>
    public override bool CanInvokeEffect() {
        if (!ItemHasBeenUsed) {
            ItemHasBeenUsed = true;
            return true;
        } else {
            return false;
        }
    }

    /// <summary>
    /// Get the game object on which to play the audio clip.
    /// </summary>
    /// <returns>The game object on which to play the audio.</returns>
    protected virtual GameObject GetPlayGameObject() {
        return m_CharacterItemAction.GameObject;
    }

    /// <summary>
    /// The effect has been started.
    /// </summary>
    protected override void InvokeEffectInternal() {
        base.InvokeEffectInternal();

        m_AudioClipSet.PlayAudioClip(GetPlayGameObject());
    }
}