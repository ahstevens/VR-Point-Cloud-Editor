using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using DotSpatial.Projections;

public class ENCManager : MonoBehaviour
{
    public GameObject controller;
    public GameObject HMD;
    public GameObject pointCloudRoot;

    public Shader ENCShader;

    public Texture2D errorTexture;

    public InputActionProperty adjustENCAction;

    public int resolution = 4096;

    public bool refreshing
    {
        get { return _refreshing; }
    }

    public GEOReference geoReference;
    public pointCloud pointCloud;
    public bool loaded
    {
        get { return _loaded; }
    }

    private GameObject ENC;
    private string encFileLocation;
    private bool adjusting;

    private bool _refreshing;
    private bool _loaded;

    void Start()
    {
        adjustENCAction.action.started += ctx => BeginAdjustENCHeight();
        adjustENCAction.action.canceled += ctx => EndAdjustENCHeight();

        adjusting = false;

        _refreshing = false;
        _loaded = false;
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
                FindObjectOfType<SatMapManager>().HideMap();
            }
            else
            {
                adjusting = false;
                ENC.SetActive(false);
                FindObjectOfType<SatMapManager>().ShowMap();
            }

            if (adjusting)
            {
                var newheight = pointCloudRoot.transform.InverseTransformPoint(controller.transform.position).y;
                ENC.transform.localPosition = new Vector3(ENC.transform.localPosition.x, newheight, ENC.transform.localPosition.z);

                FindObjectOfType<SatMapManager>().SetHeight(newheight);
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

    public GameObject GetENCObject()
    {
        return ENC;
    }

    public IEnumerator CreateENC(GEOReference geoRef, pointCloud pc, bool forceRefresh = false)
    {
        _refreshing = true;

        encFileLocation = Application.dataPath + "/ENCs" + "/ENC_" + pc.name + ".png";

        DestroyENC(forceRefresh);

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

        _refreshing = false;
        _loaded = true;
    }

    private void DestroyENC(bool deleteCached = false)
    {
        if (ENC != null)
        {
            Destroy(ENC);

            if (deleteCached && System.IO.File.Exists(encFileLocation))
            {
                System.IO.File.Delete(encFileLocation);
            }

            _loaded = false;
        }
    }

    public IEnumerator GetTexture(GEOReference geoRef, pointCloud pc, Material encMat)
    {
        if (System.IO.File.Exists(encFileLocation))
        {
            Debug.Log("Found existing ENC file " + encFileLocation);
            Texture2D tex = new(2, 2);
            tex.LoadImage(System.IO.File.ReadAllBytes(encFileLocation));
            encMat.mainTexture = tex;
        }
        else
        {
            double minBBx = geoRef.realWorldX + pc.bounds.min.x;
            double maxBBx = geoRef.realWorldX + pc.bounds.max.x;
            double minBBz = geoRef.realWorldZ + pc.bounds.min.z;
            double maxBBz = geoRef.realWorldZ + pc.bounds.max.z;

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

            bool doneDownloading = false;
            while (!www.isDone)
            {
                if (www.downloadProgress > 0 && ! doneDownloading)
                    Debug.Log("Downloading: " + www.downloadProgress * 100 + "%");

                if (www.downloadProgress >= 1f)
                    doneDownloading = true;

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
        yield return System.IO.File.WriteAllBytesAsync(encFileLocation, bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + encFileLocation);
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