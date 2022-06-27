/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if !UNITY_WEBGL
using System.Threading;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// The main class. With it you can control the map.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Online Maps")]
[Serializable]
public class OnlineMaps : MonoBehaviour, ISerializationCallbackReceiver, IOnlineMapsSavableComponent
{
#region Variables
    /// <summary>
    /// The current version of Online Maps
    /// </summary>
    public const string version = "3.7.15.1";

    /// <summary>
    /// The minimum zoom level
    /// </summary>
    public const int MINZOOM = 1;

    /// <summary>
    /// The maximum zoom level
    /// </summary>
#if ONLINEMAPS_MAXZOOM_23
    public const int MAXZOOM = 23;
#elif ONLINEMAPS_MAXZOOM_22
    public const int MAXZOOM = 22;
#elif ONLINEMAPS_MAXZOOM_21
    public const int MAXZOOM = 21;
#else
    public const int MAXZOOM = 20;
#endif

    public const float MAXZOOM_DELTA = 0.999f;
    public const float MAXZOOM_EXT = MAXZOOM + MAXZOOM_DELTA;

    #region Static Actions

    /// <summary>
    /// The event is called when the map starts.
    /// </summary>
    public static Action<OnlineMaps> OnStart;

    /// <summary>
    /// The event occurs after generating buffer and before update control to preload tiles for tileset.
    /// </summary>
    public static Action<OnlineMaps> OnPreloadTiles;

    #endregion

    #region Actions

    /// <summary>
    /// Event caused when the user change map position.
    /// </summary>
    public Action OnChangePosition;

    /// <summary>
    /// Event caused when the user change map zoom.
    /// </summary>
    public Action OnChangeZoom;

    /// <summary>
    /// Event caused at the end of OnGUI method
    /// </summary>
    public Action OnGUIAfter;

    /// <summary>
    /// Event caused at the beginning of OnGUI method
    /// </summary>
    public Action OnGUIBefore;

    /// <summary>
    /// Intercepts getting marker by the screen coordinates.
    /// </summary>
    public Func<Vector2, OnlineMapsMarker> OnGetMarkerFromScreen;

    /// <summary>
    /// The event is invoked at the end LateUpdate.
    /// </summary>
    public Action OnLateUpdateAfter;

    /// <summary>
    /// The event is called at the start LateUpdate.
    /// </summary>
    public Action OnLateUpdateBefore;

    /// <summary>
    /// Event which is called after the redrawing of the map.
    /// </summary>
    public Action OnMapUpdated;

    /// <summary>
    /// Event is called before Update.
    /// </summary>
    public Action OnUpdateBefore;

    /// <summary>
    /// Event is called after Update.
    /// </summary>
    public Action OnUpdateLate;

#endregion

#region Static Fields

    public static bool isPlaying = false;

    /// <summary>
    /// Specifies whether the user interacts with the map.
    /// </summary>
    public static bool isUserControl = false;

    private static OnlineMaps _instance;

    #endregion

    #region Public Fields

    /// <summary>
    /// Allows drawing of map.<br/>
    /// <strong>
    /// Important: The interaction with the map, add or remove markers and drawing elements, automatically allowed to redraw the map.<br/>
    /// Use lockRedraw, to prohibit the redrawing of the map.
    /// </strong>
    /// </summary>
    public bool allowRedraw;

    /// <summary>
    /// Allows you to block all user interactions with the map, markers, drawing elements. But you can still interact with the map using the API.
    /// </summary>
    public bool blockAllInteractions;

    /// <summary>
    /// Tiles for the specified number of parent levels will be loaded.
    /// </summary>
    [Range(0, 20)]
    [Tooltip("Tiles for the specified number of parent levels will be loaded")]
    public int countParentLevels = 5;

    /// <summary>
    /// URL of custom provider.<br/>
    /// Support tokens:<br/>
    /// {x} - tile x<br/>
    /// {y} - tile y<br/>
    /// {zoom} - zoom level<br/>
    /// {quad} - uniquely identifies a single tile at a particular level of detail.
    /// </summary>
    public string customProviderURL = "http://localhost/{zoom}/{y}/{x}";

    /// <summary>
    /// URL of custom traffic provider.<br/>
    /// Support tokens:<br/>
    /// {x} - tile x<br/>
    /// {y} - tile y<br/>
    /// {zoom} - zoom level<br/>
    /// {quad} - uniquely identifies a single tile at a particular level of detail.
    /// </summary>
    public string customTrafficProviderURL = "http://localhost/{zoom}/{y}/{x}";

    /// <summary>
    /// Texture displayed until the tile is not loaded.
    /// </summary>
    [Tooltip("The texture that will be displayed until the tile is loaded")]
    public Texture2D defaultTileTexture;

    /// <summary>
    /// Specifies whether to dispatch the event.
    /// </summary>
    public bool dispatchEvents = true;

    /// <summary>
    /// Drag marker while holding CTRL.
    /// </summary>
    [Tooltip("Hold CTRL and press on the marker to drag the item.")]
    public bool dragMarkerHoldingCTRL = false;

    /// <summary>
    /// Color, which is used until the tile is not loaded, unless specified field defaultTileTexture.
    /// </summary>
    [Tooltip("The color that will be displayed until the tile is loaded.\nImportant: if Default Tile Texture is specified, this value will be ignored.")]
    public Color emptyColor = Color.gray;

