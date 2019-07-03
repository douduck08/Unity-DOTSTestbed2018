using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeGenerator : MonoBehaviour {

    [System.Serializable]
    public struct IntVector3 { public int x, y, z; }

    public GameObject cubePrefab;
    public IntVector3 cubeNumber;
    public Vector3 gridSize;

    Transform[] cubes;

    void Awake () {
        QualitySettings.vSyncCount = 0;
        GenerateCubes ();
    }

    void GenerateCubes () {
        if (cubePrefab == null) {
            return;
        }

        cubes = new Transform[cubeNumber.x * cubeNumber.y * cubeNumber.z];
        var posOrigin = new Vector3 (gridSize.x * cubeNumber.x, gridSize.y * cubeNumber.y, gridSize.z * cubeNumber.z) * -0.5f;
        for (int x = 0; x < cubeNumber.x; x++) {
            for (int y = 0; y < cubeNumber.y; y++) {
                for (int z = 0; z < cubeNumber.z; z++) {
                    // var go = Instantiate<GameObject> (cubePrefab, this.transform);
                    var go = Instantiate<GameObject> (cubePrefab);
                    go.transform.localPosition = posOrigin + new Vector3 (gridSize.x * x, gridSize.y * y, gridSize.z * z);
                    cubes[GetIndex (x, y, z)] = go.transform;
                }
            }
        }
    }

    int GetIndex (int x, int y, int z) {
        return x * (cubeNumber.y * cubeNumber.z) + y * cubeNumber.z + z;
    }

    public Transform[] GetCubes () {
        return cubes;
    }
}