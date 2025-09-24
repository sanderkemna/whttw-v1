using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using WHTTW.Player;
using WHTTW.ZombieStateMachine;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
public partial class FootstepEventSystem : SystemBase {
    private NativeList<FootstepEvent> pendingEvents;
    private NativeList<FootstepEvent> currentFrameEvents;

    protected override void OnCreate() {
        pendingEvents = new NativeList<FootstepEvent>(16, Allocator.Persistent);
        currentFrameEvents = new NativeList<FootstepEvent>(16, Allocator.Persistent);

        // Subscribe to global footstep events
        PlayerSoundEventsManager.OnFootstepMade.AddListener(OnGlobalFootstepReceived);
    }

    protected override void OnDestroy() {
        // Unsubscribe from global events
        PlayerSoundEventsManager.OnFootstepMade.RemoveListener(OnGlobalFootstepReceived);

        if (pendingEvents.IsCreated) pendingEvents.Dispose();
        if (currentFrameEvents.IsCreated) currentFrameEvents.Dispose();
    }

    // Called by global event system
    private void OnGlobalFootstepReceived(PlayerSoundEventsManager.FootstepEventData eventData) {
        if (pendingEvents.IsCreated) {
            pendingEvents.Add(eventData.ToDOTSEvent());
        }
    }

    protected override void OnUpdate() {
        currentFrameEvents.Clear();
        currentFrameEvents.AddRange(pendingEvents.AsArray());
        pendingEvents.Clear();

        if (currentFrameEvents.Length == 0) return;

        var footstepDetectionJob = new FootstepDetectionJob {
            FootstepEvents = currentFrameEvents.AsArray(),
            CurrentTime = (float)SystemAPI.Time.ElapsedTime
        };

        Dependency = footstepDetectionJob.ScheduleParallel(Dependency);
    }
}

[BurstCompile]
public partial struct FootstepDetectionJob : IJobEntity {
    [ReadOnly] public NativeArray<FootstepEvent> FootstepEvents;
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
