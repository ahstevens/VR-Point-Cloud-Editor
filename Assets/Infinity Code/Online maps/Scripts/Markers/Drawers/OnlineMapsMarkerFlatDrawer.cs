/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Implements the display of flat 2D markers on dynamic mesh control
/// </summary>
public class OnlineMapsMarkerFlatDrawer : OnlineMapsMarker2DMeshDrawer
{
    /// <summary>
    /// Checks if 2D marker is visible
    /// </summary>
    public Predicate<OnlineMapsMarker> OnCheckMarker2DVisibility;

    /// <summary>
    /// Called when generating vertices of markers
    /// </summary>
    public Action<OnlineMapsMarker, List<Vector3>, int> OnGenerateMarkerVertices;

    /// <summary>
    /// Gets the marker offset along the Y axis from the map
    /// </summary>
    public Func<OnlineMapsMarker, float> OnGetFlatMarkerOffsetY;

    /// <summary>
    /// Called after setting the value for marker mesh
    /// </summary>
    public Action<Mesh, Renderer> OnSetMarkersMesh;

    /// <summary>
    /// Allows you to change the order of drawing markers
    /// </summary>
    public IComparer<OnlineMapsMarker> markerComparer;

    private List<Vector3> markersVertices;
    private List<FlatMarker> usedMarkers;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="control">Reference to dynamic mesh control</param>
    public OnlineMapsMarkerFlatDrawer(OnlineMapsControlBaseDynamicMesh control)
    {
        this.control = control;
        map = control.map;
        control.OnDrawMarkers += OnDrawMarkers;
    }

    public override void Dispose()
    {
        base.Dispose();

        control.OnDrawMarkers -= OnDrawMarkers;
        control = null;

        if (markersGameObjects != null) foreach (GameObject go in markersGameObjects) OnlineMapsUtils.Destroy(go);
        if (usedMarkers != null) foreach (FlatMarker flatMarker in usedMarkers) flatMarker.Dispose();

        markerComparer = null;
        markersGameObjects = null;
        markersMeshes = null;
        markersRenderers = null;
        markersVertices = null;
        usedMarkers = null;

        OnCheckMarker2DVisibility = null;
        OnGenerateMarkerVertices = null;
        OnGetFlatMarkerOffsetY = null;
        OnSetMarkersMesh = null;
    }

    public override OnlineMapsMarker GetMarkerFromScreen(Vector2 screenPosition)
    {
        if (usedMarkers == null || usedMarkers.Count == 0) return null;

        OnlineMapsMarker marker = null;

        RaycastHit hit;
        if (control.cl.Raycast(control.currentCamera.ScreenPointToRay(screenPosition), out hit, OnlineMapsUtils.maxRaycastDistance))
        {
            double lng = double.MinValue, lat = double.MaxValue;
            foreach (FlatMarker flatMarker in usedMarkers)
            {
                if (flatMarker.Contains(hit.point, control.transform))
                {
                    double mx, my;
                    flatMarker.marker.GetPosition(out mx, out my);
                    if (my < lat || (Math.Abs(my - lat) < double.Epsilon && mx > lng)) marker = flatMarker.marker;
                }
            }
        }
        return marker;
    }

