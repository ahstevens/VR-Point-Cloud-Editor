using System;
using UnityEngine;
using OSGeo.OSR;

//[RequireComponent(typeof(LineRenderer))]
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

    public Bounds bounds;

    public int EPSG;

    public float groundLevel;

    void Awake()
    {
    }

    void Update()
    {
        DrawBounds(bounds);
    }

    public void ResetMiniature(float size = 1f, float distance = 0.75f)
    {
        // scale the point cloud down to 1 world unit on its largest dimension
        float scaleFactor = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        var targetPos = Camera.main.transform.position + Camera.main.transform.forward * distance;
        var targetRot = new Quaternion(0.0f, Camera.main.transform.rotation.y, 0.0f, Camera.main.transform.rotation.w);

        // move center of point cloud in front of camera
        transform.root.rotation = targetRot;
        transform.root.localScale = Vector3.one * scaleFactor;
        transform.root.position = targetPos - targetRot * (bounds.center * scaleFactor);
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

        //var lr = GetComponent<LineRenderer>();
        //lr.material = new Material(Shader.Find("Sprites/Default"));
        //lr.widthMultiplier = 0.01f;
        //lr.positionCount = 10;
        //
        //lr.SetPosition(0, p1);
        //lr.SetPosition(1, p2);
        //lr.SetPosition(2, p3);
        //lr.SetPosition(3, p4);
        //lr.SetPosition(4, p1);
        //
        //lr.SetPosition(5, p5);
        //lr.SetPosition(6, p6);
        //lr.SetPosition(7, p7);
        //lr.SetPosition(8, p8);
        //lr.SetPosition(9, p5);
    }

    public void TestTransform(int epsgFrom, int epsgTo = 3857)
    {
        // 4326 is WGS84 lat/lon for GPS etc.
        // 3857 is Web Mercator for Google Maps etc.
        try
        {
            /* -------------------------------------------------------------------- */
            /*      Initialize srs                                                  */
            /* -------------------------------------------------------------------- */
            SpatialReference src = new SpatialReference("");
            src.ImportFromEPSG(epsgFrom);
            Debug.Log("SOURCE IsGeographic:" + src.IsGeographic() + " IsProjected:" + src.IsProjected());
            SpatialReference dst = new SpatialReference("");
            dst.ImportFromEPSG(epsgTo);
            Debug.Log("DEST IsGeographic:" + dst.IsGeographic() + " IsProjected:" + dst.IsProjected());
            /* -------------------------------------------------------------------- */
            /*      making the transform                                            */
            /* -------------------------------------------------------------------- */
            CoordinateTransformation ct = new CoordinateTransformation(src, dst);
            double[] p = new double[3];
            GEOReference georef = FindObjectOfType<GEOReference>();
            p[0] = georef.realWorldX; p[1] = georef.realWorldZ; p[2] = 0;
            ct.TransformPoint(p);
            Debug.Log("x:" + p[0] + " y:" + p[1] + " z:" + p[2]);
        }
        catch (System.Exception e)
        {
            Debug.Log("Error occurred: " + e.Message);
        }
    }
}
