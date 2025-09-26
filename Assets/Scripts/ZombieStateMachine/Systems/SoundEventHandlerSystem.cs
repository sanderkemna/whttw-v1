using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
            SoundEventsManager.Instance.OnSoundMade.AddListener(OnSoundMadeReceived);
        }
    }

    protected override void OnDestroy() {
        if (SoundEventsManager.Instance != null) SoundEventsManager.Instance.OnSoundMade.RemoveListener(OnSoundMadeReceived);
        if (pendingEvents.IsCreated) pendingEvents.Dispose();
        if (currentFrameEvents.IsCreated) currentFrameEvents.Dispose();
    }

    // Called by global event system
    private void OnSoundMadeReceived(SoundEventData eventData) {
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

        var footstepDetectionJob = new FootstepDetectionJob {
            SoundEvents = currentFrameEvents.AsArray(),
            CurrentTime = (float)SystemAPI.Time.ElapsedTime
        };

        Dependency = footstepDetectionJob.ScheduleParallel(Dependency);
    }
}

[BurstCompile]
public partial struct FootstepDetectionJob : IJobEntity {
    [ReadOnly] public NativeArray<SoundEvent> SoundEvents;
    [ReadOnly] public float CurrentTime;

    [BurstCompile]
    public void Execute(ref AlertStateData alertState) {
        UnityEngine.Debug.Log("Im triggered!");
    }

    //[BurstCompile]
    //public void Execute(ref AlertStateData alertState, ref FootstepListener listener,
    //    in LocalTransform transform) {
    //    if (alertState.IsTriggered) return;

    //    for (int i = 0; i < FootstepEvents.Length; i++) {
    //        var footstep = FootstepEvents[i];
    //        float distance = math.distance(transform.Position, footstep.Position);

    //        if (distance <= listener.HearingRange) {
    //            float hearingFactor = CalculateHearingFactor(distance, listener.HearingRange,
    //                footstep.Intensity, listener.HearingSensitivity);

    //            if (hearingFactor > 0.3f) {
    //                TriggerNoiseAlert(ref alertState, ref listener, footstep, hearingFactor, CurrentTime);
    //                break;
    //            }
    //        }
    //    }
    //}

    //[BurstCompile]
    //private static float CalculateHearingFactor(float distance, float maxRange,
    //    float noiseIntensity, float sensitivity) {
    //    float distanceFactor = 1.0f - (distance / maxRange);
    //    return distanceFactor * noiseIntensity * sensitivity;
    //}

    //[BurstCompile]
    //private static void TriggerNoiseAlert(ref AlertStateData alertState, ref FootstepListener listener,
    //    FootstepEvent footstep, float hearingFactor, float currentTime) {
    //    alertState.IsTriggered = true;
    //    alertState.AlertType = AlertType.NoiseHeard;
    //    alertState.AlertDuration = 5.0f + (hearingFactor * 5.0f);
    //    alertState.AlertIntensity = hearingFactor;
    //    alertState.IntensityDecayRate = 0.2f;

    //    alertState.HasTarget = true;
    //    alertState.TargetPosition = footstep.Position;
    //    alertState.HasReachedTarget = false;
    //    alertState.ReachThreshold = 2.0f;

    //    alertState.InvestigationDuration = 3.0f;
    //    alertState.InvestigationTimer = 0.0f;
    //    alertState.AlertMoveSpeed = 3.5f;

    //    listener.LastHeardTime = currentTime;
    //}
}
