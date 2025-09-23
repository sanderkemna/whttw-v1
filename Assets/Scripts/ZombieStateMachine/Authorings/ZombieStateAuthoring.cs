using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace WHTTW.ZombieStateMachine {

    public class ZombieStateAuthoring : MonoBehaviour {

        [Tooltip("The starting state of the zombie.")]
        [SerializeField] private ZombieStateType startingState;

        [SerializeField] private IdleStateSettings idleSettings = new();
        [SerializeField] private AlertStateSettings alertSettings = new();
        [SerializeField] private WalkStateSettings walkSettings = new();

        class Baker : Baker<ZombieStateAuthoring> {
            public override void Bake(ZombieStateAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ZombieStateData() {
                    StateCurrent = authoring.startingState,
                    StatePrevious = authoring.startingState,
                });

                AddComponent(entity, new IdleStateData {
                    Timer = 0,
                    IsIdle = false,
                    BoredAnimationIndex = 0,
                    timeUntilIdleAnimationChange = authoring.idleSettings.timeUntilIdleAnimationChange,
                    numberOfIdleAnimations = authoring.idleSettings.numberOfIdleAnimations
                });

                AddComponent(entity, new AlertStateData {
                    IsTriggered = false,
                    AlertIntensity = 0f,
                    AlertDuration = 0f,
                    IntensityDecayRate = authoring.alertSettings.IntensityDecayRate,
                    MaxAlertDuration = authoring.alertSettings.MaxAlertDuration,
                });

                AddComponent(entity, new WalkStateData() {
                    targetPosition = new float3(authoring.transform.position.x + 1, authoring.transform.position.y, authoring.transform.position.z),
                    originPosition = authoring.transform.position,
                    distanceMin = authoring.walkSettings.distanceMin,
                    distanceMax = authoring.walkSettings.distanceMax,
                    random = new Unity.Mathematics.Random((uint)entity.Index),
                });

                AddComponent<IdleStateTag>(entity);
                AddComponent<WalkStateTag>(entity);
                AddComponent<AlertStateTag>(entity);
                SetComponentEnabled<IdleStateTag>(entity, authoring.startingState == ZombieStateType.Idle);
                SetComponentEnabled<WalkStateTag>(entity, authoring.startingState == ZombieStateType.Walk);
                SetComponentEnabled<AlertStateTag>(entity, authoring.startingState == ZombieStateType.Alert);
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
        public bool IsIdle;
        public int BoredAnimationIndex;

        // set by the Authoring
        public float timeUntilIdleAnimationChange;
        public int numberOfIdleAnimations;
    }

    public struct WalkStateTag : IComponentData, IEnableableComponent { }
    public struct WalkStateData : IComponentData {
        public float3 targetPosition;
        public float3 originPosition;
        public bool TargetIsSet;
        public bool TargetIsReached;
        public Unity.Mathematics.Random random;

        // set by the Authoring
        public float distanceMin;
        public float distanceMax;
    }

    public struct AlertStateTag : IComponentData, IEnableableComponent { }
    public struct AlertStateData : IComponentData {
        public bool IsTriggered;
        public float AlertIntensity; // 0.0 to 1.0
        public float AlertDuration;

        // set by the Authoring
        public float IntensityDecayRate;
        public float MaxAlertDuration;
    }


    [System.Serializable]
    public class IdleStateSettings {
        [Tooltip("The time in [s] until the default animation changes to another idle animation.")]
        public float timeUntilIdleAnimationChange = 2f;

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
    }

    [System.Serializable]
    public class WalkStateSettings {
        [Tooltip("The minimum bounding box of new random walking target position.")]
        public float distanceMin = 2;

        [Tooltip("The maximum bounding box of new random walking target position.")]
        public float distanceMax = 5;

        [Tooltip("The max linger time in [s] of the unit.")]
        public float lingerTimerMax = 10f;
    }
}