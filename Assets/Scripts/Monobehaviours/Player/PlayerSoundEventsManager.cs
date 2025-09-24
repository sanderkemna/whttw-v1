using UnityEngine;
using UnityEngine.Events;
using WHTTW.ZombieStateMachine;

namespace WHTTW.Player {

    /// <summary>
    /// Global Event System for Sounds the player is making, which can trigger a zombie.
    /// </summary>
    public static class PlayerSoundEventsManager {

        // Unity Event for footstep notifications
        public static UnityEvent<FootstepEventData> OnFootstepMade = new();

        // Struct for event data (serializable for UnityEvent)
        [System.Serializable]
        public struct FootstepEventData {
            public Vector3 position;
            public float radius;
            public float intensity;
            public float timestamp;

            //Convert to DOTS FootstepEvent
            public FootstepEvent ToDOTSEvent() {
                return new FootstepEvent {
                    Position = position,
                    Radius = radius,
                    Intensity = intensity,
                    Timestamp = timestamp
                };
            }
        }

        // Method to emit global footstep event
        public static void EmitFootstep(Vector3 position, float radius, float intensity) {
            var eventData = new FootstepEventData {
                position = position,
                radius = radius,
                intensity = intensity,
                timestamp = Time.time
            };

            OnFootstepMade?.Invoke(eventData);
        }

        // Optional: Clear all listeners (useful for scene transitions)
        public static void ClearAllListeners() {
            OnFootstepMade.RemoveAllListeners();
        }
    }
}