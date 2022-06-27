/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if !UNITY_WEBGL
using System.Threading;
#endif

/// <summary>
/// This class is responsible for drawing the map.<br/>
/// <strong>Please do not use it if you do not know what you're doing.</strong><br/>
/// Perform all operations with the map through other classes.
/// </summary>
public class OnlineMapsBuffer
{
    public bool allowUnloadTiles = true;

    /// <summary>
    /// Reference to OnlineMaps.
    /// </summary>
    public OnlineMaps map;

    /// <summary>
    /// Position the tile, which begins buffer.
    /// </summary>
    public OnlineMapsVector2i bufferPosition;

    public Color32[] frontBuffer;

    public OnlineMapsVector2i frontBufferPosition;

    /// <summary>
    /// Height of the buffer.
    /// </summary>
    public int height;

    /// <summary>
    /// The current status of the buffer.
    /// </summary>
    public OnlineMapsBufferStatus status = OnlineMapsBufferStatus.wait;

    /// <summary>
    /// Width of the buffer.
    /// </summary>
    public int width;

    /// <summary>
    /// List of tiles that are already loaded, but not yet applied to the buffer.
    /// </summary>
    private List<OnlineMapsTile> newTiles;

    private Color32[] backBuffer;

    private bool disposed;
    public bool needUnloadTiles;

    public StateProps lastState;
    public StateProps renderState;

    /// <summary>
    /// The coordinates of the top-left the point of map that displays.
    /// </summary>
    public Vector2 topLeftPosition
    {
        get
        {
            int countX = renderState.width / OnlineMapsUtils.tileSize;
            int countY = renderState.height / OnlineMapsUtils.tileSize;

            double px, py;
            map.projection.CoordinatesToTile(renderState.longitude, renderState.latitude, renderState.zoom, out px, out py);

            px -= countX / 2f;
            py -= countY / 2f;

            map.projection.TileToCoordinates(px, py, renderState.zoom, out px, out py);
            return new Vector2((float)px, (float)py);
        }
    }

    public OnlineMapsBuffer(OnlineMaps map)
    {
        this.map = map;

        lastState = new StateProps
        {
            floatZoom = map.floatZoom,
            width = map.width,
            height = map.height
        };

        map.GetPosition(out lastState.longitude, out lastState.latitude);
        map.GetCorners(out lastState.leftLongitude, out lastState.topLatitude, out lastState.rightLongitude, out lastState.bottomLatitude);
        renderState = lastState;

        newTiles = new List<OnlineMapsTile>();
    }

    private void ApplyNewTiles()
    {
        if (newTiles == null || newTiles.Count == 0) return;

        lock (newTiles)
        {
            foreach (OnlineMapsTile tile in newTiles)
            {
                if (disposed) return;
                if (tile.status == OnlineMapsTileStatus.disposed) continue;

                OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;

#if !UNITY_WEBGL
                int counter = 20;
                while (rTile.colors.Length < OnlineMapsUtils.sqrTileSize && counter > 0)
                {
                    OnlineMapsUtils.ThreadSleep(1);
                    counter--;
                }
#endif
                rTile.ApplyColorsToChilds();
            }
            if (newTiles.Count > 0) newTiles.Clear();
        }
    }

    /// <summary>
    /// Adds a tile into the buffer.
    /// </summary>
    /// <param name="tile">Tile</param>
    public void ApplyTile(OnlineMapsTile tile)
    {
        if (newTiles == null) newTiles = new List<OnlineMapsTile>();
        lock (newTiles)
        {
            newTiles.Add(tile);
        }
    }

    private List<OnlineMapsTile> CreateParents(List<OnlineMapsTile> tiles, int zoom)
    {
        List<OnlineMapsTile> newParentTiles = new List<OnlineMapsTile>(tiles.Count);

        for (int i = 0; i < tiles.Count; i++)
        {
            OnlineMapsTile tile = tiles[i];
            if (tile.parent == null) CreateTileParent(zoom, tile, newParentTiles);
            else newParentTiles.Add(tile.parent);

            tile.used = true;
            tile.parent.used = true;
        }

        return newParentTiles;
    }

