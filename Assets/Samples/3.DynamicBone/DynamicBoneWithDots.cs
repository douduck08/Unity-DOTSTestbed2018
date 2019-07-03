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
public struct DynamicBoneRoot : IComponentData {
    public int length;
    public quaternion rootInvertRotation;
    public float3 objectMove;
    public float3 objectPrevPosition;
}

[System.Serializable]
public struct DynamicBoneParticle {
    public int parentIndex;
    public float3 position;
    public float3 prevPosition;
    public float3 positionDiff;

    public float restLength;
    public float inert;
    public float damping;
    public float elasticity;
}

[System.Serializable]
public struct DynamicBoneTransform : IComponentData {
    public Entity owner;
    public int index;
    public float3 initLocalPosition;
    public quaternion initLocalRotation;
}

public class DynamicBonePreUpdateSystem : JobComponentSystem {
    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        [ReadOnly] public ComponentDataArray<DynamicBoneTransform> dynamicBoneTransform;
        public TransformAccessArray transformAccessArray;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelForTransform {
        [ReadOnly] public ComponentDataArray<DynamicBoneTransform> dynamicBoneTransform;

        public void Execute (int i, TransformAccess transform) {
            transform.localPosition = dynamicBoneTransform[i].initLocalPosition;
            transform.localRotation = dynamicBoneTransform[i].initLocalRotation;
        }
    }

    [Inject] ComponentGroup componentGroup;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            dynamicBoneTransform = componentGroup.dynamicBoneTransform,
        };
        var handle = job.Schedule (componentGroup.transformAccessArray);
        handle.Complete ();
        return handle;
    }
}

[UpdateAfter (typeof (UnityEngine.Experimental.PlayerLoop.PreLateUpdate))]
public class DynamicBoneUpdateSystem : JobComponentSystem {
    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        public ComponentDataArray<DynamicBoneRoot> dynamicBoneRoot;
        public FixedArrayArray<DynamicBoneParticle> dynamicBoneParticleArrayArray;
        public TransformAccessArray transformAccessArray;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelForTransform {
        public ComponentDataArray<DynamicBoneRoot> dynamicBoneRoot;
        public FixedArrayArray<DynamicBoneParticle> dynamicBoneParticleArrayArray;

        public void Execute (int i, TransformAccess transform) {
            float3 currentObjectPos = transform.position;
            var dynamicBone = dynamicBoneRoot[i];
            dynamicBone.objectMove = currentObjectPos - dynamicBone.objectPrevPosition;
            dynamicBone.objectPrevPosition = currentObjectPos;
            dynamicBoneRoot[i] = dynamicBone;

            var dynamicBoneParticle = dynamicBoneParticleArrayArray[i];
            var rootParticle = dynamicBoneParticle[0];
            rootParticle.prevPosition = rootParticle.position;
            rootParticle.position = currentObjectPos;
            dynamicBoneParticle[0] = rootParticle;

            quaternion deltaRot = transform.rotation * dynamicBone.rootInvertRotation;
            for (int idx = 1; idx < dynamicBone.length; idx++) {
                var particle = dynamicBoneParticle[idx];
                var parentParticle = dynamicBoneParticle[particle.parentIndex];

                // verlet integration
                float3 posDiff = particle.position - particle.prevPosition;
                float3 moveInert = dynamicBone.objectMove * particle.inert;
                particle.prevPosition = particle.position + moveInert;
                particle.position += posDiff * (1 - particle.damping) + moveInert;

                // keep shape
                if (particle.elasticity > 0) {
                    float3 restPos = parentParticle.position + math.mul (deltaRot, particle.positionDiff);
                    float3 invertDiff = restPos - particle.position;
                    particle.position += invertDiff * particle.elasticity;
                }

                // keep length
                float3 boneDir = parentParticle.position - particle.position;
                float leng = math.length (boneDir);
                if (leng > 0) {
                    particle.position += boneDir * ((leng - particle.restLength) / leng);
                }

                dynamicBoneParticle[idx] = particle;
            }
        }
    }

    [Inject] ComponentGroup componentGroup;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            dynamicBoneRoot = componentGroup.dynamicBoneRoot,
            dynamicBoneParticleArrayArray = componentGroup.dynamicBoneParticleArrayArray,
        };
        var handle = job.Schedule (componentGroup.transformAccessArray);
        handle.Complete ();
        return handle;
    }
}