    /// <summary>
    /// Map height in pixels.
    /// </summary>
    public int height = 1024;

    /// <summary>
    /// Specifies whether to display the labels on the map.
    /// </summary>
    public bool labels = true;

    /// <summary>
    /// Language of the labels on the map.
    /// </summary>
    public string language = "en";

    /// <summary>
    /// Prohibits drawing of maps.<br/>
    /// <strong> Important: Do not forget to disable this restriction. Otherwise, the map will never be redrawn.</strong>
    /// </summary>
    public bool lockRedraw = false;

    /// <summary>
    /// A flag that indicates that need to redraw the map.
    /// </summary>
    public bool needRedraw;

    /// <summary>
    /// Not interact under the GUI.
    /// </summary>
    [Tooltip("Should Online Maps ignore clicks if an IMGUI or uGUI element is under the cursor?")]
    public bool notInteractUnderGUI = true;

    /// <summary>
    /// ID of current map type.
    /// </summary>
    public string mapType;

    /// <summary>
    /// Server for requests to the Open Street Map Overpass API.
    /// </summary>
    public OnlineMapsOSMOverpassServer osmServer = OnlineMapsOSMOverpassServer.main;

    /// <summary>
    /// URL of the proxy server used for WebGL platform.
    /// </summary>
    public string proxyURL = "https://service.infinity-code.com/redirect.php?";

    /// <summary>
    /// A flag that indicates whether to redraw the map at startup.
    /// </summary>
    [Tooltip("Redraw the map immediately after the start of the scene")]
    public bool redrawOnPlay;

    /// <summary>
    /// Render map in a separate thread. Recommended.
    /// </summary>
    [Tooltip("If you have any problems with multithreading, disable this field.")]
    public bool renderInThread = true;

    /// <summary>
    /// Template path in Resources, from where the tiles will be loaded. This field supports tokens.
    /// </summary>
    public string resourcesPath = "OnlineMapsTiles/{zoom}/{x}/{y}";

    /// <summary>
    /// Template path in Streaming Assets, from where the tiles will be loaded. This field supports tokens.
    /// </summary>
    public string streamingAssetsPath = "OnlineMapsTiles/{zoom}/{x}/{y}.png";

    /// <summary>
    /// Indicates when the marker will show tips.
    /// </summary>
    [Tooltip("Tooltip display rule")]
    public OnlineMapsShowMarkerTooltip showMarkerTooltip = OnlineMapsShowMarkerTooltip.onHover;

    /// <summary>
    /// Specifies from where the tiles should be loaded (Online, Resources, Online and Resources).
    /// </summary>
    [Tooltip("Source of tiles")]
    public OnlineMapsSource source = OnlineMapsSource.Online;

    /// <summary>
    /// Indicates that Unity need to stop playing when compiling scripts.
    /// </summary>
    [Tooltip("Should Online Maps stop playing when recompiling scripts?")]
    public bool stopPlayingWhenScriptsCompile = true;

    /// <summary>
    /// Texture, which is used to draw the map. <br/>
    /// <strong>To change this value, use OnlineMaps.SetTexture.</strong>
    /// </summary>
    public Texture2D texture;

    /// <summary>
    /// Reference to tile manager
    /// </summary>
    [NonSerialized]
    public OnlineMapsTileManager tileManager;

    /// <summary>
    /// Reference to tooltip drawer
    /// </summary>
    [NonSerialized]
    public OnlineMapsTooltipDrawerBase tooltipDrawer;

    /// <summary>
    /// Background texture of tooltip
    /// </summary>
    [Tooltip("Tooltip background texture")]
    public Texture2D tooltipBackgroundTexture;

    /// <summary>
    /// Specifies whether to draw traffic
    /// </summary>
    [Tooltip("Display traffic jams")]
    public bool traffic = false;

    /// <summary>
    /// Provider of traffic jams
    /// </summary>
    [NonSerialized]
    public OnlineMapsTrafficProvider trafficProvider;

    /// <summary>
    /// ID of current traffic provider
    /// </summary>
    public string trafficProviderID = "googlemaps";

    /// <summary>
    /// Use only the current zoom level of the tiles.
    /// </summary>
    [Obsolete("Use countParentLevels")]
    public bool useCurrentZoomTiles = false;

    /// <summary>
    /// Use a proxy server for WebGL?
    /// </summary>
    public bool useProxy = true;

    /// <summary>
    /// Specifies is necessary to use software JPEG decoder.
    /// Use only if you have problems with hardware decoding of JPEG.
    /// </summary>
    [Tooltip("If you have problems decoding JPEG images, use software decoder.\nKeep in mind that this greatly affects performance.")]
    public bool useSoftwareJPEGDecoder = false;

    /// <summary>
    /// Map width in pixels.
    /// </summary>
    public int width = 1024;

#endregion

#region Private Fields

    [NonSerialized]
    private OnlineMapsProvider.MapType _activeType;

    [SerializeField]
    private string _activeTypeSettings;

    [NonSerialized]
    private OnlineMapsBuffer _buffer;

    [SerializeField]
    private float _zoom = MINZOOM;

    [SerializeField]
    private double latitude = 0;

