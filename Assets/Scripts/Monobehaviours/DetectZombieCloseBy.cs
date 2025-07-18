using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class DetectZombieCloseBy : MonoBehaviour {

    void Update() {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        CollisionWorld collisionWorld = entityQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

        int count = 0;

        var distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);
        if (collisionWorld.OverlapSphere(transform.position, 2f, ref distanceHitList, CollisionFilter.Default)) {
            // hit something within radius
            foreach (DistanceHit distanceHit in distanceHitList) {
                if (entityManager.HasComponent<IdleStateData>(distanceHit.Entity)) {
                    count++;
                }
            }
        }

        Debug.Log($"hit {count} zombies");
    }
}
