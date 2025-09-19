using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct ZombieStateTransitionSystem : ISystem {

        [BurstCompile]
        private partial struct StateTransitionJob : IJobEntity {
            public float DeltaTime;

            public void Execute(ref ZombieStateData zombieState, ref WalkStateData walk) {

                zombieState.TimeInState += DeltaTime;

                // Determine next state based on current state
                switch (zombieState.State) {
                    case ZombieStateType.Idle:
                        if (zombieState.TimeInState < 10f)
                            return;

                        zombieState.State = ZombieStateType.Walk;
                        walk.TargetIsSet = false;
                        zombieState.TimeInState = 0f;

                        break;

                    case ZombieStateType.Walk:
                        if (walk.TargetIsReached) {
                            zombieState.State = ZombieStateType.Idle;
                            zombieState.TimeInState = 0f;
                            return;
                        }

                        break;

                    case ZombieStateType.Run:
                        if (zombieState.TimeInState < 2f)
                            return;

                        zombieState.State = ZombieStateType.Idle;
                        zombieState.TimeInState = 0f;

                        break;
                }

            }
        }

        public void OnUpdate(ref SystemState state) {

            new StateTransitionJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();
        }
    }
}