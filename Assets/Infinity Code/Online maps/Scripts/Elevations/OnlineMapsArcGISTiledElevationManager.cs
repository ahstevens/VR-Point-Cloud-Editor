/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Implements the use of elevation data from ArcGIS base on tiles
/// </summary>
[OnlineMapsPlugin("ArcGIS Tiled Elevations", typeof(OnlineMapsControlBaseDynamicMesh), "Elevations")]
[AddComponentMenu("Infinity Code/Online Maps/Elevations/ArcGIS Tiled")]
public class OnlineMapsArcGISTiledElevationManager : OnlineMapsTiledElevationManager<OnlineMapsArcGISTiledElevationManager>
{
    /// <summary>
    /// Resolution of request
    /// </summary>
    public int resolution = 32;

    protected override string cachePrefix
    {
        get { return "arcgis_elevation_"; }
    }

    protected override int tileWidth
    {
        get { return resolution; }
    }

    protected override int tileHeight
    {
        get { return resolution; }
    }

    public override void CancelCurrentElevationRequest()
    {

    }

    private void OnTileDownloaded(Tile tile, OnlineMapsWWW www)
    {
        if (www.hasError)
        {
            Debug.Log("Download error");
            if (OnElevationFails != null) OnElevationFails(www.error);
            return;
        }

        if (OnDownloadSuccess != null) OnDownloadSuccess(tile, www);
        string response = www.text;

        OnlineMapsCache.Add(tile.GetCacheKey(cachePrefix), Encoding.UTF8.GetBytes(response));
        ParseResponse(tile, response);

        if (OnElevationUpdated != null) OnElevationUpdated();
    }

    private void ParseResponse(Tile tile, string response)
    {
        short[,] elevations = new short[tileWidth, tileHeight];
        try
        {
            int dataIndex = response.IndexOf("\"data\":[");
            if (dataIndex == -1)
            {
                Debug.LogWarning(response);
                if (OnElevationFails != null) OnElevationFails(response);

                return;
            }

            dataIndex += 8;

            int index = 0;
            int v = 0;
            bool isNegative = false;

            for (int i = dataIndex; i < response.Length; i++)
            {
                char c = response[i];
                if (c == ',')
                {
                    int x = index % resolution;
                    int y = index / resolution;
                    if (isNegative) v = -v;
                    elevations[x, y] = (short) v;
                    v = 0;
                    isNegative = false;
                    index++;
                }
                else if (c == '-') isNegative = true;
                else if (c > 47 && c < 58) v = v * 10 + (c - 48);
                else break;
            }

            if (isNegative) v = -v;
            elevations[resolution - 1, resolution - 1] = (short) v;

            SetElevationToCache(tile, elevations);
            SetElevationData(tile, elevations);
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + exception.StackTrace);
            if (OnElevationFails != null) OnElevationFails(exception.Message);
        }
    }

    public override void StartDownloadElevationTile(Tile tile)
    {
        double lx, ty, rx, by;
        map.projection.TileToCoordinates(tile.x, tile.y, tile.zoom, out lx, out ty);
        map.projection.TileToCoordinates(tile.x + 1, tile.y + 1, tile.zoom, out rx, out @by);
        string url = "https://sampleserver4.arcgisonline.com/ArcGIS/rest/services/Elevation/ESRI_Elevation_World/MapServer/exts/ElevationsSOE/ElevationLayers/1/GetElevationData?f=json&Rows=" + resolution + "&Columns=" + resolution + "&Extent=";
        url += OnlineMapsWWW.EscapeURL("{\"spatialReference\":{\"wkid\":4326},\"ymin\":" + @by.ToString(OnlineMapsUtils.numberFormat) +
                                       ",\"ymax\":" + ty.ToString(OnlineMapsUtils.numberFormat) +
                                       ",\"xmin\":" + lx.ToString(OnlineMapsUtils.numberFormat) +
                                       ",\"xmax\":" + rx.ToString(OnlineMapsUtils.numberFormat) + "}");
        OnlineMapsWWW request = new OnlineMapsWWW(url);
        request.OnComplete += www => OnTileDownloaded(tile, www);

        if (OnElevationRequested != null) OnElevationRequested();
    }
}