/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Draws tooltips using legacy GUI.
/// </summary>
public class OnlineMapsGUITooltipDrawer: OnlineMapsTooltipDrawerBase
{
    public static Action<GUIStyle, string, Vector2> OnDrawTooltip;

    /// <summary>
    /// Allows you to customize the appearance of the tooltip.
    /// </summary>
    /// <param name="style">The reference to the style.</param>
    public delegate void OnPrepareTooltipStyleDelegate(ref GUIStyle style);

    /// <summary>
    /// Event caused when preparing tooltip style.
    /// </summary>
    public static OnPrepareTooltipStyleDelegate OnPrepareTooltipStyle;

    private GUIStyle tooltipStyle;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="map">Reference to the map</param>
    public OnlineMapsGUITooltipDrawer(OnlineMaps map)
    {
        this.map = map;
        control = map.control;

        map.OnGUIAfter += DrawTooltips;

        tooltipStyle = new GUIStyle
        {
            normal =
            {
                background = map.tooltipBackgroundTexture,
                textColor = new Color32(230, 230, 230, 255)
            },
            border = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(4, 4, 4, 4),
            wordWrap = true,
            richText = true,
            alignment = TextAnchor.MiddleCenter,
            stretchWidth = true,
            padding = new RectOffset(0, 0, 3, 3)
        };
    }

    ~OnlineMapsGUITooltipDrawer()
    {
        map.OnGUIAfter -= DrawTooltips;
        OnPrepareTooltipStyle = null;
        tooltipStyle = null;
        map = null;
        control = null;
    }

