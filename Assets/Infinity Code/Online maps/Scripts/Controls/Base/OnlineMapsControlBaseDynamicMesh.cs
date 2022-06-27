/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// The base class that implements the display of the map on the dynamic mesh.
/// </summary>
[OnlineMapsWizardControlHelper(OnlineMapsTarget.mesh)]
public abstract class OnlineMapsControlBaseDynamicMesh : OnlineMapsControlBase3D
{
    #region Variables

    /// <summary>
    /// Event that occurs after the map mesh has been updated.
    /// </summary>
    public Action OnMeshUpdated;

    public Action OnUpdateMeshAfter;
    public Action OnUpdateMeshBefore;

    /// <summary>
    /// Type of checking 2D markers on visibility.
    /// </summary>
    public OnlineMapsTilesetCheckMarker2DVisibility checkMarker2DVisibility = OnlineMapsTilesetCheckMarker2DVisibility.pivot;

    /// <summary>
    /// Resolution of the elevation map.
    /// </summary>
    public int elevationResolution = 32;

    /// <summary>
    /// Material that will be used for marker.
    /// </summary>
    public Material markerMaterial;

    /// <summary>
    /// Shader of markers.
    /// </summary>
    public Shader markerShader;

    /// <summary>
    /// Size of the map in the scene
    /// </summary>
    public Vector2 sizeInScene = new Vector2(1024, 1024);

    /// <summary>
    /// Does the collider contain elevations?
    /// </summary>
    protected bool colliderWithElevation;

    private OnlineMapsVector2i _bufferPosition;
    private Vector2 lastSizeInScene;

    #endregion

    #region Properties

    /// <summary>
    /// Position of the buffer
    /// </summary>
    public OnlineMapsVector2i bufferPosition
    {
        get
        {
            if (map.buffer.bufferPosition != null) return map.buffer.bufferPosition;

            if (_bufferPosition == null)
            {
                const int s = OnlineMapsUtils.tileSize;
                int countX = map.buffer.renderState.width / s + 2;
                int countY = map.buffer.renderState.height / s + 2;

                double px = map.buffer.renderState.longitude;
                double py = map.buffer.renderState.latitude;
                map.projection.CoordinatesToTile(px, py, map.buffer.renderState.zoom, out px, out py);

                _bufferPosition = new OnlineMapsVector2i((int)px, (int)py);
                _bufferPosition.x -= countX / 2;
                _bufferPosition.y -= countY / 2;

                int maxY = 1 << map.buffer.renderState.zoom;

                if (_bufferPosition.y < 0) _bufferPosition.y = 0;
                if (_bufferPosition.y >= maxY - countY - 1) _bufferPosition.y = maxY - countY - 1;
            }
            return _bufferPosition;
        }
        set { _bufferPosition = value; }
    }

    /// <summary>
    /// The center point of the map (without elevations) in local space.
    /// </summary>
    public Vector3 center
    {
        get
        {
            return new Vector3(sizeInScene.x / -2, 0, sizeInScene.y / 2);
        }
    }

    /// <summary>
    /// Returns true when the elevation manager is available and enabled.
    /// </summary>
    public bool hasElevation
    {
        get { return elevationManager != null && elevationManager.enabled; }
    }

    public new static OnlineMapsControlBaseDynamicMesh instance
    {
        get { return _instance as OnlineMapsControlBaseDynamicMesh; }
    }

    public override OnlineMapsTarget resultType
    {
        get { return OnlineMapsTarget.mesh; }
    }

    #endregion

    #region Methods

    public override void GetPosition(double lng, double lat, out double px, out double py)
    {
        const short tileSize = OnlineMapsUtils.tileSize;

        double dx, dy, dtx, dty;
        map.projection.CoordinatesToTile(lng, lat, map.zoom, out dx, out dy);

        double tlx, tly;
        map.GetTopLeftPosition(out tlx, out tly);

        map.projection.CoordinatesToTile(tlx, tly, map.zoom, out dtx, out dty);
        dx -= dtx;
        dy -= dty;
        int maxX = 1 << (map.zoom - 1);
        if (dx < -maxX) dx += maxX << 1;
        if (dx < 0 && map.width == (1L << map.zoom) * tileSize) dx += map.width / tileSize;
        px = dx * tileSize / map.zoomCoof;
        py = dy * tileSize / map.zoomCoof;
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        double mx, my;
        GetPosition(lng, lat, out mx, out my);
        mx /= map.width;
        my /= map.height;
        Rect mapRect = GetRect();
        mx = mapRect.x + mapRect.width * mx;
        my = mapRect.y + mapRect.height - mapRect.height * my;
        return new Vector2((float)mx, (float)my);
    }