    [SerializeField]
    private double longitude = 0;

#if NETFX_CORE
    private OnlineMapsThreadWINRT renderThread;
#elif !UNITY_WEBGL
    private Thread renderThread;
#endif

    private OnlineMapsControlBase _control;
    private bool _labels;
    private string _language;
    private string _mapType;
    private OnlineMapsPositionRange _positionRange;
    private OnlineMapsProjection _projection;
    private bool _traffic;
    private string _trafficProviderID;
    private OnlineMapsRange _zoomRange;
    private double bottomRightLatitude;
    private double bottomRightLongitude;
    private Color[] defaultColors;
    private int izoom = MINZOOM;
    private OnlineMapsSavableItem[] savableItems;
    private double topLeftLatitude;
    private double topLeftLongitude;

#endregion
#endregion

#region Properties

#region Static  Properties

    /// <summary>
    /// Singleton instance of map.
    /// </summary>
    public static OnlineMaps instance
    {
        get { return _instance; }
    }

#endregion

#region Public  Properties

    /// <summary>
    /// Active type of map.
    /// </summary>
    public OnlineMapsProvider.MapType activeType
    {
        get
        {
            if (_activeType == null || _activeType.fullID != mapType)
            {
                _activeType = OnlineMapsProvider.FindMapType(mapType);
                _projection = _activeType.provider.projection;
                mapType = _activeType.fullID;
            }
            return _activeType;
        }
        set
        {
            if (_activeType == value) return;

            _activeType = value;
            _projection = _activeType.provider.projection;
            _mapType = mapType = value.fullID;

            if (isPlaying) RedrawImmediately();
        }
    }

    /// <summary>
    /// Gets the bottom right position.
    /// </summary>
    /// <value>
    /// The bottom right position.
    /// </value>
    public Vector2 bottomRightPosition
    {
        get
        {
            if (Math.Abs(bottomRightLatitude) < double.Epsilon && Math.Abs(bottomRightLongitude) < double.Epsilon) UpdateCorners();
            return new Vector2((float)bottomRightLongitude, (float)bottomRightLatitude);
        }
    }

    /// <summary>
    /// Gets the coordinates of the map view.
    /// </summary>
    public OnlineMapsGeoRect bounds
    {
        get
        {
            return new OnlineMapsGeoRect(topLeftLongitude, topLeftLatitude, bottomRightLongitude, bottomRightLatitude);
        }
    }

    /// <summary>
    /// Reference to the current draw buffer.
    /// </summary>
    public OnlineMapsBuffer buffer
    {
        get
        {
            if (_buffer == null) _buffer = new OnlineMapsBuffer(this);
            return _buffer;
        }
    }

    /// <summary>
    /// The current state of the drawing buffer.
    /// </summary>
    public OnlineMapsBufferStatus bufferStatus
    {
        get { return buffer.status; }
    }

    /// <summary>
    /// Display control script.
    /// </summary>
    public OnlineMapsControlBase control
    {
        get
        {
            if (_control == null) _control = GetComponent<OnlineMapsControlBase>();
            return _control;
        }
    }

    /// <summary>
    /// Gets and sets float zoom value
    /// </summary>
    public float floatZoom
    {
        get { return _zoom; }
        set
        {
            if (Mathf.Abs(_zoom - value) < float.Epsilon) return;

            float z = Mathf.Clamp(value, MINZOOM, MAXZOOM_EXT);
            if (zoomRange != null) z = zoomRange.CheckAndFix(z);
            z = CheckMapSize(z);
            if (Math.Abs(_zoom - z) < float.Epsilon) return;

            _zoom = z;
            izoom = (int) z;
            SetPosition(longitude, latitude, false);
            UpdateCorners();
            allowRedraw = true;
            needRedraw = true;
            DispatchEvent(OnlineMapsEvents.changedZoom);
        }
    }

    /// <summary>
    /// Coordinates of the center point of the map.
    /// </summary>
    public Vector2 position
    {
        get { return new Vector2((float)longitude, (float)latitude); }
        set
        {
            SetPosition(value.x, value.y);
        }
    }

    /// <summary>
    /// Limits the range of map coordinates.
    /// </summary>
    public OnlineMapsPositionRange positionRange
    {
        get { return _positionRange; }
        set
        {
            _positionRange = value;
            if (value != null)
            {
                if (value.CheckAndFix(ref longitude, ref latitude)) UpdateCorners();
            }
        }
    }

    /// <summary>
    /// Projection of active provider.
    /// </summary>
    public OnlineMapsProjection projection
    {
        get
        {
            if (_projection == null) _projection = activeType.provider.projection;
            return _projection;
        }
    }

    /// <summary>
    /// Gets the top left position.
    /// </summary>
    /// <value>
    /// The top left position.
    /// </value>
    public Vector2 topLeftPosition
    {
        get
        {
            if (Math.Abs(topLeftLatitude) < double.Epsilon && Math.Abs(topLeftLongitude) < double.Epsilon) UpdateCorners();

            return new Vector2((float)topLeftLongitude, (float)topLeftLatitude);
        }
    }

    /// <summary>
    /// Current zoom.
    /// </summary>
    public int zoom
    {
        get
        {
            if (izoom == 0) izoom = (int) _zoom;
            return izoom;
        }
        set { floatZoom = value; }
    }

