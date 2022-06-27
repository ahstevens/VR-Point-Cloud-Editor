/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements drawing billboard markers
/// </summary>
public class OnlineMapsMarkerBillboardDrawer : OnlineMapsMarker2DMeshDrawer
{
    /// <summary>
    /// Size of markers
    /// </summary>
    public float marker2DSize
    {
        get { return control.marker2DSize; }
    }

    private Dictionary<int, OnlineMapsMarkerBillboard> markerBillboards;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="control">Reference to dynamic mesh control</param>
    public OnlineMapsMarkerBillboardDrawer(OnlineMapsControlBaseDynamicMesh control)
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

        if (markerBillboards != null)
        {
            foreach (KeyValuePair<int, OnlineMapsMarkerBillboard> pair in markerBillboards)
            {
                if (pair.Value != null) pair.Value.Dispose();
            }
        }

        if (markersGameObjects != null) foreach (GameObject go in markersGameObjects) OnlineMapsUtils.Destroy(go);

        markerBillboards = null;
        markersGameObjects = null;
        markersMeshes = null;
        markersRenderers = null;
        markerBillboards = null;
    }

    private void OnDrawMarkers()
    {
        if (markersGameObjects == null) InitMarkersMesh(0);
        if (markerBillboards == null) markerBillboards = new Dictionary<int, OnlineMapsMarkerBillboard>();

        double tlx, tly, brx, bry;
        map.GetCorners(out tlx, out tly, out brx, out bry);
        if (brx < tlx) brx += 360;

        double px, py;
        map.projection.CoordinatesToTile(tlx, tly, map.zoom, out px, out py);

        Bounds mapBounds = control.cl.bounds;
        Vector3 positionOffset = control.transform.position - mapBounds.min;
        Vector3 size = mapBounds.size;
        size = control.transform.rotation * size;
        if (!control.resultIsTexture) positionOffset.x -= size.x;

        foreach (KeyValuePair<int, OnlineMapsMarkerBillboard> billboard in markerBillboards) billboard.Value.used = false;

        foreach (OnlineMapsMarker marker in control.markerManager)
        {
            if (!marker.enabled || !marker.range.InRange(map.zoom)) continue;

            double mx, my;
            marker.GetPosition(out mx, out my);

            if (!((mx > tlx && mx < brx || mx + 360 > tlx && mx + 360 < brx ||
                   mx - 360 > tlx && mx - 360 < brx) &&
                  my < tly && my > bry)) continue;

            int markerHashCode = marker.GetHashCode();
            OnlineMapsMarkerBillboard markerBillboard;

            if (!markerBillboards.ContainsKey(markerHashCode))
            {
                markerBillboard = OnlineMapsMarkerBillboard.Create(marker);
                markerBillboard.transform.parent = markersGameObjects[0].transform;
                markerBillboard.gameObject.layer = markersGameObjects[0].layer;

                markerBillboards.Add(markerHashCode, markerBillboard);
            }
            else markerBillboard = markerBillboards[markerHashCode];

            if (markerBillboard == null) continue;

            float sx = size.x / map.buffer.renderState.width * marker2DSize * marker.scale;
            float sz = size.z / map.buffer.renderState.height * marker2DSize * marker.scale;
            float s = Mathf.Max(sx, sz);

            markerBillboard.transform.localScale = new Vector3(s, s, s);
            markerBillboard.transform.position = control.GetWorldPositionWithElevation(mx, my, tlx, tly, brx, bry);


            markerBillboard.used = true;
        }

        List<int> keysForRemove = new List<int>();

        foreach (KeyValuePair<int, OnlineMapsMarkerBillboard> billboard in markerBillboards)
        {
            if (!billboard.Value.used)
            {
                billboard.Value.Dispose();
                keysForRemove.Add(billboard.Key);
            }
        }

        foreach (int key in keysForRemove) markerBillboards.Remove(key);
    }
}