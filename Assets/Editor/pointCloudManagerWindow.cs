using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Runtime.InteropServices;

public class pointCloudManagerWindow : EditorWindow
{
    private static bool outliersSearch = false;
    private static int tempMinNeigboars = 5;
    private static float tempNeigboarsDistance = 5.0f;
    bool firstFrame = true;
    static int indexSelected;

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

    void OnGUI()
    {
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

                if (GUILayout.Button("Unload File"))
                    if (pointCloudManager.UnLoad(pointClouds[indexSelected].ID))
                        indexSelected = 0;
            }
        }

        if (pointClouds.Length > 0)
        {
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
        }
    }

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
        }

        pointCloudManager.CheckIsAsyncLoadFinished();

        if (GetPointCloudManagerGameObject() == null)
            CreatePointCloudManagerGameObject();
    }

    void OnInspectorUpdate()
    {
        Repaint();
    }

    private string OpenLAZFileDialog()
    {
        string currentFile = EditorUtility.OpenFilePanel("Choose file", "", "laz,las,cpc");
        if (currentFile == "")
            return currentFile;
        pointCloudManager.LoadLAZFile(currentFile);

        return currentFile;
    }

    private void SaveLAZFileDialog(string pointCloudID, string defaultName)
    {
        string saveToFile = EditorUtility.SaveFilePanel("Save to", "", defaultName, "laz");
        if (saveToFile == "")
            return;

        pointCloudManager.SaveLAZFile(saveToFile, pointCloudID);
    }

    private GameObject GetPointCloudManagerGameObject()
    {
        GameObject pointCloudManager = GameObject.Find("pointCloudManager");
        if (pointCloudManager == null)
            return null;

        return pointCloudManager;
    }

    private void CreatePointCloudManagerGameObject()
    {
        GameObject pointCloudManager = new GameObject("pointCloudManager");

        MonoScript script = Resources.Load<MonoScript>("CCOM/PointCloud/Scripts/pointCloudManager");
        ScriptableObject myCommonClass = ScriptableObject.CreateInstance(script.GetClass()) as ScriptableObject;
        pointCloudManager.AddComponent(myCommonClass.GetType());
    }
}