using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

public class ENCManager : MonoBehaviour
{
    public GameObject controller;
    public GameObject HMD;
    public GameObject pointCloudRoot;

    public Shader ENCShader;

    public Texture2D errorTexture;

    public InputActionProperty adjustENCAction;

    public int resolution = 4096;

    public bool create;

    public GEOReference geoReference;
    public pointCloud pointCloud;

    private GameObject ENC;
    private bool adjusting;

    void Start()
    {
        adjustENCAction.action.started += ctx => BeginAdjustENCHeight();
        adjustENCAction.action.canceled += ctx => EndAdjustENCHeight();

        create = false;
        adjusting = false;

        //OnlineMaps.instance.zoom = 17;

        //OnlineMaps.instance.SetPosition(-90.0632247459028, 29.9205774420515);
    }

    void Update()
    {
        if (geoReference == null)
            geoReference = FindObjectOfType<GEOReference>();

        if (create)
        {
            if (geoReference != null && pointCloud != null)
                StartCoroutine(CreateENC(geoReference, pointCloud));

            create = false;
        }
        
        var gaze = HMD.transform.forward;
        var ctrlrDown = -controller.transform.up;

        if (ENC != null)
        {
            if (Vector3.Dot(gaze, ctrlrDown) < 0f)
            {
                ENC.SetActive(true);
            }
            else
            {
                adjusting = false;
                ENC.SetActive(false);
            }

            if (adjusting)
            {
                var newheight = pointCloudRoot.transform.InverseTransformPoint(controller.transform.position).y;
                ENC.transform.localPosition = new Vector3(ENC.transform.localPosition.x, newheight, ENC.transform.localPosition.z);
            }
        }

    }

    void OnEnable()
    {
        adjustENCAction.action.Enable();
    }

    void OnDisable()
    {
        adjustENCAction.action.Disable();
    }

    public IEnumerator CreateENC(GEOReference geoRef, pointCloud pc)
    {
        DestroyENC();

        var encMat = new Material(ENCShader);

        Debug.Log("Processing ENC image... ");
        float tick = Time.realtimeSinceStartup;
        yield return GetTexture(geoRef, pc, encMat);
        Debug.Log("ENC image processed in " + (Time.realtimeSinceStartup - tick) + " seconds");

        ENC = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ENC.name = "ENC_" + pc.name;

        ENC.transform.parent = pc.transform;
        ENC.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        ENC.transform.localScale = new Vector3(pc.bounds.extents.x * 2f, pc.bounds.extents.z * 2f, 1f);
        ENC.transform.localPosition = new Vector3(pc.bounds.center.x, pc.groundLevel, pc.bounds.center.z);

        Renderer rend = ENC.GetComponent<Renderer>();
        rend.material = encMat;
    }

    private void DestroyENC()
    {
        if (ENC != null)
            Destroy(ENC);
    }

