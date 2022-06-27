/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Manages map tiles
/// </summary>
public class OnlineMapsTileManager
{
    /// <summary>
    /// The maximum number simultaneously downloading tiles.
    /// </summary>
    public static int maxTileDownloads = 5;

    /// <summary>
    /// This event is used to load a tile from the cache. Use this event only if you are implementing your own caching system.
    /// </summary>
    public static Action<OnlineMapsTile> OnLoadFromCache;

    /// <summary>
    /// The event occurs after generating buffer and before update control to preload tiles for tileset.
    /// </summary>
    public static Action OnPreloadTiles;

    /// <summary>
    /// This event is used in preparation for loading a tile.
    /// </summary>
    public static Action<OnlineMapsTile> OnPrepareDownloadTile;

    /// <summary>
    /// An event that occurs when loading the tile. Allows you to intercept of loading tile, and load it yourself.
    /// </summary>
    public static Action<OnlineMapsTile> OnStartDownloadTile;

    /// <summary>
    /// This event is occurs when a tile is loaded.
    /// </summary>
    public static Action<OnlineMapsTile> OnTileLoaded;

    private OnlineMapsTile[] downloadTiles;
    private Dictionary<ulong, OnlineMapsTile> _dtiles;
    private OnlineMaps _map;
    private List<OnlineMapsTile> _tiles;
    private List<OnlineMapsTile> unusedTiles;

    /// <summary>
    /// Dictionary of tiles
    /// </summary>
    public Dictionary<ulong, OnlineMapsTile> dTiles
    {
        get { return _dtiles; }
    }

    /// <summary>
    /// List of tiles
    /// </summary>
    public List<OnlineMapsTile> tiles
    {
        get { return _tiles; }
        set { _tiles = value; }
    }

    /// <summary>
    /// Reference to the map
    /// </summary>
    public OnlineMaps map
    {
        get { return _map; }
    }

    public OnlineMapsTileManager(OnlineMaps map)
    {
        _map = map;
        unusedTiles = new List<OnlineMapsTile>();
        _tiles = new List<OnlineMapsTile>();
        _dtiles = new Dictionary<ulong, OnlineMapsTile>();
    }

    /// <summary>
    /// Add a tile to the manager
    /// </summary>
    /// <param name="tile">Tile</param>
    public void Add(OnlineMapsTile tile)
    {
        tiles.Add(tile);
        if (dTiles.ContainsKey(tile.key)) dTiles[tile.key] = tile;
        else dTiles.Add(tile.key, tile);
    }

    public void Dispose()
    {
        foreach (OnlineMapsTile tile in tiles) tile.Dispose();

        _map = null;
        _dtiles = null;
        _tiles = null;
    }

    /// <summary>
    /// Gets a tile for zoom, x, y.
    /// </summary>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <returns>Tile or null</returns>
    public OnlineMapsTile GetTile(int zoom, int x, int y)
    {
        ulong key = GetTileKey(zoom, x, y);
        if (dTiles.ContainsKey(key))
        {
            OnlineMapsTile tile = dTiles[key];
            if (tile.status != OnlineMapsTileStatus.disposed) return tile;
        }
        return null;
    }

