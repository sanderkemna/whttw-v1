using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct IdleStateSystem : ISystem {

    [BurstCompile]
    private partial struct IdleStateJob : IJobEntity {
        public float DeltaTime;

        public void Execute(ref AgentStateData agentState) {
            if (agentState.Value != AgentStateType.Idle)
                return;

            // idle logic here
        }
    }

    public void OnUpdate(ref SystemState state) {
        var job = new IdleStateJob {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
    }
}