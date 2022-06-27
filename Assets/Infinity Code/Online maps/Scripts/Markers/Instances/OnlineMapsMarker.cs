/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;
using UnityEngine.Serialization;
#if !UNITY_WEBGL
using System.Threading;
#endif

/// <summary>
/// 2D marker class.
/// </summary>
[Serializable]
public class OnlineMapsMarker : OnlineMapsMarkerBase
{
    /// <summary>
    /// Marker texture align.
    /// </summary>
    public OnlineMapsAlign align = OnlineMapsAlign.Bottom;

    /// <summary>
    /// Specifies to OnlineMapsBuffer that the marker is available for drawing.<br/>
    /// <strong>Please do not use.</strong>
    /// </summary>
    public bool locked;

    /// <summary>
    /// Relative area of ​​activity of the marker.
    /// </summary>
    public Rect markerColliderRect = new Rect(-0.5f, -0.5f, 1, 1);

    private Color32[] _colors;
    private int _height;

    [SerializeField]
    private float _rotation = 0;

    [SerializeField, FormerlySerializedAs("texture")]
    private Texture2D _texture;

    private Color32[] _rotatedColors;
    private int _textureHeight;
    private int _textureWidth;
    private int _width;
    private float _lastRotation;
    private float _lastScale;

    /// <summary>
    /// Gets the marker colors.
    /// </summary>
    /// <value>
    /// The colors.
    /// </value>
    public Color32[] colors
    {
        get
        {
            if (!map.control.resultIsTexture) return _colors;
            if (Math.Abs(_rotation) < float.Epsilon && Math.Abs(scale - 1) < float.Epsilon) return _colors;

            if (Math.Abs(_lastRotation - _rotation) > float.Epsilon || Math.Abs(_lastScale - _scale) > float.Epsilon) UpdateRotatedBuffer();
            return _rotatedColors;
        }
    }

