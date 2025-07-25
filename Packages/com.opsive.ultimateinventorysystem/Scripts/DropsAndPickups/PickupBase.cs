/// ---------------------------------------------
/// Ultimate Inventory System
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using Opsive.Shared.Audio;

namespace Opsive.UltimateInventorySystem.DropsAndPickups
{
    using Opsive.Shared.Game;
    using Opsive.UltimateInventorySystem.Interactions;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// Base class of a pickup interactable behavior.
    /// </summary>
    public abstract class PickupBase : InteractableBehavior
    {
        [Header("On Pickup success")]
        [Tooltip("On Pickup Success Event.")]
        [SerializeField] protected UnityEvent m_OnPickupSuccess;
        [Tooltip("The audio clip to play when the object is picked up.")]
        [SerializeField] protected AudioClip m_AudioClip;
        [Tooltip("The Audio Config.")]
        [SerializeField] protected AudioConfig m_AudioConfig;
        
        [Header("On pickup fail")]
        [Tooltip("On Pickup Fail Event")]
        [SerializeField] protected UnityEvent m_OnPickupFail;
        [Tooltip("The audio clip to play when the object is picked up.")]
        [SerializeField] protected AudioClip m_FailAudioClip;
        [Tooltip("The Audio Config.")]
        [SerializeField] protected AudioConfig m_FailAudioConfig;
        
        [Header("On partial pickup.")]
        [Tooltip("On Partial Pickup Event")]
        [SerializeField] protected UnityEvent m_OnPartialPickup;
        [Tooltip("The audio clip to play when the object is picked up.")]
        [SerializeField] protected AudioClip m_PartialPickupAudioClip;
        [Tooltip("The Audio Config.")]
        [SerializeField] protected AudioConfig m_PartialPickupAudioConfig;
        
        public UnityEvent OnPickupSuccess { get => m_OnPickupSuccess; set => m_OnPickupSuccess = value; }
        public AudioClip AudioClip { get => m_AudioClip; set => m_AudioClip = value; }
        public AudioConfig AudioConfig { get => m_AudioConfig; set => m_AudioConfig = value; }
        public UnityEvent OnPickupFail { get => m_OnPickupFail; set => m_OnPickupFail = value; }
        public AudioClip FailAudioClip { get => m_FailAudioClip; set => m_FailAudioClip = value; }
        public AudioConfig FailAudioConfig { get => m_FailAudioConfig; set => m_FailAudioConfig = value; }
        public UnityEvent OnPartialPickup { get => m_OnPartialPickup; set => m_OnPartialPickup = value; }

        public AudioClip PartialPickupAudioClip
        {
            get => m_PartialPickupAudioClip;
            set => m_PartialPickupAudioClip = value;
        }

        public AudioConfig PartialPickupAudioConfig
        {
            get => m_PartialPickupAudioConfig;
            set => m_PartialPickupAudioConfig = value;
        }

        /// <summary>
        /// Deactivate.
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();

            if (m_ScheduleReactivationTime <= 0) {
                DestroyPickup();
            }
        }

        /// <summary>
        /// Set the pickup interactable.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (m_Interactable == null) {
                m_Interactable = GetComponent<Interactable>();
                if(m_Interactable == null){ return; }
            }
            m_Interactable.SetIsInteractable(true);
        }

        /// <summary>
        /// Successfully picked up the object.
        /// </summary>
        protected virtual void NotifyPickupSuccess()
        {
            m_OnPickupSuccess?.Invoke();
            Shared.Audio.AudioManager.PlayAtPosition(m_AudioClip, m_AudioConfig, transform.position);
        }
        
        /// <summary>
        /// Unsuccessfully picked up the object.
        /// </summary>
        protected virtual void NotifyPickupFailed()
        {
            m_OnPickupFail?.Invoke();
            Shared.Audio.AudioManager.PlayAtPosition(m_FailAudioClip, m_FailAudioConfig, transform.position);
        }
        
        /// <summary>
        /// OnPartial pickup.
        /// </summary>
        protected virtual void NotifyPartialPickup()
        {
            m_OnPartialPickup?.Invoke();
            Shared.Audio.AudioManager.PlayAtPosition(m_PartialPickupAudioClip, m_PartialPickupAudioConfig, transform.position);
        }

        /// <summary>
        /// Return the pickup to the pool.
        /// </summary>
        protected virtual void DestroyPickup()
        {
            if (ObjectPool.IsPooledObject(gameObject)) {
                ObjectPool.Destroy(gameObject);
            } else {
                Destroy(gameObject);
            }
        }
    }
}