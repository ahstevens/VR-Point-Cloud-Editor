/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class draws a closed polygon on the map.
/// </summary>
public class OnlineMapsDrawingPoly : OnlineMapsDrawingElement
{
    private static List<Vector3> vertices;
    private static List<Vector3> normals;
    private static List<int> triangles;
    private static List<Vector2> uv;
    private static float[] internalPoints;
    private static List<int> internalIndices;
    private static int countInternalPoints;
    private static List<int> fillTriangles;

    private Color _backgroundColor = new Color(1, 1, 1, 0);
    private Color _borderColor = Color.black;
    private float _borderWidth = 1;

    /// <summary>
    /// Background color of the polygon.<br/>
    /// Note: Not supported in tileset.
    /// </summary>
    public Color backgroundColor
    {
        get { return _backgroundColor; }
        set
        {
            _backgroundColor = value;
            if (manager != null) manager.map.Redraw();
        }
    }

    /// <summary>
    /// Border color of the polygon.
    /// </summary>
    public Color borderColor
    {
        get { return _borderColor; }
        set
        {
            _borderColor = value;
            if (manager != null) manager.map.Redraw();
        }
    }

    /// <summary>
    /// Border width of the polygon.
    /// </summary>
    public float borderWidth
    {
        get { return _borderWidth; }
        set
        {
            _borderWidth = value;
            if (manager != null) manager.map.Redraw();
        }
    }

    protected override string defaultName
    {
        get { return "Poly"; }
    }

    /// <summary>
    /// IEnumerable of points of the polygon. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
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

    protected override bool createBackgroundMaterial
    {
        get { return _backgroundColor != default(Color); }
    }

    /// <summary>
    /// Center point of the polygon.
    /// </summary>
    public override OnlineMapsVector2d center
    {
        get
        {
            OnlineMapsVector2d centerPoint = OnlineMapsVector2d.zero;
            int count = 0;
            foreach (OnlineMapsVector2d point in points)
            {
                centerPoint += point;
                count++;
            }
            if (count == 0) return OnlineMapsVector2d.zero;
            return centerPoint / count;
        }
    }

    private IEnumerable _points;

    /// <summary>
    /// Creates a new polygon.
    /// </summary>
    public OnlineMapsDrawingPoly()
    {
        _points = new List<Vector2>();
    }

    /// <summary>
    /// Creates a new polygon.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the polygon. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    public OnlineMapsDrawingPoly(IEnumerable points) : this()
    {
        if (points == null) throw new Exception("Points can not be null.");
        _points = points;
    }

    /// <summary>
    /// Creates a new polygon.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the polygon. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    /// <param name="borderColor">Border color of the polygon.</param>
    public OnlineMapsDrawingPoly(IEnumerable points, Color borderColor)
        : this(points)
    {
        _borderColor = borderColor;
    }

    /// <summary>
    /// Creates a new polygon.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the polygon. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    /// <param name="borderColor">Border color of the polygon.</param>
    /// <param name="borderWidth">Border width of the polygon.</param>
    public OnlineMapsDrawingPoly(IEnumerable points, Color borderColor, float borderWidth)
        : this(points, borderColor)
    {
        _borderWidth = borderWidth;
    }

    /// <summary>
    /// Creates a new polygon.
    /// </summary>
    /// <param name="points">
    /// IEnumerable of points of the polygon. Geographic coordinates.<br/>
    /// The values can be of type: Vector2, float, double.<br/>
    /// If values float or double, the value should go in pairs(longitude, latitude).
    /// </param>
    /// <param name="borderColor">Border color of the polygon.</param>
    /// <param name="borderWidth">Border width of the polygon.</param>
    /// <param name="backgroundColor">
    /// Background color of the polygon.<br/>
    /// Note: Not supported in tileset.
    /// </param>
    public OnlineMapsDrawingPoly(IEnumerable points, Color borderColor, float borderWidth, Color backgroundColor)
        : this(points, borderColor, borderWidth)
    {
        _backgroundColor = backgroundColor;
    }

    protected override void DisposeLate()
    {
        base.DisposeLate();

        _points = null;
    }

    public override void Draw(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, bool invertY = false)
    {
        if (!visible) return;
        if (range != null && !range.InRange(manager.map.floatZoom)) return;

        FillPoly(buffer, bufferPosition, bufferWidth, bufferHeight, zoom, points, backgroundColor, invertY);
        DrawLineToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, zoom, points, borderColor, borderWidth, true, invertY);
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

        InitMesh(control, borderColor, backgroundColor);
        InitLineMesh(points, control, ref vertices, ref normals, ref triangles, ref uv, borderWidth, true, false);

        mesh.Clear();

        if (vertices.Count < 4) return;

        active = true;

