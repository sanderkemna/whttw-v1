using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct WalkStateSystem : ISystem {

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var walkStateJob = new WalkStateJob();
            state.Dependency = walkStateJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(WalkStateTag))]
    public partial struct WalkStateJob : IJobEntity {

        [BurstCompile]
        public void Execute(ref WalkStateData walk, ref AgentBody agentBody) {

            // Set new random target position
            if (!walk.TargetIsSet) {
                Random random = walk.random;
                float3 randomDirection = new float3(
                    random.NextFloat(-1f, 1f),
                    0,
                    random.NextFloat(-1f, 1f)
                );
                randomDirection = math.normalize(randomDirection);

                walk.targetPosition = walk.originPosition +
                    randomDirection * random.NextFloat(walk.distanceMin, walk.distanceMax);

                walk.random = random; // Update the random state

                agentBody.SetDestination(walk.targetPosition);
                walk.TargetIsSet = true;
                walk.TargetIsReached = false;
            }

            // If agentbody has reached target, then send signal to controller to set new state
            if (walk.TargetIsSet && agentBody.IsStopped) {
                walk.TargetIsSet = false;
                walk.TargetIsReached = true;
            }
        }
    }
}