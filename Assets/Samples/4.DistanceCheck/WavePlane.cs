using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavePlane : MonoBehaviour {

    Camera mainCamera;
    float speed = 3f;
    float distance = 20f;

    void Start () {
        mainCamera = Camera.main;
    }

    void Update () {
        var localPos = transform.localPosition;
        if ((mainCamera.transform.localPosition - localPos).magnitude < distance) {
            localPos.y = Mathf.Sin (Time.realtimeSinceStartup * speed - localPos.x) * Mathf.Sin (Time.realtimeSinceStartup * speed - localPos.z);
        } else {
            localPos.y = 0;
        }
        transform.localPosition = localPos;
    }
}