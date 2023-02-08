using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class ModifyPoints : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern void RequestToDeleteFromUnity(IntPtr center, float size, IntPtr eventID);
    
    [DllImport("PointCloudPlugin")]
    private static extern void RequestToModifyPointsFromUnity(IntPtr center, float size, int modificationType, float additionalData, IntPtr eventID);

    [DllImport("PointCloudPlugin")]
    private static extern void UpdateModificationParameters(IntPtr center, float size, int modificationType, float additionalData);

    [DllImport("PointCloudPlugin")]
    private static extern int GetRequestResult();

    //[DllImport("PointCloudPlugin")]
    //static public extern void setHighlightDeletedPointsActive(bool active);
    
    //[DllImport("PointCloudPlugin")]
    //static public extern void UpdateDeletionSpherePositionFromUnity(IntPtr center, float size);
    
    [DllImport("PointCloudPlugin")]
    static public extern void undo(int numberToDelete); 
    
    [DllImport("PointCloudPlugin")]
    public static extern void RequestClassificationVisualizationFromUnity(bool showClassifiers);

    public GameObject scalingRoot;
    public InputActionProperty modifyInSphereAction;
    public InputActionProperty undoModificationAction;
    public InputActionProperty moveAndResizeSphereAction;
    public InputActionProperty hapticAction;
    public float moveAndResizeThumbstickDeadzone = 0.2f;
    public float moveAndResizeTouchpadDelta = 0.2f;
    public float minimumSphereRadius;
    public float maximumSphereRadius;
    public float minimumSphereOffset;
    public float maximumSphereOffset;

    public float modificationRate;
    private bool modifying;
    private int currentModificationClassifier;
    private float modificationTimer = 0;
    private bool checkModificationResult;
    private int lastFrameOfModificationRequest;

    public HapticPattern modificationHaptic;
    public HapticPattern undoHaptic;

    [SerializeField]
    private GameObject editingSphere;
    [SerializeField]
    private GameObject connector;
    private GameObject pcRoot;

    private bool movingOrResizing;
    private bool resizing;
    private bool moving;

    private bool thumbstick;
    private bool touchpad;
    private Vector2 initialTouchpadTouchPoint;
    private Vector2 initialTouchpadMeasurementPoint;
    private float initialSphereRadius;
    private float initialSphereDistance;

    [SerializeField]
    private Material cursorMaterial;
    [SerializeField]
    private Material connectorMaterial;
    private Color originalCursorColor;
    private Color classifierColor;

    private List<int> modificationOps;
    private int currentModificationOpCount;

    [SerializeField]
    GameObject classifierText;

    private static bool _classifierMode = false;
    public static bool classifierMode
    {
        get { return _classifierMode; }
    }

    private static bool _brushVisible = true;
    public static bool IsBrushVisible
    {
        get { return _brushVisible; }
    }

    // Start is called before the first frame update
    void Start()
    {
        modifyInSphereAction.action.started += ctx => OnBeginModifyInSphere();
        modifyInSphereAction.action.canceled += ctx => OnEndModifyInSphere();

        undoModificationAction.action.started += ctx => UndoModification();

        moveAndResizeSphereAction.action.started += ctx => OnBeginMoveAndResizeSphere();
        moveAndResizeSphereAction.action.canceled += ctx => OnEndMoveAndResizeSphere();

        movingOrResizing = false;
        resizing = false;
        moving = false;

        touchpad = false;
        thumbstick = false;

        checkModificationResult = false;
        lastFrameOfModificationRequest = -1;

        modificationOps = new List<int>();
        currentModificationOpCount = 0;
        
        minimumSphereRadius = UserSettings.instance.preferences.cursorRadiusMin;
        maximumSphereRadius = UserSettings.instance.preferences.cursorRadiusMax;
        minimumSphereOffset = UserSettings.instance.preferences.cursorDistanceMin;
        maximumSphereOffset = UserSettings.instance.preferences.cursorDistanceMax;

        modificationRate = 1f / UserSettings.instance.preferences.cursorDeletionRate;
        modifying = false;

        originalCursorColor = cursorMaterial.color;

        currentModificationClassifier = -1;
        classifierColor = Color.white;

        editingSphere.transform.localScale = Vector3.one * UserSettings.instance.preferences.cursorRadius;

        editingSphere.transform.localPosition = Vector3.forward * UserSettings.instance.preferences.cursorDistance;

        //setHighlightDeletedPointsActive(true);

        if (UserSettings.instance.preferences.openMenuOnStart)
            SetBrushVisibility(false);

        RequestClassificationVisualizationFromUnity(_classifierMode);
    }

    private void OnApplicationQuit()
    {
        if (UserSettings.instance.preferences.saveCursorOnExit)
        {
            UserSettings.instance.preferences.cursorRadius = editingSphere.transform.localScale.x;
            UserSettings.instance.preferences.cursorDistance = editingSphere.transform.localPosition.z;
            UserSettings.instance.SaveToFile();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pcRoot == null)
            pcRoot = GameObject.Find("Point Clouds Root");

        //Debug.Log("Before eventID check");

        int result = GetRequestResult();

        //Debug.Log("GetRequestResult() returned " + result + " for frame " + Time.frameCount + " (modification frame = " + lastFrameOfModificationRequest);
        
        if (result > 0)
        {
            currentModificationOpCount++;

            //if (result != 0)
            //    deletionOps.Add(result);

            Debug.Log(result + " points were deleted; total operations = " + currentModificationOpCount);
            
            //Debug.Log(Time.timeAsDouble - beginDeleteTimestamp);
            //Debug.Log($"{deletionSphere.transform.position.x}f, {deletionSphere.transform.position.y}f, {deletionSphere.transform.position.z}f");
            //Debug.Log(deletionSphere.transform.localScale);
            
            // Send haptic feedback to right controller
            UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, modificationHaptic.amplitude, modificationHaptic.frequency, modificationHaptic.duration, XRController.rightHand);

            //if (checkModificationResult)
            //{
            //    modificationOps.Add(currentModificationOpCount);
            //
            //    Debug.Log("Added " + currentModificationOpCount + " operations to the undo list (" + modificationOps.Count + " undo actions remaining in queue)");
            //
            //    checkModificationResult = false;
            //}
        }
        else if (result == -1)
        {
            Debug.Log("Waiting for deletion event to complete...");
        }            
        else if (checkModificationResult)
        {
            if (currentModificationOpCount > 0)
            {
                modificationOps.Add(currentModificationOpCount);

                Debug.Log("Added " + currentModificationOpCount + " operations to the undo list (" + modificationOps.Count + " undo actions remaining in queue)");
            }
            else
            {
                Debug.Log("No operations made; undo list not changed (" + modificationOps.Count + " undo actions remaining in queue)");
            }

            checkModificationResult = false;
        }
        

