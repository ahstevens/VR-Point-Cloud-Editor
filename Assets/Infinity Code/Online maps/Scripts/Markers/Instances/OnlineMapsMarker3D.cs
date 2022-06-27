/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 3D marker class.<br/>
/// <strong>Can be used only when the source display - Texture or Tileset.</strong>
/// </summary>
[Serializable]
public class OnlineMapsMarker3D : OnlineMapsMarkerBase
{
    public Func<bool> OnCheckMapBoundaries;

    /// <summary>
    /// Altitude (meters).
    /// </summary>
    public float? altitude;

    /// <summary>
    /// Type of altitude
    /// </summary>
    public OnlineMapsAltitudeType altitudeType = OnlineMapsAltitudeType.absolute;

    /// <summary>
    /// Need to check the map boundaries?<br/>
    /// It allows you to make 3D markers, which are active outside the map.
    /// </summary>
    public bool checkMapBoundaries = true;

    /// <summary>
    /// Reference of 3D control.
    /// </summary>
    [NonSerialized]
    public OnlineMapsControlBase3D control;

    /// <summary>
    /// Specifies whether the marker is initialized.
    /// </summary>
    [HideInInspector] 
    public bool inited = false;

    /// <summary>
    /// The instance.
    /// </summary>
    [NonSerialized, HideInInspector]
    public GameObject instance;

    /// <summary>
    /// Marker prefab GameObject.
    /// </summary>
    public GameObject prefab;

    /// <summary>
    /// How marker size will be calculated.
    /// </summary>
    public SizeType sizeType = SizeType.scene;

    [NonSerialized]
    public GameObject _prefab;

    private Vector3 _relativePosition;
    private bool _visible = true;

    [SerializeField]
    private float _rotationY = 0;

    /// <summary>
    /// Gets or sets marker enabled.
    /// </summary>
    /// <value>
    /// true if enabled, false if not.
    /// </value>
    public override bool enabled
    {
        set
        {
            if (_enabled != value)
            {
                _enabled = value;

                if (!value) visible = false;
                else if (OnlineMaps.isPlaying) Update();

                if (OnEnabledChange != null) OnEnabledChange(this);
            }
        }
    }

    protected OnlineMapsElevationManagerBase elevationManager
    {
        get { return control.elevationManager; }
    }

    protected bool hasElevation
    {
        get { return elevationManager != null && elevationManager.enabled; }
    }

    /// <summary>
    /// Returns the position of the marker relative to Texture.
    /// </summary>
    /// <value>
    /// The relative position.
    /// </value>
    public Vector3 relativePosition
    {
        get
        {
            return enabled ? _relativePosition : Vector3.zero;
        }
    }

    /// <summary>
    /// Gets or sets rotation of 3D marker.
    /// </summary>
    public Quaternion rotation
    {
        get { return transform != null? transform.localRotation : new Quaternion(); }
        set
        {
            if (transform != null)
            {
                transform.localRotation = value;
                _rotationY = value.eulerAngles.y;
            }
        }
    }

    /// <summary>
    /// Y rotation of 3D marker (degree).
    /// </summary>
    public float rotationY
    {
        get { return _rotationY; }
        set
        {
            _rotationY = value;
            rotation = Quaternion.Euler(0, value, 0);
        }
    }

    /// <summary>
    /// Gets the instance transform.
    /// </summary>
    /// <value>
    /// The transform.
    /// </value>
    public Transform transform
    {
        get
        {
            return instance != null? instance.transform: null;
        }
    }

    private bool visible
    {
        get { return _visible; }
        set
        {
            if (_visible == value) return;
            _visible = value;
            instance.SetActive(value);
        }
    }

    /// <summary>
    /// Constructor of 3D marker
    /// </summary>
    public OnlineMapsMarker3D()
    {
        
    }

    /// <summary>
    /// Create 3D marker from an existing GameObject.
    /// </summary>
    /// <param name="instance">GameObject to be used as a 3D marker.</param>
    public OnlineMapsMarker3D(GameObject instance):this()
    {
        prefab = _prefab = instance;
        this.instance = instance;
        instance.AddComponent<OnlineMapsMarker3DInstance>().marker = this;
        Update();
    }

    public override void DestroyInstance()
    {
        base.DestroyInstance();

        if (instance != null)
        {
            OnlineMapsUtils.Destroy(instance);
            instance = null;
        }
    }

    /// <summary>
    /// Initializes this object.
    /// </summary>
    /// <param name="parent">
    /// The parent transform.
    /// </param>
    public void Init(Transform parent)
    {
        if (instance != null) OnlineMapsUtils.Destroy(instance);

        if (prefab == null)
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.transform.localScale = Vector3.one;
        }
        else instance = Object.Instantiate(prefab) as GameObject;

        _prefab = prefab;
        
        instance.transform.parent = parent;
        instance.transform.localRotation = Quaternion.Euler(0, _rotationY, 0);

