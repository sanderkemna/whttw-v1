using ProjectDawn.Navigation;
using Rukhanka;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using WHTTW.ZombieStateMachine;

/// <summary>
/// Uses character input from Animator to trigger Rukhanka animations.
/// </summary>
[WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
[UpdateBefore(typeof(RukhankaAnimationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class RukhankaAnimatorSystem : SystemBase {
    static readonly string floatParam1Name = "forwardSpeed";
    static FastAnimatorParameter paramForwardSpeed = new(floatParam1Name);

    static readonly string paramIdleAnimationStateName = "idleAnimationId";
    static FastAnimatorParameter paramIdleAnimationState = new(paramIdleAnimationStateName);

    protected override void OnUpdate() {

        var forwardSpeedJob = new ForwardSpeedAnimatorJob() {
            paramForwardSpeed = paramForwardSpeed,
        };

        Dependency = forwardSpeedJob.ScheduleParallel(Dependency);

        var idleAnimatorJob = new IdleAnimatorJob() {
            paramIdleAnimationState = paramIdleAnimationState,
            deltaTime = SystemAPI.Time.DeltaTime,
            random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue)),
        };

        Dependency = idleAnimatorJob.ScheduleParallel(Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(WalkStateTag))]
    partial struct ForwardSpeedAnimatorJob : IJobEntity {

        public FastAnimatorParameter paramForwardSpeed;

        void Execute(AnimatorParametersAspect paramAspect, in AgentBody agentBody) {
            if (paramAspect.HasParameter(paramForwardSpeed))
                paramAspect.SetFloatParameter(paramForwardSpeed, math.length(agentBody.Velocity));

        }
    }

    [BurstCompile]
    [WithAll(typeof(IdleStateTag))]
    partial struct IdleAnimatorJob : IJobEntity {
        private static readonly float INTERPOLATION_SPEED = 5f;

        public FastAnimatorParameter paramIdleAnimationState;
        public float deltaTime;
        public Random random;

        /// <summary>
        /// Updates and switches idle state animations of the agent. This would normally be a StateMachineBehaviour 
        /// script in the Animator Controller.
        /// </summary>
        /// <remarks>
        /// Inspiration taken from: https://www.youtube.com/watch?v=OCd7terfNxk - Ketra Games, Add Random "Bored" Idle 
        /// Animations to Your Character (Unity Tutorial)
        /// Rukhanka docs: https://docs.rukhanka.com/Scripting/Events/animator_controller_events
        /// This method determines whether the agent is in an idle state by checking its velocity
        /// and updates the animation parameters accordingly. If the agent transitions into an idle state, it selects an
        /// animation index for idle behavior and smoothly blends the animation parameters over time.  The method also
        /// handles transitions between idle animations and ensures smooth blending of animation states using
        /// interpolation. It processes animation events to determine the timing of state updates and adjusts the idle
        /// state data accordingly.</remarks>
        void Execute(
            AnimatorParametersAspect paramAspect,
            in AgentBody agentBody,
            ref IdleStateData idleData,
            in DynamicBuffer<AnimatorControllerEventComponent> eventController) {

            if (paramAspect.HasParameter(paramIdleAnimationState) && math.length(agentBody.Velocity).Equals(0)) {
                // we are in idle state
                foreach (var evnt in eventController) {
                    if (evnt.eventType != AnimatorControllerEventComponent.EventType.StateUpdate) { continue; }

                    if (!idleData.IsIdle) {
                        idleData.Timer += deltaTime;

                        if (idleData.Timer > idleData.timeUntilIdleAnimationChange && evnt.timeInState % 1 < 0.02f) {
                            // new animation has just started
                            idleData.IsIdle = true;

                            int index = random.NextInt(1, idleData.numberOfIdleAnimations + 1);
                            idleData.BoredAnimationIndex = index * 2 - 1;

                            paramAspect.SetFloatParameter(paramIdleAnimationState, (float)(idleData.BoredAnimationIndex - 1));
                        }
                    } else if (evnt.timeInState % 1 > 0.98f) {
                        // animation is about to finish
                        if (idleData.IsIdle) {
                            idleData.BoredAnimationIndex--;
                        }

                        idleData.IsIdle = false;
                        idleData.Timer = 0;
                    }

                    // smooth blend
                    float current = paramAspect.GetFloatParameter(paramIdleAnimationState);
                    float target = idleData.BoredAnimationIndex;
                    float blended = math.lerp(current, target, deltaTime * INTERPOLATION_SPEED);
                    paramAspect.SetFloatParameter(paramIdleAnimationState, blended);
                }
            }
        }
    }
}