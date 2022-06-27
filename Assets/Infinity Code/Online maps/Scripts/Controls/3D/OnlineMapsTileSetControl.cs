/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Class control the map for the Tileset.
/// Tileset - a dynamic mesh, created at runtime.
/// </summary>
[Serializable]
[AddComponentMenu("Infinity Code/Online Maps/Controls/Tileset")]
public class OnlineMapsTileSetControl : OnlineMapsControlBaseDynamicMesh
{
    #region Variables
    #region Actions
    /// <summary>
    /// The event, which occurs when the changed texture tile maps.
    /// </summary>
    public Action<OnlineMapsTile, Material> OnChangeMaterialTexture;

    /// <summary>
    /// The event that occurs after draw the tile.
    /// </summary>
    public Action<OnlineMapsTile, Material> OnDrawTile;

    #endregion

    #region Public Fields

    /// <summary>
    /// Type of collider: box - for performance, mesh - for elevation.
    /// </summary>
    public OnlineMapsColliderType colliderType = OnlineMapsColliderType.fullMesh;

    /// <summary>
    /// Compress texture to reduce memory usage.
    /// </summary>
    public bool compressTextures = true;

    /// <summary>
    /// Plane by using which the map is dragged. Exists only during drag.
    /// </summary>
    public Plane? dragPlane;

    /// <summary>
    /// Container for drawing elements.
    /// </summary>
    public GameObject drawingsGameObject;

    /// <summary>
    /// Drawing API mode (meshes or overlay).
    /// </summary>
    public OnlineMapsTilesetDrawingMode drawingMode = OnlineMapsTilesetDrawingMode.meshes;

    /// <summary>
    /// Shader of drawing elements.
    /// </summary>
    public Shader drawingShader;

    /// <summary>
    /// Material that will be used for tile.
    /// </summary>
    public Material tileMaterial;

    /// <summary>
    /// Shader of map.
    /// </summary>
    public Shader tilesetShader;

    #endregion

    #region Private Fields

    [SerializeField]
    private bool _mipmapForTiles = false;

    private BoxCollider boxCollider;
    private bool firstUpdate = true;
    private RaycastHit lastRaycastHit;
    private MeshCollider meshCollider;
    private Color32[] overlayFrontBuffer;
    private Mesh tilesetMesh;
    private int[] triangles;
    private Vector2[] uv;
    private Vector3[] vertices;
    private bool hasTrafficProp;
    private bool hasOverlayBackProp;
    private bool hasOverlayBackAlphaProp;
    private bool hasOverlayFrontProp;
    private bool hasOverlayFrontAlphaProp;
    private MeshProps meshProps;

    #endregion
    #endregion

    #region Properties

    /// <summary>
    /// Singleton instance of OnlineMapsTileSetControl control.
    /// </summary>
    public new static OnlineMapsTileSetControl instance
    {
        get { return _instance as OnlineMapsTileSetControl; }
    }

    public override bool mipmapForTiles
    {
        get { return _mipmapForTiles; }
        set { _mipmapForTiles = value; }
    }

    public override OnlineMapsTarget resultType
    {
        get { return OnlineMapsTarget.tileset; }
    }

    #endregion

    #region Methods

    private void CacheMaterialProperties()
    {
        Material fMaterial = rendererInstance.materials[0];
        hasTrafficProp = fMaterial.HasProperty("_TrafficTex");
        hasOverlayBackProp = fMaterial.HasProperty("_OverlayBackTex");
        hasOverlayBackAlphaProp = fMaterial.HasProperty("_OverlayBackAlpha");
        hasOverlayFrontProp = fMaterial.HasProperty("_OverlayFrontTex");
        hasOverlayFrontAlphaProp = fMaterial.HasProperty("_OverlayFrontAlpha");
    }

    private void GenerateTileVertices(MeshProps p, int x, int y, int i)
    {
        float sizeInSceneX = -sizeInScene.x;
        float sizeInSceneZ = sizeInScene.y;
        bool useElevation = hasElevation;
        float fy = 0;

        double spx = p.startPosX - x * p.subMeshSizeX;
        double spz = p.startPosZ + y * p.subMeshSizeY;

        int subMeshVX = p.subMeshVX;
        int subMeshVZ = p.subMeshVZ;
        double uvX = p.uvX;
        double uvZ = p.uvZ;
        double cellSizeX = p.cellSizeX;
        double cellSizeY = p.cellSizeY;

        Vector3 v1 = new Vector3();
        Vector2 v2 = new Vector2();

        for (int ty = 0; ty <= subMeshVZ; ty++)
        {
            double uvy = 1 - uvZ * ty;
            double pz = spz + ty * cellSizeY;

            if (pz < 0)
            {
                uvy = uvZ * ((pz + cellSizeY) / cellSizeY - 1) + uvy;
                pz = 0;
            }
            else if (pz > sizeInSceneZ)
            {
                uvy = uvZ * ((pz - sizeInSceneZ) / cellSizeY) + uvy;
                pz = sizeInSceneZ;
            }

            for (int tx = 0; tx <= subMeshVX; tx++)
            {
                double uvx = uvX * tx;
                double px = spx - tx * cellSizeX;

                if (px > 0)
                {
                    uvx = uvX * (px - cellSizeX) / cellSizeX + uvx + uvX;
                    px = 0;
                }
                else if (px < sizeInSceneX)
                {
                    uvx = uvX * ((px - sizeInSceneX) / cellSizeX - 1) + uvx + uvX;
                    px = sizeInSceneX;
                }

                v1.x = (float) px;
                v1.z = (float) pz;

                float fux = (float) uvx;
                float fuy = (float) uvy;

                if (useElevation)
                {
                    v1.y = fy = elevationManager.GetElevationValue(px, pz, p.yScale, p.tlx, p.tly, p.brx, p.bry);
                    if (fy < p.minY) p.minY = fy;
                    if (fy > p.maxY) p.maxY = fy;
                }

                if (fux < 0) fux = 0;
                else if (fux > 1) fux = 1;

                if (fuy < 0) fuy = 0;
                else if (fuy > 1) fuy = 1;

                v2.x = fux;
                v2.y = fuy;

                vertices[i] = v1;
                uv[i++] = v2;
            }
        }
    }

