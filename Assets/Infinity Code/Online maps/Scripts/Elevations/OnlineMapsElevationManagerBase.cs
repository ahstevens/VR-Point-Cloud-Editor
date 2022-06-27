/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Base class for components that implement elevations
/// </summary>
[DisallowMultipleComponent]
public abstract class OnlineMapsElevationManagerBase : MonoBehaviour
{
    #region Variables

    protected static OnlineMapsElevationManagerBase _instance;

    /// <summary>
    /// Called when downloading elevation data failed
    /// </summary>
    public Action<string> OnElevationFails;

    /// <summary>
    /// Called when downloading of elevation data began
    /// </summary>
    public Action OnElevationRequested;

    /// <summary>
    /// Called when elevation data has been updated
    /// </summary>
    public Action OnElevationUpdated;

    /// <summary>
    /// Called when downloading of elevation data for an area begins
    /// </summary>
    public Action<double, double, double, double> OnGetElevation;

    public Action<Vector2, Vector2> OnGetElevationLegacy;

    /// <summary>
    /// The rule for calculating the lowest point of the map mesh
    /// </summary>
    public OnlineMapsElevationBottomMode bottomMode = OnlineMapsElevationBottomMode.zero;

    /// <summary>
    /// Scale of elevation values
    /// </summary>
    public float scale = 1;

    /// <summary>
    /// Range when elevations will be shown
    /// </summary>
    public OnlineMapsRange zoomRange = new OnlineMapsRange(11, OnlineMaps.MAXZOOM_EXT);

    /// <summary>
    /// The minimum elevation value.
    /// </summary>
    [NonSerialized]
    public short minValue;

    /// <summary>
    /// The maximum elevation value.
    /// </summary>
    [NonSerialized]
    public short maxValue;

    /// <summary>
    /// Lock yScale value
    /// </summary>
    public bool lockYScale = false;

    /// <summary>
    /// Fixed yScale value
    /// </summary>
    public float yScaleValue = 1;

    protected OnlineMapsVector2i elevationBufferPosition;

    private OnlineMaps _map;
    private OnlineMapsControlBaseDynamicMesh _control;
    protected Vector2 _sizeInScene;

    #endregion

    #region Properties

    #region Static

    protected OnlineMapsVector2i bufferPosition
    {
        get { return control.bufferPosition; }
    }

    protected OnlineMapsControlBaseDynamicMesh control
    {
        get
        {
            if (_control == null) _control = map.control as OnlineMapsControlBaseDynamicMesh;
            return _control;
        }
    }

    /// <summary>
    /// Instance of elevation manager
    /// </summary>
    public static OnlineMapsElevationManagerBase instance
    {
        get
        {
            return _instance;
        }
    }

    protected OnlineMaps map
    {
        get
        {
            if (_map == null) _map = GetComponent<OnlineMaps>();
            return _map;
        }
    }

    protected Vector2 sizeInScene
    {
        get { return _sizeInScene; }
    }

    /// <summary>
    /// Elevation manager is active?
    /// </summary>
    public static bool isActive
    {
        get { return _instance != null && _instance.enabled; }
    }

    /// <summary>
    /// Are elevations used for map?
    /// </summary>
    public static bool useElevation
    {
        get { return isActive && _instance.zoomRange.InRange(_instance.map.zoom) && _instance.hasData; }
    }

    #endregion

    /// <summary>
    /// Elevation manager has elevation data
    /// </summary>
    public abstract bool hasData { get; }

    #endregion

    #region Methods

    /// <summary>
    /// Cancel current elevation request
    /// </summary>
    public abstract void CancelCurrentElevationRequest();

    /// <summary>
    /// Returns yScale for an area
    /// </summary>
    /// <returns>yScale for an area</returns>
    public static float GetBestElevationYScale()
    {
        double tlx, tly, brx, bry;
        OnlineMaps map = _instance != null ? _instance.map : OnlineMaps.instance;
        if (map == null) return 0;

        map.GetCorners(out tlx, out tly, out brx, out bry);
        return GetBestElevationYScale(tlx, tly, brx, bry);
    }

    /// <summary>
    /// Returns yScale for an area
    /// </summary>
    /// <param name="tlx">Left longitude</param>
    /// <param name="tly">Top latitude</param>
    /// <param name="brx">Right longitude</param>
    /// <param name="bry">Bottom latitude</param>
    /// <returns>yScale for an area</returns>
    public static float GetBestElevationYScale(double tlx, double tly, double brx, double bry)
    {
        if (_instance != null && _instance.lockYScale) return _instance.yScaleValue;

        double dx, dy;

        OnlineMaps map = _instance != null ? _instance.map : OnlineMaps.instance;
        if (map == null) return 0;

        OnlineMapsControlBaseDynamicMesh control = map.control as OnlineMapsControlBaseDynamicMesh;
        if (control == null) return 0;

        OnlineMapsUtils.DistanceBetweenPoints(tlx, tly, brx, bry, out dx, out dy);
        dx = dx / control.sizeInScene.x * map.width;
        dy = dy / control.sizeInScene.y * map.height;
        return (float)Math.Min(map.width / dx, map.height / dy) / 1000;
    }

