/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsFingersTouchGesturesConnector))]
public class OnlineMapsFingersTouchGesturesConnectorEditor : Editor
{
#if FINGERS_TG
    private OnlineMapsCameraOrbit cameraOrbit;
    private OnlineMapsFingersTouchGesturesConnector connector;

    private void OnEnable()
    {
        connector = target as OnlineMapsFingersTouchGesturesConnector;
        cameraOrbit = connector.GetComponent<OnlineMapsCameraOrbit>();
    }
#endif

    public override void OnInspectorGUI()
    {
#if !FINGERS_TG
        if (GUILayout.Button("Enable Fingers - Touch Gestures"))
        {
            if (EditorUtility.DisplayDialog("Enable Fingers - Touch Gestures", "You have Fingers - Touch Gestures in your project?", "Yes, I have Fingers - Touch Gestures", "Cancel"))
            {
                OnlineMapsEditor.AddCompilerDirective("FINGERS_TG");
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