    /// <summary>
    /// Gets a tile for zoom, x, y.
    /// </summary>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <param name="tile">Tile</param>
    /// <returns>True - success, false - otherwise</returns>
    public bool GetTile(int zoom, int x, int y, out OnlineMapsTile tile)
    {
        tile = null;
        ulong key = GetTileKey(zoom, x, y);
        OnlineMapsTile t;
        if (dTiles.TryGetValue(key, out t))
        {
            if (t.status != OnlineMapsTileStatus.disposed)
            {
                tile = t;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets a tile key for zoom, x, y.
    /// </summary>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <returns>Tile key</returns>
    public static ulong GetTileKey(int zoom, int x, int y)
    {
        return ((ulong)zoom << 58) + ((ulong)x << 29) + (ulong)y;
    }

    private static void OnTileWWWComplete(OnlineMapsWWW www)
    {
        OnlineMapsTile tile = www["tile"] as OnlineMapsTile;

        if (tile == null) return;
        tile.LoadFromWWW(www);
    }

    public static void OnTrafficWWWComplete(OnlineMapsWWW www)
    {
        OnlineMapsRasterTile tile = www["tile"] as OnlineMapsRasterTile;

        if (tile == null) return;
        if (tile.trafficWWW == null || !tile.trafficWWW.isDone) return;

        if (tile.status == OnlineMapsTileStatus.disposed)
        {
            tile.trafficWWW = null;
            return;
        }

        if (www.hasError || www.bytesDownloaded <= 0)
        {
            tile.trafficWWW = null;
            return;
        }

        if (tile.map.control.resultIsTexture)
        {
            if (tile.OnLabelDownloadComplete()) tile.map.buffer.ApplyTile(tile);
        }
        else if (tile.trafficWWW != null && tile.map.traffic)
        {
            Texture2D trafficTexture = new Texture2D(256, 256, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            if (tile.map.useSoftwareJPEGDecoder) OnlineMapsRasterTile.LoadTexture(trafficTexture, www.bytes);
            else tile.trafficWWW.LoadImageIntoTexture(trafficTexture);

            OnlineMapsTileSetControl tsControl = tile.map.control as OnlineMapsTileSetControl;
            if (tsControl != null && tsControl.compressTextures) trafficTexture.Compress(true);

            tile.trafficTexture = trafficTexture;
        }

        if (OnlineMapsTile.OnTrafficDownloaded != null) OnlineMapsTile.OnTrafficDownloaded(tile);

        tile.map.Redraw();


    }

    /// <summary>
    /// Remove tile.
    /// </summary>
    /// <param name="tile">Tile</param>
    public void Remove(OnlineMapsTile tile)
    {
        unusedTiles.Add(tile);
        if (_dtiles.ContainsKey(tile.key)) _dtiles.Remove(tile.key);
    }

    /// <summary>
    /// Reset state of tile manager and dispose all tiles
    /// </summary>
    public void Reset()
    {
        foreach (OnlineMapsTile tile in tiles) tile.Dispose();
        tiles.Clear();
        dTiles.Clear();
    }

    /// <summary>
    /// Start next downloads (if any).
    /// </summary>
    public void StartDownloading()
    {
        if (tiles == null) return;
        float startTime = Time.realtimeSinceStartup;

        int countDownload = 0;
        int c = 0;

        lock (OnlineMapsTile.lockTiles)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                OnlineMapsTile tile = tiles[i];
                if (tile.status == OnlineMapsTileStatus.loading && tile.www != null)
                {
                    countDownload++;
                    if (countDownload >= maxTileDownloads) return;
                }
            }

            int needDownload = maxTileDownloads - countDownload;

            if (downloadTiles == null) downloadTiles = new OnlineMapsTile[maxTileDownloads];

            for (int i = 0; i < tiles.Count; i++)
            {
                OnlineMapsTile tile = tiles[i];
                if (tile.status != OnlineMapsTileStatus.none) continue;

                if (c == 0)
                {
                    downloadTiles[0] = tile;
                    c++;
                }
                else
                {
                    int index = c;
                    int index2 = index - 1;

                    while (index2 >= 0)
                    {
                        if (downloadTiles[index2].zoom <= tile.zoom) break;

                        index2--;
                        index--;
                    }

                    if (index < needDownload)
                    {
                        for (int j = needDownload - 1; j > index; j--) downloadTiles[j] = downloadTiles[j - 1];
                        downloadTiles[index] = tile;
                        if (c < needDownload) c++;
                    }
                }
            }
        }

        for (int i = 0; i < c; i++)
        {
            if (Time.realtimeSinceStartup - startTime > 0.02) break;
            OnlineMapsTile tile = downloadTiles[i];

            countDownload++;
            if (countDownload > maxTileDownloads) break;

            if (OnPrepareDownloadTile != null) OnPrepareDownloadTile(tile);

            if (OnLoadFromCache != null) OnLoadFromCache(tile);
            else if (OnStartDownloadTile != null) OnStartDownloadTile(tile);
            else StartDownloadTile(tile);
        }
    }

    /// <summary>
    /// Starts downloading of specified tile.
    /// </summary>
    /// <param name="tile">Tile to be downloaded.</param>
    public static void StartDownloadTile(OnlineMapsTile tile)
    {
        tile.status = OnlineMapsTileStatus.loading;
        tile.map.StartCoroutine(StartDownloadTileAsync(tile));
    }

    private static IEnumerator StartDownloadTileAsync(OnlineMapsTile tile)
    {
        bool loadOnline = true;

        OnlineMaps map = tile.map;
        OnlineMapsSource source = map.source;
        if (source != OnlineMapsSource.Online)
        {
            if (source == OnlineMapsSource.Resources || source == OnlineMapsSource.ResourcesAndOnline)
            {
                yield return TryLoadFromResources(tile);
                if (tile.status == OnlineMapsTileStatus.error) yield break;
                if (tile.status == OnlineMapsTileStatus.loaded) loadOnline = false;
            }
            else if (source == OnlineMapsSource.StreamingAssets || source == OnlineMapsSource.StreamingAssetsAndOnline)
            {
                yield return TryLoadFromStreamingAssets(tile);
                if (tile.status == OnlineMapsTileStatus.error) yield break;
                if (tile.status == OnlineMapsTileStatus.loaded) loadOnline = false;
            }
        }

        if (loadOnline)
        {
            if (tile.www != null)
            {
                Debug.Log("tile has www " + tile + "   " + tile.status);
                yield break;
            }

            tile.www = new OnlineMapsWWW(tile.url);
            tile.www["tile"] = tile;
            tile.www.OnComplete += OnTileWWWComplete;
            tile.status = OnlineMapsTileStatus.loading;
        }

        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;

        try
        {
            if (map.traffic && !string.IsNullOrEmpty(rTile.trafficURL))
            {
                rTile.trafficWWW = new OnlineMapsWWW(rTile.trafficURL);
                rTile.trafficWWW["tile"] = tile;
                rTile.trafficWWW.OnComplete += OnTrafficWWWComplete;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        
    }

    private static IEnumerator TryLoadFromResources(OnlineMapsTile tile)
    {
        ResourceRequest resourceRequest = Resources.LoadAsync(tile.resourcesPath);
        yield return resourceRequest;

        if (tile.map == null)
        {
            tile.MarkError();
            yield break;
        }

        Texture2D texture = resourceRequest.asset as Texture2D;

        if (texture != null)
        {
            texture.wrapMode = TextureWrapMode.Clamp;
            if (tile.map.control.resultIsTexture)
            {
                (tile as OnlineMapsRasterTile).ApplyTexture(texture);
                tile.map.buffer.ApplyTile(tile);
                Resources.UnloadAsset(texture);
            }
            else
            {
                tile.texture = texture;
                tile.status = OnlineMapsTileStatus.loaded;
            }
            tile.MarkLoaded();
            tile.loadedFromResources = true;
            tile.map.Redraw();
        }
        else if (tile.map.source == OnlineMapsSource.Resources)
        {
            tile.MarkError();
        }
    }

    private static IEnumerator TryLoadFromStreamingAssets(OnlineMapsTile tile)
    {
        if (tile.map == null)
        {
            tile.MarkError();
            yield break;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        string path = Application.streamingAssetsPath + "/" + tile.streamingAssetsPath;
#if !UNITY_ANDROID || UNITY_EDITOR
        if (!System.IO.File.Exists(path))
        {
            if (tile.map.source == OnlineMapsSource.StreamingAssets) tile.MarkError();
            yield break;
        }
        byte[] bytes = System.IO.File.ReadAllBytes(path);
#else
        OnlineMapsWWW www = new OnlineMapsWWW(path);
        yield return www;

        if (tile.map == null)
        {
            tile.MarkError();
            yield break;
        }

        if (www.hasError)
        {
            if (tile.map.source == OnlineMapsSource.StreamingAssets) tile.MarkError();
            yield break;
        }
        byte[] bytes = www.bytes;
#endif

        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);

        texture.wrapMode = TextureWrapMode.Clamp;
        if (tile.map.control.resultIsTexture)
        {
            (tile as OnlineMapsRasterTile).ApplyTexture(texture);
            tile.map.buffer.ApplyTile(tile);
            OnlineMapsUtils.Destroy(texture);
        }
        else
        {
            tile.texture = texture;

            OnlineMapsTileSetControl tsControl = tile.map.control as OnlineMapsTileSetControl;
            if (tsControl != null && tsControl.compressTextures) texture.Compress(true);

            tile.status = OnlineMapsTileStatus.loaded;
        }

        tile.MarkLoaded();
        tile.map.Redraw();
#else
        if (tile.map.source == OnlineMapsSource.StreamingAssets) tile.MarkError();
#endif
    }

    public void UnloadUnusedTiles()
    {
        if (unusedTiles == null) return;

        for (int i = 0; i < unusedTiles.Count; i++) unusedTiles[i].Destroy();
        unusedTiles.Clear();
    }
}