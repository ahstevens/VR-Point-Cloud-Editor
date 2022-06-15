//#define FRUSTUMCULLING_TEST
//#define AABB_TEST

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using System.IO;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class debugNodeBox
{
    public Vector3 center;
    public float size;
    public int depth;
    public debugNodeBox(Vector3 center, float size, int depth)
    {
        this.center = center;
        this.size = size;
        this.depth = depth;
    }
}

public class LODInformation
{
    public float maxDistance;
    public float targetPercentOFPoints;
    public int takeEach_Nth_Point;
};

public class pointCloudManager : MonoBehaviour
{
    [DllImport("PointCloudPlugin")]
    private static extern void updateCamera(IntPtr worldMatrix, IntPtr projectionMatrix, int screenIndex);
    [DllImport("PointCloudPlugin")]
    static public extern bool updateWorldMatrix(IntPtr worldMatrix, IntPtr pointCloudID);
    [DllImport("PointCloudPlugin")]
    private static extern IntPtr GetRenderEventFunc();
    [DllImport("PointCloudPlugin")]
    
    private static extern bool RequestToDeleteFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    private static extern int RequestOctreeBoundsCountFromUnity();
    [DllImport("PointCloudPlugin")]
    private static extern void RequestOctreeBoundsFromUnity(IntPtr arrayToFill);
    [DllImport("PointCloudPlugin")]
    private static extern int RequestOctreeDebugMaxNodeDepthFromUnity();
    [DllImport("PointCloudPlugin")]
    static public extern bool OpenLAZFileFromUnity(IntPtr filePath, IntPtr ID);
    [DllImport("PointCloudPlugin")]
    static public extern bool IsLastAsyncLoadFinished();
    [DllImport("PointCloudPlugin")]
    static public extern void OnSceneStartFromUnity(IntPtr projectFilePath);
    [DllImport("PointCloudPlugin")]
    static public extern bool SaveToLAZFileFromUnity(IntPtr filePath, IntPtr pointCloudID);
    [DllImport("PointCloudPlugin")]
    static public extern void SaveToOwnFormatFileFromUnity(IntPtr filePath, IntPtr pointCloudID);
    [DllImport("PointCloudPlugin")]
    static public extern void RequestPointCloudAdjustmentFromUnity(IntPtr adjustment, IntPtr ID);
    [DllImport("PointCloudPlugin")]
    static public extern void RequestPointCloudUTMZoneFromUnity(IntPtr UTMZone, IntPtr North, IntPtr ID);
    [DllImport("PointCloudPlugin")]
    static public extern void setFrustumCulling(bool active);
    [DllImport("PointCloudPlugin")]
    static public extern void setLODSystemActive(bool active);
    [DllImport("PointCloudPlugin")]
    static public extern void RequestLODInfoFromUnity(IntPtr maxDistance, IntPtr targetPercentOFPoints, int LODIndex, int pointCloudIndex);
    [DllImport("PointCloudPlugin")]
    static public extern void setLODInfo(IntPtr values, int LODIndex, int pointCloudIndex);
    [DllImport("PointCloudPlugin")]
    static public extern void RequestClosestPointToPointFromUnity(IntPtr initialPointPosition);
    [DllImport("PointCloudPlugin")]
    static public extern bool RequestIsAtleastOnePointInSphereFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void RequestClosestPointInSphereFromUnity(IntPtr center, float size);
    [DllImport("PointCloudPlugin")]
    static public extern void setHighlightDeletedPointsActive(bool active);
    [DllImport("PointCloudPlugin")]
    static public extern void UpdateDeletionSpherePositionFromUnity(IntPtr center, float size);

    [DllImport("PointCloudPlugin")]
    static public extern void undo(int actionsCount);

    [DllImport("PointCloudPlugin")]
    static public extern void DeleteOutliers_OLD(int pointCloudIndex, float outliersRange);
    [DllImport("PointCloudPlugin")]
    static public extern void highlightOutliers(float discardDistance, int minNeighborsInRange, IntPtr pointCloudID);
    [DllImport("PointCloudPlugin")]
    static public extern void deleteOutliers(IntPtr pointCloudID);
    

    [DllImport("PointCloudPlugin")]
    static public extern void setTestLevel(float unityTestLevel);
    [DllImport("PointCloudPlugin")]
    private static extern void updateTestCamera(IntPtr worldMatrix, IntPtr projectionMatrix);
    [DllImport("PointCloudPlugin")]
    private static extern bool ValidatePointCloudGMFromUnity(IntPtr filePath, IntPtr pointCloudID);
    [DllImport("PointCloudPlugin")]
    private static extern void getNewUniqueID(IntPtr IDToFill);
    [DllImport("PointCloudPlugin")]
    private static extern bool unLoad(IntPtr IDToFill);

    [DllImport("PointCloudPlugin")]
    private static extern void setScreenIndex(int newScreenIndex);
	
    List<Vector3> vertexData;
    List<Color32> vertexColors;
    public GameObject pointCloudGameObject;
    private List<List<LineRenderer>> octreeDepthViualizations;
    public static List<pointCloud> pointClouds;
    public static List<string> toLoadList;
    public static List<LODInformation> LODSettings;
#if FRUSTUMCULLING_TEST
    public Camera frustumCullingTestCamera;
#endif // FRUSTUMCULLING_TEST

    public static bool isWaitingToLoad = false;
    public static string filePathForAsyncLoad = "";
    public static float localLODTransitionDistance = 3500.0f;
    public static bool isLookingForClosestPoint = false;
    public static bool highlightDeletedPoints = false;

    private static bool renderPointClouds = true;