    public abstract float GetElevationValue(double x, double z, float yScale, double tlx, double tly, double brx, double bry);

    /// <summary>
    /// Returns the scaled elevation value for a point in the scene relative to left-top corner of the map.
    /// </summary>
    /// <param name="x">Point X</param>
    /// <param name="z">Point Y</param>
    /// <param name="yScale">Scale factor</param>
    /// <returns>Elevation value</returns>
    public static float GetElevation(double x, double z, float? yScale = null)
    {
        if (_instance == null || !_instance.enabled) return 0;

        double tlx, tly, brx, bry;
        _instance.map.GetCorners(out tlx, out tly, out brx, out bry);
        if (!yScale.HasValue) yScale = GetBestElevationYScale(tlx, tly, brx, bry);
        return GetElevation(x, z, yScale.Value, tlx, tly, brx, bry);
    }

    /// <summary>
    /// Returns the scaled elevation value for a point in the scene relative to left-top corner of the map.
    /// </summary>
    /// <param name="x">Point X</param>
    /// <param name="z">Point Y</param>
    /// <param name="yScale">Scale factor</param>
    /// <param name="tlx">Left longitude</param>
    /// <param name="tly">Top latitude</param>
    /// <param name="brx">Right longitude</param>
    /// <param name="bry">Bottom latitude</param>
    /// <returns>Elevation value</returns>
    public static float GetElevation(double x, double z, float yScale, double tlx, double tly, double brx, double bry)
    {
        if (_instance == null || !_instance.enabled) return 0;
        return _instance.GetElevationValue(x, z, yScale, tlx, tly, brx, bry);
    }

    /// <summary>
    /// Returns the maximum known elevation value
    /// </summary>
    /// <param name="yScale">Scale factor</param>
    /// <returns>Maximum known elevation value</returns>
    public float GetMaxElevation(float yScale)
    {
        return hasData ? maxValue * yScale * scale: 0;
    }

    /// <summary>
    /// Returns the minimum known elevation value
    /// </summary>
    /// <param name="yScale">Scale factor</param>
    /// <returns>Minimum known elevation value</returns>
    public float GetMinElevation(float yScale)
    {
        return hasData ? minValue * yScale * scale: 0;
    }

    /// <summary>
    /// Returns the unscaled elevation value for a point in the scene relative to left-top corner of the map.
    /// </summary>
    /// <param name="x">Point X</param>
    /// <param name="z">Point Z</param>
    /// <returns>Elevation value</returns>
    public static float GetUnscaledElevation(double x, double z)
    {
        if (_instance == null || !_instance.enabled) return 0;

        double tlx, tly, brx, bry;
        _instance.map.GetCorners(out tlx, out tly, out brx, out bry);
        return GetUnscaledElevation(x, z, tlx, tly, brx, bry);
    }

    /// <summary>
    /// Returns the unscaled elevation value for a point in the scene relative to left-top corner of the map.
    /// </summary>
    /// <param name="x">Point X</param>
    /// <param name="z">Point Z</param>
    /// <param name="tlx">Left longitude</param>
    /// <param name="tly">Top latitude</param>
    /// <param name="brx">Right longitude</param>
    /// <param name="bry">Bottom latitude</param>
    /// <returns>Elevation value</returns>
    public static float GetUnscaledElevation(double x, double z, double tlx, double tly, double brx, double bry)
    {
        if (_instance != null && _instance.enabled) return _instance.GetUnscaledElevationValue(x, z, tlx, tly, brx, bry);
        return 0;
    }

    public abstract float GetUnscaledElevationValue(double x, double z, double tlx, double tly, double brx, double bry);

    /// <summary>
    /// Returns the unscaled elevation value for a coordinate.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <returns>Elevation value</returns>
    public static float GetUnscaledElevationByCoordinate(double lng, double lat)
    {
        if (_instance == null || !_instance.enabled) return 0;

        double tlx, tly, brx, bry, mx, my;
        _instance.map.GetCorners(out tlx, out tly, out brx, out bry);

        _instance.control.GetPosition(lng, lat, out mx, out my);
        mx = -mx / _instance.map.width * _instance.control.sizeInScene.x;
        my = my / _instance.map.height * _instance.control.sizeInScene.y;

        return _instance.GetUnscaledElevationValue(mx, my, tlx, tly, brx, bry);
    }

    /// <summary>
    /// Downloads new elevation data for area
    /// </summary>
    public abstract void RequestNewElevationData();

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

    protected virtual void UpdateMinMax()
    {
        
    }

    #endregion
}