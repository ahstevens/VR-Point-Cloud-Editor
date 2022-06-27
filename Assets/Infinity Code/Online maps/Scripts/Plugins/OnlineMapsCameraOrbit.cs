/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if (!UNITY_ANDROID && !UNITY_IPHONE) || UNITY_EDITOR
#define USE_MOUSE_ROTATION
#endif

using System;
using UnityEngine;

/// <summary>
/// Implements camera rotation around the map
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Camera Orbit")]
[OnlineMapsPlugin("Camera Orbit", typeof(OnlineMapsControlBaseDynamicMesh), true)]
public class OnlineMapsCameraOrbit : MonoBehaviour, IOnlineMapsSavableComponent
{
    /// <summary>
    /// Called when the rotation has been changed in any way
    /// </summary>
    public Action OnCameraControl;

    /// <summary>
    /// Called when the rotation has been changed by Input
    /// </summary>
    public Action OnChangedByInput;

    /// <summary>
    /// Point on which the camera is looking
    /// </summary>
    public OnlineMapsCameraAdjust adjustTo = OnlineMapsCameraAdjust.averageCenter;

    /// <summary>
    /// GameObject on which the camera is looking
    /// </summary>
    public GameObject adjustToGameObject;

    /// <summary>
    /// Distance from point to camera
    /// </summary>
    public float distance = 1000;

    /// <summary>
    /// Maximum camera tilt
    /// </summary>
    public float maxRotationX = 80;

    /// <summary>
    /// Camera rotation (X - tilt, Y - pan)
    /// </summary>
    public Vector2 rotation = Vector2.zero;

    /// <summary>
    /// Camera rotation speed
    /// </summary>
    public Vector2 speed = Vector2.one;

    /// <summary>
    /// Forbid changing tilt (rotation.x)
    /// </summary>
    public bool lockTilt;

    /// <summary>
    /// Forbid changing pan (rotation.y)
    /// </summary>
    public bool lockPan;

    private static OnlineMapsCameraOrbit _instance;

    private OnlineMaps map;
    private OnlineMapsControlBaseDynamicMesh control;

    private bool isCameraControl;
    private Vector2 lastInputPosition;
    private OnlineMapsSavableItem[] savableItems;

    /// <summary>
    /// Instance
    /// </summary>
    public static OnlineMapsCameraOrbit instance
    {
        get { return _instance; }
    }

    private Camera activeCamera
    {
        get { return control.activeCamera; }
    }

