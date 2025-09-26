using UnityEngine;
using UnityEngine.Events;
using WHTTW.ZombieStateMachine;

namespace WHTTW.SoundEventsManager {

    /// <summary>
    /// Global Event System for Sounds being made by anything in the game, which can trigger a zombie.
    /// Uses UnityEvents and a queue with max events per frame
    /// </summary>
    public class SoundEventsManager : MonoBehaviour {

        [Header("Event Settings")]
        [SerializeField, Tooltip("Maximum number of events to process per frame")]
        private int maxEventsPerFrame = 10;

        [SerializeField, Tooltip("Enable debug logging for sound events")]
        private bool enableDebugLogs = false;

        // Unity Event for footstep notifications
        [System.NonSerialized]
        public UnityEvent<SoundEventData> OnSoundMade = new();

#if UNITY_EDITOR
        [Header("Debug Info")]
        [SerializeField, Tooltip("Shows current number of queued events")]
        private bool showQueueInfo = true;

        void OnGUI() {
            if (showQueueInfo && Application.isPlaying) {
                GUI.Label(new Rect(10, 10, 300, 20), $"Sound Events Queued: {eventQueue.Count}");
                GUI.Label(new Rect(10, 30, 300, 20), $"Listeners: {GetListenerCount()}");
            }
        }
#endif

        // Singleton instance
        public static SoundEventsManager Instance { get; private set; }

        // Event queue for frame-rate independent processing
        private System.Collections.Generic.Queue<SoundEventData> eventQueue = new();

        void Awake() {
            Instance = this;
        }

        void Update() {
            // Process queued events
            int eventsProcessed = 0;
            while (eventQueue.Count > 0 && eventsProcessed < maxEventsPerFrame) {
                var eventData = eventQueue.Dequeue();
                OnSoundMade?.Invoke(eventData);
                eventsProcessed++;

                if (enableDebugLogs) {
                    Debug.Log($"Processed sound event at {eventData.position} with intensity {eventData.intensity}");
                }
            }
        }

        // Static method to emit global footstep event
        public static void EmitSound(Vector3 position, float radius, float intensity) {
            if (Instance != null) {
                var eventData = new SoundEventData {
                    position = position,
                    radius = radius,
                    intensity = intensity,
                    timestamp = Time.time
                };

                Instance.eventQueue.Enqueue(eventData);
            }
        }

        // Optional: Clear all listeners (useful for scene transitions)
        public void ClearAllListeners() {
            OnSoundMade?.RemoveAllListeners();
        }

        // Get current listener count (for debugging)
        public int GetListenerCount() {
            return OnSoundMade?.GetPersistentEventCount() ?? 0;
        }

        void OnDestroy() {
            if (Instance == this) {
                ClearAllListeners();
                Instance = null;
            }
        }
    }
}

// Struct for event data (serializable for UnityEvent)
// TODO: probably want to make this a base class SoundEventData and inherit for multiple different sound events
[System.Serializable]
public struct SoundEventData {
    public Vector3 position;
    public float radius;
    public float intensity;
    public float timestamp;

    //Convert to DOTS FootstepEvent
    public SoundEvent ToDOTSEvent() {
        return new SoundEvent {
            Position = position,
            Radius = radius,
            Intensity = intensity,
            Timestamp = timestamp
        };
    }
}