    public IEnumerator GetTexture(GEOReference geoRef, pointCloud pc, Material encMat)
    {
        double minBBx = geoRef.realWorldX + pc.bounds.min.x;
        double maxBBx = geoRef.realWorldX + pc.bounds.max.x;
        double minBBz = geoRef.realWorldZ + pc.bounds.min.z;
        double maxBBz = geoRef.realWorldZ + pc.bounds.max.z;

        //SpatialReference src = new SpatialReference("");
        //src.ImportFromEPSG(pc.EPSG);
        //Debug.Log("SOURCE IsGeographic:" + src.IsGeographic() + " IsProjected:" + src.IsProjected());
        //SpatialReference dst = new SpatialReference("");
        //dst.ImportFromEPSG(4326);
        //Debug.Log("DEST IsGeographic:" + dst.IsGeographic() + " IsProjected:" + dst.IsProjected());
        //
        //CoordinateTransformation ct = new CoordinateTransformation(src, dst);
        //double[] ctr = new double[3];
        //double[] minBB = new double[3];
        //double[] maxBB = new double[3];
        //ctr[0] = minBBx + (maxBBx - minBBx) / 2.0;
        //ctr[1] = minBBz + (maxBBz - minBBz) / 2.0;
        //ctr[2] = 0;
        //minBB[0] = minBBx;
        //minBB[1] = minBBz;
        //minBB[2] = 0;
        //maxBB[0] = maxBBx;
        //maxBB[1] = maxBBz;
        //maxBB[2] = 0;
        //ct.TransformPoint(minBB);
        //ct.TransformPoint(ctr);
        //ct.TransformPoint(maxBB);
        //Debug.Log("MIN x:" + minBB[0] + " y:" + minBB[1] + " z:" + minBB[2]);
        //Debug.Log("CTR x:" + ctr[0] + " y:" + ctr[1] + " z:" + ctr[2]);
        //Debug.Log("MAX x:" + maxBB[0] + " y:" + maxBB[1] + " z:" + maxBB[2]);
        //
        //OnlineMapsMarker minMark = new OnlineMapsMarker();
        //OnlineMapsMarker maxMark = new OnlineMapsMarker();
        //minMark.SetPosition(minBB[0], minBB[1]);
        //maxMark.SetPosition(maxBB[0], maxBB[1]);
        //
        //OnlineMapsMarkerBase[] bbox = { minMark, maxMark };
        //
        //int zoom;
        //OnlineMapsUtils.GetCenterPointAndZoom(bbox, out _, out zoom);
        //
        //OnlineMaps.instance.zoom = zoom;
        //
        //OnlineMaps.instance.SetPosition(ctr[1], ctr[0]);
        //
        //OnlineMaps.instance.Redraw();
        //
        //Vector2 distanceKM = OnlineMapsUtils.DistanceBetweenPoints(OnlineMaps.instance.topLeftPosition,
        //        OnlineMaps.instance.bottomRightPosition);
        //
        //OnlineMapsControlBaseDynamicMesh.instance.sizeInScene = distanceKM * 1000;


        int epsg = pc.EPSG;

        // NAD83 (2011) / UTM15N || null || 
        if (epsg == 6344 || epsg == 0 || epsg == 32767)
            epsg = 26915; // NAD83 / UTM15N

        string url = $"https://gis.charttools.noaa.gov/arcgis/rest/services/MCS/ENCOnline/MapServer/exts/MaritimeChartService/WMSServer?LAYERS=0,1,2,3,4,5,6,7&FORMAT=image%2Fpng&CRS=EPSG:{epsg}&SERVICE=WMS&REQUEST=GetMap&WIDTH={resolution}&HEIGHT={resolution}&BBOX={minBBx},{minBBz},{maxBBx},{maxBBz}";

        Debug.Log(url);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

        Debug.Log("Downloading ENC image... ");
        float tick = Time.realtimeSinceStartup;
        www.SendWebRequest();

        while (!www.isDone)
        {
            if (www.downloadProgress > 0)
                Debug.Log("Downloading: " + www.downloadProgress * 100 + "%");
            yield return null;
        }

        Debug.Log("ENC image downloaded in " + (Time.realtimeSinceStartup - tick) + " seconds");

        bool success = www.result == UnityWebRequest.Result.Success;

        if (success)
        {
            Debug.Log("Converting image to Texture2D... ");
            tick = Time.realtimeSinceStartup;
            Texture2D encTex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            encMat.mainTexture = encTex;
            Debug.Log("Image converted to Texture2D in " + (Time.realtimeSinceStartup - tick) + " seconds");

            Debug.Log("Saving texture to disk... ");
            tick = Time.realtimeSinceStartup;
            yield return SaveTexture(encTex, pc.name);
            Debug.Log("Texture saved in " + (Time.realtimeSinceStartup - tick) + " seconds");
        }
        else
        {
            Debug.Log(www.error);

            encMat.mainTexture = errorTexture;
        }
    }

    IEnumerator SaveTexture(Texture2D texture, string name)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/ENCs";
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        //System.IO.File.WriteAllBytes(dirPath + "/ENC_" + name + ".png", bytes);
        yield return System.IO.File.WriteAllBytesAsync(dirPath + "/ENC_" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + dirPath);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

    private void BeginAdjustENCHeight()
    {
        if (ENC == null || !ENC.activeInHierarchy)
            return;

        adjusting = true;
    }

    private void EndAdjustENCHeight()
    {
        adjusting = false;
    }
}