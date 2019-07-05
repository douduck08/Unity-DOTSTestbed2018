using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneXZGenerator : MonoBehaviour {

    [System.Serializable]
    public struct IntVector2 { public int x, z; }

    public GameObject sourcePrefab;
    public IntVector2 objectNumber;
    public Vector2 gridSize;

    Transform[] objects;

    void Awake () {
        QualitySettings.vSyncCount = 0;
        Generate ();
    }

    void Generate () {
        if (sourcePrefab == null) {
            return;
        }

        objects = new Transform[objectNumber.x * objectNumber.z];
        var posOrigin = new Vector3 (gridSize.x * objectNumber.x, 0, gridSize.y * objectNumber.z) * -0.5f;
        for (int x = 0; x < objectNumber.x; x++) {
            for (int z = 0; z < objectNumber.z; z++) {
                var go = Instantiate<GameObject> (sourcePrefab, this.transform);
                var localPos = posOrigin + new Vector3 (gridSize.x * x, 0, gridSize.y * z);
                go.transform.localPosition = localPos;
                objects[GetIndex (x, z)] = go.transform;
            }
        }
    }

    int GetIndex (int x, int y) {
        return x * objectNumber.z + y;
    }

    public Transform[] GetTransforms () {
        return objects;
    }
}