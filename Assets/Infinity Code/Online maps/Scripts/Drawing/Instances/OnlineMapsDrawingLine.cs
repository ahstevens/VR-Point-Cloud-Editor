/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that draws a line on the map.
/// </summary>
public class OnlineMapsDrawingLine : OnlineMapsDrawingElement
{
    private static List<Vector3> vertices;
    private static List<Vector3> normals;
    private static List<int> triangles;
    private static List<Vector2> uv;

    /// <summary>
    /// Forces the line to follow the relief.
    /// </summary>
    public bool followRelief = false;

    /// <summary>
    /// Sets the line width used for HitTest.
    /// </summary>
    public float? hitTestWidth;

    private Color _color = Color.black;
    private Texture2D _texture;
    private float _width = 1;
    private IEnumerable _points;

    /// <summary>
    /// Color of the line.
    /// </summary>
    public Color color
    {
        get { return _color; }
        set
        {
            _color = value;
            manager.map.Redraw();
        }
    }

    protected override bool createBackgroundMaterial
    {
        get { return false; }
    }

    protected override string defaultName
    {
        get { return "Line"; }
    }

    /// <summary>
    /// Texture of line.<br/>
    /// Uses only in tileset.
    /// </summary>
    public Texture2D texture
    {
        get { return _texture; }
        set
        {
            _texture = value;
            if (manager != null) manager.map.Redraw();
        }
    }

    /// <summary>
    /// IEnumerable of points of the line. Geographic coordinates.<br/>
    /// Can be:<br/>
    /// IEnumerable<Vector2>, where X - longitide, Y - latitude, <br/>
    /// IEnumerable<float> or IEnumerable<double>, where values (lng, lat, lng, lat... etc).
    /// </summary>
    public IEnumerable points
    {
        get { return _points; }
        set
        {
            if (value == null) throw new Exception("Points can not be null.");
            _points = value;
            if (manager != null) manager.map.Redraw();
        }
    }

    protected override bool splitToPieces
    {
        get { return followRelief && hasElevation; }
    }

    /// <summary>
    /// Width of the line.
    /// </summary>
    public float width
    {
        get { return _width; }
        set
        {
            _width = value;
            if (manager != null) manager.map.Redraw();
        }
    }

    /// <summary>
    /// Creates a new line.
    /// </summary>
    public OnlineMapsDrawingLine()
    {
        _points = new List<Vector2>();
    }

    /// <summary>
    /// Creates a new line.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the line. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    public OnlineMapsDrawingLine(IEnumerable points):this()
    {
        if (_points == null) throw new Exception("Points can not be null.");
        _points = points;
    }

    /// <summary>
    /// Creates a new line.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the line. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    /// <param name="color">Color of the line.</param>
    public OnlineMapsDrawingLine(IEnumerable points, Color color):this(points)
    {
        _color = color;
    }

    /// <summary>
    /// Creates a new line.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the line. Geographic coordinates.
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    /// <param name="color">Color of the line.</param>
    /// <param name="width">Width of the line.</param>
    public OnlineMapsDrawingLine(IEnumerable points, Color color, float width) : this(points, color)
    {
        _width = width;
    }

    public override void Draw(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, bool invertY = false)
    {
        if (!visible) return;
        if (range != null && !range.InRange(manager.map.floatZoom)) return;

        DrawLineToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, zoom, points, color, width, false, invertY);
    }

    public override void DrawOnTileset(OnlineMapsTileSetControl control, int index)
    {
        if (points == null) return;

        base.DrawOnTileset(control, index);

        if (!visible)
        {
            active = false;
            return;
        }

        if (range != null && !range.InRange(control.map.floatZoom))
        {
            active = false;
            return;
        }

        active = true;

        InitMesh(control, color, default(Color), texture);
        InitLineMesh(points, control, ref vertices, ref normals, ref triangles, ref uv, width);

        mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);

        mesh.SetTriangles(triangles.ToArray(), 0);

        UpdateMaterialsQuote(control, index);
    }

    protected override void DisposeLate()
    {
        base.DisposeLate();

        _points = null;
        texture = null;
    }

    public override bool HitTest(Vector2 positionLngLat, int zoom)
    {
        if (points == null) return false;

        double cx, cy;
        OnlineMapsProjection projection = manager.map.projection;
        projection.CoordinatesToTile(positionLngLat.x, positionLngLat.y, zoom, out cx, out cy);

        int valueType = -1; // 0 - Vector2, 1 - float, 2 - double, 3 - OnlineMapsVector2d

        object v1 = null;
        object v2 = null;
        object v3 = null;
        int i = 0;

        float w = hitTestWidth.HasValue ? hitTestWidth.Value : width;
        float sqrW = w * w;

        foreach (object p in points)
        {
            if (valueType == -1)
            {
                if (p is Vector2) valueType = 0;
                else if (p is float) valueType = 1;
                else if (p is double) valueType = 2;
                else if (p is OnlineMapsVector2d) valueType = 3;
            }

            object v4 = v3;
            v3 = v2;
            v2 = v1;
            v1 = p;

            double p1tx = 0, p1ty = 0, p2tx = 0, p2ty = 0;
            bool drawPart = false;

            if (valueType == 0)
            {
                if (i > 0)
                {
                    Vector2 p1 = (Vector2)v2;
                    Vector2 p2 = (Vector2)v1;

                    projection.CoordinatesToTile(p1.x, p1.y, zoom, out p1tx, out p1ty);
                    projection.CoordinatesToTile(p2.x, p2.y, zoom, out p2tx, out p2ty);
                    drawPart = true;
                }
            }
            else if (valueType == 3)
            {
                if (i > 0)
                {
                    OnlineMapsVector2d p1 = (OnlineMapsVector2d)v2;
                    OnlineMapsVector2d p2 = (OnlineMapsVector2d)v1;

                    projection.CoordinatesToTile(p1.x, p1.y, zoom, out p1tx, out p1ty);
                    projection.CoordinatesToTile(p2.x, p2.y, zoom, out p2tx, out p2ty);
                    drawPart = true;
                }
            }
            else if (i > 2 && i % 2 == 1)
            {
                if (valueType == 1)
                {
                    projection.CoordinatesToTile((float)v4, (float)v3, zoom, out p1tx, out p1ty);
                    projection.CoordinatesToTile((float)v2, (float)v1, zoom, out p2tx, out p2ty);
                }
                else if (valueType == 2)
                {
                    projection.CoordinatesToTile((double)v4, (double)v3, zoom, out p1tx, out p1ty);
                    projection.CoordinatesToTile((double)v2, (double)v1, zoom, out p2tx, out p2ty);
                }
                drawPart = true;
            }

            if (drawPart)
            {
                double nx, ny;
                OnlineMapsUtils.NearestPointStrict(cx, cy, p1tx, p1ty, p2tx, p2ty, out nx, out ny);
                double dx = (cx - nx) * OnlineMapsUtils.tileSize;
                double dy = (cy - ny) * OnlineMapsUtils.tileSize;
                double d = dx * dx + dy * dy;
                if (d < sqrW) return true;
            }

            i++;
        }

        return false;
    }

    public override bool Validate()
    {
        return _points != null;
    }
}