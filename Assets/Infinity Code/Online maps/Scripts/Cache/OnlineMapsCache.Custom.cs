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

public partial class OnlineMapsCache
{
    public delegate bool OnGetFromCustomCacheDelegate(string key, out byte[] bytes);
    public static OnGetFromCustomCacheDelegate OnGetFromCustomCache;

    /// <summary>
    /// Rate of unloaded tiles from the file cache (0-1).
    /// </summary>
    public float customCacheUnloadRate = 0.3f;

    /// <summary>
    /// The maximum size of the file cache (mb).
    /// </summary>
    public int maxCustomCacheSize = 100;

    private CustomCacheAtlas customCacheAtlas;
    private int countCustomItems;
    private IEnumerator saveCustomCacheAtlasCoroutine;

    public int customCacheSize
    {
        get
        {
            if (customCacheAtlas == null) LoadCustomCacheAtlas();
            return customCacheAtlas.size;
        }
    }

    /// <summary>
    /// Saves data in custom cache by the key.
    /// </summary>
    /// <param name="key">The unique key of the value.</param>
    /// <param name="bytes">The value to be stored.</param>
    public static void Add(string key, byte[] bytes = null)
    {
        if (_instance != null) _instance.AddItem(key, bytes);
    }

    /// <summary>
    /// Saves data in custom cache by the key.
    /// </summary>
    /// <param name="key">The unique key of the value.</param>
    /// <param name="bytes">The value to be stored.</param>
    public void AddItem(string key, byte[] bytes = null)
    {
        if (string.IsNullOrEmpty(key) || bytes == null) return;

        if (customCacheAtlas == null) LoadCustomCacheAtlas();
        customCacheAtlas.Add(this, key, bytes);

        if (customCacheAtlas.size > maxCustomCacheSize * 1000000) customCacheAtlas.DeleteOldItems(this);

        if (saveCustomCacheAtlasCoroutine == null)
        {
            saveCustomCacheAtlasCoroutine = SaveCustomCacheAtlas();
            StartCoroutine(saveCustomCacheAtlasCoroutine);
        }
    }

    /// <summary>
    /// Clear custom cache
    /// </summary>
    public void ClearCustomCache()
    {
#if ALLOW_FILECACHE
        StringBuilder builder = GetFileCacheFolder().Append("/CustomCache");
        Directory.Delete(builder.ToString(), true);
        customCacheAtlas = new CustomCacheAtlas();
        customCacheAtlas.Save(this);
#endif
    }

    /// <summary>
    /// Returns value from custom cache by key.
    /// </summary>
    /// <param name="key">The unique key of the value.</param>
    /// <returns>Value of null.</returns>
    public static byte[] Get(string key)
    {
        return _instance != null ? _instance.GetItem(key) : null;
    }

