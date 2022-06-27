/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class implements the basic functionality control of the 3D map.
/// </summary>
[Serializable]
[RequireComponent(typeof(OnlineMapsMarker3DManager))]
public abstract class OnlineMapsControlBase3D: OnlineMapsControlBase
{
    #region Variables

    public Action OnUpdate3DMarkers;

    /// <summary>
    /// The camera you are using to display the map.
    /// </summary>
    public Camera activeCamera;

    /// <summary>
    /// Reference to the elevation manager
    /// </summary>
    public OnlineMapsElevationManagerBase elevationManager;

    /// <summary>
    /// Mode of 2D markers. Bake in texture or Billboard.
    /// </summary>
    public OnlineMapsMarker2DMode marker2DMode = OnlineMapsMarker2DMode.flat;

    /// <summary>
    /// Size of billboard markers.
    /// </summary>
    public float marker2DSize = 100;

    public OnlineMapsMarker3DManager marker3DManager;

    public Vector3 originalPosition;
    public Vector3 originalScale;

    private Collider _cl;
    private OnlineMapsMarker3DDrawer _marker3DDrawer;
    private Renderer _renderer;
    private MeshFilter _meshFilter;

    #endregion

    #region Properties

    /// <summary>
    /// Singleton instance of OnlineMapsControlBase3D control.
    /// </summary>
    public new static OnlineMapsControlBase3D instance
    {
        get { return _instance as OnlineMapsControlBase3D; }
    }

    /// <summary>
    /// Reference to the collider.
    /// </summary>
    public Collider cl
    {
        get
        {
            if (_cl == null) _cl = GetComponent<Collider>();
            return _cl;
        }
    }

    public MeshFilter meshFilter
    {
        get
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            return _meshFilter;
        }
    }

    public OnlineMapsMarker3DDrawer marker3DDrawer
    {
        get { return _marker3DDrawer; }
        set
        {
            if (_marker3DDrawer != null) _marker3DDrawer.Dispose();
            _marker3DDrawer = value;
        }
    }

    /// <summary>
    /// Reference to the renderer.
    /// </summary>
    public Renderer rendererInstance
    {
        get
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            return _renderer;
        }
    }

    #endregion

    #region Methods

    protected override void AfterUpdate()
    {
        base.AfterUpdate();

        Vector2 inputPosition = GetInputPosition();

        if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onHover && !map.blockAllInteractions)
        {
            OnlineMapsMarkerInstanceBase markerInstance = GetBillboardMarkerFromScreen(inputPosition);
            if (markerInstance != null)
            {
                OnlineMapsTooltipDrawerBase.tooltip = markerInstance.marker.label;
                OnlineMapsTooltipDrawerBase.tooltipMarker = markerInstance.marker;
            }
        }
    }

    /// <summary>
    /// Gets billboard marker on the screen position.
    /// </summary>
    /// <param name="screenPosition">Screen position.</param>
    /// <returns>Marker instance or null.</returns>
    public OnlineMapsMarkerInstanceBase GetBillboardMarkerFromScreen(Vector2 screenPosition)
    {
        //TODO: Find a way to refactory this method
        RaycastHit hit;
        if (Physics.Raycast(activeCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            return hit.collider.gameObject.GetComponent<OnlineMapsMarkerInstanceBase>();
        }
        return null;
    }

    public override IOnlineMapsInteractiveElement GetInteractiveElement(Vector2 screenPosition)
    {
        if (IsCursorOnUIElement(screenPosition)) return null;

        //TODO: Find a way to refactory this method
        RaycastHit hit;
        if (Physics.Raycast(activeCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            OnlineMapsMarkerInstanceBase markerInstance = hit.collider.gameObject.GetComponent<OnlineMapsMarkerInstanceBase>();
            if (markerInstance != null) return markerInstance.marker;
        }

        OnlineMapsMarker marker = markerDrawer.GetMarkerFromScreen(screenPosition);
        if (marker != null) return marker;

        OnlineMapsDrawingElement drawingElement = map.GetDrawingElement(screenPosition);
        return drawingElement;
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        double px, py;
        GetPosition(lng, lat, out px, out py);
        px /= map.width;
        py /= map.height;

        Bounds bounds = cl.bounds;
        Vector3 worldPos = new Vector3(
            (float)(bounds.max.x - bounds.size.x * px),
            bounds.min.y,
            (float)(bounds.min.z + bounds.size.z * py)
        );

        Camera cam = activeCamera != null? activeCamera: Camera.main;
        return cam.WorldToScreenPoint(worldPos);
    }

    protected override void OnDestroyLate()
    {
        base.OnDestroyLate();

        marker3DDrawer = null;
        _cl = null;
        _meshFilter = null;
        _renderer = null;
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        marker3DManager = GetComponent<OnlineMapsMarker3DManager>();
        elevationManager = GetComponent<OnlineMapsElevationManagerBase>();

        OnlineMapsMarker3DManager.Init();
        marker3DDrawer = new OnlineMapsMarker3DDrawer(this);
        if (activeCamera == null) activeCamera = Camera.main;
    }

    protected override OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONItem json = base.SaveSettings();
        json.AppendObject(new
        {
            marker2DMode,
            marker2DSize,
            activeCamera
        });

        return json;
    }

    /// <summary>
    /// Updates the current control.
    /// </summary>
    public virtual void UpdateControl()
    {
        if (OnDrawMarkers != null) OnDrawMarkers();
        if (OnUpdate3DMarkers != null) OnUpdate3DMarkers();
    }

    #endregion
}