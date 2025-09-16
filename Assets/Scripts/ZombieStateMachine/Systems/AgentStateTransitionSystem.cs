using Unity.Burst;
using Unity.Entities;

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
            switch (agentState.Value) {
                case AgentStateType.Idle:
                    agentState.Value = AgentStateType.Walk;
                    break;
                case AgentStateType.Walk:
                    agentState.Value = AgentStateType.Run;
                    break;
                case AgentStateType.Run:
                    agentState.Value = AgentStateType.Idle;
                    break;
            }

            agentState.TimeInState = 0f;
        }
    }

    public void OnUpdate(ref SystemState state) {
        var job = new StateTransitionJob {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
    }
}