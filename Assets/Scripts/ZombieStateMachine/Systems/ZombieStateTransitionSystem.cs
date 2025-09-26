using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct ZombieStateTransitionSystem : ISystem {

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // First job: Update state timers
            var updateTimersJob = new UpdateStateTimersJob {
                DeltaTime = deltaTime
            };
            updateTimersJob.ScheduleParallel();

            // Second job: Handle state transitions
            var stateTransitionJob = new ZombieStateTransitionJob {
                EntityCommandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };

            state.Dependency = stateTransitionJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct UpdateStateTimersJob : IJobEntity {
        public float DeltaTime;

        [BurstCompile]
        public readonly void Execute(ref ZombieStateData zombieState) {
            zombieState.TimeInState += DeltaTime;
        }
    }

    [BurstCompile]
    public partial struct ZombieStateTransitionJob : IJobEntity {
        public EntityCommandBuffer.ParallelWriter EntityCommandBuffer;

        [BurstCompile]
        public readonly void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity entity,
                ref ZombieStateData zombieState,
                in WanderStateData wanderState,
                in IdleStateData idleState,
                in AlertStateData alertState) {

            var shouldChangeState = false;
            var newState = ZombieStateType.Idle;

            // Set the new state in order of priority
            if (alertState.IsTriggered) {
                shouldChangeState = true;
                newState = ZombieStateType.Alert;
            } else {

                // Determine next state based on current state
                switch (zombieState.StateCurrent) {
                    case ZombieStateType.Idle:
                        if (zombieState.TimeInState >= idleState.MaxIdleTime) {
                            shouldChangeState = true;
                            newState = ZombieStateType.Wander;
                        }
                        break;

                    case ZombieStateType.Wander:
                        if (wanderState.TargetIsReached) {
                            shouldChangeState = true;
                            newState = ZombieStateType.Idle;
                        }
                        break;

                    default:
                        // Handle unexpected state 
                        shouldChangeState = true;
                        newState = ZombieStateType.Idle;
                        break;
                }
            }

            if (shouldChangeState) {
                UpdateState(ref zombieState, EntityCommandBuffer, chunkIndex, entity, newState);
            }
        }

        /// <summary>
        /// Sets the new state of the unit (if different than previous state)
        /// Schedule component updates via command buffer
        /// </summary>
        private static void UpdateState(ref ZombieStateData zombie, EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity entity, ZombieStateType newState) {

            if (zombie.StateCurrent == newState) return;

            zombie.StatePrevious = zombie.StateCurrent;
            zombie.StateCurrent = newState;
            zombie.TimeInState = 0f;

            // Disable all tags first
            ecb.SetComponentEnabled<IdleStateTag>(sortKey, entity, false);
            ecb.SetComponentEnabled<WanderStateTag>(sortKey, entity, false);
            ecb.SetComponentEnabled<AlertStateTag>(sortKey, entity, false);

            // Enable the appropriate tag for the new state
            switch (newState) {
                case ZombieStateType.Idle:
                    ecb.SetComponentEnabled<IdleStateTag>(sortKey, entity, true);
                    break;
                case ZombieStateType.Wander:
                    ecb.SetComponentEnabled<WanderStateTag>(sortKey, entity, true);
                    break;
                case ZombieStateType.Alert:
                    ecb.SetComponentEnabled<AlertStateTag>(sortKey, entity, true);
                    break;
            }
        }
    }
}