    /// <summary>
    /// Gets or sets marker enabled.
    /// </summary>
    /// <value>
    /// true if enabled, false if not.
    /// </value>
    public override bool enabled
    {
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                if (OnEnabledChange != null) OnEnabledChange(this);
                if (OnlineMaps.isPlaying) map.Redraw();
            }
        }
    }

    /// <summary>
    /// Gets the marker height.
    /// </summary>
    /// <value>
    /// The height.
    /// </value>
    public int height
    {
        get { return _height; }
    }

    /// <summary>
    /// Area of ​​the screen, which is a marker after map will be redrawed.
    /// Note: When used as a source display Texture or Tileset, is not returned the correct value.
    /// </summary>
    /// <value>
    /// Marker rectangle.
    /// </value>
    public Rect realScreenRect
    {
        get
        {
            Rect controlRect = map.control.GetRect();
            Rect uvRect = map.control.uvRect;
            controlRect.width /= uvRect.width;
            controlRect.height /= uvRect.height;
            controlRect.x -= controlRect.width * uvRect.x;
            controlRect.y -= controlRect.height * uvRect.y;

            double tlx, tly;
            map.GetTopLeftPosition(out tlx, out tly);
            map.projection.CoordinatesToTile(tlx, tly, map.zoom, out tlx, out tly);
            tlx *= OnlineMapsUtils.tileSize;
            tly *= OnlineMapsUtils.tileSize;

            double tx, ty;
            map.projection.CoordinatesToTile(longitude, latitude, map.zoom, out tx, out ty);

            tx *= OnlineMapsUtils.tileSize;
            ty *= OnlineMapsUtils.tileSize;

            Vector2 pos = GetAlignedPosition((int)tx, (int)ty);
            float scaleX = controlRect.width / map.width;
            float scaleY = controlRect.height / map.height;

            pos.x = Mathf.RoundToInt((float)(pos.x - tlx) * scaleX + controlRect.x);
            pos.y = Mathf.RoundToInt(controlRect.yMax - (float)(pos.y - tly + height) * scaleY);

            return new Rect(pos.x, pos.y, width * scaleX, height * scaleY);
        }
    }

    /// <summary>
    /// Gets or sets the rotation of the marker (0-1). 
    /// </summary>
    /// <value>
    /// The rotation.
    /// </value>
    public float rotation
    {
        get { return _rotation; }
        set
        {
            if (Math.Abs(_rotation - value) > float.Epsilon)
            {
                _rotation = value;
                if (OnlineMaps.isPlaying)
                {
                    if (!(map.control is OnlineMapsTileSetControl)) UpdateRotatedBuffer();
                    map.Redraw();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the rotation of the marker in degree (0-360)
    /// </summary>
    public float rotationDegree
    {
        get { return _rotation * 360; }
        set { rotation = value / 360; }
    }

    public override float scale
    {
        get { return _scale; }
        set
        {
            if (Math.Abs(_scale - value) < float.Epsilon) return;

            _scale = value;
            if (OnlineMaps.isPlaying) UpdateRotatedBuffer();
        }
    }

    /// <summary>
    /// Area of ​​the screen, which is a marker at the current map display.
    /// Note: When used as a source display Texture or Tileset, is not returned the correct value.
    /// </summary>
    /// <value>
    /// Marker rectangle.
    /// </value>
    public Rect screenRect
    {
        get
        {
            return GetRect();
        }
    }

    /// <summary>
    /// Texture marker. <br/>
    /// Texture format: ARGB32.<br/>
    /// <strong>Must enable "Read / Write enabled".</strong><br/>
    /// After changing the texture you need to call OnlineMapsMarker.Init.
    /// </summary>
    public Texture2D texture
    {
        get { return _texture; }
        set
        {
            _texture = value;
            if (map != null)
            {
                Init();
                map.Redraw();
            }
        }
    }

    /// <summary>
    /// Gets the marker width.
    /// </summary>
    /// <value>
    /// The width.
    /// </value>
    public int width
    {
        get
        {
            return _width;
        }
    }

    public OnlineMapsMarker()
    {
        
    }

    /// <summary>
    /// Gets aligned position.
    /// </summary>
    /// <param name="pos">
    /// Buffer position.
    /// </param>
    /// <returns>
    /// The aligned buffer position.
    /// </returns>
    public OnlineMapsVector2i GetAlignedPosition(OnlineMapsVector2i pos)
    {
        return GetAlignedPosition(pos.x, pos.y);
    }

    /// <summary>
    /// Gets aligned position.
    /// </summary>
    /// <param name="px">Buffer position X</param>
    /// <param name="py">Buffer position Y</param>
    /// <returns>The aligned buffer position.</returns>
    public OnlineMapsVector2i GetAlignedPosition(int px, int py)
    {
        OnlineMapsVector2i offset = GetAlignOffset();

        if (Math.Abs(_lastRotation - _rotation) > float.Epsilon || Math.Abs(_lastScale - _scale) > float.Epsilon) UpdateRotatedBuffer();
        if (Math.Abs(_rotation) < float.Epsilon && Math.Abs(scale - 1) < float.Epsilon)
        {
            return new OnlineMapsVector2i(px - offset.x, py - offset.y);
        }

        float angle = 1 - Mathf.Repeat(_rotation * 360, 360);
        Matrix4x4 matrix = new Matrix4x4();

        matrix.SetTRS(new Vector3(_width >> 1, 0, _height >> 1), Quaternion.Euler(0, angle, 0), new Vector3(scale, scale, scale));
        Vector3 off = matrix.MultiplyPoint(new Vector3(offset.x - (_textureWidth >> 1), 0, offset.y - (_textureHeight >> 1)));
        px -= (int)off.x;
        py -= (int)off.z;

        return new OnlineMapsVector2i(px, py); ;
    }

    /// <summary>
    /// Gets aligned offset (in pixels).
    /// </summary>
    /// <returns>Aligned offset.</returns>
    public OnlineMapsVector2i GetAlignOffset()
    {
        OnlineMapsVector2i offset = new OnlineMapsVector2i();
        if (align == OnlineMapsAlign.BottomRight || align == OnlineMapsAlign.Right || align == OnlineMapsAlign.TopRight) offset.x = _textureWidth;
        else if (align == OnlineMapsAlign.Bottom || align == OnlineMapsAlign.Center || align == OnlineMapsAlign.Top) offset.x = _textureWidth / 2;

        if (align == OnlineMapsAlign.BottomRight || align == OnlineMapsAlign.Bottom || align == OnlineMapsAlign.BottomLeft) offset.y = _textureHeight;
        else if (align == OnlineMapsAlign.Left || align == OnlineMapsAlign.Center || align == OnlineMapsAlign.Right) offset.y = _textureHeight / 2;
        return offset;
    }

    private Rect GetRect()
    {
        Rect controlRect = map.control.GetRect();
        Rect uvRect = map.control.uvRect;
        controlRect.width /= uvRect.width;
        controlRect.height /= uvRect.height;
        controlRect.x -= controlRect.width * uvRect.x;
        controlRect.y -= controlRect.height * uvRect.y;

        int max = 1 << map.buffer.renderState.zoom;
        bool isEntireWorld = max == map.width / OnlineMapsUtils.tileSize;
        bool isBiggerThanBuffer = max - 2 == map.width / OnlineMapsUtils.tileSize;

        double tlx, tly;
        map.projection.CoordinatesToTile(map.buffer.lastState.leftLongitude, map.buffer.renderState.topLatitude, map.buffer.renderState.zoom, out tlx, out tly);
        float zoomCoof = map.zoomCoof;
        tlx *= OnlineMapsUtils.tileSize / zoomCoof;
        tly *= OnlineMapsUtils.tileSize / zoomCoof;

        double tx, ty;
        map.projection.CoordinatesToTile(longitude, latitude, map.buffer.renderState.zoom, out tx, out ty);
        tx *= OnlineMapsUtils.tileSize / zoomCoof;
        ty *= OnlineMapsUtils.tileSize / zoomCoof;

        Vector2 pos = GetAlignedPosition((int)tx, (int)ty);
        float scaleX = controlRect.width / map.width;
        float scaleY = controlRect.height / map.height;
        pos.x = Mathf.RoundToInt((float)(pos.x - tlx) * scaleX + controlRect.x);
        pos.y = Mathf.RoundToInt(controlRect.yMax - (float)(pos.y - tly + height) * scaleY);

        if (isEntireWorld)
        {
            if (pos.x < controlRect.x) pos.x += controlRect.width / zoomCoof;
        }
        else if (isBiggerThanBuffer)
        {
            if (pos.x < controlRect.x) pos.x += controlRect.width * 2 / zoomCoof;
        }

        return new Rect(pos.x, pos.y, width * scaleX, height * scaleY);
    }

    /// <summary>
    /// Determines if the marker at the specified coordinates.
    /// </summary>
    /// <param name="coordinates">
    /// Coordinate (X - Longitude, Y - Latitude).
    /// </param>
    /// <param name="zoom">
    /// The zoom.
    /// </param>
    /// <returns>
    /// True if the marker in position, false if not.
    /// </returns>
    public bool HitTest(Vector2 coordinates, int zoom)
    {
        double px, py;
        map.projection.CoordinatesToTile(longitude, latitude, zoom, out px, out py);
        float zoomCoof = map.buffer.renderState.zoomCoof;
        px *= OnlineMapsUtils.tileSize / zoomCoof;
        py *= OnlineMapsUtils.tileSize / zoomCoof;

        float w = width;
        float h = height;

        OnlineMapsVector2i pos = GetAlignedPosition((int)px, (int)py);
        double mx, my;
        map.projection.CoordinatesToTile(coordinates.x, coordinates.y, zoom, out mx, out my);
        mx *= OnlineMapsUtils.tileSize / zoomCoof;
        my *= OnlineMapsUtils.tileSize / zoomCoof;
        mx -= pos.x;
        my -= pos.y;

        return mx >= w * (markerColliderRect.x + 0.5f) && mx <= w * (markerColliderRect.xMax + 0.5f) && my >= w * (markerColliderRect.y + 0.5f) && my <= h * (markerColliderRect.yMax + 0.5f);
    }

    /// <summary>
    /// Initializes this marker.
    /// </summary>
    /// <param name="width">Width of the marker texture.</param>
    /// <param name="height">Height of the marker texture.</param>
    public void Init(int? width = null, int? height = null)
    {
        if (texture != null)
        {
            if (map.control.resultIsTexture) _colors = texture.GetPixels32();
            _width = _textureWidth = width ?? texture.width;
            _height = _textureHeight = height ?? texture.height;
        }
        else
        {
            Texture2D defaultTexture = (manager as OnlineMapsMarkerManager).defaultTexture;
            if (defaultTexture != null)
            {
                if (map.control.resultIsTexture) _colors = defaultTexture.GetPixels32();
                _width = _textureWidth = width ?? defaultTexture.width;
                _height = _textureHeight = height ?? defaultTexture.height;
            }
        }
        if (Math.Abs(_rotation) > float.Epsilon || Math.Abs(scale - 1) > float.Epsilon) UpdateRotatedBuffer();
        if (OnInitComplete != null) OnInitComplete(this);
    }

    public override void LookToCoordinates(OnlineMapsVector2d coordinates)
    {
        double p1x, p1y, p2x, p2y;
        map.projection.CoordinatesToTile(coordinates.x, coordinates.y, 20, out p1x, out p1y);
        map.projection.CoordinatesToTile(longitude, latitude, 20, out p2x, out p2y);
        rotation = (float)(1.25 - OnlineMapsUtils.Angle2D(p1x, p1y, p2x, p2y) / 360);
    }

    public override OnlineMapsJSONItem ToJSON()
    {
        return base.ToJSON().AppendObject(new
        {
            align = (int)align,
            texture = texture != null ? texture.GetInstanceID() : 0,
            rotation
        });
    }

    private void UpdateRotatedBuffer()
    {
        _lastRotation = _rotation;
        _lastScale = _scale;

        if (!map.control.resultIsTexture || (Math.Abs(_rotation) < float.Epsilon && Math.Abs(scale - 1) < float.Epsilon))
        {
            _width = _textureWidth;
            _height = _textureHeight;
            return;
        }

#if !UNITY_WEBGL
        int maxLocked = 20;
        while (locked && maxLocked > 0)
        {
            OnlineMapsUtils.ThreadSleep(1);
            maxLocked--;
        }
#endif

        locked = true;

        float angle = Mathf.Repeat(_rotation * 360, 360);
        Matrix4x4 matrix = new Matrix4x4();
        matrix.SetTRS(Vector3.zero, Quaternion.Euler(0, angle, 0), new Vector3(scale, scale, scale));
        Matrix4x4 inv = matrix.inverse;
        Vector3 p1 = matrix.MultiplyPoint3x4(new Vector3(_textureWidth, 0, 0));
        Vector3 p2 = matrix.MultiplyPoint3x4(new Vector3(0, 0, _textureHeight));
        Vector3 p3 = matrix.MultiplyPoint3x4(new Vector3(_textureWidth, 0, _textureHeight));

        float minX = Mathf.Min(0, p1.x, p2.x, p3.x);
        float minZ = Mathf.Min(0, p1.z, p2.z, p3.z);
        float maxX = Mathf.Max(0, p1.x, p2.x, p3.x);
        float maxZ = Mathf.Max(0, p1.z, p2.z, p3.z);

        _width = Mathf.RoundToInt(maxX - minX + 0.5f);
        _height = Mathf.RoundToInt(maxZ - minZ + 0.5f);

        Color32 emptyColor = new Color(0, 0, 0, 0);

        if (_rotatedColors == null || _rotatedColors.Length != _width * _height) _rotatedColors = new Color32[_width * _height];

        int tw = _textureWidth;
        int th = _textureHeight;

        Color32 c1, c2, c3, c4;

        for (int y = 0; y < _height; y++)
        {
            float ry = minZ + y;
            int cy = y * _width;
            for (int x = 0; x < _width; x++)
            {
                float rx = minX + x;
                Vector3 p = inv.MultiplyPoint3x4(new Vector3(rx, 0, ry));
                int iz = (int)p.z;
                int ix = (int)p.x;
                float fx = p.x - ix;
                float fz = p.z - iz;

                if (ix + 1 >= 0 && ix < tw && iz + 1 >= 0 && iz < th)
                {
                    if (ix >= 0 && iz >= 0) c1 = _colors[iz * tw + ix];
                    else c1 = emptyColor;

                    if (ix + 1 < tw && iz >= 0) c2 = _colors[iz * tw + ix + 1];
                    else c2 = emptyColor;

                    if (ix >= 0 && iz + 1 < th) c3 = _colors[(iz + 1) * tw + ix];
                    else c3 = emptyColor;

                    if (ix + 1 < tw && iz + 1 < th) c4 = _colors[(iz + 1) * tw + ix + 1];
                    else c4 = emptyColor;

                    c1 = Color32.Lerp(c1, c2, fx);
                    c3 = Color32.Lerp(c3, c4, fx);

                    _rotatedColors[cy + x] = Color32.Lerp(c1, c3, fz);
                }
                else _rotatedColors[cy + x] = emptyColor;
            }
        }

        locked = false;
    }
}