/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsTouchScriptConnector))]
public class OnlineMapsTouchScriptConnectorEditor : Editor
{
#if TOUCHSCRIPT
    private OnlineMapsCameraOrbit cameraOrbit;
    private OnlineMapsTouchScriptConnector connector;

    private void OnEnable()
    {
        connector = target as OnlineMapsTouchScriptConnector;
        cameraOrbit = connector.GetComponent<OnlineMapsCameraOrbit>();
    }
#endif

    public override void OnInspectorGUI()
    {
#if !TOUCHSCRIPT
        if (GUILayout.Button("Enable TouchScript"))
        {
            if (EditorUtility.DisplayDialog("Enable TouchScript", "You have TouchScript in your project?", "Yes, I have TouchScript", "Cancel"))
            {
                OnlineMapsEditor.AddCompilerDirective("TOUCHSCRIPT");
            }
        }
#else 
        base.OnInspectorGUI();
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