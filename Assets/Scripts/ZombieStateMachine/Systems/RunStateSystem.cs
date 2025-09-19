using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct RunStateSystem : ISystem {

        [BurstCompile]
        private partial struct RunStateJob : IJobEntity {
            public float DeltaTime;

            public void Execute(ref ZombieStateData agentState) {
                if (agentState.State != ZombieStateType.Run) { return; }

                // run logic here
            }
        }

        public void OnUpdate(ref SystemState state) {
            var job = new RunStateJob {
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            job.ScheduleParallel();
        }
    }
}