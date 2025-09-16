using Unity.Entities;
using UnityEngine;

public class AgentStateAuthoring : MonoBehaviour {

    [Tooltip("The starting state of the zombie.")]
    [SerializeField] private AgentStateType startingState;

    class Baker : Baker<AgentStateAuthoring> {
        public override void Bake(AgentStateAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AgentStateData() {
                Value = authoring.startingState,
            });
        }
    }
}

public struct AgentStateData : IComponentData {
    public AgentStateType Value;
    public float TimeInState;
}