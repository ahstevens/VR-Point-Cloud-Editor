/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof (OnlineMaps))]
public class OnlineMapsEditor : Editor
{
    private static GUIStyle _warningStyle;
    public static readonly int[] availableSizes = { 256, 512, 1024, 2048, 4096 };

#if UNITY_WEBGL
    private SerializedProperty pUseProxy;
    private SerializedProperty pProxyURL;
#endif

#if !UNITY_WEBGL
    private SerializedProperty pRenderInThread;
#endif

    private OnlineMaps map;
    private OnlineMapsControlBase control;

    private string[] availableSizesStr;
    private List<OnlineMapsSavableItem> savableItems;
    private bool showAdvanced;
    private bool showCreateTexture;
    private bool showCustomProviderTokens;
    private bool showSave;
    private bool showResourcesTokens;
    private bool showTroubleshooting;
    private int textureHeight = 512;
    private int textureWidth = 512;
    private GUIContent updateAvailableContent;
    private GUIContent wizardIconContent;
    private string textureFilename = "OnlineMaps";

    private SerializedProperty pSource;
    private SerializedProperty pMapType;

    private SerializedProperty pLabels;
    private SerializedProperty pCustomProviderURL;
    private SerializedProperty pResourcesPath;
    private SerializedProperty pStreamingAssetsPath;
    private SerializedProperty pTexture;
    private SerializedProperty pRedrawOnPlay;
    private SerializedProperty pCountParentLevels;
    private SerializedProperty pTraffic;
    private SerializedProperty pEmptyColor;
    private SerializedProperty pDefaultTileTexture;
    private SerializedProperty pTooltipTexture;
    private SerializedProperty pShowMarkerTooltip;
    private SerializedProperty pLanguage;
    private SerializedProperty pUseSoftwareJPEGDecoder;
    private SerializedProperty pNotInteractUnderGUI;
    private SerializedProperty pStopPlayingWhenScriptsCompile;
    private SerializedProperty pWidth;
    private SerializedProperty pHeight;
    private SerializedProperty pActiveTypeSettings;

    private GUIContent cWidth;
    private GUIContent cHeight;
    private GUIContent cUseSoftwareJPEGDecoder;

    private OnlineMapsProvider[] providers;
    private string[] providersTitle;
    private OnlineMapsProvider.MapType mapType;
    private int providerIndex;
    private GUIContent cTooltipTexture;

    private SerializedProperty pTrafficProviderID;
    private OnlineMapsTrafficProvider[] trafficProviders;
    private GUIContent[] cTrafficProviders;
    private int trafficProviderIndex;
    private SerializedProperty pCustomTrafficProviderURL;
    private SerializedProperty pOSMServer;
    private SerializedProperty pDragMarkerHoldingCTRL;

    public static GUIStyle warningStyle
    {
        get
        {
            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = {textColor = Color.red},
                    fontStyle = FontStyle.Bold
                };
            }
            
