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
    
    private static extern int RequestToDeleteFromUnity(IntPtr center, float size);
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
    static public extern bool IsLastAsyncSaveFinished();
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
    public static bool highlightDeletedPoints = false;

    private static bool renderPointClouds = true;

    private static Material deletedPointsBox;

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
        EditorUtility.DisplayProgressBar("Point Cloud Plugin", "Loading Point Cloud...", 0f);
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
                    DestroyImmediate(pointClouds[i].gameObject);
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
                    DestroyImmediate(geoReference);
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

            pcComponent.bounds = new Bounds();

            pcComponent.bounds.SetMinMax(
                new Vector3(
                    (float)adjustmentResult[5],
                    (float)adjustmentResult[6], 
                    (float)adjustmentResult[7]),
                new Vector3(
                    (float)adjustmentResult[8],
                    (float)adjustmentResult[9],
                    (float)adjustmentResult[10])
                );

            Debug.Log(pcComponent.bounds);

            pcComponent.EPSG = (int)(adjustmentResult[11]);

            pcComponent.groundLevel = (float)adjustmentResult[12];

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
                    pcComponent.ResetMiniature(
                        UserSettings.instance.GetPreferences().fitSizeOnLoad,
                        UserSettings.instance.GetPreferences().distanceOnLoad
                    );

                    FindObjectOfType<MapManager>().CreateMaps(getReferenceScript(), pcComponent);                    
                }
            }

            pointCloudGameObject.transform.localPosition = new Vector3((float)(pcComponent.initialXShift),
                                                                  y,
                                                                  (float)(pcComponent.initialZShift));

            pointCloudGameObject.transform.localRotation = Quaternion.identity;
            pointCloudGameObject.transform.localScale = Vector3.one;

            AddSecretBoxForDeletedPoints(pointCloudGameObject);

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
            foreach (var pc in getPointCloudsInScene())
            {
                UnLoad(pc.ID);
            }
        }
    }
#endif

    void Start()
    {
        deletedPointsBox = new Material(Shader.Find("Unlit/Color"));
        deletedPointsBox.color = Camera.main.backgroundColor;

        OnValidate();
    }

    void Update()
    {
        deletedPointsBox.color = Camera.main.backgroundColor;

        checkIsAsyncLoadFinished();

        if (Camera.onPostRender != OnPostRenderCallback && pointClouds == null)
        {
            reInitialize();
        }
        else if (Camera.onPostRender != OnPostRenderCallback)
        {
            Camera.onPostRender = OnPostRenderCallback;
        }

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
    }

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

    public static void AddSecretBoxForDeletedPoints(GameObject pointCloud)
    {
        var pcComponent = pointCloud.GetComponent<pointCloud>();

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.name = "Deleted Points Obscurer";
        box.transform.parent = pointCloud.transform;
        box.transform.rotation = Quaternion.identity;
        box.transform.localScale = Vector3.one * 1000;
        box.transform.localPosition = Vector3.one * -200000.0f;

        var br = box.GetComponent<Renderer>();
        br.material = deletedPointsBox;
    }
}