//#if UNITY_EDITOR
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            ActivateClassificationMode(!_classifierMode);
        }
//#endif
        if (movingOrResizing && _brushVisible)
        {
            var sample = moveAndResizeSphereAction.action.ReadValue<Vector2>();

            if (touchpad)
            {
                var delta = sample - initialTouchpadTouchPoint;

                if (!(moving || resizing) && (delta.magnitude > moveAndResizeTouchpadDelta))
                {
                    initialTouchpadMeasurementPoint = sample;

                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        resizing = true; // horizontal swipe
                        movingOrResizing = false;
                        initialSphereRadius = editingSphere.transform.localScale.x;
                    }
                    else
                    {
                        moving = true; // vertical swipe
                        movingOrResizing = false;
                        initialSphereDistance = editingSphere.transform.localPosition.z;
                    }
                }
            }

            if (thumbstick)
            {
                if ((Mathf.Abs(sample.x) > moveAndResizeThumbstickDeadzone) && (Mathf.Abs(sample.x) > Mathf.Abs(sample.y)))
                {
                    resizing = true;
                    movingOrResizing = false;
                }
                else if ((Mathf.Abs(sample.y) > moveAndResizeThumbstickDeadzone) && (Mathf.Abs(sample.x) < Mathf.Abs(sample.y)))
                {
                    moving = true;
                    movingOrResizing = false;
                }
            }
        }

        if ((moving || resizing) && _brushVisible)
        {
            if (thumbstick)
                ThumbstickMoveOrResize();

            if (touchpad)
                TouchpadMoveOrResize();
        }

        MaintainSphereOffsetFromController();
    }

    void OnEnable()
    {
        modifyInSphereAction.action.Enable();
        undoModificationAction.action.Enable();
        moveAndResizeSphereAction.action.Enable();
    }

    void OnDisable()
    {
        modifyInSphereAction.action.Disable();
        undoModificationAction.action.Disable();
        moveAndResizeSphereAction.action.Disable();

        cursorMaterial.color = Color.white;
        connectorMaterial.color = Color.white;
    }

    public void ActivateClassificationMode(bool onOff)
    {
        _classifierMode = onOff;
        RequestClassificationVisualizationFromUnity(_classifierMode);
        Debug.Log("Mode set to " + (_classifierMode ? "classifiers" : "RGB"));

        connectorMaterial.color = _classifierMode ? classifierColor : Color.white;

        classifierText.SetActive(_classifierMode);
            
    }

    void OnBeginModifyInSphere()
    {
        if (editingSphere == null || !_brushVisible)
            return;

        if (cursorMaterial != null)
        {
            originalCursorColor = cursorMaterial.color;
            cursorMaterial.color = _classifierMode ? classifierColor : Color.red;
        }

        currentModificationOpCount = 0;

        modifying = true;
        modificationTimer = 0f;
    }

    void OnEndModifyInSphere()
    {
        if (cursorMaterial != null)
        {
            cursorMaterial.color = originalCursorColor;
        }

        modifying = false;
        modificationTimer = 0f;

        checkModificationResult = _brushVisible;
    }

    public void ModifyInSphere()
    {
        if (pcRoot == null)
            return;

        if (modifying)
        {
            modificationTimer += Time.deltaTime;

            if (modificationTimer >= modificationRate)
            {
                modificationTimer -= modificationRate;

                float[] center = new float[3];
                center[0] = editingSphere.transform.position.x;
                center[1] = editingSphere.transform.position.y;
                center[2] = editingSphere.transform.position.z;

                GCHandle sphereCenterPtr = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);

                float editingRadius = editingSphere.transform.localScale.x;

                int modificationAction = classifierMode ? 1 : 0;

                UpdateModificationParameters(sphereCenterPtr.AddrOfPinnedObject(), editingRadius, modificationAction, (float)currentModificationClassifier);

                //checkModificationResult = true;
                lastFrameOfModificationRequest = Time.frameCount;
            }
        }
    }

    public void SetModificationClassifier(int classifier, Color color)
    {
        currentModificationClassifier = classifier;
        classifierColor = color;

        if (_classifierMode)
            connectorMaterial.color = classifierColor;
    }

    private void UndoModification()
    {
        if (modificationOps.Count > 0)
        {
            var numHalfSteps = modificationOps.Count;
            var frequency = undoHaptic.frequency * Mathf.Pow(2, numHalfSteps / 12f);
            UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, undoHaptic.amplitude, frequency, undoHaptic.duration);//, UnityEngine.InputSystem.XR.XRController.rightHand);

            Debug.Log("Requesting undo for the last " + modificationOps.Last() + " deletion operations (" + (modificationOps.Count - 1) + " undo actions remaining in queue)");
            undo(modificationOps.Last());

            modificationOps.RemoveAt(modificationOps.Count - 1);
        }
        else
        {
            StartCoroutine(NoMoreUndo());
        }
    }

    private void OnBeginMoveAndResizeSphere()
    {
        if (!_brushVisible)
            return;

        movingOrResizing = true;

        if (moveAndResizeSphereAction.action.activeControl.displayName == "trackpad" || moveAndResizeSphereAction.action.activeControl.displayName == "touchpad")
        {
            touchpad = true;
            initialTouchpadTouchPoint = moveAndResizeSphereAction.action.ReadValue<Vector2>();
        }
        else
        {
            thumbstick = true;
        }
    }

    private void OnEndMoveAndResizeSphere()
    {
        movingOrResizing = false;
        moving = false;
        resizing = false;
        thumbstick = false;
        touchpad = false;
    }

    //private void UpdateSpherePositionInPlugin()
    //{
    //    float[] center = new float[3];
    //
    //    GCHandle toDelete = GCHandle.Alloc(center, GCHandleType.Pinned);
    //
    //    center[0] = editingSphere.transform.position.x;
    //    center[1] = editingSphere.transform.position.y;
    //    center[2] = editingSphere.transform.position.z;
    //    
    //    UpdateDeletionSpherePositionFromUnity(toDelete.AddrOfPinnedObject(), (editingSphere.transform.localScale.x) / scalingRoot.transform.localScale.x);
    //}

    private void ThumbstickMoveOrResize()
    {
        if (resizing)
        {
            var delta = moveAndResizeSphereAction.action.ReadValue<Vector2>().x;

            var scaleFactor = Mathf.Exp(delta * 0.1F);

            var size = editingSphere.transform.localScale.x * scaleFactor;            

            if (size < minimumSphereRadius)
                size = minimumSphereRadius;            
            else if (size > maximumSphereRadius)            
                size = maximumSphereRadius;

            editingSphere.transform.localScale = Vector3.one * size;

            if (Mathf.Abs(delta) <= moveAndResizeThumbstickDeadzone)
            {
                resizing = false;
                movingOrResizing = true;
            }
        }

        if (moving)
        {
            var delta = moveAndResizeSphereAction.action.ReadValue<Vector2>().y;

            var scaleFactor = delta * 0.1F;

            editingSphere.transform.localPosition += Vector3.forward * scaleFactor;

            if (editingSphere.transform.localPosition.z < minimumSphereOffset)
                editingSphere.transform.localPosition = Vector3.forward * minimumSphereOffset;
            else if (editingSphere.transform.localPosition.z > maximumSphereOffset)
                editingSphere.transform.localPosition = Vector3.forward * maximumSphereOffset;

            if (Mathf.Abs(delta) <= moveAndResizeThumbstickDeadzone)
            {
                moving = false;
                movingOrResizing = true;
            }
        }
    }

    private void TouchpadMoveOrResize()
    {
        var sample = moveAndResizeSphereAction.action.ReadValue<Vector2>();

        if (moving)
        {
            float dy = (sample - initialTouchpadMeasurementPoint).y;

            float range = maximumSphereOffset - minimumSphereOffset;

            var offset = initialSphereDistance + dy * range * 0.5f;

            if (offset > maximumSphereOffset)
            {
                offset = maximumSphereOffset;
                initialSphereDistance = maximumSphereOffset;
                initialTouchpadMeasurementPoint.y = sample.y;
            }
            else if (offset < minimumSphereOffset)
            {
                offset = minimumSphereOffset;
                initialSphereDistance = minimumSphereOffset;
                initialTouchpadMeasurementPoint.y = sample.y;
            }

            editingSphere.transform.localPosition = Vector3.forward * offset;
        }

        if (resizing)
        {
            float dx = sample.x - initialTouchpadMeasurementPoint.x;

            float range = maximumSphereRadius - minimumSphereRadius;

            var size = initialSphereRadius + dx * range;

            if (size > maximumSphereRadius)
            {
                size = maximumSphereRadius;
                initialSphereRadius = maximumSphereRadius;
                initialTouchpadMeasurementPoint.x = sample.x;
            }
            else if (size < minimumSphereRadius)
            {
                size = minimumSphereRadius;
                initialSphereRadius = minimumSphereRadius;
                initialTouchpadMeasurementPoint.x = sample.x;
            }

            editingSphere.transform.localScale = Vector3.one * size;
        }
    }
    
    private void MaintainSphereOffsetFromController()
    {
        // Keep sphere from getting too close to controller
        var currentRadius = editingSphere.transform.localScale.x;// * 0.5f;

        if (editingSphere.transform.localPosition.z < currentRadius)
        {
            editingSphere.transform.localPosition = Vector3.forward * currentRadius;
        }
    }

    IEnumerator NoMoreUndo()
    {
        UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, 1f, undoHaptic.frequency, 0.05f);
        yield return new WaitForSeconds(0.1f);
        UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, 0.5f, undoHaptic.frequency * Mathf.Pow(2, -2 / 12f), 0.05f);
        yield return new WaitForSeconds(0.1f);
        UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, 0.25f, undoHaptic.frequency * Mathf.Pow(2, -4 / 12f), 0.05f);
    }

    public void SetBrushVisibility(bool isVisible)
    {
        _brushVisible = isVisible;

        foreach (var torus in editingSphere.GetComponentsInChildren<MeshRenderer>())
        {
            torus.enabled = _brushVisible;
        }

        connector.GetComponent<MeshRenderer>().enabled = _brushVisible;

        if (!_brushVisible && modifying)
            OnEndModifyInSphere();
    }
}