    private void OnDrawMarkers()
    {
        if (markersGameObjects == null) InitMarkersMesh(0);

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        if (brx < tlx) brx += 360;

        int zoom = map.buffer.renderState.zoom;
        int maxX = 1 << zoom;

        double tx, ty;
        map.projection.CoordinatesToTile(tlx, tly, zoom, out tx, out ty);

        float yScale = OnlineMapsElevationManagerBase.GetBestElevationYScale(tlx, tly, brx, bry);

        float cx = -control.sizeInScene.x / map.buffer.renderState.width;
        float cy = control.sizeInScene.y / map.buffer.renderState.height;

        if (usedMarkers == null) usedMarkers = new List<FlatMarker>(32);
        else
        {
            for (int i = 0; i < usedMarkers.Count; i++) usedMarkers[i].Dispose();
            usedMarkers.Clear();
        }

        List<Texture> usedTextures = new List<Texture>(32) { control.markerManager.defaultTexture };
        List<List<int>> usedTexturesMarkerIndex = new List<List<int>>(32) { new List<int>(32) };

        int usedMarkersCount = 0;

        Bounds tilesetBounds = new Bounds(
            new Vector3(control.sizeInScene.x / -2, 0, control.sizeInScene.y / 2), 
            new Vector3(control.sizeInScene.x, 0, control.sizeInScene.y));

        IEnumerable<OnlineMapsMarker> markers;
        if (control.markerManager.enabled)
        {
            markers = control.markerManager.Where(delegate (OnlineMapsMarker marker)
            {
                if (!marker.enabled || !marker.range.InRange(zoom)) return false;

                if (OnCheckMarker2DVisibility != null)
                {
                    if (!OnCheckMarker2DVisibility(marker)) return false;
                }
                else if (control.checkMarker2DVisibility == OnlineMapsTilesetCheckMarker2DVisibility.pivot)
                {
                    double mx, my;
                    marker.GetPosition(out mx, out my);

                    bool a = my > tly ||
                             my < bry ||
                             (
                                 (mx < tlx || mx > brx) &&
                                 (mx + 360 < tlx || mx + 360 > brx) &&
                                 (mx - 360 < tlx || mx - 360 > brx)
                             );
                    if (a) return false;
                }

                return true;
            });
        }
        else markers = new List<OnlineMapsMarker>();

        float[] offsets = null;
        bool useOffsetY = false;

        int index = 0;

        if (markerComparer != null)
        {
            markers = markers.OrderBy(m => m, markerComparer);
        }
        else
        {
            markers = markers.OrderBy(m =>
            {
                double mx, my;
                m.GetPosition(out mx, out my);
                return 90 - my;
            });
            useOffsetY = OnGetFlatMarkerOffsetY != null;

            if (useOffsetY)
            {
                int countMarkers = markers.Count();

                SortedMarker[] sortedMarkers = new SortedMarker[countMarkers];
                foreach (OnlineMapsMarker marker in markers)
                {
                    sortedMarkers[index++] = new SortedMarker
                    {
                        marker = marker,
                        offset = OnGetFlatMarkerOffsetY(marker)
                    };
                }

                offsets = new float[countMarkers];
                OnlineMapsMarker[] nMarkers = new OnlineMapsMarker[countMarkers];
                int i = 0;
                foreach (SortedMarker sm in sortedMarkers.OrderBy(m => m.offset))
                {
                    nMarkers[i] = sm.marker;
                    offsets[i] = sm.offset;
                    i++;
                    sm.Dispose();
                }
                markers = nMarkers;
            }
        }

        if (markersVertices == null) markersVertices = new List<Vector3>(64);
        else markersVertices.Clear();

        Vector3 tpos = control.transform.position;

        foreach (Mesh mesh in markersMeshes) mesh.Clear();

        float zoomCoof = map.buffer.renderState.zoomCoof;

        Matrix4x4 matrix = new Matrix4x4();
        int meshIndex = 0;
        bool elevationActive = hasElevation;
        index = -1;
        foreach (OnlineMapsMarker marker in markers)
        {
            index++;
            double fx, fy;
            marker.GetTilePosition(out fx, out fy);

            Vector2 offset = marker.GetAlignOffset();
            offset *= marker.scale;

            fx = fx - tx;

            if (fx < 0) fx += maxX;
            else if (fx > maxX) fx -= maxX;

            fx /= zoomCoof;

            fx = fx * OnlineMapsUtils.tileSize - offset.x;
            fy = (fy - ty) / zoomCoof * OnlineMapsUtils.tileSize - offset.y;

            if (marker.texture == null)
            {
                marker.texture = control.markerManager.defaultTexture;
                marker.Init();
            }

            float markerWidth = marker.texture.width * marker.scale;
            float markerHeight = marker.texture.height * marker.scale;

            float rx1 = (float)(fx * cx);
            float ry1 = (float)(fy * cy);
            float rx2 = (float)((fx + markerWidth) * cx);
            float ry2 = (float)((fy + markerHeight) * cy);

            Vector3 center = new Vector3((float)((fx + offset.x) * cx), 0, (float)((fy + offset.y) * cy));

            Vector3 p1 = new Vector3(rx1 - center.x, 0, ry1 - center.z);
            Vector3 p2 = new Vector3(rx2 - center.x, 0, ry1 - center.z);
            Vector3 p3 = new Vector3(rx2 - center.x, 0, ry2 - center.z);
            Vector3 p4 = new Vector3(rx1 - center.x, 0, ry2 - center.z);

            float angle = Mathf.Repeat(marker.rotation, 1) * 360;

            if (Math.Abs(angle) > float.Epsilon)
            {
                matrix.SetTRS(Vector3.zero, Quaternion.Euler(0, angle, 0), Vector3.one);

                p1 = matrix.MultiplyPoint(p1) + center;
                p2 = matrix.MultiplyPoint(p2) + center;
                p3 = matrix.MultiplyPoint(p3) + center;
                p4 = matrix.MultiplyPoint(p4) + center;
            }
            else
            {
                p1 += center;
                p2 += center;
                p3 += center;
                p4 += center;
            }

            if (control.checkMarker2DVisibility == OnlineMapsTilesetCheckMarker2DVisibility.bounds)
            {
                Vector3 markerCenter = (p2 + p4) / 2;
                Vector3 markerSize = p4 - p2;
                if (!tilesetBounds.Intersects(new Bounds(markerCenter, markerSize))) continue;
            }

            float y = elevationActive? elevationManager.GetElevationValue((rx1 + rx2) / 2, (ry1 + ry2) / 2, yScale, tlx, tly, brx, bry): 0;
            float yOffset = useOffsetY ? offsets[index] : 0;

            p1.y = p2.y = p3.y = p4.y = y + yOffset;

            int vIndex = markersVertices.Count;

            markersVertices.Add(p1);
            markersVertices.Add(p2);
            markersVertices.Add(p3);
            markersVertices.Add(p4);

            usedMarkers.Add(new FlatMarker(marker, p1 + tpos, p2 + tpos, p3 + tpos, p4 + tpos));

            if (OnGenerateMarkerVertices != null) OnGenerateMarkerVertices(marker, markersVertices, vIndex);

            if (marker.texture == control.markerManager.defaultTexture)
            {
                usedTexturesMarkerIndex[0].Add(usedMarkersCount);
            }
            else
            {
                int textureIndex = usedTextures.IndexOf(marker.texture);
                if (textureIndex != -1)
                {
                    usedTexturesMarkerIndex[textureIndex].Add(usedMarkersCount);
                }
                else
                {
                    usedTextures.Add(marker.texture);
                    usedTexturesMarkerIndex.Add(new List<int>(32));
                    usedTexturesMarkerIndex[usedTexturesMarkerIndex.Count - 1].Add(usedMarkersCount);
                }
            }

            usedMarkersCount++;

            if (usedMarkersCount == 16250)
            {
                SetMarkersMesh(usedMarkersCount, usedTextures, usedTexturesMarkerIndex, meshIndex);
                meshIndex++;
                markersVertices.Clear();
                usedMarkersCount = 0;
                usedTextures.Clear();
                usedTextures.Add(control.markerManager.defaultTexture);
                usedTexturesMarkerIndex.Clear();
                usedTexturesMarkerIndex.Add(new List<int>(32));
            }
        }

        SetMarkersMesh(usedMarkersCount, usedTextures, usedTexturesMarkerIndex, meshIndex);
    }

