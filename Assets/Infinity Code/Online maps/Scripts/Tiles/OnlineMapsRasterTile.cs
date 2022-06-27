/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Implements work with raster tiles
/// </summary>
public class OnlineMapsRasterTile : OnlineMapsTile
{
    /// <summary>
    /// Buffer default colors.
    /// </summary>
    public static Color32[] defaultColors;

    /// <summary>
    /// The texture that is used until the tile is loaded.
    /// </summary>
    public static Texture2D emptyColorTexture;

    /// <summary>
    /// Labels is used in tile?
    /// </summary>
    public bool labels;

    /// <summary>
    /// Language is used in tile?
    /// </summary>
    public string language;

    public OnlineMapsProvider.MapType mapType;

    /// <summary>
    /// This event occurs when the tile gets colors based on parent colors.
    /// </summary>
    public Action<OnlineMapsRasterTile> OnSetColor;

    /// <summary>
    /// Traffic texture.
    /// </summary>
    public Texture2D trafficTexture;

    /// <summary>
    /// Instance of the traffic loader.
    /// </summary>
    public OnlineMapsWWW trafficWWW;

    private Color32[] _colors;
    private OnlineMapsTrafficProvider _trafficProvider;
    private string _trafficURL;
    private byte[] labelData;
    private Color32[] labelColors;
    public Color32[] mergedColors;


    /// <summary>
    /// Array of colors of the tile.
    /// </summary>
    public Color32[] colors
    {
        get
        {
            if (mergedColors != null) return mergedColors;
            if (_colors != null) return _colors;
            return defaultColors;
        }
        set
        {
            _colors = value;
            hasColors = _colors != null;
        }
    }

    /// <summary>
    /// Provider of the traffic textures
    /// </summary>
    public OnlineMapsTrafficProvider trafficProvider
    {
        get { return _trafficProvider; }
        set
        {
            _trafficProvider = value;
            _trafficURL = null;
        }
    }

    /// <summary>
    ///  URL of the traffic texture
    /// </summary>
    public string trafficURL
    {
        get
        {
            if (string.IsNullOrEmpty(_trafficURL))
            {
                if (trafficProvider.isCustom) _trafficURL = Regex.Replace(map.customTrafficProviderURL, @"{\w+}", CustomTrafficProviderReplaceToken);
                else _trafficURL = trafficProvider.GetURL(this);
            }
            return _trafficURL;
        }
        set { _trafficURL = value; }
    }

    public override string url
    {
        get
        {
            if (string.IsNullOrEmpty(_url))
            {
                if (mapType.isCustom) _url = Regex.Replace(map.customProviderURL, @"{\w+}", CustomProviderReplaceToken);
                else _url = mapType.GetURL(this);
            }
            return _url;
        }
    }

    public OnlineMapsRasterTile(int x, int y, int zoom, OnlineMaps map, bool isMapTile = true) : base(x, y, zoom, map, isMapTile)
    {
        _trafficProvider = map.trafficProvider;
        mapType = map.activeType;

        labels = map.labels;
        language = map.language;
    }

    public void ApplyColorsToChilds()
    {
        if (OnSetColor != null) OnSetColor(this);
    }

    private void ApplyLabelTexture()
    {
        Texture2D t = new Texture2D(OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize);
        t.LoadImage(labelData);
        labelData = null;
        labelColors = t.GetPixels32();

        if (map.control.resultIsTexture)
        {
#if !UNITY_WEBGL
            if (map.renderInThread) OnlineMapsThreadManager.AddThreadAction(MergeColors);
            else MergeColors();
#else
            MergeColors();
#endif
            OnlineMapsUtils.Destroy(t);
        }
        else
        {
            _colors = texture.GetPixels32();
            MergeColors();
            t.SetPixels32(mergedColors);
            texture = t;
            mergedColors = null;
        }
    }

    public void ApplyTexture(Texture2D texture)
    {
        _colors = texture.GetPixels32();
        status = OnlineMapsTileStatus.loaded;
        hasColors = true;
    }

    /// <summary>
    /// Checks the size of the tile texture.
    /// </summary>
    /// <param name="texture">Tile texture</param>
    public void CheckTextureSize(Texture2D texture)
    {
        if (texture == null) return;
        if (map.control.resultIsTexture && mapType.isCustom && (texture.width != 256 || texture.height != 256))
        {
            Debug.LogError(string.Format("Size tiles {0}x{1}. Expected to 256x256. Please check the URL.", texture.width, texture.height));
            status = OnlineMapsTileStatus.error;
        }
    }

