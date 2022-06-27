/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Class to limit the position and zoom of the map.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Limits")]
[Serializable]
[OnlineMapsPlugin("Limits", typeof(OnlineMapsControlBase))]
public class OnlineMapsLimits : MonoBehaviour, IOnlineMapsSavableComponent
{
    /// <summary>
    /// The minimum zoom value.
    /// </summary>
    public float minZoom = OnlineMaps.MINZOOM;

    /// <summary>
    /// The maximum zoom value. 
    /// </summary>
    public float maxZoom = OnlineMaps.MAXZOOM_EXT;

    /// <summary>
    /// The minimum latitude value.
    /// </summary>
    public float minLatitude = -90;

    /// <summary>
    /// The maximum latitude value. 
    /// </summary>
    public float maxLatitude = 90;

    /// <summary>
    /// The minimum longitude value.
    /// </summary>
    public float minLongitude = -180;

    /// <summary>
    /// The maximum longitude value. 
    /// </summary>
    public float maxLongitude = 180;

    /// <summary>
    /// Type of limitation position map.
    /// </summary>
    public OnlineMapsPositionRangeType positionRangeType = OnlineMapsPositionRangeType.center;

    /// <summary>
    /// Flag indicating that need to limit the zoom.
    /// </summary>
    public bool useZoomRange;

    /// <summary>
    /// Flag indicating that need to limit the position.
    /// </summary>
    public bool usePositionRange;

    private OnlineMapsSavableItem[] savableItems;
    private OnlineMaps map;

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("limits", "Limits", SaveSettings)
            {
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    public void LoadSettings(OnlineMapsJSONObject json)
    {
        json.DeserializeObject(this);
    }

    private void OnEnable()
    {
        map = GetComponent<OnlineMaps>();
    }

    private OnlineMapsJSONItem SaveSettings()
    {
        return OnlineMapsJSON.Serialize(this);
    }

    private void Start()
    {
        if (useZoomRange) map.zoomRange = new OnlineMapsRange(minZoom, maxZoom);
        if (usePositionRange) map.positionRange = new OnlineMapsPositionRange(minLatitude, minLongitude, maxLatitude, maxLongitude, positionRangeType);
    }
}
