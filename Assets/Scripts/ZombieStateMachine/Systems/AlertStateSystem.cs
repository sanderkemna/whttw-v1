using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct AlertStateSystem : ISystem {
        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var alertStateJob = new AlertStateJob {
                DeltaTime = deltaTime,
            };
            state.Dependency = alertStateJob.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(AlertStateTag))]
    public partial struct AlertStateJob : IJobEntity {
        public float DeltaTime;

        [BurstCompile]
        public void Execute(ref AlertStateData alert, ref AgentLocomotion agentLocomotion, ref AgentBody agentBody, LocalTransform transform) {

            // upon entry for the first time, set the triggered to true
            if (!alert.IsTriggered) {
                alert.IsTriggered = true;
                alert.AlertIntensity = 1.0f;
                alert.AlertDuration = 0f;

                // move to a new target fast
                agentLocomotion.Speed = alert.MaxSpeed;

                Random random = alert.random;
                float3 randomDirection = new float3(
                    random.NextFloat(-1f, 1f),
                    0,
                    random.NextFloat(-1f, 1f)
                );
                randomDirection = math.normalize(randomDirection);

                alert.targetPosition = alert.originPosition +
                    randomDirection * random.NextFloat(alert.distanceMin, alert.distanceMax);

                alert.random = random; // Update the random state

                agentBody.SetDestination(alert.targetPosition);
            }

            alert.AlertDuration += DeltaTime;

            UpdateAlertIntensity(ref alert);

            // Check if alert should be cleared
            if (ShouldClearAlert(ref alert)) {
                ClearAlert(ref alert);
            }
        }

        [BurstCompile]
        private void UpdateAlertIntensity(ref AlertStateData alertState) {
            float intensityDecay = alertState.IntensityDecayRate * DeltaTime;
            alertState.AlertIntensity = math.max(0.0f, alertState.AlertIntensity - intensityDecay);
        }

        [BurstCompile]
        private bool ShouldClearAlert(ref AlertStateData alertState) {
            // Clear alert if:
            // 1. Alert intensity has dropped to zero
            // 2. Alert duration is at maximum time
            if (alertState.AlertIntensity <= 0.0f)
                return true;

            if (alertState.AlertDuration >= alertState.MaxAlertDuration)
                return true;

            return false;
        }

        [BurstCompile]
        private void ClearAlert(ref AlertStateData alertState) {
            alertState.IsTriggered = false;
            alertState.AlertIntensity = 0.0f;
            alertState.AlertDuration = 0.0f;
        }
    }
}