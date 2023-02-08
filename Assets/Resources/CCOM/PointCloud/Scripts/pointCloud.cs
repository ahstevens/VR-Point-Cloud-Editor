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

    public bool validEPSG;

    public float groundLevel;

    public float easingTime = 1;

    private System.Collections.IEnumerator currentEaseCoroutine = null;

    EasingFunction.Function easeFunc;

    private Vector3 easeStartPos;
    private Vector3 easeStartScale;
    private Quaternion easeStartRot;

    private Vector3 easeTargetPos;
    private Vector3 easeTargetScale;
    private Quaternion easeTargetRot;

    void Awake()
    {
        easeFunc = EasingFunction.GetEasingFunction(EasingFunction.Ease.EaseInOutCubic);
        EPSG = -1;
        validEPSG = false;
    }

    void Update()
    {
        DrawBounds(bounds);

        if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            ResetMiniature(
                UserSettings.instance.preferences.fitSizeOnLoad,
                UserSettings.instance.preferences.distanceOnLoad
            );

        if (currentEaseCoroutine != null)
        {
            StartCoroutine(currentEaseCoroutine);
            currentEaseCoroutine = null;
        }
    }

    public void ResetMiniature(float size = 1f, float distance = 0.75f)
    {
        easeStartPos = this.transform.root.transform.position;
        easeStartRot = this.transform.root.transform.rotation;
        easeStartScale = this.transform.root.transform.localScale;
        
        // scale the point cloud down to 1 world unit on its largest dimension
        float scaleFactor = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        var targetPos = Camera.main.transform.position + Camera.main.transform.forward * distance;
        var targetRot = new Quaternion(0.0f, Camera.main.transform.rotation.y, 0.0f, Camera.main.transform.rotation.w);

        // move center of point cloud in front of camera
        easeTargetPos = targetPos - targetRot * (bounds.center * scaleFactor);
        easeTargetRot = targetRot;
        easeTargetScale = Vector3.one * scaleFactor;

        currentEaseCoroutine = EasingCoroutine(0.5f, size, distance);
    }

    public void ResetOrigin(float size = 1f)
    {
        // scale the point cloud down to 1 world unit on its largest dimension
        float scaleFactor = size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        // move center of point cloud in front of camera
        transform.root.position = Vector3.zero;
        transform.root.rotation = Quaternion.identity;
        transform.root.localScale = Vector3.one * scaleFactor;
    }

    System.Collections.IEnumerator EasingCoroutine(float easeTime, float size = 1f, float distance = 0.75f)
    {
        float waitTime = 0;
        while (waitTime <= 1)
        {
            float easeAmt = easeFunc(0, 1, waitTime);

            // calc rotation
            var rotAmt = Quaternion.Slerp(
                easeStartRot,
                //new Quaternion(0.0f, Camera.main.transform.rotation.y, 0.0f, Camera.main.transform.rotation.w),
                easeTargetRot,
                easeAmt);

            transform.root.rotation = rotAmt;

            // calc scale
            var easingScale = Vector3.Lerp(
                easeStartScale,
                //Vector3.one * size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
                easeTargetScale,
                easeAmt);

            transform.root.localScale = easingScale;

            // calc position
            var easingDist = Vector3.Lerp(
                easeStartPos,
                //Camera.main.transform.position + Camera.main.transform.forward * distance,
                easeTargetPos,
                easeAmt);

            transform.root.position = easingDist;

            yield return null;
            waitTime += Time.deltaTime / easeTime;
        }

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