    /// <summary>
    /// Converts geographical coordinates to position in world space.
    /// </summary>
    /// <param name="coords">Geographical coordinates.</param>
    /// <returns>Position in world space.</returns>
    public Vector3 GetWorldPosition(Vector2 coords)
    {
        return GetWorldPosition(coords.x, coords.y);
    }

    /// <summary>
    /// Converts geographical coordinates to position in world space.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <returns></returns>
    public Vector3 GetWorldPosition(double lng, double lat)
    {
        double mx, my;
        GetPosition(lng, lat, out mx, out my);

        double px = -mx / map.width * sizeInScene.x;
        double pz = my / map.height * sizeInScene.y;

        Vector3 offset = transform.rotation * new Vector3((float)px, 0, (float)pz);
        offset.Scale(map.transform.lossyScale);

        return map.transform.position + offset;
    }

    /// <summary>
    /// Converts geographical coordinates to position in world space with elevation.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Laatitude</param>
    /// <returns>Position in world space.</returns>
    public Vector3 GetWorldPositionWithElevation(double lng, double lat)
    {
        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        return GetWorldPositionWithElevation(lng, lat, tlx, tly, brx, bry);
    }

    /// <summary>
    /// Converts geographical coordinates to position in world space with elevation.
    /// </summary>
    /// <param name="coords">Geographical coordinates.</param>
    /// <param name="topLeftPosition">Coordinates of top-left corner of map.</param>
    /// <param name="bottomRightPosition">Coordinates of bottom-right corner of map.</param>
    /// <returns>Position in world space.</returns>
    public Vector3 GetWorldPositionWithElevation(Vector2 coords, Vector2 topLeftPosition, Vector2 bottomRightPosition)
    {
        return GetWorldPositionWithElevation(coords.x, coords.y, topLeftPosition.x, topLeftPosition.y, bottomRightPosition.x, bottomRightPosition.y);
    }

    /// <summary>
    /// Converts geographical coordinates to position in world space with elevation.
    /// </summary>
    /// <param name="coords">Geographical coordinates.</param>
    /// <param name="tlx">Top-left longitude.</param>
    /// <param name="tly">Top-left latitude.</param>
    /// <param name="brx">Bottom-right longitude.</param>
    /// <param name="bry">Bottom-right latitude.</param>
    /// <returns>Position in world space.</returns>
    public Vector3 GetWorldPositionWithElevation(Vector2 coords, double tlx, double tly, double brx, double bry)
    {
        return GetWorldPositionWithElevation(coords.x, coords.y, tlx, tly, brx, bry);
    }

    /// <summary>
    /// Converts geographical coordinates to position in world space with elevation.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Laatitude</param>
    /// <param name="tlx">Top-left longitude.</param>
    /// <param name="tly">Top-left latitude.</param>
    /// <param name="brx">Bottom-right longitude.</param>
    /// <param name="bry">Bottom-right latitude.</param>
    /// <returns>Position in world space.</returns>
    public Vector3 GetWorldPositionWithElevation(double lng, double lat, double tlx, double tly, double brx, double bry)
    {
        double mx, my;
        GetPosition(lng, lat, out mx, out my);

        mx = -mx / map.width * sizeInScene.x;
        my = my / map.height * sizeInScene.y;

        float y = hasElevation ? elevationManager.GetElevationValue(mx, my, OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry), tlx, tly, brx, bry) : 0;

        Vector3 offset = transform.rotation * new Vector3((float)mx, y, (float)my);
        offset.Scale(map.transform.lossyScale);

        return map.transform.position + offset;
    }

    protected virtual void InitMapMesh()
    {
        
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        lastSizeInScene = sizeInScene;

        if (marker2DMode == OnlineMapsMarker2DMode.flat) markerDrawer = new OnlineMapsMarkerFlatDrawer(this);
        else markerDrawer = new OnlineMapsMarkerBillboardDrawer(this);
    }

    protected virtual void ReinitMapMesh()
    {

    }

    public override void UpdateControl()
    {
        base.UpdateControl();

        if (sizeInScene != lastSizeInScene)
        {
            ReinitMapMesh();
            lastSizeInScene = sizeInScene;
        }

        _bufferPosition = null;
    }

    #endregion

    #region Internal Types

    public enum ElevationBottomMode
    {
        zero,
        minValue
    }

    /// <summary>
    /// Where to create a mesh?
    /// </summary>
    public enum CreateMapTarget
    {
        /// <summary>
        /// On the current GameObject
        /// </summary>
        currentGameobject,

        /// <summary>
        /// On the new GameObject
        /// </summary>
        newGameobject
    }

    #endregion
}