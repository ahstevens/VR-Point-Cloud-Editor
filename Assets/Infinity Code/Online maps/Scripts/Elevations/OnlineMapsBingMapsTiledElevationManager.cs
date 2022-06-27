/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Implements the use of elevation data from Bing Maps base on tiles
/// </summary>
[OnlineMapsPlugin("Bing Maps Tiled Elevations", typeof(OnlineMapsControlBaseDynamicMesh), "Elevations")]
[AddComponentMenu("Infinity Code/Online Maps/Elevations/Bing Maps Tiled")]
public class OnlineMapsBingMapsTiledElevationManager : OnlineMapsTiledElevationManager<OnlineMapsBingMapsTiledElevationManager>
{
    public override void CancelCurrentElevationRequest()
    {
        
    }

    protected override int tileWidth
    {
        get { return 32; }
    }

    protected override int tileHeight
    {
        get { return 32; }
    }

    protected override string cachePrefix
    {
        get { return "bing_elevation_"; }
    }

    private void OnTileDownloaded(Tile tile, OnlineMapsBingMapsElevation request)
    {
        if (request.status == OnlineMapsQueryStatus.error)
        {
            Debug.Log("Download error");
            if (OnElevationFails != null) OnElevationFails(request.response);
            return;
        }

        if (OnDownloadSuccess != null) OnDownloadSuccess(tile, request.GetWWW());

        short[,] elevations = new short[32, 32];
        Array ed = elevations;
        if (OnlineMapsBingMapsElevation.ParseElevationArray(request.response, OnlineMapsBingMapsElevation.Output.json, ref ed))
        {
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    short t = elevations[x, y];
                    elevations[x, y] = elevations[x, 31 - y];
                    elevations[x, 31 - y] = t;
                }
            }

            SetElevationToCache(tile, elevations);
            SetElevationData(tile, elevations);
        }

        if (OnElevationUpdated != null) OnElevationUpdated();
    }

    public override void StartDownloadElevationTile(Tile tile)
    {
        double lx, ty, rx, by;
        map.projection.TileToCoordinates(tile.x, tile.y, tile.zoom, out lx, out ty);
        map.projection.TileToCoordinates(tile.x + 1, tile.y + 1, tile.zoom, out rx, out @by);
        OnlineMapsBingMapsElevation request = OnlineMapsBingMapsElevation.GetElevationByBounds(OnlineMapsKeyManager.BingMaps(), lx, ty, rx, @by, tileWidth, tileHeight);
        request.OnFinish += r => OnTileDownloaded(tile, request);

        if (OnElevationRequested != null) OnElevationRequested();
    }
}