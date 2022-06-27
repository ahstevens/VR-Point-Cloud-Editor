/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class OnlineMapsWizard : EditorWindow
{
    internal delegate void OnlineMapsWizardDelegate(ref bool allowCreate);

    #region Variables

    #region Wizard

    private string[] control2DTitles;
    private string[] control3DTitles;
    private List<Control> controls2D;
    private List<Control> controls3D;
    private int default2DIndex = 0;
    private int default3DIndex = 0;
    private bool is2D = false;

    private List<IPlugin> plugins;
    private Vector2 scrollPosition;
    private int selectedIndex = 0;
    private List<OnlineMapsWizardDelegate> steps;

    #endregion

    #region General

    private OnlineMapsProvider.MapType activeMapType;
    private string customProviderURL;
    private bool labels;
    private string language = "en";
    private int providerIndex;
    private OnlineMapsProvider[] providers;
    private string[] providersTitle;
    private string proxyURL = "https://service.infinity-code.com/redirect.php?";
    private bool showCustomProviderTokens;
    private OnlineMapsSource source;
    private bool traffic;

    #endregion

    #region 3D Controls

    private Camera activeCamera;
    private Vector2 sizeInScene = new Vector2(1024, 1024);

    #endregion

    #region Tileset

    private Shader defaultTilesetShader;
    private Shader drawingShader;
    private Material markerMaterial;
    private Shader markerShader;
    private bool moveCameraToMap;
    private Material tileMaterial;
    private int tilesetHeight = 1024;
    private Shader tilesetShader;
    private int tilesetWidth = 1024;

    #endregion

    #region Texture

    private string[] availableSizesStr;
    private bool createTexture = true;
    private string textureFilename = "OnlineMaps";
    private int textureHeight = 512;
    private int textureWidth = 512;
    private GameObject uGUIParent;
#if NGUI
    private GameObject NGUIParent;
#endif

    #endregion

    #endregion

    private Control activeControl
    {
        get { return is2D ? controls2D[selectedIndex] : controls3D[selectedIndex]; }
    }

    private void CacheControls()
    {
        controls2D = new List<Control>();
        controls3D = new List<Control>();

        Type[] types = typeof(OnlineMaps).Assembly.GetTypes();
        foreach (Type t in types)
        {
            if (!t.IsSubclassOf(typeof(OnlineMapsControlBase)) || t.IsAbstract) continue;
#if !NGUI
            if (t == typeof(OnlineMapsNGUITextureControl)) continue;
#endif

            string fullName = t.FullName;
            if (fullName.StartsWith("OnlineMaps")) fullName = fullName.Substring(10);

            int controlIndex = fullName.IndexOf("Control");
            fullName = fullName.Insert(controlIndex, " ");

            int textureIndex = fullName.IndexOf("Texture");
            if (textureIndex > 0) fullName = fullName.Insert(textureIndex, " ");

            Control control = new Control(fullName, t);

            if (t.IsSubclassOf(typeof(OnlineMapsControlBase2D))) controls2D.Add(control);
            else
            {
                if (t == typeof(OnlineMapsTileSetControl)) default3DIndex = controls3D.Count;
                controls3D.Add(control);
            }

            if (t == typeof(OnlineMapsUIImageControl) || t == typeof(OnlineMapsUIRawImageControl)) control.steps.Add(DrawUGUIParent);
            else if (t == typeof(OnlineMapsNGUITextureControl)) control.steps.Add(DrawNGUIParent);
            if (t.IsSubclassOf(typeof(OnlineMapsControlBase3D))) control.steps.Add(DrawCamera);
            if (t.IsSubclassOf(typeof(OnlineMapsControlBaseDynamicMesh))) control.steps.Add(DrawMeshSize);
            if (t == typeof(OnlineMapsTileSetControl)) control.steps.Add(DrawMaterialsAndShaders);

            object[] controlHelperAttibutes = t.GetCustomAttributes(typeof(OnlineMapsWizardControlHelperAttribute), true);
            if (controlHelperAttibutes.Length > 0)
            {
                control.resultType = (controlHelperAttibutes[0] as OnlineMapsWizardControlHelperAttribute).resultType;
                if (control.resultType == OnlineMapsTarget.texture)
                {
                    control.steps.Add(DrawTextureSize);
                }
            }

            foreach (IPlugin plugin in plugins)
            {
                Type requiredType = null;
                if (plugin is Plugin) requiredType = (plugin as Plugin).attribute.requiredType;
                else if (plugin is PluginGroup) requiredType = (plugin as PluginGroup).plugins[0].attribute.requiredType;

                if (t.IsSubclassOf(requiredType)) control.plugins.Add(plugin);
            }
        }

        control2DTitles = controls2D.Select(c => c.title).ToArray();
        control3DTitles = controls3D.Select(c => c.title).ToArray();
        selectedIndex = default3DIndex;
    }

    private void CachePlugins()
    {
        plugins = new List<IPlugin>();
        Type[] types = typeof(OnlineMaps).Assembly.GetTypes();
        foreach (Type t in types)
        {
            object[] attributes = t.GetCustomAttributes(typeof(OnlineMapsPluginAttribute), true);
            if (attributes.Length > 0 && !t.IsAbstract)
            {
                OnlineMapsPluginAttribute p = attributes[0] as OnlineMapsPluginAttribute;
                if (!string.IsNullOrEmpty(p.group))
                {
                    PluginGroup g = plugins.FirstOrDefault(pg => pg is PluginGroup && pg.title == p.group) as PluginGroup;
                    if (g == null)
                    {
                        g = new PluginGroup(p.group);
                        plugins.Add(g);
                    }
                    g.Add(t, p);
                }
                else plugins.Add(new Plugin(t, p));
            }
        }
        plugins = plugins.OrderBy(p => p.title).ToList();
    }

    private float CheckCameraDistance(Camera tsCamera)
    {
        if (tsCamera == null) return -1;

        Vector3 cameraPosition;
        if (moveCameraToMap)
        {
            cameraPosition = new Vector3(sizeInScene.x / -2, Mathf.Min(sizeInScene.x, sizeInScene.y), sizeInScene.y / 2);
        }
        else
        {
            cameraPosition = tsCamera.transform.position;
        }

        Vector3 mapCenter = new Vector3(sizeInScene.x / -2, 0, sizeInScene.y / 2);
        float distance = (cameraPosition - mapCenter).magnitude * 3f;

        if (distance <= tsCamera.farClipPlane) return -1;

        return distance;
    }

    private void CreateMap()
    {
        OnlineMaps map = CreateMapGameObject();
        GameObject go = map.gameObject;
        Sprite sprite = null;

        Control control = activeControl;
        Component component = go.AddComponent(control.type);
        if (control.resultType == OnlineMapsTarget.texture)
        {
            map.redrawOnPlay = true;

            string texturePath;
            Texture2D texture = CreateTexture(map, out texturePath);

            if (control.useSprite)
            {

                if (!string.IsNullOrEmpty(texturePath))
                {
                    TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.SaveAndReimport();

                    sprite = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Sprite)) as Sprite;
                }

                if (component is OnlineMapsSpriteRendererControl)
                {
                    SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
                    Debug.Log(spriteRenderer);
                    spriteRenderer.sprite = sprite;
                    go.AddComponent<BoxCollider>();
                }
            }

            if (component is OnlineMapsUIImageControl || component is OnlineMapsUIRawImageControl)
            {
                if (uGUIParent == null) uGUIParent = OnlineMapsEditorUtils.GetCanvas().gameObject;

                RectTransform rectTransform = go.AddComponent<RectTransform>();
                rectTransform.SetParent(uGUIParent.transform as RectTransform);
                go.AddComponent<CanvasRenderer>();
                rectTransform.localPosition = Vector3.zero;
                rectTransform.anchorMax = rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(textureWidth, textureHeight);

                if (component is OnlineMapsUIImageControl)
                {
                    Image image = go.AddComponent<Image>();
                    image.sprite = sprite;
                }
                else
                {
                    RawImage image = go.AddComponent<RawImage>();
                    image.texture = texture;
                }
            }

            if (component is OnlineMapsNGUITextureControl)
            {
#if NGUI
                go.layer = NGUIParent.layer;
                UITexture uiTexture = go.AddComponent<UITexture>();
                uiTexture.mainTexture = texture;
                uiTexture.width = textureWidth;
                uiTexture.height = textureHeight;
                go.transform.parent = NGUIParent.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.Euler(Vector3.zero);
                BoxCollider boxCollider = go.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(textureWidth, textureHeight, 0);
#endif
            }
            if (component is OnlineMapsTextureControl)
            {
                Renderer renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Diffuse"));
                renderer.sharedMaterial.mainTexture = texture;
            }
        }
        else
        {
            OnlineMapsControlBaseDynamicMesh control3D = component as OnlineMapsControlBaseDynamicMesh;
            map.width = tilesetWidth;
            map.height = tilesetHeight;
            control3D.sizeInScene = sizeInScene;
            map.renderInThread = false;

            OnlineMapsTileSetControl tsControl = component as OnlineMapsTileSetControl;
            if (tsControl != null)
            {
                tsControl.tileMaterial = tileMaterial;
                tsControl.markerMaterial = markerMaterial;
                tsControl.tilesetShader = tilesetShader;
                tsControl.drawingShader = drawingShader;
                tsControl.markerShader = markerShader;
            }

            if (moveCameraToMap)
            {
                GameObject cameraGO = activeCamera.gameObject;
                float minSide = Mathf.Min(sizeInScene.x, sizeInScene.y);
                Vector3 pos = new Vector3(sizeInScene.x / -2, minSide, sizeInScene.y / 2);
                cameraGO.transform.position = pos;
                cameraGO.transform.rotation = Quaternion.Euler(90, 180, 0);
            }
        }

        foreach (IPlugin plugin in control.plugins)
        {
            if (plugin is Plugin)
            {
                Plugin p = plugin as Plugin;
                if (p.enabled) go.AddComponent(p.type);
            }
            else if (plugin is PluginGroup)
            {
                PluginGroup g = plugin as PluginGroup;
                if (g.selected > 0)
                {
                    go.AddComponent(g.plugins.First(p => p.title == g.titles[g.selected]).type);
                }
            }
        }

        EditorGUIUtility.PingObject(go);
        Selection.activeGameObject = go;
    }

    private OnlineMaps CreateMapGameObject()
    {
        GameObject go;
        if (activeControl.type == typeof(OnlineMapsTextureControl)) go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        else go = new GameObject("Map");

        OnlineMaps map = go.AddComponent<OnlineMaps>();

        map.source = source;
        map.mapType = activeMapType.ToString();
        map.proxyURL = proxyURL;
        map.labels = labels;
        map.customProviderURL = customProviderURL;
        map.language = language;
        map.traffic = traffic;
        map.redrawOnPlay = true;

        return map;
    }

    private Texture2D CreateTexture(OnlineMaps map, out string texturePath)
    {
        if (!createTexture)
        {
            texturePath = String.Empty;
            return null;
        }

        texturePath = string.Format("Assets/{0}.png", textureFilename);
        map.texture = new Texture2D(textureWidth, textureHeight);
        File.WriteAllBytes(texturePath, map.texture.EncodeToPNG());
        AssetDatabase.Refresh();
        TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = true;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = Mathf.Max(textureWidth, textureHeight);

            if (activeControl.useSprite)
            {
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.npotScale = TextureImporterNPOTScale.None;
            }

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            map.texture = (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));
        }
        return map.texture;
    }

    private void DrawCamera(ref bool allowCreate)
    {
        EditorGUILayout.LabelField("Camera Settings");

        Camera cam = activeCamera != null ? activeCamera : Camera.main;
        activeCamera = EditorGUILayout.ObjectField("Camera: ", cam, typeof(Camera), true) as Camera;
        moveCameraToMap = EditorGUILayout.Toggle("Move camera to Map", moveCameraToMap);

        float needFixCameraDistance = CheckCameraDistance(activeCamera);

        if (Math.Abs(needFixCameraDistance + 1) > float.Epsilon)
        {
            EditorGUILayout.HelpBox("Potential problem detected:\n\"Camera - Clipping Planes - Far\" is too small.",
                MessageType.Warning);

            if (GUILayout.Button("Fix Clipping Planes - Far")) cam.farClipPlane = needFixCameraDistance;
        }
    }

    private void DrawControls(ref bool allowcreate)
    {
        if (is2D)
        {
            EditorGUILayout.HelpBox(
                "All 2D controls have the same features.\nSelect a control, depending on the place where you want to show the map.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Tileset - a dynamic mesh. Faster, uses less memory and has many additional features. It is recommended for most applications.\nTexture (Plane) - used to display maps on the plane.",
                MessageType.Info);
        }

        EditorGUILayout.LabelField("Select control");

        string[] titles = is2D ? control2DTitles : control3DTitles;

        EditorGUI.BeginChangeCheck();
        selectedIndex = GUILayout.SelectionGrid(selectedIndex, titles, 1, "toggle");
        if (EditorGUI.EndChangeCheck())
        {
            if (is2D) default2DIndex = selectedIndex;
            else default3DIndex = selectedIndex;
            InitSteps();
        }
    }

    private void DrawLabels()
    {
        bool showLanguage;
        if (activeMapType.hasLabels)
        {
            labels = EditorGUILayout.Toggle("Labels: ", labels);
            showLanguage = labels;
        }
        else
        {
            showLanguage = activeMapType.labelsEnabled;
            GUILayout.Label("Labels " + (showLanguage ? "enabled" : "disabled"));
        }
        if (showLanguage && activeMapType.hasLanguage)
        {
            language = EditorGUILayout.TextField("Language: ", language);
            EditorGUILayout.HelpBox(activeMapType.provider.twoLetterLanguage ? "Use two-letter code such as: en" : "Use three-letter code such as: eng", MessageType.Info);
        }
    }

    private void DrawMapType(ref bool allowCreate)
    {
        EditorGUILayout.LabelField("Select the type of map");
        EditorGUI.BeginChangeCheck();
        is2D = GUILayout.SelectionGrid(is2D? 0: 1, new[] { "2D", "3D" }, 1, "toggle") == 0;
        if (EditorGUI.EndChangeCheck())
        {
            selectedIndex = is2D ? default2DIndex : default3DIndex;
            InitSteps();
        }
    }

    private void DrawMaterialsAndShaders(ref bool allowcreate)
    {
        EditorGUILayout.LabelField("Materials and Shaders (optional)");

        tileMaterial =
            EditorGUILayout.ObjectField("Tile material: ", tileMaterial, typeof(Material), false) as Material;
        markerMaterial =
            EditorGUILayout.ObjectField("Marker Material:", markerMaterial, typeof(Material), false) as Material;
        tilesetShader = EditorGUILayout.ObjectField("Tileset Shader:", tilesetShader, typeof(Shader), true) as Shader;
        markerShader = EditorGUILayout.ObjectField("Marker Shader:", markerShader, typeof(Shader), false) as Shader;
        drawingShader = EditorGUILayout.ObjectField("Drawing Shader:", drawingShader, typeof(Shader), false) as Shader;
    }

    private void DrawMeshSize(ref bool allowCreate)
    {
        EditorGUILayout.LabelField("Size");
        tilesetWidth = EditorGUILayout.IntField("Width (pixels): ", tilesetWidth);
        tilesetHeight = EditorGUILayout.IntField("Height (pixels): ", tilesetHeight);
        sizeInScene = EditorGUILayout.Vector2Field("Size (in scene): ", sizeInScene);

        textureWidth = Mathf.ClosestPowerOfTwo(textureWidth);
        if (textureWidth < 512) textureWidth = 512;

        tilesetHeight = Mathf.ClosestPowerOfTwo(tilesetHeight);
        if (tilesetHeight < 512) tilesetHeight = 512;
    }

    private void DrawMoreFeatures(ref bool allowcreate)
    {
        EditorGUILayout.LabelField("More Features");
        traffic = EditorGUILayout.Toggle("Traffic: ", traffic);
    }

    private void DrawPlugins(ref bool allowcreate)
    {
        EditorGUILayout.LabelField("Plugins");
        EditorGUIUtility.labelWidth += 100;
        foreach (IPlugin plugin in activeControl.plugins)
        {
            if (plugin is Plugin)
            {
                Plugin p = plugin as Plugin;
                p.enabled = EditorGUILayout.Toggle(plugin.title, p.enabled);
            }
            else if (plugin is PluginGroup)
            {
                PluginGroup g = plugin as PluginGroup;
                g.selected = EditorGUILayout.Popup(g.title, g.selected, g.titles);
            }
        }
        EditorGUIUtility.labelWidth -= 50;
    }

    private void DrawProvider()
    {
        EditorGUI.BeginChangeCheck();
        providerIndex = EditorGUILayout.Popup("Provider", providerIndex, providersTitle);
        if (EditorGUI.EndChangeCheck()) activeMapType = providers[providerIndex].types[0];

        if (activeMapType.isCustom)
        {
            customProviderURL = EditorGUILayout.TextField("URL: ", customProviderURL);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            showCustomProviderTokens = OnlineMapsEditor.Foldout(showCustomProviderTokens, "Available tokens");
            if (showCustomProviderTokens)
            {
                GUILayout.Label("{zoom}");
                GUILayout.Label("{x}");
                GUILayout.Label("{y}");
                GUILayout.Label("{quad}");
                GUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSource(ref bool allowCreate)
    {
        source = (OnlineMapsSource) EditorGUILayout.EnumPopup("Source: ", source);

        if (source != OnlineMapsSource.Resources)
        {
            proxyURL = EditorGUILayout.TextField("Proxy (for WebGL): ", proxyURL);

            DrawProvider();

            GUIContent[] availableTypes = activeMapType.provider.types.Select(t => new GUIContent(t.title)).ToArray();
            int index = activeMapType.index;
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup(new GUIContent("Type: ", "Type of map texture"), index, availableTypes);
            if (EditorGUI.EndChangeCheck()) activeMapType = activeMapType.provider.types[index];

            DrawLabels();
        }
    }

    private void DrawTextureSize(ref bool allowCreate)
    {
        createTexture = EditorGUILayout.ToggleLeft("Create Texture", createTexture);
        EditorGUI.BeginDisabledGroup(!createTexture);

        if (availableSizesStr == null || availableSizesStr.Length == 0)
            availableSizesStr = OnlineMapsEditor.availableSizes.Select(s => s.ToString()).ToArray();

        if (!OnlineMapsEditor.availableSizes.Contains(textureWidth)) textureWidth = 512;
        if (!OnlineMapsEditor.availableSizes.Contains(textureHeight)) textureHeight = 512;

        textureWidth = EditorGUILayout.IntPopup("Width: ", textureWidth,
            availableSizesStr, OnlineMapsEditor.availableSizes);
        textureHeight = EditorGUILayout.IntPopup("Height: ", textureHeight,
            availableSizesStr, OnlineMapsEditor.availableSizes);

        textureFilename = EditorGUILayout.TextField("Filename: ", textureFilename);

        EditorGUI.EndDisabledGroup();
    }

    private void DrawNGUIParent(ref bool allowCreate)
    {
#if NGUI
        EditorGUILayout.HelpBox("Select the parent GameObject in the scene.", MessageType.Warning);
        NGUIParent = EditorGUILayout.ObjectField("Parent: ", NGUIParent, typeof(GameObject), true) as GameObject;
        if (NGUIParent == null) allowCreate = false;
#endif
    }

    private void DrawUGUIParent(ref bool allowCreate)
    {
        EditorGUILayout.HelpBox("Select the parent GameObject in the scene.", MessageType.Warning);
        uGUIParent = EditorGUILayout.ObjectField("Parent: ", uGUIParent, typeof(GameObject), true) as GameObject;
        if (uGUIParent != null && uGUIParent.GetComponent<CanvasRenderer>() == null && uGUIParent.GetComponent<Canvas>() == null)
        {
            EditorGUILayout.HelpBox("Selected the wrong parent. Parent must contain the Canvas or Canvas Renderer.", MessageType.Error);
            allowCreate = false;
        }
    }

    private void InitSteps()
    {
        steps = new List<OnlineMapsWizardDelegate>();
        steps.Add(DrawMapType);
        steps.Add(DrawControls);
        steps.Add(DrawSource);
        steps.AddRange(activeControl.steps);
        steps.Add(DrawMoreFeatures);
        steps.Add(DrawPlugins);
    }

    private void OnEnable()
    {
        activeMapType = OnlineMapsProvider.FindMapType("arcgis");
        providers = OnlineMapsProvider.GetProviders();
        providersTitle = OnlineMapsProvider.GetProvidersTitle();
        providerIndex = activeMapType.provider.index;

#if UNITY_2019_1_OR_NEWER
        bool useSRP = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null;
#else
        bool useSRP = false;
#endif

        if (useSRP)
        {
            string[] assets = AssetDatabase.FindAssets("TilesetPBRShader");
            if (assets.Length > 0) defaultTilesetShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(assets[0]));
            else defaultTilesetShader = Shader.Find("Infinity Code/Online Maps/Tileset Cutout");
            tilesetShader = defaultTilesetShader;

            assets = AssetDatabase.FindAssets("TilesetPBRMarkerShader");
            if (assets.Length > 0) markerShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(assets[0]));
            else markerShader = Shader.Find("Transparent/Diffuse");

            assets = AssetDatabase.FindAssets("TilesetPBRDrawingElement");
            if (assets.Length > 0) drawingShader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(assets[0]));
            else drawingShader = Shader.Find("Infinity Code/Online Maps/Tileset DrawingElement");
        }
        else
        {
            defaultTilesetShader = Shader.Find("Infinity Code/Online Maps/Tileset Cutout");
            tilesetShader = defaultTilesetShader;
            markerShader = Shader.Find("Transparent/Diffuse");
            drawingShader = Shader.Find("Infinity Code/Online Maps/Tileset DrawingElement");
        }
        

        activeCamera = Camera.main;

        CachePlugins();
        CacheControls();
        InitSteps();

        if (useSRP)
        {
            Plugin plugin = plugins.FirstOrDefault(p => p.title == "PBR Bridge") as Plugin;
            if (plugin != null) plugin.enabled = true;
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        bool allowCreate = true;

        if (steps == null || steps.Count == 0) InitSteps();

        foreach (OnlineMapsWizardDelegate s in steps)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            s(ref allowCreate);
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("", GUIStyle.none);

        EditorGUI.BeginDisabledGroup(!allowCreate);
        if (GUILayout.Button("Create", GUILayout.ExpandWidth(false)))
        {
            try
            {
                CreateMap();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
            Close();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    [MenuItem("GameObject/Infinity Code/Online Maps/Map Wizard", false, 0)]
    public static void OpenWindow()
    {
        GetWindow<OnlineMapsWizard>(true, "Map Wizard", true);
    }

    internal class Control
    {
        public List<IPlugin> plugins;
        public OnlineMapsTarget resultType;
        public List<OnlineMapsWizardDelegate> steps;
        public string title;
        public Type type;
        public bool useSprite;

        public Control(string fullName, Type t)
        {
            title = fullName;
            type = t;

            useSprite = t == typeof(OnlineMapsSpriteRendererControl) || t == typeof(OnlineMapsUIImageControl);

            steps = new List<OnlineMapsWizardDelegate>();
            plugins = new List<IPlugin>();
        }
    }

    internal interface IPlugin
    {
        string title { get; }
    }

    internal class Plugin: IPlugin
    {
        public OnlineMapsPluginAttribute attribute;
        public bool enabled;
        public Type type;

        private string _title;

        public Plugin(Type type, OnlineMapsPluginAttribute attribute)
        {
            this.type = type;
            this.attribute = attribute;
            enabled = attribute.enabledByDefault;
            _title = attribute.title;
        }

        public string title { get { return _title; } }
    }

    internal class PluginGroup: IPlugin
    {
        public string title { get { return _title; } }

        
        public int selected = 0;

        public List<Plugin> plugins;
        private string _title;
        private List<string> _titles;
        private string[] _ts;

        public string[] titles
        {
            get
            {
                if (_ts == null)
                {
                    List<string> orderedTitles = _titles.OrderBy(t => t).ToList();
                    orderedTitles.Insert(0, "None");
                    _ts = orderedTitles.ToArray();
                }
                return _ts;
            }
        }

        public PluginGroup(string title)
        {
            _title = title;
            plugins = new List<Plugin>();
            _titles = new List<string>();
        }

        public void Add(Type type, OnlineMapsPluginAttribute p)
        {
            plugins.Add(new Plugin(type, p));
            _titles.Add(p.title);
        }
    }
}