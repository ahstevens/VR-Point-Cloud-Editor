/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implements drawing markers in the buffer
/// </summary>
public class OnlineMapsMarkerBufferDrawer : OnlineMapsMarker2DDrawer
{
    /// <summary>
    /// Allows you to change the order of drawing markers.
    /// </summary>
    public Func<IEnumerable<OnlineMapsMarker>, IEnumerable<OnlineMapsMarker>> OnSortMarker;

    private OnlineMapsControlBase control;

    /// <summary>
    /// Contructor
    /// </summary>
    /// <param name="control">Reference to control</param>
    public OnlineMapsMarkerBufferDrawer(OnlineMapsControlBase control)
    {
        this.control = control;
        map = control.map;
        control.OnDrawMarkers += Draw;
    }

    /// <summary>
    /// Dispose the current drawer
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();
        control.OnDrawMarkers -= Draw;
        control = null;
        OnSortMarker = null;
    }

    private void Draw()
    {
        if (control.markerManager == null) return;

        float s = OnlineMapsUtils.tileSize;

        OnlineMapsVector2i bufferPosition = map.buffer.bufferPosition;
        OnlineMapsVector2i frontBufferPosition = map.buffer.frontBufferPosition;
        int bufferZoom = map.buffer.renderState.zoom;

        double sx, sy, ex, ey;
        map.projection.TileToCoordinates(bufferPosition.x + frontBufferPosition.x / s, bufferPosition.y + frontBufferPosition.y / s, bufferZoom, out sx, out sy);
        map.projection.TileToCoordinates(bufferPosition.x + (frontBufferPosition.x + map.buffer.renderState.width) / s, bufferPosition.y + (frontBufferPosition.y + map.height) / s, bufferZoom, out ex, out ey);

        if (ex < sx) ex += 360;

        IEnumerable<OnlineMapsMarker> usedMarkers = control.markerManager.Where(m => m.enabled && m.range.InRange(bufferZoom));
        usedMarkers = OnSortMarker != null ? OnSortMarker(usedMarkers) : usedMarkers.OrderByDescending(m => m, new MarkerComparer());

        foreach (OnlineMapsMarker marker in usedMarkers) SetMarkerToBuffer(marker, bufferPosition, bufferZoom, frontBufferPosition, sx, sy, ex, ey);
    }

    private void SetMarkerToBuffer(OnlineMapsMarker marker, OnlineMapsVector2i bufferPosition, int bufferZoom, OnlineMapsVector2i frontBufferPosition, double sx, double sy, double ex, double ey)
    {
        const int s = OnlineMapsUtils.tileSize;

        double mx, my;
        marker.GetPosition(out mx, out my);

        long maxX = 1 << bufferZoom;

        bool isEntireWorld = map.buffer.renderState.width == maxX * s;
        bool isBiggestThatBuffer = map.buffer.renderState.width + 512 == maxX * s;

        if (isEntireWorld || isBiggestThatBuffer)
        {

        }
        else if (!(((mx > sx && mx < ex) || (mx + 360 > sx && mx + 360 < ex) || (mx - 360 > sx && mx - 360 < ex)) && 
            my < sy && my > ey)) return;

#if !UNITY_WEBGL
        int maxCount = 20;
        while (marker.locked && maxCount > 0)
        {
            OnlineMapsUtils.ThreadSleep(1);
            maxCount--;
        }
#endif

        marker.locked = true;

        double px, py;
        map.projection.CoordinatesToTile(mx, my, bufferZoom, out px, out py);
        px -= bufferPosition.x;
        py -= bufferPosition.y;

        if (isEntireWorld)
        {
            double tx, ty;
            map.projection.CoordinatesToTile(map.buffer.renderState.longitude, map.buffer.renderState.latitude, bufferZoom, out tx, out ty);
            tx -= map.buffer.renderState.width / s / 2;

            if (px < tx) px += maxX;
        }
        else
        {
            if (px < 0) px += maxX;
            else if (px > maxX) px -= maxX;
        }

        float zoomCoof = map.buffer.renderState.zoomCoof;

        px *= s;
        py *= s;

        int ipx = (int)((px - frontBufferPosition.x) / zoomCoof);
        int ipy = (int)((py - frontBufferPosition.y) / zoomCoof);

        OnlineMapsVector2i ip = marker.GetAlignedPosition(ipx, ipy);

        Color32[] markerColors = marker.colors;
        if (markerColors == null || markerColors.Length == 0) return;

        int markerWidth = marker.width;
        int markerHeight = marker.height;

        OnlineMapsBuffer buffer = map.buffer;

        for (int y = 0; y < marker.height; y++)
        {
            if (ip.y + y < 0 || ip.y + y >= map.height) continue;

            int cy = (markerHeight - y - 1) * markerWidth;

            for (int x = 0; x < marker.width; x++)
            {
                if (ip.x + x < 0 || ip.x + x >= map.buffer.renderState.width) continue;

                try
                {
                    buffer.SetColorToBuffer(markerColors[cy + x], ip, y, x);
                }
                catch
                {
                }
            }
        }

        if (isEntireWorld)
        {
            ip.x -= (int)(buffer.renderState.width / zoomCoof);
            for (int y = 0; y < marker.height; y++)
            {
                if (ip.y + y < 0 || ip.y + y >= map.height) continue;

                int cy = (markerHeight - y - 1) * markerWidth;

                for (int x = 0; x < marker.width; x++)
                {
                    if (ip.x + x < 0 || ip.x + x >= map.buffer.renderState.width) continue;

                    try
                    {
                        buffer.SetColorToBuffer(markerColors[cy + x], ip, y, x);
                    }
                    catch
                    {
                    }
                }
            }

            ip.x += (int)(buffer.renderState.width * 2 / zoomCoof);
            for (int y = 0; y < marker.height; y++)
            {
                if (ip.y + y < 0 || ip.y + y >= map.height) continue;

                int cy = (markerHeight - y - 1) * markerWidth;

                for (int x = 0; x < marker.width; x++)
                {
                    if (ip.x + x < 0 || ip.x + x >= map.buffer.renderState.width) continue;

                    try
                    {
                        buffer.SetColorToBuffer(markerColors[cy + x], ip, y, x);
                    }
                    catch
                    {
                    }
                }
            }
        }

        marker.locked = false;
    }

    internal class MarkerComparer : IComparer<OnlineMapsMarkerBase>
    {
        public int Compare(OnlineMapsMarkerBase m1, OnlineMapsMarkerBase m2)
        {
            double m1x, m1y, m2x, m2y;
            m1.GetPosition(out m1x, out m1y);
            m2.GetPosition(out m2x, out m2y);

            if (m1y > m2y) return 1;
            if (Math.Abs(m1y - m2y) < double.Epsilon)
            {
                if (m1x < m2x) return 1;
                return 0;
            }
            return -1;
        }
    }
}