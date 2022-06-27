/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The base class for markers.
/// </summary>
[Serializable]
public class OnlineMapsMarkerBase: IOnlineMapsInteractiveElement
{
    /// <summary>
    /// Default event caused to draw tooltip.
    /// </summary>
    public static Action<OnlineMapsMarkerBase> OnMarkerDrawTooltip;

    /// <summary>
    /// Events that occur when user click on the marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnClick;

    /// <summary>
    /// Events that occur when user double click on the marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnDoubleClick;

    /// <summary>
    /// Events that occur when user drag the marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnDrag;

    /// <summary>
    /// Event caused to draw tooltip.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnDrawTooltip;

    /// <summary>
    /// Event occurs when the marker enabled change.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnEnabledChange;

    /// <summary>
    /// Event occurs when the marker is initialized.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnInitComplete;

    /// <summary>
    /// Events that occur when user long press on the marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnLongPress;

    /// <summary>
    /// Events that occur when user press on the marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnPress;

    /// <summary>
    /// Event that occurs when the marker position changed.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnPositionChanged;

    /// <summary>
    /// Events that occur when user release on the marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnRelease;

    /// <summary>
    /// Events that occur when user roll out marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnRollOut;

    /// <summary>
    /// Events that occur when user roll over marker.
    /// </summary>
    public Action<OnlineMapsMarkerBase> OnRollOver;

    /// <summary>
    /// Marker label.
    /// </summary>
    public string label = "";

    /// <summary>
    /// Zoom range, in which the marker will be displayed.
    /// </summary>
    public OnlineMapsRange range;

    /// <summary>
    /// List of tags.
    /// </summary>
    public List<string> tags;

    [SerializeField]
    protected bool _enabled = true;

    private Dictionary<string, object> _customFields;

    [SerializeField]
    protected double latitude;

    [SerializeField]
    protected double longitude;

    [SerializeField]
    protected float _scale = 1;

    [SerializeField]
    protected bool expand = true;

    private IOnlineMapsInteractiveElementManager _manager;
    private bool _isDraggable;

    /// <summary>
    /// Get customFields dictionary.
    /// </summary>
    public Dictionary<string, object> customFields
    {
        get
        {
            if (_customFields == null) _customFields = new Dictionary<string, object>();
            return _customFields;
        }
    }

    /// <summary>
    /// Get or set a value in the customFields dictionary by key.
    /// </summary>
    /// <param name="key">Field key.</param>
    /// <returns>Field value.</returns>
    public object this[string key]
    {
        get
        {
            object val;
            return customFields.TryGetValue(key, out val)? val: null;
        }
        set { customFields[key] = value; }
    }

