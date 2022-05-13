using System;
using UnityEngine;

public class pointCloud : MonoBehaviour
{
    public String pathToRawData;
    public String ID;

    public double adjustmentX;
    public double adjustmentY;
    public double adjustmentZ;

    public double initialXShift;
    public double initialZShift;

    public string spatialInfo;
    public int UTMZone;
    public bool North;

    public double AABB_min_x;
    public double AABB_min_y;
    public double AABB_min_z;

    public double AABB_max_x;
    public double AABB_max_y;
    public double AABB_max_z;

    public int EPSG;

    public Bounds bounds;

    void Start()
    {
        bounds = new Bounds();
    }

    private void Update()
    {
        bounds.max = new Vector3((float)AABB_max_x, (float)AABB_max_y, (float)AABB_max_z);
        bounds.min = new Vector3((float)AABB_min_x, (float)AABB_min_y, (float)AABB_min_z);

        DrawBounds(bounds);
    }

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawSphere(transform.position, 100);
    //}

    //void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireCube(transform.position, new Vector3(1, 1, 1));
    //}

    //void OnDestroy()
    //{
    //    Debug.Log("pointCloud OnDestroy!");
    //    pointCloudManager.UnLoad(ID);
    //}

    public void ResetMiniature(float size = 1f, float distance = 0.75f)
    {
        Vector3 center = new Vector3(
            (float)(AABB_min_x + (AABB_max_x - AABB_min_x) / 2f),
            (float)(AABB_min_y + (AABB_max_y - AABB_min_y) / 2f),
            (float)(AABB_min_z + (AABB_max_z - AABB_min_z) / 2f)
        );

        Vector3 dimensions = new Vector3(
            (float)(AABB_max_x - AABB_min_x),
            (float)(AABB_max_y - AABB_min_y),
            (float)(AABB_max_z - AABB_min_z)
        );

        // scale the point cloud down to 1 world unit on its largest dimension
        float scaleFactor = size / Mathf.Max(dimensions.x, dimensions.y, dimensions.z);

        var targetPos = Camera.main.transform.position + Camera.main.transform.forward * distance;
        var targetRot = new Quaternion(0.0f, Camera.main.transform.rotation.y, 0.0f, Camera.main.transform.rotation.w);

        // move center of point cloud in front of camera
        transform.root.rotation = targetRot;
        transform.root.localScale = Vector3.one * scaleFactor;
        transform.root.position = targetPos - targetRot * (center * scaleFactor);
    }

    void DrawBounds(Bounds b, float delay = 0)
    {
        // bottom
        var p1 = transform.TransformPoint(new Vector3(b.min.x, b.min.y, b.min.z));
        var p2 = transform.TransformPoint(new Vector3(b.max.x, b.min.y, b.min.z));
        var p3 = transform.TransformPoint(new Vector3(b.max.x, b.min.y, b.max.z));
        var p4 = transform.TransformPoint(new Vector3(b.min.x, b.min.y, b.max.z));

        Debug.DrawLine(p1, p2, Color.blue, delay);
        Debug.DrawLine(p2, p3, Color.red, delay);
        Debug.DrawLine(p3, p4, Color.yellow, delay);
        Debug.DrawLine(p4, p1, Color.magenta, delay);

        // top
        var p5 = transform.TransformPoint(new Vector3(b.min.x, b.max.y, b.min.z));
        var p6 = transform.TransformPoint(new Vector3(b.max.x, b.max.y, b.min.z));
        var p7 = transform.TransformPoint(new Vector3(b.max.x, b.max.y, b.max.z));
        var p8 = transform.TransformPoint(new Vector3(b.min.x, b.max.y, b.max.z));

        Debug.DrawLine(p5, p6, Color.blue, delay);
        Debug.DrawLine(p6, p7, Color.red, delay);
        Debug.DrawLine(p7, p8, Color.yellow, delay);
        Debug.DrawLine(p8, p5, Color.magenta, delay);

        // sides
        Debug.DrawLine(p1, p5, Color.white, delay);
        Debug.DrawLine(p2, p6, Color.gray, delay);
        Debug.DrawLine(p3, p7, Color.green, delay);
        Debug.DrawLine(p4, p8, Color.cyan, delay);
    }
}
