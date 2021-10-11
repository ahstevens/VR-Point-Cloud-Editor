using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class XRScaling : MonoBehaviour
{
    public InputAction scale;
    public InputAction showScalingMarker;
    public GameObject LeftHandController;
    public GameObject RightHandController;
    public InputAction LeftControllerPosition;
    public InputAction RightControllerPosition;

    public GameObject scalingRoot;

    public GameObject scalePointPrefab;
       
    private bool scaling;
    private float originalDistance;
    private Vector3 originalScale;
    private Vector3 scalePoint;
    private Vector3 pivotDirection;
    private Vector3 controllerDirection;
    private Quaternion originalRotation;
    private GameObject scalingPointObject;
    private GameObject scaleRod;

    private GameObject xrrig;

    // Start is called before the first frame update
    void Start()
    {
        xrrig = this.gameObject;

        scalingPointObject = Instantiate(scalePointPrefab, scalePoint, Quaternion.identity);
        scalingPointObject.SetActive(false);

        scaleRod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scaleRod.SetActive(false);

        scale.started += ctx => OnBeginScale();
        scale.canceled += ctx => OnEndScale();

        scaling = false;

        showScalingMarker.started += ctx => ShowScalingMarker();
        showScalingMarker.canceled += ctx => HideScalingMarker();

        if (scalingRoot == null)
            scalingRoot = GameObject.Find("Scaling Root");
    }

    private void ShowScalingMarker()
    {
        scalingPointObject.SetActive(true);
    }

    private void HideScalingMarker()
    {
        scalingPointObject.SetActive(false);
    }

    void OnEnable()
    {
        scale.Enable();
        showScalingMarker.Enable();
        LeftControllerPosition.Enable();
        RightControllerPosition.Enable();
    }

    void OnDisable()
    {
        scale.Disable();
        showScalingMarker.Disable();
        LeftControllerPosition.Disable();
        RightControllerPosition.Disable();
    }

    private void OnBeginScale()
    {
        if(scalingRoot == null)
        {
            Debug.Log("No Scaling Root GameObject set/found!");
            return;
        }

        scaling = true;

        scaleRod.SetActive(true);
        scaleRod.transform.position = scalePoint;
        scaleRod.transform.LookAt(RightHandController.transform.position, Vector3.up);
        scaleRod.transform.rotation *= Quaternion.Euler(90, 0, 0);
        scaleRod.transform.localScale = new Vector3(0.001f, (RightHandController.transform.position - LeftHandController.transform.position).magnitude * 0.5f, 0.001f);

        pivotDirection = scalingRoot.transform.position - scalePoint;

        originalRotation = scalingRoot.transform.rotation;

        controllerDirection = RightHandController.transform.position - LeftHandController.transform.position;

        originalDistance = GetControllersDistancePhysicalSpace();
        originalScale = scalingRoot.transform.localScale;
    }

    private void OnEndScale()
    {
        scaling = false;

        scaleRod.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        scalePoint = GetMidpoint(LeftHandController.transform.position, RightHandController.transform.position);

        scalingPointObject.transform.position = scalePoint;

        if (scaling)
        {
            float currentDistance = GetControllersDistancePhysicalSpace();
            float delta = currentDistance - originalDistance;
            float scaleFactor = Mathf.Exp(delta * 10f);

            scaleRod.transform.position = scalePoint;
            scaleRod.transform.localScale = new Vector3(0.001f, (RightHandController.transform.position - LeftHandController.transform.position).magnitude * 0.5f, 0.001f);
            scaleRod.transform.LookAt(RightHandController.transform.position, Vector3.up);
            scaleRod.transform.rotation *= Quaternion.Euler(90, 0, 0);

            //scaleSphere.transform.localScale = Vector3.one * 0.01f * scaleFactor;

            var pivotDelta = pivotDirection;
            pivotDelta.Scale(scaleFactor * Vector3.one);
            scalingRoot.transform.localPosition = scalePoint + pivotDelta;

            scalingRoot.transform.localScale = originalScale * scaleFactor;

            var newControllerDirection = RightHandController.transform.position - LeftHandController.transform.position;

            var newRotation = Quaternion.FromToRotation(controllerDirection, newControllerDirection);

            //scalingRoot.transform.rotation = originalRotation * newRotation;            
        }
    }

    float GetControllersDistancePhysicalSpace()
    {
        var leftPos = LeftControllerPosition.ReadValue<Vector3>();
        var rightPos = RightControllerPosition.ReadValue<Vector3>();

        return (rightPos - leftPos).magnitude;
    }

    Vector3 GetMidpoint(Vector3 firstPoint, Vector3 secondPoint)
    {
        return firstPoint + (secondPoint - firstPoint) * 0.5f;
    }
}
