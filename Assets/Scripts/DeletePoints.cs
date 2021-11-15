using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem;
using UnityEditor;

public class DeletePoints : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern bool RequestToDeleteFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void setHighlightDeletedPointsActive(bool active);
    [DllImport("PointCloudPlugin")]
    static public extern void UpdateDeletionSpherePositionFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void undo(int numberToDelete);

    public GameObject scalingRoot;
    public InputAction deleteSphereAction;
    public InputAction undoDeletionAction;
    public InputAction moveAndResizeSphereAction;
    public float moveAndResizeThumbstickDeadzone = 0.1f;
    public float moveAndResizeTouchpadDelta = 0.2f;
    public float minimumSphereSize = 0.01f;
    public float maximumSphereSize = 0.5f;
    public float minimumSphereOffset = 0.1f;
    public float maximumSphereOffset = 2f;

    public float deleteRate = 0.25f;

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

        deleteSphereAction.started += ctx => OnBeginDeleteSphere();
        deleteSphereAction.canceled += ctx => OnEndDeleteSphere();

        undoDeletionAction.started += ctx => UndoDeletion();

        moveAndResizeSphereAction.started += ctx => OnBeginMoveAndResizeSphere();
        moveAndResizeSphereAction.canceled += ctx => OnEndMoveAndResizeSphere();

        movingOrResizing = false;
        resizing = false;
        moving = false;

        touchpad = false;
        thumbstick = false;

        deletionOps = new List<int>();
        currentDeletionOpCount = 0;

        setHighlightDeletedPointsActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (pcRoot == null)
            pcRoot = GameObject.Find("Point Clouds Root");

        if (movingOrResizing)
        {
            var sample = moveAndResizeSphereAction.ReadValue<Vector2>();

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
                if (Mathf.Abs(sample.x) > moveAndResizeThumbstickDeadzone && Mathf.Abs(sample.x) > Mathf.Abs(sample.y))
                {
                    resizing = true;
                    movingOrResizing = false;
                }
                else if (Mathf.Abs(sample.y) > moveAndResizeThumbstickDeadzone && Mathf.Abs(sample.x) < Mathf.Abs(sample.y))
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
        deleteSphereAction.Enable();
        undoDeletionAction.Enable();
        moveAndResizeSphereAction.Enable();
    }

    void OnDisable()
    {
        deleteSphereAction.Disable();
        undoDeletionAction.Disable();
        moveAndResizeSphereAction.Disable();
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
        Debug.Log("Editing Started");
    }

    void OnEndDeleteSphere()
    {
        if (CursorMaterial != null)
        {
            CursorMaterial.color = originalCursorColor;
        }

        CancelInvoke("deleteInSphere");
        Debug.Log("Editing Finished");

        deletionOps.Add(currentDeletionOpCount);
    }

    void deleteInSphere()
    {
        if (pcRoot == null)
            return;

        //Debug.Log(thisController.SendHapticImpulse(0.7f, 2f));

        float[] center = new float[3];
        center[0] = deletionSphere.transform.position.x;
        center[1] = deletionSphere.transform.position.y;
        center[2] = deletionSphere.transform.position.z;
        
        GCHandle toDelete = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);
        bool pointsWereDeleted = RequestToDeleteFromUnity(toDelete.AddrOfPinnedObject(), deletionSphere.transform.localScale.x);        

        if (pointsWereDeleted)
        {
            currentDeletionOpCount++;

            // haptic impulse here once you figure out how (or if it's even possible at the moment)
        }
    }

    private void UndoDeletion()
    {
        if (deletionOps.Count > 0)
        {
            undo(deletionOps.Last());

            deletionOps.RemoveAt(deletionOps.Count - 1);
        }        
    }

    private void OnBeginMoveAndResizeSphere()
    {
        movingOrResizing = true;

        if (moveAndResizeSphereAction.activeControl.displayName == "trackpad" || moveAndResizeSphereAction.activeControl.displayName == "touchpad")
        {
            touchpad = true;
            initialTouchpadTouchPoint = moveAndResizeSphereAction.ReadValue<Vector2>();
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
            var delta = moveAndResizeSphereAction.ReadValue<Vector2>().x;

            var scaleFactor = Mathf.Exp(delta * 0.1F);

            deletionSphere.transform.localScale *= scaleFactor;

            if (deletionSphere.transform.localScale.x < minimumSphereSize)
            {
                deletionSphere.transform.localScale = Vector3.one * minimumSphereSize;
            }

            if (Mathf.Abs(delta) <= moveAndResizeThumbstickDeadzone)
            {
                resizing = false;
                movingOrResizing = true;
            }
        }

        if (moving)
        {
            var delta = moveAndResizeSphereAction.ReadValue<Vector2>().y;

            var scaleFactor = delta * 0.1F;

            deletionSphere.transform.localPosition += Vector3.forward * scaleFactor;

            if (Mathf.Abs(delta) <= moveAndResizeThumbstickDeadzone)
            {
                moving = false;
                movingOrResizing = true;
            }
        }
    }

    private void TouchpadMoveOrResize()
    {
        var sample = moveAndResizeSphereAction.ReadValue<Vector2>();

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
}
