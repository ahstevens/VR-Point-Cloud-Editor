using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using System.Dynamic;
using System.Collections.Generic;

public class argument
{
    public string name = "";
    public string value = "";
}

public class bagConverter
{
    public List<argument> arguments = new List<argument>();
    public bool validFile = false;
    public bool supergrids = false;
    public bool requestFullResolution = false;

    public int XResolution = 0;
    public int YResolution = 0;
    public float Elevation = 0.0f;
    public float maxCellResolution = 0.0f;

    public int rasterResolution = 0;
    public float resInM = 0;
    public double requestedResInM = 1.1;

    public float realWorldW = 0.0f;
    public float realWorldH = 0.0f;

    public float realWorldScale = 1.0f;

    public double coordinateMinX = 0.0;
    public double coordinateMinY = 0.0;
    public double coordinateMaxX = 0.0;
    public double coordinateMaxY = 0.0;

    public string ProjectionRef;
    public int UTMZone = 0;

    [DllImport("Dll1")]
    static public extern IntPtr getGDALInfo(IntPtr filePath, IntPtr projectFilePath);

    [DllImport("Dll1")]
    static public extern void convertBAG(double outputResolution, double ResInM, IntPtr filePath, float beginX, float beginY, float W, float H);

    [DllImport("Dll1")]
    static public extern void convertBAGFullResolution(IntPtr filePath, float beginX, float beginY);

    public void getInfo(string filePath)
    {
        IntPtr strPtr = Marshal.StringToHGlobalAnsi(filePath);
        IntPtr projectPathPtr = Marshal.StringToHGlobalAnsi(Application.dataPath);
        IntPtr outStrPtr = getGDALInfo(strPtr, projectPathPtr);
        string convertedResult = Marshal.PtrToStringAnsi(outStrPtr);

        //Debug.Log(convertedResult);

        var newArguments = new List<String>();
        int position = 0;
        int start = 0;

        position = convertedResult.IndexOf(";,;", start);
        while (position > 0)
        {
            newArguments.Add(convertedResult.Substring(start, position - start).Trim());
            start = position + 3;
            position = convertedResult.IndexOf(";,;", start);
        }
        newArguments.Add(convertedResult.Substring(start, convertedResult.Length - start).Trim());

        arguments.Clear();
        foreach (var argument in newArguments)
        {
            position = argument.IndexOf(':', 0);
            string name = argument.Substring(0, position).Trim();
            string value = argument.Substring(position + 1, argument.Length - (position + 1)).Trim();
            
            argument newEntry = new argument();
            newEntry.name = name;
            newEntry.value = value;
            arguments.Add(newEntry);
        }

        foreach (var argument in arguments)
        {
            if (argument.name == "Valid")
            {
                if (float.Parse(argument.value) == 0)
                {
                    validFile = false;
                }
                else if (float.Parse(argument.value) == 1)
                {
                    validFile = true;
                }
            }

            if (argument.name == "Supergrids")
            {
                if (float.Parse(argument.value) == 0)
                {
                    supergrids = false;
                }
                else if (float.Parse(argument.value) == 1)
                {
                    supergrids = true;
                }
            }

            if (argument.name == "XResolution")
            {
                XResolution = (int)float.Parse(argument.value);
            }

            if (argument.name == "YResolution")
            {
                YResolution = (int)float.Parse(argument.value);
            }

            if (argument.name == "rasterResolution")
            {
                rasterResolution = (int)float.Parse(argument.value);
            }

            if (argument.name == "resInM")
            {
                resInM = float.Parse(argument.value);
            }

            if (argument.name == "Elevation")
            {
                Elevation = float.Parse(argument.value);
            }

            if (argument.name == "maxCellResolution")
            {
                maxCellResolution = float.Parse(argument.value);
            }

            if (argument.name == "coordinate.min.x")
            {
                coordinateMinX = float.Parse(argument.value);
            }

            if (argument.name == "coordinate.min.y")
            {
                coordinateMinY = float.Parse(argument.value);
            }

            if (argument.name == "coordinate.max.x")
            {
                coordinateMaxX = float.Parse(argument.value);
            }

            if (argument.name == "coordinate.max.y")
            {
                coordinateMaxY = float.Parse(argument.value);
            }

            if (argument.name == "ProjectionRef")
            {
                ProjectionRef = argument.value;
            }

            if (argument.name == "UTMZone")
            {
                UTMZone = (int)float.Parse(argument.value);
            }

            //Debug.Log("name: " + argument.name);
            //Debug.Log("value: " + argument.value);
        }

        if (supergrids)
        {
            XResolution = (int)((float)XResolution * resInM / 1.1);
            YResolution = (int)((float)YResolution * resInM / 1.1);

            realWorldW = rasterResolution * resInM;
            realWorldH = rasterResolution * resInM;
        }
        else
        {
            realWorldW = XResolution * resInM;
            realWorldH = YResolution * resInM;
        }
    }