    static private GEOReference getReferenceScript()
    {
        GameObject geoReference = GameObject.Find("UnityZeroGeoReference");
        if (geoReference == null)
            return null;

        return geoReference.GetComponent<GEOReference>();
    }

    static private float getGEOScale()
    {
        GEOReference scriptClass = getReferenceScript();

        if (scriptClass == null)
            return 1.0f;

        return scriptClass.scale;
    }

    private static void createGEOReference(Vector3 GEOPosition, int UTMZone)
    {
        GameObject geoReference = new GameObject("UnityZeroGeoReference");
        geoReference.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

        // Add script
        GEOReference scriptClass = geoReference.AddComponent<GEOReference>();

        scriptClass.setRealWorldX(GEOPosition.x);
        scriptClass.setRealWorldZ(GEOPosition.z);

        scriptClass.UTMZone = UTMZone;
    }

    public static bool loadLAZFile(string filePath, GameObject reinitialization = null)
    {
        IntPtr strPtr = Marshal.StringToHGlobalAnsi(filePath);
        string LAZFileName = Path.GetFileNameWithoutExtension(filePath);

        string newID = GetNewID();
        IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(newID);

        if (!OpenLAZFileFromUnity(strPtr, IDStrPtr))
        {
            Debug.Log("Loading of " + filePath + " failed!");
            return false;
        }

        if (toLoadList == null)
            toLoadList = new List<string>();
        toLoadList.Add(newID);

        isWaitingToLoad = true;

#if UNITY_EDITOR
        EditorUtility.DisplayProgressBar("Point Cloud Plugin", "Loading Point Cloud...", 0.5f);
#endif

        if (pointClouds == null)
            pointClouds = new List<pointCloud>();

        LODSettings = new List<LODInformation>();
        IntPtr maxDistance = Marshal.AllocHGlobal(8);
        IntPtr targetPercentOFPoints = Marshal.AllocHGlobal(8);

        for (int i = 0; i < 4; i++)
        {
            LODSettings.Add(new LODInformation());
            RequestLODInfoFromUnity(maxDistance, targetPercentOFPoints, i, 0);
            float[] distance = new float[1];
            Marshal.Copy(maxDistance, distance, 0, 1);
            LODSettings[i].maxDistance = distance[0];

            float[] percentOFPoints = new float[1];
            Marshal.Copy(targetPercentOFPoints, percentOFPoints, 0, 1);
            LODSettings[i].targetPercentOFPoints = percentOFPoints[0];
        }

        Marshal.FreeHGlobal(maxDistance);
        Marshal.FreeHGlobal(targetPercentOFPoints);

        filePathForAsyncLoad = filePath;

        return true;
    }

    public static bool UnLoad(string pointCloudID)
    {
        bool result = false;
        IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointCloudID);
        result = unLoad(IDStrPtr);
        Marshal.FreeHGlobal(IDStrPtr);

        if (result)
        {
            pointCloud[] pointClouds = (pointCloud[])GameObject.FindObjectsOfType(typeof(pointCloud));
            for (int i = 0; i < pointClouds.Length; i++)
            {
                if (pointClouds[i].ID == pointCloudID)
                {
#if UNITY_EDITOR
                    DestroyImmediate(pointClouds[i].gameObject);
#else
					Destroy(pointClouds[i].gameObject);
#endif
                    break;
                }
            }

            pointClouds = (pointCloud[])GameObject.FindObjectsOfType(typeof(pointCloud));
            if (pointClouds.Length == 0)
            {
                // It is not intended to work fine along with bag loader.
                // Should fix that.
                GameObject geoReference = GameObject.Find("UnityZeroGeoReference");
                if (geoReference != null)
#if UNITY_EDITOR
                    DestroyImmediate(geoReference);
#else
					Destroy(geoReference);
#endif
            }
        }

