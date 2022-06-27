/*           INFINITY CODE           */
/*     https://infinity-code.com     */

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


/// <summary>
/// Class for caching tiles in memory and the file system.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Cache")]
[OnlineMapsPlugin("Cache", typeof(OnlineMapsControlBase), true)]
public partial class OnlineMapsCache:MonoBehaviour, IOnlineMapsSavableComponent
{
    private static OnlineMapsCache _instance;
    private static StringBuilder _stringBuilder;

    /// <summary>
    /// Event occurs when loading the tile from the file cache or memory cache.
    /// </summary>
    public Action<OnlineMapsTile> OnLoadedFromCache;

    [Obsolete("Use OnlineMapsTileManager.OnStartDownloadTile")]
    public Action<OnlineMapsTile> OnStartDownloadTile;

    private OnlineMapsSavableItem[] savableItems;

    /// <summary>
    /// The reference to an instance of the cache.
    /// </summary>
    public static OnlineMapsCache instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Clear all caches.
    /// </summary>
    public void ClearAllCaches()
    {
        ClearMemoryCache();
        ClearFileCache();
    }

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("cache", "Cache", SaveSettings)
            {
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    protected static StringBuilder GetStringBuilder()
    {
        if (_stringBuilder == null) _stringBuilder = new StringBuilder();
        else _stringBuilder.Length = 0;

        return _stringBuilder;
    }

    private void LoadSettings(OnlineMapsJSONObject json)
    {
        json.DeserializeObject(this);
    }

    private void OnDestroy()
    {
        OnlineMaps.OnPreloadTiles -= OnPreloadTiles;
        OnlineMapsTileManager.OnLoadFromCache -= OnStartDownloadTileM;
        OnlineMapsTile.OnTileDownloaded -= OnTileDownloaded;
    }

    private void OnDisable()
    {
        if (saveFileCacheAtlasCoroutine != null)
        {
            StopCoroutine(saveFileCacheAtlasCoroutine);
            if (fileCacheAtlas != null) fileCacheAtlas.Save(this);
        }

        if (saveCustomCacheAtlasCoroutine != null)
        {
            StopCoroutine(saveCustomCacheAtlasCoroutine);
            if (customCacheAtlas != null) customCacheAtlas.Save(this);
        }
    }

    private void OnEnable()
    {
        _instance = this;
    }

    private void OnPreloadTiles(OnlineMaps map)
    {
        lock (OnlineMapsTile.lockTiles)
        {
            float start = Time.realtimeSinceStartup;
            for (int i = 0; i < map.tileManager.tiles.Count; i++)
            {
                OnlineMapsTile tile = map.tileManager.tiles[i];
                if (tile.status != OnlineMapsTileStatus.none || tile.cacheChecked) continue;
                if (!TryLoadFromCache(tile)) tile.cacheChecked = true;
                else if (OnLoadedFromCache != null) OnLoadedFromCache(tile);
                if (Time.realtimeSinceStartup - start > 0.02) return;
            }
        }
    }

    private void OnStartDownloadTileM(OnlineMapsTile tile)
    {
        if (TryLoadFromCache(tile))
        {
            if (OnLoadedFromCache != null) OnLoadedFromCache(tile);
        }
        else
        {
#pragma warning disable 618
            if (OnStartDownloadTile != null) OnStartDownloadTile(tile);
#pragma warning restore 618
            else if (OnlineMapsTileManager.OnStartDownloadTile != null) OnlineMapsTileManager.OnStartDownloadTile(tile);
            else OnlineMapsTileManager.StartDownloadTile(tile);
        }
    }

    private void OnTileDownloaded(OnlineMapsTile tile)
    {
        if (useMemoryCache) AddMemoryCacheItem(tile);
        if (useFileCache) AddFileCacheItem(tile, tile.www.bytes);
    }

    private OnlineMapsJSONItem SaveSettings()
    {
        return OnlineMapsJSON.Serialize(new
        {
            useMemoryCache,
            maxMemoryCacheSize,
            memoryCacheUnloadRate,

            useFileCache,
            maxFileCacheSize,
            fileCacheUnloadRate,
            fileCacheLocation,
            fileCacheCustomPath,
            fileCacheTilePath
        });
    }

    private void Start()
    {
        OnlineMapsTileManager.OnLoadFromCache += OnStartDownloadTileM;
        OnlineMaps.OnPreloadTiles += OnPreloadTiles;
        OnlineMapsTile.OnTileDownloaded += OnTileDownloaded;
    }

    public abstract class CacheAtlas<T> where T: CacheItem
    {
        protected const short ATLAS_VERSION = 1;

        protected int capacity = 256;
        protected int count = 0;

        protected T[] items;

        protected abstract string atlasName { get; }

        public int size { get; protected set; }

        public CacheAtlas()
        {
            size = 0;
            items = new T[capacity];
        }

        public bool Contains(string filename)
        {
            int hash = filename.GetHashCode();
            for (int i = 0; i < count; i++)
            {
                if (items[i].hash == hash && items[i].key == filename) return true;
            }
            return false;
        }

        public abstract T CreateItem(string filename, int size, long time);

        public abstract void DeleteOldItems(OnlineMapsCache cache);

        public void Load(OnlineMapsCache cache)
        {
#if ALLOW_FILECACHE
            StringBuilder builder = cache.GetFileCacheFolder();
            builder.Append("/").Append(atlasName);
            string filename = builder.ToString();

            if (!File.Exists(filename)) return;

            FileStream stream = new FileStream(filename, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);

            byte c1 = reader.ReadByte();
            byte c2 = reader.ReadByte();

            if (c1 == 'T' && c2 == 'C')
            {
                int cacheVersion = reader.ReadInt16();
                if (cacheVersion > 0)
                {
                    // For future versions
                }
            }
            else stream.Position = 0;

            size = reader.ReadInt32();

            long l = stream.Length;
            while (stream.Position < l)
            {
                filename = reader.ReadString();
                int s = reader.ReadInt32();
                long time = reader.ReadInt64();
                T item = CreateItem(filename, s, time);
                if (capacity <= count)
                {
                    capacity *= 2;
                    Array.Resize(ref items, capacity);
                }
                items[count++] = item;
            }

            reader.Close();
#endif
        }

        public void Save(OnlineMapsCache cache)
        {
#if ALLOW_FILECACHE
            StringBuilder builder = cache.GetFileCacheFolder();
            builder.Append("/").Append(atlasName);
            string filename = builder.ToString();

            FileInfo fileInfo = new FileInfo(filename);
            if (!Directory.Exists(fileInfo.DirectoryName)) Directory.CreateDirectory(fileInfo.DirectoryName);

            T[] itemsCopy = new T[items.Length];
            items.CopyTo(itemsCopy, 0);

#if !UNITY_WEBGL
            OnlineMapsThreadManager.AddThreadAction(() =>
            {
#endif
                FileStream stream = new FileStream(filename, FileMode.Create);
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write((byte)'T');
                writer.Write((byte)'C');
                writer.Write(ATLAS_VERSION);

                writer.Write(size);

                for (int i = 0; i < count; i++)
                {
                    T item = itemsCopy[i];
                    writer.Write(item.key);
                    writer.Write(item.size);
                    writer.Write(item.time);
                }

                writer.Close();
#if !UNITY_WEBGL
        });
#endif
#endif
        }
    }

    public abstract class CacheItem
    {
        public int size;
        public int hash;
        public string key;
        public long time;

        public static CacheItem Create()
        {
            return null;
        }
    }

    /// <summary>
    /// Location of the file cache
    /// </summary>
    public enum CacheLocation
    {
        /// <summary>
        /// Application.persistentDataPath
        /// </summary>
        persistentDataPath,
        /// <summary>
        /// Custom
        /// </summary>
        custom
    }
}