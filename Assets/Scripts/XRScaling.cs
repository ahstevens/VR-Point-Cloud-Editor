using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Runtime.InteropServices;

public class XRScaling : MonoBehaviour
{
    public InputActionProperty scale;
    public InputActionProperty showScalingMarker;
    public GameObject LeftHandController;
    public GameObject RightHandController;
    public InputActionProperty LeftControllerPosition;
    public InputActionProperty RightControllerPosition;

    public GameObject scalingRoot;

    public bool scaleOnClosestPoint = false;

    public GameObject scalePointPrefab;

    public GameObject connectorPrefab;
       
    private bool scaling;
    private float originalDistance;
    private Vector3 originalScale;
    private Vector3 scalePoint;
    private Vector3 pivotDirection;
    private GameObject scalingPointObject;
    private GameObject scaleRod;
    private GameObject leftConnector;
    private GameObject rightConnector;

    private GameObject closestPoint;

    private bool scalingOnCenter = false;

    // Start is called before the first frame update
    void Start()
    {
        scalingPointObject = Instantiate(scalePointPrefab, scalePoint, Quaternion.identity);
        scalingPointObject.SetActive(false);

        scaleRod = Instantiate(connectorPrefab);
        var scaleRodScript = scaleRod.GetComponent<CylinderConnector>();
        scaleRodScript.anchor = RightHandController.transform;
        scaleRodScript.target = LeftHandController.transform;
        scaleRodScript.thickness = 0.001f;
        scaleRodScript.dynamic = true;
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

        if (scaleOnClosestPoint)
        {
            closestPoint = new("Closest Point");
            closestPoint.transform.SetParent(FindObjectOfType<PointCloud>().transform);
            closestPoint.transform.position = FindObjectOfType<PointCloud>().FindClosestPoint(GetControllerMidpoint());

            CreateConnectingLinesToControllers(closestPoint.transform);

            scalePoint = closestPoint.transform.position;
            scalingPointObject.transform.position = scalePoint;
        }
    }

    private void HideScalingMarker()
    {
        scalingPointObject.SetActive(false);

        if (scaleOnClosestPoint)
        {
            Destroy(closestPoint);
            DestroyConnectingLines();
        }
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
        // If already grabbing, ignore scale action
        if (FindObjectOfType<XRGrabbing>() != null && FindObjectOfType<XRGrabbing>().IsGrabbing())
            return;

        if (scalingRoot == null)
        {
            Debug.Log("No Scaling Root GameObject set/found!");
            return;
        }

        FindObjectOfType<XRGrabbing>().enabled = false;
        FindObjectOfType<ModifyPoints>().enabled = false;
        FindObjectOfType<ModifyPoints>().SetBrushVisibility(false);

        scaling = true;

        scaleRod.SetActive(true);

        scalePoint = GetMidpointOrCenter();

        if (scalePoint != GetControllerMidpoint())
        {
            scalingOnCenter = true;
            CreateConnectingLinesToControllers(scalingPointObject.transform);
        }

        pivotDirection = scalingRoot.transform.position - scalePoint;

        originalDistance = GetControllersDistancePhysicalSpace();
        originalScale = scalingRoot.transform.localScale;
    }

    private void OnEndScale()
    {
        scaling = false;

        if (scalingOnCenter)
        {
            DestroyConnectingLines();
            scalingOnCenter = false;
        }


        FindObjectOfType<XRGrabbing>().enabled = true;
        FindObjectOfType<ModifyPoints>().enabled = true;
        FindObjectOfType<ModifyPoints>().SetBrushVisibility(!FindObjectOfType<PointCloudUI>().MenuOpen);

        scaleRod.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (scalingPointObject.activeSelf && !scaleOnClosestPoint && !scalingOnCenter)
        {
            scalePoint = GetMidpointOrCenter();
            scalingPointObject.transform.position = scalePoint;
        }

        if (scaling)
        {
            float currentDistance = GetControllersDistancePhysicalSpace();
            float delta = currentDistance - originalDistance;
            float scaleFactor = Mathf.Exp(delta * 10f);

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

    Vector3 GetControllerMidpoint()
    {
        return GetMidpoint(LeftHandController.transform.position, RightHandController.transform.position);
    }

    Vector3 GetMidpoint(Vector3 firstPoint, Vector3 secondPoint)
    {
        return firstPoint + (secondPoint - firstPoint) * 0.5f;
    }

    public bool IsScaling()
    {
        return scaling;
    }

    private Vector3 GetMidpointOrCenter()
    {
        var pc = FindObjectOfType<PointCloud>();
        var bounds = pc.bounds;

        var controllerMidPoint = GetControllerMidpoint();

        if (bounds.Contains(pc.transform.InverseTransformPoint(controllerMidPoint)))
        {
            return GetControllerMidpoint();
        }
        else
        {
            return pc.transform.TransformPoint(bounds.center);
        }
    }

    private void CreateConnectingLinesToControllers(Transform connectedObject)
    {
        leftConnector = Instantiate(connectorPrefab);
        var leftConnectorScript = leftConnector.GetComponent<CylinderConnector>();
        leftConnectorScript.target = connectedObject;
        leftConnectorScript.anchor = LeftHandController.transform;
        leftConnectorScript.thickness = 0.001f;
        leftConnectorScript.dynamic = true;

        rightConnector = Instantiate(connectorPrefab);
        var rightConnectorScript = rightConnector.GetComponent<CylinderConnector>();
        rightConnectorScript.target = connectedObject;
        rightConnectorScript.anchor = RightHandController.transform;
        rightConnectorScript.thickness = 0.001f;
        rightConnectorScript.dynamic = true;
    }

    private void DestroyConnectingLines()
    {
        if (leftConnector) Destroy(leftConnector);
        if (rightConnector) Destroy(rightConnector);
    }
}