        instance.layer = parent.gameObject.layer;
        instance.AddComponent<OnlineMapsMarker3DInstance>().marker = this;
        _visible = false;
        instance.SetActive(false);
        inited = true;

        control = map.control as OnlineMapsControlBase3D;

        Update();

        if (OnInitComplete != null) OnInitComplete(this);
    }

    public override void LookToCoordinates(OnlineMapsVector2d coordinates)
    {
        double p1x, p1y, p2x, p2y;
        map.projection.CoordinatesToTile(coordinates.x, coordinates.y, 20, out p1x, out p1y);
        map.projection.CoordinatesToTile(longitude, latitude, 20, out p2x, out p2y);
        rotation = Quaternion.Euler(0, (float)(OnlineMapsUtils.Angle2D(p1x, p1y, p2x, p2y) - 90), 0);
    }

    /// <summary>
    /// Reinitialises this object.
    /// </summary>
    public void Reinit()
    {
        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        Reinit(tlx, tly, brx, bry, map.zoom);
    }

    /// <summary>
    /// Reinitialises this object.
    /// </summary>
    /// <param name="tlx">Top-left longitude of map</param>
    /// <param name="tly">Top-left latitude of map</param>
    /// <param name="brx">Bottom-right longitude of map</param>
    /// <param name="bry">Bottom-right latitude of map</param>
    /// <param name="zoom">Map zoom</param>
    public void Reinit(double tlx, double tly, double brx, double bry, int zoom)
    {
        if (instance != null)
        {
            Transform parent = instance.transform.parent;
            OnlineMapsUtils.Destroy(instance);
            Init(parent);
        }
        Update(tlx, tly, brx, bry, zoom);
        if (OnInitComplete != null) OnInitComplete(this);
    }

    public override OnlineMapsJSONItem ToJSON()
    {
        return base.ToJSON().AppendObject(new
        {
            prefab = prefab != null ? prefab.GetInstanceID() : 0,
            rotationY = _rotationY,
            sizeType = (int)sizeType
        });
    }

    /// <summary>
    /// Updates marker instance.
    /// </summary>
    public override void Update()
    {
        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        Update(tlx, tly, brx, bry, map.zoom);
    }

    /// <summary>
    /// Updates marker instance.
    /// </summary>
    /// <param name="tlx">Longitude of top-left corner of the map</param>
    /// <param name="tly">Latitude of top-left corner of the map</param>
    /// <param name="brx">Longitude of bottom-right corner of the map</param>
    /// <param name="bry">Latitude of bottom-right corner of the map</param>
    /// <param name="zoom">Zoom of the map</param>
    public override void Update(double tlx, double tly, double brx, double bry, int zoom)
    {
        if (control == null) control = OnlineMapsControlBase3D.instance;
        if (control.meshFilter == null) return;
        double ttlx, ttly, tbrx, tbry;
        map.GetTileCorners(out ttlx, out ttly, out tbrx, out tbry, zoom);
        float bestYScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);
        Update(control.meshFilter.sharedMesh.bounds, tlx, tly, brx, bry, zoom, ttlx, ttly, tbrx, tbry, bestYScale);
    }

    /// <summary>
    /// Updates marker instance.
    /// </summary>
    /// <param name="bounds">Bounds of the map mesh</param>
    /// <param name="tlx">Longitude of top-left corner of the map</param>
    /// <param name="tly">Latitude of top-left corner of the map</param>
    /// <param name="brx">Longitude of bottom-right corner of the map</param>
    /// <param name="bry">Latitude of bottom-right corner of the map</param>
    /// <param name="zoom">Zoom of the map</param>
    /// <param name="ttlx">Tile X of top-left corner of the map</param>
    /// <param name="ttly">Tile Y of top-left corner of the map</param>
    /// <param name="tbrx">Tile X of bottom-right corner of the map</param>
    /// <param name="tbry">Tile Y of bottom-right corner of the map</param>
    /// <param name="bestYScale">Best y scale for current map view</param>
    public void Update(Bounds bounds, double tlx, double tly, double brx, double bry, int zoom, double ttlx, double ttly, double tbrx, double tbry, float bestYScale)
    {
        if (!enabled) return;
        if (instance == null) Init(control.marker3DManager.container);

        if (!range.InRange(zoom)) visible = false;
        else if (OnCheckMapBoundaries != null) visible = OnCheckMapBoundaries();
        else if (checkMapBoundaries)
        {
            if (latitude > tly || latitude < bry) visible = false;
            else if (tlx < brx &&
                     (longitude < tlx || longitude > brx) &&
                     (longitude - 360 < tlx || longitude - 360 > brx) &&
                     (longitude + 360 < tlx || longitude + 360 > brx)) visible = false;
            else if (tlx > brx && longitude < tlx && longitude > brx) visible = false;
            else visible = true;
        }
        else visible = true;

        if (!visible) return;

        if (_prefab != prefab) Reinit(tlx, tly, brx, bry, zoom);

        double mx, my;
        map.projection.CoordinatesToTile(longitude, latitude, zoom, out mx, out my);

        int maxX = 1 << zoom;

        double sx = tbrx - ttlx;
        double mpx = mx - ttlx;
        if (sx <= 0) sx += maxX;

        if (checkMapBoundaries)
        {
            if (mpx < 0) mpx += maxX;
            else if (mpx > maxX) mpx -= maxX;
        }
        else
        {
            double dx1 = Math.Abs(mpx - ttlx);
            double dx2 = Math.Abs(mpx - tbrx);
            double dx3 = Math.Abs(mpx - tbrx + maxX);
            if (dx1 > dx2 && dx1 > dx3) mpx += maxX;
        }

        double px = mpx / sx;
        double pz = (ttly - my) / (ttly - tbry);

        _relativePosition = new Vector3((float)px, 0, (float)pz);

        OnlineMapsTileSetControl tsControl = control as OnlineMapsTileSetControl;

        if (tsControl != null)
        {
            px = -tsControl.sizeInScene.x / 2 - (px - 0.5) * tsControl.sizeInScene.x;
            pz = tsControl.sizeInScene.y / 2 + (pz - 0.5) * tsControl.sizeInScene.y;
        }
        else
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            px = center.x - (px - 0.5) * size.x / map.transform.lossyScale.x;
            pz = center.z + (pz - 0.5) * size.z / map.transform.lossyScale.z;
        }

        Vector3 oldPosition = instance.transform.localPosition;
        float y = 0;

        bool elevationActive = hasElevation;

        if (altitude.HasValue)
        {
            float yScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);
            y = altitude.Value;
            if (altitudeType == OnlineMapsAltitudeType.relative && tsControl != null && elevationActive) y += elevationManager.GetUnscaledElevationValue(px, pz, tlx, tly, brx, bry);
            y *= yScale;

            if (tsControl != null && elevationActive)
            {
                if (elevationManager.bottomMode == OnlineMapsElevationBottomMode.minValue) y -= elevationManager.minValue * bestYScale;
                y *= elevationManager.scale;
            }
        }
        else if (tsControl != null && elevationActive)
        {
            y = elevationManager.GetElevationValue(px, pz, bestYScale, tlx, tly, brx, bry);
        }

        Vector3 newPosition = new Vector3((float)px, y, (float)pz);

        if (sizeType == SizeType.meters)
        {
            double dx, dy;
            OnlineMapsUtils.DistanceBetweenPoints(tlx, tly, brx, bry, out dx, out dy);
            if (tsControl != null)
            {
                dx = tsControl.sizeInScene.x / dx / 1000;
                dy = tsControl.sizeInScene.y / dy / 1000;
            }

            double d = (dx + dy) / 2 * scale;
            float fd = (float)d;

            instance.transform.localScale = new Vector3(fd, fd, fd);
        }

        if (oldPosition != newPosition) instance.transform.localPosition = newPosition;
    }

    /// <summary>
    /// Updates marker instance.
    /// </summary>
    /// <param name="map">Reference to the map</param>
    /// <param name="control">Reference to the control</param>
    /// <param name="bounds">Bounds of the map mesh</param>
    /// <param name="tlx">Longitude of top-left corner of the map</param>
    /// <param name="tly">Latitude of top-left corner of the map</param>
    /// <param name="brx">Longitude of bottom-right corner of the map</param>
    /// <param name="bry">Latitude of bottom-right corner of the map</param>
    /// <param name="zoom">Zoom of the map</param>
    /// <param name="ttlx">Tile X of top-left corner of the map</param>
    /// <param name="ttly">Tile Y of top-left corner of the map</param>
    /// <param name="tbrx">Tile X of bottom-right corner of the map</param>
    /// <param name="tbry">Tile Y of bottom-right corner of the map</param>
    /// <param name="bestYScale">Best y scale for current map view</param>
    public void Update(OnlineMaps map, OnlineMapsControlBase3D control, Bounds bounds, double tlx, double tly, double brx, double bry, int zoom, double ttlx, double ttly, double tbrx, double tbry, float bestYScale)
    {
        Update(bounds, tlx, tly, brx, bry, zoom, ttlx, ttly, tbrx, tbry, bestYScale);
    }

    /// <summary>
    /// Type of 3d marker size
    /// </summary>
    public enum SizeType
    {
        /// <summary>
        /// Uses transform.scale of marker instance. Same for each zoom level
        /// </summary>
        scene,

        /// <summary>
        /// Scale is 1 for zoom - 20, and is halved every previous zoom
        /// </summary>
        realWorld,

        /// <summary>
        /// Specific marker size in meters
        /// </summary>
        meters
    }
}
