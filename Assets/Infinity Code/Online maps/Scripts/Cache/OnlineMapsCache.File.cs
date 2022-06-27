/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if (!UNITY_WP_8_1 && !UNITY_WEBGL) || UNITY_EDITOR
#define ALLOW_FILECACHE
#endif

using System;
using System.Collections;
using System.Text;
using UnityEngine;

#if ALLOW_FILECACHE
using System.IO;
#endif

public partial class OnlineMapsCache
{
    /// <summary>
    /// Event occurs when loading the tile from the file cache.
    /// </summary>
    public Action<OnlineMapsTile> OnLoadedFromFileCache;

    /// <summary>
    /// Location of the file cache.
    /// </summary>
    public CacheLocation fileCacheLocation = CacheLocation.persistentDataPath;

    /// <summary>
    /// Custom file cache path.
    /// </summary>
    public string fileCacheCustomPath;

    /// <summary>
    /// Template file name in the file cache.
    /// </summary>
    public string fileCacheTilePath = "TileCache/{pid}/{mid}/{lbs}/{lng}/{zoom}/{x}/{y}";

    /// <summary>
    /// Rate of unloaded tiles from the file cache (0-1).
    /// </summary>
    public float fileCacheUnloadRate = 0.3f;

    /// <summary>
    /// The maximum size of the file cache (mb).
    /// </summary>
    public int maxFileCacheSize = 100;

    /// <summary>
    /// Flag indicating that the file cache is used.
    /// </summary>
    public bool useFileCache = true;

    private FileCacheAtlas fileCacheAtlas;
    private IEnumerator saveFileCacheAtlasCoroutine;
    private Texture2D tempTexture;

    /// <summary>
    /// The current size of the file cache (mb). 
    /// </summary>
    public int fileCacheSize
    {
        get
        {
            if (fileCacheAtlas == null) LoadFileCacheAtlas();
            return fileCacheAtlas.size;
        }
    }

    private void AddFileCacheItem(OnlineMapsTile tile, byte[] bytes)
    {
        if (!useFileCache || maxFileCacheSize <= 0) return;
        if (fileCacheAtlas == null) LoadFileCacheAtlas();

        fileCacheAtlas.Add(this, tile, bytes);
        if (fileCacheAtlas.size > maxFileCacheSize * 1000000) fileCacheAtlas.DeleteOldItems(this);

        if (saveFileCacheAtlasCoroutine == null)
        {
            saveFileCacheAtlasCoroutine = SaveFileCacheAtlas();
            StartCoroutine(saveFileCacheAtlasCoroutine);
        }
    }

    /// <summary>
    /// Clear file cache.
    /// </summary>
    public void ClearFileCache()
    {
#if ALLOW_FILECACHE
        StringBuilder builder = GetFileCacheFolder().Append("/TileCache");
        Directory.Delete(builder.ToString(), true);
        fileCacheAtlas = new FileCacheAtlas();
        fileCacheAtlas.Save(this);
#endif
    }

    /// <summary>
    /// Gets the file cache folder.
    /// </summary>
    /// <returns>File cache folder</returns>
    public StringBuilder GetFileCacheFolder()
    {
        StringBuilder stringBuilder = GetStringBuilder();

        if (fileCacheLocation == CacheLocation.persistentDataPath) stringBuilder.Append(OnlineMapsUtils.persistentDataPath).Append("/").Append("OnlineMapsCache");
        else
        {
            if (!string.IsNullOrEmpty(fileCacheCustomPath)) //throw new Exception("Custom path is empty.");
                stringBuilder.Append(fileCacheCustomPath);
        }
        return stringBuilder;
    }

    /// <summary>
    /// Fast way to get the size of the file cache.
    /// </summary>
    /// <returns>Size of the file cache (bytes)</returns>
    public int GetFileCacheSizeFast()
    {
#if ALLOW_FILECACHE
        if (fileCacheAtlas != null) return fileCacheAtlas.size;

        StringBuilder builder = GetFileCacheFolder();
        builder.Append("/").Append(FileCacheAtlas.AtlasName);
        string filename = builder.ToString();

        if (!File.Exists(filename)) return 0;

        FileStream stream = new FileStream(filename, FileMode.Open);
        BinaryReader reader = new BinaryReader(stream);

        reader.ReadByte();
        reader.ReadByte();
        reader.ReadInt16();
        int size = reader.ReadInt32();

        reader.Close();
        return size;
#else
        return 0;
#endif
    }

