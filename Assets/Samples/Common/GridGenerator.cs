using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour {

    [System.Serializable]
    public struct IntVector3 { public int x, y, z; }

    public bool spawnAsChildren;
    public GameObject sourcePrefab;
    public IntVector3 objectNumber;
    public Vector3 gridSize;

    Transform[] objects;

    void Awake () {
        QualitySettings.vSyncCount = 0;
        GenerateCubes ();
    }

    void GenerateCubes () {
        if (sourcePrefab == null) {
            return;
        }

        objects = new Transform[objectNumber.x * objectNumber.y * objectNumber.z];
        var posOrigin = new Vector3 (gridSize.x * objectNumber.x, gridSize.y * objectNumber.y, gridSize.z * objectNumber.z) * -0.5f;
        for (int x = 0; x < objectNumber.x; x++) {
            for (int y = 0; y < objectNumber.y; y++) {
                for (int z = 0; z < objectNumber.z; z++) {
                    var go = Instantiate<GameObject> (sourcePrefab, spawnAsChildren ? this.transform : null);
                    go.transform.localPosition = posOrigin + new Vector3 (gridSize.x * x, gridSize.y * y, gridSize.z * z);
                    objects[GetIndex (x, y, z)] = go.transform;
                }
            }
        }
    }

    int GetIndex (int x, int y, int z) {
        return x * (objectNumber.y * objectNumber.z) + y * objectNumber.z + z;
    }

    public Transform[] GetTransforms () {
        return objects;
    }
}