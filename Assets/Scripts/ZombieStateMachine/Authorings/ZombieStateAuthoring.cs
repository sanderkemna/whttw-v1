using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace WHTTW.ZombieStateMachine {

    //TODO: just make this one big zombiestatedata component, instead of multiple small ones.
    public class ZombieStateAuthoring : MonoBehaviour {

        [Tooltip("The starting state of the zombie.")]
        [SerializeField] private ZombieStateType startingState;

        [SerializeField] private IdleStateSettings idleSettings = new();

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
                    timeUntilIdleChange = authoring.idleSettings.timeUntilIdleChange,
                    numberOfIdleAnimations = authoring.idleSettings.numberOfIdleAnimations
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
                SetComponentEnabled<IdleStateTag>(entity, authoring.startingState == ZombieStateType.Idle);
                SetComponentEnabled<WalkStateTag>(entity, authoring.startingState == ZombieStateType.Walk);
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
        public float timeUntilIdleChange;
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

    [System.Serializable]
    public class IdleStateSettings {
        [Tooltip("The time until the default animation changes to another idle animation.")]
        public float timeUntilIdleChange = 2;

        [Tooltip("The amount of extra idle animations there are in the blend tree.")]
        public int numberOfIdleAnimations = 6;
    }

    [System.Serializable]
    public class WalkStateSettings {
        [Tooltip("The minimum bounding box of new random walking target position.")]
        public float distanceMin = 2;

        [Tooltip("The maximum bounding box of new random walking target position.")]
        public float distanceMax = 5;

        [Tooltip("The max linger time in [s] of the unit.")]
        public float lingerTimerMax = 10;
    }
}