    /// <summary>
    /// The scaling factor for zoom
    /// </summary>
    public float zoomCoof
    {
        get { return 1 - zoomScale / 2; }
    }

    /// <summary>
    /// Specifies the valid range of map zoom.
    /// </summary>
    public OnlineMapsRange zoomRange
    {
        get { return _zoomRange; }
        set
        {
            _zoomRange = value;
            if (value != null) floatZoom = value.CheckAndFix(floatZoom);
        }
    }

    /// <summary>
    /// The fractional part of zoom
    /// </summary>
    public float zoomScale
    {
        get { return _zoom - zoom; }
    }

#endregion
#endregion

#region Methods

    public void Awake()
    {
        _instance = this;
        tileManager = new OnlineMapsTileManager(this);

        if (control == null)
        {
            Debug.LogError("Can not find a Control.");
            return;
        }

        if (control.resultIsTexture)
        {
            if (texture != null)
            {
                width = texture.width;
                height = texture.height;
            }
        }
        else
        {
            texture = null;
        }

        izoom = (int)floatZoom;
        UpdateCorners();

        control.OnAwakeBefore();

        if (control.resultIsTexture)
        {
            if (texture != null) defaultColors = texture.GetPixels();

            if (defaultTileTexture == null)
            {
                OnlineMapsRasterTile.defaultColors = new Color32[OnlineMapsUtils.sqrTileSize];
                for (int i = 0; i < OnlineMapsUtils.sqrTileSize; i++) OnlineMapsRasterTile.defaultColors[i] = emptyColor;
            }
            else OnlineMapsRasterTile.defaultColors = defaultTileTexture.GetPixels32();
        }

        SetPosition(longitude, latitude);
    }

    private void CheckBaseProps()
    {
        if (mapType != _mapType)
        {
            activeType = OnlineMapsProvider.FindMapType(mapType);
            _mapType = mapType = activeType.fullID;
            if (_buffer != null) _buffer.UnloadOldTypes();
            Redraw();
        }

        if (_language != language || _labels != labels)
        {
            _labels = labels;
            _language = language;

            if (_buffer != null)
            {
                _buffer.Dispose();
                _buffer = null;
#if NETFX_CORE
                if (renderThread != null) renderThread.Dispose();
#endif
#if !UNITY_WEBGL
                renderThread = null;
#endif
            }
            
            Redraw();
        }
        if (traffic != _traffic || trafficProviderID != _trafficProviderID)
        {
            _traffic = traffic;

            _trafficProviderID = trafficProviderID;
            trafficProvider = OnlineMapsTrafficProvider.GetByID(trafficProviderID);

            OnlineMapsTile[] tiles;
            lock (OnlineMapsTile.lockTiles)
            {
                tiles = tileManager.tiles.ToArray();
            }
            if (traffic)
            {
                foreach (OnlineMapsTile tile in tiles)
                {
                    OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;
                    rTile.trafficProvider = trafficProvider;
                    rTile.trafficWWW = new OnlineMapsWWW(rTile.trafficURL);
                    rTile.trafficWWW["tile"] = tile;
                    rTile.trafficWWW.OnComplete += OnlineMapsTileManager.OnTrafficWWWComplete;
                    if (rTile.trafficTexture != null)
                    {
                        OnlineMapsUtils.Destroy(rTile.trafficTexture);
                        rTile.trafficTexture = null;
                    }

                    rTile.mergedColors = null;
                }
            }
            else
            {
                foreach (OnlineMapsTile tile in tiles)
                {
                    OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;
                    if (rTile.trafficTexture != null)
                    {
                        OnlineMapsUtils.Destroy(rTile.trafficTexture);
                        rTile.trafficTexture = null;
                    }
                    rTile.trafficWWW = null;
                    rTile.mergedColors = null;
                }
            }
            Redraw();
        }
    }

    private void CheckBufferComplete()
    {
        if (buffer.status != OnlineMapsBufferStatus.complete) return;
        if (buffer.needUnloadTiles) buffer.UnloadOldTiles();

        tileManager.UnloadUnusedTiles();

        if (allowRedraw)
        {
            if (control.resultIsTexture)
            {
                if (texture != null)
                {
                    texture.SetPixels32(buffer.frontBuffer);
                    texture.Apply(false);
                    if (control.activeTexture != texture) control.SetTexture(texture);
                }
            }

            if (OnPreloadTiles != null) OnPreloadTiles(this);
            if (OnlineMapsTileManager.OnPreloadTiles != null) OnlineMapsTileManager.OnPreloadTiles();
            if (control is OnlineMapsControlBase3D) (control as OnlineMapsControlBase3D).UpdateControl();

            if (OnMapUpdated != null) OnMapUpdated();
        }

        buffer.status = OnlineMapsBufferStatus.wait;
    }

    public float CheckMapSize(float z)
    {
        int iz = (int)z;
        long max = (1L << iz) * OnlineMapsUtils.tileSize;
        if (max < width || max < height) return CheckMapSize(iz + 1);

        return z;
    }

#if UNITY_EDITOR
    private void CheckScriptCompiling()
    {
        isPlaying = EditorApplication.isPlaying;
        if (!isPlaying) EditorApplication.update -= CheckScriptCompiling;

        if (stopPlayingWhenScriptsCompile && isPlaying && EditorApplication.isCompiling)
        {
            Debug.Log("Online Maps stop playing to compile scripts.");
            EditorApplication.isPlaying = false;
        }
    }
#endif

