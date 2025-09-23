using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

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
        public void Execute(ref AlertStateData alertState) {

            // upon entry for the first time, set the triggered to true
            if (!alertState.IsTriggered) {
                alertState.IsTriggered = true;
                alertState.AlertIntensity = 1.0f;
                alertState.AlertDuration = 0f;
            }

            alertState.AlertDuration += DeltaTime;

            // Update alert intensity over time
            //float intensityDecay = alertState.IntensityDecayRate * DeltaTime;
            //alertState.AlertIntensity = math.max(0.0f, alertState.AlertIntensity - intensityDecay);
            UpdateAlertIntensity(ref alertState);

            // Check if alert should be cleared
            if (ShouldClearAlert(ref alertState)) {
                ClearAlert(ref alertState);
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