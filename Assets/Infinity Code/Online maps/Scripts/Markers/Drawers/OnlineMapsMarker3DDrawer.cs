/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Implements the display of 3D markers
/// </summary>
public class OnlineMapsMarker3DDrawer : OnlineMapsMarkerDrawerBase
{
    private OnlineMapsControlBase3D control;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="control">Reference to 3D control</param>
    public OnlineMapsMarker3DDrawer(OnlineMapsControlBase3D control)
    {
        this.control = control;
        map = control.map;
        control.OnUpdate3DMarkers += Update3DMarkers;
    }

    public override void Dispose()
    {
        base.Dispose();
        control.OnUpdate3DMarkers -= Update3DMarkers;
        control = null;
    }

    private void Update3DMarkers()
    {
        OnlineMapsMarker3DManager manager = control.marker3DManager;
        if (manager == null || !manager.enabled) return;
        if (control.cl == null) return;

        int zoom = map.zoom;

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);

        double ttlx, ttly, tbrx, tbry;
        map.projection.CoordinatesToTile(tlx, tly, zoom, out ttlx, out ttly);
        map.projection.CoordinatesToTile(brx, bry, zoom, out tbrx, out tbry);

        long maxX = 1 << zoom;

        bool isEntireWorld = map.buffer.renderState.width == maxX * OnlineMapsUtils.tileSize;
        if (isEntireWorld && Math.Abs(tlx - brx) < 180)
        {
            if (tlx < 0)
            {
                brx += 360;
                tbrx += maxX;
            }
            else
            {
                tlx -= 360;
                ttlx -= maxX;
            }
        }

        Bounds bounds = control.meshFilter.sharedMesh.bounds;
        float bestYScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);

        for (int i = manager.Count - 1; i >= 0; i--)
        {
            OnlineMapsMarker3D marker = manager[i];
            if (marker.manager == null) marker.manager = manager;
            marker.Update(bounds, tlx, tly, brx, bry, zoom, ttlx, ttly, tbrx, tbry, bestYScale);
        }
    }
}