    /// <summary>
    /// Allows you to test the connection to the Internet.
    /// </summary>
    /// <param name="callback">Function, which will return the availability of the Internet.</param>
    public void CheckServerConnection(Action<bool> callback)
    {
        OnlineMapsTile tempTile = control.CreateTile(350, 819, 11, false);
        string url = tempTile.url;
        tempTile.Dispose();

        OnlineMapsWWW checkConnectionWWW = new OnlineMapsWWW(url);
        checkConnectionWWW.OnComplete += www =>
        {
            callback(!www.hasError);
        };
    }

    /// <summary>
    /// Dispatch map events.
    /// </summary>
    /// <param name="evs">Events you want to dispatch.</param>
    public void DispatchEvent(params OnlineMapsEvents[] evs)
    {
        if (!dispatchEvents) return;

        foreach (OnlineMapsEvents ev in evs)
        {
            if (ev == OnlineMapsEvents.changedPosition && OnChangePosition != null) OnChangePosition();
            else if (ev == OnlineMapsEvents.changedZoom && OnChangeZoom != null) OnChangeZoom();
        }
    }

    private void FixPositionUsingBorders(ref double lng, ref double lat, int countX, int countY)
    {
        double px, py;
        projection.CoordinatesToTile(lng, lat, zoom, out px, out py);
        double ox = countX / 2d;
        double oy = countY / 2d;

        double tlx, tly, brx, bry;

        projection.TileToCoordinates(px - ox, py - oy, zoom, out tlx, out tly);
        projection.TileToCoordinates(px + ox, py + oy, zoom, out brx, out bry);

        bool tlxc = false;
        bool tlyc = false;
        bool brxc = false;
        bool bryc = false;

        if (tlx < positionRange.minLng)
        {
            tlxc = true;
            tlx = positionRange.minLng;
        }
        if (brx > positionRange.maxLng)
        {
            brxc = true;
            brx = positionRange.maxLng;
        }
        if (tly > positionRange.maxLat)
        {
            tlyc = true;
            tly = positionRange.maxLat;
        }
        if (bry < positionRange.minLat)
        {
            bryc = true;
            bry = positionRange.minLat;
        }

        double tmp;
        bool recheckX = false, recheckY = false;

        if (tlxc && brxc)
        {
            double tx1, tx2;
            projection.CoordinatesToTile(positionRange.minLng, positionRange.maxLat, zoom, out tx1, out tmp);
            projection.CoordinatesToTile(positionRange.maxLng, positionRange.minLat, zoom, out tx2, out tmp);
            px = (tx1 + tx2) / 2;
        }
        else if (tlxc)
        {
            projection.CoordinatesToTile(tlx, tly, zoom, out px, out tmp);
            px += ox;
            recheckX = true;
        }
        else if (brxc)
        {
            projection.CoordinatesToTile(brx, bry, zoom, out px, out tmp);
            px -= ox;
            recheckX = true;
        }

        if (tlyc && bryc)
        {
            double ty1, ty2;
            projection.CoordinatesToTile(positionRange.minLng, positionRange.maxLat, zoom, out tmp, out ty1);
            projection.CoordinatesToTile(positionRange.maxLng, positionRange.minLat, zoom, out tmp, out ty2);
            py = (ty1 + ty2) / 2;
        }
        else if (tlyc)
        {
            projection.CoordinatesToTile(tlx, tly, zoom, out tmp, out py);
            py += oy;
            recheckY = true;
        }
        else if (bryc)
        {
            projection.CoordinatesToTile(brx, bry, zoom, out tmp, out py);
            py -= oy;
            recheckY = true;
        }

        if (recheckX || recheckY)
        {
            projection.TileToCoordinates(px - ox, py - oy, zoom, out tlx, out tly);
            projection.TileToCoordinates(px + ox, py + oy, zoom, out brx, out bry);
            bool centerX = false, centerY = false;
            if (tlx < positionRange.minLng && brxc) centerX = true;
            else if (brx > positionRange.maxLng && tlxc) centerX = true;

            if (tly > positionRange.maxLat && bryc) centerY = true;
            else if (bry < positionRange.minLat && tlyc) centerY = true;

            if (centerX)
            {
                double tx1, tx2;
                projection.CoordinatesToTile(positionRange.minLng, positionRange.maxLat, zoom, out tx1, out tmp);
                projection.CoordinatesToTile(positionRange.maxLng, positionRange.minLat, zoom, out tx2, out tmp);
                px = (tx1 + tx2) / 2;
            }
            if (centerY)
            {
                double ty1, ty2;
                projection.CoordinatesToTile(positionRange.minLng, positionRange.maxLat, zoom, out tmp, out ty1);
                projection.CoordinatesToTile(positionRange.maxLng, positionRange.minLat, zoom, out tmp, out ty2);
                py = (ty1 + ty2) / 2;
            }
        }

        if (tlxc || brxc || tlyc || bryc) projection.TileToCoordinates(px, py, zoom, out lng, out lat);
    }

