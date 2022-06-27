/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Base class for marker manager components
/// </summary>
[Serializable]
[DisallowMultipleComponent]
[AddComponentMenu("")]
public class OnlineMapsMarkerManager : OnlineMapsMarkerManagerBase<OnlineMapsMarkerManager, OnlineMapsMarker>
{
    /// <summary>
    /// Texture to be used if marker texture is not specified.
    /// </summary>
    public Texture2D defaultTexture;

    /// <summary>
    /// Align for new markers
    /// </summary>
    public OnlineMapsAlign defaultAlign = OnlineMapsAlign.Bottom;

    /// <summary>
    /// Specifies whether to create a 2D marker by pressing M under the cursor.
    /// </summary>
    public bool allowAddMarkerByM = true;

    /// <summary>
    /// Create a new marker
    /// </summary>
    /// <param name="location">Location of the marker (X - longitude, Y - latitude)</param>
    /// <param name="label">Tooltip</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker CreateItem(Vector2 location, string label)
    {
        if (instance != null) return instance.Create(location.x, location.y, null, label);
        return null;
    }

    /// <summary>
    /// Create a new marker
    /// </summary>
    /// <param name="location">Location of the marker (X - longitude, Y - latitude)</param>
    /// <param name="texture">Texture of the marker</param>
    /// <param name="label">Tooltip</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker CreateItem(Vector2 location, Texture2D texture = null, string label = "")
    {
        if (instance != null) return instance.Create(location.x, location.y, texture, label);
        return null;
    }

    /// <summary>
    /// Create a new marker
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="label">Tooltip</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker CreateItem(double longitude, double latitude, string label)
    {
        if (instance != null) return instance.Create(longitude, latitude, null, label);
        return null;
    }

    /// <summary>
    /// Create a new marker
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="texture">Texture of the marker</param>
    /// <param name="label">Tooltip</param>
    /// <returns>Instance of the marker</returns>
    public static OnlineMapsMarker CreateItem(double longitude, double latitude, Texture2D texture = null, string label = "")
    {
        if (instance != null) return instance.Create(longitude, latitude, texture, label);
        return null;
    }

    /// <summary>
    /// Create a new marker
    /// </summary>
    /// <param name="location">Location of the marker (X - longitude, Y - latitude)</param>
    /// <param name="texture">Texture of the marker</param>
    /// <param name="label">Tooltip</param>
    /// <returns>Instance of the marker</returns
    public OnlineMapsMarker Create(Vector2 location, Texture2D texture = null, string label = "")
    {
        if (instance != null) return Create(location.x, location.y, texture, label);
        return null;
    }

    /// <summary>
    /// Create a new marker
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    /// <param name="texture">Texture of the marker</param>
    /// <param name="label">Tooltip</param>
    /// <returns>Instance of the marker</returns>
    public OnlineMapsMarker Create(double longitude, double latitude, Texture2D texture = null, string label = "")
    {
        if (texture == null) texture = defaultTexture;
        OnlineMapsMarker marker = _CreateItem(longitude, latitude);
        marker.manager = this;
        marker.texture = texture;
        marker.label = label;
        marker.align = defaultAlign;
        marker.scale = defaultScale;
        marker.Init();
        Redraw();
        return marker;
    }

    public override OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new []
        {
            new OnlineMapsSavableItem("markers", "2D Markers", SaveSettings)
            {
                priority = 90,
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    /// <summary>
    /// Load items and component settings from JSON
    /// </summary>
    /// <param name="json">JSON item</param>
    public void LoadSettings(OnlineMapsJSONItem json)
    {
        OnlineMapsJSONItem jitems = json["items"];
        RemoveAll();
        foreach (OnlineMapsJSONItem jitem in jitems)
        {
            OnlineMapsMarker marker = new OnlineMapsMarker();

            double mx = jitem.ChildValue<double>("longitude");
            double my = jitem.ChildValue<double>("latitude");

            marker.SetPosition(mx, my);

            marker.range = jitem.ChildValue<OnlineMapsRange>("range");
            marker.label = jitem.ChildValue<string>("label");
            marker.texture = OnlineMapsUtils.GetObject(jitem.ChildValue<int>("texture")) as Texture2D;
            marker.align = (OnlineMapsAlign)jitem.ChildValue<int>("align");
            marker.rotation = jitem.ChildValue<float>("rotation");
            marker.enabled = jitem.ChildValue<bool>("enabled");
            Add(marker);
        }

        OnlineMapsJSONItem jsettings = json["settings"];
        defaultTexture = OnlineMapsUtils.GetObject(jsettings.ChildValue<int>("defaultTexture")) as Texture2D;
        defaultAlign = (OnlineMapsAlign)jsettings.ChildValue<int>("defaultAlign");
        defaultScale = jsettings.ChildValue<float>("defaultScale");
        allowAddMarkerByM = jsettings.ChildValue<bool>("allowAddMarkerByM");
    }

    protected override OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONItem jitem = base.SaveSettings();
        jitem["settings"].AppendObject(new
        {
            defaultTexture = defaultTexture != null? defaultTexture.GetInstanceID(): -1,
            defaultAlign = (int)defaultAlign,
            defaultScale,
            allowAddMarkerByM
        });
        return jitem;
    }

    protected override void Start()
    {
        base.Start();

        foreach (OnlineMapsMarker marker in items)
        {
            marker.manager = this;
            marker.Init();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (allowAddMarkerByM && Input.GetKeyUp(KeyCode.M))
        {
            double lng, lat;
            if (map.control.GetCoords(out lng, out lat)) Create(lng, lat);
        }
    }
}