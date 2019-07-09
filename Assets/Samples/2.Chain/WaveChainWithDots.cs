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
public struct Owner : IComponentData {
    public Entity Value;
    public int Index;
}

[System.Serializable]
public struct ChainRoot : IComponentData {
    public int Length;
}

[System.Serializable]
public struct ChainData {
    public float3 InitLocalPosition;
    public float3 LocalPosition;
}

// [UpdateAfter (typeof (TransformSystem))]
[UpdateAfter (typeof (UnityEngine.Experimental.PlayerLoop.PreLateUpdate))]
public class UpdateChainDataSystem : JobComponentSystem {

    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        public ComponentDataArray<ChainRoot> chainRoot;
        public FixedArrayArray<ChainData> chainDataArrayArray;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelFor {
        public float time;
        public ComponentDataArray<ChainRoot> chainRoot;
        public FixedArrayArray<ChainData> chainDataArrayArray;

        public void Execute (int i) {
            var chainDataArray = chainDataArrayArray[i];
            var localGravity = new float3 (0, 0, 1) * 5;
            for (int j = 0; j < 10; j++) {
                var t = 1f * j / chainRoot[i].Length;
                var chainData = chainDataArray[j];
                chainData.LocalPosition = chainData.InitLocalPosition + new float3 (math.cos (time + t), math.sin (time + t), 0);
                chainDataArray[j] = chainData;
            }
        }
    }

    [Inject] ComponentGroup componentGroup;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            time = Time.realtimeSinceStartup,
            chainRoot = componentGroup.chainRoot,
            chainDataArrayArray = componentGroup.chainDataArrayArray,
        };
        var handle = job.Schedule (componentGroup.Length, 1);
        handle.Complete ();
        return handle;
    }
}

[UpdateAfter (typeof (UpdateChainDataSystem))]
public class ApplyChainDataSystem : JobComponentSystem {

    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        [ReadOnly] public ComponentDataArray<Owner> owner;
        public TransformAccessArray transformAccessArray;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelForTransform {
        [ReadOnly] public ComponentDataArray<Owner> owner;
        [ReadOnly] public FixedArrayFromEntity<ChainData> chainDataArrayArray;

        public void Execute (int i, TransformAccess transform) {
            if (!chainDataArrayArray.Exists (owner[i].Value)) {
                return;
            }

            var chainData = chainDataArrayArray[owner[i].Value][owner[i].Index];
            transform.localPosition = chainData.LocalPosition;
        }
    }

    [Inject] ComponentGroup componentGroup;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            owner = componentGroup.owner,
            chainDataArrayArray = GetFixedArrayFromEntity<ChainData> (true),
        };
        var handle = job.Schedule (componentGroup.transformAccessArray);
        handle.Complete ();
        return handle;
    }
}

public class WaveChainWithDots : MonoBehaviour {

    int depth;
    Transform[] cubes;

    EntityManager entityManager;
    EntityArchetype entityArchetype;

    void Start () {
        cubes = GetComponent<SphereGenerator> ().GetTransforms ();
        depth = GetDepth (cubes[0]);

        entityManager = World.Active.GetExistingManager<EntityManager> ();
        entityArchetype = entityManager.CreateArchetype (
            ComponentType.Create<ChainRoot> (),
            ComponentType.FixedArray (typeof (ChainData), depth)
        );

        var time = Time.realtimeSinceStartup;
        for (int i = 0; i < cubes.Length; i++) {
            var entity = entityManager.CreateEntity (entityArchetype);
            entityManager.SetComponentData (entity, new ChainRoot () { Length = depth });

            var chain = cubes[i].GetComponentsInChildren<Transform> ();
            var chainDatas = entityManager.GetFixedArray<ChainData> (entity);
            for (int j = 0; j < depth; j++) {
                chainDatas[j] = new ChainData () { InitLocalPosition = chain[j].localPosition };
            }

            for (int j = 0; j < depth; j++) {
                // var subEntity = chain[j].gameObject.AddComponent<GameObjectEntity> ().Entity;
                var subEntity = GameObjectEntity.AddToEntityManager (entityManager, chain[j].gameObject); // faster initialization
                entityManager.AddComponentData (subEntity, new Owner () { Value = entity, Index = j });
            }
        }
        Debug.Log (Time.realtimeSinceStartup - time);
    }

    int GetDepth (Transform root) {
        var t = root.GetComponentsInChildren<Transform> ();
        return t.Length;
    }
}