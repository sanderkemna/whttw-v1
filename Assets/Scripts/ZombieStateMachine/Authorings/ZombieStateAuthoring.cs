
namespace WHTTW.ZombieStateMachine {

    using ProjectDawn.Navigation;
    using ProjectDawn.Navigation.Hybrid;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using WHTTW.SoundEventsManager;

    [RequireComponent(typeof(AgentAuthoring), typeof(AgentCylinderShapeAuthoring), typeof(AgentColliderAuthoring))]
    [RequireComponent(typeof(AgentNavMeshAuthoring))]
    public class ZombieStateAuthoring : MonoBehaviour {

        [Tooltip("The starting state of the zombie.")]
        [SerializeField] private ZombieStateType startingState;

        [SerializeField] private IdleStateSettings idleSettings = new();
        [SerializeField] private AlertStateSettings alertSettings = new();
        [SerializeField] private WanderStateSettings wanderSettings = new();
        [SerializeField] private SoundEventListenerSettings soundEventSettings = new();

        class Baker : Baker<ZombieStateAuthoring> {
            public override void Bake(ZombieStateAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ZombieStateData() {
                    StateCurrent = authoring.startingState,
                    StatePrevious = authoring.startingState,
                });

                AddComponent(entity, new IdleStateData {
                    Timer = 0,
                    IsInExtraAnimationMode = false,
                    BoredAnimationIndex = 0,
                    timeUntilIdleAnimationChange = authoring.idleSettings.timeUntilIdleAnimationChange,
                    numberOfIdleAnimations = authoring.idleSettings.numberOfIdleAnimations,
                    MaxIdleTime = authoring.idleSettings.maxIdleTime,
                });

                AddComponent(entity, new AlertStateData {
                    IsTriggered = false,
                    AlertIntensity = 0f,
                    AlertDuration = 0f,
                    IntensityDecayRate = authoring.alertSettings.IntensityDecayRate,
                    MaxAlertDuration = authoring.alertSettings.MaxAlertDuration,
                    MaxSpeed = authoring.alertSettings.MaxSpeed,
                });

                AddComponent(entity, new WanderStateData() {
                    targetPosition = new float3(authoring.transform.position.x + 1, authoring.transform.position.y, authoring.transform.position.z),
                    originPosition = authoring.transform.position,
                    distanceMin = authoring.wanderSettings.distanceMin,
                    distanceMax = authoring.wanderSettings.distanceMax,
                    random = new Unity.Mathematics.Random((uint)entity.Index),
                    MaxSpeed = authoring.wanderSettings.MaxSpeed,
                });

                AddComponent(entity, new SoundEventListener {
                    HearingRange = authoring.soundEventSettings.hearingRange,
                    HearingSensitivity = authoring.soundEventSettings.hearingSensitivity,
                });

                AddComponent<IdleStateTag>(entity);
                AddComponent<WanderStateTag>(entity);
                AddComponent<AlertStateTag>(entity);
                SetComponentEnabled<IdleStateTag>(entity, authoring.startingState == ZombieStateType.Idle);
                SetComponentEnabled<WanderStateTag>(entity, authoring.startingState == ZombieStateType.Wander);
                SetComponentEnabled<AlertStateTag>(entity, authoring.startingState == ZombieStateType.Alert);
            }
        }

        void Start() {
            if (SoundEventsManager.Instance == null) {
                Debug.LogError("No SoundEventsManager is active in the scene!");
            }
        }
    }

    public struct ZombieStateData : IComponentData {
        public ZombieStateType StateCurrent;
        public ZombieStateType StatePrevious;
        public float TimeInState;
    }
    public struct IdleStateTag : IComponentData, IEnableableComponent { }
    public struct IdleStateData : IComponentData {
        public float Timer;
        public bool IsInExtraAnimationMode;
        public int BoredAnimationIndex;

        // set by the Authoring
        public float MaxIdleTime;
        public float timeUntilIdleAnimationChange;
        public int numberOfIdleAnimations;
    }

    public struct WanderStateTag : IComponentData, IEnableableComponent { }
    public struct WanderStateData : IComponentData {
        public float3 targetPosition;
        public float3 originPosition;
        public bool TargetIsSet;
        public bool TargetIsReached;
        public Unity.Mathematics.Random random;

        // set by the Authoring
        public float MaxSpeed;
        public float distanceMin;
        public float distanceMax;
    }

    public struct AlertStateTag : IComponentData, IEnableableComponent { }
    public struct AlertStateData : IComponentData {
        public bool IsTriggered;
        public float AlertIntensity; // 0.0 to 1.0
        public float AlertDuration;
        public bool HasTarget;
        public float3 TargetPosition;
        public bool HasReachedTarget;

        // set by the Authoring
        public float IntensityDecayRate;
        public float MaxAlertDuration;
        public float MaxSpeed;
    }

    /// <summary>
    /// The parameters of the unit's sensitivity to sound events.
    /// </summary>
    public struct SoundEventListener : IComponentData {
        public float HearingRange;
        public float HearingSensitivity; // 0.0 to 1.0
    }

    /// <summary>
    /// Package of data used to send around in the event when a sound is made.
    /// </summary>
    public struct SoundEvent {
        public float3 Position;
        public float Radius;
        public float Intensity;
        public float Timestamp;
    }

    [System.Serializable]
    public class IdleStateSettings {
        [Tooltip("The maximum time in [s] this unit will stay in this state.")]
        public float maxIdleTime = 10f;

        [Tooltip("The time in [s] until the default animation changes to another idle animation.")]
        public float timeUntilIdleAnimationChange = 4f;

        [Tooltip("The amount of extra idle animations there are in the blend tree.")]
        public int numberOfIdleAnimations = 6;
    }

    [System.Serializable]
    public class AlertStateSettings {
        [Tooltip("The max time in [s] a zombie can stay in the alert state, as precaution.")]
        public float MaxAlertDuration = 300f;

        [Tooltip("The decay rate of the alert intensity, after a while the alertness will slowly " +
            "degrade untill the zombie is back at idle state.")]
        public float IntensityDecayRate = 0.02f;

        [Tooltip("The maximum speed of the zombie in alert state.")]
        public float MaxSpeed = 2.5f;
    }

    [System.Serializable]
    public class SoundEventListenerSettings {
        [Tooltip("The range in which this unit can hear sound events.")]
        public float hearingRange = 15f;

        [Tooltip("The sensitivy of this to sound events, will be a factor in the increase of alertness.")]
        [Range(0f, 1f)]
        public float hearingSensitivity = 0.7f;
    }

    [System.Serializable]
    public class WanderStateSettings {
        [Tooltip("The minimum bounding box of new random walking target position.")]
        public float distanceMin = 2;

        [Tooltip("The maximum bounding box of new random walking target position.")]
        public float distanceMax = 5;

        [Tooltip("The maximum speed of the zombie in wander state.")]
        public float MaxSpeed = 1.5f;
    }
}