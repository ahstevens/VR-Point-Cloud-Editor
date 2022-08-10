using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using DotSpatial.Projections;

public class MapManager : MonoBehaviour
{
    public GameObject pointCloudRoot;

    public Shader encShader;

    public Texture2D encErrorTexture;

    public InputActionProperty adjustMapHeightAction;
    public InputActionProperty changeMapAction;

    private GameObject ENC;
    private int encResolution;
    private string encFileLocation;

    [SerializeField]
    private GameObject SatMap;

    private bool _changingMap;
    public bool changingMap
    {
        get { return _changingMap; }
    }

    private bool _adjustingHeight;
    public bool adjustingHeight
    {
        get { return _adjustingHeight; }
    }

    private bool _refreshing;
    public bool refreshing
    {
        get { return _refreshing; }
    }

    private bool _loaded;
    public bool loaded
    {
        get { return _loaded; }
    }

    private bool _encError;
    public bool encError
    {
        get { return _encError; }
    }

    enum MAPTYPE
    {
        NONE,
        ARCGIS,
        GOOGLE,
        ENC
    }

    MAPTYPE currentMap;

    bool satMapInitialRefresh;

    float deadzone = 0.2f;

    enum SECTOR
    {
        NONE,
        NORTH,
        EAST,
        SOUTH,
        WEST
    }

    // Start is called before the first frame update
    void Start()
    {
        adjustMapHeightAction.action.started += ctx => BeginAdjustMapHeight();
        adjustMapHeightAction.action.canceled += ctx => EndAdjustMapHeight();

        changeMapAction.action.started += ctx => BeginChangeMap();
        changeMapAction.action.canceled += ctx => EndChangeMap();

        encResolution = UserSettings.instance.GetPreferences().encResolution;

        _changingMap = false;
        _adjustingHeight = false;
        _refreshing = false;
        _loaded = false;
        _encError = false;

        currentMap = MAPTYPE.NONE;

        satMapInitialRefresh = false;

        // Subscribe to the event of success download tile.
        OnlineMapsTile.OnTileDownloaded += OnTileDownloaded;

        // Intercepts requests to the download of the tile.
        OnlineMapsTileManager.OnStartDownloadTile += OnStartDownloadTile;
    }

    // Update is called once per frame
    void Update()
    {
        if (_loaded && pointCloudManager.getPointCloudsInScene().Length > 0)
        {
            if (_changingMap)
            {
                switch(GetCurrentSector(changeMapAction.action.ReadValue<Vector2>()))
                {
                    case SECTOR.NORTH:
                        currentMap = MAPTYPE.NONE;
                        break;
                    case SECTOR.EAST:
                        currentMap = MAPTYPE.ARCGIS;
                        break;
                    case SECTOR.SOUTH:
                        currentMap = MAPTYPE.ENC;
                        break;
                    case SECTOR.WEST:
                        currentMap = MAPTYPE.GOOGLE;
                        break;
                    case SECTOR.NONE:
                        //currentMap = MAPTYPE.NONE;
                        break;
                }
            }

            switch (currentMap)
            {
                case MAPTYPE.NONE:
                    SatMap.SetActive(false);
                    if (ENC != null)
                        ENC.SetActive(false);
                    break;
                case MAPTYPE.ARCGIS:
                    SatMap.SetActive(true);
                    OnlineMaps.instance.countParentLevels = 1;
                    OnlineMaps.instance.mapType = "arcgis.worldimagery";
                    if (ENC != null)
                        ENC.SetActive(false);
                    break;
                case MAPTYPE.GOOGLE:
                    SatMap.SetActive(true);
                    OnlineMaps.instance.countParentLevels = 1;
                    OnlineMaps.instance.mapType = "google.satellite";
                    if (ENC != null)
                        ENC.SetActive(false);
                    break;
                case MAPTYPE.ENC:
                    SatMap.SetActive(false);
                    if (ENC != null)
                        ENC.SetActive(true);
                    break;
            }

            if (_adjustingHeight)
            {
                SetHeight(pointCloudRoot.transform.InverseTransformPoint(GameObject.Find("RightHand Controller").transform.position).y);
            }

            // Need to redraw the map after everything is loaded
            if (SatMap.activeSelf && !satMapInitialRefresh)
            {
                OnlineMaps.instance.Redraw();
                satMapInitialRefresh = true;
            }
        }
        else
        {
            SatMap.SetActive(false);

            if (ENC != null)
                ENC.SetActive(false);
        }
    }