    public override bool GetCoords(Vector2 position, out double lng, out double lat)
    {
        lat = 0;
        lng = 0;

        if (!HitTest(position)) return false;
        return GetCoordsByWorldPosition(out lng, out lat, lastRaycastHit.point);
    }

    protected override bool GetCoordsInternal(out double lng, out double lat)
    {
        Vector2 position = GetInputPosition();
        if (dragPlane == null) return GetCoords(position, out lng, out lat);

        lat = lng = 0;

        float distance;
        Ray ray = activeCamera.ScreenPointToRay(position);
        if (!dragPlane.Value.Raycast(ray, out distance)) return false;

        return GetCoordsByWorldPosition(out lng, out lat, ray.GetPoint(distance));
    }

    /// <summary>
    /// Returns the geographical coordinates by world position.
    /// </summary>
    /// <param name="position">World position</param>
    /// <returns>Geographical coordinates or Vector2.zero</returns>
    public Vector2 GetCoordsByWorldPosition(Vector3 position)
    {
        Vector3 boundsSize = new Vector3(sizeInScene.x, 0, sizeInScene.y);
        boundsSize.Scale(transform.lossyScale);
        Vector3 size = new Vector3(0, 0, sizeInScene.y * transform.lossyScale.z) - Quaternion.Inverse(transform.rotation) * (position - transform.position);

        size.x = size.x / boundsSize.x;
        size.z = size.z / boundsSize.z;

        Vector2 r = new Vector3(size.x - .5f, size.z - .5f);

        float zoomCoof = map.buffer.renderState.zoomCoof;
        int countX = map.buffer.renderState.width / OnlineMapsUtils.tileSize;
        int countY = map.buffer.renderState.height / OnlineMapsUtils.tileSize;

        double px, py;
        map.GetTilePosition(out px, out py, map.buffer.renderState.zoom);
        px += countX * r.x * zoomCoof;
        py -= countY * r.y * zoomCoof;
        map.projection.TileToCoordinates(px, py, map.buffer.renderState.zoom, out px, out py);
        return new Vector2((float) px, (float) py);
    }

    /// <summary>
    /// Returns the geographical coordinates by world position.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <param name="position">World position</param>
    /// <returns>True - success, False - otherwise.</returns>
    public bool GetCoordsByWorldPosition(out double lng, out double lat, Vector3 position)
    {
        return GetCoordsByWorldPosition(position, out lng, out lat);
    }

    /// <summary>
    /// Returns the geographical coordinates by world position.
    /// </summary>
    /// <param name="position">World position</param>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <returns>True - success, False - otherwise.</returns>
    public bool GetCoordsByWorldPosition(Vector3 position, out double lng, out double lat)
    {
        lng = lat = 0;

        double tx, ty;
        if (!GetTileByWorldPosition(position, out tx, out ty)) return false;

        map.projection.TileToCoordinates(tx, ty, map.zoom, out lng, out lat);
        return true;
    }

    private Texture GetEmptyTileTexture()
    {
        if (map.defaultTileTexture != null) return map.defaultTileTexture;
        if (OnlineMapsRasterTile.emptyColorTexture != null) return OnlineMapsRasterTile.emptyColorTexture;
        
        Texture tileTexture = OnlineMapsRasterTile.emptyColorTexture = new Texture2D(1, 1, TextureFormat.ARGB32, mipmapForTiles);
        tileTexture.name = "Empty Texture";
        OnlineMapsRasterTile.emptyColorTexture.SetPixel(0, 0, map.emptyColor);
        OnlineMapsRasterTile.emptyColorTexture.Apply(false);

        return tileTexture;
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        double px, py;
        GetPosition(lng, lat, out px, out py);
        px /= map.buffer.renderState.width;
        py /= map.buffer.renderState.height;

        double cpx = -sizeInScene.x * px;
        double cpy = sizeInScene.y * py;

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);

