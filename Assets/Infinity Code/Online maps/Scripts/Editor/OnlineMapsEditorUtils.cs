/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public static class OnlineMapsEditorUtils
{
    private static string _assetPath;
    private static Texture2D _helpIcon;
    private static GUIStyle _helpStyle;

    public static string assetPath
    {
        get
        {
            if (_assetPath == null)
            {
                string[] assets = AssetDatabase.FindAssets("OnlineMapsEditorUtils");
                FileInfo info = new FileInfo(AssetDatabase.GUIDToAssetPath(assets[0]));
                _assetPath = info.Directory.Parent.Parent.FullName.Substring(Application.dataPath.Length - 6);
            }
            return _assetPath;
        }
    }

    public static Texture2D helpIcon
    {
        get
        {
            if (_helpIcon == null) _helpIcon = LoadAsset<Texture2D>("Icons\\HelpIcon.png");
            return _helpIcon;
        }
    }

    public static GUIStyle helpStyle
    {
        get
        {
            if (_helpStyle == null)
            {
                _helpStyle = new GUIStyle();
                _helpStyle.margin = new RectOffset(0, 0, 2, 0);
            }
            return _helpStyle;
        }
    }


    public static void CheckMarkerTextureImporter(SerializedProperty property)
    {
        Texture2D texture = property.objectReferenceValue as Texture2D;
        CheckMarkerTextureImporter(texture);
    }

    public static void CheckMarkerTextureImporter(Texture2D texture)
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

        if (needReimport) AssetDatabase.ImportAsset(textureFilename, ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// Returns the current canvas or creates a new one
    /// </summary>
    /// <returns>Instance of Canvas</returns>
    public static Canvas GetCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvasGO.layer = LayerMask.NameToLayer("UI");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        GetEventSystem();

        return canvas;
    }

    /// <summary>
    /// Returns the current event system or creates a new one
    /// </summary>
    /// <returns>Instance of event system</returns>
    public static EventSystem GetEventSystem()
    {
        EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        return eventSystem;
    }

    public static void HelpButton(string help, string url = null)
    {
        if (GUILayout.Button(new GUIContent(helpIcon, help), helpStyle, GUILayout.ExpandWidth(false)) && !string.IsNullOrEmpty(url))
        {
            Process.Start(url);
        }
    }

    public static void ImportPackage(string path, Warning warning = null, string errorMessage = null)
    {
        if (warning != null && !warning.Show()) return;
        if (string.IsNullOrEmpty(assetPath))
        {
            if (!string.IsNullOrEmpty(errorMessage)) Debug.LogError(errorMessage);
            return;
        }

        string filaname = assetPath + "\\" + path;
        if (!File.Exists(filaname))
        {
            if (!string.IsNullOrEmpty(errorMessage)) Debug.LogError(errorMessage);
            return;
        }

        AssetDatabase.ImportPackage(filaname, true);
    }

    public static T LoadAsset<T>(string path, bool throwOnMissed = false) where T : Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            if (throwOnMissed) throw new FileNotFoundException(assetPath);
            return default(T);
        }
        string filename = assetPath + "\\" + path;
        if (!File.Exists(filename))
        {
            if (throwOnMissed) throw new FileNotFoundException(assetPath);
            return default(T);
        }
        return (T)AssetDatabase.LoadAssetAtPath(filename, typeof(T));
    }

    public static void PropertyField(SerializedProperty sp, string help = null, string url = null)
    {
        EditorGUI.BeginChangeCheck();
        bool hasHelp = !string.IsNullOrEmpty(help);
        if (hasHelp) EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(sp);
        if (hasHelp)
        {
            HelpButton(help, url);
            EditorGUILayout.EndHorizontal();
        }
    }

    public static void PropertyField(SerializedProperty sp, GUIContent content, string help = null, string url = null)
    {
        EditorGUI.BeginChangeCheck();
        bool hasHelp = !string.IsNullOrEmpty(help);
        if (hasHelp) EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(sp, content);
        if (hasHelp)
        {
            HelpButton(help, url);
            EditorGUILayout.EndHorizontal();
        }
    }

    public class Warning
    {
        public string title = "Warning";
        public string message;
        public string ok = "OK";
        public string cancel = "Cancel";

        public bool Show()
        {
            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }
    }
}