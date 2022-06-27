/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements elevation managers, which loads elevation data by tiles
/// </summary>
/// <typeparam name="T">Type of elevation manager</typeparam>
public abstract class OnlineMapsTiledElevationManager<T> : OnlineMapsElevationManager<T>
    where T : OnlineMapsTiledElevationManager<T>
{
    public Action<OnlineMapsTiledElevationManager<T>> OnAllTilesLoaded;

    /// <summary>
    /// Called when data download starts.
    /// </summary>
    public Action<Tile> OnDownload;

    /// <summary>
    /// Called when data is successfully downloaded.
    /// </summary>
    public Action<Tile, OnlineMapsWWW> OnDownloadSuccess;

    /// <summary>
    /// Offset of tile zoom from map zoom
    /// </summary>
    public int zoomOffset = 3;

    /// <summary>
    /// Cache elevations?
    /// </summary>
    public bool cacheElevations = true;

    protected Dictionary<ulong, Tile> tiles;
    protected bool needUpdateMinMax = true;
    private bool needUpdateTiles = true;

    private int prevTileX;
    private int prevTileY;

    protected abstract int tileWidth { get; }
    protected abstract int tileHeight { get; }
    protected abstract string cachePrefix { get; }

    public override bool hasData
    {
        get { return true; }
    }

    protected void CheckAllTilesLoaded()
    {
        foreach (var pair in tiles)
        {
            if (!pair.Value.loaded) return;
        }

        if (OnAllTilesLoaded != null) OnAllTilesLoaded(this);
    }

    public override float GetElevationValue(double x, double z, float yScale, double tlx, double tly, double brx, double bry)
    {
        float v = GetUnscaledElevationValue(x, z, tlx, tly, brx, bry);

        if (bottomMode == OnlineMapsElevationBottomMode.minValue) v -= minValue;
        return v * yScale * scale;
    }

    public override float GetUnscaledElevationValue(double x, double z, double tlx, double tly, double brx, double bry)
    {
        if (tiles == null)
        {
            tiles = new Dictionary<ulong, Tile>();
            return 0;
        }
        x = x / -sizeInScene.x;
        z = z / sizeInScene.y;

        double ttlx, ttly, tbrx, tbry;

        OnlineMaps m = map;
        OnlineMapsProjection projection = m.projection;

        projection.CoordinatesToTile(tlx, tly, m.zoom, out ttlx, out ttly);
        projection.CoordinatesToTile(brx, bry, m.zoom, out tbrx, out tbry);

        if (tbrx < ttlx) tbrx += 1 << m.zoom;

        double cx = (tbrx - ttlx) * x + ttlx;
        double cz = (tbry - ttly) * z + ttly;

        int zoom = m.zoom - zoomOffset;
        double tx, ty;
        projection.TileToCoordinates(cx, cz, m.zoom, out cx, out cz);
        projection.CoordinatesToTile(cx, cz, zoom, out tx, out ty);
        int ix = (int)tx;
        int iy = (int)ty;

        ulong key = OnlineMapsTileManager.GetTileKey(zoom, ix, iy);
        Tile tile;
        bool hasTile = tiles.TryGetValue(key, out tile);
        if (hasTile && !tile.loaded) hasTile = false;

        if (!hasTile)
        {
            int nz = zoom;

            while (!hasTile && nz < OnlineMaps.MAXZOOM)
            {
                nz++;
                projection.CoordinatesToTile(cx, cz, nz, out tx, out ty);
                ix = (int)tx;
                iy = (int)ty;
                key = OnlineMapsTileManager.GetTileKey(nz, ix, iy);

                hasTile = tiles.TryGetValue(key, out tile) && tile.loaded;
            }
        }

        if (!hasTile)
        {
            int nz = zoom;

            while (!hasTile && nz > 1)
            {
                nz--;
                projection.CoordinatesToTile(cx, cz, nz, out tx, out ty);
                ix = (int)tx;
                iy = (int)ty;
                key = OnlineMapsTileManager.GetTileKey(nz, ix, iy);

                hasTile = tiles.TryGetValue(key, out tile) && tile.loaded;
            }
        }

        if (!hasTile) return 0;

        projection.CoordinatesToTile(cx, cz, tile.zoom, out tx, out ty);
        return tile.GetElevation(tx, ty);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (map != null)
        {
            map.OnChangePosition -= OnChangePosition;
            map.OnChangeZoom -= OnChangeZoom;
            map.OnLateUpdateBefore -= OnLateUpdateBefore;
        }
    }

    private void OnChangePosition()
    {
        if (needUpdateTiles) return;

        double sx, sy;
        map.GetTilePosition(out sx, out sy);
        int isx = (int) sx;
        int isy = (int) sy;

        if (prevTileX != isx || prevTileY != isy) needUpdateTiles = true;
    }

    private void OnChangeZoom()
    {
        needUpdateTiles = true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (tiles == null) tiles = new Dictionary<ulong, Tile>();
    }

    private void OnLateUpdateBefore()
    {
        if (!needUpdateTiles)
        {
            if (needUpdateMinMax) UpdateMinMax();
            return;
        }

        needUpdateTiles = false;

        int zoom = map.zoom - zoomOffset;
        if (zoom < 1) zoom = 1;
        if (!zoomRange.InRange(map.zoom)) return;

        int currentOffset = map.zoom - zoom;
        int coef = (1 << currentOffset) * OnlineMapsUtils.tileSize;
        int countX = Mathf.CeilToInt(map.width / 2f / coef) * 2 + 2;
        int countY = Mathf.CeilToInt(map.height / 2f / coef) * 2 + 2;

        double sx, sy;
        map.GetTilePosition(out sx, out sy, zoom);
        prevTileX = (int) sx;
        prevTileY = (int) sy;
        int isx = prevTileX - countX / 2;
        int isy = prevTileY - countY / 2;

        int max = 1 << zoom;

        foreach (KeyValuePair<ulong, Tile> pair in tiles)
        {
            pair.Value.used = false;
        }

        for (int x = isx; x < isx + countX; x++)
        {
            int cx = x;
            if (cx < 0) cx += max;
            else if (cx >= max) cx -= max;

            for (int y = Mathf.Max(isy, 0); y < Mathf.Min(isy + countY, max); y++)
            {
                ulong key = OnlineMapsTileManager.GetTileKey(zoom, cx, y);
                Tile t;
                if (tiles.TryGetValue(key, out t))
                {
                    t.used = true;
                    continue;
                }

                t = new Tile
                {
                    x = x,
                    y = y,
                    zoom = zoom,
                    width = tileWidth,
                    height = tileHeight,
                    used = true
                };
                tiles.Add(key, t);

                if (TryLoadFromCache(t)) { }
                else if (OnDownload != null) OnDownload(t);
                else StartDownloadElevationTile(t);
            }
        }

        double tlx, tly, brx, bry;
        map.GetTileCorners(out tlx, out tly, out brx, out bry);

        int itlx = (int)tlx;
        int itly = (int)tly;
        int ibrx = (int)brx;
        int ibry = (int)bry;

        List<ulong> unloadKeys = new List<ulong>();
        foreach (KeyValuePair<ulong, Tile> pair in tiles)
        {
            Tile tile = pair.Value;
            if (tile.used) continue;

            int scale = 1 << (map.zoom - tile.zoom);
            int tx = tile.x * scale;
            int ty = tile.y * scale;

            if (ibrx >= tx && itlx <= tx + scale && ibry >= ty && itly <= ty + scale)
            {
                if (Mathf.Abs(zoom - tile.zoom) < 3) continue;
            }

            unloadKeys.Add(pair.Key);
        }

        foreach (ulong key in unloadKeys) tiles.Remove(key);
        UpdateMinMax();
    }

    protected void SetElevationToCache(Tile tile, short[,] elevations)
    {
        if (!cacheElevations) return;

        byte[] cache = new byte[tileWidth * tileHeight * 2];
        int cacheIndex = 0;

        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                short s = elevations[x, y];
                cache[cacheIndex++] = (byte)(s & 255);
                cache[cacheIndex++] = (byte)(s >> 8);
            }
        }

        OnlineMapsCache.Add(tile.GetCacheKey(cachePrefix), cache);
    }

    protected void SetElevationData(Tile tile, short[,] elevations)
    {
        tile.elevations = elevations;

        short max = short.MinValue;
        short min = short.MaxValue;

        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                short v = elevations[x, y];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        tile.minValue = min;
        tile.maxValue = max;

        tile.loaded = true;
        needUpdateMinMax = true;

        CheckAllTilesLoaded();

        map.Redraw();
    }

    protected override void Start()
    {
        map.OnChangePosition += OnChangePosition;
        map.OnChangeZoom += OnChangeZoom;
        map.OnLateUpdateBefore += OnLateUpdateBefore;
    }

    /// <summary>
    /// Starts downloading elevation data for a tile
    /// </summary>
    /// <param name="tile">Tile</param>
    public abstract void StartDownloadElevationTile(Tile tile);

    protected bool TryLoadFromCache(Tile tile)
    {
        if (!cacheElevations) return false;

        byte[] data = OnlineMapsCache.Get(tile.GetCacheKey(cachePrefix));
        if (data == null || data.Length != tileWidth * tileHeight * 2) return false;

        short[,] elevations = new short[tileWidth, tileHeight];
        int dataIndex = 0;

        for (int y = 0; y < tileHeight; y++)
        {
            for (int x = 0; x < tileWidth; x++)
            {
                elevations[x, y] = (short)((data[dataIndex + 1] << 8) + data[dataIndex]);
                dataIndex += 2;
            }
        }

        SetElevationData(tile, elevations);

        if (OnElevationUpdated != null) OnElevationUpdated();

        return true;
    }

    protected override void UpdateMinMax()
    {
        needUpdateMinMax = false;

        double tlx, tly, brx, bry;
        map.GetTileCorners(out tlx, out tly, out brx, out bry);

        minValue = short.MaxValue;
        maxValue = short.MinValue;

        int itlx = (int)tlx;
        int itly = (int)tly;
        int ibrx = (int)brx;
        int ibry = (int)bry;

        foreach (KeyValuePair<ulong, Tile> pair in tiles)
        {
            Tile tile = pair.Value;
            if (!tile.loaded) continue;

            int scale = 1 << (map.zoom - tile.zoom);
            int tx = tile.x * scale;
            int ty = tile.y * scale;

            if (ibrx < tx || itlx > tx + scale) continue;
            if (ibry < ty || itly > ty + scale) continue;

            if (tile.minValue < minValue) minValue = tile.minValue;
            if (tile.maxValue > maxValue) maxValue = tile.maxValue;
        }
    }

    /// <summary>
    /// Elevation tile
    /// </summary>
    public class Tile
    {
        /// <summary>
        /// Is the tile loaded?
        /// </summary>
        public bool loaded = false;

        /// <summary>
        /// Tile X
        /// </summary>
        public int x;

        /// <summary>
        /// Tile Y
        /// </summary>
        public int y;

        /// <summary>
        /// Tile zoom
        /// </summary>
        public int zoom;

        /// <summary>
        /// Minimum elevation value
        /// </summary>
        public short minValue;

        /// <summary>
        /// Maximum elevation value
        /// </summary>
        public short maxValue;

        /// <summary>
        /// Elevation data width
        /// </summary>
        public int width;

        /// <summary>
        /// Elevation data height
        /// </summary>
        public int height;

        /// <summary>
        /// Elevation values
        /// </summary>
        public short[,] elevations;

        public bool used;

        /// <summary>
        /// Get elevation value from tile
        /// </summary>
        /// <param name="tx">Relative X (0-1)</param>
        /// <param name="ty">Relative Y (0-1)</param>
        /// <returns>Elevation value</returns>
        public float GetElevation(double tx, double ty)
        {
            if (!loaded) return 0;

            double rx = (tx - (int)tx) * (width - 1);
            double ry = (ty - (int)ty) * (height - 1);

            int x1 = (int) rx;
            int x2 = x1 + 1;
            int y1 = (int) ry;
            int y2 = y1 + 1;

            if (x2 >= width) x2 = width - 1;
            if (y2 >= height) y2 = height - 1;

            double dx = rx - x1;
            double dy = ry - y1;

            double v1 = elevations[x1, y1];
            double v2 = elevations[x2, y1];
            double v3 = elevations[x1, y2];
            double v4 = elevations[x2, y2];

            v1 = (v2 - v1) * dx + v1;
            v2 = (v4 - v3) * dx + v3;
            return (float)((v2 - v1) * dy + v1);
        }

        public string GetCacheKey(string prefix)
        {
            return prefix + OnlineMapsTileManager.GetTileKey(zoom, x, y);
        }
    }
}