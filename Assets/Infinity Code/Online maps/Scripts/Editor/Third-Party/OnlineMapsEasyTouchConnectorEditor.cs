/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsEasyTouchConnector))]
public class OnlineMapsEasyTouchConnectorEditor : Editor
{
#if EASYTOUCH
    private OnlineMapsCameraOrbit cameraOrbit;
    private OnlineMapsEasyTouchConnector connector;
    private SerializedProperty forwarder;

    private void OnEnable()
    {
        connector = target as OnlineMapsEasyTouchConnector;
        cameraOrbit = connector.GetComponent<OnlineMapsCameraOrbit>();
        forwarder = serializedObject.FindProperty("forwarder");
    }
#endif

    public override void OnInspectorGUI()
    {
#if !EASYTOUCH
        if (GUILayout.Button("Enable EasyTouch"))
        {
            if (EditorUtility.DisplayDialog("Enable EasyTouch", "You have EasyTouch in your project?", "Yes, I have EasyTouch", "Cancel"))
            {
                OnlineMapsEditor.AddCompilerDirective("EASYTOUCH");
            }
        }
#else
        serializedObject.Update();
        EditorGUILayout.PropertyField(forwarder);
        serializedObject.ApplyModifiedProperties();

        if (cameraOrbit == null)
        {
            EditorGUILayout.HelpBox("To use twist and tilt gestures, add Online Maps Camera Orbit component.", MessageType.Warning);
            if (GUILayout.Button("Add Camera Orbit"))
            {
                connector.gameObject.AddComponent<OnlineMapsCameraOrbit>();
            }
        }
#endif
    }
}