    private void SetMarkersMesh(int usedMarkersCount, List<Texture> usedTextures, List<List<int>> usedTexturesMarkerIndex, int meshIndex)
    {
        Vector2[] markersUV = new Vector2[markersVertices.Count];
        Vector3[] markersNormals = new Vector3[markersVertices.Count];

        Vector2 uvp1 = new Vector2(1, 1);
        Vector2 uvp2 = new Vector2(0, 1);
        Vector2 uvp3 = new Vector2(0, 0);
        Vector2 uvp4 = new Vector2(1, 0);

        for (int i = 0; i < usedMarkersCount; i++)
        {
            int vi = i * 4;
            markersNormals[vi] = Vector3.up;
            markersNormals[vi + 1] = Vector3.up;
            markersNormals[vi + 2] = Vector3.up;
            markersNormals[vi + 3] = Vector3.up;

            markersUV[vi] = uvp2;
            markersUV[vi + 1] = uvp1;
            markersUV[vi + 2] = uvp4;
            markersUV[vi + 3] = uvp3;
        }

        if (markersGameObjects == null) InitMarkersMesh(meshIndex);

        Mesh markersMesh = markersMeshes.Count > meshIndex ? markersMeshes[meshIndex] : null;
        if (markersMesh == null) markersMesh = InitMarkersMesh(meshIndex);

        markersMesh.SetVertices(markersVertices);

        markersMesh.uv = markersUV;
        markersMesh.normals = markersNormals;

        Renderer markersRenderer = markersRenderers[meshIndex];
        if (markersRenderer.materials.Length != usedTextures.Count) markersRenderer.materials = new Material[usedTextures.Count];

        markersMesh.subMeshCount = usedTextures.Count;

        for (int i = 0; i < usedTextures.Count; i++)
        {
            int markerCount = usedTexturesMarkerIndex[i].Count;
            int[] markersTriangles = new int[markerCount * 6];

            for (int j = 0; j < markerCount; j++)
            {
                int vi = usedTexturesMarkerIndex[i][j] * 4;
                int vj = j * 6;

                markersTriangles[vj + 0] = vi;
                markersTriangles[vj + 1] = vi + 1;
                markersTriangles[vj + 2] = vi + 2;
                markersTriangles[vj + 3] = vi;
                markersTriangles[vj + 4] = vi + 2;
                markersTriangles[vj + 5] = vi + 3;
            }

            markersMesh.SetTriangles(markersTriangles, i);

            Material material = markersRenderer.materials[i];
            if (material == null)
            {
                if (control.markerMaterial != null) material = markersRenderer.materials[i] = new Material(control.markerMaterial);
                else material = markersRenderer.materials[i] = new Material(control.markerShader);
            }

            if (material.mainTexture != usedTextures[i])
            {
                if (control.markerMaterial != null)
                {
                    material.shader = control.markerMaterial.shader;
                    material.CopyPropertiesFromMaterial(control.markerMaterial);
                    material.name = control.markerMaterial.name;
                }
                else
                {
                    material.shader = control.markerShader;
                    material.color = Color.white;
                }
                material.SetTexture("_MainTex", usedTextures[i]);
            }
        }

        if (OnSetMarkersMesh != null) OnSetMarkersMesh(markersMesh, markersRenderer);
    }

    internal class FlatMarker
    {
        public OnlineMapsMarker marker;
        private double[] poly;

        public FlatMarker(OnlineMapsMarker marker, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            this.marker = marker;
            poly = new double[] { p1.x, p1.z, p2.x, p2.z, p3.x, p3.z, p4.x, p4.z };
        }

        public bool Contains(Vector3 point, Transform transform)
        {
            Vector3 p = Quaternion.Inverse(transform.rotation) * (point - transform.position);
            p.x /= transform.lossyScale.x;
            p.z /= transform.lossyScale.z;
            p += transform.position;
            return OnlineMapsUtils.IsPointInPolygon(poly, p.x, p.z);
        }

        public void Dispose()
        {
            marker = null;
            poly = null;
        }
    }

    internal class SortedMarker
    {
        public OnlineMapsMarker marker;
        public float offset;

        public void Dispose()
        {
            marker = null;
        }
    }
}