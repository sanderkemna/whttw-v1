using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class AgentSetDestinationAuthoring : MonoBehaviour {

    public Transform Target;

    class Baker : Baker<AgentSetDestinationAuthoring> {
        public override void Bake(AgentSetDestinationAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SetDestination {
                Value = authoring.Target.position
            });
        }
    }
}

// ECS component
public struct SetDestination : IComponentData {
    public float3 Value;
}