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
            in WalkStateData walkState,
            in AlertStateData alertState) {

            var shouldChangeState = false;
            var newState = ZombieStateType.Idle;

            // Determine next state based on current state
            switch (zombieState.StateCurrent) {
                case ZombieStateType.Idle:
                    if (zombieState.TimeInState >= 10f) {
                        shouldChangeState = true;
                        newState = ZombieStateType.Walk;
                    }
                    break;

                case ZombieStateType.Walk:
                    if (walkState.TargetIsReached) {
                        shouldChangeState = true;
                        newState = ZombieStateType.Alert;
                    }
                    break;

                case ZombieStateType.Alert:
                    if (!alertState.IsTriggered) {
                        shouldChangeState = true;
                        newState = ZombieStateType.Idle;
                    }
                    break;
            }

            if (shouldChangeState) {
                // Update state data
                ChangeState(ref zombieState, newState);

                // Schedule component updates via command buffer
                UpdateStateComponents(EntityCommandBuffer, chunkIndex, entity, newState);
            }
        }

        [BurstCompile]
        private static void ChangeState(ref ZombieStateData zombie, ZombieStateType newState) {
            if (zombie.StateCurrent == newState) return;

            zombie.StatePrevious = zombie.StateCurrent;
            zombie.StateCurrent = newState;
            zombie.TimeInState = 0f;
        }

        private static void UpdateStateComponents(EntityCommandBuffer.ParallelWriter ecb,
            int sortKey, Entity entity, ZombieStateType newState) {
            // Disable all tags first
            ecb.SetComponentEnabled<IdleStateTag>(sortKey, entity, false);
            ecb.SetComponentEnabled<WalkStateTag>(sortKey, entity, false);
            ecb.SetComponentEnabled<AlertStateTag>(sortKey, entity, false);

            // Enable the appropriate tag for the new state
            switch (newState) {
                case ZombieStateType.Idle:
                    ecb.SetComponentEnabled<IdleStateTag>(sortKey, entity, true);
                    break;
                case ZombieStateType.Walk:
                    ecb.SetComponentEnabled<WalkStateTag>(sortKey, entity, true);
                    break;
                case ZombieStateType.Alert:
                    ecb.SetComponentEnabled<AlertStateTag>(sortKey, entity, true);
                    break;
            }
        }
    }
}