    /// <summary>
    /// Gets or sets marker enabled.
    /// </summary>
    /// <value>
    /// true if enabled, false if not.
    /// </value>
    public virtual bool enabled
    {
        get { return _enabled; }
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                if (OnEnabledChange != null) OnEnabledChange(this);
            }
        }
    }

    /// <summary>
    /// Makes the marker draggable or un draggable
    /// </summary>
    public bool isDraggable
    {
        get { return _isDraggable; }
        set
        {
            if (_isDraggable == value) return;

            if (value)
            {
                OnPress -= OnMarkerPress;
                OnPress += OnMarkerPress;
            }
            else OnPress -= OnMarkerPress;

            _isDraggable = value;
        }
    }

    /// <summary>
    /// Reference to Marker Manager
    /// </summary>
    public IOnlineMapsInteractiveElementManager manager
    {
        get { return _manager != null? _manager: OnlineMapsMarkerManager.instance; }
        set { _manager = value; }
    }

    protected OnlineMaps map
    {
        get
        {
            if (_manager == null) return OnlineMaps.instance;
            return _manager.map;
        }
    }

    /// <summary>
    /// Marker coordinates.
    /// </summary>
    public Vector2 position
    {
        get
        {
            return new Vector2((float)longitude, (float)latitude);
        }
        set
        {
            longitude = value.x;
            latitude = value.y;
            if (OnPositionChanged != null) OnPositionChanged(this);
        }
    }

    /// <summary>
    /// Scale of marker.
    /// </summary>
    public virtual float scale
    {
        get { return _scale; }
        set { _scale = value; }
    }

    /// <summary>
    /// Checks to display marker in current map view.
    /// </summary>
    public virtual bool inMapView
    {
        get
        {
            if (!enabled) return false;

            if (!range.InRange(map.zoom)) return false;

            double tlx, tly, brx, bry;
            map.GetCorners(out tlx, out tly, out brx, out bry);

            if (latitude < bry || latitude > tly) return false;

            bool isEntireWorld = 1 << map.zoom == map.width / OnlineMapsUtils.tileSize;
            if (isEntireWorld) return true;

            if (tlx > brx) brx += 360;

            double lng = longitude;

            if (tlx - lng > 180) lng += 360;
            else if (tlx - lng < -180) lng -= 360;

            if (lng < tlx || lng > brx) return false;
            return true;
        }
    }

    public OnlineMapsMarkerBase()
    {
        range = new OnlineMapsRange(OnlineMaps.MINZOOM, OnlineMaps.MAXZOOM_EXT);
        tags = new List<string>();
    }

    public virtual void DestroyInstance()
    {
        
    }

    /// <summary>
    /// Disposes marker
    /// </summary>
    public virtual void Dispose()
    {
        tags = null;
        _customFields = null;
        _manager = null;

        OnClick = null;
        OnDoubleClick = null;
        OnDrag = null;
        OnDrawTooltip = null;
        OnEnabledChange = null;
        OnInitComplete = null;
        OnLongPress = null;
        OnPress = null;
        OnRelease = null;
        OnRollOut = null;
        OnRollOver = null;

        DestroyInstance();
    }

    /// <summary>
    /// Gets location of marker.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public void GetPosition(out double lng, out double lat)
    {
        lng = longitude;
        lat = latitude;
    }

    /// <summary>
    /// Get tile position of the marker
    /// </summary>
    /// <param name="px">Tile X</param>
    /// <param name="py">Tile Y</param>
    public void GetTilePosition(out double px, out double py)
    {
        map.projection.CoordinatesToTile(longitude, latitude, map.zoom, out px, out py);
    }

    /// <summary>
    /// Get tile position of the marker
    /// </summary>
    /// <param name="px">Tile X</param>
    /// <param name="py">Tile Y</param>
    /// <param name="zoom">Zoom</param>
    public void GetTilePosition(out double px, out double py, int zoom)
    {
        map.projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);
    }

    /// <summary>
    /// Get tile position of the marker
    /// </summary>
    /// <param name="zoom">Zoom</param>
    /// <param name="px">Tile X</param>
    /// <param name="py">Tile Y</param>
    public void GetTilePosition(int zoom, out double px, out double py)
    {
        map.projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);
    }

    /// <summary>
    /// Checks if the marker is in the current map view
    /// </summary>
    /// <returns>True - in map view. False - outside map view</returns>
    public bool InMapView()
    {
        return map.InMapView(longitude, latitude);
    }

    /// <summary>
    /// Turns the marker in the direction specified coordinates
    /// </summary>
    /// <param name="coordinates">The coordinates</param>
    public virtual void LookToCoordinates(OnlineMapsVector2d coordinates)
    {
        
    }

    /// <summary>
    /// Turns the marker in the direction specified coordinates
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    public void LookToCoordinates(double longitude, double latitude)
    {
        LookToCoordinates(new OnlineMapsVector2d(longitude, latitude));
    }

    private void OnMarkerPress(OnlineMapsMarkerBase marker)
    {
        map.control.dragMarker = this;
    }

    /// <summary>
    /// Set location of marker
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public void SetPosition(double lng, double lat)
    {
        longitude = lng;
        latitude = lat;
        if (OnPositionChanged != null) OnPositionChanged(this);
    }

    /// <summary>
    /// Makes the marker draggable or un draggable
    /// </summary>
    /// <param name="value">True - set draggable, false - unset draggable</param>
    public void SetDraggable(bool value = true)
    {
        isDraggable = value;
    }

    public virtual OnlineMapsJSONItem ToJSON()
    {
        return OnlineMapsJSON.Serialize(new 
        {
            longitude,
            latitude,
            range = new
            {
                range.min,
                range.max
            },
            label,
            scale,
            enabled
        });
    }

    /// <summary>
    /// Update of marker instance.
    /// </summary>
    public virtual void Update()
    {
        
    }

    /// <summary>
    /// Method that called when need update marker.
    /// </summary>
    /// <param name="tlx">Top-left longitude.</param>
    /// <param name="tly">Top-left latutude.</param>
    /// <param name="brx">Bottom-right longitude.</param>
    /// <param name="bry">Bottom-right latitude.</param>
    /// <param name="zoom">Map zoom.</param>
    public virtual void Update(double tlx, double tly, double brx, double bry, int zoom)
    {

    }
}