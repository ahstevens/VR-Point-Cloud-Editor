//#define BENCHMARK
//#define ALTERNATIVE_LOAD
//#define ADVANCED_SETTINGS
//#define POINT_IN_SPHERE_TEST

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class pointCloudManagerWindow : EditorWindow
{
#if ADVANCED_SETTINGS
    private bool frustumCulling = false;
    private bool LODSystemActive = false;
#endif // ADVANCED_SETTINGS

#if BENCHMARK
    private float FPS;
    private float escapedTime;
    private List<float> FPSvalues;
    private bool benchmarkActive = false;

    private bool playModeWasInitByWindow = false;
    private string PrevScene;
    private bool benchmarkFinishedLoadPrevScene = false;
#endif // BENCHMARK

#if ALTERNATIVE_LOAD
    private DirectoryInfo currentDirectory = null;
    private List<FileInfo> fileList = new List<FileInfo>();
#endif // ALTERNATIVE_LOAD

#if POINT_IN_SPHERE_TEST
    private bool downScale = true;
    private float sizeDifference = 0.0f;
    private float workingRange = 0.0f;
    private bool pointInRange = false;
    private bool pointInRangeLastStep = false;
    private float lastScaleWithPoints = 0.0f;
#endif // POINT_IN_SPHERE_TEST

    private static bool outliersSearch = false;
    private static int tempMinNeigboars = 5;
    private static float tempNeigboarsDistance = 5.0f;

    [DllImport("PointCloudPlugin")]
    private static extern void updateCamera(IntPtr worldMatrix, IntPtr projectionMatrix);
    [DllImport("PointCloudPlugin")]
    static public extern bool updateWorldMatrix(IntPtr worldMatrix, IntPtr pointCloudID);
    [DllImport("PointCloudPlugin")]
    private static extern IntPtr GetRenderEventFunc();
 
    [MenuItem("Hydrographic Toolkit/Point Cloud Manager")]
    public static void ShowWindow()
    {
        GetWindow(typeof(pointCloudManagerWindow));
    }

    pointCloud[] getPointCloudsInScene()
    {
        pointCloud[] pointClouds = (pointCloud[])GameObject.FindObjectsOfType(typeof(pointCloud));
        return pointClouds;
    }

    private string OpenLAZFileDialog()
    {
        string currentFile = EditorUtility.OpenFilePanel("Choose file", "", "laz,las,cpc");
        if (currentFile == "")
            return currentFile;
        pointCloudManager.loadLAZFile(currentFile);

        return currentFile;
    }

    private void SaveLAZFileDialog(string pointCloudID, string defaultName)
    {
        string saveToFile = EditorUtility.SaveFilePanel("Save to", "", defaultName, "laz");
        if (saveToFile == "")
            return;

        pointCloudManager.SaveLAZFile(saveToFile, pointCloudID);
    }

    private void SaveOwnFormatFileDialog(string pointCloudID, string defaultName)
    {
        string saveToFile = EditorUtility.SaveFilePanel("Save to", "", defaultName, "cpc");
        if (saveToFile == "")
            return;

        pointCloudManager.SaveOwnFormatFile(saveToFile, pointCloudID);
    }

    private GameObject getPointCloudManagerGameObject()
    {
        GameObject pointCloudManager = GameObject.Find("pointCloudManager");
        if (pointCloudManager == null)
            return null;

        return pointCloudManager;
    }

    private void createPointCloudManagerGameObject()
    {
        GameObject pointCloudManager = new GameObject("pointCloudManager");

        MonoScript script = Resources.Load<MonoScript>("CCOM/PointCloud/Scripts/pointCloudManager");
        ScriptableObject myCommonClass = ScriptableObject.CreateInstance(script.GetClass()) as ScriptableObject;
        pointCloudManager.AddComponent(myCommonClass.GetType());
    }

    bool firstFrame = true;

    void Update()
    {
        if (firstFrame)
        {
            firstFrame = false;
            pointCloudManager.OnSceneStart();

            Camera.onPostRender -= pointCloudManager.OnPostRenderCallback;
            Camera.onPostRender += pointCloudManager.OnPostRenderCallback;

            EditorSceneManager.sceneSaved -= pointCloudManager.OnSceneSaveCallback;
            EditorSceneManager.sceneSaved += pointCloudManager.OnSceneSaveCallback;
#if ALTERNATIVE_LOAD
        updateFileList();
#endif // ALTERNATIVE_LOAD

#if BENCHMARK
        benchmarkActive = false;
#endif // BENCHMARK
        }

        pointCloudManager.checkIsAsyncLoadFinished();

        if (getPointCloudManagerGameObject() == null)
            createPointCloudManagerGameObject();

#if BENCHMARK
        if (!benchmarkActive && benchmarkFinishedLoadPrevScene && !EditorApplication.isPlaying)
        {
            benchmarkFinishedLoadPrevScene = false;
            EditorSceneManager.OpenScene(PrevScene/*"Assets/Scenes/BenchmarkScene0.unity"*/);
        }

        if (benchmarkActive && !EditorApplication.isPlaying && !playModeWasInitByWindow)
        {
            escapedTime = 0;
            benchmarkActive = false;
        }

        if (benchmarkActive && !pointCloudManager.isWaitingToLoad && pointCloudManager.isReInitializationObjectsAsyncEmpty())
            escapedTime += Time.unscaledDeltaTime;
#endif // BENCHMARK
    }

#if BENCHMARK
    static bool loadStarted = false;
#endif // BENCHMARK


    void OnInspectorUpdate()
    {
#if BENCHMARK
        if (pointCloudManager.isWaitingToLoad)
            loadStarted = true;

        if (!benchmarkActive || pointCloudManager.isWaitingToLoad || !pointCloudManager.isReInitializationObjectsAsyncEmpty())
            return;

        if (!Camera.main.GetComponent<Animator>().enabled && loadStarted)
        {
            loadStarted = false;
            Camera.main.GetComponent<Animator>().enabled = true;
            Camera.main.GetComponent<Animator>().Play("BenchmarkAnimation", -1, 0f);
            Camera.main.GetComponent<Animator>().SetBool("BenchmarkAnimation", true);
        }

        if (benchmarkActive && Camera.main.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && !Camera.main.GetComponent<Animator>().IsInTransition(0))
        {
            escapedTime = 0;
            benchmarkActive = false;
            Camera.main.GetComponent<Animator>().enabled = false;
            playModeWasInitByWindow = false;

            float totalFPS = 0.0f;
            for (int i = 0; i < FPSvalues.Count; i++)
            {
                totalFPS += FPSvalues[i];
            }

            Debug.Log("avarage fps: " + totalFPS / FPSvalues.Count);

            EditorApplication.isPlaying = false;
            benchmarkFinishedLoadPrevScene = true;
            //SceneManager.LoadScene("Assets/Scenes/BenchmarkScene0.unity", LoadSceneMode.Single);
            //EditorSceneManager.OpenScene("Assets/Scenes/BenchmarkScene0.unity");
        }

        FPS = 1.0f / Time.unscaledDeltaTime;
        FPSvalues.Add(FPS);
        if (FPSvalues.Count > 10000)
            FPSvalues.Clear();
#endif // BENCHMARK
        Repaint();
    }

    static int indexSelected;

    void OnGUI()
    {
#if BENCHMARK
        GUILayout.Label("FPS: " + FPS);
        GUILayout.Label("Escaped time: " + escapedTime);

        if (EditorApplication.isPlaying)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Lunch benchmark"))
        {
            if (FPSvalues == null)
                FPSvalues = new List<float>();

            FPSvalues.Clear();
            escapedTime = 0;

            PrevScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/Scenes/BenchmarkScene1.unity");

            //Camera.main.GetComponent<Animator>().enabled = true;
            //Camera.main.GetComponent<Animator>().Play("BenchmarkAnimation", -1, 0f);
            //Camera.main.GetComponent<Animator>().SetBool("BenchmarkAnimation", true);

            // Only in play mode
            //SceneManager.LoadScene("BenchmarkScene0", LoadSceneMode.Additive);

            benchmarkActive = true;
            if (EditorApplication.isPlaying == false)
            {
                playModeWasInitByWindow = true;
                EditorApplication.isPlaying = true;
            }

        }
        GUI.enabled = true;
#endif // BENCHMARK

        if (pointCloudManager.isWaitingToLoad)
            GUI.enabled = false;

        if (GUILayout.Button("Import LAZ file"))
            OpenLAZFileDialog();

        GUI.enabled = true;

        pointCloud[] pointClouds = (pointCloud[])GameObject.FindObjectsOfType(typeof(pointCloud));
        if (pointClouds.Length > 0)
        {
            string[] availableIndexes = new string[pointClouds.Length];

            if (indexSelected >= 0 && indexSelected < pointClouds.Length && pointClouds[indexSelected].UTMZone == 0 && !pointClouds[indexSelected].North)
            {
                GUILayout.Label("UTMZone : Information about UTMZone was not found");
            }
            else
            {
                GUILayout.Label("UTMZone : " + pointClouds[indexSelected].UTMZone + (pointClouds[indexSelected].North ? "N" : "S"));
            }

            for (int i = 0; i < pointClouds.Length; i++)
            {
                availableIndexes[i] = pointClouds[i].name;
            }

            if (pointClouds.Length != 0)
            {
                GUILayout.Space(16);

                indexSelected = EditorGUILayout.Popup("Choose file: ", indexSelected, availableIndexes);

                GUILayout.Label("Saving: ");

                if (GUILayout.Button("Export to LAZ file"))
                    SaveLAZFileDialog(pointClouds[indexSelected].ID, availableIndexes[indexSelected]);

                if (GUILayout.Button("Export to internal (.cpc) file format"))
                    SaveOwnFormatFileDialog(pointClouds[indexSelected].ID, availableIndexes[indexSelected]);

                if (GUILayout.Button("Unload File"))
                    if (pointCloudManager.UnLoad(pointClouds[indexSelected].ID))
                        indexSelected = 0;
            }

#if ADVANCED_SETTINGS
            frustumCulling = GUILayout.Toggle(frustumCulling, "use frustum culling");
            pointCloudManager.SetFrustumCulling(frustumCulling);

            LODSystemActive = GUILayout.Toggle(LODSystemActive, "use LOD");
            pointCloudManager.SetLODSystemActive(LODSystemActive);

            if (LODSystemActive)
            {
                for (int j = 0; j < 4; j++)
                {
                    float tempMaxDistance = EditorGUILayout.FloatField("LODs[" + j + "].maxDistance: ", pointCloudManager.LODSettings[j].maxDistance);
                    float tempTargetPercentOFPoints = EditorGUILayout.FloatField("LODs[" + j + "].targetPercentOFPoints: ", pointCloudManager.LODSettings[j].targetPercentOFPoints);

                    if (tempMaxDistance != pointCloudManager.LODSettings[j].maxDistance ||
                        tempTargetPercentOFPoints != pointCloudManager.LODSettings[j].targetPercentOFPoints)
                    {
                        pointCloudManager.SetLODInfo(tempMaxDistance, tempTargetPercentOFPoints, j, 0);
                        pointCloudManager.LODSettings[j].maxDistance = tempMaxDistance;
                        pointCloudManager.LODSettings[j].targetPercentOFPoints = tempTargetPercentOFPoints;
                    }
                }
            }
#endif // ADVANCED_SETTINGS
        }

#if ALTERNATIVE_LOAD
        GUILayout.Label("Alternative load : ");
        if (currentDirectory == null)
            currentDirectory = new DirectoryInfo(Application.dataPath + "//PointClouds");

        if (!EditorApplication.isPlaying || pointCloudManager.isWaitingToLoad)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Go to parent directory"))
            currentDirectory = currentDirectory.Parent;
        GUILayout.Label("Current directory: " + currentDirectory.FullName);

        foreach (FileInfo file in fileList)
        {
            //GUILayout.Button(file.Name);
            if (GUILayout.Button(file.Name))
                pointCloudManager.loadLAZFile(currentDirectory.FullName + "//" +file.Name);
        }

        GUI.enabled = true;

#endif // ALTERNATIVE_LOAD

#if POINT_IN_SPHERE_TEST
        if (GUILayout.Button("Test isAtleastOnePointInSphere"))
            pointCloudManager.testIsAtleastOnePointInSphere();

        if (GUILayout.Button("Test search sphere size"))
        {
            float minDistance = float.MaxValue;
            int pointCloudIndex = -1;
            for (int i = 0; i < pointCloudManager.pointClouds.Count; i++)
            {
                float currentDistance = Vector3.Distance(pointCloudManager.getTestSphereGameObject().transform.position, pointCloudManager.pointClouds[i].inSceneRepresentation.transform.position);
                if (currentDistance < minDistance)
                {
                    minDistance = currentDistance;
                    pointCloudIndex = i;
                }
            }

            // Multiplying by 4 to decrease chance that we will not "caught" any points.
            workingRange = minDistance * 4.0f;
            pointCloudManager.getTestSphereGameObject().transform.localScale = new Vector3(workingRange, workingRange, workingRange);
            downScale = true;
            sizeDifference = workingRange / 2.0f;
            pointInRange = pointCloudManager.testIsAtleastOnePointInSphere();
            if (pointInRange)
                lastScaleWithPoints = workingRange;

            pointInRangeLastStep = pointInRange;

            for (int i = 0; i < 20; i++)
            {
                workingRange += downScale ? -sizeDifference : sizeDifference;
                pointCloudManager.getTestSphereGameObject().transform.localScale = new Vector3(workingRange, workingRange, workingRange);
                pointInRange = pointCloudManager.testIsAtleastOnePointInSphere();
                if (pointInRange)
                    lastScaleWithPoints = workingRange;

                if (pointInRange != pointInRangeLastStep)
                {
                    pointInRangeLastStep = pointInRange;
                    downScale = !downScale;
                    sizeDifference = sizeDifference / 2.0f;
                }
            }

            //Debug.Log("lastScaleWithPoints: " + lastScaleWithPoints / 2.0f);
            //Debug.Log("closestPointDistance: " + Vector3.Distance(pointCloudManager.lineToClosestPoint.GetPosition(0), pointCloudManager.lineToClosestPoint.GetPosition(1)));

            //Debug.Log("pointCloudManager.getTestSphereGameObject().transform.position: " + pointCloudManager.getTestSphereGameObject().transform.position);

            //GameObject closestPoint_Fast = pointCloudManager.getPointGameObjectForSearch_Fast(pointCloudManager.getTestSphereGameObject().transform.position, 0.0f/*lastScaleWithPoints / 2.0f*/);
            //Debug.Log("closestPointDistance_Fast: " + Vector3.Distance(pointCloudManager.lineToClosestPoint.GetPosition(0), closestPoint_Fast.transform.position));
            //Debug.Log("closestPoint_Fast: " + closestPoint_Fast.transform.position);
        }

        pointCloudManager.isLookingForClosestPoint = GUILayout.Toggle(pointCloudManager.isLookingForClosestPoint, "isLookingForClosestPoint");
        if (!pointCloudManager.isLookingForClosestPoint)
        {
            var tempGameObject = GameObject.Find("PointRepresentation_PointCloudPlugin");
            if (tempGameObject != null)
                GameObject.Destroy(tempGameObject);

            tempGameObject = GameObject.Find("lineToClosestPoint_LineRenderer");
            if (tempGameObject != null)
                GameObject.Destroy(tempGameObject);
        }
#endif // POINT_IN_SPHERE_TEST

        if (pointClouds.Length > 0)
        {
#if ADVANCED_SETTINGS
            pointCloudManager.highlightDeletedPoints = GUILayout.Toggle(pointCloudManager.highlightDeletedPoints, "Highlight points to delete");
            pointCloudManager.setHighlightDeletedPoints(pointCloudManager.highlightDeletedPoints);
#endif // ADVANCED_SETTINGS

            GUILayout.Space(16);
            GUILayout.Label("Outliers: ");

            if (GUILayout.Button("Show outliers"))
            {
                pointCloudManager.HighlightOutliers(tempNeigboarsDistance, tempMinNeigboars, pointClouds[indexSelected].ID);
            }

            if (GUILayout.Button("Delete shown outliers"))
            {
                pointCloudManager.DeleteOutliers(pointClouds[indexSelected].ID);
#if UNITY_EDITOR
                if (!EditorApplication.isPlaying)
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
            }

            outliersSearch = GUILayout.Toggle(outliersSearch, "Advanced outliers settings");
            if (outliersSearch)
            {
                tempMinNeigboars = EditorGUILayout.IntField("Min Neighbor Count: ", tempMinNeigboars);
                if (tempMinNeigboars < 1)
                    tempMinNeigboars = 1;

                tempNeigboarsDistance = EditorGUILayout.FloatField("Max Neighbor Distance: ", tempNeigboarsDistance);
                if (tempNeigboarsDistance < 0.1f)
                    tempNeigboarsDistance = 0.1f;
            }

            //if (GUILayout.Button("Test closest point algorithms"))
            //{
            //    pointCloudManager.test_Closest_Point();
            //}
        }

#if ALTERNATIVE_LOAD
        timePassed += Time.unscaledDeltaTime;
        if (timePassed > 2.0f)
        {
            timePassed = 0.0f;
            updateFileList();
        }
#endif // ALTERNATIVE_LOAD
    }

#if ALTERNATIVE_LOAD
    static float timePassed = 0.0f;
    void updateFileList()
    {
        fileList.Clear();
        fileList.AddRange(new List<FileInfo>(currentDirectory.GetFiles("*.las", SearchOption.AllDirectories)));
        fileList.AddRange(new List<FileInfo>(currentDirectory.GetFiles("*.laz", SearchOption.AllDirectories)));
    }
#endif // ALTERNATIVE_LOAD
}