/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

public class OnlineMapsMapboxTile : OnlineMapsVectorTile
{
    private const float size = 256;

    private Material baseMaterial;
    private int renderQueueOffset;

    public override string url
    {
        get { return "https://b.tiles.mapbox.com/v4/mapbox.mapbox-terrain-v2,mapbox.mapbox-streets-v8/" + zoom + "/" + x + "/" + y + ".vector.pbf?access_token=" + OnlineMapsKeyManager.Mapbox(); }
    }

    public OnlineMapsMapboxTile(int x, int y, int zoom, OnlineMaps map, bool isMapTile = true) : base(x, y, zoom, map, isMapTile)
    {
    }

    protected override void LoadTileFromWWW(OnlineMapsWWW www)
    {
        status = OnlineMapsTileStatus.loaded;

        byte[] decompressed = OnlineMapsZipDecompressor.Decompress(www.bytes);
        Read(decompressed);

        MarkLoaded();
        map.Redraw();
    }
}