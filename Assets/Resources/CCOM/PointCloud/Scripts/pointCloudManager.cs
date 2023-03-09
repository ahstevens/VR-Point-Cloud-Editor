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

public class PointCloudManager : MonoBehaviour
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
    //[DllImport("PointCloudPlugin")]
    //static public extern void setHighlightDeletedPointsActive(bool active);
    //[DllImport("PointCloudPlugin")]
    //static public extern void UpdateDeletionSpherePositionFromUnity(IntPtr center, float size);

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

    [DllImport("PointCloudPlugin")]
    private static extern bool IsDebugLogFileOutActive();

    [DllImport("PointCloudPlugin")]
    private static extern void SetDebugLogFileOutput(bool NewValue);



    public static List<PointCloud> pointClouds;
    public static List<string> toLoadList;
    public static List<LODInformation> LODSettings;

    public static bool isWaitingToLoad = false;
    public static string filePathForAsyncLoad = "";
    public static float localLODTransitionDistance = 3500.0f;
    public static bool highlightDeletedPoints = false;

    private static bool renderPointClouds = true;

    private static Material deletedPointsBox;

    private static string demoFile;

    private static bool _commandLineMode = false;
    public static bool commandLineMode
    {
        get { return _commandLineMode; }
    }

    private static string _commandLineInputFile;
    public static string commandLineInputFile
    {
        get { return _commandLineInputFile; }
    }

    private static string _commandLineOutputFile;
    public static string commandLineOutputFile
    {
        get { return _commandLineOutputFile; }
    }

    private static PointCloudManager _instance;

    public static PointCloudManager instance
    {
        get { return _instance; }
    }

    [SerializeField]
    private GameObject backingPrefab;

    [SerializeField]
    private int debugEPSG = 0;

    private static int forcedEPSG = 0;

    private static bool firstFrame = true;

    static int screenIndex = 0;

    private void Awake()
    {
        _instance = this;

        SetDebugLogFileOutput(false);

        // find command line input file, if supplied
        string[] arguments = Environment.GetCommandLineArgs();

        _commandLineInputFile = "";
        _commandLineOutputFile = "";

        if (debugEPSG > 0)
            forcedEPSG = debugEPSG;

        for (int i = 1; i < arguments.Length; ++i)
        {
            if (arguments[i] == "-i" && i != arguments.Length - 1)
            {
                _commandLineInputFile = arguments[i + 1];
                _commandLineMode = true;
            }

            if (arguments[i] == "-o" && i != arguments.Length - 1)
            {
                _commandLineOutputFile = arguments[i + 1];
            }

            if (arguments[i] == "-debug")
            {
                SetDebugLogFileOutput(true);
            }

            if (arguments[i].ToLower() == "-epsg" && i != arguments.Length - 1)
            {
                try
                {
                    forcedEPSG = int.Parse(arguments[i + 1]);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

    void OnValidate()
    {
        Camera.onPostRender = OnPostRenderCallback;

#if UNITY_EDITOR
        EditorSceneManager.sceneSaved -= OnSceneSaveCallback;
        EditorSceneManager.sceneSaved += OnSceneSaveCallback;

        EditorApplication.playModeStateChanged -= ClearPointCloudsOnPlayModeExit;
        EditorApplication.playModeStateChanged += ClearPointCloudsOnPlayModeExit;
#endif
        OnSceneStartFromUnity(Marshal.StringToHGlobalAnsi(Application.dataPath));

        PointCloud[] pointClouds = (PointCloud[])GameObject.FindObjectsOfType(typeof(PointCloud));
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

    void Start()
    {
        demoFile = Application.dataPath + "/../sample";

        deletedPointsBox = new Material(Shader.Find("Unlit/Color"))
        {
            color = Camera.main.backgroundColor
        };

        OnValidate();

        if (_commandLineInputFile != "")
        {
            if (LoadLAZFile(_commandLineInputFile))
            {                
                Debug.Log("Successfully loaded " + _commandLineInputFile);
            }
            else
            {
                Debug.Log("Error loading " + _commandLineInputFile);                
            }
        }
    }

    void Update()
    {
        deletedPointsBox.color = Camera.main.backgroundColor;

        CheckIsAsyncLoadFinished();

        if (Camera.onPostRender != OnPostRenderCallback && pointClouds == null)
        {
            Reinitialize();
        }
        else if (Camera.onPostRender != OnPostRenderCallback)
        {
            Camera.onPostRender = OnPostRenderCallback;
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            renderPointClouds = !renderPointClouds;
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            LoadDemoFile();
        }
    }

    static private GEOReference GetGeoReference()
    {
        GameObject geoReference = GameObject.Find("UnityZeroGeoReference");
        if (geoReference == null)
            return null;

        return geoReference.GetComponent<GEOReference>();
    }

    private static void CreateGEOReference(double xOrigin, double yOrigin)
    {
        GameObject geoReference = new GameObject("UnityZeroGeoReference");
        geoReference.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

        // Add script
        GEOReference scriptClass = geoReference.AddComponent<GEOReference>();

        scriptClass.setReferenceX(xOrigin);
        scriptClass.setReferenceY(yOrigin);
    }

    public static bool LoadLAZFile(string filePath, GameObject reinitialization = null)
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
            pointClouds = new();

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
        IntPtr IDStrPtr = Marshal.StringToHGlobalAnsi(pointCloudID);
        bool result = unLoad(IDStrPtr);
        Marshal.FreeHGlobal(IDStrPtr);

        if (result)
        {
            PointCloud[] pointClouds = (PointCloud[])GameObject.FindObjectsOfType(typeof(PointCloud));
            for (int i = 0; i < pointClouds.Length; i++)
            {
                if (pointClouds[i].ID == pointCloudID)
                {
                    DestroyImmediate(pointClouds[i].gameObject);
                    break;
                }
            }

            pointClouds = (PointCloud[])GameObject.FindObjectsOfType(typeof(PointCloud));
            if (pointClouds.Length == 0)
            {
                FindObjectOfType<ModifyPoints>().ActivateClassificationMode(false);

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

    public static PointCloud[] GetPointCloudsInScene()
    {
        PointCloud[] pointClouds = (PointCloud[])FindObjectsOfType(typeof(PointCloud));
        return pointClouds;
    }

    public void Reinitialize()
    {
        Camera.onPostRender += OnPostRenderCallback;

        pointClouds = new();
        LODSettings = new();
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

    public static void CheckIsAsyncLoadFinished()
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
            GameObject pointCloudGameObject = new(name);

            // Add script to a point cloud game object.
            var pcComponent = pointCloudGameObject.AddComponent<PointCloud>();            
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

            for (int i = 0; i < adjustmentResult.Length; ++i)
                Debug.Log(i + ": " + adjustmentResult[i]);

            pcComponent.adjustmentX = adjustmentResult[0];
            pcComponent.adjustmentY = adjustmentResult[1];
            pcComponent.adjustmentZ = adjustmentResult[2];

            if (GetGeoReference() == null)
            {
                CreateGEOReference(adjustmentResult[3], adjustmentResult[4]);
            }
            else
            {
                pcComponent.initialXShift = -(GetGeoReference().refX - adjustmentResult[3]);
                pcComponent.initialZShift = -(GetGeoReference().refY - adjustmentResult[4]);
            }

            for (int i = 0; i != adjustmentResult.Length; ++i)
            {
                Debug.Log(i + ": " + adjustmentResult[i]);
            }    

            pcComponent.bounds = new Bounds();

            pcComponent.bounds.SetMinMax(
                new Vector3(
                    (float)(adjustmentResult[5] - adjustmentResult[3]),
                    (float)(adjustmentResult[6]), 
                    (float)(adjustmentResult[7] - adjustmentResult[4])),
                new Vector3(
                    (float)(adjustmentResult[8] - adjustmentResult[3]) ,
                    (float)(adjustmentResult[9]) ,
                    (float)(adjustmentResult[10] - adjustmentResult[4]))
                );

            Debug.Log(pcComponent.bounds);

            Debug.Log((int)(adjustmentResult[11]));

            pcComponent.EPSG = forcedEPSG > 0 ? forcedEPSG : (int)(adjustmentResult[11]);

            pcComponent.groundLevel = (float)adjustmentResult[12];

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
                        UserSettings.instance.preferences.fitSizeOnLoad,
                        UserSettings.instance.preferences.distanceOnLoad
                    );

                    if (UserSettings.instance.preferences.enableMaps)
                        FindObjectOfType<MapManager>().CreateMaps(GetGeoReference(), pcComponent);                    
                }
            }

            pointCloudGameObject.transform.localPosition = new Vector3((float)(pcComponent.initialXShift),
                                                                  y,
                                                                  (float)(pcComponent.initialZShift));

            pointCloudGameObject.transform.localRotation = Quaternion.identity;
            pointCloudGameObject.transform.localScale = Vector3.one;

            AddSecretBoxForDeletedPoints(pointCloudGameObject);
            CreateBacking(ref pointCloudGameObject);

            if (!FindObjectOfType<PointCloudUI>().MenuOpen)
                FindObjectOfType<ModifyPoints>().SetBrushVisibility(true);

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

#if UNITY_EDITOR
    private static void ClearPointCloudsOnPlayModeExit(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            foreach (var pc in GetPointCloudsInScene())
            {
                UnLoad(pc.ID);
            }
        }
    }
#endif

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

            if (!FindObjectOfType<PointCloudUI>().MenuOpen)
                FindObjectOfType<ModifyPoints>().ModifyInSphere();
            
            updateCamera(pointerTocameraToWorld.AddrOfPinnedObject(), pointerProjection.AddrOfPinnedObject(), screenIndex);

            pointerTocameraToWorld.Free();
            pointerProjection.Free();

            Matrix4x4 world;
            float[] worldArray = new float[16];
            GCHandle pointerWorld;

            PointCloud[] pointClouds_ = GetPointCloudsInScene();
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
        PointCloud[] pointCloudsInScene = GetPointCloudsInScene();
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
    public static void OnSceneStart()
    {
        if (firstFrame)
        {
            firstFrame = false;
            Camera.onPostRender -= OnPostRenderCallback;
            Camera.onPostRender += OnPostRenderCallback;
            OnSceneStart();
        }
        OnSceneStartFromUnity(Marshal.StringToHGlobalAnsi(Application.dataPath));
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        EditorSceneManager.sceneSaved -= OnSceneSaveCallback;
        EditorUtility.ClearProgressBar();
#endif
        Camera.onPostRender -= OnPostRenderCallback;
    }

    //public static void SetHighlightDeletedPoints(bool active)
    //{
    //    setHighlightDeletedPointsActive(active);
    //}

    //public static void UpdateDeletionSpherePositionFromUnity(Vector3 center, float size)
    //{
    //    float[] deletionSpherePosition = new float[3];
    //    
    //    GCHandle deletionSpherePositionPointer = GCHandle.Alloc(deletionSpherePosition, GCHandleType.Pinned);
    //    deletionSpherePosition[0] = center.x;
    //    deletionSpherePosition[1] = center.y;
    //    deletionSpherePosition[2] = center.z;
    //
    //    UpdateDeletionSpherePositionFromUnity(deletionSpherePositionPointer.AddrOfPinnedObject(), size);
    //}

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
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);

        box.name = "Deleted Points Obscurer";
        box.transform.parent = pointCloud.transform;
        box.transform.rotation = Quaternion.identity;
        box.transform.localScale = Vector3.one * 1000;
        box.transform.localPosition = Vector3.one * -200000.0f;

        var br = box.GetComponent<Renderer>();
        br.material = deletedPointsBox;
    }

    public static void CreateBacking(ref GameObject pointCloud)
    {
        GameObject backing = Instantiate(_instance.backingPrefab, pointCloud.transform);

        backing.name = pointCloud.name + " Backing";
        backing.transform.localRotation = Quaternion.identity;
        backing.transform.localPosition = Vector3.zero;
        backing.transform.localScale = Vector3.one;

        var b = pointCloud.GetComponent<PointCloud>().bounds;

        backing.transform.localPosition = b.center;

        float squareBoundSize = MathF.Max(b.size.x, b.size.z);
        backing.transform.localScale = new Vector3(squareBoundSize, b.size.y, squareBoundSize);
    }

    public static bool LoadDemoFile()
    {
        bool demoLas = File.Exists(demoFile + ".las");
        bool demoLaz = File.Exists(demoFile + ".laz");

        if (demoLas || demoLaz)
        {
            var pcs = GetPointCloudsInScene();

            if (pcs != null)
                for (int i = 0; i < pcs.Length; ++i)
                    UnLoad(pcs[i].ID);

            return LoadLAZFile(demoFile + ".la" + (demoLas ? "s" : "z"));
        }
        else
        {
            Debug.Log("Demo file " + demoFile + " not found!");
            return false;
        }
    }
}