[UpdateAfter (typeof (DynamicBoneUpdateSystem))]
public class DynamicBoneApplyTransformSystem : JobComponentSystem {
    struct ComponentGroup {
        [ReadOnly] public readonly int Length;
        [ReadOnly] public EntityArray entity;
        [ReadOnly] public ComponentDataArray<DynamicBoneTransform> dynamicBoneTransform;
        public TransformAccessArray transformAccessArray;
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelForTransform {
        [ReadOnly] public ComponentDataArray<DynamicBoneTransform> dynamicBoneTransform;
        [ReadOnly] public FixedArrayFromEntity<DynamicBoneParticle> dynamicBoneParticleFromEntity;

        public void Execute (int i, TransformAccess transform) {
            if (!dynamicBoneParticleFromEntity.Exists (dynamicBoneTransform[i].owner)) {
                return;
            }

            var dynamicBoneParticle = dynamicBoneParticleFromEntity[dynamicBoneTransform[i].owner];
            var particle = dynamicBoneParticle[dynamicBoneTransform[i].index];
            var parentParticle = dynamicBoneParticle[particle.parentIndex];

            // // if (pp.bone.childCount <= 1) {
            // // do not modify bone orientation if has more then one child
            // Vector3 v = transform.localPosition;
            // Vector3 v2 = particle.position - particle.position;
            // Quaternion rot = Quaternion.FromToRotation (v, pp.bone.InverseTransformDirection (v2));
            // pp.bone.localRotation = rot * pp.bone.localRotation;
            // // }
            transform.position = particle.position;
        }
    }

    [Inject] ComponentGroup componentGroup;

    protected override JobHandle OnUpdate (JobHandle inputDeps) {
        var job = new UpdateJob () {
            dynamicBoneTransform = componentGroup.dynamicBoneTransform,
            dynamicBoneParticleFromEntity = GetFixedArrayFromEntity<DynamicBoneParticle> (true),
        };
        var handle = job.Schedule (componentGroup.transformAccessArray);
        handle.Complete ();
        return handle;
    }
}

public class DynamicBoneWithDots : MonoBehaviour {

    public Transform root;

    EntityManager entityManager;
    // EntityArchetype entityArchetype;

    void Start () {
        var particles = new List<DynamicBoneParticle> ();
        var bones = new List<Transform> ();
        GenerateParticles (particles, bones);

        entityManager = World.Active.GetExistingManager<EntityManager> ();
        // entityArchetype = entityManager.CreateArchetype (
        //     ComponentType.Create<DynamicBoneRoot> (),
        //     ComponentType.FixedArray (typeof (DynamicBoneParticle), particles.Count)
        // );

        // var entity = entityManager.CreateEntity (entityArchetype);
        var entity = bones[0].gameObject.AddComponent<GameObjectEntity> ().Entity;
        entityManager.AddComponentData (entity, new DynamicBoneRoot () {
            length = particles.Count,
                rootInvertRotation = Quaternion.Inverse (root.rotation),
                objectMove = new float3 (),
                objectPrevPosition = root.position
        });

        entityManager.AddComponent (entity, ComponentType.FixedArray (typeof (DynamicBoneParticle), particles.Count));
        var particleArray = entityManager.GetFixedArray<DynamicBoneParticle> (entity);
        for (int i = 0; i < particles.Count; i++) {
            particleArray[i] = particles[i];
        }

        for (int i = 1; i < particles.Count; i++) {
            var boneEntity = bones[i].gameObject.AddComponent<GameObjectEntity> ().Entity;
            entityManager.AddComponentData (boneEntity, new DynamicBoneTransform () {
                owner = entity,
                    index = i,
                    initLocalPosition = bones[i].localPosition,
                    initLocalRotation = bones[i].localRotation,
            });
        }
    }

    void GenerateParticles (List<DynamicBoneParticle> particles, List<Transform> bones) {
        AppendParticles (particles, bones, root, -1);
    }

    void AppendParticles (List<DynamicBoneParticle> particles, List<Transform> bones, Transform bone, int parentIndex) {
        var particle = new DynamicBoneParticle ();
        particle.parentIndex = parentIndex;
        particle.position = particle.prevPosition = bone.position;

        if (parentIndex >= 0) {
            var diff = particle.position - particles[parentIndex].position;
            particle.positionDiff = diff;
            particle.restLength = math.length (diff);
        }
        particle.inert = 0.5f;
        particle.damping = 0.2f;
        particle.elasticity = 0.05f;

        int index = particles.Count;
        particles.Add (particle);
        bones.Add (bone);

        for (int i = 0; i < bone.childCount; ++i) {
            var childBone = bone.GetChild (i);
            AppendParticles (particles, bones, childBone, index);
        }
    }
}