    /// <summary>
    /// Fast way to get the size of the file cache.
    /// </summary>
    /// <returns>Size of the file cache (bytes)</returns>
    public int GetCustomCacheSizeFast()
    {
#if ALLOW_FILECACHE
        if (customCacheAtlas != null) return customCacheAtlas.size;

        StringBuilder builder = GetFileCacheFolder();
        builder.Append("/").Append(CustomCacheAtlas.AtlasName);
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
    /// Returns value from custom cache by key.
    /// </summary>
    /// <param name="key">The unique key of the value.</param>
    /// <returns>Value of null.</returns>
    public byte[] GetItem(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (OnGetFromCustomCache != null)
        {
            byte[] bytes;
            if (OnGetFromCustomCache(key, out bytes)) return bytes;
        }

        if (customCacheAtlas == null) LoadCustomCacheAtlas();
        return customCacheAtlas.GetItem(this, key);
    }

    private void LoadCustomCacheAtlas()
    {
        customCacheAtlas = new CustomCacheAtlas();
        customCacheAtlas.Load(this);
    }

    private IEnumerator SaveCustomCacheAtlas()
    {
        yield return new WaitForSeconds(5);
        if (customCacheAtlas != null) customCacheAtlas.Save(this);
        saveCustomCacheAtlasCoroutine = null;
    }

    public class CustomCacheAtlas : CacheAtlas<CustomCacheItem>
    {
        public const string AtlasName = "customcacheatlas.dat";

        protected override string atlasName
        {
            get { return AtlasName; }
        }

        public void Add(OnlineMapsCache cache, string key, byte[] bytes)
        {
#if ALLOW_FILECACHE
            int hash = key.GetHashCode();

            int index = -1;
            string path = "";

            for (int i = 0; i < count; i++)
            {
                CustomCacheItem item = items[i];
                if (item.hash == hash && item.key == key)
                {
                    size -= item.size;
                    item.size = bytes.Length;
                    size += bytes.Length;

                    for (int j = i + 1; j < count; j++) items[j - 1] = items[j];
                    items[count - 1] = item;
                    path = item.GetFullPath(cache);
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                CustomCacheItem item = AddItem(key, bytes.Length);
                path = item.GetFullPath(cache);
                size += bytes.Length;
            }

#if !UNITY_WEBGL
            OnlineMapsThreadManager.AddThreadAction(() =>
            {
#endif
                FileInfo fileInfo = new FileInfo(path);
                if (!Directory.Exists(fileInfo.DirectoryName)) Directory.CreateDirectory(fileInfo.DirectoryName);
                File.WriteAllBytes(path, bytes);
#if !UNITY_WEBGL
            });
#endif
#endif
        }

        private CustomCacheItem AddItem(string key, int size)
        {
            CustomCacheItem item = new CustomCacheItem(key, size);
            if (capacity <= count)
            {
                capacity += 100;
                Array.Resize(ref items, capacity);
            }
            items[count++] = item;
            return item;
        }

        public override CustomCacheItem CreateItem(string filename, int size, long time)
        {
            return new CustomCacheItem(filename, size, time);
        }

        public override void DeleteOldItems(OnlineMapsCache cache)
        {
#if ALLOW_FILECACHE
            int unloadSize = Mathf.RoundToInt(cache.maxCustomCacheSize * cache.customCacheUnloadRate);

            int s = 0;
            int countUnload = 0;

            while (countUnload < items.Length && s < unloadSize)
            {
                s += items[countUnload].size;
                countUnload++;
            }

            string[] unloadFiles = new string[countUnload];

            for (int i = 0; i < countUnload; i++)
            {
                CustomCacheItem item = items[i];
                size -= item.size;
                string fullFilename = item.GetFullPath(cache);
                unloadFiles[i] = fullFilename;
                items[i] = null;
            }

            for (int i = countUnload; i < count; i++) items[i - countUnload] = items[i];

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

        public byte[] GetItem(OnlineMapsCache cache, string key)
        {
            int hash = key.GetHashCode();

            for (int i = 0; i < count; i++)
            {
                CustomCacheItem item = items[i];
                if (item.hash == hash && item.key == key) return item.GetBytes(cache);
            }

            return null;
        }
    }

    public class CustomCacheItem : CacheItem
    {
        public CustomCacheItem(string key, int size) : this(key, size, DateTime.Now.Ticks)
        {

        }

        public CustomCacheItem(string key, int size, long time)
        {
            this.key = key;
            hash = key.GetHashCode();
            this.size = size;
            this.time = time;
        }

        public byte[] GetBytes(OnlineMapsCache cache)
        {
#if ALLOW_FILECACHE
            string path = GetFullPath(cache);
            if (!File.Exists(path)) return null;
            return File.ReadAllBytes(path);
#else
            return null;
#endif
        }

        public string GetFullPath(OnlineMapsCache cache)
        {
#if ALLOW_FILECACHE
            StringBuilder stringBuilder = cache.GetFileCacheFolder();
            stringBuilder.Append("/CustomCache/").Append(hash).Append(".dat");
            return stringBuilder.ToString();
#else
            return null;
#endif
        }
    }
}