using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[System.Serializable]
public struct Velocity : IComponentData {
    public float3 Value;
}

[UpdateAfter (typeof (TransformSystem))]
public class GravitySystem : JobComponentSystem {

    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        public ComponentDataArray<Position> position;
        public ComponentDataArray<Velocity> velocity;
        public TransformAccessArray transformsAccess;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelForTransform {
        [ReadOnly] public float deltaTime;
        public ComponentDataArray<Position> position;
        public ComponentDataArray<Velocity> velocity;

        public void Execute (int i, TransformAccess transform) {
            var pos = position[i].Value;
            var v = velocity[i].Value - pos / (math.lengthSquared (pos) + 0.01f);
            transform.position = pos + v * deltaTime;
            velocity[i] = new Velocity () { Value = v };
            position[i] = new Position () { Value = pos + v * deltaTime };
        }
    }

    [Inject] ComponentGroup componentGroup;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            deltaTime = Time.deltaTime,
            position = componentGroup.position,
            velocity = componentGroup.velocity
        };
        return job.Schedule (componentGroup.transformsAccess);
    }
}

public class GravityWithDots : MonoBehaviour {

    Transform[] cubes;
    EntityManager entityManager;

    void Start () {
        cubes = GetComponent<GridGenerator> ().GetTransforms ();

        entityManager = World.Active.GetExistingManager<EntityManager> ();

        for (int i = 0; i < cubes.Length; i++) {
            var entity = cubes[i].GetComponent<GameObjectEntity> ().Entity;
            entityManager.AddComponentData (entity, new Position () { Value = cubes[i].position });
            entityManager.AddComponentData (entity, new Velocity ());

        }
    }
}