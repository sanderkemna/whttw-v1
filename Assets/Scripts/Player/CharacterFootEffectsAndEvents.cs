
namespace WHTTW.Player {

    using Opsive.UltimateCharacterController.Character;
    using Opsive.UltimateCharacterController.Editor.Inspectors.Character;
    using System;
    using UnityEditor;
    using UnityEngine;
    using WHTTW.SoundEventsManager;

    /// <summary>
    /// Overrides the Opsive CharacterFootEffects to add a global event on each footstep.
    /// </summary>
    public class CharacterFootEffectsAndEvents : CharacterFootEffects {

        [Tooltip("The base range that a zombie will hear the footstep.")]
        [SerializeField] private float noiseRadius = 10f;
        [Tooltip("The intensity of the sound, ranging 0-1. The higher the intensity, the higher the alertness of the zombie will be.")]
        [SerializeField, Range(0f, 1f)] private float noiseIntensity = 0.8f;
        [Tooltip("Multiplier of the range and itensity depending on the speed of the player.")]
        [SerializeField] private float runningNoiseMultiplier = 1.5f;

        public override bool FootStep(Transform foot, bool flipFootprint) {
            bool surfaceEffectSuccess = base.FootStep(foot, flipFootprint);

            EmitFootstepEvent(foot, surfaceEffectSuccess);

            return surfaceEffectSuccess;
        }

        private void EmitFootstepEvent(Transform foot, bool hitSurface) {
            float finalRadius = CalculateNoiseRadius(hitSurface);
            float finalIntensity = CalculateNoiseIntensity(hitSurface);

            // Emit global event - any system can listen to this
            SoundEventsManager.EmitSound(foot.position, finalRadius, finalIntensity);
        }

        private float CalculateNoiseRadius(bool hitSurface) {
            float baseRadius = noiseRadius;

            if (!hitSurface) baseRadius *= 0.5f;

            if (m_CharacterLocomotion != null) {
                float speed = m_CharacterLocomotion.Velocity.magnitude;
                float walkSpeed = 2f;

                if (speed > walkSpeed) {
                    baseRadius *= Mathf.Lerp(1f, runningNoiseMultiplier, (speed - walkSpeed) / walkSpeed);
                }
            }

            return baseRadius * GetSurfaceNoiseModifier();
        }

        private float CalculateNoiseIntensity(bool hitSurface) {
            float baseIntensity = noiseIntensity;

            if (!hitSurface) baseIntensity *= 0.3f;

            if (m_CharacterLocomotion != null) {
                float speed = m_CharacterLocomotion.Velocity.magnitude;
                float walkSpeed = 2f;

                if (speed > walkSpeed) {
                    baseIntensity *= Mathf.Lerp(1f, runningNoiseMultiplier, (speed - walkSpeed) / walkSpeed);
                }
            }

            return Mathf.Clamp01(baseIntensity * GetSurfaceNoiseModifier());
        }

        private float GetSurfaceNoiseModifier() {
            return 1f; // Can be extended to read surface types
        }

        void OnDrawGizmosSelected() {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, noiseRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, noiseRadius * runningNoiseMultiplier);
        }
    }

    /// <summary>
    /// override the custom editor from Opsive
    /// </summary>
    [CustomEditor(typeof(CharacterFootEffectsAndEvents), true)]
    public class CharacterFootEffectsInspector2 : CharacterFootEffectsInspector {
        protected override Action GetDrawCallback() {
            var baseCallback = base.GetDrawCallback();

            baseCallback += () => {
                if (Foldout("Sound Events")) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(PropertyFromName("noiseRadius"));
                    EditorGUILayout.PropertyField(PropertyFromName("noiseIntensity"));
                    EditorGUILayout.PropertyField(PropertyFromName("runningNoiseMultiplier"));
                }
            };
            return baseCallback;
        }
    }
}