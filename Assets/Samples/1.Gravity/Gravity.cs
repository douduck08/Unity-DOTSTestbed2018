using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour {

    Transform[] cubes;
    Vector3[] velocity;

    void Start () {
        cubes = GetComponent<CubeGenerator> ().GetCubes ();
        velocity = new Vector3[cubes.Length];
    }

    void Update () {
        for (int i = 0; i < cubes.Length; i++) {
            var pos = cubes[i].transform.position;
            velocity[i] += -pos / (pos.sqrMagnitude + 0.01f);
            cubes[i].transform.Translate (velocity[i] * Time.deltaTime);
        }
    }
}