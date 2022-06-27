/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// Alignment of marker.
/// </summary>
public enum OnlineMapsAlign
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight
}

/// <summary>
/// Type of altitude
/// </summary>
public enum OnlineMapsAltitudeType
{
    /// <summary>
    /// Altitude above sea level
    /// </summary>
    absolute,

    /// <summary>
    /// Altitude above ground level
    /// </summary>
    relative
}

/// <summary>
/// Buffer state
/// </summary>
public enum OnlineMapsBufferStatus
{
    wait,
    working,
    complete,
    start,
    disposed
}

/// <summary>
/// Point at which the camera should look.
/// </summary>
public enum OnlineMapsCameraAdjust
{
    maxElevationInArea,
    centerPointElevation,
    gameObject,
    averageCenter
}

/// <summary>
/// The rule for calculating the lowest mesh point
/// </summary>
public enum OnlineMapsElevationBottomMode
{
    /// <summary>
    /// Based on zero elevation
    /// </summary>
    zero,

    /// <summary>
    /// Based on the minimum value in the area
    /// </summary>
    minValue
}

/// <summary>
/// OnlineMaps events.
/// </summary>
public enum OnlineMapsEvents
{
    changedPosition,
    changedZoom
}

public enum OnlineMapsLocationServiceMarkerType
{
    twoD = 0,
    threeD = 1
}

public enum OnlineMapsMarker2DMode
{
    flat,
    billboard
}

public enum OnlineMapsOSMOverpassServer
{
    main = 0,
    main2 = 1,
    french = 2,
    taiwan = 3,
    kumiSystems = 4,
}

public enum OnlineMapsPositionRangeType
{
    center,
    border
}

public enum OnlineMapsProjectionEnum
{
    sphericalMercator,
    wgs84Mercator
}

/// <summary>
/// Status of the request to the Google Maps API.
/// </summary>
public enum OnlineMapsQueryStatus
{
    idle,
    downloading,
    success,
    error,
    disposed
}

/// <summary>
/// Map redraw type.
/// </summary>
public enum OnlineMapsRedrawType
{
    full,
    area,
    move,
    none
}

/// <summary>
/// When need to show marker tooltip.
/// </summary>
public enum OnlineMapsShowMarkerTooltip
{
    onHover,
    onPress,
    always,
    none
}

/// <summary>
/// Source of map tiles.
/// </summary>
public enum OnlineMapsSource
{
    Online,
    Resources,
    ResourcesAndOnline,
    StreamingAssets,
    StreamingAssetsAndOnline
}

public enum OnlineMapsTarget
{
    texture,
    mesh,
    tileset,
    spriteset,
}

/// <summary>
/// Tile state
/// </summary>
public enum OnlineMapsTileStatus
{
    none,
    loading,
    loaded,
    error,
    disposed
}

/// <summary>
/// Type of checking 2D markers on visibility.
/// </summary>
public enum OnlineMapsTilesetCheckMarker2DVisibility
{
    /// <summary>
    /// Will be checked only coordinates of markers. Faster.
    /// </summary>
    pivot,

    /// <summary>
    /// Will be checked all the border of marker. If the marker is located on the map at least one point, then it will be shown.
    /// </summary>
    bounds
}

public enum OnlineMapsTilesetDrawingMode
{
    meshes,
    overlay
}

/// <summary>
/// Mode of smooth zoom.
/// </summary>
public enum OnlineMapsZoomMode
{
    /// <summary>
    /// Zoom at touch point.
    /// </summary>
    target,

    /// <summary>
    /// Zoom at center of map.
    /// </summary>
    center
}

public enum OnlineMapsZoomEvent
{
    doubleClick,
    wheel,
    gesture
}