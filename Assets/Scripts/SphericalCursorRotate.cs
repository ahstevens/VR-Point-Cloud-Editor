using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalCursorRotate : MonoBehaviour
{
    public GameObject xTorus, yTorus, zTorus;
    public float rotationSpeed;
    public bool billboardZTorus;

    void Update()
    {
        var tick = Time.realtimeSinceStartup;

        var rotationAmount = ((tick % rotationSpeed) / rotationSpeed) * 360f;

        xTorus.transform.localRotation = Quaternion.Euler(rotationAmount, 0, 0);
        yTorus.transform.localRotation = Quaternion.Euler(rotationAmount, 90, 0);

        if (billboardZTorus)
            zTorus.transform.rotation = Quaternion.LookRotation(Camera.main.transform.position - zTorus.transform.position, Camera.main.transform.up);
        else
            zTorus.transform.localRotation = Quaternion.Euler(0, rotationAmount, 90);
    }
}
