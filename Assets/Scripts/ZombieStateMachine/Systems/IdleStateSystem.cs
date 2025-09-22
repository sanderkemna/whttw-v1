using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct IdleStateSystem : ISystem {

        [BurstCompile]
        [WithAll(typeof(IdleStateTag))]
        private partial struct IdleStateJob : IJobEntity {

            public void Execute(ref ZombieStateData agentState) {

                // idle logic here
            }
        }

        public void OnUpdate(ref SystemState state) {
            new IdleStateJob().ScheduleParallel();
        }
    }
}