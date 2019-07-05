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

[UpdateAfter (typeof (TransformSystem))]
public class WavePlaneSystem : JobComponentSystem {

    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        [ReadOnly] public ComponentDataArray<PlaneWaveTag> planeWaveTag;
        public TransformAccessArray transformsAccess;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelForTransform {
        [ReadOnly] public float realtimeSinceStartup;
        [ReadOnly] public float speed;
        [ReadOnly] public float distance;
        [ReadOnly] public float3 cameraPos;

        public void Execute (int i, TransformAccess transform) {
            float3 pos = transform.localPosition;
            if (math.length (cameraPos - pos) < distance) {
                pos.y = Mathf.Sin (realtimeSinceStartup * speed - pos.x) * Mathf.Sin (realtimeSinceStartup * speed - pos.z);
            } else {
                pos.y = 0;
            }
            transform.position = pos;
        }
    }

    [Inject] ComponentGroup componentGroup;

    Camera _mainCamera;
    Camera mainCamera {
        get {
            if (_mainCamera == null) {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            realtimeSinceStartup = Time.realtimeSinceStartup,
            speed = 3f,
            distance = 20f,
            cameraPos = mainCamera.transform.position,
        };
        return job.Schedule (componentGroup.transformsAccess);
    }
}

public class WavePlaneWithDots : MonoBehaviour {

    EntityManager entityManager;

    void Start () {
        entityManager = World.Active.GetExistingManager<EntityManager> ();

        var entity = gameObject.AddComponent<GameObjectEntity> ().Entity;
        entityManager.AddComponentData (entity, new PlaneWaveTag ());
    }
}