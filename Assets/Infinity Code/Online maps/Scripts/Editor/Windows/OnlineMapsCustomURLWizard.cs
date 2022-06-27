/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class OnlineMapsCustomURLWizard : EditorWindow
{
    private string url;
    private string newUrl;
    private bool hasError = true;
    private Vector2 scrollPosition;

    private void OnEnable()
    {
        ModifyURL();
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(EditorStyles.textArea);
        style.wordWrap = true;

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.HelpBox("Tool that helps you to find and replace the value of zoom, x, y in the url of tile.\nSupports: Google Maps, Mapbox.", MessageType.Info);

        EditorGUILayout.LabelField("Original URL");
        EditorGUI.BeginChangeCheck();
        url = EditorGUILayout.TextArea(url, style, GUILayout.Height(100));
        if (EditorGUI.EndChangeCheck())
        {
            ModifyURL();
        }
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("New URL");
        EditorGUILayout.TextArea(hasError? "Can not identify the type of map": newUrl, style, GUILayout.Height(100));

        EditorGUILayout.EndScrollView();

        EditorGUI.BeginDisabledGroup(hasError);

        if (GUILayout.Button("Apply"))
        {
            OnlineMaps map = FindObjectOfType<OnlineMaps>();
            if (map != null)
            {
                map.customProviderURL = newUrl;
                Close();
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    private void ModifyURL()
    {
        if (string.IsNullOrEmpty(url)) hasError = true;
        else if (url.Contains("maps.googleapis.com"))
        {
            newUrl = Regex.Replace(url, @"!1m4!1i\d+!2i\d+!3i\d+!4i256", "!1m4!1i{zoom}!2i{x}!3i{y}!4i256");
            if (newUrl == url)
            {
                hasError = true;
                return;
            }
            int startIndex = newUrl.IndexOf("http");
            int endIndex = newUrl.IndexOf("!4e0");
            if (startIndex != -1 && endIndex != -1)
            {
                hasError = false;
                newUrl = newUrl.Substring(startIndex, endIndex - startIndex + 4);
            }
            else hasError = true;
        }
        else if (url.Contains(".tiles.mapbox.com/v4"))
        {
            newUrl = Regex.Replace(url, @"/\d+/\d+/\d+\.png", "/{zoom}/{x}/{y}.png");
            hasError = newUrl == url;
        }
        else hasError = true;
    }

    public static void OpenWindow()
    {
        OnlineMapsCustomURLWizard window = GetWindow<OnlineMapsCustomURLWizard>("Custom URL Wizard");
        window.minSize = new Vector2(450, 350);
        window.minSize = Vector2.zero;
        OnlineMaps map = FindObjectOfType<OnlineMaps>();
        if (map != null)
        {
            window.url = map.customProviderURL;
            window.ModifyURL();
        }
    }
}
