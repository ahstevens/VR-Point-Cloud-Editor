using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRScaling : MonoBehaviour
{
    [SerializeField] private InputActionProperty scale;
    [SerializeField] private InputActionProperty showScalingMarker;
    [SerializeField] private GameObject LeftHandController;
    [SerializeField] private GameObject RightHandController;
    [SerializeField] private InputActionProperty LeftControllerPosition;
    [SerializeField] private InputActionProperty RightControllerPosition;

    public GameObject scalingRoot;

    public Action PreScalingBegin;
    public Action PreScalingEnd;

    public Action ScalingBegin;
    public Action ScalingEnd;

    public Vector3 offsetFromController = Vector3.zero;

    public enum ScalingInteractionPoint
    {
        CONTROLLERS_MIDPOINT,
        LEFT_CONTROLLER,
        RIGHT_CONTROLLER,
        DATA_CENTER
    }

    public enum ScalingType
    {
        CLOSEST_POINT,
        EXACT
    }

    public ScalingInteractionPoint interactionPointInsideDataBounds;
    public ScalingType scalingTypeInsideDataBounds;

    public ScalingInteractionPoint interactionPointOutsideDataBounds;
    public ScalingType scalingTypeOutsideDataBounds;

    private bool _preScaling;
    private bool _scaling;

    public bool IsScaling
    {
        get { return _scaling; }
    }

    private Vector3 scalePoint;
    private bool scalePointUpdatedThisFrame = false;

    private Vector3 interactionPoint;
    private bool interactionPointUpdatedThisFrame = false;

    private float originalDistance;
    private Vector3 originalScale;
    private Vector3 pivotVector;

    private GameObject closestPoint;

    // Start is called before the first frame update
    void Start()
    {
        _scaling = _preScaling = false;

        showScalingMarker.action.started += ctx => PreScaleStart();
        showScalingMarker.action.started += ctx => PreScalingBegin?.Invoke();

        showScalingMarker.action.canceled += ctx => PreScaleStop();
        showScalingMarker.action.canceled += ctx => PreScalingEnd?.Invoke();

        scale.action.started += ctx => OnBeginScale();
        scale.action.started += ctx => ScalingBegin?.Invoke();
        scale.action.canceled += ctx => OnEndScale();
        scale.action.canceled += ctx => ScalingEnd?.Invoke();


        if (scalingRoot == null)
            scalingRoot = GameObject.Find("Scaling Root");

        if (scalingRoot == null)        
            Debug.Log("No Scaling Root GameObject set/found!");
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

    private void PreScaleStart()
    {
        _preScaling = true;

        interactionPoint = GetActiveInteractionPoint();
        scalePoint = GetActiveScalingPoint();  
    }

    private void PreScaleStop()
    {
        _preScaling = false;

        if (closestPoint != null)
            Destroy(closestPoint);
    }

    private void OnBeginScale()
    {
        // If already grabbing, ignore scale action
        if (FindObjectOfType<XRGrabbing>() != null && FindObjectOfType<XRGrabbing>().IsGrabbing)
            return;

        FindObjectOfType<XRGrabbing>().enabled = false;
        FindObjectOfType<ModifyPoints>().enabled = false;
        FindObjectOfType<ModifyPoints>().SetBrushVisibility(false);

        _scaling = true;
        _preScaling = false;

        ScalingBegin?.Invoke();

        interactionPoint = GetActiveInteractionPoint();
        scalePoint = GetActiveScalingPoint();

        pivotVector = scalingRoot.transform.position - scalePoint;

        originalDistance = GetControllersDistancePhysicalSpace();
        originalScale = scalingRoot.transform.localScale;
    }

    private void OnEndScale()
    {
        _scaling = false;

        ScalingEnd?.Invoke();

        FindObjectOfType<XRGrabbing>().enabled = true;
        FindObjectOfType<ModifyPoints>().enabled = true;
        FindObjectOfType<ModifyPoints>().SetBrushVisibility(!FindObjectOfType<PointCloudUI>().MenuOpen);
    }

    // Update is called once per frame
    void Update()
    {
        if (_preScaling && !_scaling)
        {
            interactionPoint = GetActiveInteractionPoint();
            scalePoint = GetActiveScalingPoint();
            if (closestPoint != null)
            {
                closestPoint.transform.position = scalePoint;
            }
            //Debug.Log("Prescaling but not scaling");
        }

        if (_scaling)
        {
            ScaleWithTranslation();
        }
    }

    void ScaleWithTranslation()
    {
        float scaleFactor = GetScalingFactor();

        scalingRoot.transform.localScale = originalScale * scaleFactor;
        scalingRoot.transform.localPosition = scalePoint + pivotVector * scaleFactor;
    }

    void LateUpdate()
    {
        interactionPointUpdatedThisFrame = false;
        scalePointUpdatedThisFrame = false;
    }

    float GetScalingFactor()
    {
        float currentDistance = GetControllersDistancePhysicalSpace();
        float delta = currentDistance - originalDistance;

        return Mathf.Exp(delta * 10f);
    }

    Vector3 GetControllersVectorPhysicalSpace()
    {
        var leftPos = LeftControllerPosition.action.ReadValue<Vector3>();
        var rightPos = RightControllerPosition.action.ReadValue<Vector3>();

        return (rightPos - leftPos);
    }

    float GetControllersDistancePhysicalSpace()
    {
        return GetControllersVectorPhysicalSpace().magnitude;
    }

    Vector3 GetMidpoint(Vector3 firstPoint, Vector3 secondPoint)
    {
        return firstPoint + (secondPoint - firstPoint) * 0.5f;
    }

    public Vector3 GetActiveScalingPoint()
    {
        //Debug.Log("Get Active Scaling Point (" + scalePointUpdatedThisFrame + ")");
        if (!scalePointUpdatedThisFrame)
        {
            var ip = GetActiveInteractionPoint();
            var ipInBounds = IsPointInsideBounds(ip);

            if ((ipInBounds && scalingTypeInsideDataBounds == ScalingType.CLOSEST_POINT) || 
                (!ipInBounds && scalingTypeOutsideDataBounds == ScalingType.CLOSEST_POINT))
            {
                var pc = FindObjectOfType<PointCloud>();

                if (closestPoint == null)
                {
                    closestPoint = new("Closest Point");
                    closestPoint.transform.SetParent(pc.transform);
                }

                scalePoint = pc.FindClosestPoint(ip);
            }
            else
            {
                scalePoint = ip;
            }
            
            scalePointUpdatedThisFrame = true;
        }
                                     
        return scalePoint;
    }

    public Vector3 GetActiveInteractionPoint()
    {
        if (!interactionPointUpdatedThisFrame)        
        {
            var pp = GetInteractionPoint(interactionPointInsideDataBounds);

            if (IsPointInsideBounds(pp))
                interactionPoint = pp;
            else
                interactionPoint = GetInteractionPoint(interactionPointOutsideDataBounds);

            interactionPointUpdatedThisFrame = true;
        }

        return interactionPoint;
    }

    private Vector3 GetInteractionPoint(ScalingInteractionPoint pivotType)
    {
        Vector3 pivot;

        switch (pivotType)
        {
            case ScalingInteractionPoint.CONTROLLERS_MIDPOINT:
                pivot = GetMidpoint(LeftHandController.transform.position, RightHandController.transform.position);
                break;

            case ScalingInteractionPoint.LEFT_CONTROLLER:
                pivot = LeftHandController.transform.TransformPoint(offsetFromController);
                break;

            case ScalingInteractionPoint.RIGHT_CONTROLLER:
                pivot = RightHandController.transform.TransformPoint(offsetFromController);
                break;

            case ScalingInteractionPoint.DATA_CENTER:
                var pc = FindObjectOfType<PointCloud>();
                pivot = pc.transform.TransformPoint(pc.bounds.center);
                break;

            default:
                pivot = Vector3.zero;
                break;
        }

        return pivot;
    }

    private bool IsPointInsideBounds(Vector3 worldPoint)
    {
        var pc = FindObjectOfType<PointCloud>();
        return pc.bounds.Contains(pc.transform.InverseTransformPoint(worldPoint));
    }
}
