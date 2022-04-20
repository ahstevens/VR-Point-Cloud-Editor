using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class XRScaling : MonoBehaviour
{
    public InputActionProperty scale;
    public InputActionProperty showScalingMarker;
    public GameObject LeftHandController;
    public GameObject RightHandController;
    public InputActionProperty LeftControllerPosition;
    public InputActionProperty RightControllerPosition;

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

        scale.action.started += ctx => OnBeginScale();
        scale.action.canceled += ctx => OnEndScale();

        scaling = false;

        showScalingMarker.action.started += ctx => ShowScalingMarker();
        showScalingMarker.action.canceled += ctx => HideScalingMarker();

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
        scale.action.Enable();
        showScalingMarker.action.Enable();
        LeftControllerPosition.action.Enable();
        RightControllerPosition.action.Enable();
    }

    void OnDisable()
    {
        scale.action.Disable();
        showScalingMarker.action.Disable();
        LeftControllerPosition.action.Disable();
        RightControllerPosition.action.Disable();
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

            var pivotDelta = pivotDirection;
            pivotDelta.Scale(scaleFactor * Vector3.one);
            scalingRoot.transform.localPosition = scalePoint + pivotDelta;

            scalingRoot.transform.localScale = originalScale * scaleFactor;      
        }
    }

    float GetControllersDistancePhysicalSpace()
    {
        var leftPos = LeftControllerPosition.action.ReadValue<Vector3>();
        var rightPos = RightControllerPosition.action.ReadValue<Vector3>();

        return (rightPos - leftPos).magnitude;
    }

    Vector3 GetMidpoint(Vector3 firstPoint, Vector3 secondPoint)
    {
        return firstPoint + (secondPoint - firstPoint) * 0.5f;
    }
}