    /// <summary>
    /// Get the relative path to the tile in the file cache.
    /// </summary>
    /// <param name="tile">Tile</param>
    /// <returns>Relative path to the tile in the file cache</returns>
    public StringBuilder GetShortTilePath(OnlineMapsTile tile)
    {
#if ALLOW_FILECACHE
        int startIndex = 0;
        StringBuilder stringBuilder = GetStringBuilder();

        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;

        int l = fileCacheTilePath.Length;
        for (int i = 0; i < l; i++)
        {
            char c = fileCacheTilePath[i];
            if (c == '{')
            {
                for (int j = i + 1; j < l; j++)
                {
                    c = fileCacheTilePath[j];
                    if (c == '}')
                    {
                        stringBuilder.Append(fileCacheTilePath.Substring(startIndex, i - startIndex));
                        string v = fileCacheTilePath.Substring(i + 1, j - i - 1).ToLower();
                        if (v == "pid") stringBuilder.Append(rTile.mapType.provider.id);
                        else if (v == "mid") stringBuilder.Append(rTile.mapType.id);
                        else if (v == "zoom" || v == "z") stringBuilder.Append(tile.zoom);
                        else if (v == "x") stringBuilder.Append(tile.x);
                        else if (v == "y") stringBuilder.Append(tile.y);
                        else if (v == "quad") OnlineMapsUtils.TileToQuadKey(tile.x, tile.y, tile.zoom, stringBuilder);
                        else if (v == "lng") stringBuilder.Append(rTile.language);
                        else if (v == "lbs") stringBuilder.Append(rTile.labels ? "le" : "ld");
                        else stringBuilder.Append(v);
                        i = j;
                        startIndex = j + 1;
                        break;
                    }
                }
            }
        }

        stringBuilder.Append(fileCacheTilePath.Substring(startIndex, l - startIndex));
        return stringBuilder;
#else
        return null;
#endif
    }

    private void LoadFileCacheAtlas()
    {
        fileCacheAtlas = new FileCacheAtlas();
        fileCacheAtlas.Load(this);
    }

    private void LoadTile(OnlineMapsTile tile, byte[] bytes)
    {
        Texture2D texture = null;
        OnlineMaps map = tile.map;
        if (!map.control.resultIsTexture || tempTexture == null)
        {
            texture = tempTexture = new Texture2D(0, 0, TextureFormat.ARGB32, map.control.mipmapForTiles);
        }
        else texture = tempTexture;
            
        texture.LoadImage(bytes);
        texture.wrapMode = TextureWrapMode.Clamp;

        if (map.control.resultIsTexture)
        {
            (tile as OnlineMapsRasterTile).ApplyTexture(texture);
            map.buffer.ApplyTile(tile);
        }
        else
        {
            tile.texture = texture;

            OnlineMapsTileSetControl tsControl = map.control as OnlineMapsTileSetControl;
            if (tsControl != null && tsControl.compressTextures) texture.Compress(true);
        }

        tile.status = OnlineMapsTileStatus.loaded;
        tile.MarkLoaded();
        tile.map.Redraw();

        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;

        if (map.traffic && !string.IsNullOrEmpty(rTile.trafficURL))
        {
            if (map.traffic && !string.IsNullOrEmpty(rTile.trafficURL))
            {
                rTile.trafficWWW = new OnlineMapsWWW(rTile.trafficURL);
                rTile.trafficWWW["tile"] = tile;
                rTile.trafficWWW.OnComplete += OnlineMapsTileManager.OnTrafficWWWComplete;
            }
        }
    }

    private IEnumerator SaveFileCacheAtlas()
    {
        yield return new WaitForSeconds(5);
        if (fileCacheAtlas != null) fileCacheAtlas.Save(this);
        saveFileCacheAtlasCoroutine = null;
    }

    private bool TryLoadFromCache(OnlineMapsTile tile)
    {
#if ALLOW_FILECACHE
        if (useFileCache && TryLoadFromFileCache(tile))
        {
            return true;
        }
#endif
        return false;
    }

    private bool TryLoadFromFileCache(OnlineMapsTile tile)
    {
#if ALLOW_FILECACHE
        if (fileCacheAtlas == null) LoadFileCacheAtlas();

        StringBuilder filename = GetShortTilePath(tile);
        string shortFilename = filename.ToString();
        if (!fileCacheAtlas.Contains(shortFilename)) return false;

        string fullTilePath = fileCacheAtlas.GetFullPath(this, shortFilename);
        if (!File.Exists(fullTilePath)) return false;

        tile.status = OnlineMapsTileStatus.loading;

#if !UNITY_WEBGL
        OnlineMapsThreadManager.AddThreadAction(() =>
        {
            byte[] bytes = File.ReadAllBytes(fullTilePath);

            OnlineMapsThreadManager.AddMainThreadAction(() =>
            {
                if (tile.map == null) return;

                LoadTile(tile, bytes);
                AddMemoryCacheItem(tile);
                if (OnLoadedFromFileCache != null) OnLoadedFromFileCache(tile);
                OnlineMapsLog.Info("Tile " + tile + " loaded from cache.", OnlineMapsLog.Type.cache);
            });
        });
#else
        byte[] bytes = File.ReadAllBytes(fullTilePath);
        LoadTile(tile, bytes);
        AddMemoryCacheItem(tile);
        if (OnLoadedFromFileCache != null) OnLoadedFromFileCache(tile);
        OnlineMapsLog.Info("Tile " + tile + " loaded from cache.", OnlineMapsLog.Type.cache);
#endif

        return true;
#else
        return false;
#endif
    }

