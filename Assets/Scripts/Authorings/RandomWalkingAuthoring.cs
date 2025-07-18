using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RandomWalkingAuthoring : MonoBehaviour {

    //[Tooltip("The next position this unit will move towards, is set by the system.")]
    //public float3 targetPosition;
    //[Tooltip("The center of the squares of min and max bounding box of walking distances.")]
    //public float3 originPosition;
    //[Tooltip("The minimum bounding box of walking distances.")]
    public float distanceMin;
    [Tooltip("The maximum bounding box of walking distances.")]
    public float distanceMax;
    [Tooltip("The max linger time in [s] of the unit.")]
    public float lingerTimerMax;

    public class Baker : Baker<RandomWalkingAuthoring> {
        public override void Bake(RandomWalkingAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RandomWalking() {
                targetPosition = new float3(authoring.transform.position.x + 1, authoring.transform.position.y, authoring.transform.position.z),
                originPosition = authoring.transform.position,
                distanceMin = authoring.distanceMin,
                distanceMax = authoring.distanceMax,
                random = new Unity.Mathematics.Random((uint)entity.Index),
                lingerTimerMax = authoring.lingerTimerMax
            });
        }
    }
}

public struct RandomWalking : IComponentData {
    public float3 targetPosition;
    public float3 originPosition;
    public float distanceMin;
    public float distanceMax;
    public Unity.Mathematics.Random random;
    public float lingerTimer;
    public float lingerTimerMax;
}