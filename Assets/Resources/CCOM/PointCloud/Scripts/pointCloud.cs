using System;
using UnityEngine;

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

        if (UnityEngine.InputSystem.Keyboard.current.vKey.wasPressedThisFrame)
            ResetMiniature(
                UserSettings.instance.preferences.fitSizeOnLoad,
                UserSettings.instance.preferences.distanceOnLoad
            );
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
}
