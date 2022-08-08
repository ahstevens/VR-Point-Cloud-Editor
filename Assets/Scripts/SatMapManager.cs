using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DotSpatial.Projections;

public class SatMapManager : MonoBehaviour
{
    [SerializeField]
    GameObject map;

    private bool refreshed;

    private float elapsedTime = 0f;
    private bool flip = false;

    // Start is called before the first frame update
    void Start()
    {
        HideMap();
        refreshed = false;

        // Subscribe to the event of success download tile.
        OnlineMapsTile.OnTileDownloaded += OnTileDownloaded;

        // Intercepts requests to the download of the tile.
        OnlineMapsTileManager.OnStartDownloadTile += OnStartDownloadTile;
    }

    // Update is called once per frame
    void Update()
    {
        if (map.activeSelf)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= 5f)
            {
                elapsedTime = 0f;

                if (flip)
                    OnlineMaps.instance.mapType = "arcgis.worldimagery";
                else
                    OnlineMaps.instance.mapType = "google.satellite";

                flip = !flip;
            }

            // Need to redraw the map after everything is loaded
            if (!refreshed && FindObjectOfType<ENCManager>().loaded)
            {
                OnlineMaps.instance.Redraw();
                refreshed = true;
            }
        }
    }

    public void ShowMap()
    {
        map.SetActive(true);
    }

    public void HideMap()
    {
        map.SetActive(false);
    }

    public void SetHeight(float height)
    {
        map.transform.localPosition = new Vector3(
            map.transform.localPosition.x,
            height,
            map.transform.localPosition.z
        );
    }

    public void CreateSatelliteMap(GEOReference geoRef, pointCloud pc)
    {
        refreshed = false;

        ShowMap();

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

        OnlineMaps.instance.gameObject.transform.localPosition = newPos;
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
}
