using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

partial struct RandomWalkingSystem : ISystem {

    private const float REACHED_TARGET_DISTANCE = .2f; // Threshold for reaching the target position

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState) {
        foreach (var (randomWalking, agentBody) in SystemAPI.Query<RefRW<RandomWalking>, RefRW<AgentBody>>()) {

            bool firstTimeDone = false;

            if (!firstTimeDone) {
                agentBody.ValueRW.SetDestination(randomWalking.ValueRO.targetPosition);
                firstTimeDone = true;
            }

            if (agentBody.ValueRO.RemainingDistance <= REACHED_TARGET_DISTANCE) {

                // reached target, linger a bit
                randomWalking.ValueRW.lingerTimer -= SystemAPI.Time.DeltaTime;
                if (randomWalking.ValueRO.lingerTimer > 0f) {
                    continue;
                }
                randomWalking.ValueRW.lingerTimer = randomWalking.ValueRO.lingerTimerMax;

                // set new target position
                Random random = randomWalking.ValueRO.random;

                float3 randomDirection = new float3(random.NextFloat(-1f, 1f), 0, random.NextFloat(-1f, 1f));
                randomDirection = math.normalize(randomDirection);

                randomWalking.ValueRW.targetPosition =
                    randomWalking.ValueRO.originPosition +
                    randomDirection * random.NextFloat(randomWalking.ValueRO.distanceMin, randomWalking.ValueRO.distanceMax);

                randomWalking.ValueRW.random = random; // Update the random state

            } else {
                agentBody.ValueRW.SetDestination(randomWalking.ValueRO.targetPosition);
            }
        }
    }
}
