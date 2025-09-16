using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct AgentStateTransitionSystem : ISystem {

        [BurstCompile]
        private partial struct StateTransitionJob : IJobEntity {
            public float DeltaTime;

            public void Execute(ref AgentStateData agentState) {

                agentState.TimeInState += DeltaTime;

                if (agentState.TimeInState < 2f)
                    return;

                // Determine next state based on current state
                switch (agentState.State) {
                    case AgentStateType.Idle:
                        agentState.State = AgentStateType.Walk;
                        break;
                    case AgentStateType.Walk:
                        agentState.State = AgentStateType.Run;
                        break;
                    case AgentStateType.Run:
                        agentState.State = AgentStateType.Idle;
                        break;
                }

                agentState.TimeInState = 0f;
            }
        }

        public void OnUpdate(ref SystemState state) {

            new StateTransitionJob {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.ScheduleParallel();
        }
    }
}