        Vector3 v1 = (vertices[0] + vertices[3]) / 2;
        Vector3 v2 = (vertices[vertices.Count - 3] + vertices[vertices.Count - 2]) / 2;
        if ((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z) < float.Epsilon)
        {
            int s1, s2;
            Vector3 v0 = vertices[0];
            v1 = vertices[1];
            v2 = vertices[2];
            Vector3 v3 = vertices[3];
            Vector3 vs1 = vertices[vertices.Count - 1];
            Vector3 vs2 = vertices[vertices.Count - 2];
            Vector3 vs3 = vertices[vertices.Count - 3];
            Vector3 vs4 = vertices[vertices.Count - 4];
            Vector3 nv1 = Vector3.zero, nv2 = Vector3.zero;
            s1 = OnlineMapsUtils.GetIntersectionPointOfTwoLines(v0.x, v0.z, v1.x, v1.z, vs4.x, vs4.z, vs3.x, vs3.z, out nv1.x, out nv1.z);
            s2 = OnlineMapsUtils.GetIntersectionPointOfTwoLines(v3.x, v3.z, v2.x, v2.z, vs1.x, vs1.z, vs2.x, vs2.z, out nv2.x, out nv2.z);

            if (s1 == 1 && s2 == 1)
            {
                if (hasElevation)
                {
                    nv1.y = elevationManager.GetElevationValue(nv1.x, nv1.z, bestElevationYScale, tlx, tly, brx, bry);
                    nv2.y = elevationManager.GetElevationValue(nv2.x, nv2.z, bestElevationYScale, tlx, tly, brx, bry);
                }

                vertices[0] = vertices[vertices.Count - 3] = nv1;
                vertices[3] = vertices[vertices.Count - 2] = nv2;
            }
            else
            {
                vertices[0] = vertices[vertices.Count - 3] = (vertices[0] + vertices[vertices.Count - 3]) / 2;
                vertices[3] = vertices[vertices.Count - 2] = (vertices[3] + vertices[vertices.Count - 2]) / 2;
            }
        }

        if (fillTriangles == null) fillTriangles = new List<int>(128);
        else fillTriangles.Clear();

        if (!checkMapBoundaries && backgroundColor.a > 0 && vertices.Count > 0)
        {
            float l1 = 0;
            float l2 = 0;

            for (int i = 0; i < vertices.Count / 4 - 1; i++)
            {
                Vector3 p11 = vertices[i * 4];
                Vector3 p12 = vertices[(i + 1) * 4];

                Vector3 p21 = vertices[i * 4 + 3];
                Vector3 p22 = vertices[(i + 1) * 4 + 3];

                l1 += (p11 - p12).magnitude;
                l2 += (p21 - p22).magnitude;
            }

            bool side = l2 < l1;
            int off1 = side ? 3 : 0;
            int off2 = side ? 2 : 1;

            Vector2 lastPoint = Vector2.zero;

            if (internalIndices == null) internalIndices = new List<int>(128);
            else internalIndices.Clear();

            if (internalPoints == null) internalPoints = new float[128];
            countInternalPoints = 0;
            int ipl = internalPoints.Length;

            float w = borderWidth / 2;
            w *= w;
            for (int i = 0, j = 0; i < vertices.Count / 4; i++, j += 4)
            {
                Vector3 p = vertices[j + off1];
                Vector2 p2 = new Vector2(p.x, p.z);
                if (i > 0)
                {
                    if ((lastPoint - p2).sqrMagnitude > w)
                    {
                        internalIndices.Add(j + off1);

                        internalPoints[countInternalPoints++] = p2.x;
                        internalPoints[countInternalPoints++] = p2.y;
                        if (ipl == countInternalPoints) Array.Resize(ref internalPoints, ipl *= 2);

                        lastPoint = p2;
                    }
                }
                else
                {
                    internalIndices.Add(j + off1);

                    internalPoints[countInternalPoints++] = p2.x;
                    internalPoints[countInternalPoints++] = p2.y;
                    if (ipl == countInternalPoints) Array.Resize(ref internalPoints, ipl *= 2);

                    lastPoint = p2;
                }
                p = vertices[j + off2];
                p2 = new Vector2(p.x, p.z);
                if ((lastPoint - p2).sqrMagnitude > w)
                {
                    internalIndices.Add(j + off2);

                    internalPoints[countInternalPoints++] = p2.x;
                    internalPoints[countInternalPoints++] = p2.y;
                    if (ipl == countInternalPoints) Array.Resize(ref internalPoints, ipl *= 2);

                    lastPoint = p2;
                }
            }

            if (Math.Abs(internalPoints[0] - internalPoints[countInternalPoints - 2]) < float.Epsilon &&
                Math.Abs(internalPoints[1] - internalPoints[countInternalPoints - 1]) < float.Epsilon)
            {
                countInternalPoints -= 2;
            }

            OnlineMapsUtils.Triangulate(internalPoints, countInternalPoints / 2, fillTriangles);

            if (fillTriangles.Count > 2)
            {
                for (int i = 0; i < fillTriangles.Count; i++) fillTriangles[i] = internalIndices[fillTriangles[i]];

                Vector3 side1 = vertices[fillTriangles[1]] - vertices[fillTriangles[0]];
                Vector3 side2 = vertices[fillTriangles[2]] - vertices[fillTriangles[0]];
                Vector3 perp = Vector3.Cross(side1, side2);

                bool reversed = perp.y < 0;
                if (reversed) fillTriangles.Reverse();
            }
            else fillTriangles.Clear();
        }

        mesh.subMeshCount = 2;

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv);

        mesh.SetTriangles(triangles.ToArray(), 0);
        if (fillTriangles.Count > 0) mesh.SetTriangles(fillTriangles.ToArray(), 1);

        UpdateMaterialsQuote(control, index);
    }

    public override bool HitTest(Vector2 positionLngLat, int zoom)
    {
        if (points == null) return false;
        return OnlineMapsUtils.IsPointInPolygon(points, positionLngLat.x, positionLngLat.y);
    }

    public override bool Validate()
    {
        return _points != null;
    }
}