            return _warningStyle;
        }
    }

    public static void AddCompilerDirective(string directive)
    {
        BuildTargetGroup[] targetGroups = (BuildTargetGroup[]) Enum.GetValues(typeof(BuildTargetGroup));
        foreach (BuildTargetGroup g in targetGroups)
        {
            if (g == BuildTargetGroup.Unknown) continue;
            int ig = (int) g;
            if (ig == 2 || 
                ig == 5 || 
                ig == 6 || 
                ig >= 15 && ig <= 18 ||
                ig == 20 || 
                ig >= 22 && ig <= 24 ||
                ig == 26) continue;

            string currentDefinitions = PlayerSettings.GetScriptingDefineSymbolsForGroup(g);
            List<string> directives = currentDefinitions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            if (!directives.Contains(directive))
            {
                directives.Add(directive);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(g, String.Join(";", directives.ToArray()));
            }
        }
    }

    private void CacheSerializedProperties()
    {
        pSource = serializedObject.FindProperty("source");
        pMapType = serializedObject.FindProperty("mapType");

        pCustomProviderURL = serializedObject.FindProperty("customProviderURL");
        pCustomTrafficProviderURL = serializedObject.FindProperty("customTrafficProviderURL");
        pResourcesPath = serializedObject.FindProperty("resourcesPath");
        pStreamingAssetsPath = serializedObject.FindProperty("streamingAssetsPath");

        pLabels = serializedObject.FindProperty("labels");
        pLanguage = serializedObject.FindProperty("language");
        pTexture = serializedObject.FindProperty("texture");

        pWidth = serializedObject.FindProperty("width");
        pHeight = serializedObject.FindProperty("height");

        pRedrawOnPlay = serializedObject.FindProperty("redrawOnPlay");
        pCountParentLevels = serializedObject.FindProperty("countParentLevels");
        pTraffic = serializedObject.FindProperty("traffic");
        pTrafficProviderID = serializedObject.FindProperty("trafficProviderID");
        pEmptyColor = serializedObject.FindProperty("emptyColor");
        pDefaultTileTexture = serializedObject.FindProperty("defaultTileTexture");
        pTooltipTexture = serializedObject.FindProperty("tooltipBackgroundTexture");
        pShowMarkerTooltip = serializedObject.FindProperty("showMarkerTooltip");
        pUseSoftwareJPEGDecoder = serializedObject.FindProperty("useSoftwareJPEGDecoder");
        pActiveTypeSettings = serializedObject.FindProperty("_activeTypeSettings");
        pDragMarkerHoldingCTRL = serializedObject.FindProperty("dragMarkerHoldingCTRL");

#if !UNITY_WEBGL
        pRenderInThread = serializedObject.FindProperty("renderInThread");
#else
        pUseProxy = serializedObject.FindProperty("useProxy");
        pProxyURL = serializedObject.FindProperty("proxyURL");
#endif
        pNotInteractUnderGUI = serializedObject.FindProperty("notInteractUnderGUI");
        pStopPlayingWhenScriptsCompile = serializedObject.FindProperty("stopPlayingWhenScriptsCompile");
        pOSMServer = serializedObject.FindProperty("osmServer");

        cWidth = new GUIContent("Width (pixels)", "Width of the map. It works as a resolution.\nImportant: the map must have a side size of N * 256.");
        cHeight = new GUIContent("Height (pixels)", "Height of the map. It works as a resolution.\nImportant: the map must have a side size of N * 256.");
        cUseSoftwareJPEGDecoder = new GUIContent("Software JPEG Decoder");
        cTooltipTexture = new GUIContent("Tooltip Background");
    }

    private void CheckAPITextureImporter(SerializedProperty property)
    {
        Texture2D texture = property.objectReferenceValue as Texture2D;
        CheckAPITextureImporter(texture);
    }

    private static void CheckAPITextureImporter(Texture2D texture)
    {
        if (texture == null) return;

        string textureFilename = AssetDatabase.GetAssetPath(texture.GetInstanceID());
        TextureImporter textureImporter = AssetImporter.GetAtPath(textureFilename) as TextureImporter;
        if (textureImporter == null) return;

        bool needReimport = false;
        if (textureImporter.mipmapEnabled)
        {
            textureImporter.mipmapEnabled = false;
            needReimport = true;
        }
        if (!textureImporter.isReadable)
        {
            textureImporter.isReadable = true;
            needReimport = true;
        }
        if (textureImporter.textureCompression != TextureImporterCompression.Uncompressed)
        {
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            needReimport = true;
        }
        if (textureImporter.maxTextureSize < 256)
        {
            textureImporter.maxTextureSize = 256;
            needReimport = true;
        }

        if (needReimport) AssetDatabase.ImportAsset(textureFilename, ImportAssetOptions.ForceUpdate);
    }

    private void CheckNullControl()
    {
        OnlineMapsControlBase[] controls = map.GetComponents<OnlineMapsControlBase>();
        if (controls != null && controls.Length != 0) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.HelpBox("Problem detected:\nCan not find OnlineMaps Control component.", MessageType.Error);
        if (GUILayout.Button("Add Control"))
        {
            GenericMenu menu = new GenericMenu();

            Type[] types = map.GetType().Assembly.GetTypes();
            foreach (Type t in types)
            {
                if (t.IsSubclassOf(typeof (OnlineMapsControlBase)) && !t.IsAbstract)
                {
                    string fullName = t.FullName;
                    if (fullName.StartsWith("OnlineMaps")) fullName = fullName.Substring(10);

                    int controlIndex = fullName.IndexOf("Control");
                    fullName = fullName.Insert(controlIndex, " ");

                    int textureIndex = fullName.IndexOf("Texture");
                    if (textureIndex > 0) fullName = fullName.Insert(textureIndex, " ");

                    menu.AddItem(new GUIContent(fullName), false, data =>
                    {
                        Type ct = data as Type;
                        map.gameObject.AddComponent(ct);
                        Repaint();
                    }, t);
                }
            }

            menu.ShowAsContext();
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateTexture()
    {
        string texturePath = string.Format("Assets/{0}.png", textureFilename);
        
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        File.WriteAllBytes(texturePath, texture.EncodeToPNG());
        AssetDatabase.Refresh();
        TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = true;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.maxTextureSize = Mathf.Max(textureWidth, textureHeight);

            OnlineMapsControlBase control = map.GetComponent<OnlineMapsControlBase>();
            if (control is OnlineMapsUIImageControl || control is OnlineMapsSpriteRendererControl)
            {
                textureImporter.spriteImportMode = SpriteImportMode.Single;
                textureImporter.npotScale = TextureImporterNPOTScale.None;
            }

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
            Texture2D newTexture = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
            pTexture.objectReferenceValue = newTexture;

            if (control is OnlineMapsSpriteRendererControl)
            {
                SpriteRenderer spriteRenderer = map.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Sprite)) as Sprite;
            }
            else if (control is OnlineMapsUIImageControl)
            {
                Image img = map.GetComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Sprite)) as Sprite;
            }
            else if (control is OnlineMapsUIRawImageControl)
            {
                RawImage img = map.GetComponent<RawImage>();
                img.texture = newTexture;
            }
            else if (control is OnlineMapsTextureControl)
            {
                Renderer renderer = map.GetComponent<Renderer>();
                renderer.sharedMaterial.mainTexture = texture;
            }
            else if (control is OnlineMapsNGUITextureControl)
            {
#if NGUI
                UITexture uiTexture = map.GetComponent<UITexture>();
                uiTexture.mainTexture = newTexture;
#endif
            }
        }

        OnlineMapsUtils.Destroy(texture);
        EditorUtility.UnloadUnusedAssetsImmediate();
    }

    private void DrawAdvancedGUI()
    {
        float oldWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 160;

        if (control != null && control.resultIsTexture)
        {
            OnlineMapsEditorUtils.PropertyField(pRedrawOnPlay, "Redraw the map immediately after the start of the scene");
        }

        EditorGUI.BeginChangeCheck();
        OnlineMapsEditorUtils.PropertyField(pCountParentLevels, "Tiles for the specified number of parent levels will be loaded");
        if (EditorGUI.EndChangeCheck())
        {
            pCountParentLevels.intValue = Mathf.Clamp(pCountParentLevels.intValue, 0, 20);
        }

        OnlineMapsEditorUtils.PropertyField(pTraffic, "Display traffic jams");
        if (pTraffic.boolValue)
        {
            EditorGUI.BeginChangeCheck();
            trafficProviderIndex = EditorGUILayout.Popup(new GUIContent("Traffic Provider"), trafficProviderIndex, cTrafficProviders);
            if (EditorGUI.EndChangeCheck()) pTrafficProviderID.stringValue = trafficProviders[trafficProviderIndex].id;
            if (trafficProviders[trafficProviderIndex].isCustom) EditorGUILayout.PropertyField(pCustomTrafficProviderURL, new GUIContent("URL"));
        }

        OnlineMapsEditorUtils.PropertyField(pEmptyColor, "The color that will be displayed until the tile is loaded.\nImportant: if Default Tile Texture is specified, this value will be ignored.");

        EditorGUI.BeginChangeCheck();
        OnlineMapsEditorUtils.PropertyField(pDefaultTileTexture, "The texture that will be displayed until the tile is loaded");
        if (EditorGUI.EndChangeCheck()) CheckAPITextureImporter(pDefaultTileTexture);

        OnlineMapsEditorUtils.PropertyField(pTooltipTexture, cTooltipTexture, "Tooltip background texture");
        OnlineMapsEditorUtils.PropertyField(pShowMarkerTooltip, "Tooltip display rule");
        OnlineMapsEditorUtils.PropertyField(pDragMarkerHoldingCTRL, "Hold CTRL and press on the marker to drag the item.");

        EditorGUIUtility.labelWidth = oldWidth;
    }

    private void DrawCacheGUI(ref bool dirty)
    {
        if (pSource.enumValueIndex == (int)OnlineMapsSource.Resources || pSource.enumValueIndex == (int)OnlineMapsSource.StreamingAssets) return;

        string targetSource = "Resources";
        if (pSource.enumValueIndex == (int) OnlineMapsSource.StreamingAssetsAndOnline) targetSource = "Streaming Assets";

        if (!GUILayout.Button("Cache tiles to " + targetSource)) return;

        OnlineMapsTileSetControl tsControl = control as OnlineMapsTileSetControl;
        if (tsControl != null && tsControl.compressTextures)
        {
            if (EditorUtility.DisplayDialog("Error", "To cache tiles, do the following:\n1.Enter to edit mode (stop the game).\n2.Tileset / Materials & Shaders / Compress Textures - OFF.\n3.Run the game and press the button again.\nAfter caching, you can enable texture compression again.", "Stop Game", "Cancel"))
            {
                EditorApplication.isPlaying = false;
            }
            return;
        }

        lock (OnlineMapsTile.lockTiles)
        {
            string resPath = "Assets/Resources";
            if (pSource.enumValueIndex == (int) OnlineMapsSource.StreamingAssetsAndOnline) resPath = Application.streamingAssetsPath;

            foreach (OnlineMapsTile tile in map.tileManager.tiles)
            {
                if (tile.status != OnlineMapsTileStatus.loaded) continue;

                string tilePath = Path.Combine(resPath, tile.resourcesPath + ".png");
                FileInfo info = new FileInfo(tilePath);
                if (!info.Directory.Exists) info.Directory.Create();

                if (!control.resultIsTexture)
                {
                    if (tile.texture != null) File.WriteAllBytes(tilePath, tile.texture.EncodeToPNG());
                }
                else
                {
                    OnlineMapsRasterTile rasterTile = tile as OnlineMapsRasterTile;
                    if (rasterTile != null && rasterTile.colors != null)
                    {
                        Texture2D texture = new Texture2D(OnlineMapsUtils.tileSize, OnlineMapsUtils.tileSize, TextureFormat.ARGB32, false);
                        texture.SetPixels32(rasterTile.colors);
                        texture.Apply();
                        File.WriteAllBytes(tilePath, texture.EncodeToPNG());
                    }
                }
            }
        }

        EditorPrefs.SetBool("OnlineMapsRefreshAssets", true);

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Cache complete", "Stop playback and select 'Source - " + targetSource + " And Online'.", "OK");

        dirty = true;
    }

    private void DrawCreateTextureGUI(ref bool dirty)
    {
        if (availableSizesStr == null) availableSizesStr = availableSizes.Select(s => s.ToString()).ToArray();

        textureFilename = EditorGUILayout.TextField("Filename", textureFilename);

        textureWidth = EditorGUILayout.IntPopup("Width", textureWidth, availableSizesStr, availableSizes);
        textureHeight = EditorGUILayout.IntPopup("Height", textureHeight, availableSizesStr, availableSizes);

        if (GUILayout.Button("Create"))
        {
            CreateTexture();
            dirty = true;
        }

        EditorGUILayout.Space();
    }

    private void DrawGeneralGUI(ref bool dirty)
    {
        DrawSourceGUI(ref dirty);
        DrawLocationGUI(ref dirty);
        DrawTargetGUI();

        if (OnlineMaps.isPlaying)
        {
            if (GUILayout.Button("Redraw"))
            {
                map.Redraw();
            }

            DrawCacheGUI(ref dirty);

            if (!showSave) 
            {
                if (GUILayout.Button("Save state"))
                {
                    showSave = true;
                    dirty = true;
                }
            }
            else
            {
                DrawSaveGUI(ref dirty);
            }
        }
    }

    private void DrawLabelsGUI()
    {
        if (mapType.isCustom) return;

        bool showLanguage;
        if (mapType.hasLabels)
        {
            OnlineMapsEditorUtils.PropertyField(pLabels, "Show labels?");
            showLanguage = pLabels.boolValue;
        }
        else
        {
            showLanguage = mapType.labelsEnabled;
            GUILayout.Label("Labels " + (showLanguage ? "enabled" : "disabled"));
        }
        if (showLanguage && mapType.hasLanguage)
        {
            OnlineMapsEditorUtils.PropertyField(pLanguage, mapType.provider.twoLetterLanguage ? "Use two-letter code such as: en" : "Use three-letter code such as: eng");
        }
    }

    private void DrawLocationGUI(ref bool dirty)
    {
        double px, py;
        map.GetPosition(out px, out py);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        EditorGUI.BeginChangeCheck();
        py = EditorGUILayout.DoubleField(new GUIContent("Latitude", "Latitude of the center point of the map"), py);
        px = EditorGUILayout.DoubleField(new GUIContent("Longitude", "Longitude of the center point of the map"), px);

        if (EditorGUI.EndChangeCheck())
        {
            dirty = true;
            map.SetPosition(px, py);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(16));
        GUILayout.Space(10);
        OnlineMapsEditorUtils.HelpButton("Coordinates of the center point of the map");
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        string tooltip = "Current zoom of the map";
        float newZoom = EditorGUILayout.Slider(new GUIContent("Zoom", tooltip), map.floatZoom, OnlineMaps.MINZOOM, OnlineMaps.MAXZOOM_EXT);

        if (EditorGUI.EndChangeCheck())
        {
            map.floatZoom = newZoom;
            dirty = true;
        }

        OnlineMapsEditorUtils.HelpButton(tooltip);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawProviderGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        string helpMessage = "Tile provider.\nImportant: all tile presets are for testing purpose only. Before using the tile provider, make sure that it suits you by the terms of use and price.";
        providerIndex = EditorGUILayout.Popup(new GUIContent("Provider", helpMessage), providerIndex, providersTitle);
        if (EditorGUI.EndChangeCheck())
        {
            mapType = providers[providerIndex].types[0];
            pMapType.stringValue = mapType.ToString();
            pActiveTypeSettings.stringValue = "";
        }

        OnlineMapsEditorUtils.HelpButton(helpMessage);

        EditorGUILayout.EndHorizontal();

        if (mapType.useHTTP)
        {
            EditorGUILayout.HelpBox(mapType.provider.title + " - " + mapType.title + " uses HTTP, which can cause problems in iOS9+.", MessageType.Warning);
        }
        else if (mapType.isCustom)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(pCustomProviderURL);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button(wizardIconContent, GUILayout.ExpandWidth(false))) OnlineMapsCustomURLWizard.OpenWindow();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            showCustomProviderTokens = Foldout(showCustomProviderTokens, "Available tokens");
            if (showCustomProviderTokens)
            {
                GUILayout.Label("{zoom} or {z} - Tile zoom");
                GUILayout.Label("{x} - Tile X");
                GUILayout.Label("{y} - Tile Y");
                GUILayout.Label("{quad} - Tile Quad");
                GUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawProviderExtraFields(ref bool dirty, OnlineMapsProvider.IExtraField[] extraFields)
    {
        if (extraFields == null) return;

        foreach (OnlineMapsProvider.IExtraField field in extraFields)
        {
            if (field is OnlineMapsProvider.ToggleExtraGroup) DrawProviderToggleExtraGroup(field as OnlineMapsProvider.ToggleExtraGroup, ref dirty);
            else if (field is OnlineMapsProvider.ExtraField) DrawProviderExtraField(field as OnlineMapsProvider.ExtraField, ref dirty);
        }
    }

    private void DrawProviderExtraField(OnlineMapsProvider.ExtraField field, ref bool dirty)
    {
        EditorGUI.BeginChangeCheck();
        field.value = EditorGUILayout.TextField(field.title, field.value);
        if (EditorGUI.EndChangeCheck()) dirty = true;
    }

    private void DrawProviderToggleExtraGroup(OnlineMapsProvider.ToggleExtraGroup group, ref bool dirty)
    {
        group.value = EditorGUILayout.Toggle(group.title, group.value);
        EditorGUI.BeginDisabledGroup(group.value);

        if (group.fields != null)
        {
            foreach (OnlineMapsProvider.IExtraField field in group.fields)
            {
                if (field is OnlineMapsProvider.ToggleExtraGroup) DrawProviderToggleExtraGroup(field as OnlineMapsProvider.ToggleExtraGroup, ref dirty);
                else if (field is OnlineMapsProvider.ExtraField) DrawProviderExtraField(field as OnlineMapsProvider.ExtraField, ref dirty);
            }
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawSaveGUI(ref bool dirty)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.LabelField("Save state:");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("All", GUILayout.ExpandWidth(false))) foreach (OnlineMapsSavableItem item in savableItems) item.enabled = true;
        if (GUILayout.Button("None", GUILayout.ExpandWidth(false))) foreach (OnlineMapsSavableItem item in savableItems) item.enabled = false;
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        foreach (OnlineMapsSavableItem item in savableItems)
        {
            item.enabled = EditorGUILayout.Toggle(item.label, item.enabled);
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save state"))
        {
            try
            {
                OnlineMapsJSONObject obj = new OnlineMapsJSONObject();
                foreach (OnlineMapsSavableItem item in savableItems)
                {
                    if (!item.enabled) continue;
                    if (item.jsonCallback != null) obj.Add(item.name, item.jsonCallback());
                    if (item.invokeCallback != null) item.invokeCallback();
                }
                OnlineMapsPrefs.Save(obj.ToString());
            }
            catch
            {
                // ignored
            }


            showSave = false;
            dirty = true;
        }

        if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
        {
            showSave = false;
            dirty = true;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSourceGUI(ref bool dirty)
    {
        EditorGUI.BeginDisabledGroup(OnlineMaps.isPlaying);

        OnlineMapsEditorUtils.PropertyField(pSource, "Source of tiles");

#if UNITY_WEBGL
        if (pSource.enumValueIndex != (int)OnlineMapsSource.Resources && pSource.enumValueIndex != (int)OnlineMapsSource.StreamingAssets)
        {
            EditorGUILayout.PropertyField(pUseProxy, new GUIContent("Use Proxy"));
            EditorGUI.BeginDisabledGroup(!pUseProxy.boolValue);
            
            EditorGUILayout.PropertyField(pProxyURL, new GUIContent("Proxy"));
            EditorGUI.EndDisabledGroup();
        }
#endif

        if (pSource.enumValueIndex != (int)OnlineMapsSource.Online)
        {
            if (GUILayout.Button("Fix Import Settings for Tiles")) FixImportSettings();
            if (GUILayout.Button("Import from GMapCatcher")) ImportFromGMapCatcher();

            if (pSource.enumValueIndex == (int) OnlineMapsSource.Resources || pSource.enumValueIndex == (int) OnlineMapsSource.ResourcesAndOnline)
            {
                OnlineMapsEditorUtils.PropertyField(pResourcesPath, "The path pattern inside Resources folder");
            }
            else
            {
                OnlineMapsEditorUtils.PropertyField(pStreamingAssetsPath, "The path pattern inside Streaming Assets folder");
#if UNITY_WEBGL
                EditorGUILayout.HelpBox("Streaming Assets folder is not available for WebGL!", MessageType.Warning);
#endif
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            showResourcesTokens = Foldout(showResourcesTokens, "Available Tokens");
            if (showResourcesTokens)
            {
                GUILayout.Label("{zoom} - Tile zoom");
                GUILayout.Label("{x} - Tile x");
                GUILayout.Label("{y} - Tile y");
                GUILayout.Label("{quad} - Tile quad key");
                GUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUI.EndDisabledGroup();

        DrawMapTypes(ref dirty);
    }

    private void DrawMapTypes(ref bool dirty)
    {
        if (control != null && !control.useRasterTiles) return;
        if (pSource.enumValueIndex == (int) OnlineMapsSource.Resources) return;
        if (pSource.enumValueIndex == (int)OnlineMapsSource.StreamingAssets) return;

        DrawProviderGUI();

        if (mapType.provider.types.Length > 1)
        {
            GUIContent[] availableTypes = mapType.provider.types.Select(t => new GUIContent(t.title)).ToArray();
            int index = mapType.index;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string tooltip = "Type (style) of the map";
            index = EditorGUILayout.Popup(new GUIContent("Type", tooltip), index, availableTypes);
            if (EditorGUI.EndChangeCheck())
            {
                mapType = mapType.provider.types[index];
                pMapType.stringValue = mapType.ToString();
            }
            OnlineMapsEditorUtils.HelpButton(tooltip);
            EditorGUILayout.EndHorizontal();
        }

        DrawProviderExtraFields(ref dirty, mapType.provider.extraFields);
        DrawProviderExtraFields(ref dirty, mapType.extraFields);
        if (mapType.fullID == "google.satellite")
        {
            if (GUILayout.Button("Detect the latest version of tiles"))
            {
                WebClient client = new WebClient();
                string response = client.DownloadString("http://maps.googleapis.com/maps/api/js");
                Match match = Regex.Match(response, @"kh\?v=(\d+)");
                if (match.Success)
                {
                    OnlineMapsProvider.ExtraField version = mapType.extraFields.FirstOrDefault(f =>
                    {
                        OnlineMapsProvider.ExtraField ef = f as OnlineMapsProvider.ExtraField;
                        if (ef == null) return false;
                        if (ef.token != "version") return false;
                        return true;
                    }) as OnlineMapsProvider.ExtraField;
                    if (version != null)
                    {
                        version.value = match.Groups[1].Value;
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }
        DrawLabelsGUI();
    }

    private void DrawTargetGUI()
    {
        if (control == null) return;

        EditorGUI.BeginDisabledGroup(OnlineMaps.isPlaying);

        if (control.resultType == OnlineMapsTarget.texture) DrawTexturePropsGUI();
        else if (control.resultType == OnlineMapsTarget.tileset) DrawTilesetPropsGUI();

        EditorGUI.EndDisabledGroup();
    }

    private void DrawTexturePropsGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        Object oldValue = pTexture.objectReferenceValue;
        EditorGUILayout.PropertyField(pTexture);
        bool changed = EditorGUI.EndChangeCheck();
        OnlineMapsEditorUtils.HelpButton("The texture where the map will be drawn.\nImportant: must have Read / Write Enabled - ON.");
        EditorGUILayout.EndHorizontal();
        if (!changed) return;

        Texture2D texture = pTexture.objectReferenceValue as Texture2D;
        if (texture != null && (!Mathf.IsPowerOfTwo(texture.width) || !Mathf.IsPowerOfTwo(texture.height)))
        {
            EditorUtility.DisplayDialog("Error", "Texture width and height must be power of two!!!", "OK");
            pTexture.objectReferenceValue = oldValue;
        }
        else CheckAPITextureImporter(texture);

        texture = pTexture.objectReferenceValue as Texture2D;
        if (texture != null)
        {
            map.width = texture.width;
            map.height = texture.height;
        }
    }

    private void DrawTilesetPropsGUI()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.PropertyField(pWidth, cWidth);
        EditorGUILayout.PropertyField(pHeight, cHeight);
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(16));
        GUILayout.Space(10);
        OnlineMapsEditorUtils.HelpButton("Width / height of the map. It works as a resolution.\nImportant: the map must have a side size of N * 256.");
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        int dts = OnlineMapsUtils.tileSize * 2;

        if (pWidth.intValue % 256 != 0)
        {
            EditorGUILayout.HelpBox("Width is not equal to 256 * N, the map will not work correctly.", MessageType.Error);
            if (GUILayout.Button("Fix Width")) pWidth.intValue = Mathf.NextPowerOfTwo(pWidth.intValue);
        }
        else if (pHeight.intValue % 256 != 0)
        {
            EditorGUILayout.HelpBox("Height is not equal to 256 * N, the map will not work correctly.", MessageType.Error);
            if (GUILayout.Button("Fix Height")) pHeight.intValue = Mathf.NextPowerOfTwo(pHeight.intValue);
        }

        if (pWidth.intValue <= 128) pWidth.intValue = dts;
        if (pHeight.intValue <= 128) pHeight.intValue = dts;
    }

    private void DrawToolbarGUI()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (OnlineMapsUpdater.hasNewVersion && updateAvailableContent != null)
        {
            Color defBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1, 0.5f, 0.5f);
            if (GUILayout.Button(updateAvailableContent, EditorStyles.toolbarButton))
            {
                OnlineMapsUpdater.OpenWindow();
            }
            GUI.backgroundColor = defBackgroundColor;
        }
        else GUILayout.Label("");

        if (GUILayout.Button("Help", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Documentation"), false, OnViewDocs);
            menu.AddItem(new GUIContent("API Reference"), false, OnViewAPI);
            menu.AddItem(new GUIContent("Atlas of Examples"), false, OnViewAtlas);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Product Page"), false, OnProductPage);
            menu.AddItem(new GUIContent("Forum"), false, OnViewForum);
            menu.AddItem(new GUIContent("Check Updates"), false, OnCheckUpdates);
            menu.AddItem(new GUIContent("Support"), false, OnSendMail);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Rate and Review"), false, OnlineMapsWelcome.RateAndReview);
            menu.AddItem(new GUIContent("About"), false, OnAbout);
            menu.ShowAsContext();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawTroubleshootingGUI(ref bool dirty)
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        float oldWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 220;
        GUILayout.Label("Use this props only if you have a problem!!!", warningStyle);
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();

        OnlineMapsEditorUtils.PropertyField(pUseSoftwareJPEGDecoder, cUseSoftwareJPEGDecoder, "If you have problems decoding JPEG images, use software decoder.\nKeep in mind that this greatly affects performance.");

#if !UNITY_WEBGL
        if (control != null && control.resultIsTexture) OnlineMapsEditorUtils.PropertyField(pRenderInThread, "If you have any problems with multithreading, disable this field.");
#endif

        OnlineMapsEditorUtils.PropertyField(pNotInteractUnderGUI, "Should Online Maps ignore clicks if an IMGUI or uGUI element is under the cursor?");
        OnlineMapsEditorUtils.PropertyField(pStopPlayingWhenScriptsCompile, "Should Online Maps stop playing when recompiling scripts?");
        EditorGUI.BeginChangeCheck();
        OnlineMapsEditorUtils.PropertyField(pOSMServer, new GUIContent("Overpass Server"));
        if (EditorGUI.EndChangeCheck() && EditorApplication.isPlaying) OnlineMapsOSMAPIQuery.InitOSMServer((OnlineMapsOSMOverpassServer)pOSMServer.enumValueIndex);

        EditorGUIUtility.labelWidth = oldWidth;

        if (EditorGUI.EndChangeCheck()) dirty = true;
    }

    private void FixImportSettings()
    {
        string path;
        string specialFolderName;

        if (pSource.enumValueIndex == (int) OnlineMapsSource.Resources || pSource.enumValueIndex == (int) OnlineMapsSource.ResourcesAndOnline)
        {
            path = pResourcesPath.stringValue;
            specialFolderName = "Resources";
        }
        else
        {
            path = pStreamingAssetsPath.stringValue;
            specialFolderName = "StreamingAssets";
        }

        int tokenIndex = path.IndexOf("{");

        string specialFolder = Path.Combine(Application.dataPath, specialFolderName);

        if (tokenIndex != -1)
        {
            if (tokenIndex > 1)
            {
                string folder = path.Substring(0, tokenIndex - 1);
                specialFolder = Path.Combine(specialFolder, folder);
            }
        }
        else specialFolder = Path.Combine(specialFolder, "OnlineMapsTiles");

        if (!Directory.Exists(specialFolder)) return;

        string[] tiles = Directory.GetFiles(specialFolder, "*.png", SearchOption.AllDirectories);
        float count = tiles.Length;
        for (int i = 0; i < tiles.Length; i++)
        {
            string shortPath = "Assets/" + tiles[i].Substring(Application.dataPath.Length + 1);
            FixTileImporter(shortPath, i / count);
        }

        EditorUtility.ClearProgressBar();
    }

    private static void FixTileImporter(string shortPath, float progress)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(shortPath) as TextureImporter;
        EditorUtility.DisplayProgressBar("Update import settings for tiles", "Please wait, this may take several minutes.", progress);
        if (textureImporter != null)
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = true;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.maxTextureSize = 256;
            AssetDatabase.ImportAsset(shortPath, ImportAssetOptions.ForceSynchronousImport);
        }
    }

    public static bool Foldout(bool value, string text)
    {
        return GUILayout.Toggle(value, text, EditorStyles.foldout);
    }

    private void ImportFromGMapCatcher()
    {
        string folder = EditorUtility.OpenFolderPanel("Select GMapCatcher tiles folder", string.Empty, "");
        if (string.IsNullOrEmpty(folder)) return;

        string[] files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
        if (files.Length == 0) return;

        string specialFolderName;

        if (pSource.enumValueIndex == (int)OnlineMapsSource.Resources || pSource.enumValueIndex == (int)OnlineMapsSource.ResourcesAndOnline)
        {
            specialFolderName = "Resources";
        }
        else
        {
            specialFolderName = "StreamingAssets";
        }

        string specialPath = "Assets/" + specialFolderName + "/OnlineMapsTiles";

        bool needAsk = true;
        bool overwrite = false;
        foreach (string file in files)
        {
            if (!ImportTileFromGMapCatcher(file, folder, specialPath, ref overwrite, ref needAsk)) break;
        }

        AssetDatabase.Refresh();
    }

    private static bool ImportTileFromGMapCatcher(string file, string folder, string resPath, ref bool overwrite, ref bool needAsk)
    {
        string shortPath = file.Substring(folder.Length + 1);
        shortPath = shortPath.Replace('\\', '/');
        string[] shortArr = shortPath.Split('/');
        int zoom = 17 - int.Parse(shortArr[0]);
        int x = int.Parse(shortArr[1]) * 1024 + int.Parse(shortArr[2]);
        int y = int.Parse(shortArr[3]) * 1024 + int.Parse(shortArr[4].Substring(0, shortArr[4].Length - 4));
        string dir = Path.Combine(resPath, string.Format("{0}/{1}", zoom, x));
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string destFileName = Path.Combine(dir, y + ".png");
        if (File.Exists(destFileName))
        {
            if (needAsk)
            {
                needAsk = false;
                int result = EditorUtility.DisplayDialogComplex("File already exists", "File already exists. Overwrite?", "Overwrite", "Skip", "Cancel");
                if (result == 0) overwrite = true;
                else if (result == 1)
                {
                    overwrite = false;
                    return true;
                }
                else return false;
            }

            if (!overwrite) return true;
        }
        File.Copy(file, destFileName, true);
        return true;
    }

    private void OnAbout()
    {
        OnlineMapsAboutWindow.OpenWindow();
    }

    private void OnCheckUpdates()
    {
        OnlineMapsUpdater.OpenWindow();
    }

    private void OnEnable()
    {
        try
        {
            CacheSerializedProperties();
            map = (OnlineMaps)target;
            control = map.GetComponent<OnlineMapsControlBase>();

            savableItems = map.GetComponents<IOnlineMapsSavableComponent>().SelectMany(c => c.GetSavableItems()).OrderByDescending(s => s.priority).ThenBy(s => s.label).ToList();

            providers = OnlineMapsProvider.GetProviders();
            providersTitle = providers.Select(p => p.title).ToArray();

            trafficProviders = OnlineMapsTrafficProvider.GetProviders();
            cTrafficProviders = trafficProviders.Select(p => new GUIContent(p.title)).ToArray();
            trafficProviderIndex = 0;
            for (int i = 0; i < trafficProviders.Length; i++)
            {
                if (trafficProviders[i].id == pTrafficProviderID.stringValue)
                {
                    trafficProviderIndex = i;
                    break;
                }
            }

            if (pTooltipTexture.objectReferenceValue == null) pTooltipTexture.objectReferenceValue = OnlineMapsEditorUtils.LoadAsset<Texture2D>("Textures\\Tooltip.psd");

            updateAvailableContent = new GUIContent("Update Available", OnlineMapsEditorUtils.LoadAsset<Texture2D>("Icons\\update_available.png"), "Update Available");
            wizardIconContent = new GUIContent(OnlineMapsEditorUtils.LoadAsset<Texture2D>("Icons\\WizardIcon.png"), "Wizard");

            OnlineMapsUpdater.CheckNewVersionAvailable();

            mapType = OnlineMapsProvider.FindMapType(pMapType.stringValue);
            providerIndex = mapType.provider.index;

            serializedObject.ApplyModifiedProperties();

            map.floatZoom = map.CheckMapSize(map.floatZoom);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + "\n" + e.StackTrace);
            //throw;
        }
        
    }

    public override void OnInspectorGUI()
    {
        DrawToolbarGUI();

        serializedObject.Update();

        bool dirty = false;

        try
        {
            DrawGeneralGUI(ref dirty);

            if (control != null && control.resultIsTexture)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                showCreateTexture = Foldout(showCreateTexture, "Create texture");
                if (showCreateTexture) DrawCreateTextureGUI(ref dirty);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            showAdvanced = Foldout(showAdvanced, "Advanced");
            if (showAdvanced) DrawAdvancedGUI();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            showTroubleshooting = Foldout(showTroubleshooting, "Troubleshooting");
            if (showTroubleshooting) DrawTroubleshootingGUI(ref dirty);
            EditorGUILayout.EndVertical();

            CheckNullControl();

            serializedObject.ApplyModifiedProperties();

            if (dirty)
            {
                EditorUtility.SetDirty(map);
                if (!OnlineMaps.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
                else map.Redraw();
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message + "\n" + e.StackTrace);
            //throw;
        }
        
    }

    private void OnProductPage()
    {
        Process.Start("https://infinity-code.com/en/products/online-maps");
    }

    private void OnSendMail()
    {
        Process.Start("mailto:support@infinity-code.com?subject=Online maps");
    }

    private void OnViewAPI()
    {
        Process.Start("https://infinity-code.com/en/docs/api/online-maps");
    }

    private void OnViewAtlas()
    {
        Process.Start("https://infinity-code.com/atlas/online-maps");
    }

    private void OnViewDocs()
    {
        Process.Start("https://infinity-code.com/en/docs/online-maps");
    }

    private void OnViewForum()
    {
        Process.Start("https://forum.infinity-code.com");
    }
}