    /// <summary>
    /// Get the bottom-right corner of the map.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public void GetBottomRightPosition(out double lng, out double lat)
    {
        if (Math.Abs(bottomRightLatitude) < double.Epsilon && Math.Abs(bottomRightLongitude) < double.Epsilon) UpdateCorners();
        lng = bottomRightLongitude;
        lat = bottomRightLatitude;
    }

    /// <summary>
    /// Returns the coordinates of the corners of the map
    /// </summary>
    /// <param name="tlx">Longitude of the left border</param>
    /// <param name="tly">Latitude of the top border</param>
    /// <param name="brx">Longitude of the right border</param>
    /// <param name="bry">Latitude of the bottom border</param>
    public void GetCorners(out double tlx, out double tly, out double brx, out double bry)
    {
        if (Math.Abs(bottomRightLatitude) < double.Epsilon && Math.Abs(bottomRightLongitude) < double.Epsilon || Math.Abs(topLeftLatitude) < double.Epsilon && Math.Abs(topLeftLongitude) < double.Epsilon) UpdateCorners();

        brx = bottomRightLongitude;
        bry = bottomRightLatitude;
        tlx = topLeftLongitude;
        tly = topLeftLatitude;
    }

    /// <summary>
    /// Gets drawing element from screen.
    /// </summary>
    /// <param name="screenPosition">Screen position.</param>
    /// <returns>Drawing element</returns>
    public OnlineMapsDrawingElement GetDrawingElement(Vector2 screenPosition)
    {
        Vector2 coords = control.GetCoords(screenPosition);
        return control.drawingElementManager.LastOrDefault(el => el.HitTest(coords, zoom));
    }

