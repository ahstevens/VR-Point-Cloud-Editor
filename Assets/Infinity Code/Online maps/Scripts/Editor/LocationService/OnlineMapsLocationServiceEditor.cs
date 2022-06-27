/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsLocationService))]
public class OnlineMapsLocationServiceEditor : OnlineMapsLocationServiceEditorBase
{
    private SerializedProperty desiredAccuracy;
    private SerializedProperty updateDistance;
    private SerializedProperty requestPermissionRuntime;

    protected override void CacheSerializedProperties()
    {
        base.CacheSerializedProperties();
        desiredAccuracy = serializedObject.FindProperty("desiredAccuracy");
        updateDistance = serializedObject.FindProperty("updateDistance");
        requestPermissionRuntime = serializedObject.FindProperty("requestPermissionRuntime");
    }

    public override void CustomInspectorGUI()
    {
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 170;

        EditorGUILayout.PropertyField(desiredAccuracy, new GUIContent("Desired Accuracy (meters)"));
        EditorGUIUtility.labelWidth = labelWidth;

        EditorGUILayout.PropertyField(updateDistance);
        EditorGUILayout.PropertyField(requestPermissionRuntime);
    }

    public override void CustomUpdatePositionGUI()
    {
    }
}