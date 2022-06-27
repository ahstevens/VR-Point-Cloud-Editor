/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Class implements the basic functionality of drawing on the map.
/// </summary>
public abstract class OnlineMapsDrawingElement: IOnlineMapsInteractiveElement
{
    /// <summary>
    /// Default event caused to draw tooltip.
    /// </summary>
    public static Action<OnlineMapsDrawingElement> OnElementDrawTooltip;

    /// <summary>
    /// Events that occur when user click on the drawing element.
    /// </summary>
    public Action<OnlineMapsDrawingElement> OnClick;

    /// <summary>
    /// Events that occur when user double click on the drawing element.
    /// </summary>
    public Action<OnlineMapsDrawingElement> OnDoubleClick;

    /// <summary>
    /// Event caused to draw tooltip.
    /// </summary>
    public Action<OnlineMapsDrawingElement> OnDrawTooltip;

    /// <summary>
    /// Event that occur when tileset initializes a mesh.
    /// </summary>
    public Action<OnlineMapsDrawingElement, Renderer> OnInitMesh;

    /// <summary>
    /// Events that occur when user long press on the drawing element.
    /// </summary>
    public Action<OnlineMapsDrawingElement> OnLongPress;

    /// <summary>
    /// Events that occur when user press on the drawing element.
    /// </summary>
    public Action<OnlineMapsDrawingElement> OnPress;

    /// <summary>
    /// Events that occur when user release on the drawing element.
    /// </summary>
    public Action<OnlineMapsDrawingElement> OnRelease;

    /// <summary>
    /// Need to check the map boundaries? <br/>
    /// It allows you to make drawing element, which are active outside the map.<br/>
    /// </summary>
    public bool checkMapBoundaries = true;

    /// <summary>
    /// Zoom range, in which the drawing element will be displayed.
    /// </summary>
    public OnlineMapsRange range;

    /// <summary>
    /// Tooltip that is displayed when user hover on the drawing element.
    /// </summary>
    public string tooltip;

    /// <summary>
    /// The local Y position for the GameObject on Tileset.
    /// </summary>
    public float yOffset = 0;

    protected bool _visible = true;
    protected float bestElevationYScale;
    protected GameObject gameObject;
    protected Mesh mesh;
    protected double tlx;
    protected double tly;
    protected double brx;
    protected double bry;
    protected Material[] materials;

    private Dictionary<string, object> _customFields;
    private string _name;
    private int _renderQueueOffset;
    private IOnlineMapsInteractiveElementManager _manager;
    private static List<Vector2> localPoints;
    private OnlineMapsElevationManagerBase _elevationManager;
    private bool elevationManagerInited = false;

    public object this[string key]
    {
        get
        {
            object val;
            return customFields.TryGetValue(key, out val) ? val : null;
        }
        set { customFields[key] = value; }
    }

    protected virtual bool active
    {
        get
        {
            if (gameObject == null) return false;
            return gameObject.activeSelf;
        }
        set
        {
            if (gameObject != null) gameObject.SetActive(value);
        }
    }

    protected abstract bool createBackgroundMaterial { get; }

    public Dictionary<string, object> customFields
    {
        get
        {
            if (_customFields == null) _customFields = new Dictionary<string, object>();
            return _customFields;
        }
    }

    /// <summary>
    /// Center point of the drawing element.
    /// </summary>
    public virtual OnlineMapsVector2d center
    {
        get { return OnlineMapsVector2d.zero; }
    }

    protected virtual string defaultName
    {
        get { return "Drawing Element"; }
    }

    protected OnlineMapsElevationManagerBase elevationManager
    {
        get
        {
            if (!elevationManagerInited)
            {
                elevationManagerInited = true;

                OnlineMapsControlBaseDynamicMesh control = manager.map.control as OnlineMapsControlBaseDynamicMesh;
                if (control != null) _elevationManager = control.elevationManager;
            }

            return _elevationManager;
        }
    }

    protected bool hasElevation
    {
        get { return elevationManager != null && elevationManager.enabled; }
    }

    public GameObject instance
    {
        get { return gameObject; }
    }

    public IOnlineMapsInteractiveElementManager manager
    {
        get { return _manager != null? _manager: OnlineMapsDrawingElementManager.instance; }
        set { _manager = value; }
    }

    public string name
    {
        get
        {
            if (!string.IsNullOrEmpty(_name)) return _name;
            return defaultName;
        }
        set
        {
            _name = value;
            if (gameObject != null) gameObject.name = name;
            if (mesh != null) mesh.name = name;
        }
    }

