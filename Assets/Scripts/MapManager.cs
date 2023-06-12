using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using DotSpatial.Projections;
using System.Collections.Specialized;
using System.Xml.Linq;
using System.Data.SqlTypes;
using System.Xml;
using System;

public class MapManager : MonoBehaviour
{
    public GameObject pointCloudRoot;

    public Shader encShader;

    public Texture2D encErrorTexture;

    public InputActionProperty adjustMapHeightAction;
    public InputActionProperty changeMapAction;
    public InputActionProperty stickMapAction;

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

    private bool _compatibleEPSG; // compatible with ENC server CRSs
    public bool compatibleEPSG
    {
        get { return _compatibleEPSG; }
    }

    enum MAPTYPE
    {
        NONE,
        ARCGIS,
        GOOGLE,
        ENC
    }

    MAPTYPE currentMap;

    private HashSet<int> validENCEPSGs;
    private string encURL = $"https://gis.charttools.noaa.gov/arcgis/rest/services/MCS/ENCOnline/MapServer/exts/MaritimeChartService/WMSServer?";

    private bool validEPSG;
    private bool mapStuck;

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

    /// <summary>
    /// Empty tile color
    /// </summary>
    private Color emptyColorArcGIS = new Color32(203, 203, 203, 255);

    // Start is called before the first frame update
    void Start()
    {
        adjustMapHeightAction.action.started += ctx => BeginAdjustMapHeight();
        adjustMapHeightAction.action.canceled += ctx => EndAdjustMapHeight();

        changeMapAction.action.started += ctx => BeginChangeMap();
        changeMapAction.action.canceled += ctx => EndChangeMap();

        stickMapAction.action.started += ctx => BeginStickMap();
        stickMapAction.action.canceled += ctx => EndStickMap();

        encResolution = UserSettings.instance.preferences.encResolution;

        _changingMap = false;
        _adjustingHeight = false;
        _refreshing = false;
        _loaded = false;
        _encError = false;
        _compatibleEPSG = false;

        currentMap = MAPTYPE.NONE;

        validEPSG = false;
        mapStuck = false;
        satMapInitialRefresh = false;

        // Intercepts requests to the download of the tile.
        OnlineMapsTileManager.OnStartDownloadTile += OnStartDownloadTile;

        // Subscribe to the event of success download tile.
        OnlineMapsTile.OnTileDownloaded += OnTileDownloaded;

        // Subscribe to tile loaded event
        //OnlineMapsTileManager.OnTileLoaded += OnTileLoaded;

        OnlineMaps.instance.countParentLevels = 1;
        OnlineMaps.instance.width = 4096;
        OnlineMaps.instance.height = 4096;
        SatMap.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!UserSettings.instance.preferences.enableMaps || !validEPSG)
            return;

        if (_loaded && PointCloudManager.GetPointCloudsInScene().Length > 0)
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
        if (!validEPSG)
            return;

