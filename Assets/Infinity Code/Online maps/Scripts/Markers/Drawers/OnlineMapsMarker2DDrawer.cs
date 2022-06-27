/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Implements the display of 2D markers
/// </summary>
public abstract class OnlineMapsMarker2DDrawer : OnlineMapsMarkerDrawerBase
{
    /// <summary>
    /// Gets 2D marker in screen position
    /// </summary>
    /// <param name="screenPosition">Screen position</param>
    /// <returns>2D marker</returns>
    public virtual OnlineMapsMarker GetMarkerFromScreen(Vector2 screenPosition)
    {
        Vector2 coords = map.control.GetCoords(screenPosition);
        if (coords == Vector2.zero) return null;

        OnlineMapsMarker marker = null;
        double lng = double.MinValue, lat = double.MaxValue;
        double mx, my;
        int zoom = map.zoom;

        foreach (OnlineMapsMarker m in map.control.markerManager)
        {
            if (!m.enabled || !m.range.InRange(zoom)) continue;
            if (m.HitTest(coords, zoom))
            {
                m.GetPosition(out mx, out my);
                if (my < lat || (Math.Abs(my - lat) < double.Epsilon && mx > lng))
                {
                    marker = m;
                    lat = my;
                    lng = mx;
                }
            }
        }

        return marker;
    }
}