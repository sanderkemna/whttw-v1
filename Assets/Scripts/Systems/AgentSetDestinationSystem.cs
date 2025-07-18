using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;

partial struct AgentSetDestinationSystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState) {
        foreach (var (destination, body) in SystemAPI.Query<RefRO<SetDestination>, RefRW<AgentBody>>()) {
            body.ValueRW.SetDestination(destination.ValueRO.Value);
        }
    }
}
