using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct WalkStateSystem : ISystem {

        // TODO: make this a job
        [BurstCompile]
        public void OnUpdate(ref SystemState systemState) {

            foreach (var (zombieState, walk, agentBody)
                    in SystemAPI
                        .Query<RefRW<ZombieStateData>, RefRW<WalkStateData>, RefRW<AgentBody>>()
                        .WithAll<WalkStateTag>()) {

                // set new random target position
                if (!walk.ValueRO.TargetIsSet) {
                    Random random = walk.ValueRO.random;

                    float3 randomDirection = new float3(random.NextFloat(-1f, 1f), 0, random.NextFloat(-1f, 1f));
                    randomDirection = math.normalize(randomDirection);

                    walk.ValueRW.targetPosition =
                        walk.ValueRO.originPosition +
                        randomDirection * random.NextFloat(walk.ValueRO.distanceMin, walk.ValueRO.distanceMax);

                    walk.ValueRW.random = random; // Update the random state

                    agentBody.ValueRW.SetDestination(walk.ValueRO.targetPosition);
                    walk.ValueRW.TargetIsSet = true;
                    walk.ValueRW.TargetIsReached = false;
                }

                // If agentbody has reached target, then send signal to controller to set new state.
                if (walk.ValueRO.TargetIsSet && agentBody.ValueRO.IsStopped) {
                    walk.ValueRW.TargetIsSet = false;
                    walk.ValueRW.TargetIsReached = true;
                }
            }
        }
    }
}