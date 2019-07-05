using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveChain : MonoBehaviour {

    Transform[] cubes;

    int depth;
    List<Transform[]> chains = new List<Transform[]> ();
    List<Vector3[]> initPos = new List<Vector3[]> ();

    void Start () {
        cubes = GetComponent<SphereGenerator> ().GetTransforms ();

        depth = GetDepth (cubes[0]);
        for (int i = 0; i < cubes.Length; i++) {
            var chain = cubes[i].GetComponentsInChildren<Transform> ();
            chains.Add (chain);

            var pos = new Vector3[depth];
            for (int j = 0; j < depth; j++) {
                pos[j] = chain[j].localPosition;
            }
            initPos.Add (pos);
        }
    }

    void Update () {
        for (int i = 0; i < cubes.Length; i++) {
            var chain = chains[i];
            for (int j = 1; j < depth; j++) {
                var t = 1f * j / depth;
                var pos = initPos[i][j] + new Vector3 (Mathf.Cos (Time.realtimeSinceStartup + t), Mathf.Sin (Time.realtimeSinceStartup + t));
                chain[j].localPosition = pos;
            }
        }
    }

    int GetDepth (Transform root) {
        var t = root.GetComponentsInChildren<Transform> ();
        return t.Length;
    }
}