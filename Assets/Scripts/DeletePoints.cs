using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;

public class DeletePoints : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern int RequestToDeleteFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void setHighlightDeletedPointsActive(bool active);
    [DllImport("PointCloudPlugin")]
    static public extern void UpdateDeletionSpherePositionFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void undo(int numberToDelete);

    public GameObject scalingRoot;
    public InputActionProperty deleteSphereAction;
    public InputActionProperty undoDeletionAction;
    public InputActionProperty moveAndResizeSphereAction;
    public InputActionProperty hapticAction;
    public float moveAndResizeThumbstickDeadzone = 0.2f;
    public float moveAndResizeTouchpadDelta = 0.2f;
    public float minimumSphereRadius;
    public float maximumSphereRadius;
    public float minimumSphereOffset;
    public float maximumSphereOffset;

    public float deleteRate;

    public HapticPattern deleteHaptic;
    public HapticPattern undoHaptic;

    private GameObject deletionSphere;
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

    public Material CursorMaterial;
    private Color originalCursorColor;

    private List<int> deletionOps;
    private int currentDeletionOpCount;

    private double beginDeleteTimestamp;

    // Start is called before the first frame update
    void Start()
    {
        deletionSphere = this.gameObject;

        deleteSphereAction.action.started += ctx => OnBeginDeleteSphere();
        deleteSphereAction.action.canceled += ctx => OnEndDeleteSphere();

        undoDeletionAction.action.started += ctx => UndoDeletion();

        moveAndResizeSphereAction.action.started += ctx => OnBeginMoveAndResizeSphere();
        moveAndResizeSphereAction.action.canceled += ctx => OnEndMoveAndResizeSphere();

        movingOrResizing = false;
        resizing = false;
        moving = false;

        touchpad = false;
        thumbstick = false;

        deletionOps = new List<int>();
        currentDeletionOpCount = 0;
        
        minimumSphereRadius = UserSettings.instance.preferences.cursorRadiusMin;
        maximumSphereRadius = UserSettings.instance.preferences.cursorRadiusMax;
        minimumSphereOffset = UserSettings.instance.preferences.cursorDistanceMin;
        maximumSphereOffset = UserSettings.instance.preferences.cursorDistanceMax;

        deleteRate = 1f / UserSettings.instance.preferences.cursorDeletionRate;

        deletionSphere.transform.localScale = Vector3.one * UserSettings.instance.preferences.cursorRadius;

        deletionSphere.transform.localPosition = Vector3.forward * UserSettings.instance.preferences.cursorDistance;

        setHighlightDeletedPointsActive(true);
    }

    private void OnApplicationQuit()
    {
        if (UserSettings.instance.preferences.saveCursorOnExit)
        {
            UserSettings.instance.preferences.cursorRadius = deletionSphere.transform.localScale.x;
            UserSettings.instance.preferences.cursorDistance = deletionSphere.transform.localPosition.z;
            UserSettings.instance.SaveToFile();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pcRoot == null)
            pcRoot = GameObject.Find("Point Clouds Root");

        if (movingOrResizing)
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
                        initialSphereRadius = deletionSphere.transform.localScale.x;
                    }
                    else
                    {
                        moving = true; // vertical swipe
                        movingOrResizing = false;
                        initialSphereDistance = deletionSphere.transform.localPosition.z;
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

        if (moving || resizing)
        {
            if (thumbstick)
                ThumbstickMoveOrResize();

            if (touchpad)
                TouchpadMoveOrResize();
        }

        MaintainSphereOffsetFromController();

        //UpdateSpherePositionInPlugin();

        if (Keyboard.current.jKey.wasPressedThisFrame)
            StartCoroutine(TestDeletion());
    }

    private IEnumerator TestDeletion()
    {
        int[] deletedPointCount =
        {
            263012,
            13585,
            130916,
            153210,
            92382,
            131323,
            37955,
            67696,
            194492,
            42134,
            1292
        };

        double[] times = {
            0,
            0.0448127000000227,
            0.0897679999999923,
            0.147307436150811,
            0.201390500000002,
            0.246885599999985,
            0.292302699999993,
            0.337679299999991,
            0.405794200000003,
            0.447307429445289,
            0.496555900000033,
            0.496555900000033
        };

        Vector3[] centers =
        {
            new Vector3(0.0764281f, -0.5221392f, 0.09180087f),
            new Vector3(0.08775251f, -0.5302286f, 0.07303953f),
            new Vector3(0.1198498f, -0.525166f, 0.1029456f),
            new Vector3(0.1625102f, -0.5231225f, 0.1266485f),
            new Vector3(0.2002803f, -0.5286195f, 0.1217237f),
            new Vector3(0.2412187f, -0.5219654f, 0.1481559f),
            new Vector3(0.259298f, -0.5154028f, 0.1613957f),
            new Vector3(0.2898942f, -0.5156108f, 0.1616098f),
            new Vector3(0.3288844f, -0.508836f, 0.1736135f),
            new Vector3(0.3471495f, -0.5040783f, 0.1845295f),
            new Vector3(0.3806944f, -0.4974589f, 0.1972415f)
        };

        Vector3 size = Vector3.one * 0.5f;

        for (int i = 0; i < times.Length - 1; i++)
        {
            float[] center = { centers[i].x, centers[i].y, centers[i].z };

            GCHandle toDelete = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);
            UpdateDeletionSpherePositionFromUnity(toDelete.AddrOfPinnedObject(), 79.00522f);

            int pointsDeleted = deleteInSphere(centers[i], 0.05f);

            if (pointsDeleted != deletedPointCount[i])
            {
                Debug.Log("WARNING: Deleted point counts on operation " + i + " did not agree!");
                Debug.Log("EXPECTED: " + deletedPointCount[i]);
                Debug.Log("ACTUAL: " + pointsDeleted);
            }

            yield return new WaitForSeconds((float)(times[i+1] - times[i]));
        }

        yield return new WaitForSeconds(1f);

        UndoDeletion();
    }

    void OnEnable()
    {
        deleteSphereAction.action.Enable();
        undoDeletionAction.action.Enable();
        moveAndResizeSphereAction.action.Enable();
    }

    void OnDisable()
    {
        deleteSphereAction.action.Disable();
        undoDeletionAction.action.Disable();
        moveAndResizeSphereAction.action.Disable();
    }

    void OnBeginDeleteSphere()
    {
        if (deletionSphere == null)
            return;

        if (CursorMaterial != null)
        {
            originalCursorColor = CursorMaterial.color;
            CursorMaterial.color = Color.red;
        }

        currentDeletionOpCount = 0;

        beginDeleteTimestamp = Time.timeAsDouble;
        InvokeRepeating("deleteInSphere", 0, deleteRate);
        //Debug.Log("Editing Started");
    }

    void OnEndDeleteSphere()
    {
        if (CursorMaterial != null)
        {
            CursorMaterial.color = originalCursorColor;
        }

        CancelInvoke("deleteInSphere");

        deletionOps.Add(currentDeletionOpCount);

        Debug.Log("Added " + currentDeletionOpCount + " operations to the undo list (" + deletionOps.Count + " undo actions remaining in queue)");
    }

    void deleteInSphere()
    {
        if (pcRoot == null)
            return;

        float[] center = new float[3];
        center[0] = deletionSphere.transform.position.x;
        center[1] = deletionSphere.transform.position.y;
        center[2] = deletionSphere.transform.position.z;
        
        GCHandle toDelete = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);
        int pointsDeleted = RequestToDeleteFromUnity(toDelete.AddrOfPinnedObject(), deletionSphere.transform.localScale.x);        

        if (pointsDeleted > 0)
        {
            currentDeletionOpCount++;

            Debug.Log(pointsDeleted + " points were deleted; total operations = " + currentDeletionOpCount);

            //Debug.Log(Time.timeAsDouble - beginDeleteTimestamp);
            //Debug.Log($"{deletionSphere.transform.position.x}f, {deletionSphere.transform.position.y}f, {deletionSphere.transform.position.z}f");
            //Debug.Log(deletionSphere.transform.localScale);

            // Send haptic feedback to right controller
            UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, deleteHaptic.amplitude, deleteHaptic.frequency, deleteHaptic.duration, UnityEngine.InputSystem.XR.XRController.rightHand);
        }
    }

    int deleteInSphere(Vector3 position, float radius)
    {
        if (pcRoot == null)
            return 0;

        float[] center = new float[3];
        center[0] = position.x;
        center[1] = position.y;
        center[2] = position.z;

        GCHandle toDelete = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);
        int pointsDeleted = RequestToDeleteFromUnity(toDelete.AddrOfPinnedObject(), radius);

        if (pointsDeleted > 0)
        {
            currentDeletionOpCount++;

            Debug.Log(pointsDeleted + " points were deleted; total operations = " + currentDeletionOpCount);
        }

        return pointsDeleted;
    }

    private void UndoDeletion()
    {
        if (deletionOps.Count > 0)
        {
            var numHalfSteps = deletionOps.Count;
            var frequency = undoHaptic.frequency * Mathf.Pow(2, numHalfSteps / 12f);
            UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, undoHaptic.amplitude, frequency, undoHaptic.duration);//, UnityEngine.InputSystem.XR.XRController.rightHand);

            Debug.Log("Requesting undo for the last " + deletionOps.Last() + " deletion operations (" + (deletionOps.Count - 1) + " undo actions remaining in queue)");
            undo(deletionOps.Last());

            deletionOps.RemoveAt(deletionOps.Count - 1);
        }
        else
        {
            StartCoroutine(NoMoreUndo());
        }
    }

    private void OnBeginMoveAndResizeSphere()
    {
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

    private void UpdateSpherePositionInPlugin()
    {
        float[] center = new float[3];

        GCHandle toDelete = GCHandle.Alloc(center, GCHandleType.Pinned);

        center[0] = deletionSphere.transform.position.x;
        center[1] = deletionSphere.transform.position.y;
        center[2] = deletionSphere.transform.position.z;
        
        UpdateDeletionSpherePositionFromUnity(toDelete.AddrOfPinnedObject(), (deletionSphere.transform.localScale.x) / scalingRoot.transform.localScale.x);
    }

    private void ThumbstickMoveOrResize()
    {
        if (resizing)
        {
            var delta = moveAndResizeSphereAction.action.ReadValue<Vector2>().x;

            var scaleFactor = Mathf.Exp(delta * 0.1F);

            var size = deletionSphere.transform.localScale.x * scaleFactor;            

            if (size < minimumSphereRadius)
                size = minimumSphereRadius;            
            else if (size > maximumSphereRadius)            
                size = maximumSphereRadius;            

            deletionSphere.transform.localScale = Vector3.one * size;

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

            deletionSphere.transform.localPosition += Vector3.forward * scaleFactor;

            if (deletionSphere.transform.localPosition.z < minimumSphereOffset)
                deletionSphere.transform.localPosition = Vector3.forward * minimumSphereOffset;
            else if (deletionSphere.transform.localPosition.z > maximumSphereOffset)
                deletionSphere.transform.localPosition = Vector3.forward * maximumSphereOffset;

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

            deletionSphere.transform.localPosition = Vector3.forward * offset;
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

            deletionSphere.transform.localScale = Vector3.one * size;
        }
    }
    
    private void MaintainSphereOffsetFromController()
    {
        // Keep sphere from getting too close to controller
        var currentRadius = deletionSphere.transform.localScale.x;// * 0.5f;

        if (deletionSphere.transform.localPosition.z < currentRadius)
        {
            deletionSphere.transform.localPosition = Vector3.forward * currentRadius;
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
}