    private void CreateTileParent(int zoom, OnlineMapsTile tile, List<OnlineMapsTile> newParentTiles)
    {
        int px = tile.x / 2;
        int py = tile.y / 2;

        OnlineMapsTile parent;
        if (!map.tileManager.GetTile(zoom, px, py, out parent))
        {
            parent = map.control.CreateTile(px, py, zoom);
            OnlineMapsRasterTile rParent = parent as OnlineMapsRasterTile;
            if (rParent != null) rParent.OnSetColor = OnTileSetColor;
        }

        newParentTiles.Add(parent);
        parent.used = true;
        tile.SetParent(parent);
    }

    /// <summary>
    /// Dispose of buffer.
    /// </summary>
    public void Dispose()
    {
        try
        {
            map.tileManager.Reset();

            frontBuffer = null;
            backBuffer = null;
            map = null;

            status = OnlineMapsBufferStatus.disposed;
            newTiles = null;
            disposed = true;
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }

    public void GenerateFrontBuffer()
    {
        try
        {
            lastState = new StateProps
            {
                floatZoom = map.floatZoom,
                width = map.width,
                height = map.height
            };

            map.GetPosition(out lastState.longitude, out lastState.latitude);
            map.GetCorners(out lastState.leftLongitude, out lastState.topLatitude, out lastState.rightLongitude, out lastState.bottomLatitude);

            while (!disposed)
            {
#if !UNITY_WEBGL
                while (status != OnlineMapsBufferStatus.start && map.renderInThread)
                {
                    if (disposed) return;
                    OnlineMapsUtils.ThreadSleep(1);
                }
#endif

                status = OnlineMapsBufferStatus.working;

                renderState = new StateProps
                {
                    floatZoom = map.floatZoom,
                    width = map.width,
                    height = map.height
                };

                try
                {
                    map.GetPosition(out renderState.longitude, out renderState.latitude);
                    map.GetCorners(out renderState.leftLongitude, out renderState.topLatitude, out renderState.rightLongitude, out renderState.bottomLatitude);
                    
                    if (newTiles != null && map.control.resultIsTexture) ApplyNewTiles();

                    if (disposed) return;

                    UpdateBackBuffer();
                    if (disposed) return;

                    if (map.control.resultIsTexture)
                    {
                        GetFrontBufferPosition();
                        UpdateFrontBuffer();

                        if (disposed) return;

                        foreach (OnlineMapsDrawingElement element in map.control.drawingElementManager)
                        {
                            if (disposed) return;
                            element.Draw(frontBuffer, new Vector2(bufferPosition.x + (float)frontBufferPosition.x / OnlineMapsUtils.tileSize, bufferPosition.y + (float)frontBufferPosition.y / OnlineMapsUtils.tileSize), renderState.width, renderState.height, renderState.floatZoom);
                        }

                        if (map.control.OnDrawMarkers != null) map.control.OnDrawMarkers();
                    }
                }
                catch (Exception exception)
                {
                    if (disposed) return;
                    Debug.Log(exception.Message + "\n" + exception.StackTrace);
                }

                status = OnlineMapsBufferStatus.complete;

                lastState = renderState;
#if !UNITY_WEBGL
                if (!map.renderInThread) break;
#else
                break;
#endif
            }
        }
        catch
        {
        }
    }

    public void GetCorners(out double tlx, out double tly, out double brx, out double bry)
    {
        int countX = renderState.width / OnlineMapsUtils.tileSize;
        int countY = renderState.height / OnlineMapsUtils.tileSize;

        map.projection.CoordinatesToTile(renderState.longitude, renderState.latitude, renderState.zoom, out tlx, out tly);

        float coof = renderState.zoomCoof;

        brx = tlx + countX / 2f * coof;
        bry = tly + countY / 2f * coof;
        tlx -= countX / 2f * coof;
        tly -= countY / 2f * coof;

        map.projection.TileToCoordinates(tlx, tly, renderState.zoom, out tlx, out tly);
        map.projection.TileToCoordinates(brx, bry, renderState.zoom, out brx, out bry);

        long max = (1L << renderState.zoom) * OnlineMapsUtils.tileSize;
        if (max == renderState.width && Math.Abs(coof) < float.Epsilon)
        {
            double lng = renderState.longitude + 180;
            tlx = lng + 0.001;
            if (tlx > 180) tlx -= 360;

            brx = lng - 0.001;
            if (brx > 180) brx -= 360;
        }
    }

    private void GetFrontBufferPosition()
    {
        double px, py;

        // Tile of center position
        map.projection.CoordinatesToTile(renderState.longitude, renderState.latitude, renderState.zoom, out px, out py);

        int countX = renderState.width / OnlineMapsUtils.tileSize;
        int countY = renderState.height / OnlineMapsUtils.tileSize;

        // Tile center position in the backbuffer
        px -= bufferPosition.x;
        py -= bufferPosition.y;

        // Top-left frontbuffer tile in the backbuffer
        px -= countX / 2f * renderState.zoomCoof;
        py -= countY / 2f * renderState.zoomCoof;

        // Top-left frontbuffer pixel in the backbuffer
        int ix = (int) (px * OnlineMapsUtils.tileSize);
        int iy = (int) (py * OnlineMapsUtils.tileSize);

        if (iy < 0) iy = 0;
        else if (iy >= (int)(height - renderState.height * renderState.zoomCoof)) iy = (int)(height - renderState.height * renderState.zoomCoof);

        frontBufferPosition = new OnlineMapsVector2i(ix, iy);
    }

    private void InitTile(int zoom, OnlineMapsVector2i pos, int maxY, List<OnlineMapsTile> newBaseTiles, int y, int px)
    {
        int py = y + pos.y;
        if (py < 0 || py >= maxY) return;

        OnlineMapsTile tile;

        if (!map.tileManager.GetTile(zoom, px, py, out tile))
        {
            OnlineMapsTile parent = null;

            if (renderState.zoom - zoom > map.countParentLevels)
            {
                int ptx = px / 2;
                int pty = py / 2;
                if (map.tileManager.GetTile(zoom - 1, ptx, pty, out parent)) parent.used = true;
            }

            tile = map.control.CreateTile(px, py, zoom);
            tile.parent = parent;
            if (tile is OnlineMapsRasterTile) (tile as OnlineMapsRasterTile).OnSetColor = OnTileSetColor;
        }

        newBaseTiles.Add(tile);
        tile.used = true;
    }

    private void InitTiles(int zoom, int countX, OnlineMapsVector2i pos, int countY, int maxY, List<OnlineMapsTile> newBaseTiles)
    {
        int maxX = 1 << renderState.zoom;
        for (int x = 0; x < countX; x++)
        {
            int px = x + pos.x;
            if (px < 0) px += maxX;
            else if (px >= maxX) px -= maxX;

            for (int y = 0; y < countY; y++) InitTile(zoom, pos, maxY, newBaseTiles, y, px);
        }
    }

    private void OnTileSetColor(OnlineMapsRasterTile tile)
    {
        if (tile.zoom == renderState.zoom) SetBufferTile(tile);
    }

    private Rect SetBufferTile(OnlineMapsTile tile, int? offsetX = null)
    {
        if (!map.control.resultIsTexture) return default(Rect);

        const int s = OnlineMapsUtils.tileSize;
        int i = 0;
        int px = tile.x - bufferPosition.x;
        int py = tile.y - bufferPosition.y;

        int maxX = 1 << tile.zoom;

        if (px < 0) px += maxX;
        else if (px >= maxX) px -= maxX;

        if (renderState.width == maxX * s && px < 2 && !offsetX.HasValue) SetBufferTile(tile, maxX);

        if (offsetX.HasValue) px += offsetX.Value;

        px *= s;
        py *= s;

        if (px + s < 0 || py + s < 0 || px > width || py > height) return new Rect(0, 0, 0, 0);

        if (!tile.hasColors || tile.status != OnlineMapsTileStatus.loaded)
        {
            const int hs = s / 2;
            int sx = tile.x % 2 * hs;
            int sy = tile.y % 2 * hs;
            if (SetBufferTileFromParent(tile, px, py, s / 2, sx, sy)) return new Rect(px, py, s, s);
        }

        OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;
        Color32[] colors = rTile.colors;

        lock (colors)
        {
            int maxSize = width * height;

            for (int y = py + s - 1; y >= py; y--)
            {
                int bp = y * width + px;
                if (bp + s < 0 || bp >= maxSize) continue;
                int l = s;
                if (bp < 0)
                {
                    l -= bp;
                    bp = 0;
                }
                else if (bp + s > maxSize)
                {
                    l -= maxSize - (bp + s);
                    bp = maxSize - s - 1;
                }

                try
                {
                    Array.Copy(colors, i, backBuffer, bp, l);
                }
                catch
                {
                }

                i += s;
            }

            return new Rect(px, py, OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize);
        }
    }

    private bool SetBufferTileFromParent(OnlineMapsTile tile, int px, int py, int size, int sx, int sy)
    {
        OnlineMapsTile parent = tile.parent;
        if (parent == null) return false;

        const int s = OnlineMapsUtils.tileSize;
        const int hs = s / 2;

        if (parent.status != OnlineMapsTileStatus.loaded || !parent.hasColors)
        {
            sx = sx / 2 + parent.x % 2 * hs;
            sy = sy / 2 + parent.y % 2 * hs;
            return SetBufferTileFromParent(parent, px, py, size / 2, sx, sy);
        }

        OnlineMapsRasterTile rParent = parent as OnlineMapsRasterTile;
        Color32[] colors = rParent.colors;
        int scale = s / size;

        if (colors.Length != OnlineMapsUtils.sqrTileSize) return false;

        int ry = s - sy - 1;

        lock (colors)
        {
            if (size == hs)
            {
                for (int y = 0; y < hs; y++)
                {
                    int oys = (ry - y) * s + sx;
                    int bp = (y * 2 + py) * width + px;
                    for (int x = 0; x < hs; x++)
                    {
                        Color32 clr = colors[oys + x];

                        backBuffer[bp] = clr;
                        backBuffer[bp + width] = clr;
                        backBuffer[++bp] = clr;
                        backBuffer[bp + width] = clr;
                        bp++;
                    }
                }
            }
            else
            {
                for (int y = 0; y < size; y++)
                {
                    int oys = (ry - y) * s + sx;
                    int scaledY = y * scale + py;
                    for (int x = 0; x < size; x++)
                    {
                        Color32 clr = colors[oys + x];
                        int scaledX = x * scale + px;

                        for (int by = scaledY; by < scaledY + scale; by++)
                        {
                            int bpy = by * width + scaledX;
                            for (int bx = bpy; bx < bpy + scale; bx++) backBuffer[bx] = clr;
                        }
                    }
                }
            }
        }

        return true;
    }

    public void SetColorToBuffer(Color clr, OnlineMapsVector2i ip, int y, int x)
    {
        if (Math.Abs(clr.a) < float.Epsilon) return;
        int bufferIndex = (renderState.height - ip.y - y) * renderState.width + ip.x + x;
        if (clr.a < 1)
        {
            float alpha = clr.a;
            Color bufferColor = frontBuffer[bufferIndex];
            clr.a = 1;
            clr.r = Mathf.Lerp(bufferColor.r, clr.r, alpha);
            clr.g = Mathf.Lerp(bufferColor.g, clr.g, alpha);
            clr.b = Mathf.Lerp(bufferColor.b, clr.b, alpha);
        }
        frontBuffer[bufferIndex] = clr;
    }

    public void UnloadOldTiles()
    {
        needUnloadTiles = false;

#if !UNITY_WEBGL
        int count = 100;

        while (map.renderInThread && !allowUnloadTiles && count > 0)
        {
            OnlineMapsUtils.ThreadSleep(1);
            count--;
        }

        if (count == 0) return;
#endif
        lock (OnlineMapsTile.lockTiles)
        {
            foreach (OnlineMapsTile tile in map.tileManager.tiles)
            {
                if (!tile.used && !tile.isBlocked && tile.map == map) tile.Dispose();
            }
        }
    }

    public void UnloadOldTypes()
    {
        try
        {
            lock (OnlineMapsTile.lockTiles)
            {
                foreach (OnlineMapsTile tile in map.tileManager.tiles)
                {
                    OnlineMapsRasterTile rt = tile as OnlineMapsRasterTile;
                    if (rt != null && rt.map == map && map.activeType != rt.mapType)
                    {
                        tile.Dispose();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message);
        }
    }

    private void UpdateBackBuffer()
    {
        const int s = OnlineMapsUtils.tileSize;
        int countX = renderState.width / s + 2;
        int countY = renderState.height / s + 2;

        double cx, cy;
        map.projection.CoordinatesToTile(renderState.longitude, renderState.latitude, renderState.zoom, out cx, out cy);
        OnlineMapsVector2i pos = new OnlineMapsVector2i((int)cx - countX / 2, (int)cy - countY / 2);

        int max = 1 << renderState.zoom;

        if (pos.y < 0) pos.y = 0;
        else if (pos.y >= max - countY) pos.y = max - countY;

        if (map.control.resultIsTexture)
        {
            if (frontBuffer == null || frontBuffer.Length != renderState.width * renderState.height) frontBuffer = new Color32[renderState.width * renderState.height];
            if (backBuffer == null || width != countX * s || height != countY * s)
            {
                width = countX * s;
                height = countY * s;
                backBuffer = new Color32[height * width];
            }
        }

        bufferPosition = pos;

        List<OnlineMapsTile> newBaseTiles = new List<OnlineMapsTile>();

        lock (OnlineMapsTile.lockTiles)
        {
            for (int i = 0; i < map.tileManager.tiles.Count; i++) map.tileManager.tiles[i].used = false;

            InitTiles(renderState.zoom, countX, pos, countY, max, newBaseTiles);

            if (map.countParentLevels > 0)
            {
                List<OnlineMapsTile> newParentTiles = newBaseTiles;
                for (int z = renderState.zoom - 1; z >= Mathf.Max(renderState.zoom - map.countParentLevels, OnlineMaps.MINZOOM); z--) newParentTiles = CreateParents(newParentTiles, z);
            }

            if (map.control.resultIsTexture) for (int i = 0; i < newBaseTiles.Count; i++) SetBufferTile(newBaseTiles[i]);
        }

        needUnloadTiles = true;
    }

    private void UpdateFrontBuffer()
    {
        float zoomCoof = renderState.zoomCoof;
        int w = renderState.width;
        int h = renderState.height;
        int bufferSize = height * width;

        for (int y = 0; y < h; y++)
        {
            float fy = y * zoomCoof + frontBufferPosition.y;
            int iy1 = (int) fy;
            int iyw1 = iy1 * width;
            int iyw2 = iyw1 + width + 1;
            if (iyw2 >= bufferSize - 1) continue;
            
            int fby = (h - y - 1) * w;
            float fx = frontBufferPosition.x;

            for (int x = 0; x < w; x++)
            {
                Color32 clr1 = backBuffer[iyw1 + (int)fx];
                frontBuffer[fby++] = clr1;
                fx += zoomCoof;
            }
        }
    }

    /// <summary>
    /// The main properties of the map
    /// </summary>
    public struct StateProps
    {
        /// <summary>
        /// Longitude of the center point
        /// </summary>
        public double longitude;

        /// <summary>
        /// Latitude of the center point
        /// </summary>
        public double latitude;

        /// <summary>
        /// Latitude of the top border
        /// </summary>
        public double topLatitude;

        /// <summary>
        /// Longitude of the left border
        /// </summary>
        public double leftLongitude;

        /// <summary>
        /// Latitude of the bottom border
        /// </summary>
        public double bottomLatitude;

        /// <summary>
        /// Longitude of the right border
        /// </summary>
        public double rightLongitude;

        /// <summary>
        /// Zoom
        /// </summary>
        public int zoom;

        /// <summary>
        /// Width of the map
        /// </summary>
        public int width;

        /// <summary>
        /// Height of the map
        /// </summary>
        public int height;

        /// <summary>
        /// The scaling factor for zoom
        /// </summary>
        public float zoomCoof;

        /// <summary>
        /// The fractional part of zoom
        /// </summary>
        public float zoomScale;

        private float _floatZoom;

        /// <summary>
        /// Float zoom
        /// </summary>
        public float floatZoom
        {
            get { return _floatZoom; }
            set
            {
                _floatZoom = value;
                zoom = (int) value;
                zoomScale = _floatZoom - zoom;
                zoomCoof = 1 - zoomScale / 2;
            }
        }
    }
}