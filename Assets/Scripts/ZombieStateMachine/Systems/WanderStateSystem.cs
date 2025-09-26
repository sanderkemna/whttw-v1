using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct WanderStateSystem : ISystem {

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var walkStateJob = new WanderStateJob();
            state.Dependency = walkStateJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(WanderStateTag))]
    public partial struct WanderStateJob : IJobEntity {

        [BurstCompile]
        public void Execute(ref WanderStateData wander, ref AgentBody agentBody, ref AgentLocomotion agentLocomotion) {

            // Set new random target position
            if (!wander.TargetIsSet) {
                Random random = wander.random;
                float3 randomDirection = new float3(
                    random.NextFloat(-1f, 1f),
                    0,
                    random.NextFloat(-1f, 1f)
                );
                randomDirection = math.normalize(randomDirection);

                wander.targetPosition = wander.originPosition +
                    randomDirection * random.NextFloat(wander.distanceMin, wander.distanceMax);

                wander.random = random; // Update the random state

                agentBody.SetDestination(wander.targetPosition);
                agentLocomotion.Speed = wander.MaxSpeed;
                wander.TargetIsSet = true;
                wander.TargetIsReached = false;
            }

            // If agentbody has reached target, then send signal to controller to set new state
            if (wander.TargetIsSet && agentBody.IsStopped) {
                wander.TargetIsSet = false;
                wander.TargetIsReached = true;
            }
        }
    }
}