        return result;
    }
	
    public static void SaveLAZFile(string filePath, string pointCloudID)
    {
        IntPtr strPtr = Marshal.StringToHGlobalAnsi(filePath);
		IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointCloudID);

        SaveToLAZFileFromUnity(strPtr, IDStrPtr);
		
		Marshal.FreeHGlobal(strPtr);
		Marshal.FreeHGlobal(IDStrPtr);
    }

    public static void SaveOwnFormatFile(string filePath, string pointCloudID)
    {
        IntPtr strPtr = Marshal.StringToHGlobalAnsi(filePath);
		IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointCloudID);
		
        SaveToOwnFormatFileFromUnity(strPtr, IDStrPtr);
		
		Marshal.FreeHGlobal(strPtr);
		Marshal.FreeHGlobal(IDStrPtr);
    }

    public static pointCloud[] getPointCloudsInScene()
    {
        pointCloud[] pointClouds = (pointCloud[])GameObject.FindObjectsOfType(typeof(pointCloud));
        return pointClouds;
    }

    public void reInitialize()
    {
        Camera.onPostRender += OnPostRenderCallback;

        pointClouds = new List<pointCloud>();
        LODSettings = new List<LODInformation>();
        IntPtr maxDistance = Marshal.AllocHGlobal(8);
        IntPtr targetPercentOFPoints = Marshal.AllocHGlobal(8);

        for (int i = 0; i < 4; i++)
        {
            LODSettings.Add(new LODInformation());
            RequestLODInfoFromUnity(maxDistance, targetPercentOFPoints, i, 0);
            float[] distance = new float[1];
            Marshal.Copy(maxDistance, distance, 0, 1);
            LODSettings[i].maxDistance = distance[0];

            float[] percentOFPoints = new float[1];
            Marshal.Copy(targetPercentOFPoints, percentOFPoints, 0, 1);
            LODSettings[i].targetPercentOFPoints = percentOFPoints[0];
        }

        Marshal.FreeHGlobal(maxDistance);
        Marshal.FreeHGlobal(targetPercentOFPoints);
    }

    public static void checkIsAsyncLoadFinished()
    {
        if (!isWaitingToLoad)
            return;

        if (IsLastAsyncLoadFinished() && toLoadList.Count > 0)
        {
            string ID = toLoadList[0];
            toLoadList.RemoveAt(0);
            IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(ID);

            // Create game object that will represent point cloud.
            string name = Path.GetFileNameWithoutExtension(filePathForAsyncLoad);
            GameObject pointCloudGameObject = new GameObject(name);

            // Add script to a point cloud game object.
            var pcComponent = pointCloudGameObject.AddComponent<pointCloud>();
            pcComponent.ID = ID;

            IntPtr adjustmentArray = Marshal.AllocHGlobal(8 * 12);
            RequestPointCloudAdjustmentFromUnity(adjustmentArray, IDStrPtr);

            IntPtr UTMZone = Marshal.AllocHGlobal(8);
            IntPtr North = Marshal.AllocHGlobal(8);
            RequestPointCloudUTMZoneFromUnity(UTMZone, North, IDStrPtr);
            int[] zone = new int[1];
            Marshal.Copy(UTMZone, zone, 0, 1);

            int[] north = new int[1];
            Marshal.Copy(North, north, 0, 1);

            Marshal.FreeHGlobal(IDStrPtr);

            pcComponent.UTMZone = zone[0];
            pcComponent.North = north[0] == 1;

            string rawDataPath = Application.dataPath + "/Resources/CCOM/PointCloud/Data/" + Path.GetFileName(filePathForAsyncLoad);

            pcComponent.pathToRawData = filePathForAsyncLoad;
#if UNITY_EDITOR
            pcComponent.pathToRawData = rawDataPath;
#endif

            double[] adjustmentResult = new double[13];
            Marshal.Copy(adjustmentArray, adjustmentResult, 0, 13);

            pcComponent.adjustmentX = adjustmentResult[0];
            pcComponent.adjustmentY = adjustmentResult[1];
            pcComponent.adjustmentZ = adjustmentResult[2];

            pcComponent.AABB_min_x = adjustmentResult[5];
            pcComponent.AABB_min_y = adjustmentResult[6];
            pcComponent.AABB_min_z = adjustmentResult[7];

            pcComponent.AABB_max_x = adjustmentResult[8];
            pcComponent.AABB_max_y = adjustmentResult[9];
            pcComponent.AABB_max_z = adjustmentResult[10];

            pcComponent.UpdateBounds();

            pcComponent.EPSG = (int)(adjustmentResult[11]);

            pcComponent.groundLevel = (float)adjustmentResult[12];

            //pointClouds[pointClouds.Count - 1].LODs = new List<LODInformation>();
            //IntPtr maxDistance = Marshal.AllocHGlobal(8);
            //IntPtr targetPercentOFPoints = Marshal.AllocHGlobal(8);

            //for (int i = 0; i < 4; i++)
            //{
            //    pointClouds[pointClouds.Count - 1].LODs.Add(new LODInformation());
            //    RequestLODInfoFromUnity(maxDistance, targetPercentOFPoints, i, pointClouds.Count - 1);
            //    float[] distance = new float[1];
            //    Marshal.Copy(maxDistance, distance, 0, 1);
            //    pointClouds[pointClouds.Count - 1].LODs[i].maxDistance = distance[0];

            //    float[] percentOFPoints = new float[1];
            //    Marshal.Copy(targetPercentOFPoints, percentOFPoints, 0, 1);
            //    pointClouds[pointClouds.Count - 1].LODs[i].targetPercentOFPoints = percentOFPoints[0];
            //}

            //Marshal.FreeHGlobal(maxDistance);
            //Marshal.FreeHGlobal(targetPercentOFPoints);

            if (getReferenceScript() == null)
            {
                createGEOReference(new Vector3((float)adjustmentResult[3], 0.0f, (float)adjustmentResult[4]), pcComponent.UTMZone);
            }
            else
            {
                pcComponent.initialXShift = -(getReferenceScript().realWorldX - adjustmentResult[3]);
                pcComponent.initialZShift = -(getReferenceScript().realWorldZ - adjustmentResult[4]);
            }

            // Default value for y, it should be calculated but for now it is a magic number.
            //float y = 905.0f;
            float y = 0f;

            // If we are re initializing existing objects, we should preserve y coordinate.
            GameObject pcRoot = GameObject.Find("Point Clouds Root");

            if (pcRoot != null)
            {
                pointCloudGameObject.transform.parent = pcRoot.transform;

                if (pointClouds.Count == 0)
                {
                    pcComponent.ResetMiniature();

                    var encm = FindObjectOfType<ENCManager>();
                    encm.geoReference = getReferenceScript();
                    encm.pointCloud = pcComponent;
                    encm.CreateENC(getReferenceScript(), pcComponent);
                }
            }

            pointCloudGameObject.transform.localPosition = new Vector3((float)(pcComponent.initialXShift),
                                                                  y,
                                                                  (float)(pcComponent.initialZShift));

            pointCloudGameObject.transform.localRotation = Quaternion.identity;
            pointCloudGameObject.transform.localScale = Vector3.one;

            isWaitingToLoad = false;

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
        }
#if UNITY_EDITOR
        else
        {
            EditorUtility.DisplayProgressBar("Point Cloud Plugin", "Loading Point Cloud...", UnityEngine.Random.Range(0f, 1f));
        }
#endif
    }

    void OnValidate()
    {
        Camera.onPostRender = pointCloudManager.OnPostRenderCallback;

#if UNITY_EDITOR
        EditorSceneManager.sceneSaved -= OnSceneSaveCallback;
        EditorSceneManager.sceneSaved += OnSceneSaveCallback;

        EditorApplication.playModeStateChanged -= ClearPointCloudsOnPlayModeExit;
        EditorApplication.playModeStateChanged += ClearPointCloudsOnPlayModeExit;
#endif
        OnSceneStartFromUnity(Marshal.StringToHGlobalAnsi(Application.dataPath));

        pointCloud[] pointClouds = (pointCloud[])GameObject.FindObjectsOfType(typeof(pointCloud));
        for (int i = 0; i < pointClouds.Length; i++)
        {
            IntPtr strPtr = Marshal.StringToHGlobalAnsi(pointClouds[i].pathToRawData);
            IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointClouds[i].ID);

            if (ValidatePointCloudGMFromUnity(strPtr, IDStrPtr))
            {
                if (toLoadList == null)
                    toLoadList = new List<string>();

                toLoadList.Add(pointClouds[i].ID);
            }
			
			Marshal.FreeHGlobal(strPtr);
			Marshal.FreeHGlobal(IDStrPtr);
        }
    }

