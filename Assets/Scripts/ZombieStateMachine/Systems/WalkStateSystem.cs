using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct WalkStateSystem : ISystem {

    [BurstCompile]
    private partial struct WalkStateJob : IJobEntity {
        public float DeltaTime;

        public void Execute(ref AgentStateData agentState) {
            if (agentState.Value != AgentStateType.Walk)
                return;

            // Walk logic here
        }
    }

    public void OnUpdate(ref SystemState state) {
        var job = new WalkStateJob {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
    }
}