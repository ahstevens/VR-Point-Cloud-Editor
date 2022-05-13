using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ENCManager : MonoBehaviour
{

    public int resolution = 4096;

    public bool create = false;

    public GEOReference geoReference;
    public pointCloud pointCloud;

    private GameObject ENC;

    void Start()
    {
        create = false;
    }

    void Update()
    {
        if (geoReference == null)
            geoReference = FindObjectOfType<GEOReference>();
         
        if (create)
        {
            if (geoReference != null && pointCloud != null)
                CreateENC(geoReference, pointCloud);

            create = false;
        }
    }

    public void CreateENC(GEOReference geoRef, pointCloud pc)
    {
        DestroyENC();

        ENC = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ENC.name = "ENC_" + pc.name;

        ENC.transform.parent = pc.transform;
        ENC.transform.localRotation = Quaternion.Euler(90f, 0, 0);

        Renderer rend = ENC.GetComponent<Renderer>();
        rend.material = new Material(Shader.Find("Unlit/Texture"));

        StartCoroutine(GetTexture(geoRef, pc));
    }

    private void DestroyENC()
    {
        if (ENC != null)
            Destroy(ENC);
    }

    IEnumerator GetTexture(GEOReference geoRef, pointCloud pc)
    {
        double minBBx = geoRef.realWorldX + pc.AABB_min_x;
        double maxBBx = geoRef.realWorldX + pc.AABB_max_x;
        double minBBz = geoRef.realWorldZ + pc.AABB_min_z;
        double maxBBz = geoRef.realWorldZ + pc.AABB_max_z;

        int epsg = pc.EPSG;

        if (epsg == 6344 || epsg == 0)
            epsg = 26915;

        string url = $"https://gis.charttools.noaa.gov/arcgis/rest/services/MCS/ENCOnline/MapServer/exts/MaritimeChartService/WMSServer?LAYERS=0,1,2,3,4,5,6,7&FORMAT=image%2Fpng&CRS=EPSG:{epsg}&SERVICE=WMS&REQUEST=GetMap&WIDTH={resolution}&HEIGHT={resolution}&BBOX={minBBx},{minBBz},{maxBBx},{maxBBz}";        

        Debug.Log(url);        
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Texture2D myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;

            ENC.transform.localScale = new Vector3(pc.bounds.extents.x * 2, pc.bounds.extents.z * 2, 1);
            ENC.transform.localPosition = pc.bounds.center;

            SaveTexture(myTexture, pc.name);

            ENC.GetComponent<Renderer>().material.mainTexture = myTexture;
        }
    }

    private void SaveTexture(Texture2D texture, string name)
    {
        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/ENCs";
        if (!System.IO.Directory.Exists(dirPath))
        {
            System.IO.Directory.CreateDirectory(dirPath);
        }
        System.IO.File.WriteAllBytes(dirPath + "/ENC_" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + dirPath);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

}