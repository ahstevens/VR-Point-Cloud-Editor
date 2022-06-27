/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

/// <summary>
/// The base class for implementation of tooltip drawers
/// </summary>
public abstract class OnlineMapsTooltipDrawerBase: OnlineMapsGenericBase<OnlineMapsTooltipDrawerBase>
{
    /// <summary>
    /// Tooltip
    /// </summary>
    public static string tooltip;

    /// <summary>
    /// The drawing element for which the tooltip is drawn
    /// </summary>
    public static OnlineMapsDrawingElement tooltipDrawingElement;

    /// <summary>
    /// The marker for which the tooltip is drawn
    /// </summary>
    public static OnlineMapsMarkerBase tooltipMarker;

    private static OnlineMapsMarkerBase rolledMarker;

    protected OnlineMaps map;
    protected OnlineMapsControlBase control;

    /// <summary>
    /// Checks if the marker in the specified screen coordinates, and shows him a tooltip.
    /// </summary>
    /// <param name="screenPosition">Screen coordinates</param>
    public void ShowMarkersTooltip(Vector2 screenPosition)
    {
        if (map.showMarkerTooltip != OnlineMapsShowMarkerTooltip.onPress)
        {
            tooltip = string.Empty;
            tooltipDrawingElement = null;
            tooltipMarker = null;
        }

        IOnlineMapsInteractiveElement el = control.GetInteractiveElement(screenPosition);
        OnlineMapsMarkerBase marker = el as OnlineMapsMarkerBase;

        if (map.showMarkerTooltip == OnlineMapsShowMarkerTooltip.onHover)
        {
            if (marker != null)
            {
                tooltip = marker.label;
                tooltipMarker = marker;
            }
            else
            {
                OnlineMapsDrawingElement drawingElement = map.GetDrawingElement(screenPosition);
                if (drawingElement != null)
                {
                    tooltip = drawingElement.tooltip;
                    tooltipDrawingElement = drawingElement;
                }
            }
        }

        if (rolledMarker != marker)
        {
            if (rolledMarker != null && rolledMarker.OnRollOut != null) rolledMarker.OnRollOut(rolledMarker);
            rolledMarker = marker;
            if (rolledMarker != null && rolledMarker.OnRollOver != null) rolledMarker.OnRollOver(rolledMarker);
        }
    }
}