    private void DrawTooltips()
    {
        if (string.IsNullOrEmpty(tooltip) && map.showMarkerTooltip != OnlineMapsShowMarkerTooltip.always) return;

        GUIStyle style = new GUIStyle(tooltipStyle);

        if (OnPrepareTooltipStyle != null) OnPrepareTooltipStyle(ref style);

        if (!string.IsNullOrEmpty(tooltip)) InvokeInteractiveElementEvents(style);

        if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.always)
        {
            if (control is OnlineMapsTileSetControl) DrawTooltipForTileset(style);
            else DrawTooltipForOtherControls(style);
        }
    }

    private void DrawTooltipForOtherControls(GUIStyle style)
    {
        foreach (OnlineMapsMarker marker in control.markerManager)
        {
            if (string.IsNullOrEmpty(marker.label)) continue;

            Rect rect = marker.screenRect;

            if (rect.xMax > 0 && rect.xMin < Screen.width && rect.yMax > 0 && rect.yMin < Screen.height)
            {
                if (marker.OnDrawTooltip != null) marker.OnDrawTooltip(marker);
                else if (OnlineMapsMarkerBase.OnMarkerDrawTooltip != null) OnlineMapsMarkerBase.OnMarkerDrawTooltip(marker);
                else OnGUITooltip(style, marker.label, new Vector2(rect.x + rect.width / 2, rect.y + rect.height));
            }
        }

        OnlineMapsControlBase3D control3D = control as OnlineMapsControlBase3D;
        if (control3D != null)
        {
            double tlx, tly, brx, bry;
            map.GetCorners(out tlx, out tly, out brx, out bry);
            if (brx < tlx) brx += 360;

            Camera cam = control3D.currentCamera;
            Vector3 position = map.transform.position;
            Quaternion rotation = map.transform.rotation;
            Vector3 localScale = map.transform.localScale;
            Vector3 lossyScale = map.transform.lossyScale;
            Vector3 boundsSize = control3D.cl.bounds.size;

            foreach (OnlineMapsMarker3D marker in control3D.marker3DManager)
            {
                if (string.IsNullOrEmpty(marker.label)) continue;

                double mx, my;
                marker.GetPosition(out mx, out my);

                if (!(((mx > tlx && mx < brx) || (mx + 360 > tlx && mx + 360 < brx) ||
                       (mx - 360 > tlx && mx - 360 < brx)) &&
                      my < tly && my > bry)) continue;

                if (marker.OnDrawTooltip != null) marker.OnDrawTooltip(marker);
                else if (OnlineMapsMarkerBase.OnMarkerDrawTooltip != null) OnlineMapsMarkerBase.OnMarkerDrawTooltip(marker);
                else
                {
                    double mx1, my1;
                    control3D.GetPosition(mx, my, out mx1, out my1);

                    double px = (-mx1 / map.width + 0.5) * boundsSize.x;
                    double pz = (my1 / map.height - 0.5) * boundsSize.z;

                    Vector3 offset = rotation * new Vector3((float) px, 0, (float) pz);
                    offset.Scale(lossyScale);

                    Vector3 p1 = position + offset;
                    Vector3 p2 = p1 + new Vector3(0, 0, boundsSize.z / map.height * marker.scale);

                    Vector2 screenPoint1 = cam.WorldToScreenPoint(p1);
                    Vector2 screenPoint2 = cam.WorldToScreenPoint(p2);

                    float yOffset = (screenPoint1.y - screenPoint2.y) * localScale.x - 10;

                    OnGUITooltip(style, marker.label, screenPoint1 + new Vector2(0, yOffset));
                }
            }
        }
    }

    private void DrawTooltipForTileset(GUIStyle style)
    {
        OnlineMapsTileSetControl tsControl = control as OnlineMapsTileSetControl;

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        if (brx < tlx) brx += 360;

        Vector2 sizeInScene = tsControl.sizeInScene;

        float widthScale = sizeInScene.x / map.width / 2;
        float heightScale = sizeInScene.y / map.height / 2;

        Camera cam = tsControl.currentCamera;
        Vector3 localScale = map.transform.localScale;

        foreach (OnlineMapsMarker marker in control.markerManager)
        {
            if (string.IsNullOrEmpty(marker.label)) continue;

            double mx, my;
            marker.GetPosition(out mx, out my);

            if (!(((mx > tlx && mx < brx) || (mx + 360 > tlx && mx + 360 < brx) || (mx - 360 > tlx && mx - 360 < brx)) && my < tly && my > bry)) continue;

            if (marker.OnDrawTooltip != null) marker.OnDrawTooltip(marker);
            else if (OnlineMapsMarkerBase.OnMarkerDrawTooltip != null) OnlineMapsMarkerBase.OnMarkerDrawTooltip(marker);
            else
            {
                Vector3 pivotPoint = tsControl.GetWorldPositionWithElevation(mx, my, tlx, tly, brx, bry);
                Vector3 centerPoint = pivotPoint;

                float xOffset = widthScale * marker.width * marker.scale;
                float zOffset = heightScale * marker.height * marker.scale;

                if (marker.align == OnlineMapsAlign.BottomLeft ||
                    marker.align == OnlineMapsAlign.Left ||
                    marker.align == OnlineMapsAlign.TopLeft)
                {
                    centerPoint.x += xOffset;
                }
                else if (marker.align == OnlineMapsAlign.BottomRight ||
                         marker.align == OnlineMapsAlign.Right ||
                         marker.align == OnlineMapsAlign.TopRight)
                {
                    centerPoint.x -= xOffset;
                }

                if (marker.align == OnlineMapsAlign.Top ||
                    marker.align == OnlineMapsAlign.TopLeft ||
                    marker.align == OnlineMapsAlign.TopRight)
                {
                    centerPoint.z += zOffset;
                }
                else if (marker.align == OnlineMapsAlign.BottomLeft ||
                         marker.align == OnlineMapsAlign.Bottom ||
                         marker.align == OnlineMapsAlign.BottomRight)
                {
                    centerPoint.z -= zOffset;
                }

                bool useRotation = marker.align != OnlineMapsAlign.Center && Math.Abs(marker.rotation) > float.Epsilon;
                if (useRotation) centerPoint = Quaternion.Euler(0, marker.rotation * 360, 0) * (centerPoint - pivotPoint) + pivotPoint;

                Vector3 topPoint = centerPoint + new Vector3(0, 0, zOffset);

                if (useRotation) topPoint = Quaternion.Euler(0, marker.rotation * -360, 0) * (centerPoint - topPoint) + centerPoint;

                Vector2 screenPoint1 = cam.WorldToScreenPoint(centerPoint);
                Vector2 screenPoint2 = cam.WorldToScreenPoint(topPoint);

                float yOffset = (screenPoint1 - screenPoint2).magnitude * localScale.x - 10;

                OnGUITooltip(style, marker.label, screenPoint1 + new Vector2(0, yOffset));
            }
        }

        foreach (OnlineMapsMarker3D marker in tsControl.marker3DManager)
        {
            if (string.IsNullOrEmpty(marker.label)) continue;

            double mx, my;
            marker.GetPosition(out mx, out my);

            if (!(((mx > tlx && mx < brx) || (mx + 360 > tlx && mx + 360 < brx) ||
                   (mx - 360 > tlx && mx - 360 < brx)) &&
                  my < tly && my > bry)) continue;

            if (marker.OnDrawTooltip != null) marker.OnDrawTooltip(marker);
            else if (OnlineMapsMarkerBase.OnMarkerDrawTooltip != null) OnlineMapsMarkerBase.OnMarkerDrawTooltip(marker);
            else
            {
                Vector3 p1 = tsControl.GetWorldPositionWithElevation(mx, my, tlx, tly, brx, bry);
                Vector3 p2 = p1 + new Vector3(0, 0, sizeInScene.y / map.height * marker.scale);

                Vector2 screenPoint1 = cam.WorldToScreenPoint(p1);
                Vector2 screenPoint2 = cam.WorldToScreenPoint(p2);

                float yOffset = (screenPoint1.y - screenPoint2.y) * localScale.x - 10;

                OnGUITooltip(style, marker.label, screenPoint1 + new Vector2(0, yOffset));
            }
        }
    }

    private void InvokeInteractiveElementEvents(GUIStyle style)
    {
        Vector2 inputPosition = control.GetInputPosition();

        if (tooltipMarker != null)
        {
            if (tooltipMarker.OnDrawTooltip != null) tooltipMarker.OnDrawTooltip(tooltipMarker);
            else if (OnlineMapsMarkerBase.OnMarkerDrawTooltip != null) OnlineMapsMarkerBase.OnMarkerDrawTooltip(tooltipMarker);
            else OnGUITooltip(style, tooltip, inputPosition);
        }
        else if (tooltipDrawingElement != null)
        {
            if (tooltipDrawingElement.OnDrawTooltip != null) tooltipDrawingElement.OnDrawTooltip(tooltipDrawingElement);
            else if (OnlineMapsDrawingElement.OnElementDrawTooltip != null) OnlineMapsDrawingElement.OnElementDrawTooltip(tooltipDrawingElement);
            else OnGUITooltip(style, tooltip, inputPosition);
        }
    }

    private void OnGUITooltip(GUIStyle style, string text, Vector2 position)
    {
        if (OnDrawTooltip != null)
        {
            OnDrawTooltip(style, text, position);
            return;
        }

        GUIContent tip = new GUIContent(text);
        Vector2 size = style.CalcSize(tip);
        GUI.Label(new Rect(position.x - size.x / 2 - 5, Screen.height - position.y - size.y - 20, size.x + 10, size.y + 5), text, style);
    }
}