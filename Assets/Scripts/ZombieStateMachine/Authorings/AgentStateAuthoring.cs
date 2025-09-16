using Unity.Entities;
using UnityEngine;

namespace WHTTW.ZombieStateMachine {

    public class AgentStateAuthoring : MonoBehaviour {

        [Tooltip("The starting state of the zombie.")]
        [SerializeField] private AgentStateType startingState;

        class Baker : Baker<AgentStateAuthoring> {
            public override void Bake(AgentStateAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AgentStateData() {
                    State = authoring.startingState,
                });
            }
        }
    }

    public struct AgentStateData : IComponentData {
        public AgentStateType State;
        public float TimeInState;
    }
}