#if UNITY_EDITOR
    private static void ClearPointCloudsOnPlayModeExit(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            foreach (var pc in pointCloudManager.getPointCloudsInScene())
            {
                pointCloudManager.UnLoad(pc.ID);
            }
        }
    }
#endif

    void Start()
    {
        OnValidate();
        //InvokeRepeating("checkIsAsyncLoadFinished", 1.0f, 0.3f);
        //reInitialize();
#if AABB_TEST
        double lastInterval = Time.realtimeSinceStartup;

        double time = (Time.realtimeSinceStartup - lastInterval) * 1000;
        Debug.Log("Octree build time: " + time + " ms.");

        List<debugNodeBox> list = new List<debugNodeBox>();
        int testSize = RequestOctreeBoundsCountFromUnity();
        // 8 bytes for 64 bit dll
        IntPtr testArrayToFill = Marshal.AllocHGlobal(testSize * 8 * 5);
        RequestOctreeBoundsFromUnity(testArrayToFill);

        float[] testArray = new float[testSize * 5];
        Marshal.Copy(testArrayToFill, testArray, 0, testSize * 5);

        for (int i = 0; i < testSize; i++)
        {
            list.Add(new debugNodeBox(new Vector3(testArray[i * 5], testArray[i * 5 + 1], testArray[i * 5 + 2]), testArray[i * 5 + 3], (int)testArray[i * 5 + 4]));
        }
        Marshal.FreeHGlobal(testArrayToFill);

        int boxCount = list.Count;
        LineRenderer[] lines = new LineRenderer[boxCount * 12];

        int debugMaxNodeDepth = RequestOctreeDebugMaxNodeDepthFromUnity();
        octreeDepthViualizations = new List<List<LineRenderer>>();
        for (int i = 0; i < debugMaxNodeDepth + 1; i++)
        {
            octreeDepthViualizations.Add(new List<LineRenderer>());
        }

        //lastInterval = Time.realtimeSinceStartup;
        for (int i = 0; i < boxCount; i++)
        {
            float currentSize = list[i].size / 2.0f;
            for (int j = 0; j < 12; j++)
            {
                GameObject gObject = new GameObject("MyGameObject");
                lines[i * 12 + j] = gObject.AddComponent<LineRenderer>();
                lines[i * 12 + j].material = new Material(Shader.Find("Sprites/Default"));

                octreeDepthViualizations[list[i].depth].Add(lines[i * 12 + j]);

                if (list[i].depth == 1)
                {
                    lines[i * 12 + j].startColor = Color.green;
                    lines[i * 12 + j].endColor = Color.green;
                }
                else if (list[i].depth == 2)
                {
                    lines[i * 12 + j].startColor = Color.blue;
                    lines[i * 12 + j].endColor = Color.blue;
                }
                else if (list[i].depth == 3)
                {
                    lines[i * 12 + j].startColor = Color.yellow;
                    lines[i * 12 + j].endColor = Color.yellow;
                }
                else if (list[i].depth == 4)
                {
                    lines[i * 12 + j].startColor = Color.cyan;
                    lines[i * 12 + j].endColor = Color.cyan;
                }
                else if (list[i].depth == 5)
                {
                    lines[i * 12 + j].startColor = Color.magenta;
                    lines[i * 12 + j].endColor = Color.magenta;
                }
                else
                {
                    lines[i * 12 + j].startColor = Color.red;
                    lines[i * 12 + j].endColor = Color.red;
                }

                lines[i * 12 + j].widthMultiplier = 0.5f;
                lines[i * 12 + j].positionCount = 2;
            }

            list[i].center = pointClouds[pointClouds.Count - 1].inSceneRepresentation.transform.localToWorldMatrix * new Vector4(list[i].center.x, list[i].center.y, list[i].center.z, 1.0f);

            // bottom
            lines[i * 12 + 0].SetPosition(0, list[i].center + new Vector3(-currentSize, -currentSize, -currentSize));
            lines[i * 12 + 0].SetPosition(1, list[i].center + new Vector3(-currentSize, -currentSize, currentSize));
            lines[i * 12 + 1].SetPosition(0, list[i].center + new Vector3(-currentSize, -currentSize, currentSize));
            lines[i * 12 + 1].SetPosition(1, list[i].center + new Vector3(currentSize, -currentSize, currentSize));
            lines[i * 12 + 2].SetPosition(0, list[i].center + new Vector3(currentSize, -currentSize, currentSize));
            lines[i * 12 + 2].SetPosition(1, list[i].center + new Vector3(currentSize, -currentSize, -currentSize));
            lines[i * 12 + 3].SetPosition(0, list[i].center + new Vector3(currentSize, -currentSize, -currentSize));
            lines[i * 12 + 3].SetPosition(1, list[i].center + new Vector3(-currentSize, -currentSize, -currentSize));

            // vertical connections
            lines[i * 12 + 4].SetPosition(0, list[i].center + new Vector3(-currentSize, -currentSize, -currentSize));
            lines[i * 12 + 4].SetPosition(1, list[i].center + new Vector3(-currentSize, currentSize, -currentSize));
            lines[i * 12 + 5].SetPosition(0, list[i].center + new Vector3(-currentSize, -currentSize, currentSize));
            lines[i * 12 + 5].SetPosition(1, list[i].center + new Vector3(-currentSize, currentSize, currentSize));
            lines[i * 12 + 6].SetPosition(0, list[i].center + new Vector3(currentSize, -currentSize, currentSize));
            lines[i * 12 + 6].SetPosition(1, list[i].center + new Vector3(currentSize, currentSize, currentSize));
            lines[i * 12 + 7].SetPosition(0, list[i].center + new Vector3(currentSize, -currentSize, -currentSize));
            lines[i * 12 + 7].SetPosition(1, list[i].center + new Vector3(currentSize, currentSize, -currentSize));

            // top
            lines[i * 12 + 8].SetPosition(0, list[i].center + new Vector3(-currentSize, currentSize, -currentSize));
            lines[i * 12 + 8].SetPosition(1, list[i].center + new Vector3(-currentSize, currentSize, currentSize));
            lines[i * 12 + 9].SetPosition(0, list[i].center + new Vector3(-currentSize, currentSize, currentSize));
            lines[i * 12 + 9].SetPosition(1, list[i].center + new Vector3(currentSize, currentSize, currentSize));
            lines[i * 12 + 10].SetPosition(0, list[i].center + new Vector3(currentSize, currentSize, currentSize));
            lines[i * 12 + 10].SetPosition(1, list[i].center + new Vector3(currentSize, currentSize, -currentSize));
            lines[i * 12 + 11].SetPosition(0, list[i].center + new Vector3(currentSize, currentSize, -currentSize));
            lines[i * 12 + 11].SetPosition(1, list[i].center + new Vector3(-currentSize, currentSize, -currentSize));
        }

        time = (Time.realtimeSinceStartup - lastInterval) * 1000;
        Debug.Log("line list time: " + time + " ms.");
#endif // AABB_TEST
    }

