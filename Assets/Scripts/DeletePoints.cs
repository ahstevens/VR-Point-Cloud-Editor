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
    public float minimumSphereSize;
    public float maximumSphereSize;
    public float minimumSphereOffset;
    public float maximumSphereOffset;

    public float deleteRate;

    public HapticPattern deleteHaptic;
    public HapticPattern undoHaptic;

    private GameObject deletionSphere;
    private GameObject connectingRod;
    private GameObject pcRoot;

    private bool movingOrResizing;
    private bool resizing;
    private bool moving;

    private bool thumbstick;
    private bool touchpad;
    private bool horizontalSwipe;
    private bool verticalSwipe;
    private Vector2 initialTouchpadTouchPoint;
    private Vector2 initialTouchpadMeasurementPoint;
    private float initialSphereSize;
    private float initialSphereDistance;

    public Material CursorMaterial;
    private Color originalCursorColor;

    private List<int> deletionOps;
    private int currentDeletionOpCount;

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
        
        minimumSphereSize = UserSettings.instance.GetPreferences().cursorMinSize;
        maximumSphereSize = UserSettings.instance.GetPreferences().cursorMaxSize;
        minimumSphereOffset = UserSettings.instance.GetPreferences().cursorMinDistance;
        maximumSphereOffset = UserSettings.instance.GetPreferences().cursorMaxDistance;

        deleteRate = UserSettings.instance.GetPreferences().cursorDeletionRate;

        deletionSphere.transform.localScale = Vector3.one * UserSettings.instance.GetPreferences().cursorSize;

        deletionSphere.transform.localPosition = Vector3.forward * UserSettings.instance.GetPreferences().cursorDistance;

        setHighlightDeletedPointsActive(true);
    }

    private void OnApplicationQuit()
    {
        if (UserSettings.instance.GetPreferences().saveCursorOnExit)
        {
            UserSettings.instance.GetPreferences().cursorSize = deletionSphere.transform.localScale.x;
            UserSettings.instance.GetPreferences().cursorDistance = deletionSphere.transform.localPosition.z;
            UserSettings.instance.SaveToFile();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pcRoot == null)
            pcRoot = GameObject.Find("Point Clouds Root");

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Application.Quit();

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
                        initialSphereSize = deletionSphere.transform.localScale.x;
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

        UpdateSpherePositionInPlugin();
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
        //Debug.Log("Editing Finished");

        if (currentDeletionOpCount > 0)
            deletionOps.Add(currentDeletionOpCount);
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

            Debug.Log(pointsDeleted + " points were deleted");

            // Send haptic feedback to right controller
            UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, deleteHaptic.amplitude, deleteHaptic.frequency, deleteHaptic.duration, UnityEngine.InputSystem.XR.XRController.rightHand);
        }
    }

    private void UndoDeletion()
    {
        if (deletionOps.Count > 0)
        {
            var numHalfSteps = deletionOps.Count;
            var frequency = undoHaptic.frequency * Mathf.Pow(2, numHalfSteps / 12f);
            UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(hapticAction.action, undoHaptic.amplitude, frequency, undoHaptic.duration);//, UnityEngine.InputSystem.XR.XRController.rightHand);

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

            if (size < minimumSphereSize)
                size = minimumSphereSize;            
            else if (size > maximumSphereSize)            
                size = maximumSphereSize;            

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

            float range = maximumSphereSize - minimumSphereSize;

            var size = initialSphereSize + dx * range;

            if (size > maximumSphereSize)
            {
                size = maximumSphereSize;
                initialSphereSize = maximumSphereSize;
                initialTouchpadMeasurementPoint.x = sample.x;
            }
            else if (size < minimumSphereSize)
            {
                size = minimumSphereSize;
                initialSphereSize = minimumSphereSize;
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