    private Vector2 sizeInScene
    {
        get
        {
            return control.sizeInScene;
        }
    }

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("cameraOrbit", "Camera Orbit", SaveSettings)
            {
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    private void LateUpdate()
    {
        UpdateCameraPosition();
    }

    private void LoadSettings(OnlineMapsJSONObject obj)
    {
        obj.DeserializeObject(this);
    }

    private OnlineMapsJSONItem SaveSettings()
    {
        return OnlineMapsJSON.Serialize(this);
    }

    private void OnEnable()
    {
        _instance = this;
        map = GetComponent<OnlineMaps>();
        if (map == null) map = FindObjectOfType<OnlineMaps>();
        control = map.control as OnlineMapsControlBaseDynamicMesh;
    }

    private void Start()
    {
        if (control != null) control.OnMeshUpdated += UpdateCameraPosition;
    }

    private void Update()
    {
#if USE_MOUSE_ROTATION
        if (Input.GetMouseButton(1))
        { 
            Vector2 inputPosition = control.GetInputPosition();
#else
        if (Input.touchCount == 2)
        {
            Vector2 p1 = Input.GetTouch(0).position;
            Vector2 p2 = Input.GetTouch(1).position;

            Vector2 inputPosition = Vector2.Lerp(p1, p2, 0.5f);
#endif
            if (!control.IsCursorOnUIElement(inputPosition))
            {
                isCameraControl = true;
                if (lastInputPosition == Vector2.zero) lastInputPosition = inputPosition;
                if (lastInputPosition != inputPosition && lastInputPosition != Vector2.zero)
                {
                    Vector2 offset = lastInputPosition - inputPosition;
                    bool changed = offset.sqrMagnitude > 0 && (!lockPan || !lockTilt);
                    if (!lockTilt) rotation.x -= offset.y / 10f * speed.x;
                    if (!lockPan) rotation.y -= offset.x / 10f * speed.y;

                    if (changed && OnChangedByInput != null) OnChangedByInput();
                }
                lastInputPosition = inputPosition;
            }
        }
        else if (isCameraControl)
        {
            lastInputPosition = Vector2.zero;
            isCameraControl = false;
        }
    }

    /// <summary>
    /// Updates camera position
    /// </summary>
    public void UpdateCameraPosition()
    {
        if (rotation.x > maxRotationX) rotation.x = maxRotationX;
        else if (rotation.x < 0) rotation.x = 0;

        float rx = 90 - rotation.x;
        if (rx > 89.9) rx = 89.9f;

        double px = Math.Cos(rx * Mathf.Deg2Rad) * distance;
        double py = Math.Sin(rx * Mathf.Deg2Rad) * distance;
        double pz = Math.Cos(rotation.y * Mathf.Deg2Rad) * px;
        px = Math.Sin(rotation.y * Mathf.Deg2Rad) * px;

        Vector3 targetPosition;

        if (adjustTo == OnlineMapsCameraAdjust.gameObject && adjustToGameObject != null)
        {
            targetPosition = adjustToGameObject.transform.position;
        }
        else
        {
            targetPosition = map.transform.position;
            Vector3 offset = new Vector3(sizeInScene.x / -2, 0, sizeInScene.y / 2);

            if (OnlineMapsElevationManagerBase.useElevation)
            {
                double tlx, tly, brx, bry;
                map.GetCorners(out tlx, out tly, out brx, out bry);
                float yScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);

                if (adjustTo == OnlineMapsCameraAdjust.maxElevationInArea) offset.y = OnlineMapsElevationManagerBase.instance.GetMaxElevation(yScale);
                else if (adjustTo == OnlineMapsCameraAdjust.averageCenter)
                {
                    float ox = sizeInScene.x / 64;
                    float oz = sizeInScene.y / 64;
                    offset.y = OnlineMapsElevationManagerBase.GetElevation(targetPosition.x, targetPosition.z, yScale, tlx, tly, brx, bry) * 3;

                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x - ox, targetPosition.z - oz, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x, targetPosition.z - oz, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x + ox, targetPosition.z - oz, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x + ox, targetPosition.z, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x + ox, targetPosition.z + oz, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x, targetPosition.z + oz, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x - ox, targetPosition.z + oz, yScale, tlx, tly, brx, bry) * 2;
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x - ox, targetPosition.z, yScale, tlx, tly, brx, bry) * 2;

                    ox *= 2;
                    oz *= 2;

                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x - ox, targetPosition.z - oz, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x, targetPosition.z - oz, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x + ox, targetPosition.z - oz, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x + ox, targetPosition.z, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x + ox, targetPosition.z + oz, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x, targetPosition.z + oz, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x - ox, targetPosition.z + oz, yScale, tlx, tly, brx, bry);
                    offset.y += OnlineMapsElevationManagerBase.GetElevation(targetPosition.x - ox, targetPosition.z, yScale, tlx, tly, brx, bry);

                    offset.y /= 27;
                }
                else offset.y = OnlineMapsElevationManagerBase.GetElevation(targetPosition.x, targetPosition.z, yScale, tlx, tly, brx, bry);
            }

            offset.Scale(map.transform.lossyScale);
            
            targetPosition += map.transform.rotation * offset;
        }

        Vector3 oldPosition = activeCamera.transform.position;
        Vector3 newPosition = map.transform.rotation * new Vector3((float)px,  (float)py, (float)pz) + targetPosition;

        activeCamera.transform.position = newPosition;
        activeCamera.transform.LookAt(targetPosition);

        if (control.isMapDrag) control.UpdateLastPosition();

        if (oldPosition != newPosition && OnCameraControl != null) OnCameraControl();
    }
}