    /// <summary>
    /// Get the map coordinate.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public void GetPosition(out double lng, out double lat)
    {
        lat = latitude;
        lng = longitude;
    }

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("map", "Map settings", SaveSettings)
            {
                priority = 100,
                loadCallback = Load
            }
        };

        return savableItems;
    }

    /// <summary>
    /// Get the tile coordinates of the corners of the map
    /// </summary>
    /// <param name="tlx">Left tile X</param>
    /// <param name="tly">Top tile Y</param>
    /// <param name="brx">Right tile X</param>
    /// <param name="bry">Bottom tile Y</param>
    public void GetTileCorners(out double tlx, out double tly, out double brx, out double bry)
    {
        GetTileCorners(out tlx, out tly, out brx, out bry, zoom);
    }

    /// <summary>
    /// Get the tile coordinates of the corners of the map
    /// </summary>
    /// <param name="tlx">Left tile X</param>
    /// <param name="tly">Top tile Y</param>
    /// <param name="brx">Right tile X</param>
    /// <param name="bry">Bottom tile Y</param>
    /// <param name="zoom">Zoom</param>
    public void GetTileCorners(out double tlx, out double tly, out double brx, out double bry, int zoom)
    {
        if (Math.Abs(bottomRightLatitude) < double.Epsilon && Math.Abs(bottomRightLongitude) < double.Epsilon || 
            Math.Abs(topLeftLatitude) < double.Epsilon && Math.Abs(topLeftLongitude) < double.Epsilon) UpdateCorners();

        projection.CoordinatesToTile(topLeftLongitude, topLeftLatitude, zoom, out tlx, out tly);
        projection.CoordinatesToTile(bottomRightLongitude, bottomRightLatitude, zoom, out brx, out bry);
    }

    /// <summary>
    /// Get the tile coordinates of the map
    /// </summary>
    /// <param name="px">Tile X</param>
    /// <param name="py">Tile Y</param>
    public void GetTilePosition(out double px, out double py)
    {
        projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);
    }

    /// <summary>
    /// Get the tile coordinates of the map
    /// </summary>
    /// <param name="px">Tile X</param>
    /// <param name="py">Tile Y</param>
    /// <param name="zoom">Zoom</param>
    public void GetTilePosition(out double px, out double py, int zoom)
    {
        projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);
    }

    /// <summary>
    /// Get the top-left corner of the map.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public void GetTopLeftPosition(out double lng, out double lat)
    {
        if (Math.Abs(topLeftLatitude) < double.Epsilon && Math.Abs(topLeftLongitude) < double.Epsilon) UpdateCorners();
        lng = topLeftLongitude;
        lat = topLeftLatitude;
    }

    /// <summary>
    /// Checks if the coordinates are in the map view.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <returns>True - coordinates in map view. False - coordinates outside the map view.</returns>
    public bool InMapView(double lng, double lat)
    {
        if (lat > topLeftLatitude || lat < bottomRightLatitude) return false;

        double tlx = topLeftLongitude;
        double brx = bottomRightLongitude;

        if (tlx > brx)
        {
            brx += 360;
            if (lng < tlx) lng += 360;
        }

        return tlx <= lng && brx >= lng;
    }

    private void LateUpdate()
    {
        if (OnLateUpdateBefore != null) OnLateUpdateBefore();

        if (control == null || lockRedraw) return;
        StartBuffer();
        CheckBufferComplete();

        if (OnLateUpdateAfter != null) OnLateUpdateAfter();
    }

    public void Load(OnlineMapsJSONItem json)
    {
        (json as OnlineMapsJSONObject).DeserializeObject(this, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        izoom = (int) _zoom;
    }

    public void OnAfterDeserialize()
    {
        try
        {
            activeType.LoadSettings(_activeTypeSettings);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(exception.Message + "\n" + exception.StackTrace);
        }
    }

    public void OnBeforeSerialize()
    {
        _activeTypeSettings = activeType.GetSettings();
    }

    private void OnDestroy()
    {
        OnlineMapsThreadManager.Dispose();

        if (_buffer != null)
        {
            _buffer.Dispose();
            _buffer = null;
        }
#if NETFX_CORE
        if (renderThread != null) renderThread.Dispose();
#endif
#if !UNITY_WEBGL
        renderThread = null;
#endif
        if (tileManager != null)
        {
            tileManager.Dispose();
            tileManager = null;
        }

        _control = null;

        if (defaultColors != null && texture != null)
        {
            if (texture.width * texture.height == defaultColors.Length)
            {
                texture.SetPixels(defaultColors);
                texture.Apply();
            }
        }

        OnChangePosition = null;
        OnChangeZoom = null;
        OnMapUpdated = null;
        OnUpdateBefore = null;
        OnUpdateLate = null;
    }

    private void OnDisable ()
    {
        OnlineMapsThreadManager.Dispose();

        if (_buffer != null)
        {
            _buffer.Dispose();
            _buffer = null;
        }

#if NETFX_CORE
        if (renderThread != null) renderThread.Dispose();
#endif
#if !UNITY_WEBGL
        renderThread = null;
#endif

        /*if (tileManager != null)
        {
            tileManager.Dispose();
            tileManager = null;
        }*/

        _control = null;

        if (_instance == this) _instance = null;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.update += CheckScriptCompiling;
#endif

        OnlineMapsUtils.persistentDataPath = Application.persistentDataPath;

        isPlaying = true;
        _instance = this;

        tooltipDrawer = new OnlineMapsGUITooltipDrawer(this);

        activeType = OnlineMapsProvider.FindMapType(mapType);
        _mapType = mapType = activeType.fullID;
        if (tileManager == null) tileManager = new OnlineMapsTileManager(this);

        trafficProvider = OnlineMapsTrafficProvider.GetByID(trafficProviderID);

        if (language == "") language = activeType.provider.twoLetterLanguage ? "en" : "eng";

        _language = language;
        _labels = labels;
        _traffic = traffic;
        _trafficProviderID = trafficProviderID;
        izoom = (int) _zoom;

        OnlineMapsOSMAPIQuery.InitOSMServer(osmServer);

        UpdateCorners();
    }

#if !ONLINEMAPS_NOGUI
    private void OnGUI()
    {
        if (OnGUIBefore != null) OnGUIBefore();
        if (OnGUIAfter != null) OnGUIAfter();
    }
#endif

    /// <summary>
    /// Full redraw map.
    /// </summary>
    public void Redraw()
    {
        needRedraw = true;
        allowRedraw = true;
    }

    /// <summary>
    /// Stops the current process map generation, clears all buffers and completely redraws the map.
    /// </summary>
    public void RedrawImmediately()
    {
        OnlineMapsThreadManager.Dispose();

        if (renderInThread)
        {
            if (_buffer != null)
            {
                _buffer.Dispose();
                _buffer = null;
            }

#if NETFX_CORE
            if (renderThread != null) renderThread.Dispose();
#endif
#if !UNITY_WEBGL
            renderThread = null;
#endif
        }
        else StartBuffer();

        Redraw();
    }

    private OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONObject json = OnlineMapsJSON.Serialize(new {
            longitude,
            latitude,
            floatZoom,
            source,
            mapType,
            labels,
            traffic,
            redrawOnPlay,
            emptyColor,
            defaultTileTexture,
            tooltipBackgroundTexture,
            showMarkerTooltip,
            useSoftwareJPEGDecoder,
            countParentLevels
        }) as OnlineMapsJSONObject;

        if (activeType.isCustom) json.Add("customProviderURL", customProviderURL);

        if (control.resultIsTexture)
        {
            defaultColors = texture.GetPixels();
#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            }
#endif
            json.Add("texture", texture);
        }
        else
        {
            json.AppendObject(new
            {
                width,
                height
            });
        }

        return json;
    }

    /// <summary>
    /// Set the the map coordinate.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public void SetPosition(double lng, double lat, bool ignoreSamePosition = true)
    {
        if (ignoreSamePosition && Math.Abs(latitude - lat) < double.Epsilon && Math.Abs(longitude - lng) < double.Epsilon) return;

        if (width == 0 && height == 0)
        {
            if (control.resultIsTexture && texture != null)
            {
                width = texture.width;
                height = texture.height;
            }
        }
        int countX = width / OnlineMapsUtils.tileSize;
        int countY = height / OnlineMapsUtils.tileSize;

        if (lng < -180) lng += 360;
        else if (lng > 180) lng -= 360;

        if (positionRange != null)
        {
            if (positionRange.type == OnlineMapsPositionRangeType.center) positionRange.CheckAndFix(ref lng, ref lat);
            else if (positionRange.type == OnlineMapsPositionRangeType.border) FixPositionUsingBorders(ref lng, ref lat, countX, countY);
        }

        double tpx, tpy;
        projection.CoordinatesToTile(lng, lat, zoom, out tpx, out tpy);

        float haftCountY = countY / 2f * zoomCoof;
        int maxY = 1 << zoom;
        bool modified = false;
        if (tpy < haftCountY)
        {
            tpy = haftCountY;
            modified = true;
        }
        else if (tpy + haftCountY >= maxY)
        {
            tpy = maxY - haftCountY;
            modified = true;
        }

        if (modified) projection.TileToCoordinates(tpx, tpy, zoom, out lng, out lat);

        if (Math.Abs(latitude - lat) < double.Epsilon && Math.Abs(longitude - lng) < double.Epsilon) return;

        allowRedraw = true;
        needRedraw = true;

        latitude = lat;
        longitude = lng;
        UpdateCorners();

        DispatchEvent(OnlineMapsEvents.changedPosition);
    }

    /// <summary>
    /// Sets the position and zoom.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <param name="ZOOM">Zoom</param>
    public void SetPositionAndZoom(double lng, double lat, float? ZOOM = null)
    {
        if (ZOOM.HasValue) floatZoom = ZOOM.Value;
        SetPosition(lng, lat);
    }

    /// <summary>
    /// Sets the texture, which will draw the map.
    /// Texture displaying on the source you need to change yourself.
    /// </summary>
    /// <param name="newTexture">Texture, where you want to draw the map.</param>
    public void SetTexture(Texture2D newTexture)
    {
        texture = newTexture;
        width = texture.width;
        height = texture.height;

        float z = CheckMapSize(floatZoom);
        if (Math.Abs(floatZoom - z) > float.Epsilon) floatZoom = z;

        control.SetTexture(texture);

        allowRedraw = true;
        needRedraw = true;
    }

    /// <summary>
    /// Sets the position of the center point of the map based on the tile position.
    /// </summary>
    /// <param name="tx">Tile X</param>
    /// <param name="ty">Tile Y</param>
    /// <param name="tileZoom">Tile zoom</param>
    public void SetTilePosition(double tx, double ty, int? tileZoom = null)
    {
        double lng, lat;
        projection.TileToCoordinates(tx, ty, tileZoom != null ? tileZoom.Value : zoom, out lng, out lat);
        SetPosition(lng, lat);
    }

    private void Start()
    {
        if (OnStart != null) OnStart(this);
        if (redrawOnPlay) allowRedraw = true;
        needRedraw = true;
        _zoom = CheckMapSize(_zoom);
    }

    private void StartBuffer()
    {
        if (!allowRedraw || !needRedraw) return;
        if (buffer.status != OnlineMapsBufferStatus.wait) return;

        if (latitude < -90) latitude = -90;
        else if (latitude > 90) latitude = 90;
        while (longitude < -180 || longitude > 180)
        {
            if (longitude < -180) longitude += 360;
            else if (longitude > 180) longitude -= 360;
        }
        
        buffer.status = OnlineMapsBufferStatus.start;

        if (!control.resultIsTexture) renderInThread = false;

#if !UNITY_WEBGL
        if (renderInThread)
        {
            if (renderThread == null)
            {
#if NETFX_CORE
                renderThread = new OnlineMapsThreadWINRT(buffer.GenerateFrontBuffer);
#else
                renderThread = new Thread(buffer.GenerateFrontBuffer);
#endif
                renderThread.Start();
            }
        }
        else buffer.GenerateFrontBuffer();
#else
        buffer.GenerateFrontBuffer();
#endif

        needRedraw = false;
    }

    private void Update()
    {
        OnlineMapsThreadManager.ExecuteMainThreadActions();

        if (OnUpdateBefore != null) OnUpdateBefore();
        
        CheckBaseProps();
        tileManager.StartDownloading();

        if (OnUpdateLate != null) OnUpdateLate();
    }

    /// <summary>
    /// Updates the coordinates of the corners of the map
    /// </summary>
    public void UpdateCorners()
    {
        UpdateTopLeftPosition();
        UpdateBottonRightPosition();

        long max = (1L << izoom) * OnlineMapsUtils.tileSize;
        if (max == width && Mathf.Abs(zoomScale) < float.Epsilon)
        {
            double lng = longitude + 180;
            topLeftLongitude = lng + 0.001;
            if (topLeftLongitude > 180) topLeftLongitude -= 360;

            bottomRightLongitude = lng - 0.001;
            if (bottomRightLongitude > 180) bottomRightLongitude -= 360;
        }
    }

    private void UpdateBottonRightPosition()
    {
        int countX = width / OnlineMapsUtils.tileSize;
        int countY = height / OnlineMapsUtils.tileSize;

        double px, py;
        projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);

        px += countX / 2d * zoomCoof;
        py += countY / 2d * zoomCoof;

        projection.TileToCoordinates(px, py, zoom, out bottomRightLongitude, out bottomRightLatitude);
    }

    private void UpdateTopLeftPosition()
    {
        int countX = width / OnlineMapsUtils.tileSize;
        int countY = height / OnlineMapsUtils.tileSize;

        double px, py;

        projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);

        px -= countX / 2d * zoomCoof;
        py -= countY / 2d * zoomCoof;

        projection.TileToCoordinates(px, py, zoom, out topLeftLongitude, out topLeftLatitude);
    }

#endregion
}