        adjustMapHeightAction.action.Enable();
        changeMapAction.action.Enable();
        stickMapAction.action.Enable();
    }

    void OnDisable()
    {
        adjustMapHeightAction.action.Disable();
        changeMapAction.action.Disable();
        stickMapAction.action.Disable();
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

        if (UserSettings.instance.preferences.stickyMaps && !mapStuck)
            currentMap = MAPTYPE.NONE;
    }

    private void BeginStickMap()
    {
        if (UserSettings.instance.preferences.stickyMaps && !mapStuck)
            mapStuck = true;        
        else
            mapStuck = false;
    }

    private void EndStickMap()
    {
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

    public void CreateMaps(GEOReference geoRef, PointCloud pc)
    {
        try
        {
            ProjectionInfo.FromEpsgCode(pc.EPSG);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            Debug.Log($"EPSG Code {pc.EPSG} is not valid: {e}");
            pc.validEPSG = false;
            validEPSG = false;
            return;
        }

        pc.validEPSG = true;
        validEPSG = true;

        OnEnable();

        CreateSatelliteMap(geoRef, pc);
        StartCoroutine(CreateENC(geoRef, pc));
    }

    public IEnumerator CreateENC(GEOReference geoRef, PointCloud pc, bool forceRefresh = false)
    {
        if (validENCEPSGs == null)
        {
            GetENCCompatibleEPSGs();
        }

        if (!validENCEPSGs.Contains(pc.EPSG))
        {
            _compatibleEPSG = false;
            yield return null;
        }

        _compatibleEPSG = true;

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
        float squareScaleFactor = Mathf.Max(pc.bounds.extents.x, pc.bounds.extents.z);
        ENC.transform.localScale = new Vector3(squareScaleFactor * 2f, squareScaleFactor * 2f, 1f);
        // CHANGED FOR NOAA DEMO -- FIX ME BACK!
        //float maxCenter = Mathf.Max(pc.bounds.center.x, pc.bounds.center.z);
        //ENC.transform.localPosition = new Vector3(maxCenter, pc.groundLevel, maxCenter);
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

    public IEnumerator GetENCTexture(GEOReference geoRef, PointCloud pc, Material encMat)
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
            // Calculate a square ENC bound centered on the dataset
            float maxXZsize = Mathf.Max(pc.bounds.extents.x, pc.bounds.extents.z);
            double minBBx = geoRef.refX + pc.bounds.center.x - maxXZsize;
            double maxBBx = minBBx + maxXZsize * 2f;
            double minBBy = geoRef.refY + pc.bounds.center.z - maxXZsize;
            double maxBBy = minBBy + maxXZsize * 2f;

            int epsg = pc.EPSG;

            if (epsg == 6344) { epsg = 26915; }

            NameValueCollection encQueryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

            encQueryString.Add("SERVICE", "WMS");
            encQueryString.Add("REQUEST", "GetMap");
            encQueryString.Add("FORMAT", "image/png");
            encQueryString.Add("LAYERS", "0,1,2,3,4,5,6,7");
            encQueryString.Add("CRS", $"EPSG:{epsg}");
            encQueryString.Add("WIDTH", encResolution.ToString());
            encQueryString.Add("HEIGHT", encResolution.ToString());
            encQueryString.Add("BBOX", $"{minBBx},{minBBy},{maxBBx},{maxBBy}");

            Debug.Log(encURL + encQueryString.ToString());
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(encURL + encQueryString.ToString());

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


    public void CreateSatelliteMap(GEOReference geoRef, PointCloud pc)
    {
        satMapInitialRefresh = false;

        var wasActive = SatMap.activeSelf;

        SatMap.SetActive(true);

        int epsg = pc.EPSG;

        // Calculate a square ENC bound centered on the dataset
        float maxXZsize = Mathf.Max(pc.bounds.extents.x, pc.bounds.extents.z);
        double minBBx = geoRef.refX + pc.bounds.center.x - maxXZsize;
        double maxBBx = minBBx + maxXZsize * 2f;
        double minBBy = geoRef.refY + pc.bounds.center.z - maxXZsize;
        double maxBBy = minBBy + maxXZsize * 2f;

        double[] points = new double[6];
        // center coord
        points[0] = minBBx + (maxBBx - minBBx) / 2.0;
        points[1] = minBBy + (maxBBy - minBBy) / 2.0;
        // bb min coord
        points[2] = minBBx;
        points[3] = minBBy;
        // bb max coord
        points[4] = maxBBx;
        points[5] = maxBBy;

        // heights don't matter
        double[] elevs = { 0, 0, 0 };

        // reproject the 3 coords to GPS coords (WGS84) for OnlineMaps
        ProjectionInfo src = ProjectionInfo.FromEpsgCode(epsg);

        ProjectionInfo dest = ProjectionInfo.FromEpsgCode(4326);

        Reproject.ReprojectPoints(points, elevs, src, dest, 0, 3);

        // set markers representing the bounding box edges
        OnlineMapsMarker minMark = new();
        minMark.SetPosition(points[2], points[3]);

        OnlineMapsMarker maxMark = new();
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
                OnlineMaps.instance.bottomRightPosition) * 1000f;

        // now increase zoom and redraw to get the more zoomed-in level's geographic distance
        OnlineMaps.instance.zoom = zoom + 1;

        OnlineMaps.instance.Redraw();

        Vector2 distance2 = OnlineMapsUtils.DistanceBetweenPoints(OnlineMaps.instance.topLeftPosition,
                OnlineMaps.instance.bottomRightPosition) * 1000f;

        // find the difference in coverage between the two zoom levels
        float zoomRange = (distance1 - distance2).magnitude;

        Vector2 bbRange = new((float)(maxBBx - minBBx), (float)(maxBBy - minBBy));

        // now calc zoom fraction for bound box size
        float ratio = (bbRange.magnitude - distance2.magnitude) / zoomRange;

        //Debug.Log("bbRange: " + bbRange.magnitude);
        //Debug.Log("Zoom " + (zoom) + ": " + distance1.magnitude);
        //Debug.Log("Zoom " + (zoom + 1) + ": " + distance2.magnitude);
        //Debug.Log("Zoom Range: " + zoomRange);
        //Debug.Log("Ratio: " + ratio);

        OnlineMaps.instance.floatZoom = (zoom + 1f) - ratio;

        OnlineMapsControlBaseDynamicMesh.instance.sizeInScene = bbRange;

        // OnlineMaps position is based off the corner of the map, not the center, need offset to align
        float maxBBdim = Mathf.Max(bbRange.x, bbRange.y);
        Vector3 mapAdjustment = new(-(maxBBdim / 2f), 0f, (maxBBdim / 2f));

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
    /// This method is called when tile is successfully downloaded.
    /// </summary>
    /// <param name="tile">Reference to tile.</param>
    private void OnTileDownloaded(OnlineMapsTile tile)
    {

        if ((tile as OnlineMapsRasterTile).mapType.provider.id == "arcgis" && ArcGISTileEmpty(tile))
        {
            tile.status = OnlineMapsTileStatus.error;
            return;
        }

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

    /// <summary>
    /// This method is called when tile is success downloaded.
    /// </summary>
    /// <param name="tile">Reference to tile.</param>
    private bool ArcGISTileEmpty(OnlineMapsTile tile)
    {
        // Get pixels from texture corners
        Texture2D texture = tile.texture;
        Color c1 = texture.GetPixel(1, 1);
        Color c2 = texture.GetPixel(254, 254);

        // If both colors are empty, the tile is empty
        if (c1 == emptyColorArcGIS && c2 == emptyColorArcGIS)
        {
            return true;
        }

        return false;
    }

    void GetENCCompatibleEPSGs()
    {
        validENCEPSGs = new();

        XmlDocument xmlDoc = new();
        xmlDoc.Load(encURL + "REQUEST=GetCapabilities&SERVICE=WMS");

        var nodes = xmlDoc.GetElementsByTagName("CRS");

        foreach ( XmlNode node in nodes )
        {
            if (node.ParentNode.ParentNode.Name == "Capability" && node.InnerText.Substring(0, node.InnerText.IndexOf(':')) == "EPSG")
            {
                int epsg = int.Parse(node.InnerText.Substring(node.InnerText.LastIndexOf(':') + 1));
                validENCEPSGs.Add(epsg);
            }
        }
    }    
}
