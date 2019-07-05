using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class GravityWithJob : MonoBehaviour {

    Transform[] cubes;
    TransformAccessArray transformsAccess;
    NativeArray<float3> velocity;

    [BurstCompile]
    struct WaveParallelJob : IJobParallelForTransform {
        [ReadOnly] public float deltaTime;
        public NativeArray<float3> velocity;

        public void Execute (int i, TransformAccess transform) {
            float3 pos = transform.position;
            float3 v = velocity[i] - pos / (math.lengthSquared (pos) + 0.01f);
            transform.position = pos + v * deltaTime;
            velocity[i] = v;
        }
    }

    void Start () {
        cubes = GetComponent<GridGenerator> ().GetTransforms ();

        transformsAccess = new TransformAccessArray (cubes);
        velocity = new NativeArray<float3> (cubes.Length, Allocator.Persistent);
    }

    void Update () {
        var job = new WaveParallelJob () {
            deltaTime = Time.deltaTime,
            velocity = velocity
        };

        var jobHandler = job.Schedule (transformsAccess);
        jobHandler.Complete ();
    }

    void OnDestroy () {
        if (transformsAccess.isCreated) {
            transformsAccess.Dispose ();
            velocity.Dispose ();
        }
    }
}