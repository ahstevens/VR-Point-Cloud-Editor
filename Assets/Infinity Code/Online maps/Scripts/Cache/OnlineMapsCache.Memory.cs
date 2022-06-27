/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

public partial class OnlineMapsCache
{
    /// <summary>
    /// Event occurs when loading the tile from the memory cache.
    /// </summary>
    [Obsolete("Now this event is never called.")]
    public Action<OnlineMapsTile> OnLoadedFromMemoryCache;

    /// <summary>
    /// The maximum size of the memory cache (mb).
    /// </summary>
    public int maxMemoryCacheSize = 10;

    /// <summary>
    /// Rate of unloaded tiles from the memory cache (0-1).
    /// </summary>
    public float memoryCacheUnloadRate = 0.3f;

    /// <summary>
    /// Flag indicating that the memory cache is used.
    /// </summary>
    public bool useMemoryCache = true;

    private int countMemoryItems;
    private MemoryCacheItem[] memoryCache;

    /// <summary>
    /// The current size of the memory cache (mb).
    /// </summary>
    public int memoryCacheSize { get; private set; }

    private void AddMemoryCacheItem(OnlineMapsTile tile)
    {
        if (!useMemoryCache || maxMemoryCacheSize <= 0) return;

        string tileKey = GetTileKey(tile);
        int tileHash = tileKey.GetHashCode();

        AddMemoryCacheItem(tile, tileHash, tileKey);
    }

    private void AddMemoryCacheItem(OnlineMapsTile tile, int tileHash, string tileKey)
    {
        for (int i = 0; i < countMemoryItems; i++)
        {
            if (memoryCache[i].hash == tileHash && memoryCache[i].key == tileKey)
            {
                memoryCache[i].time = DateTime.Now.Ticks;
                return;
            }
        }

        if (memoryCache == null) memoryCache = new MemoryCacheItem[maxMemoryCacheSize * 10];
        else if (memoryCache.Length == countMemoryItems) Array.Resize(ref memoryCache, memoryCache.Length + 50);

        MemoryCacheItem item = new MemoryCacheItem(tileKey, tile);
        memoryCache[countMemoryItems++] = item;
        memoryCacheSize += item.size;
        if (memoryCacheSize > maxMemoryCacheSize * 1000000) UnloadOldMemoryCacheItems();
    }

    /// <summary>
    /// Clear memory cache.
    /// </summary>
    public void ClearMemoryCache()
    {
        for (int i = 0; i < countMemoryItems; i++)
        {
            memoryCache[i].Dispose();
            memoryCache[i] = null;
        }
        memoryCacheSize = 0;
        countMemoryItems = 0;
    }

    private static string GetTileKey(OnlineMapsTile tile)
    {
        StringBuilder stringBuilder = GetStringBuilder();

        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;

        stringBuilder.Append(rTile.mapType.fullID).Append(tile.key).Append(rTile.labels).Append(rTile.language);
        return stringBuilder.ToString();
    }

    private static string GetTrafficKey(OnlineMapsTile tile)
    {
        StringBuilder stringBuilder = GetStringBuilder();

        stringBuilder.Append("traffic").Append(tile.key);
        return stringBuilder.ToString();
    }

    private void UnloadOldMemoryCacheItems()
    {
        int countUnload = Mathf.RoundToInt(countMemoryItems * memoryCacheUnloadRate);
        if (countUnload == 0) return;
        if (countUnload < 0) throw new Exception("Can not unload a negative number of items. Check memoryCacheUnloadRate.");
        if (countMemoryItems < countUnload) countUnload = countMemoryItems;

        long[] unloadTimes = new long[countUnload];
        int[] unloadIndices = new int[countUnload];
        int c = 0;

        for (int i = 0; i < countMemoryItems; i++)
        {
            long t = memoryCache[i].time;
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
            MemoryCacheItem mci = memoryCache[index];
            if (mci.tileUsed) continue;

            memoryCacheSize -= mci.size;
            mci.Dispose();
            memoryCache[index] = null;
        }

        int offset = 0;
        for (int i = 0; i < countMemoryItems; i++)
        {
            if (memoryCache[i] == null) offset++;
            else if (offset > 0) memoryCache[i - offset] = memoryCache[i];
        }

        countMemoryItems -= countUnload;
    }

    private class MemoryCacheItem : CacheItem
    {
        private OnlineMapsTile tile;

        public bool tileUsed
        {
            get { return tile.used; }
        }

        public MemoryCacheItem(string key, OnlineMapsTile tile)
        {
            this.key = key;
            hash = key.GetHashCode();
            size = 30000;
            time = DateTime.Now.Ticks;
            this.tile = tile;
            tile.Block(this);
        }

        public void Dispose()
        {
            tile.Unblock(this);
            tile = null;
            key = null;
        }

        public override string ToString()
        {
            return key;
        }
    }
}