#if AABB_TEST
    int lastVisualizationDepth = -1;
    public int visualizedDepth = -1;
#endif // AABB_TEST
    static bool requestWasSend = false;

    void Update()
    {
        checkIsAsyncLoadFinished();

        if (isLookingForClosestPoint && !requestWasSend)
        {
            requestWasSend = true;
        }

        if (Camera.onPostRender != OnPostRenderCallback && pointClouds == null)
        {
            reInitialize();
        }
        else if (Camera.onPostRender != OnPostRenderCallback)
        {
            Camera.onPostRender = OnPostRenderCallback;
        }

#if AABB_TEST
        if (visualizedDepth != lastVisualizationDepth)
        {
            lastVisualizationDepth = visualizedDepth;
            for (int i = 0; i < octreeDepthViualizations.Count; i++)
            {
                for (int j = 0; j < octreeDepthViualizations[i].Count; j++)
                {
                    if (visualizedDepth == i)
                    {
                        octreeDepthViualizations[i][j].enabled = true;
                    }
                    else
                    {
                        octreeDepthViualizations[i][j].enabled = false;
                    }
                }
            }
        }
#endif // AABB_TEST

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            renderPointClouds = !renderPointClouds;
        }
    }

    static int screenIndex = 0;

    public static void OnPostRenderCallback(Camera cam)
    {
#if UNITY_EDITOR
        EditorSceneManager.sceneSaved -= OnSceneSaveCallback;
        EditorSceneManager.sceneSaved += OnSceneSaveCallback;
#endif

        if (cam == Camera.main && renderPointClouds)
        {
            screenIndex++;
            if (screenIndex > 1)
                screenIndex = 0;

            //setScreenIndex(screenIndex);
            Matrix4x4 cameraToWorld = cam.cameraToWorldMatrix;
            cameraToWorld = cameraToWorld.inverse;
            float[] cameraToWorldArray = new float[16];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    cameraToWorldArray[i * 4 + j] = cameraToWorld[i, j];
                }
            }
            GCHandle pointerTocameraToWorld = GCHandle.Alloc(cameraToWorldArray, GCHandleType.Pinned);

            cam.enabled = true;

            Matrix4x4 projection = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);

            float[] projectionArray = new float[16];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    projectionArray[i * 4 + j] = projection[i, j];
                }
            }
            GCHandle pointerProjection = GCHandle.Alloc(projectionArray, GCHandleType.Pinned);

            updateCamera(pointerTocameraToWorld.AddrOfPinnedObject(), pointerProjection.AddrOfPinnedObject(), screenIndex);

            pointerTocameraToWorld.Free();
            pointerProjection.Free();

