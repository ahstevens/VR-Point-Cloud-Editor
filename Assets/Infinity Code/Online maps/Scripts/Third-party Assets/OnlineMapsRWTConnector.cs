/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

#if RWT3
using InfinityCode.RealWorldTerrain;
#endif

/// <summary>
/// Real World Terrain Connector
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Real World Terrain Connector")]
[OnlineMapsPlugin("Real World Terrain Connector", typeof(OnlineMapsControlBase))]
public class OnlineMapsRWTConnector : MonoBehaviour, IOnlineMapsSavableComponent
{
    /// <summary>
    /// Connection mode
    /// </summary>
    public Mode mode = Mode.markerOnPosition;

    /// <summary>
    /// Which object should be displayed on the map.
    /// </summary>
    public PositionMode positionMode = PositionMode.transform;

    /// <summary>
    /// The texture of the marker to be displayed on the map.
    /// </summary>
    public Texture2D markerTexture;

    /// <summary>
    /// The label of the marker to be displayed on the map.
    /// </summary>
    public string markerLabel;

    /// <summary>
    /// Position, which must be shown on the map.
    /// </summary>
    public Vector3 scenePosition;

    /// <summary>
    /// Coordinates, which must be shown on the map.
    /// </summary>
    public Vector2 coordinates;

    /// <summary>
    /// Transform, which must be shown on the map.
    /// </summary>
    public Transform targetTransform;

    private OnlineMapsSavableItem[] savableItems;

#if RWT || RWT3
    private RealWorldTerrainContainer rwt;
    private OnlineMapsMarker marker;
    private OnlineMaps map;
#endif

    /// <summary>
    /// Coordinates of selecteted object
    /// </summary>
    public Vector2 currentPosition
    {
        get
        {
            if (positionMode == PositionMode.coordinates) return coordinates;
            if (positionMode == PositionMode.scenePosition) return WorldPositionToCoordinates(scenePosition);
            if (positionMode == PositionMode.transform) return WorldPositionToCoordinates(targetTransform.position);
            return Vector2.zero;
        }
    }

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("rwtConnector", "RWT Connector", SaveSettings)
            {
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    private void LoadSettings(OnlineMapsJSONObject json)
    {
        json.DeserializeObject(this);
    }

    private OnlineMapsJSONItem SaveSettings()
    {
        return OnlineMapsJSON.Serialize(this);
    }

    /// <summary>
    /// Converts position to geographic coordinates.
    /// </summary>
    /// <param name="position">World position</param>
    /// <returns>Geographic coordinates</returns>
    public Vector2 WorldPositionToCoordinates(Vector3 position)
    {
#if RWT3
        Vector2 result;
        rwt.GetCoordinatesByWorldPosition(position, out result);
        return result;
#elif RWT
        RealWorldTerrainItem[,] terrains = rwt.terrains;

        foreach (RealWorldTerrainItem item in terrains)
        {
            Vector3 tp = item.terrain.transform.position;
            Vector3 size = item.terrainData.size;

            if (tp.x <= position.x && tp.z <= position.z && tp.x + size.x >= position.x && tp.z + size.z >= position.z)
            {
                float dx = item.bottomRight.x - item.topLeft.x;
                float dy = item.topLeft.y - item.bottomRight.y;

                float rx = (position.x - tp.x) / size.x;
                float rz = (position.z - tp.z) / size.z;

                float px = dx * rx;
                float py = dy * rz;

                return new Vector2(px + item.topLeft.x, py + item.bottomRight.y);
            }
        }
        return Vector2.zero;
#else
        return Vector2.zero;
#endif
    }

#if RWT || RWT3
    private void Start()
    {
        rwt = GetComponent<RealWorldTerrainContainer>();
        if (rwt == null)
        {
            Debug.LogError("Real World Terrain Connector should be together with Real World Terrain Container.");
            OnlineMapsUtils.Destroy(this);
            return;
        }
        
        if (positionMode == PositionMode.transform && targetTransform == null)
        {
            Debug.LogError("Target Transform is not specified.");
            OnlineMapsUtils.Destroy(this);
            return;
        }

        map = OnlineMaps.instance;

        if (mode == Mode.centerOnPosition)
        {
            map.position = currentPosition;
        }
        else if (mode == Mode.markerOnPosition)
        {
            marker = OnlineMapsMarkerManager.CreateItem(currentPosition, markerTexture, markerLabel);
        }
    }

    private void Update()
    {
        if (mode == Mode.markerOnPosition)
        {
            Vector2 newPos = currentPosition;
            if (marker.position != newPos)
            {
                marker.position = newPos;
                map.Redraw();
            }
        }
        else if (mode == Mode.centerOnPosition)
        {
            Vector2 newPos = currentPosition;
            if (map.position != newPos)
            {
                map.position = currentPosition;
                map.Redraw();
            }
                
        }
    }
#endif

    /// <summary>
    /// Connection mode
    /// </summary>
    public enum Mode
    {
        markerOnPosition,
        centerOnPosition
    }

    /// <summary>
    /// Which object should be displayed on the map.
    /// </summary>
    public enum PositionMode
    {
        transform,
        coordinates,
        scenePosition
    }
}