        float elevationScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);
        float elevation = 0;
        if (hasElevation) elevation = elevationManager.GetElevationValue(cpx, cpy, elevationScale, tlx, tly, brx, bry);
        Vector3 worldPos = transform.position + transform.rotation * new Vector3((float)(cpx * transform.lossyScale.x), elevation * transform.lossyScale.y, (float)(cpy * transform.lossyScale.z));

        Camera cam = activeCamera != null? activeCamera: Camera.main;
        return cam.WorldToScreenPoint(worldPos);
    }

    private OnlineMapsTile GetTargetTile(OnlineMapsTile tile)
    {
        if (tile == null || tile.status == OnlineMapsTileStatus.loaded) return tile;

        int tx = tile.x;
        int ty = tile.y;

        int zoom = tile.zoom;
        int z = zoom;

        while (z > 0)
        {
            z--;

            int s = 1 << (zoom - z);
            int ctx = tx / s;
            int cty = ty / s;

            OnlineMapsTile t;
            map.tileManager.GetTile(z, ctx, cty, out t);
            if (t != null && t.status == OnlineMapsTileStatus.loaded) return t;
        }

        return null;
    }

    public override bool GetTile(Vector2 position, out double tx, out double ty)
    {
        tx = ty = 0;

        if (!HitTest(position)) return false;
        return GetTileByWorldPosition(lastRaycastHit.point, out tx, out ty);
    }


    public bool GetTileByWorldPosition(Vector3 position, out double tx, out double ty)
    {
        Vector3 boundsSize = new Vector3(sizeInScene.x, 0, sizeInScene.y);
        boundsSize.Scale(transform.lossyScale);
        Vector3 size = new Vector3(0, 0, sizeInScene.y * transform.lossyScale.z) - Quaternion.Inverse(transform.rotation) * (position - transform.position);

        size.x /= boundsSize.x;
        size.z /= boundsSize.z;

        Vector2 r = new Vector3(size.x - .5f, size.z - .5f);

        float zoomCoof = map.zoomCoof;
        int countX = map.buffer.renderState.width / OnlineMapsUtils.tileSize;
        int countY = map.buffer.renderState.height / OnlineMapsUtils.tileSize;

        map.GetTilePosition(out tx, out ty);
        tx += countX * r.x * zoomCoof;
        ty -= countY * r.y * zoomCoof;

        return true;
    }

    protected override bool GetTileInternal(Vector2 position, out double tx, out double ty)
    {
        if (dragPlane == null) return GetTile(position, out tx, out ty);

        tx = ty = 0;

        float distance;
        Ray ray = activeCamera.ScreenPointToRay(position);
        if (!dragPlane.Value.Raycast(ray, out distance)) return false;

        return GetTileByWorldPosition(ray.GetPoint(distance), out tx, out ty);
    }

    protected override bool HitTest(Vector2 position)
    {
#if NGUI
        if (UICamera.Raycast(position)) return false;
#endif
        Rect rect = activeCamera.rect;
        if (rect.width == 0 || rect.height == 0) return false;
        return cl.Raycast(activeCamera.ScreenPointToRay(position), out lastRaycastHit, OnlineMapsUtils.maxRaycastDistance);
    }

    private void InitDrawingsMesh()
    {
        drawingsGameObject = new GameObject("Drawings");
        drawingsGameObject.transform.parent = transform;
        drawingsGameObject.transform.localPosition = new Vector3(0, sizeInScene.magnitude / 4344, 0);
        drawingsGameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
        drawingsGameObject.transform.localScale = Vector3.one;
        drawingsGameObject.layer = gameObject.layer;
    }

    protected override void InitMapMesh()
    {
        base.InitMapMesh();

        Shader tileShader = tilesetShader;

        MeshFilter meshFilter;
        boxCollider = null;

        if (tilesetMesh == null)
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.hideFlags = HideFlags.HideInInspector;

            if (colliderType == OnlineMapsColliderType.fullMesh || colliderType == OnlineMapsColliderType.simpleMesh)
            {
                meshCollider = gameObject.GetComponent<MeshCollider>();
                if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            else if (colliderType == OnlineMapsColliderType.box || colliderType == OnlineMapsColliderType.flatBox)
            {
                boxCollider = gameObject.GetComponent<BoxCollider>();
                if (boxCollider == null) boxCollider = gameObject.AddComponent<BoxCollider>();
            }

            tilesetMesh = new Mesh {name = "Tileset"};
        }
        else
        {
            meshFilter = GetComponent<MeshFilter>();
            tilesetMesh.Clear();
        }

        int w1 = map.buffer.renderState.width / OnlineMapsUtils.tileSize;
        int h1 = map.buffer.renderState.height / OnlineMapsUtils.tileSize;

        int subMeshVX = 1;
        int subMeshVZ = 1;

        if (hasElevation)
        {
            if (w1 < elevationResolution) subMeshVX = elevationResolution % w1 == 0 ? elevationResolution / w1 : elevationResolution / w1 + 1;
            if (h1 < elevationResolution) subMeshVZ = elevationResolution % h1 == 0 ? elevationResolution / h1 : elevationResolution / h1 + 1;
        }

        Vector2 subMeshSize = new Vector2(sizeInScene.x / w1, sizeInScene.y / h1);

        int w = w1 + 2;
        int h = h1 + 2;

        int countVertices = w * h * (subMeshVX + 1) * (subMeshVZ + 1);
        vertices = new Vector3[countVertices];
        uv = new Vector2[countVertices];
        Vector3[] normals = new Vector3[countVertices];
        Material[] materials = new Material[w * h];
        tilesetMesh.subMeshCount = w * h;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                InitMapSubMesh(ref normals, x, y, w, h, subMeshSize, subMeshVX, subMeshVZ);
            }
        }

        tilesetMesh.vertices = vertices;
        tilesetMesh.uv = uv;
        tilesetMesh.normals = normals;

        triangles = null;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                InitMapSubMeshTriangles(ref materials, x, y, w, h, subMeshVX, subMeshVZ, tileShader);
            }
        }

        triangles = null;

        rendererInstance.materials = materials;
        CacheMaterialProperties();

        tilesetMesh.MarkDynamic();
        tilesetMesh.RecalculateBounds();
        meshFilter.sharedMesh = tilesetMesh;

        if (colliderType == OnlineMapsColliderType.fullMesh) meshCollider.sharedMesh = Instantiate(tilesetMesh) as Mesh;
        else if (colliderType == OnlineMapsColliderType.simpleMesh)
        {
            InitSimpleMeshCollider();
            meshCollider.sharedMesh = meshCollider.sharedMesh;
        }
        else if (boxCollider != null)
        {
            boxCollider.center = new Vector3(-sizeInScene.x / 2, 0, sizeInScene.y / 2);
            boxCollider.size = new Vector3(sizeInScene.x, 0, sizeInScene.y);
        }

        firstUpdate = false;

        UpdateMapMesh();
    }

    private void InitMapSubMesh(ref Vector3[] normals, int x, int y, int w, int h, Vector2 subMeshSize, int subMeshVX, int subMeshVZ)
    {
        int i = (x + y * w) * (subMeshVX + 1) * (subMeshVZ + 1);

        Vector2 cellSize = new Vector2(subMeshSize.x / subMeshVX, subMeshSize.y / subMeshVZ);

        float sx = x > 0 && x < w - 1 ? cellSize.x : 0;
        float sy = y > 0 && y < h - 1 ? cellSize.y : 0;

        float nextY = subMeshSize.y * (y - 1);

        float uvX = 1f / subMeshVX;
        float uvZ = 1f / subMeshVZ;

        for (int ty = 0; ty <= subMeshVZ; ty++)
        {
            float nextX = -subMeshSize.x * (x - 1);
            float uvy = 1 - uvZ * ty;

            for (int tx = 0; tx <= subMeshVX; tx++)
            {
                float uvx = 1 - uvX * tx;

                vertices[i] = new Vector3(nextX, 0, nextY);
                uv[i] = new Vector2(uvx, uvy);
                normals[i++] = new Vector3(0.0f, 1f, 0.0f);
                
                nextX -= sx;
            }

            nextY += sy;
        }
    }

    private void InitMapSubMeshTriangles(ref Material[] materials, int x, int y, int w, int h, int subMeshVX, int subMeshVZ, Shader tileShader)
    {
        if (triangles == null) triangles = new int[subMeshVX * subMeshVZ * 6];
        int i = (x + y * w) * (subMeshVX + 1) * (subMeshVZ + 1);

        for (int ty = 0; ty < subMeshVZ; ty++)
        {
            int cy = ty * subMeshVX * 6;
            int py1 = i + ty * (subMeshVX + 1);
            int py2 = i + (ty + 1) * (subMeshVX + 1);

            for (int tx = 0; tx < subMeshVX; tx++)
            {
                int ti = tx * 6 + cy;
                int p1 = py1 + tx;
                int p2 = p1 + 1;
                int p3 = py2 + tx;
                int p4 = p3 + 1;

                triangles[ti] = p1;
                triangles[ti + 1] = p2;
                triangles[ti + 2] = p4;
                triangles[ti + 3] = p1;
                triangles[ti + 4] = p4;
                triangles[ti + 5] = p3;
            }
        }

        tilesetMesh.SetTriangles(triangles, x + y * w);
        Material material;

        if (tileMaterial != null) material = Instantiate(tileMaterial) as Material;
        else material = new Material(tileShader);

        material.hideFlags = HideFlags.HideInInspector;

        if (map.defaultTileTexture != null) material.mainTexture = map.defaultTileTexture;
        materials[x + y * w] = material;
    }

    private void InitSimpleMeshCollider()
    {
        Mesh simpleMesh = new Mesh();
        simpleMesh.MarkDynamic();

        int res = hasElevation ? 6 : 1;
        int r2 = res + 1;
        Vector3[] vertices = new Vector3[r2 * r2];
        int[] triangles = new int[res * res * 6];

        float sx = -sizeInScene.x / res;
        float sy = sizeInScene.y / res;

        int ti = 0;

        for (int y = 0; y < r2; y++)
        {
            for (int x = 0; x < r2; x++)
            {
                vertices[y * r2 + x] = new Vector3(sx * x, 0, sy * y);

                if (x != 0 && y != 0)
                {
                    int p4 = y * r2 + x;
                    int p3 = p4 - 1;
                    int p2 = p4 - r2;
                    int p1 = p2 - 1;

                    triangles[ti++] = p1;
                    triangles[ti++] = p2;
                    triangles[ti++] = p4;
                    triangles[ti++] = p1;
                    triangles[ti++] = p4;
                    triangles[ti++] = p3;
                }
            }
        }

        simpleMesh.vertices = vertices;
        simpleMesh.SetTriangles(triangles, 0);
        simpleMesh.RecalculateBounds();

        meshCollider.sharedMesh = simpleMesh;
    }

    public override void OnAwakeBefore()
    {
        base.OnAwakeBefore();

        InitMapMesh();
    }

    protected override void OnDestroyLate()
    {
        base.OnDestroyLate();

        OnSmoothZoomBegin = null;
        OnSmoothZoomFinish = null;
        OnSmoothZoomProcess = null;

        if (drawingsGameObject != null) OnlineMapsUtils.Destroy(drawingsGameObject);
        drawingsGameObject = null;
        meshCollider = null;
        tilesetMesh = null;
        triangles = null;
        uv = null;
        vertices = null;
    }

    protected override void OnMapBasePress()
    {
        base.OnMapBasePress();

        if (isMapDrag)
        {
            //if (hasElevation && elevationManager.zoomRange.InRange(map.zoom) && dragPlane == null)
            {
                RaycastHit hit;
                if (cl.Raycast(activeCamera.ScreenPointToRay(GetInputPosition()), out hit, OnlineMapsUtils.maxRaycastDistance))
                {
                    dragPlane = new Plane(Vector3.up, new Vector3(0, hit.point.y, 0));
                }
            }
        }
    }

    protected override void OnMapBaseRelease()
    {
        base.OnMapBaseRelease();

        dragPlane = null;
    }

    protected override void ReinitMapMesh()
    {
        int width = map.width;
        int height = map.height;

        int w1 = width / OnlineMapsUtils.tileSize;
        int h1 = height / OnlineMapsUtils.tileSize;

        int subMeshVX = 1;
        int subMeshVZ = 1;

        if (hasElevation)
        {
            if (w1 < elevationResolution) subMeshVX = elevationResolution % w1 == 0 ? elevationResolution / w1 : elevationResolution / w1 + 1;
            if (h1 < elevationResolution) subMeshVZ = elevationResolution % h1 == 0 ? elevationResolution / h1 : elevationResolution / h1 + 1;
        }

        int w = w1 + 2;
        int h = h1 + 2;

        bufferPosition = null;

        Material[] materials = rendererInstance.materials;
        CacheMaterialProperties();

        vertices = new Vector3[w * h * (subMeshVX + 1) * (subMeshVZ + 1)];
        uv = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        Array.Resize(ref materials, w * h);

        for (int i = 0; i < normals.Length; i++) normals[i] = new Vector3(0, 1, 0);
        tilesetMesh.Clear();
        tilesetMesh.vertices = vertices;
        tilesetMesh.uv = uv;
        tilesetMesh.normals = normals;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null) continue;

            if (tileMaterial != null) materials[i] = Instantiate(tileMaterial) as Material;
            else materials[i] = new Material(tilesetShader);
            materials[i].hideFlags = HideFlags.HideInInspector;

            if (map.defaultTileTexture != null) materials[i].mainTexture = map.defaultTileTexture;
        }

        tilesetMesh.subMeshCount = w * h;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (triangles == null) triangles = new int[subMeshVX * subMeshVZ * 6];
                int i = (x + y * w) * (subMeshVX + 1) * (subMeshVZ + 1);

                for (int ty = 0; ty < subMeshVZ; ty++)
                {
                    int cy = ty * subMeshVX * 6;
                    int py1 = i + ty * (subMeshVX + 1);
                    int py2 = i + (ty + 1) * (subMeshVX + 1);

                    for (int tx = 0; tx < subMeshVX; tx++)
                    {
                        int ti = tx * 6 + cy;
                        int p1 = py1 + tx;
                        int p2 = py1 + tx + 1;
                        int p3 = py2 + tx;
                        int p4 = py2 + tx + 1;

                        triangles[ti] = p1;
                        triangles[ti + 1] = p2;
                        triangles[ti + 2] = p4;
                        triangles[ti + 3] = p1;
                        triangles[ti + 4] = p4;
                        triangles[ti + 5] = p3;
                    }
                }

                tilesetMesh.SetTriangles(triangles, x + y * w);
            }
        }

        triangles = null;
        rendererInstance.materials = materials;
        firstUpdate = true;
    }

    /// <summary>
    /// Resize map
    /// </summary>
    /// <param name="width">Width (pixels)</param>
    /// <param name="height">Height (pixels)</param>
    /// <param name="changeSizeInScene">Change the size of the map in the scene or leave the same.</param>
    public void Resize(int width, int height, bool changeSizeInScene = true)
    {
        Resize(width, height, changeSizeInScene? new Vector2(width, height) : sizeInScene);
    }

    /// <summary>
    /// Resize map
    /// </summary>
    /// <param name="width">Width (pixels)</param>
    /// <param name="height">Height (pixels)</param>
    /// <param name="sizeX">Size X (in scene)</param>
    /// <param name="sizeZ">Size Z (in scene)</param>
    public void Resize(int width, int height, float sizeX, float sizeZ)
    {
        Resize(width, height, new Vector2(sizeX, sizeZ));
    }

    /// <summary>
    /// Resize map
    /// </summary>
    /// <param name="width">Width (pixels)</param>
    /// <param name="height">Height (pixels)</param>
    /// <param name="sizeInScene">Size in scene (X-X, Y-Z)</param>
    public void Resize(int width, int height, Vector2 sizeInScene)
    {
        map.width = width;
        map.height = height;
        this.sizeInScene = sizeInScene;

        ReinitMapMesh();

        map.UpdateCorners();
        map.Redraw();
    }

    protected override OnlineMapsJSONItem SaveSettings()
    {
        return base.SaveSettings().AppendObject(new
        {
            checkMarker2DVisibility,
            tileMaterial,
            tilesetShader,
            drawingShader,
            markerMaterial,
            markerShader
        });
    }

    private void SetBackOverlayTexture(OnlineMapsTile tile, Material material)
    {
        if (!hasOverlayBackProp) return;

        Vector2 overlayTextureOffset = Vector2.zero;
        Vector2 overlayTextureScale = Vector2.one;
        Texture2D overlayTexture = (tile as OnlineMapsRasterTile).overlayBackTexture;

        int zoom = tile.zoom;
        int tx = tile.x;
        int ty = tile.y;

        if (overlayTexture == null)
        {
            OnlineMapsRasterTile t = tile.parent as OnlineMapsRasterTile;

            while (t != null)
            {
                if (t.overlayBackTexture != null)
                {
                    int s = 1 << (zoom - t.zoom);
                    float scale2 = 1f / s;
                    overlayTextureOffset.x = tx % s * scale2;
                    overlayTextureOffset.y = (s - ty % s - 1) * scale2;
                    overlayTextureScale = new Vector2(scale2, scale2);

                    overlayTexture = t.overlayBackTexture;
                    break;
                }

                t = t.parent as OnlineMapsRasterTile;
            }
        }

        material.SetTexture("_OverlayBackTex", overlayTexture);
        material.SetTextureOffset("_OverlayBackTex", overlayTextureOffset);
        material.SetTextureScale("_OverlayBackTex", overlayTextureScale);

        if (hasOverlayBackAlphaProp) material.SetFloat("_OverlayBackAlpha", tile.overlayBackAlpha);
    }

    private void SetEmptyMaterials(Material material)
    {
        if (map.defaultTileTexture != null) material.mainTexture = map.defaultTileTexture;
        else
        {
            if (OnlineMapsRasterTile.emptyColorTexture == null)
            {
                OnlineMapsRasterTile.emptyColorTexture = new Texture2D(1, 1, TextureFormat.ARGB32, mipmapForTiles);
                OnlineMapsRasterTile.emptyColorTexture.name = "Empty Texture";
                OnlineMapsRasterTile.emptyColorTexture.SetPixel(0, 0, map.emptyColor);
                OnlineMapsRasterTile.emptyColorTexture.Apply(false);
            }

            material.mainTexture = OnlineMapsRasterTile.emptyColorTexture;
        }

        if (hasTrafficProp) material.SetTexture("_TrafficTex", null);
        if (hasOverlayBackProp) material.SetTexture("_OverlayBackTex", null);
        if (hasOverlayFrontProp) material.SetTexture("_OverlayFrontTex", null);
    }

    private void SetFrontOverlayTexture(OnlineMapsTile tile, Material material)
    {
        if (!hasOverlayFrontProp) return;

        Texture2D overlayTexture = (tile as OnlineMapsRasterTile).overlayFrontTexture;
        if (drawingMode == OnlineMapsTilesetDrawingMode.overlay && overlayTexture == null)
        {
            if (tile.status == OnlineMapsTileStatus.loaded && tile.drawingChanged)
            {
                if (overlayFrontBuffer == null) overlayFrontBuffer = new Color32[OnlineMapsUtils.sqrTileSize];
                else
                {
                    for (int k = 0; k < OnlineMapsUtils.sqrTileSize; k++) overlayFrontBuffer[k] = new Color32();
                }

                foreach (OnlineMapsDrawingElement drawingElement in drawingElementManager)
                {
                    drawingElement.Draw(overlayFrontBuffer, new OnlineMapsVector2i(tile.x, tile.y), OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize, tile.zoom, false);
                }

                if (tile.overlayFrontTexture == null)
                {
                    tile.overlayFrontTexture = new Texture2D(OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize, TextureFormat.ARGB32, mipmapForTiles);
                    tile.overlayFrontTexture.wrapMode = TextureWrapMode.Clamp;
                }

                tile.overlayFrontTexture.SetPixels32(overlayFrontBuffer);
                tile.overlayFrontTexture.Apply(false);
            }
        }

        Vector2 overlayTextureOffset = Vector2.zero;
        Vector2 overlayTextureScale = Vector2.one;

        int zoom = tile.zoom;
        int tx = tile.x;
        int ty = tile.y;
        
        if (overlayTexture == null)
        {
            OnlineMapsRasterTile t = tile.parent as OnlineMapsRasterTile;

            while (t != null)
            {
                if (t.overlayFrontTexture != null)
                {
                    int s = 1 << (zoom - t.zoom);
                    float scale2 = 1f / s;
                    overlayTextureOffset.x = tx % s * scale2;
                    overlayTextureOffset.y = (s - ty % s - 1) * scale2;
                    overlayTextureScale = new Vector2(scale2, scale2);

                    overlayTexture = t.overlayFrontTexture;
                    break;
                }

                t = t.parent as OnlineMapsRasterTile;
            }
        }

        material.SetTexture("_OverlayFrontTex", overlayTexture);
        material.SetTextureOffset("_OverlayFrontTex", overlayTextureOffset);
        material.SetTextureScale("_OverlayFrontTex", overlayTextureScale);

        if (hasOverlayFrontAlphaProp) material.SetFloat("_OverlayFrontAlpha", tile.overlayFrontAlpha);
    }

    private void SetTileMaterials(OnlineMapsTile tile, OnlineMapsTile targetTile, Texture tileTexture, bool sendEvent, Material material, Vector2 offset, float scale)
    {
        if (targetTile == null)
        {
            SetEmptyMaterials(material);
            return;
        }

        if (tileTexture == null)
        {
            tileTexture = GetEmptyTileTexture();
            sendEvent = false;
        }

        material.mainTextureOffset = offset;
        material.mainTextureScale = new Vector2(scale, scale);

        if (material.mainTexture != tileTexture)
        {
            material.mainTexture = tileTexture;
            if (sendEvent && OnChangeMaterialTexture != null) OnChangeMaterialTexture(targetTile, material);
        }

        SetTrafficTexture(targetTile, material);
        SetBackOverlayTexture(tile, material);
        SetFrontOverlayTexture(tile, material);

        if (OnDrawTile != null) OnDrawTile(targetTile, material);
    }

    private void SetTrafficTexture(OnlineMapsTile tile, Material material)
    {
        if (!hasTrafficProp) return;

        if (!map.traffic)
        {
            material.SetTexture("_TrafficTex", null);
            return;
        }

        Vector2 trafficTextureOffset = material.mainTextureOffset;
        Vector2 trafficTextureScale = material.mainTextureScale;
        Texture2D trafficTexture = (tile as OnlineMapsRasterTile).trafficTexture;

        int zoom = tile.zoom;
        int tx = tile.x;
        int ty = tile.y;

        if (trafficTexture == null)
        {
            OnlineMapsRasterTile t = tile.parent as OnlineMapsRasterTile;

            while (t != null)
            {
                if (t.trafficTexture != null)
                {
                    int s = 1 << (zoom - t.zoom);
                    float scale2 = 1f / s;
                    trafficTextureOffset.x = tx % s * scale2;
                    trafficTextureOffset.y = (s - ty % s - 1) * scale2;
                    trafficTextureScale = new Vector2(scale2, scale2);

                    trafficTexture = t.trafficTexture;
                    break;
                }

                t = t.parent as OnlineMapsRasterTile;
            }
        }

        material.SetTexture("_TrafficTex", trafficTexture);
        material.SetTextureOffset("_TrafficTex", trafficTextureOffset);
        material.SetTextureScale("_TrafficTex", trafficTextureScale);
    }

    public override void UpdateControl()
    {
        base.UpdateControl();

        UpdateMapMesh();

        if (OnlineMapsDrawingElementManager.CountItems > 0)
        {
            if (drawingMode == OnlineMapsTilesetDrawingMode.meshes)
            {
                if (drawingsGameObject == null) InitDrawingsMesh();
                int index = 0;
                foreach (OnlineMapsDrawingElement drawingElement in drawingElementManager)
                {
                    drawingElement.DrawOnTileset(this, index++);
                }
            }
        }

        if (OnDrawMarkers != null) OnDrawMarkers();
    }

    private void UpdateMapMesh()
    {
        if (OnUpdateMeshBefore != null) OnUpdateMeshBefore();

        int zoom = map.buffer.renderState.zoom;

        int w1 = map.buffer.renderState.width / OnlineMapsUtils.tileSize;
        int h1 = map.buffer.renderState.height / OnlineMapsUtils.tileSize;

        int subMeshVX = 1;
        int subMeshVZ = 1;

        if (elevationManager == null)
        {
            elevationManager = GetComponent<OnlineMapsElevationManagerBase>();
            if (elevationManager == null) elevationManager = OnlineMapsElevationManagerBase.instance;
        }

        if (hasElevation)
        {
            if (w1 < elevationResolution) subMeshVX = elevationResolution % w1 == 0 ? elevationResolution / w1 : elevationResolution / w1 + 1;
            if (h1 < elevationResolution) subMeshVZ = elevationResolution % h1 == 0 ? elevationResolution / h1 : elevationResolution / h1 + 1;
        }

        float zoomScale = 1 - map.buffer.renderState.zoomScale / 2;

        double subMeshSizeX = sizeInScene.x / w1;
        double subMeshSizeY = sizeInScene.y / h1;

        double px = map.buffer.renderState.longitude;
        double py = map.buffer.renderState.latitude;

        double tlpx, tlpy;

        map.projection.CoordinatesToTile(px, py, zoom, out tlpx, out tlpy);
        double posX = tlpx - bufferPosition.x;
        double posY = tlpy - bufferPosition.y;

        posX -= w1 / 2d * zoomScale;
        posY -= h1 / 2d * zoomScale;

        int maxX = 1 << zoom;
        if (posX >= maxX) posX -= maxX;
        else if (posX < 0) posX += maxX;

        subMeshSizeX /= zoomScale;
        subMeshSizeY /= zoomScale;

        double startPosX = subMeshSizeX * posX;
        double startPosZ = -subMeshSizeY * posY;

        double tlx, tly, brx, bry;
        map.buffer.GetCorners(out tlx, out tly, out brx, out bry);

        float yScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);

        int w = w1 + 2;
        int h = h1 + 2;

        if (vertices.Length != w * h * (subMeshVX + 1) * (subMeshVZ + 1))
        {
            ReinitMapMesh();
            CacheMaterialProperties();
        }

        Material[] materials = rendererInstance.materials;

        if (meshProps == null) meshProps = new MeshProps();

        meshProps.Set(subMeshSizeX, subMeshSizeY, subMeshVX, subMeshVZ);
        meshProps.w = w;
        meshProps.h = h;
        meshProps.startPosX = startPosX;
        meshProps.startPosZ = startPosZ;
        meshProps.yScale = yScale;
        meshProps.tlx = tlx;
        meshProps.tly = tly;
        meshProps.brx = brx;
        meshProps.bry = bry;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                UpdateMapSubMesh(x, y, meshProps, materials);
            }
        }

        if (!hasElevation) meshProps.minY = meshProps.maxY = 0;

        tilesetMesh.vertices = vertices;
        tilesetMesh.uv = uv;

        tilesetMesh.RecalculateBounds();

        if (hasElevation || firstUpdate)
        {
            if (meshCollider != null)
            {
                if (firstUpdate || hasElevation && elevationManager.zoomRange.InRange(zoom))
                {
                    colliderWithElevation = true;
                    if (colliderType == OnlineMapsColliderType.fullMesh)
                    {
                        if (meshCollider.sharedMesh != null) OnlineMapsUtils.Destroy(meshCollider.sharedMesh);
                        meshCollider.sharedMesh = Instantiate(tilesetMesh) as Mesh;
                    }
                    else UpdateSimpleMeshCollider(yScale, tlx, tly, brx, bry);
                }
                else if (colliderWithElevation)
                {
                    colliderWithElevation = false;
                    if (colliderType == OnlineMapsColliderType.fullMesh)
                    {
                        if (meshCollider.sharedMesh != null) OnlineMapsUtils.Destroy(meshCollider.sharedMesh);
                        meshCollider.sharedMesh = Instantiate(tilesetMesh) as Mesh;
                    }
                    else UpdateSimpleMeshCollider(yScale, tlx, tly, brx, bry);
                }
            }
            else if (boxCollider != null)
            {
                boxCollider.center = new Vector3(-sizeInScene.x / 2, (meshProps.minY + meshProps.maxY) / 2, sizeInScene.y / 2);
                boxCollider.size = new Vector3(sizeInScene.x, colliderType == OnlineMapsColliderType.box ? meshProps.maxY - meshProps.minY : 0, sizeInScene.y);
            }

            firstUpdate = false;
        }

        if (OnMeshUpdated != null) OnMeshUpdated();
        if (OnUpdateMeshAfter != null) OnUpdateMeshAfter();
    }

    private void UpdateMapSubMesh(int x, int y, MeshProps p, Material[] materials)
    {
        int mi = x + y * p.w;
        int i = mi * (p.subMeshVX + 1) * (p.subMeshVZ + 1);

        int tx = x + bufferPosition.x;
        int ty = y + bufferPosition.y;

        int zoom = map.buffer.renderState.zoom;
        int maxX = 1 << zoom;

        if (tx >= maxX) tx -= maxX;
        if (tx < 0) tx += maxX;

        OnlineMapsTile tile;
        map.tileManager.GetTile(zoom, tx, ty, out tile);

        Vector2 offset = Vector2.zero;
        float scale = 1;
        Texture tileTexture = null;

        OnlineMapsTile targetTile = GetTargetTile(tile);
        bool sendEvent = true;

        if (targetTile != null)
        {
            if (tile != targetTile)
            {
                sendEvent = false;
                int s = 1 << (zoom - targetTile.zoom);
                tileTexture = targetTile.texture;
                scale = 1f / s;
                offset.x = tx % s * scale;
                offset.y = (s - ty % s - 1) * scale;
            }
            else tileTexture = tile.texture;
        }

        GenerateTileVertices(p, x, y, i);

        Material material = materials[mi];
        material.hideFlags = HideFlags.HideInInspector;

        SetTileMaterials(tile, targetTile, tileTexture, sendEvent, material, offset, scale);
    }

    private void UpdateSimpleMeshCollider(float yScale, double tlx, double tly, double brx, double bry)
    {
        bool elevationActive = hasElevation;

        int res = elevationActive ? 6 : 1;
        int r2 = res + 1;

        Vector3[] vertices = new Vector3[r2 * r2];
        float sx = -sizeInScene.x / res;
        float sy = sizeInScene.y / res;

        int[] triangles = new int[res * res * 6];
        int ti = 0;

        for (int y = 0; y < r2; y++)
        {
            for (int x = 0; x < r2; x++)
            {
                float px = sx * x;
                float pz = sy * y;

                float py = 0;
                if (elevationActive) py = elevationManager.GetElevationValue(px, pz, yScale, tlx, tly, brx, bry);

                vertices[y * r2 + x] = new Vector3(sx * x, py, sy * y);

                if (x != 0 && y != 0)
                {
                    int p4 = y * r2 + x;
                    int p3 = p4 - 1;
                    int p2 = p4 - r2;
                    int p1 = p2 - 1;

                    triangles[ti++] = p1;
                    triangles[ti++] = p2;
                    triangles[ti++] = p4;
                    triangles[ti++] = p1;
                    triangles[ti++] = p4;
                    triangles[ti++] = p3;
                }
            }
        }

        Mesh mesh = meshCollider.sharedMesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        meshCollider.sharedMesh = mesh;
    }

    #endregion

    #region Internal Types

    /// <summary>
    /// Type of tileset map collider.
    /// </summary>
    public enum OnlineMapsColliderType
    {
        box,
        fullMesh,
        simpleMesh,
        flatBox
    }

    public class MeshProps
    {
        public double cellSizeX, cellSizeY;
        public double uvX, uvZ;
        public double subMeshSizeX, subMeshSizeY;
        public int subMeshVX, subMeshVZ;
        public int w, h;
        public double startPosX, startPosZ;
        public float yScale;
        public float minY = float.PositiveInfinity;
        public float maxY = float.NegativeInfinity;
        public double tlx, tly, brx, bry;

        public void Set(double subMeshSizeX, double subMeshSizeY, int subMeshVX, int subMeshVZ)
        {
            this.subMeshSizeX = subMeshSizeX;
            this.subMeshSizeY = subMeshSizeY;
            this.subMeshVX = subMeshVX;
            this.subMeshVZ = subMeshVZ;

            cellSizeX = subMeshSizeX / subMeshVX;
            cellSizeY = subMeshSizeY / subMeshVZ;

            uvX = 1.0 / subMeshVX;
            uvZ = 1.0 / subMeshVZ;

            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;
        }
    }

    #endregion
}