    public void newTerrain(int resolution, string filePath, float beginX, float beginY, float W, float H)
    {
        IntPtr strPtr = Marshal.StringToHGlobalAnsi(filePath);
        convertBAG(resolution, requestedResInM, strPtr, beginX, beginY, W, H);
    }

    public void newTerrain(string filePath, float beginX, float beginY)
    {
        IntPtr strPtr = Marshal.StringToHGlobalAnsi(filePath);
        convertBAGFullResolution(strPtr, beginX, beginY);
    }
}

public class TerrainSpawner : EditorWindow
{
    static public bagConverter myConverter = new bagConverter();
    static public string currentFile = "";
    static public int resolutionSelected = 0;
    static public string[] availableResolutions = { "513x513", "1025x1025", "2049x2049", "4097x4097" };
    static public string[] availableColoringSchemas = { "2 color(smooth)", "rainbow color", "rainbow color(smooth)", "precision navigation" };
    private Texture2D BAGMap = null;

    private bool selectArea = false;
    private bool wantNewMap = false;
    private bool newMaploaded = false;
    private bool hideNoData = false;
    private bool showContourLines = false;

    private float iselectedX = 0;
    private float iselectedY = 0;
    private float iselectedW = 128;
    private float iselectedH = 128;

    private int BAGImageOverviewResolution = 260;

    private int coloringSchema = 0;
    private float precisionNavUpperDepth = 5.0f;
    private float precisionNavBottomDepth = 10.0f;

    private float contourStepInM = 10.0f;
    private float contourWidth = 1.0f;

    private static Material lastMaterial = null;
    private static object obj = null;

    [MenuItem("BAG To Terrain/BAG To Terrain")]

    public static void ShowWindow()
    {
        GetWindow(typeof(TerrainSpawner));
    }

