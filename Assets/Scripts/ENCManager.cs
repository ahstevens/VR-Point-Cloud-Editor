using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class ENCManager : MonoBehaviour
{

    public int resolution = 4096;
    public int EPSG = 3857;

    private GameObject ENC;

    void Start()
    {
    }

    public void CreateENC(GEOReference geoRef, pointCloud pc)
    {
        DestroyENC();

        ENC = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ENC.name = "ENC_" + pc.name;
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
        double minBBx = geoRef.realWorldX;
        double maxBBx = geoRef.realWorldX + pc.AABB_max_x;
        double minBBz = geoRef.realWorldZ;
        double maxBBz = geoRef.realWorldZ + pc.AABB_max_z;

        string url = $"https://gis.charttools.noaa.gov/arcgis/rest/services/MCS/ENCOnline/MapServer/exts/MaritimeChartService/WMSServer?LAYERS=0,1,2,3,4,5,6,7&FORMAT=image%2Fpng&CRS=EPSG:{EPSG}&SERVICE=WMS&REQUEST=GetMap&WIDTH={resolution}&HEIGHT={resolution}&BBOX={minBBx},{minBBz},{maxBBx},{maxBBz}";        


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

            ENC.transform.position = pc.bounds.center;
            ENC.transform.localScale = new Vector3(pc.bounds.max.x, pc.bounds.max.z, 1);

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