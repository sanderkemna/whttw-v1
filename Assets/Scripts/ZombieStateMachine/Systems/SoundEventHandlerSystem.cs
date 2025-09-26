using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using WHTTW.SoundEventsManager;
using WHTTW.ZombieStateMachine;

/// <summary>
/// System that listens to sound events happening globaly. If a zombie is close by it will 
/// trigger the alert state.
/// </summary>
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class SoundEventHandlerSystem : SystemBase {
    private NativeList<SoundEvent> pendingEvents;
    private NativeList<SoundEvent> currentFrameEvents;

    protected override void OnCreate() {
        pendingEvents = new NativeList<SoundEvent>(16, Allocator.Persistent);
        currentFrameEvents = new NativeList<SoundEvent>(16, Allocator.Persistent);

        SubscribeToManagerIfNotAlready();
    }

    private void SubscribeToManagerIfNotAlready() {
        // Subscribe to global footstep events
        if (SoundEventsManager.Instance != null) {
            SoundEventsManager.Instance.OnSoundMade.AddListener(OnSoundReceived);
        }
    }

    protected override void OnDestroy() {
        if (SoundEventsManager.Instance != null) SoundEventsManager.Instance.OnSoundMade.RemoveListener(OnSoundReceived);
        if (pendingEvents.IsCreated) pendingEvents.Dispose();
        if (currentFrameEvents.IsCreated) currentFrameEvents.Dispose();
    }

    private void OnSoundReceived(SoundEventData eventData) {
        if (pendingEvents.IsCreated) {
            pendingEvents.Add(eventData.ToDOTSEvent());
        }
    }

    protected override void OnUpdate() {
        SubscribeToManagerIfNotAlready();

        currentFrameEvents.Clear();
        currentFrameEvents.AddRange(pendingEvents.AsArray());
        pendingEvents.Clear();

        if (currentFrameEvents.Length == 0) return;

        var soundDetectionJob = new SoundDetectionJob {
            SoundEvents = currentFrameEvents.AsArray(),
            CurrentTime = (float)SystemAPI.Time.ElapsedTime
        };

        Dependency = soundDetectionJob.ScheduleParallel(Dependency);
    }
}

[BurstCompile]
public partial struct SoundDetectionJob : IJobEntity {
    [ReadOnly] public NativeArray<SoundEvent> SoundEvents;
    [ReadOnly] public float CurrentTime;

    [BurstCompile]
    public void Execute(ref AlertStateData alertState, ref SoundEventListener listener, in LocalTransform transform) {

        for (int i = 0; i < SoundEvents.Length; i++) {
            var sound = SoundEvents[i];
            float distance = math.distance(transform.Position, sound.Position);

            if (distance <= listener.HearingRange) {
                float hearingFactor = CalculateHearingFactor(distance, listener.HearingRange, sound.Intensity, listener.HearingSensitivity);

                if (hearingFactor > 0.3f) {
                    alertState.IsTriggered = true;
                    alertState.AlertIntensity += hearingFactor;
                    alertState.HasTarget = true;
                    alertState.TargetPosition = sound.Position;
                    alertState.HasReachedTarget = false;

                    break;
                }
            }
        }
    }

    [BurstCompile]
    private static float CalculateHearingFactor(float distance, float maxRange,
        float noiseIntensity, float sensitivity) {
        float distanceFactor = 1.0f - (distance / maxRange);
        return distanceFactor * noiseIntensity * sensitivity;
    }
}