#if FRUSTUMCULLING_TEST
            if (frustumCullingTestCamera != null)
            {
                Matrix4x4 cameraToWorld_ = frustumCullingTestCamera.cameraToWorldMatrix;
                cameraToWorld_ = cameraToWorld_.inverse;
                float[] cameraToWorldArray_ = new float[16];
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        cameraToWorldArray_[i * 4 + j] = cameraToWorld_[i, j];
                    }
                }
                GCHandle pointerTocameraToWorld_ = GCHandle.Alloc(cameraToWorldArray_, GCHandleType.Pinned);

                //Matrix4x4 projection = cam.nonJitteredProjectionMatrix;
                Matrix4x4 projection_ = GL.GetGPUProjectionMatrix(frustumCullingTestCamera.projectionMatrix, true);

                float[] projectionArray_ = new float[16];
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        projectionArray_[i * 4 + j] = projection_[i, j];
                    }
                }
                GCHandle pointerProjection_ = GCHandle.Alloc(projectionArray_, GCHandleType.Pinned);

                updateTestCamera(pointerTocameraToWorld_.AddrOfPinnedObject(), pointerProjection_.AddrOfPinnedObject());

                pointerTocameraToWorld_.Free();
                pointerProjection_.Free();
            }
#endif // FRUSTUMCULLING_TEST

            Matrix4x4 world;
            float[] worldArray = new float[16];
            GCHandle pointerWorld;

            pointCloud[] pointClouds_ = getPointCloudsInScene();
            for (int i = 0; i < pointClouds_.Length; i++)
            {
                world = pointClouds_[i].gameObject.transform.localToWorldMatrix;
                //Matrix4x4 worldALternative = pointCloudManager.pointClouds[i].inSceneRepresentation.transform.worldToLocalMatrix;

                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        worldArray[j * 4 + k] = world[j, k];
                    }
                }

                pointerWorld = GCHandle.Alloc(worldArray, GCHandleType.Pinned);
                IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointClouds_[i].ID);
                updateWorldMatrix(pointerWorld.AddrOfPinnedObject(), IDStrPtr);
				Marshal.FreeHGlobal(IDStrPtr);
                pointerWorld.Free();
            }

            //setScreenIndex(screenIndex);
            GL.IssuePluginEvent(GetRenderEventFunc(), screenIndex);
        }
    }

    public static void OnSceneSaveCallback(Scene scene)
    {
        pointCloud[] pointCloudsInScene = getPointCloudsInScene();
        for (int i = 0; i < pointCloudsInScene.Length; i++)
        {
            string extension = Path.GetExtension(pointCloudsInScene[i].pathToRawData);
            extension.ToLower();

            if (extension == ".laz" || extension == ".las")
            {
                SaveLAZFile(pointCloudsInScene[i].pathToRawData, pointCloudsInScene[i].ID);
            }
            else if (extension == ".cpc")
            {
                SaveOwnFormatFile(pointCloudsInScene[i].pathToRawData, pointCloudsInScene[i].ID);
            }
        }
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        EditorSceneManager.sceneSaved -= OnSceneSaveCallback;
#endif
        Camera.onPostRender -= OnPostRenderCallback;
    }

    public static void SetFrustumCulling(bool active)
    {
        setFrustumCulling(active);
    }

    public static void SetLODSystemActive(bool active)
    {
        setLODSystemActive(active);
    }

    //public static void SetLODTransitionDistance(float LODTransitionDistance)
    //{
    //    // float[] worldArray = new float[16];
    //    localLODTransitionDistance = LODTransitionDistance;

    //    GCHandle valuePointer = GCHandle.Alloc(LODTransitionDistance, GCHandleType.Pinned);
    //    setLODTransitionDistance(valuePointer.AddrOfPinnedObject());
    //    valuePointer.Free();
    //}

    public static void SetLODInfo(float maxDistance, float targetPercentOFPoints, int LODIndex, int pointCloudIndex)
    {
        float[] valuesArray = new float[2];
        valuesArray[0] = maxDistance;
        valuesArray[1] = targetPercentOFPoints;

        GCHandle valuePointer = GCHandle.Alloc(valuesArray, GCHandleType.Pinned);
        setLODInfo(valuePointer.AddrOfPinnedObject(), LODIndex, pointCloudIndex);
        valuePointer.Free();
    }

    public static LineRenderer lineToClosestPoint;
    public static Vector3 closestPointPosition;
    public static GameObject getPointGameObjectForSearch()
    {
        GameObject pointRepresentation = GameObject.Find("PointRepresentation_PointCloudPlugin");
        if (pointRepresentation == null /*&& EditorApplication.isPlaying*/)
        {
            pointRepresentation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointRepresentation.name = "PointRepresentation_PointCloudPlugin";
            pointRepresentation.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
        }

        closestPointPosition = new Vector3(10.0f, 10.0f, 10.0f);

        float[] initialPointPosition = new float[3];
        initialPointPosition[0] = pointRepresentation.transform.position.x;
        initialPointPosition[1] = pointRepresentation.transform.position.y;
        initialPointPosition[2] = pointRepresentation.transform.position.z;

        GCHandle initialPointPositionPointer = GCHandle.Alloc(initialPointPosition, GCHandleType.Pinned);
        RequestClosestPointToPointFromUnity(initialPointPositionPointer.AddrOfPinnedObject());

        float[] closestPointPositionFromDLL = new float[3];
        Marshal.Copy(initialPointPositionPointer.AddrOfPinnedObject(), closestPointPositionFromDLL, 0, 3);
        closestPointPosition.x = closestPointPositionFromDLL[0];
        closestPointPosition.y = closestPointPositionFromDLL[1];
        closestPointPosition.z = closestPointPositionFromDLL[2];

        initialPointPositionPointer.Free();

        if (lineToClosestPoint == null /*&& EditorApplication.isPlaying*/)
        {
            GameObject gObject = new GameObject("lineToClosestPoint_LineRenderer");
            lineToClosestPoint = gObject.AddComponent<LineRenderer>();
            lineToClosestPoint.material = new Material(Shader.Find("Sprites/Default"));
            lineToClosestPoint.startColor = Color.green;
            lineToClosestPoint.endColor = Color.green;

            lineToClosestPoint.widthMultiplier = 2.0f;
            lineToClosestPoint.positionCount = 2;
        }

        lineToClosestPoint.SetPosition(0, pointRepresentation.transform.position);
        lineToClosestPoint.SetPosition(1, closestPointPosition);

        return pointRepresentation;
    }

    public static GameObject getTestSphereGameObject()
    {
        GameObject testSphereGameObject = GameObject.Find("TestSphereGameObject_PointCloudPlugin");
        if (testSphereGameObject == null /*&& EditorApplication.isPlaying*/)
        {
            testSphereGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testSphereGameObject.name = "TestSphereGameObject_PointCloudPlugin";
            testSphereGameObject.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
        }

        GameObject pointRepresentation = GameObject.Find("PointRepresentation_PointCloudPlugin");
        if (pointRepresentation != null)
            testSphereGameObject.transform.position = pointRepresentation.transform.position;

        return testSphereGameObject;
    }

    public static bool testIsAtleastOnePointInSphere()
    {
        GameObject testSphereGameObject = getTestSphereGameObject();

        float[] center = new float[3];
        center[0] = testSphereGameObject.transform.position.x;
        center[1] = testSphereGameObject.transform.position.y;
        center[2] = testSphereGameObject.transform.position.z;

        GCHandle toDelete = GCHandle.Alloc(center.ToArray(), GCHandleType.Pinned);

        if (RequestIsAtleastOnePointInSphereFromUnity(toDelete.AddrOfPinnedObject(), testSphereGameObject.transform.localScale.x / 2.0f))
        {
            Debug.Log("Atleast one point found in sphere!");
            return true;
        }
        else
        {
            Debug.Log("No points found!");
            return false;
        }
    }

    public static GameObject getPointGameObjectForSearch_Fast()
    {
        GameObject pointRepresentation = GameObject.Find("PointRepresentation_PointCloudPlugin");
        if (pointRepresentation == null /*&& EditorApplication.isPlaying*/)
        {
            pointRepresentation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointRepresentation.name = "PointRepresentation_PointCloudPlugin";
            pointRepresentation.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
        }

        closestPointPosition = new Vector3(10.0f, 10.0f, 10.0f);

        float[] initialPointPosition = new float[3];
        initialPointPosition[0] = pointRepresentation.transform.position.x;
        initialPointPosition[1] = pointRepresentation.transform.position.y;
        initialPointPosition[2] = pointRepresentation.transform.position.z;

        GCHandle initialPointPositionPointer = GCHandle.Alloc(initialPointPosition, GCHandleType.Pinned);
        RequestClosestPointInSphereFromUnity(initialPointPositionPointer.AddrOfPinnedObject(), 0.0f);

        float[] closestPointPositionFromDLL = new float[3];
        Marshal.Copy(initialPointPositionPointer.AddrOfPinnedObject(), closestPointPositionFromDLL, 0, 3);
        closestPointPosition.x = closestPointPositionFromDLL[0];
        closestPointPosition.y = closestPointPositionFromDLL[1];
        closestPointPosition.z = closestPointPositionFromDLL[2];

        initialPointPositionPointer.Free();

        if (lineToClosestPoint == null /*&& EditorApplication.isPlaying*/)
        {
            GameObject gObject = new GameObject("lineToClosestPoint_LineRenderer");
            lineToClosestPoint = gObject.AddComponent<LineRenderer>();
            lineToClosestPoint.material = new Material(Shader.Find("Sprites/Default"));
            lineToClosestPoint.startColor = Color.green;
            lineToClosestPoint.endColor = Color.green;

            lineToClosestPoint.widthMultiplier = 2.0f;
            lineToClosestPoint.positionCount = 2;
        }

        lineToClosestPoint.SetPosition(0, pointRepresentation.transform.position);
        lineToClosestPoint.SetPosition(1, closestPointPosition);

        return pointRepresentation;

        //GameObject pointRepresentation = GameObject.Find("PointRepresentation_PointCloudPlugin_Fast");
        //if (pointRepresentation == null && EditorApplication.isPlaying)
        //{
        //    pointRepresentation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    pointRepresentation.name = "PointRepresentation_PointCloudPlugin_Fast";
        //    pointRepresentation.GetComponent<Renderer>().material.SetColor("_Color", Color.blue);
        //}

        //closestPointPosition = new Vector3(10.0f, 10.0f, 10.0f);

        //float[] centerPosition = new float[3];
        //centerPosition[0] = pointRepresentation.transform.position.x;
        //centerPosition[1] = pointRepresentation.transform.position.y;
        //centerPosition[2] = pointRepresentation.transform.position.z;

        //GCHandle centerPositionPointer = GCHandle.Alloc(centerPosition, GCHandleType.Pinned);
        //RequestClosestPointInSphereFromUnity(centerPositionPointer.AddrOfPinnedObject(), 0.0f);

        //float[] closestPointPositionFromDLL = new float[3];
        //Marshal.Copy(centerPositionPointer.AddrOfPinnedObject(), closestPointPositionFromDLL, 0, 3);
        //closestPointPosition.x = closestPointPositionFromDLL[0];
        //closestPointPosition.y = closestPointPositionFromDLL[1];
        //closestPointPosition.z = closestPointPositionFromDLL[2];

        //centerPositionPointer.Free();
        //pointRepresentation.transform.position = closestPointPosition;

        //if (lineToClosestPoint == null && EditorApplication.isPlaying)
        //{
        //    GameObject gObject = new GameObject("lineToClosestPoint_LineRenderer");
        //    lineToClosestPoint = gObject.AddComponent<LineRenderer>();
        //    lineToClosestPoint.material = new Material(Shader.Find("Sprites/Default"));
        //    lineToClosestPoint.startColor = Color.green;
        //    lineToClosestPoint.endColor = Color.green;

        //    lineToClosestPoint.widthMultiplier = 2.0f;
        //    lineToClosestPoint.positionCount = 2;
        //}

        //lineToClosestPoint.SetPosition(0, pointRepresentation.transform.position);
        //lineToClosestPoint.SetPosition(1, closestPointPosition);

        //return pointRepresentation;
    }

    //public static bool test_Closest_Point()
    //{
    //    float[] initialPointPosition = new float[3];
    //    GCHandle initialPointPositionPointer = GCHandle.Alloc(initialPointPosition, GCHandleType.Pinned);

    //    float lastInterval = 0.0f;
    //    float firstAlgTime = 0.0f;
    //    float secondAlgTime = 0.0f;

    //    for (int i = 0; i < 100; i++)
    //    {
    //        Vector3 randomPoint = new Vector3(UnityEngine.Random.value * 2000.0f - 1000.0f, UnityEngine.Random.value * 2000.0f - 1000.0f, UnityEngine.Random.value * 2000.0f - 1000.0f);

    //        initialPointPosition[0] = randomPoint.x;
    //        initialPointPosition[1] = randomPoint.y;
    //        initialPointPosition[2] = randomPoint.z;

    //        lastInterval = Time.realtimeSinceStartup;
    //        RequestClosestPointInSphereFromUnity(initialPointPositionPointer.AddrOfPinnedObject(), 0.0f);
    //        Debug.Log("delta time: " + (Time.realtimeSinceStartup - lastInterval) * 1000 + " ms.");
    //        firstAlgTime += (Time.realtimeSinceStartup - lastInterval) * 1000;
            
    //        float[] closestPointPositionFromDLL_Fast = new float[3];
    //        Marshal.Copy(initialPointPositionPointer.AddrOfPinnedObject(), closestPointPositionFromDLL_Fast, 0, 3);

    //        initialPointPosition[0] = randomPoint.x;
    //        initialPointPosition[1] = randomPoint.y;
    //        initialPointPosition[2] = randomPoint.z;

    //        lastInterval = Time.realtimeSinceStartup;
    //        RequestClosestPointToPointFromUnity(initialPointPositionPointer.AddrOfPinnedObject());
    //        secondAlgTime += (Time.realtimeSinceStartup - lastInterval) * 1000;

    //        float[] closestPointPositionFromDLL = new float[3];
    //        Marshal.Copy(initialPointPositionPointer.AddrOfPinnedObject(), closestPointPositionFromDLL, 0, 3);

    //        if (closestPointPositionFromDLL_Fast[0] != closestPointPositionFromDLL[0] ||
    //            closestPointPositionFromDLL_Fast[1] != closestPointPositionFromDLL[1] ||
    //            closestPointPositionFromDLL_Fast[2] != closestPointPositionFromDLL[2])
    //        {
    //            initialPointPositionPointer.Free();
    //            return false;
    //        }
    //    }

    //    Debug.Log("naive algorithm time: " + secondAlgTime + " ms.");
    //    Debug.Log("Octree with binary search area decrease time: " + firstAlgTime + " ms.");

    //    initialPointPositionPointer.Free();

    //    Debug.Log("Both algorithms produced same results!");

    //    return true;
    //}

    public static void setHighlightDeletedPoints(bool active)
    {
        setHighlightDeletedPointsActive(active);
    }

    public static void UpdateDeletionSpherePositionFromUnity(Vector3 center, float size)
    {
        float[] deletionSpherePosition = new float[3];
        
        GCHandle deletionSpherePositionPointer = GCHandle.Alloc(deletionSpherePosition, GCHandleType.Pinned);
        deletionSpherePosition[0] = center.x;
        deletionSpherePosition[1] = center.y;
        deletionSpherePosition[2] = center.z;

        UpdateDeletionSpherePositionFromUnity(deletionSpherePositionPointer.AddrOfPinnedObject(), size);
    }

    public static void SetTestLevel(float value)
    {
        setTestLevel(value);
    }

    public static void requestUndo(int actionsCount)
    {
        undo(actionsCount);
    }

    public static void HighlightOutliers(float discardDistance, int minNeighborsInRange, string pointCloudID)
    {
		IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointCloudID);
        highlightOutliers(discardDistance, minNeighborsInRange, IDStrPtr);
		Marshal.FreeHGlobal(IDStrPtr);
    }

    public static void DeleteOutliers(string pointCloudID)
    {
		IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointCloudID);
        deleteOutliers(IDStrPtr);
		Marshal.FreeHGlobal(IDStrPtr);
    }
    private static bool firstFrame = true;
    public static void OnSceneStart()
    {
        if (firstFrame)
        {
            firstFrame = false;
            Camera.onPostRender -= pointCloudManager.OnPostRenderCallback;
            Camera.onPostRender += pointCloudManager.OnPostRenderCallback;
            OnSceneStart();
        }
        OnSceneStartFromUnity(Marshal.StringToHGlobalAnsi(Application.dataPath));
    }

    public static String GetNewID()
    {
		IntPtr arrayToFill = Marshal.AllocHGlobal(24 * 8);
        getNewUniqueID(arrayToFill);

        int[] tempArray = new int[24];
        Marshal.Copy(arrayToFill, tempArray, 0, 24);
        Marshal.FreeHGlobal(arrayToFill);

        String result = "";
        for (int i = 0; i < 24; i++)
        {
            result += (char)tempArray[i];
        } 

        return result;
    }
}