    public class FileCacheAtlas: CacheAtlas<FileCacheItem>
    {
        public const string AtlasName = "tilecacheatlas.dat";

        protected override string atlasName
        {
            get { return AtlasName; }
        }

        public void Add(OnlineMapsCache cache, OnlineMapsTile tile, byte[] bytes)
        {
#if ALLOW_FILECACHE
            StringBuilder filename = cache.GetShortTilePath(tile);
            string shortFilename = filename.ToString();
            if (Contains(shortFilename)) return;

            string fullFilename = GetFullPath(cache, shortFilename);

#if !UNITY_WEBGL
            OnlineMapsThreadManager.AddThreadAction(() =>
            {
#endif
                FileInfo fileInfo = new FileInfo(fullFilename);
                if (!Directory.Exists(fileInfo.DirectoryName)) Directory.CreateDirectory(fileInfo.DirectoryName);
                File.WriteAllBytes(fullFilename, bytes);
#if !UNITY_WEBGL
        });
#endif

            AddItem(shortFilename, bytes.Length);
            size += bytes.Length;
#endif
        }

        private void AddItem(string filename, int size)
        {
            FileCacheItem item = new FileCacheItem(filename, size);
            if (capacity <= count)
            {
                capacity += 100;
                Array.Resize(ref items, capacity);
            }
            items[count++] = item;
        }

        public override FileCacheItem CreateItem(string filename, int size, long time)
        {
            return new FileCacheItem(filename, size, time);
        }

        public override void DeleteOldItems(OnlineMapsCache cache)
        {
#if ALLOW_FILECACHE
            int countUnload = Mathf.RoundToInt(count * cache.fileCacheUnloadRate);
            if (countUnload <= 0) throw new Exception("Can not unload a negative number of items. Check fileCacheUnloadRate.");
            if (count < countUnload) countUnload = count;

            long[] unloadTimes = new long[countUnload];
            int[] unloadIndices = new int[countUnload];
            string[] unloadFiles = new string[countUnload];
            int c = 0;

            for (int i = 0; i < count; i++)
            {
                long t = items[i].time;
                if (c == 0)
                {
                    unloadIndices[0] = 0;
                    unloadTimes[0] = t;
                    c++;
                }
                else
                {
                    int index = c;
                    int index2 = index - 1;

                    while (index2 >= 0)
                    {
                        if (unloadTimes[index2] < t) break;

                        index2--;
                        index--;
                    }

                    if (index < countUnload)
                    {
                        for (int j = countUnload - 1; j > index; j--)
                        {
                            unloadIndices[j] = unloadIndices[j - 1];
                            unloadTimes[j] = unloadTimes[j - 1];
                        }
                        unloadIndices[index] = i;
                        unloadTimes[index] = t;
                        if (c < countUnload) c++;
                    }
                }
            }

            for (int i = 0; i < countUnload; i++)
            {
                int index = unloadIndices[i];
                size -= items[index].size;
                string fullFilename = GetFullPath(cache, items[index].key);
                unloadFiles[i] = fullFilename;
                items[index] = null;
            }

            int offset = 0;
            for (int i = 0; i < count; i++)
            {
                if (items[i] == null) offset++;
                else if (offset > 0) items[i - offset] = items[i];
            }

            count -= countUnload;

#if !UNITY_WEBGL
            OnlineMapsThreadManager.AddThreadAction(() =>
            {
#endif
                for (int i = 0; i < countUnload; i++)
                {
                    string fn = unloadFiles[i];
                    if (File.Exists(fn)) File.Delete(fn);
                }
#if !UNITY_WEBGL
        });
#endif
#endif
        }

        public string GetFullPath(OnlineMapsCache cache, string shortFilename)
        {
#if ALLOW_FILECACHE
            StringBuilder stringBuilder = cache.GetFileCacheFolder();
            stringBuilder.Append("/").Append(shortFilename).Append(".png");
            return stringBuilder.ToString();
#else
            return null;
#endif
        }
    }

    public class FileCacheItem : CacheItem
    {
        public FileCacheItem(string filename, int size) : this(filename, size, DateTime.Now.Ticks)
        {

        }

        public FileCacheItem(string filename, int size, long time)
        {
            key = filename;
            hash = filename.GetHashCode();
            this.size = size;
            this.time = time;
        }
    }
}