    public int renderQueueOffset
    {
        get { return _renderQueueOffset; }
        set
        {
            _renderQueueOffset = value;
            if (materials != null)
            {
                Shader shader = (manager.map.control as OnlineMapsTileSetControl).drawingShader;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material m = materials[i];
                    if (m != null) m.renderQueue = shader.renderQueue + value;
                }
            }
        }
    }

    protected virtual bool splitToPieces
    {
        get { return false; }
    }

    /// <summary>
    /// Gets or sets the visibility of the drawing element.
    /// </summary>
    public virtual bool visible
    {
        get { return _visible; }
        set
        {
            if (_visible == value) return;

            _visible = value;
            manager.map.Redraw();
        }
    }

    protected OnlineMapsDrawingElement()
    {
        
    }

    private static void AddLineSegment(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, List<Vector2> uv, Vector3 s1, Vector3 s2, Vector3 prevS1, Vector3 prevS2)
    {
        int ti = vertices.Count;
        vertices.Add(prevS1);
        vertices.Add(s1);
        vertices.Add(s2);
        vertices.Add(prevS2);

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        uv.Add(new Vector2(0, 0));
        uv.Add(new Vector2(0, 1));
        uv.Add(new Vector2(1, 1));
        uv.Add(new Vector2(1, 0));

        triangles.Add(ti);
        triangles.Add(ti + 1);
        triangles.Add(ti + 2);
        triangles.Add(ti);
        triangles.Add(ti + 2);
        triangles.Add(ti + 3);
    }

    public virtual void DestroyInstance()
    {
        if (gameObject != null)
        {
            OnlineMapsUtils.Destroy(gameObject);
            gameObject = null;
        }

        if (mesh != null)
        {
            OnlineMapsUtils.Destroy(mesh);
            mesh = null;
        }

        if (materials != null)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material != null) OnlineMapsUtils.Destroy(material);
            }

            materials = null;
        }
    }

    /// <summary>
    /// Dispose drawing element.
    /// </summary>
    public void Dispose()
    {
        _manager = null;
        OnClick = null;
        OnDoubleClick = null;
        OnDrawTooltip = null;
        OnPress = null;
        OnRelease = null;
        _customFields = null;

        DestroyInstance();
        tooltip = null;

        DisposeLate();
    }

    protected virtual void DisposeLate()
    {
        
    }

    /// <summary>
    /// Draw element on the map.
    /// </summary>
    /// <param name="buffer">Backbuffer</param>
    /// <param name="bufferPosition">Backbuffer position</param>
    /// <param name="bufferWidth">Backbuffer width</param>
    /// <param name="bufferHeight">Backbuffer height</param>
    /// <param name="zoom">Zoom of the map</param>
    /// <param name="invertY">Invert Y direction</param>
    public virtual void Draw(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, bool invertY = false)
    {
        
    }

    protected void DrawActivePoints(OnlineMapsTileSetControl control, ref List<Vector2> activePoints, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles, ref List<Vector2> uv, float width)
    {
        if (activePoints.Count < 2)
        {
            activePoints.Clear();
            return;
        }

        List<Vector2> points = activePoints;

        if (splitToPieces) points = SplitToPieces(control, points);

        float w2 = width * 2;

        Vector3 prevS1 = Vector3.zero;
        Vector3 prevS2 = Vector3.zero;

        int c = points.Count - 1;
        bool extraPointAdded = false;
        bool elevationActive = hasElevation;

        for (int i = 0; i < points.Count; i++)
        {
            float px = -points[i].x;
            float pz = points[i].y;

            Vector3 s1;
            Vector3 s2;

            if (i == 0 || i == c)
            {
                float p1x, p1z, p2x, p2z;

                if (i == 0)
                {
                    p1x = px;
                    p1z = pz;
                    p2x = -points[i + 1].x;
                    p2z = points[i + 1].y;
                }
                else
                {
                    p1x = -points[i - 1].x;
                    p1z = points[i - 1].y;
                    p2x = px;
                    p2z = pz;
                }

                float a = OnlineMapsUtils.Angle2DRad(p1x, p1z, p2x, p2z, 90);

                float offX = Mathf.Cos(a) * width;
                float offZ = Mathf.Sin(a) * width;

                float s1x = px + offX;
                float s1z = pz + offZ;
                float s2x = px - offX;
                float s2z = pz - offZ;

                float s1y = 0;
                float s2y = 0; 

                if (elevationActive)
                {
                    s1y = elevationManager.GetElevationValue(s1x, s1z, bestElevationYScale, tlx, tly, brx, bry);
                    s2y = elevationManager.GetElevationValue(s2x, s2z, bestElevationYScale, tlx, tly, brx, bry);
                }

                s1 = new Vector3(s1x, s1y, s1z);
                s2 = new Vector3(s2x, s2y, s2z);
            }
            else
            {
                float p1x = -points[i - 1].x;
                float p1z = points[i - 1].y;
                float p2x = -points[i + 1].x;
                float p2z = points[i + 1].y;

                float a1 = OnlineMapsUtils.Angle2DRad(p1x, p1z, px, pz, 90);
                float a3 = OnlineMapsUtils.AngleOfTriangle(points[i - 1], points[i + 1], points[i]) * Mathf.Rad2Deg;
                if (a3 < 60 && !extraPointAdded)
                {
                    points.Insert(i + 1, Vector2.Lerp(points[i], points[i + 1], 0.001f));
                    points[i] = Vector2.Lerp(points[i], points[i - 1], 0.001f);
                    c++;
                    i--;
                    extraPointAdded = true;
                    continue;
                }

                extraPointAdded = false;
                float a2 = OnlineMapsUtils.Angle2DRad(px, pz, p2x, p2z, 90);

                float off1x = Mathf.Cos(a1) * width;
                float off1z = Mathf.Sin(a1) * width;
                float off2x = Mathf.Cos(a2) * width;
                float off2z = Mathf.Sin(a2) * width;

                float p21x = px + off1x;
                float p21z = pz + off1z;
                float p22x = px - off1x;
                float p22z = pz - off1z;
                float p31x = px + off2x;
                float p31z = pz + off2z;
                float p32x = px - off2x;
                float p32z = pz - off2z;

                float is1x, is1z, is2x, is2z;
                
                int state1 = OnlineMapsUtils.GetIntersectionPointOfTwoLines(p1x + off1x, p1z + off1z, p21x, p21z, p31x, p31z, p2x + off2x, p2z + off2z, out is1x, out is1z);
                int state2 = OnlineMapsUtils.GetIntersectionPointOfTwoLines(p1x - off1x, p1z - off1z, p22x, p22z, p32x, p32z, p2x - off2x, p2z - off2z, out is2x, out is2z);

                if (state1 == 1 && state2 == 1)
                {
                    float o1x = is1x - px;
                    float o1z = is1z - pz;
                    float o2x = is2x - px;
                    float o2z = is2z - pz;

                    float m1 = Mathf.Sqrt(o1x * o1x + o1z * o1z);
                    float m2 = Mathf.Sqrt(o2x * o2x + o2z * o2z);

                    if (m1 > w2)
                    {
                        is1x = o1x / m1 * w2 + px;
                        is1z = o1z / m1 * w2 + pz;
                    }
                    if (m2 > w2)
                    {
                        is2x = o2x / m2 * w2 + px;
                        is2z = o2z / m2 * w2 + pz;
                    }

                    float s1y = 0;
                    float s2y = 0;

                    if (elevationActive)
                    {
                        s1y = elevationManager.GetElevationValue(is1x, is1z, bestElevationYScale, tlx, tly, brx, bry);
                        s2y = elevationManager.GetElevationValue(is2x, is2z, bestElevationYScale, tlx, tly, brx, bry);
                    }

                    s1 = new Vector3(is1x, s1y, is1z);
                    s2 = new Vector3(is2x, s2y, is2z);
                }
                else
                {
                    float po1x = p31x;
                    float po1z = p31z;
                    float po2x = p32x;
                    float po2z = p32z;

                    float s1y = 0;
                    float s2y = 0;

                    if (elevationActive)
                    {
                        s1y = elevationManager.GetElevationValue(po1x, po1z, bestElevationYScale, tlx, tly, brx, bry);
                        s2y = elevationManager.GetElevationValue(po2x, po2z, bestElevationYScale, tlx, tly, brx, bry);
                    }

                    s1 = new Vector3(po1x, s1y, po1z);
                    s2 = new Vector3(po2x, s2y, po2z);
                }
            }

            if (i > 0)
            {
                AddLineSegment(vertices, normals, triangles, uv, s1, s2, prevS1, prevS2);
            }

            prevS1 = s1;
            prevS2 = s2;
        }

        activePoints.Clear();
    }

    protected void DrawLineToBuffer(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, IEnumerable points, Color32 color, float width, bool closed, bool invertY)
    {
        if (color.a == 0) return;

        int izoom = (int) zoom;
        float zoomScale = 1 - (zoom - izoom) / 2;

        double sx, sy;
        manager.map.projection.CoordinatesToTile(0, 0, izoom, out sx, out sy);

        int max = 1 << izoom;

        int w = Mathf.RoundToInt(width);

        double ppx1 = 0;

        float bx1 = bufferPosition.x;
        float bx2 = bx1 + zoomScale * bufferWidth / OnlineMapsUtils.tileSize;
        float by1 = bufferPosition.y;
        float by2 = by1 + zoomScale * bufferHeight / OnlineMapsUtils.tileSize;

        int valueType = -1; // 0 - Vector2, 1 - float, 2 - double, 3 - OnlineMapsVector2d
        object firstValue = null;
        object secondValue = null;
        object v1 = null;
        object v2 = null;
        object v3 = null;
        int i = 0;

        lock (points)
        {
            foreach (object p in points)
            {
                if (valueType == -1)
                {
                    firstValue = p;
                    if (p is Vector2) valueType = 0;
                    else if (p is float) valueType = 1;
                    else if (p is double) valueType = 2;
                    else if (p is OnlineMapsVector2d) valueType = 3;
                }

                object v4 = v3;
                v3 = v2;
                v2 = v1;
                v1 = p;

                if (i == 1) secondValue = p;

                double p1tx = 0, p1ty = 0, p2tx = 0, p2ty = 0;
                bool drawPart = false;

                if (valueType == 0)
                {
                    if (i > 0)
                    {
                        Vector2 p1 = (Vector2)v2;
                        Vector2 p2 = (Vector2)v1;

                        manager.map.projection.CoordinatesToTile(p1.x, p1.y, izoom, out p1tx, out p1ty);
                        manager.map.projection.CoordinatesToTile(p2.x, p2.y, izoom, out p2tx, out p2ty);
                        drawPart = true;
                    }
                }
                else if (valueType == 3)
                {
                    if (i > 0)
                    {
                        OnlineMapsVector2d p1 = (OnlineMapsVector2d)v2;
                        OnlineMapsVector2d p2 = (OnlineMapsVector2d)v1;

                        manager.map.projection.CoordinatesToTile(p1.x, p1.y, izoom, out p1tx, out p1ty);
                        manager.map.projection.CoordinatesToTile(p2.x, p2.y, izoom, out p2tx, out p2ty);
                        drawPart = true;
                    }
                }
                else if (i > 2 && i % 2 == 1)
                {
                    if (valueType == 1)
                    {
                        manager.map.projection.CoordinatesToTile((float)v4, (float)v3, izoom, out p1tx, out p1ty);
                        manager.map.projection.CoordinatesToTile((float)v2, (float)v1, izoom, out p2tx, out p2ty);
                    }
                    else if (valueType == 2)
                    {
                        manager.map.projection.CoordinatesToTile((double)v4, (double)v3, izoom, out p1tx, out p1ty);
                        manager.map.projection.CoordinatesToTile((double)v2, (double)v1, izoom, out p2tx, out p2ty);
                    }
                    drawPart = true;
                }

                if (drawPart)
                {
                    if ((p1tx < bx1 && p2tx < bx1) || (p1tx > bx2 && p2tx > bx2))
                    {

                    }
                    else if ((p1ty < by1 && p2ty < by1) || (p1ty > by2 && p2ty > by2))
                    {

                    }
                    else DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, sx, sy, p1tx, p1ty, p2tx, p2ty, i, max, ref ppx1, w, invertY, zoomScale);
                }

                i++;
            }
        }

        if (closed && i > 0)
        {
            double p1tx = 0, p1ty = 0, p2tx = 0, p2ty = 0;

            if (valueType == 0)
            {
                Vector2 p1 = (Vector2)firstValue;
                Vector2 p2 = (Vector2)v1;

                manager.map.projection.CoordinatesToTile(p1.x, p1.y, izoom, out p1tx, out p1ty);
                manager.map.projection.CoordinatesToTile(p2.x, p2.y, izoom, out p2tx, out p2ty);
            }
            else if (valueType == 3)
            {
                OnlineMapsVector2d p1 = (OnlineMapsVector2d)firstValue;
                OnlineMapsVector2d p2 = (OnlineMapsVector2d)v1;

                manager.map.projection.CoordinatesToTile(p1.x, p1.y, izoom, out p1tx, out p1ty);
                manager.map.projection.CoordinatesToTile(p2.x, p2.y, izoom, out p2tx, out p2ty);
            }
            else if (valueType == 1)
            {
                manager.map.projection.CoordinatesToTile((float)firstValue, (float)secondValue, izoom, out p1tx, out p1ty);
                manager.map.projection.CoordinatesToTile((float)v2, (float)v1, izoom, out p2tx, out p2ty);
            }
            else if (valueType == 2)
            {
                manager.map.projection.CoordinatesToTile((double)firstValue, (double)secondValue, izoom, out p1tx, out p1ty);
                manager.map.projection.CoordinatesToTile((double)v2, (double)v1, izoom, out p2tx, out p2ty);
            }

            if ((p1tx < bx1 && p2tx < bx1) || (p1tx > bx2 && p2tx > bx2))
            {
                    
            }
            else if ((p1ty < by1 && p2ty < by1) || (p1ty > by2 && p2ty > by2))
            {
                    
            }
            else DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, sx, sy, p1tx, p1ty, p2tx, p2ty, i, max, ref ppx1, w, invertY, zoomScale);
        }
    }

    private static void DrawLinePartToBuffer(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, Color32 color, double sx, double sy, double p1tx, double p1ty, double p2tx, double p2ty, int j, int maxX, ref double ppx1, int w, bool invertY, float zoomScale)
    {
        if ((p1tx < bufferPosition.x && p2tx < bufferPosition.x) || (p1tx > bufferPosition.x + (bufferWidth >> 8) / zoomScale && p2tx > bufferPosition.x + (bufferWidth >> 8) / zoomScale)) return;
        if ((p1ty < bufferPosition.y && p2ty < bufferPosition.y) || (p1ty > bufferPosition.y + (bufferHeight >> 8) / zoomScale && p2ty > bufferPosition.y + (bufferHeight >> 8) / zoomScale)) return;

        if ((p1tx - p2tx) * (p1tx - p2tx) + (p1ty - p2ty) * (p1ty - p2ty) > 0.04)
        {
            double p3tx = (p1tx + p2tx) / 2;
            double p3ty = (p1ty + p2ty) / 2;
            DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, sx, sy, p1tx, p1ty, p3tx, p3ty, j, maxX, ref ppx1, w, invertY, zoomScale);
            DrawLinePartToBuffer(buffer, bufferPosition, bufferWidth, bufferHeight, color, sx, sy, p3tx, p3ty, p2tx, p2ty, j, maxX, ref ppx1, w, invertY, zoomScale);
            return;
        }

        p1tx -= sx;
        p2tx -= sx;
        p1ty -= sy;
        p2ty -= sy;

        if (j == 0)
        {
            if (p1tx < maxX * -0.25) p1tx += maxX;
            else if (p1tx > maxX * 0.75) p1tx -= maxX;
        }
        else
        {
            double gpx1 = p1tx + maxX;
            double lpx1 = p1tx - maxX;

            if (Math.Abs(ppx1 - gpx1) < Math.Abs(ppx1 - p1tx)) p1tx = gpx1;
            else if (Math.Abs(ppx1 - lpx1) < Math.Abs(ppx1 - p1tx)) p1tx = lpx1;
        }

        ppx1 = p1tx;

        double gpx2 = p2tx + maxX;
        double lpx2 = p2tx - maxX;

        if (Math.Abs(ppx1 - gpx2) < Math.Abs(ppx1 - p2tx)) p2tx = gpx2;
        else if (Math.Abs(ppx1 - lpx2) < Math.Abs(ppx1 - p2tx)) p2tx = lpx2;

        double p1x = (p1tx + sx - bufferPosition.x) / zoomScale;
        double p1y = (p1ty + sy - bufferPosition.y) / zoomScale;
        double p2x = (p2tx + sx - bufferPosition.x) / zoomScale;
        double p2y = (p2ty + sy - bufferPosition.y) / zoomScale;

        if (p1x > maxX && p2x > maxX)
        {
            p1x -= maxX;
            p2x -= maxX;
        }

        double fromX = p1x * OnlineMapsUtils.tileSize;
        double fromY = p1y * OnlineMapsUtils.tileSize;
        double toX = p2x * OnlineMapsUtils.tileSize;
        double toY = p2y * OnlineMapsUtils.tileSize;

        double stX = (fromX < toX ? fromX : toX) - w;
        if (stX < 0) stX = 0;
        else if (stX > bufferWidth) stX = bufferWidth;

        double stY = (fromY < toY ? fromY : toY) - w;
        if (stY < 0) stY = 0;
        else if (stY > bufferHeight) stY = bufferHeight;

        double endX = (fromX > toX ? fromX : toX) + w;
        if (endX < 0) endX = 0;
        else if (endX > bufferWidth) endX = bufferWidth;

        double endY = (fromY > toY ? fromY : toY) + w;
        if (endY < 0) endY = 0;
        else if (endY > bufferHeight) endY = bufferHeight;

        int istx = (int) Math.Round(stX);
        int isty = (int) Math.Round(stY);

        int sqrW = w * w;

        int lengthX = (int) Math.Round(endX - stX);
        int lengthY = (int) Math.Round(endY - stY);

        byte clrR = color.r;
        byte clrG = color.g;
        byte clrB = color.b;
        byte clrA = color.a;
        float alpha = clrA / 256f;
        if (alpha > 1) alpha = 1;

        for (int y = 0; y < lengthY; y++)
        {
            double py = y + stY;
            int ipy = y + isty;
            double centerY = py + 0.5;
            if (!invertY) ipy = bufferHeight - ipy - 1;
            ipy *= bufferWidth;

            for (int x = 0; x < lengthX; x++)
            {
                double px = x + stX;
                int ipx = x + istx;
                double centerX = px + 0.5;

                double npx, npy;

                OnlineMapsUtils.NearestPointStrict(centerX, centerY, fromX, fromY, toX, toY, out npx, out npy);
                double onpx = centerX - npx;
                double onpy = centerY - npy;

                double dist = onpx * onpx + onpy * onpy;

                if (dist <= sqrW)
                {
                    int bufferIndex = ipy + ipx;
                    Color32 pc = buffer[bufferIndex];
                    pc.r = (byte)((clrR - pc.r) * alpha + pc.r);
                    pc.g = (byte)((clrG - pc.g) * alpha + pc.g);
                    pc.b = (byte)((clrB - pc.b) * alpha + pc.b);
                    pc.a = (byte)((clrA - pc.a) * alpha + pc.a);
                    buffer[bufferIndex] = pc;
                }
            }
        }
    }

    /// <summary>
    /// Draws element on a specified TilesetControl.
    /// </summary>
    /// <param name="control">Reference to tileset control.</param>
    /// <param name="index">Index of drawing element</param>
    public virtual void DrawOnTileset(OnlineMapsTileSetControl control, int index)
    {
        
    }

    protected void FillPoly(Color32[] buffer, Vector2 bufferPosition, int bufferWidth, int bufferHeight, float zoom, IEnumerable points, Color32 color, bool invertY)
    {
        if (color.a == 0) return;
        float alpha = color.a / 255f;

        double minX, maxX, minY, maxY;
        double[] bufferPoints = GetBufferPoints(bufferPosition, zoom, points, out minX, out maxX, out minY, out maxY);

        if (maxX < 0 || minX > bufferWidth || maxY < 0 || minY > bufferHeight) return;

        double stX = minX;
        if (stX < 0) stX = 0;
        else if (stX > bufferWidth) stX = bufferWidth;

        double stY = minY;
        if (stY < 0) stY = 0;
        else if (stY > bufferHeight) stY = bufferHeight;

        double endX = maxX;
        if (endX < 0) stX = 0;
        else if (endX > bufferWidth) endX = bufferWidth;

        double endY = maxY;
        if (endY < 0) endY = 0;
        else if (endY > bufferHeight) endY = bufferHeight;

        int lengthX = (int)Math.Round(endX - stX);
        int lengthY = (int)Math.Round(endY - stY);

        Color32 clr = new Color32(color.r, color.g, color.b, 255);

        const int blockSize = 5;
        int blockCountX = lengthX / blockSize + (lengthX % blockSize == 0 ? 0 : 1);
        int blockCountY = lengthY / blockSize + (lengthY % blockSize == 0 ? 0 : 1);

        byte clrR = clr.r;
        byte clrG = clr.g;
        byte clrB = clr.b;

        int istx = (int) Math.Round(stX);
        int isty = (int) Math.Round(stY);

        for (int by = 0; by < blockCountY; by++)
        {
            int byp = by * blockSize;
            double bufferY = byp + stY;
            int iby = byp + isty;

            for (int bx = 0; bx < blockCountX; bx++)
            {
                int bxp = bx * blockSize;
                double bufferX = bxp + stX;
                int ibx = bxp + istx;

                bool p1 = OnlineMapsUtils.IsPointInPolygon(bufferPoints, bufferX, bufferY);
                bool p2 = OnlineMapsUtils.IsPointInPolygon(bufferPoints, bufferX + blockSize - 1, bufferY);
                bool p3 = OnlineMapsUtils.IsPointInPolygon(bufferPoints, bufferX + blockSize - 1, bufferY + blockSize - 1);
                bool p4 = OnlineMapsUtils.IsPointInPolygon(bufferPoints, bufferX, bufferY + blockSize - 1);

                if (p1 && p2 && p3 && p4)
                {
                    for (int y = 0; y < blockSize; y++)
                    {
                        if (byp + y >= lengthY) break;
                        int cby = iby + y;
                        if (!invertY) cby = bufferHeight - cby - 1;
                        int byi = cby * bufferWidth + ibx;

                        for (int x = 0; x < blockSize; x++)
                        {
                            if (bxp + x >= lengthX) break;

                            int bufferIndex = byi + x;
                            
                            Color32 a = buffer[bufferIndex];
                            a.r = (byte) (a.r + (clrR - a.r) * alpha);
                            a.g = (byte) (a.g + (clrG - a.g) * alpha);
                            a.b = (byte) (a.b + (clrB - a.b) * alpha);
                            a.a = (byte) (a.a + (255 - a.a) * alpha);
                            buffer[bufferIndex] = a;
                        }
                    }
                }
                else if (!p1 && !p2 && !p3 && !p4)
                {
                    
                }
                else
                {
                    for (int y = 0; y < blockSize; y++)
                    {
                        if (byp + y >= lengthY) break;
                        int cby = iby + y;
                        if (!invertY) cby = bufferHeight - cby - 1;
                        int byi = cby * bufferWidth + ibx;

                        for (int x = 0; x < blockSize; x++)
                        {
                            if (bxp + x >= lengthX) break;

                            if (OnlineMapsUtils.IsPointInPolygon(bufferPoints, bufferX + x, bufferY + y))
                            {
                                int bufferIndex = byi + x;
                                Color32 a = buffer[bufferIndex];
                                a.r = (byte)(a.r + (clrR - a.r) * alpha);
                                a.g = (byte)(a.g + (clrG - a.g) * alpha);
                                a.b = (byte)(a.b + (clrB - a.b) * alpha);
                                a.a = (byte)(a.a + (255 - a.a) * alpha);
                                buffer[bufferIndex] = a;
                            }
                        }
                    }
                }
            }
        }
    }

    private double[] GetBufferPoints(Vector2 bufferPosition, float zoom, IEnumerable points, out double minX, out double maxX, out double minY, out double maxY)
    {
        int izoom = (int) zoom;
        float zoomScale = 1 - (zoom - izoom) / 2;
        float scaledTileSize = OnlineMapsUtils.tileSize / zoomScale;

        double[] bufferPoints = null;

        int countPoints = points.Cast<object>().Count();

        minX = double.MaxValue;
        maxX = double.MinValue;
        minY = double.MaxValue;
        maxY = double.MinValue;

        int valueType = -1; // 0 - Vector2, 1 - float, 2 - double, 3 - OnlineMapsVector2d
        object v1 = null;

        int i = 0;

        foreach (object p in points)
        {
            if (valueType == -1)
            {
                if (p is Vector2)
                {
                    valueType = 0;
                    bufferPoints = new double[countPoints * 2];
                }
                else if (p is float)
                {
                    valueType = 1;
                    bufferPoints = new double[countPoints];
                }
                else if (p is double)
                {
                    valueType = 2;
                    bufferPoints = new double[countPoints];
                }
                else if (p is OnlineMapsVector2d)
                {
                    valueType = 3;
                    bufferPoints = new double[countPoints * 2];
                }
            }

            object v2 = v1;
            v1 = p;

            if (valueType == 0 || valueType == 3)
            {
                double px, py;
                if (valueType == 0)
                {
                    Vector2 point = (Vector2) p;
                    px = point.x;
                    py = point.y;
                }
                else
                {
                    OnlineMapsVector2d point = (OnlineMapsVector2d)p;
                    px = point.x;
                    py = point.y;
                }
                double tx, ty;
                manager.map.projection.CoordinatesToTile(px, py, izoom, out tx, out ty);
                tx = (tx - bufferPosition.x) * scaledTileSize;
                ty = (ty - bufferPosition.y) * scaledTileSize;

                if (tx < minX) minX = tx;
                if (tx > maxX) maxX = tx;
                if (ty < minY) minY = ty;
                if (ty > maxY) maxY = ty;

                bufferPoints[i * 2] = tx;
                bufferPoints[i * 2 + 1] = ty;
            }
            else if (i % 2 == 1)
            {
                double tx = 0, ty = 0;
                if (valueType == 1) manager.map.projection.CoordinatesToTile((float) v2, (float) v1, izoom, out tx, out ty);
                else if (valueType == 2) manager.map.projection.CoordinatesToTile((double) v2, (double) v1, izoom, out tx, out ty);
                tx = (tx - bufferPosition.x) * scaledTileSize;
                ty = (ty - bufferPosition.y) * scaledTileSize;

                if (tx < minX) minX = tx;
                if (tx > maxX) maxX = tx;
                if (ty < minY) minY = ty;
                if (ty > maxY) maxY = ty;

                bufferPoints[i - 1] = tx;
                bufferPoints[i] = ty;
            }

            i++;
        }
        return bufferPoints;
    }

    protected List<Vector2> GetLocalPoints(IEnumerable points, bool closed = false, bool optimize = true)
    {
        double sx, sy;

        OnlineMaps map = manager.map;
        int zoom = map.zoom;
        float zoomCoof = map.zoomCoof;

        OnlineMapsProjection projection = map.projection;
        projection.CoordinatesToTile(tlx, tly, zoom, out sx, out sy);

        int max = 1 << zoom;
        int halfMax = max / 2;

        if (localPoints == null) localPoints = new List<Vector2>();
        else localPoints.Clear();

        double ppx = 0;
        Vector2 sizeInScene = (map.control as OnlineMapsControlBaseDynamicMesh).sizeInScene;
        double scaleX = OnlineMapsUtils.tileSize * sizeInScene.x / map.buffer.renderState.width / zoomCoof;
        double scaleY = OnlineMapsUtils.tileSize * sizeInScene.y / map.buffer.renderState.height / zoomCoof;

        double prx = 0, pry = 0;

        object v1 = null;
        int i = -1;
        int valueType = -1; // 0 - Vector2, 1 - float, 2 - double, 3 - OnlineMapsVector2d
        bool isOptimized = false;
        double px = 0, py = 0;

        IEnumerator enumerator = points.GetEnumerator();
        int mapTileWidth = map.width / OnlineMapsUtils.tileSize / 2;

        while (enumerator.MoveNext())
        {
            i++;

            object p = enumerator.Current;
            if (valueType == -1)
            {
                if (p is Vector2) valueType = 0;
                else if (p is float) valueType = 1;
                else if (p is double) valueType = 2;
                else if (p is OnlineMapsVector2d) valueType = 3;
            }

            object v2 = v1;
            v1 = p;

            bool useValue = false;

            if (valueType == 0)
            {
                Vector2 point = (Vector2)p;
                projection.CoordinatesToTile(point.x, point.y, zoom, out px, out py);
                useValue = true;
            }
            else if (valueType == 3)
            {
                OnlineMapsVector2d point = (OnlineMapsVector2d)p;
                projection.CoordinatesToTile(point.x, point.y, zoom, out px, out py);
                useValue = true;
            }
            else if (i % 2 == 1)
            {
                if (valueType == 1) projection.CoordinatesToTile((float)v2, (float)v1, zoom, out px, out py);
                else if (valueType == 2) projection.CoordinatesToTile((double)v2, (double)v1, zoom, out px, out py);
                useValue = true;
            }

            if (!useValue) continue;

            isOptimized = false;

            if (optimize && i > 0)
            {
                if ((prx - px) * (prx - px) + (pry - py) * (pry - py) < 0.001)
                {
                    isOptimized = true;
                    continue;
                }
            }

            prx = px;
            pry = py;

            px -= sx;
            py -= sy;

            if (i == 0)
            {
                double ox = px - mapTileWidth;
                if (ox < -halfMax) px += max;
                else if (ox > halfMax) px -= max;
            }
            else
            {
                double ox = px - ppx;
                int maxIt = 3;
                while (maxIt-- > 0)
                {
                    if (ox < -halfMax)
                    {
                        px += max;
                        ox += max;
                    }
                    else if (ox > halfMax)
                    {
                        px -= max;
                        ox -= max;
                    }
                    else break;
                }
            }

            ppx = px;

            double rx1 = px * scaleX;
            double ry1 = py * scaleY;

            Vector2 np = new Vector2((float)rx1, (float)ry1);
            localPoints.Add(np);
        }

        if (isOptimized)
        {
            px -= sx;
            py -= sy;

            if (i == 0)
            {
                double ox = px - mapTileWidth;
                if (ox < -halfMax) px += max;
                else if (ox > halfMax) px -= max;
            }
            else
            {
                double ox = px - ppx;
                int maxIt = 3;
                while (maxIt-- > 0)
                {
                    if (ox < -halfMax)
                    {
                        px += max;
                        ox += max;
                    }
                    else if (ox > halfMax)
                    {
                        px -= max;
                        ox -= max;
                    }
                    else break;
                }
            }

            double rx1 = px * scaleX;
            double ry1 = py * scaleY;

            Vector2 np = new Vector2((float)rx1, (float)ry1);
            localPoints.Add(np);
        }

        if (closed && (localPoints[0] - localPoints[localPoints.Count - 1]).magnitude > sizeInScene.x / 256) localPoints.Add(localPoints[0]);

        return localPoints;
    }

    /// <summary>
    /// Determines if the drawing element at the specified coordinates.
    /// </summary>
    /// <param name="positionLngLat">
    /// Position.
    /// </param>
    /// <param name="zoom">
    /// The zoom.
    /// </param>
    /// <returns>
    /// True if the drawing element in position, false if not.
    /// </returns>
    public virtual bool HitTest(Vector2 positionLngLat, int zoom)
    {
        return false;
    }

    protected void InitLineMesh(IEnumerable points, OnlineMapsTileSetControl control, ref List<Vector3> vertices, ref List<Vector3> normals, ref List<int> triangles, ref List<Vector2> uv, float width, bool closed = false, bool optimize = true)
    {
        if (points == null) return;

        manager.map.buffer.GetCorners(out tlx, out tly, out brx, out bry);
        if (brx < tlx) brx += 360;

        List<Vector2> localPoints = GetLocalPoints(points, closed, optimize);
        List<Vector2> activePoints = new List<Vector2>(localPoints.Count);

        long maxX = 1L << manager.map.zoom;
        float maxSize = maxX * OnlineMapsUtils.tileSize * control.sizeInScene.x / manager.map.width / manager.map.zoomCoof;
        float halfSize = maxSize / 2;

        float lastPointX = 0;
        float lastPointY = 0;

        float sizeX = control.sizeInScene.x;
        float sizeY = control.sizeInScene.y;

        bestElevationYScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);

        if (vertices == null) vertices = new List<Vector3>(Mathf.Max(Mathf.NextPowerOfTwo(localPoints.Count * 4), 32));
        else vertices.Clear();

        if (normals == null) normals = new List<Vector3>(vertices.Capacity);
        else normals.Clear();

        if (triangles == null) triangles = new List<int>(Mathf.Max(Mathf.NextPowerOfTwo(localPoints.Count * 6), 32));
        else triangles.Clear();

        if (uv == null) uv = new List<Vector2>(vertices.Capacity);
        else uv.Clear();

        Vector2[] intersections = new Vector2[4];
        bool needExtraPoint = false;
        float extraX = 0, extraY = 0;

        bool isEntireWorld = manager.map.buffer.renderState.width == maxX * OnlineMapsUtils.tileSize;

        for (int i = 0; i < localPoints.Count; i++)
        {
            Vector2 p = localPoints[i];
            float px = p.x;
            float py = p.y;

            if (needExtraPoint)
            {
                activePoints.Add(new Vector2(extraX, extraY));

                float ox = extraX - lastPointX;
                if (ox > halfSize) lastPointX += maxSize;
                else if (ox < -halfSize) lastPointX -= maxSize;

                activePoints.Add(new Vector2(lastPointX, lastPointY));

                needExtraPoint = false;
            }

            if (i > 0 && checkMapBoundaries)
            {
                int countIntersections = 0;

                float ox = px - lastPointX;
                while (Math.Abs(ox) > halfSize)
                {
                    if (ox < 0)
                    {
                        px += maxSize;
                        ox += maxSize;
                    }
                    else if (ox > 0)
                    {
                        px -= maxSize;
                        ox -= maxSize;
                    }
                }

                float crossTopX, crossTopY, crossLeftX, crossLeftY, crossBottomX, crossBottomY, crossRightX, crossRightY;

                bool hasCrossTop =      OnlineMapsUtils.LineIntersection(lastPointX, lastPointY, px, py, 0,     0,     sizeX, 0,     out crossTopX,    out crossTopY);
                bool hasCrossBottom =   OnlineMapsUtils.LineIntersection(lastPointX, lastPointY, px, py, 0,     sizeY, sizeX, sizeY, out crossBottomX, out crossBottomY);
                bool hasCrossLeft =     OnlineMapsUtils.LineIntersection(lastPointX, lastPointY, px, py, 0,     0,     0,     sizeY, out crossLeftX,   out crossLeftY);
                bool hasCrossRight =    OnlineMapsUtils.LineIntersection(lastPointX, lastPointY, px, py, sizeX, 0,     sizeX, sizeY, out crossRightX,  out crossRightY);

                if (hasCrossTop)
                {
                    intersections[0] = new Vector2(crossTopX, crossTopY);
                    countIntersections++;
                }
                if (hasCrossBottom)
                {
                    intersections[countIntersections] = new Vector2(crossBottomX, crossBottomY);
                    countIntersections++;
                }
                if (hasCrossLeft)
                {
                    intersections[countIntersections] = new Vector2(crossLeftX, crossLeftY);
                    countIntersections++;
                }
                if (hasCrossRight)
                {
                    intersections[countIntersections] = new Vector2(crossRightX, crossRightY);
                    countIntersections++;
                }

                if (countIntersections == 1) activePoints.Add(intersections[0]);
                else if (countIntersections == 2)
                {
                    Vector2 lastPoint = new Vector2(lastPointX, lastPointY);
                    int minIndex = (lastPoint - intersections[0]).sqrMagnitude < (lastPoint - intersections[1]).sqrMagnitude? 0: 1;
                    activePoints.Add(intersections[minIndex]);
                    activePoints.Add(intersections[1 - minIndex]);
                }

                if (hasCrossLeft)
                {
                    needExtraPoint = OnlineMapsUtils.LineIntersection(lastPointX + maxSize, lastPointY, px + maxSize, py, sizeX, 0, sizeX, sizeY, out extraX, out extraY);
                }
                else if (hasCrossRight)
                {
                    needExtraPoint = OnlineMapsUtils.LineIntersection(lastPointX - maxSize, lastPointY, px - maxSize, py, 0, 0, 0, sizeY, out extraX, out extraY);
                }
                else if (isEntireWorld)
                {
                    if (px < 0)
                    {
                        DrawActivePoints(control, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);
                        px += maxSize;

                    }
                    else if (px > sizeX)
                    {
                        DrawActivePoints(control, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);
                        px -= maxSize;
                    }
                }
            }

            if (!checkMapBoundaries || px >= 0 && py >= 0 && px <= sizeX && py <= sizeY) activePoints.Add(new Vector2(px, py));
            else if (activePoints.Count > 0) DrawActivePoints(control, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);

            lastPointX = px;
            lastPointY = py;
        }

        if (needExtraPoint)
        {
            activePoints.Add(new Vector2(extraX, extraY));

            float ox = extraX - lastPointX;
            if (ox > halfSize) lastPointX += maxSize;
            else if (ox < -halfSize) lastPointX -= maxSize;

            activePoints.Add(new Vector2(lastPointX, lastPointY));
        }
        if (activePoints.Count > 0) DrawActivePoints(control, ref activePoints, ref vertices, ref normals, ref triangles, ref uv, width);
    }

    protected bool InitMesh(OnlineMapsTileSetControl control, Color borderColor, Color backgroundColor = default(Color), Texture borderTexture = null, Texture backgroundTexture = null)
    {
        if (mesh != null)
        {
            materials[0].color = borderColor;
            if (backgroundColor != default(Color)) materials[1].color = backgroundColor;
            return false;
        }

        gameObject = new GameObject(name);
        gameObject.transform.parent = control.drawingsGameObject.transform;
        gameObject.transform.localPosition = new Vector3(0, yOffset, 0);
        gameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
        gameObject.transform.localScale = Vector3.one;
        gameObject.layer = control.drawingsGameObject.layer;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        mesh = new Mesh {name = name};
        meshFilter.mesh = mesh;
        materials = new Material[createBackgroundMaterial?2: 1];
        Shader shader = control.drawingShader;
        Material borderMaterial = materials[0] = new Material(shader);
        borderMaterial.shader = shader;
        borderMaterial.color = borderColor;
        borderMaterial.mainTexture = borderTexture;

        if (createBackgroundMaterial)
        {
            Material backgroundMaterial = materials[1] = new Material(shader);
            backgroundMaterial.shader = shader;
            if (backgroundColor != default(Color)) backgroundMaterial.color = backgroundColor;
            backgroundMaterial.mainTexture = backgroundTexture;
        }

        renderer.materials = materials;
        for (int i = 0; i < materials.Length; i++) materials[i].renderQueue = shader.renderQueue + renderQueueOffset;

        if (OnInitMesh != null) OnInitMesh(this, renderer);

        return true;
    }

    /// <summary>
    /// It marks the elements changed.<br/>
    /// It is used for the Drawing API as an overlay.
    /// </summary>
    public static void MarkChanged()
    {
        lock (OnlineMapsTile.lockTiles)
        {
            foreach (OnlineMapsTile tile in OnlineMaps.instance.tileManager.tiles) tile.drawingChanged = true;
        }
    }

    private List<Vector2> SplitToPieces(OnlineMapsTileSetControl control, List<Vector2> activePoints)
    {
        List<Vector2> newPoints = new List<Vector2>(activePoints.Count);
        float d = control.sizeInScene.x / 4;
        Vector2 p1 = activePoints[0];
        newPoints.Add(p1);

        for (int i = 1; i < activePoints.Count; i++)
        {
            Vector2 p2 = activePoints[i];
            if ((p2 - p1).sqrMagnitude < d) newPoints.Add(p2);
            else SplitToPieces(newPoints, p1, p2, d);

            p1 = p2;
        }

        return newPoints;
    }

    private static void SplitToPieces(List<Vector2> points, Vector2 p1, Vector2 p2, float d)
    {
        Vector2 c = (p1 + p2) / 2;
        if ((p1 - c).sqrMagnitude < d) points.Add(c);
        else SplitToPieces(points, p1, c, d);

        if ((c - p2).sqrMagnitude < d) points.Add(p2);
        else SplitToPieces(points, c, p2, d);
    }

    protected void UpdateMaterialsQuote(OnlineMapsTileSetControl control, int index)
    {
        foreach (Material material in materials) material.renderQueue = control.drawingShader.renderQueue + renderQueueOffset + index;
    }

    public virtual bool Validate()
    {
        return true;
    }
}