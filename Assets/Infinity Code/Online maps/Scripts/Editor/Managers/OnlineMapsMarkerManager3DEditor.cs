/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsMarker3DManager), true)]
public class OnlineMapsMarkerManager3DEditor : OnlineMapsMarkerManagerBaseEditor<OnlineMapsMarker3DManager, OnlineMapsMarker3D>
{
    private SerializedProperty allowAddMarker3DByN;
    private SerializedProperty defaultPrefab;
    private SerializedProperty defaultScale;

    protected override void AddMarker()
    {
        if (!OnlineMaps.isPlaying)
        {
            OnlineMapsMarker3D marker = new OnlineMapsMarker3D
            {
                position = map.position
            };
            manager.Add(marker);
        }
        else
        {
            double lng, lat;
            map.GetPosition(out lng, out lat);
            manager.Create(lng, lat, manager.defaultPrefab);
        }
    }

    protected override void DrawItem(int i, ref int removedIndex)
    {
        base.DrawItem(i, ref removedIndex);

        if (OnlineMapsMarker3DPropertyDrawer.isRotationChanged.HasValue)
        {
            manager[i].rotationY = OnlineMapsMarker3DPropertyDrawer.isRotationChanged.Value;
            OnlineMapsMarker3DPropertyDrawer.isRotationChanged = null;
        }
    }

    protected override void DrawSettings(ref bool dirty)
    {
        base.DrawSettings(ref dirty);

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(defaultPrefab);
        EditorGUILayout.PropertyField(defaultScale);
        EditorGUILayout.PropertyField(allowAddMarker3DByN, new GUIContent("Add Marker3D by N"));

        if (EditorGUI.EndChangeCheck()) dirty = true;
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        allowAddMarker3DByN = serializedObject.FindProperty("allowAddMarker3DByN");
        defaultPrefab = serializedObject.FindProperty("defaultPrefab");
        defaultScale = serializedObject.FindProperty("defaultScale");
    }
}