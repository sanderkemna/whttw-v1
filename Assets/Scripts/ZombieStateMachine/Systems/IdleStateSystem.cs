using Unity.Burst;
using Unity.Entities;

namespace WHTTW.ZombieStateMachine {

    [BurstCompile]
    public partial struct IdleStateSystem : ISystem {

        [BurstCompile]
        [WithAll(typeof(IdleStateTag))]
        private partial struct IdleStateJob : IJobEntity {

            public void Execute(ref IdleStateData idle) {

                // noop, only the rukhanka animation is happening here.
            }
        }

        public void OnUpdate(ref SystemState state) {
            new IdleStateJob().ScheduleParallel();
        }
    }
}