    void OnEnable()
    {
        adjustMapHeightAction.action.Enable();
        changeMapAction.action.Enable();
    }

    void OnDisable()
    {
        adjustMapHeightAction.action.Disable();
        changeMapAction.action.Disable();
    }

    private void BeginAdjustMapHeight()
    {
        if (!_loaded || currentMap == MAPTYPE.NONE)
            return;

        _adjustingHeight = true;
    }

    private void EndAdjustMapHeight()
    {
        _adjustingHeight = false;
    }

    private void BeginChangeMap()
    {
        if (!_loaded)
            return;

        _changingMap = true;
    }

    private void EndChangeMap()
    {
        _changingMap = false;

    }

    public void SetHeight(float height)
    {
        ENC.transform.localPosition = new Vector3(ENC.transform.localPosition.x, height, ENC.transform.localPosition.z);

        SatMap.transform.localPosition = new Vector3(
            SatMap.transform.localPosition.x,
            height,
            SatMap.transform.localPosition.z
        );
    }

    public void CreateMaps(GEOReference geoRef, pointCloud pc)
    {
        CreateSatelliteMap(geoRef, pc);
        StartCoroutine(CreateENC(geoRef, pc));
    }

    public IEnumerator CreateENC(GEOReference geoRef, pointCloud pc, bool forceRefresh = false)
    {
        _refreshing = true;

        encFileLocation = Application.dataPath + "/Maps/ENCs" + "/ENC_" + pc.name + ".png";

        DestroyENC(forceRefresh);

        var encMat = new Material(encShader);

        Debug.Log("Processing ENC image... ");
        float tick = Time.realtimeSinceStartup;
        yield return GetENCTexture(geoRef, pc, encMat);
        Debug.Log("ENC image processed in " + (Time.realtimeSinceStartup - tick) + " seconds");

        ENC = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ENC.name = "ENC_" + pc.name;

        ENC.transform.parent = pc.transform;
        ENC.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        ENC.transform.localScale = new Vector3(pc.bounds.extents.x * 2f, pc.bounds.extents.z * 2f, 1f);
        ENC.transform.localPosition = new Vector3(pc.bounds.center.x, pc.groundLevel, pc.bounds.center.z);

        Renderer rend = ENC.GetComponent<Renderer>();
        rend.material = encMat;

        ENC.SetActive(false);

        _refreshing = false;
        _loaded = true;
    }

    private void DestroyENC(bool deleteCached = false)
    {
        if (ENC != null)
        {
            Destroy(ENC);

            if (deleteCached && System.IO.File.Exists(encFileLocation))
            {
                System.IO.File.Delete(encFileLocation);
            }

            _loaded = false;
        }
    }

    public IEnumerator GetENCTexture(GEOReference geoRef, pointCloud pc, Material encMat)
    {
        if (System.IO.File.Exists(encFileLocation))
        {
            Debug.Log("Found existing ENC file " + encFileLocation);
            Texture2D tex = new(2, 2);
            tex.LoadImage(System.IO.File.ReadAllBytes(encFileLocation));
            encMat.mainTexture = tex;
        }
        else
        {
            double minBBx = geoRef.realWorldX + pc.bounds.min.x;
            double maxBBx = geoRef.realWorldX + pc.bounds.max.x;
            double minBBz = geoRef.realWorldZ + pc.bounds.min.z;
            double maxBBz = geoRef.realWorldZ + pc.bounds.max.z;

            int epsg = pc.EPSG;

            // NAD83 (2011) / UTM15N || null || 
            if (epsg == 6344 || epsg == 0 || epsg == 32767)
                epsg = 26915; // NAD83 / UTM15N

            string url = $"https://gis.charttools.noaa.gov/arcgis/rest/services/MCS/ENCOnline/MapServer/exts/MaritimeChartService/WMSServer?LAYERS=0,1,2,3,4,5,6,7&FORMAT=image%2Fpng&CRS=EPSG:{epsg}&SERVICE=WMS&REQUEST=GetMap&WIDTH={encResolution}&HEIGHT={encResolution}&BBOX={minBBx},{minBBz},{maxBBx},{maxBBz}";

            Debug.Log(url);
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

            Debug.Log("Downloading ENC image... ");
            float tick = Time.realtimeSinceStartup;
            www.SendWebRequest();

            bool doneDownloading = false;
            while (!www.isDone)
            {
                if (www.downloadProgress > 0 && !doneDownloading)
                    Debug.Log("Downloading: " + www.downloadProgress * 100 + "%");

                if (www.downloadProgress >= 1f)
                    doneDownloading = true;

                yield return null;
            }

            Debug.Log("ENC image downloaded in " + (Time.realtimeSinceStartup - tick) + " seconds");

            bool success = www.result == UnityWebRequest.Result.Success;

            if (success)
            {
                Debug.Log("Converting image to Texture2D... ");
                tick = Time.realtimeSinceStartup;
                Texture2D encTex = ((DownloadHandlerTexture)www.downloadHandler).texture;
                encMat.mainTexture = encTex;
                Debug.Log("Image converted to Texture2D in " + (Time.realtimeSinceStartup - tick) + " seconds");

                Debug.Log("Saving texture to disk... ");
                tick = Time.realtimeSinceStartup;
                yield return SaveENCTexture(encTex, pc.name);
                Debug.Log("Texture saved in " + (Time.realtimeSinceStartup - tick) + " seconds");

                _encError = false;
            }
            else
            {
                Debug.Log(www.error);

                encMat.mainTexture = encErrorTexture;

                _encError = true;
            }
        }
    }

