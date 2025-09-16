using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct WalkStateSystem : ISystem {

        [BurstCompile]
        private partial struct WalkStateJob : IJobEntity {

            public void Execute(ref AgentStateData agentState) {
                if (agentState.State != AgentStateType.Walk) { return; }

                // Walk logic here
            }
        }

        public void OnUpdate(ref SystemState state) {
            new WalkStateJob().ScheduleParallel();
        }
    }
}