    private string CustomTrafficProviderReplaceToken(Match match)
    {
        string v = match.Value.ToLower().Trim('{', '}');

        if (OnReplaceTrafficURLToken != null)
        {
            string ret = OnReplaceTrafficURLToken(this, v);
            if (ret != null) return ret;
        }

        if (v == "zoom") return zoom.ToString();
        if (v == "x") return x.ToString();
        if (v == "y") return y.ToString();
        if (v == "quad") return OnlineMapsUtils.TileToQuadKey(x, y, zoom);
        return v;
    }

    public override void Destroy()
    {
        base.Destroy();

        if (trafficTexture != null)
        {
            OnlineMapsUtils.Destroy(trafficTexture);
            trafficTexture = null;
        }

        mapType = null;
        _trafficProvider = null;
        _colors = null;
        mergedColors = null;
        labelData = null;
        labelColors = null;
        OnSetColor = null;
    }

    public override void DownloadComplete()
    {
        base.DownloadComplete();

        if (www == null) Debug.Log(status + "  " + this);
        else
        {
            data = www.bytes;
            LoadTexture();
            data = null;
        }
    }

    public void LoadTexture()
    {
        if (status == OnlineMapsTileStatus.error) return;

        Texture2D texture = new Texture2D(OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize);
        if (map.useSoftwareJPEGDecoder) LoadTexture(texture, data);
        else
        {
            texture.LoadImage(data);
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        CheckTextureSize(texture);

        if (status != OnlineMapsTileStatus.error)
        {
            ApplyTexture(texture);
            if (labelData != null) ApplyLabelTexture();
        }
        OnlineMapsUtils.Destroy(texture);
    }

    public static void LoadTexture(Texture2D texture, byte[] bytes)
    {
        if (bytes[0] == 0xFF)
        {
            Color32[] colors = OnlineMapsJPEGDecoder.GetColors(bytes);
            texture.SetPixels32(colors);
            texture.Apply();
        }
        else texture.LoadImage(bytes);
    }

    protected override void LoadTileFromWWW(OnlineMapsWWW www)
    {
        if (map == null) return;

        if (map.control.resultIsTexture)
        {
            DownloadComplete();
            if (status != OnlineMapsTileStatus.error) map.buffer.ApplyTile(this);
        }
        else
        {
            Texture2D tileTexture = new Texture2D(256, 256, TextureFormat.RGB24, map.control.mipmapForTiles)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            if (map.useSoftwareJPEGDecoder) LoadTexture(tileTexture, www.bytes);
            else www.LoadImageIntoTexture(tileTexture);

            tileTexture.name = zoom + "x" + x + "x" + y;

            CheckTextureSize(tileTexture);

            if (status != OnlineMapsTileStatus.error && status != OnlineMapsTileStatus.disposed)
            {
                texture = tileTexture;
                OnlineMapsTileSetControl tsControl = map.control as OnlineMapsTileSetControl;
                if (tsControl != null && tsControl.compressTextures) texture.Compress(true);
                status = OnlineMapsTileStatus.loaded;
            }
        }

        if (status != OnlineMapsTileStatus.error && status != OnlineMapsTileStatus.disposed)
        {
            if (OnTileDownloaded != null) OnTileDownloaded(this);
        }

        MarkLoaded();
        map.Redraw();
    }

    private void MergeColors()
    {
        try
        {
            if (status == OnlineMapsTileStatus.error || status == OnlineMapsTileStatus.disposed) return;
            if (labelColors == null || _colors == null || labelColors.Length != _colors.Length) return;
            
            Color32[] mColors = new Color32[_colors.Length];

            for (int i = 0; i < _colors.Length; i++)
            {
                Color32 lColor = labelColors[i];
                float a = lColor.a;
                if (a > 0)
                {
                    mColors[i] = Color32.Lerp(_colors[i], lColor, a);
                    mColors[i].a = 255;
                }
                else mColors[i] = _colors[i];
            }

            mergedColors = mColors;
            labelColors = null;
        }
        catch
        {
        }
    }

    public bool OnLabelDownloadComplete()
    {
        labelData = trafficWWW.bytes;
        if (status == OnlineMapsTileStatus.loaded)
        {
            ApplyLabelTexture();
            return true;
        }
        return false;
    }

    public bool SetLabelData(byte[] bytes)
    {
        labelData = bytes;
        if (status == OnlineMapsTileStatus.loaded)
        {
            ApplyLabelTexture();
            return true;
        }
        return false;
    }
}