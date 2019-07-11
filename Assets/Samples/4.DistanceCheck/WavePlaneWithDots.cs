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
public struct PlaneWaveTag : IComponentData { }

public class WavePlaneBarrier : BarrierSystem { }

[UpdateAfter (typeof (CopyTransformFromGameObjectSystem))]
[UpdateBefore (typeof (CopyTransformToGameObjectSystem))]
public class WavePlaneSystem : JobComponentSystem {

    Camera _mainCamera;
    Camera mainCamera {
        get {
            if (_mainCamera == null) {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    public static bool useTransformJob = true;
    ComponentGroup transformGroup;
    ComponentGroup positionGroup;

    // struct PlaneWaveTagGroup {
    //     [ReadOnly] public ComponentDataArray<PlaneWaveTag> planeWaveTag;
    //     public TransformAccessArray transformsAccess;
    // }

    // [Inject] PlaneWaveTagGroup planeWaveTagGroup;
    [Inject] WavePlaneBarrier barrier;

    protected override void OnCreateManager (int capacity) {
        transformGroup = GetComponentGroup (ComponentType.Create<Transform> (), ComponentType.ReadOnly<PlaneWaveTag> ());
        positionGroup = GetComponentGroup (ComponentType.Create<Position> (), ComponentType.ReadOnly<PlaneWaveTag> ());
    }

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        if (useTransformJob) {
            var job = new TransformUpdateJob () {
                realtimeSinceStartup = Time.realtimeSinceStartup,
                speed = 3f,
                distance = 20f,
                cameraPos = mainCamera.transform.position,
            };
            // inputDeps = job.Schedule (planeWaveTagGroup.transformsAccess, inputDeps);
            inputDeps = job.Schedule (transformGroup.GetTransformAccessArray (), inputDeps);
        } else {
            var job = new PositionUpdateJob () {
                realtimeSinceStartup = Time.realtimeSinceStartup,
                speed = 3f,
                distance = 20f,
                cameraPos = mainCamera.transform.position,
                position = positionGroup.GetComponentDataArray<Position> ()
            };
            inputDeps = job.Schedule (positionGroup.CalculateLength (), 64, inputDeps);
        }

        return inputDeps;
    }

    [BurstCompile]
    struct TransformUpdateJob : IJobParallelForTransform {
        [ReadOnly] public float realtimeSinceStartup;
        [ReadOnly] public float speed;
        [ReadOnly] public float distance;
        [ReadOnly] public float3 cameraPos;

        public void Execute (int i, TransformAccess transform) {
            float3 pos = transform.localPosition;
            if (math.length (cameraPos - pos) < distance) {
                pos.y = math.sin (realtimeSinceStartup * speed - pos.x) * math.sin (realtimeSinceStartup * speed - pos.z);
            } else {
                pos.y = 0;
            }
            transform.position = pos;
        }
    }

    [BurstCompile]
    struct PositionUpdateJob : IJobParallelFor {
        [ReadOnly] public float realtimeSinceStartup;
        [ReadOnly] public float speed;
        [ReadOnly] public float distance;
        [ReadOnly] public float3 cameraPos;
        public ComponentDataArray<Position> position;

        public void Execute (int i) {
            float3 pos = position[i].Value;
            if (math.length (cameraPos - pos) < distance) {
                pos.y = math.sin (realtimeSinceStartup * speed - pos.x) * math.sin (realtimeSinceStartup * speed - pos.z);
            } else {
                pos.y = 0;
            }
            position[i] = new Position () { Value = pos };
        }
    }
}

public class WavePlaneWithDots : MonoBehaviour {

    public bool useTransformJob = true;
    EntityManager entityManager;
    Entity entity;

    void Start () {
        WavePlaneSystem.useTransformJob = useTransformJob;
        entityManager = World.Active.GetExistingManager<EntityManager> ();

        // var entity = gameObject.AddComponent<GameObjectEntity> ().Entity;
        entity = GameObjectEntity.AddToEntityManager (entityManager, gameObject);
        entityManager.AddComponent (entity, typeof (PlaneWaveTag));

        if (!WavePlaneSystem.useTransformJob) {
            entityManager.AddComponent (entity, typeof (Position));
            entityManager.AddComponent (entity, typeof (CopyTransformFromGameObject));
            entityManager.AddComponent (entity, typeof (CopyTransformToGameObject));
        }
    }

    void OnEnable () {
        if (entityManager != null && entityManager.IsCreated) {
            entityManager.AddComponent (entity, typeof (PlaneWaveTag));
        }
    }

    void OnDisable () {
        if (entityManager != null && entityManager.IsCreated) {
            entityManager.RemoveComponent (entity, typeof (PlaneWaveTag));
        }
        var pos = transform.position;
        pos.y = 0;
        transform.position = pos;
    }
}