    void Update()
    {
        if (wantNewMap)
        {
            if (File.Exists("Assets/Resources/BAGMap.png") && !newMaploaded)
            {
                byte[] imageData = File.ReadAllBytes("Assets/Resources/BAGMap.png");
                BAGMap = new Texture2D(1, 1);
                // Load data into the texture.
                BAGMap.LoadImage(imageData);

                newMaploaded = true;
                wantNewMap = false;
                Repaint();
            }
        }

        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<Terrain>() != null &&
            (string)Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.shader.name == (string)"Custom/HeightShader")
        {
            if (Selection.activeGameObject == obj)
                return;
            obj = Selection.activeGameObject;
            
            hideNoData = Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("hideNoData") == 0.0f ? false : true;
            showContourLines = Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("showContourLines") == 0.0f ? false : true;
            contourStepInM = 1.0f / Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("contourStepSize");
            contourWidth = Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("contourWidth");
            contourWidth = Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("contourWidth");
            coloringSchema = (int)Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("coloringSchema");
            precisionNavUpperDepth = (int)Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("precisionNavUpperDepth");
            precisionNavBottomDepth = (int)Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.GetFloat("precisionNavBottomDepth");
            Repaint();
        }
    }

    void OnGUI()
    {
        if (!myConverter.validFile && myConverter.arguments.Count > 0)
        {
            GUILayout.Label("Current file: " + currentFile, EditorStyles.boldLabel);
            UnityEngine.Color regularColor = GUI.contentColor;
            GUI.contentColor = Color.red;
            GUILayout.Label("An error occurred while loading file!", EditorStyles.boldLabel);
            GUI.contentColor = regularColor;

            if (GUILayout.Button("Open different BAG"))
            {
                getInfo();
            }
        }
        else if (myConverter.validFile)
        {
            if (!wantNewMap)
            {
                GUILayout.Box("", GUILayout.Width(BAGImageOverviewResolution), GUILayout.Height(BAGImageOverviewResolution));
                GUI.DrawTexture(new Rect(6, 3, BAGImageOverviewResolution, BAGImageOverviewResolution), BAGMap);

                //GUILayout.Box(BAGMap, GUILayout.Width(BAGImageOverviewResolution), GUILayout.Height(BAGImageOverviewResolution));
                //GUILayout.Label(BAGMap, GUILayout.Width(BAGImageOverviewResolution), GUILayout.Height(BAGImageOverviewResolution));
            }

            GUILayout.Label("Current file: " + currentFile, EditorStyles.boldLabel);

            if (myConverter.supergrids)
            {
                GUILayout.Label("Muilti resolution: TRUE");
            }
            else
            {
                GUILayout.Label("Muilti resolution: FALSE");
            }
            
            GUILayout.Label("Max resolution: " + myConverter.XResolution + " x " + myConverter.YResolution);
            if (selectArea || myConverter.requestFullResolution)
            {
                float areaRealWorldW = myConverter.realWorldW * ((float)iselectedW / (float)BAGImageOverviewResolution);
                float areaRealWorldH = myConverter.realWorldH * ((float)iselectedH / (float)BAGImageOverviewResolution);

                if (areaRealWorldW > 100000 || areaRealWorldH > 100000)
                {
                    myConverter.realWorldScale = 0.1f;
                    areaRealWorldW *= myConverter.realWorldScale;
                    areaRealWorldH *= myConverter.realWorldScale;
                    while (areaRealWorldW > 100000 || areaRealWorldH > 100000)
                    {
                        myConverter.realWorldScale *= 0.1f;
                        areaRealWorldW *= myConverter.realWorldScale;
                        areaRealWorldH *= myConverter.realWorldScale;
                    }
                }
                else if (areaRealWorldW <= 100000 && areaRealWorldH <= 100000)
                {
                    myConverter.realWorldScale = 1.0f;
                }

                GUILayout.Label("Real world size: " + myConverter.realWorldW * ((float)iselectedW / (float)BAGImageOverviewResolution) + " x " + myConverter.realWorldH * ((float)iselectedH / (float)BAGImageOverviewResolution) + " m");
            }
            else
            {
                float tempRealWorldW = myConverter.realWorldW;
                float tempRealWorldH = myConverter.realWorldH;
                if (tempRealWorldW > 100000 || tempRealWorldH > 100000)
                {
                    myConverter.realWorldScale = 0.1f;
                    tempRealWorldW *= myConverter.realWorldScale;
                    tempRealWorldH *= myConverter.realWorldScale;
                    while (tempRealWorldW > 100000 || tempRealWorldH > 100000)
                    {
                        myConverter.realWorldScale *= 0.1f;
                        tempRealWorldW *= myConverter.realWorldScale;
                        tempRealWorldH *= myConverter.realWorldScale;
                    }
                }
                else if (tempRealWorldW <= 100000 && tempRealWorldH <= 100000)
                {
                    myConverter.realWorldScale = 1.0f;
                }

                GUILayout.Label("Real world size: " + myConverter.realWorldW + " x " + myConverter.realWorldH + " m");
            }

            if (myConverter.realWorldScale < 1.0f)
            {
                UnityEngine.Color regularColor = GUI.contentColor;
                GUI.contentColor = Color.red;
                GUILayout.Label("Terrain size would be downscaled by " + 1.0f / myConverter.realWorldScale + " times because of Unity restriction.");
                GUI.contentColor = regularColor;
            }

            selectArea = GUILayout.Toggle(selectArea, "Select what area to import");
            if (selectArea)
                myConverter.requestFullResolution = false;

            if (myConverter.supergrids)
            {
                myConverter.requestFullResolution = GUILayout.Toggle(myConverter.requestFullResolution, "Select area with full resolution");
                if (myConverter.requestFullResolution)
                    selectArea = false;
            }
            else
            {
                myConverter.requestFullResolution = false;
            }

            if (selectArea)
            {
                if (iselectedX + iselectedW > BAGImageOverviewResolution)
                    iselectedX = 0;

                if (iselectedY + iselectedH > BAGImageOverviewResolution)
                    iselectedY = 0;

                float temp = EditorGUILayout.FloatField("area.left in %: ", ((float)iselectedX / (float)BAGImageOverviewResolution) * 100.0f);
                temp *= (float)BAGImageOverviewResolution / 100.0f;
                if (temp + iselectedW <= BAGImageOverviewResolution && temp >= 0)
                    iselectedX = temp;
                temp = EditorGUILayout.FloatField("area.top in %: ", ((float)iselectedY / (float)BAGImageOverviewResolution) * 100.0f);
                temp *= (float)BAGImageOverviewResolution / 100.0f;
                if (temp + iselectedH <= BAGImageOverviewResolution && temp >= 0)
                    iselectedY = temp;

                if (myConverter.supergrids)
                {
                    float cof = (float)myConverter.requestedResInM;
                    iselectedW = (float)BAGImageOverviewResolution * (((float)Math.Pow(2.0, 9.0 + resolutionSelected) + 1.0f) / (float)myConverter.XResolution / 1.1f) * cof;
                    iselectedH = (float)BAGImageOverviewResolution * (((float)Math.Pow(2.0, 9.0 + resolutionSelected) + 1.0f) / (float)myConverter.YResolution / 1.1f) * cof;
                }
                else
                {
                    iselectedW = (float)BAGImageOverviewResolution * (((float)Math.Pow(2.0, 9.0 + resolutionSelected) + 1.0f) / (float)myConverter.XResolution);
                    iselectedH = (float)BAGImageOverviewResolution * (((float)Math.Pow(2.0, 9.0 + resolutionSelected) + 1.0f) / (float)myConverter.YResolution);
                }

                if (iselectedW > BAGImageOverviewResolution)
                    iselectedW = BAGImageOverviewResolution;

                if (iselectedH > BAGImageOverviewResolution)
                    iselectedH = BAGImageOverviewResolution;

                float beginX = 6;
                float beginY = 3;
                EditorGUI.DrawRect(new Rect(beginX + iselectedX, beginY + iselectedY, iselectedW, iselectedH), new Color(0.5f, 0.5f, 0.9f, 0.5f));

                if (myConverter.supergrids)
                {
                    myConverter.requestedResInM = EditorGUILayout.DoubleField("resolution in M per pixel: ", myConverter.requestedResInM);
                    if (myConverter.requestedResInM < 1.1)
                        myConverter.requestedResInM = 1.1;

                    double resultResolution = (myConverter.resInM * myConverter.rasterResolution) / myConverter.requestedResInM;
                    if (resultResolution < (Math.Pow(2, 9 + resolutionSelected) + 1))
                    {
                        myConverter.requestedResInM = myConverter.resInM / ((Math.Pow(2, 9 + resolutionSelected) + 1) / myConverter.rasterResolution);
                    }
                }
            }

            if (myConverter.requestFullResolution)
            {
                if (iselectedX + iselectedW > BAGImageOverviewResolution)
                    iselectedX = 0;

                if (iselectedY + iselectedH > BAGImageOverviewResolution)
                    iselectedY = 0;

                float temp = EditorGUILayout.FloatField("area.left in %: ", ((float)iselectedX / (float)BAGImageOverviewResolution) * 100.0f);
                temp *= (float)BAGImageOverviewResolution / 100.0f;
                if (temp + iselectedW <= BAGImageOverviewResolution && temp >= 0)
                    iselectedX = temp;
                temp = EditorGUILayout.FloatField("area.top in %: ", ((float)iselectedY / (float)BAGImageOverviewResolution) * 100.0f);
                temp *= (float)BAGImageOverviewResolution / 100.0f;
                if (temp + iselectedH <= BAGImageOverviewResolution && temp >= 0)
                    iselectedY = temp;

                iselectedW = (float)BAGImageOverviewResolution * (4050.0f / (myConverter.rasterResolution * myConverter.maxCellResolution));
                iselectedH = (float)BAGImageOverviewResolution * (4050.0f / (myConverter.rasterResolution * myConverter.maxCellResolution));

                if (iselectedW > BAGImageOverviewResolution)
                    iselectedW = BAGImageOverviewResolution;

                if (iselectedH > BAGImageOverviewResolution)
                    iselectedH = BAGImageOverviewResolution;

                float beginX = 6;
                float beginY = 3;
                EditorGUI.DrawRect(new Rect(beginX + iselectedX, beginY + iselectedY, iselectedW, iselectedH), new Color(0.5f, 0.5f, 0.9f, 0.5f));
            }

            if (!myConverter.requestFullResolution)
                resolutionSelected = EditorGUILayout.Popup("Output resolution", resolutionSelected, availableResolutions);

            if (GUILayout.Button("Create terrain from current BAG file"))
            {
                GameObject newTerrain = newSpawnTerrain();

                GameObject geoReference = GameObject.Find("UnityZeroGeoReference");
                if (geoReference == null)
                {
                    geoReference = new GameObject("UnityZeroGeoReference");

                    geoReference.transform.position = new Vector3((float)(myConverter.coordinateMinX),
                                                                  myConverter.UTMZone,
                                                                  (float)(myConverter.coordinateMinY));
                }
                else
                {
                    //newTerrain.transform.localScale = new Vector3(-1.0f, 1.0f, -1.0f);
                    newTerrain.transform.position = new Vector3((float)-(geoReference.transform.position.x - myConverter.coordinateMinX),
                                                                0.0f,
                                                                (float)-(geoReference.transform.position.z - myConverter.coordinateMinY));
                }
            }

            if (GUILayout.Button("Open different BAG"))
            {
                selectArea = false;

                iselectedX = 0;
                iselectedY = 0;
                iselectedW = 128;
                iselectedH = 128;
                getInfo();
            }
        }
        else if (myConverter.arguments.Count == 0)
        {
            if (GUILayout.Button("Open BAG"))
            {
                selectArea = false;
                
                iselectedX = 0;
                iselectedY = 0;
                iselectedW = 128;
                iselectedH = 128;

                getInfo();
            }
        }

        GUILayout.Label("ProjectionRef: " + myConverter.ProjectionRef);
        GUILayout.Label("UTMZone: " + myConverter.UTMZone);



        GUILayout.Label("coordinate.min.x: " + myConverter.coordinateMinX);
        GUILayout.Label("coordinate.min.y: " + myConverter.coordinateMinY);
        GUILayout.Label("coordinate.max.x: " + myConverter.coordinateMaxX);
        GUILayout.Label("coordinate.max.y: " + myConverter.coordinateMaxY);

        GUILayout.Label("Shading settings: ");

        float newColoringSchema = EditorGUILayout.Popup("Coloring schema", coloringSchema, availableColoringSchemas);
        bool newHideNoData = GUILayout.Toggle(hideNoData, "Hide area with no data");
        bool newShowContourLines = GUILayout.Toggle(showContourLines, "Show contour lines");
        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<Terrain>() != null &&
            (string)Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.shader.name == (string)"Custom/HeightShader")
        {
            Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("hideNoData", newHideNoData ? 1.0f : 0.0f);
            hideNoData = newHideNoData;
            Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("showContourLines", newShowContourLines ? 1.0f : 0.0f);
            showContourLines = newShowContourLines;

            if (newShowContourLines)
            {
                float newContourStepInM = (EditorGUILayout.FloatField("Contour lines step in M: ", contourStepInM));
                if (newContourStepInM < 0.1f)
                    newContourStepInM = 0.1f;
                contourStepInM = newContourStepInM;

                float newContourWidth = (EditorGUILayout.FloatField("Contour lines width: ", contourWidth));
                if (newContourWidth < 0.1f)
                    newContourWidth = 0.1f;
                contourWidth = newContourWidth;

                Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("contourStepSize", 1.0f / newContourStepInM);
                Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("contourWidth", newContourWidth);
            }

            Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("coloringSchema", (float)newColoringSchema);
            coloringSchema = (int)newColoringSchema;

            if (newColoringSchema == 3)
            {
                float newPrecisionNavUpperDepth = EditorGUILayout.FloatField("precisionNavUpperDepth: ", precisionNavUpperDepth);
                float newPrecisionNavBottomDepth = precisionNavBottomDepth;
                if (newPrecisionNavBottomDepth < newPrecisionNavUpperDepth)
                {
                    newPrecisionNavBottomDepth = newPrecisionNavUpperDepth + 0.1f;
                }

                newPrecisionNavBottomDepth = EditorGUILayout.FloatField("precisionNavBottomDepth: ", precisionNavBottomDepth);
                if (newPrecisionNavUpperDepth > newPrecisionNavBottomDepth)
                {
                    newPrecisionNavUpperDepth = newPrecisionNavBottomDepth - 0.1f;
                }

                Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("precisionNavUpperDepth", newPrecisionNavUpperDepth);
                precisionNavUpperDepth = newPrecisionNavUpperDepth;
                Selection.activeGameObject.GetComponent<Terrain>().materialTemplate.SetFloat("precisionNavBottomDepth", newPrecisionNavBottomDepth);
                precisionNavBottomDepth = newPrecisionNavBottomDepth;
            }
        }
    }

    private void getInfo()
    {
        currentFile = EditorUtility.OpenFilePanel("Choose bag file", "", "bag");
        if (currentFile != "")
        {
            if (!Directory.Exists("Assets/Resources"))
                Directory.CreateDirectory("Assets/Resources");

            File.Delete("Assets/Resources/BAGMap.png");
            File.Delete("Assets/Resources/BAGMap.png.meta");

            File.Delete("Assets/Resources/BAGMap.png.aux.xml");
            File.Delete("Assets/Resources/BAGMap.png.aux.xml.meta");

            newMaploaded = false;
            wantNewMap = true;
            myConverter.getInfo(currentFile);
        }
    }

    private GameObject newSpawnTerrain()
    {
        if (selectArea)
        {
            float beginXInPercent = ((float)iselectedX / (float)BAGImageOverviewResolution);
            float beginYInPercent = ((float)iselectedY / (float)BAGImageOverviewResolution);

            myConverter.newTerrain(Convert.ToInt32(Math.Pow(2, 9 + resolutionSelected)) + 1, currentFile,
                                   beginXInPercent, beginYInPercent,
                                   Convert.ToInt32(Math.Pow(2, 9 + resolutionSelected)) + 1, Convert.ToInt32(Math.Pow(2, 9 + resolutionSelected)) + 1);
        }
        else if (myConverter.requestFullResolution)
        {
            float beginXInPercent = ((float)iselectedX / (float)BAGImageOverviewResolution);
            float beginYInPercent = ((float)iselectedY / (float)BAGImageOverviewResolution);

            myConverter.newTerrain(currentFile, beginXInPercent, beginYInPercent);
        }
        else
        {
            myConverter.newTerrain(Convert.ToInt32(Math.Pow(2, 9 + resolutionSelected)) + 1, currentFile, -1, -1, -1, -1);
        }

        TerrainData terrainData = new TerrainData();
        terrainData.baseMapResolution = 1024;
        terrainData.heightmapResolution = Convert.ToInt32(Math.Pow(2, 9 + resolutionSelected)) + 1;
        if (myConverter.requestFullResolution)
            terrainData.heightmapResolution = 4097;

        terrainData.alphamapResolution = 1024;

        if (selectArea || myConverter.requestFullResolution)
        {
            terrainData.size = new Vector3(myConverter.realWorldW * ((float)iselectedW / (float)BAGImageOverviewResolution) * myConverter.realWorldScale, myConverter.Elevation, myConverter.realWorldH * ((float)iselectedH / (float)BAGImageOverviewResolution) * myConverter.realWorldScale);
        }
        else
        {
            terrainData.size = new Vector3(myConverter.realWorldW * myConverter.realWorldScale, myConverter.Elevation, myConverter.realWorldH * myConverter.realWorldScale);
        }

        if (myConverter.requestFullResolution)
        {
            LoadTerrain(Application.dataPath + "/data.raw", terrainData, 4097);
        }
        else
        {
            LoadTerrain(Application.dataPath + "/data.raw", terrainData, Convert.ToInt32(Math.Pow(2, 9 + resolutionSelected)) + 1);
        }
        GameObject terrain = (GameObject)Terrain.CreateTerrainGameObject(terrainData);

        // choosing name for new terrain
        Terrain[] allTerrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
        string newTerrainName = "newTerrain_0";
        int iteration = 0;
        bool nameAlreadyExist = false;
        while (true)
        {
            nameAlreadyExist = false;
            foreach (Terrain currentTerrain in allTerrains)
            {
                if ((string)currentTerrain.name == newTerrainName)
                    nameAlreadyExist = true;
            }

            if (!nameAlreadyExist)
                break;

            newTerrainName = "newTerrain_" + iteration;
            iteration++;

            if (iteration > 1000)
                break;
        }
        terrain.name = newTerrainName;

        Material terrainMaterial = Resources.Load<Material>("Material/BAGToTerrain/standardMaterial");
        Material terrainMaterialFinal = new Material(terrainMaterial.shader);

        terrainMaterialFinal.SetFloat("zeroLevel", myConverter.Elevation);
        terrain.GetComponent<Terrain>().materialTemplate = terrainMaterialFinal;
        terrainMaterialFinal.SetTexture("_MainTex", terrainMaterial.GetTexture("_MainTex"));
        lastMaterial = terrainMaterialFinal;

        //terrainMaterial.SetFloat("zeroLevel", myConverter.Elevation);
        //terrain.GetComponent<Terrain>().materialTemplate = terrainMaterial;
        terrain.transform.position = new Vector3(0, 0, 0);

        string newTerrainDataFileName = "Assets/" + "TerrainData_0.asset";
        iteration = 1;
        while(File.Exists(newTerrainDataFileName))
        {
            newTerrainDataFileName = "Assets/" + "TerrainData_" + iteration + ".asset";
            iteration++;
            if (iteration > 1000)
                break;
        }
        AssetDatabase.CreateAsset(terrainData, newTerrainDataFileName);

        return terrain;
    }

    void LoadTerrain(string aFileName, TerrainData aTerrain, int realResolution)
	{
		int h = realResolution;
		int w = realResolution;

		float[,] data = new float[h, w];

		using (var file = System.IO.File.OpenRead(aFileName))
		using (var reader = new System.IO.BinaryReader(file))
		{
			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
                    if (myConverter.requestFullResolution && (y < 4050 && x < 4050) || !myConverter.requestFullResolution)
                    {
                        float v = (float)reader.ReadUInt16() / 0xFFFF;
                        data[y, x] = v;
                    }
                    else if (myConverter.requestFullResolution && (y >= 4050 || x >= 4050))
                    {
                        data[y, x] = 1.0f;
                    }
                }
			}
		}
		aTerrain.SetHeights(0, 0, data);

        File.Delete("Assets/data.raw");
        File.Delete("Assets/data.raw.meta");
    }
}