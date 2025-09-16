using Unity.Entities;
using UnityEngine;

public class IdleStateDataAuthoring : MonoBehaviour {
    [Tooltip("The time untill the default animation changes to another idle animation. " +
        "This does not change directly but after the default animation has ended.")]
    [SerializeField] private float timeUntilIdleChange;

    [Tooltip("The amount of extra idle animations there are in the blend tree. The default " +
        "idle animation should be on position 0, and then inbetween all other animations. " +
        "You end up with n*2-1 animations in the blend tree.")]
    [SerializeField] private int numberOfIdleAnimations;

    class Baker : Baker<IdleStateDataAuthoring> {
        public override void Bake(IdleStateDataAuthoring authoring) {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new IdleStateData {
                Timer = 0,
                IsIdle = false,
                BoredAnimationIndex = 0,
                timeUntilIdleChange = authoring.timeUntilIdleChange,
                numberOfIdleAnimations = authoring.numberOfIdleAnimations
            });
        }
    }
}

// ECS component
public struct IdleStateData : IComponentData {
    public float Timer;
    public bool IsIdle;
    public int BoredAnimationIndex;

    // set by the Authoring
    public float timeUntilIdleChange;
    public int numberOfIdleAnimations;
}