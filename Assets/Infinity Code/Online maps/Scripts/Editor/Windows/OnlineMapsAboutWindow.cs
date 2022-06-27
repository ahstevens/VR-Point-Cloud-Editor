/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEditor;
using UnityEngine;

public class OnlineMapsAboutWindow:EditorWindow
{
    private string years = "2013-" + DateTime.Now.Year;

    [MenuItem("GameObject/Infinity Code/Online Maps/About", false, 300)]
    public static void OpenWindow()
    {
        OnlineMapsAboutWindow window = GetWindow<OnlineMapsAboutWindow>(true, "About", true);
        window.minSize = new Vector2(200, 100);
        window.maxSize = new Vector2(200, 100);
    }

    public void OnGUI()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle textStyle = new GUIStyle(EditorStyles.label);
        textStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Label("Online Maps", titleStyle);
        GUILayout.Label("version " + OnlineMaps.version, textStyle);
        GUILayout.Label("created Infinity Code", textStyle);
        GUILayout.Label(years, textStyle);
    }
}