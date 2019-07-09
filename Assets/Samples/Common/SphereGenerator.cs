using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereGenerator : MonoBehaviour {

    [System.Serializable]
    public struct IntVector2 { public int x, y; }

    public bool spawnAsChildren;
    public GameObject sourcePrefab;
    public float radius;
    public IntVector2 segmentNumber;

    Transform[] objects;

    void Awake () {
        QualitySettings.vSyncCount = 0;
        GenerateCubes ();
    }

    void GenerateCubes () {
        if (sourcePrefab == null) {
            return;
        }

        objects = new Transform[segmentNumber.x * segmentNumber.y];
        var degreeX = Mathf.PI * 2f / segmentNumber.x;
        var degreeY = Mathf.PI / segmentNumber.y;
        var halfPI = Mathf.PI / -2f;
        for (int x = 0; x < segmentNumber.x; x++) {
            for (int y = 0; y < segmentNumber.y; y++) {
                var go = Instantiate<GameObject> (sourcePrefab, spawnAsChildren ? this.transform : null);
                var dx = degreeX * x;
                var dy = halfPI + degreeY * y;
                var dir = new Vector3 (Mathf.Cos (dx) * Mathf.Cos (dy), Mathf.Sin (dy), Mathf.Sin (dx) * Mathf.Cos (dy));
                go.transform.localPosition = dir * radius;
                go.transform.localRotation = Quaternion.LookRotation (dir, Vector3.up);
                objects[GetIndex (x, y)] = go.transform;
            }
        }
    }

    int GetIndex (int x, int y) {
        return x * segmentNumber.y + y;
    }

    public Transform[] GetTransforms () {
        return objects;
    }
}