    IEnumerator SaveENCTexture(Texture2D texture, string name)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/Maps/ENCs";
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        //System.IO.File.WriteAllBytes(dirPath + "/ENC_" + name + ".png", bytes);
        yield return System.IO.File.WriteAllBytesAsync(encFileLocation, bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + encFileLocation);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }


    public void CreateSatelliteMap(GEOReference geoRef, pointCloud pc)
    {
        satMapInitialRefresh = false;

        var wasActive = SatMap.activeSelf;

        SatMap.SetActive(true);

        int epsg = pc.EPSG;

        // NAD83 (2011) / UTM15N || null || 
        if (epsg == 6344 || epsg == 0 || epsg == 32767)
            epsg = 26915; // NAD83 / UTM15N


        double minBBx = geoRef.realWorldX + pc.bounds.min.x;
        double maxBBx = geoRef.realWorldX + pc.bounds.max.x;
        double minBBz = geoRef.realWorldZ + pc.bounds.min.z;
        double maxBBz = geoRef.realWorldZ + pc.bounds.max.z;

        Vector2 bbRange = new((float)(maxBBx - minBBx), (float)(maxBBz - minBBz));

        ProjectionInfo src = ProjectionInfo.FromEpsgCode(epsg);
        ProjectionInfo dest = ProjectionInfo.FromEpsgCode(4326);

        double[] points = new double[6];
        // center coord
        points[0] = minBBx + (maxBBx - minBBx) / 2.0;
        points[1] = minBBz + (maxBBz - minBBz) / 2.0;
        // bb min coord
        points[2] = minBBx;
        points[3] = minBBz;
        // bb max coord
        points[4] = maxBBx;
        points[5] = maxBBz;

        // heights don't matter
        double[] elevs = { 0, 0, 0 };

        // reproject the 3 coords to GPS coords (WGS84) for OnlineMaps
        Reproject.ReprojectPoints(points, elevs, src, dest, 0, 3);

        // set markers representing the bounding box edges
        OnlineMapsMarker minMark = new OnlineMapsMarker();
        minMark.SetPosition(points[2], points[3]);

        OnlineMapsMarker maxMark = new OnlineMapsMarker();
        maxMark.SetPosition(points[4], points[5]);

        OnlineMapsMarkerBase[] bbox = { minMark, maxMark };

        // request the zoom level that contains the markers (don't care about what it thinks the center should be)
        OnlineMapsUtils.GetCenterPointAndZoom(bbox, out _, out int zoom);

        // the returned zoom level from the previous function is one level lower than it needs to be (not sure why)
        zoom++;

        // set map coordinate, zoom level, and redraw map
        OnlineMaps.instance.SetPositionAndZoom(points[0], points[1], zoom);

        OnlineMaps.instance.Redraw();

        // get the geographical distance of the more zoomed-out level's corners
        Vector2 distance1 = OnlineMapsUtils.DistanceBetweenPoints(OnlineMaps.instance.topLeftPosition,
                OnlineMaps.instance.bottomRightPosition) * 1000;

        // now increase zoom and redraw to get the more zoomed-in level's geographic distance
        OnlineMaps.instance.zoom = zoom + 1;

        OnlineMaps.instance.Redraw();

        Vector2 distance2 = OnlineMapsUtils.DistanceBetweenPoints(OnlineMaps.instance.topLeftPosition,
                OnlineMaps.instance.bottomRightPosition) * 1000;

        // find the difference in coverage between the two zoom levels
        float zoomRange = (distance1 - distance2).magnitude;

        // now calc zoom fraction for bound box size
        float ratio = (bbRange.magnitude - distance2.magnitude) / zoomRange;

        //Debug.Log("bbRange: " + bbRange.magnitude);
        //Debug.Log("Zoom " + (zoom) + ": " + distance1.magnitude);
        //Debug.Log("Zoom " + (zoom + 1) + ": " + distance2.magnitude);
        //Debug.Log("Zoom Range: " + zoomRange);
        //Debug.Log("Ratio: " + ratio);

        OnlineMaps.instance.floatZoom = (float)zoom + (1f - ratio);

        OnlineMapsControlBaseDynamicMesh.instance.sizeInScene = bbRange;

        // OnlineMaps position is based off the corner of the map, not the center, need offset to align
        Vector3 mapAdjustment = new(-(bbRange.x / 2), 0, (bbRange.y / 2));

        var newPos = new Vector3(pc.bounds.center.x, pc.groundLevel, pc.bounds.center.z) + mapAdjustment;

        SatMap.transform.localPosition = newPos;

        SatMap.SetActive(wasActive);
    }

    /// <summary>
    /// Gets the local path for tile.
    /// </summary>
    /// <param name="tile">Reference to tile</param>
    /// <returns>Local path for tile</returns>
    private static string GetTilePath(OnlineMapsTile tile)
    {
        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;
        string[] parts =
        {
                Application.dataPath,
                "Maps",
                "OnlineMapsTileCache",
                rTile.mapType.provider.id,
                rTile.mapType.id,
                tile.zoom.ToString(),
                tile.x.ToString(),
                tile.y + ".png"
            };
        return string.Join("/", parts);
    }

    /// <summary>
    /// This method is called when loading the tile.
    /// </summary>
    /// <param name="tile">Reference to tile</param>
    private void OnStartDownloadTile(OnlineMapsTile tile)
    {
        // Get local path.
        string path = GetTilePath(tile);

        // If the tile is cached.
        if (System.IO.File.Exists(path))
        {
            // Load tile texture from cache.
            Texture2D tileTexture = new Texture2D(256, 256, TextureFormat.RGB24, false);
            tileTexture.LoadImage(System.IO.File.ReadAllBytes(path));
            tileTexture.wrapMode = TextureWrapMode.Clamp;

            // Send texture to map.
            if (OnlineMapsControlBase.instance.resultIsTexture)
            {
                (tile as OnlineMapsRasterTile).ApplyTexture(tileTexture);
                OnlineMaps.instance.buffer.ApplyTile(tile);
                OnlineMapsUtils.Destroy(tileTexture);
            }
            else
            {
                tile.texture = tileTexture;
                tile.status = OnlineMapsTileStatus.loaded;
            }

            // Redraw map.
            OnlineMaps.instance.Redraw();
        }
        else
        {
            // If the tile is not cached, download tile with a standard loader.
            OnlineMapsTileManager.StartDownloadTile(tile);
        }
    }

    /// <summary>
    /// This method is called when tile is success downloaded.
    /// </summary>
    /// <param name="tile">Reference to tile.</param>
    private void OnTileDownloaded(OnlineMapsTile tile)
    {
        // Get local path.
        string path = GetTilePath(tile);

        // Cache tile.
        System.IO.FileInfo fileInfo = new System.IO.FileInfo(path);
        System.IO.DirectoryInfo directory = fileInfo.Directory;
        if (!directory.Exists) directory.Create();

        System.IO.File.WriteAllBytes(path, tile.www.bytes);
    }

    private SECTOR GetCurrentSector(Vector2 sample)
    {
        if (Mathf.Abs(sample.x) < deadzone && Mathf.Abs(sample.y) < deadzone)
            return SECTOR.NONE;

        var angle = Mathf.Atan2(sample.y, sample.x) * Mathf.Rad2Deg;
        var absAngle = Mathf.Abs(angle);
        if (absAngle < 45f)
            return SECTOR.EAST;
        if (absAngle > 135f)
            return SECTOR.WEST;
        return angle >= 0f ? SECTOR.NORTH : SECTOR.SOUTH;
    }
}
