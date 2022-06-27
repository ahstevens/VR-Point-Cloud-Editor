/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsMarkerManager), true)]
public class OnlineMapsMarkerManagerEditor: OnlineMapsMarkerManagerBaseEditor<OnlineMapsMarkerManager, OnlineMapsMarker>
{
    private SerializedProperty defaultTexture;
    private SerializedProperty defaultAlign;
    private SerializedProperty allowAddMarkerByM;
    private SerializedProperty defaultScale;

    protected override void AddMarker()
    {
        if (!OnlineMaps.isPlaying)
        {
            OnlineMapsMarker marker = new OnlineMapsMarker
            {
                position = map.position,
                align = manager.defaultAlign,
                scale = manager.defaultScale
            };
            manager.Add(marker);
        }
        else
        {
            double lng, lat;
            map.GetPosition(out lng, out lat);
            OnlineMapsMarker marker = manager.Create(lng, lat);
            marker.align = manager.defaultAlign;
            marker.scale = manager.defaultScale;
        }
    }

    protected override void DrawSettings(ref bool dirty)
    {
        base.DrawSettings(ref dirty);

        EditorGUI.BeginChangeCheck();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(defaultTexture);
        if (EditorGUI.EndChangeCheck()) OnlineMapsEditorUtils.CheckMarkerTextureImporter(defaultTexture);

        EditorGUILayout.PropertyField(defaultAlign);
        EditorGUILayout.PropertyField(defaultScale);
        EditorGUILayout.PropertyField(allowAddMarkerByM, new GUIContent("Add Marker by M"));

        if (EditorGUI.EndChangeCheck()) dirty = true;
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        defaultTexture = serializedObject.FindProperty("defaultTexture");
        defaultAlign = serializedObject.FindProperty("defaultAlign");
        defaultScale = serializedObject.FindProperty("defaultScale");
        allowAddMarkerByM = serializedObject.FindProperty("allowAddMarkerByM");

        if (defaultTexture.objectReferenceValue == null) defaultTexture.objectReferenceValue = OnlineMapsEditorUtils.LoadAsset<Texture2D>("Textures\\Markers\\DefaultMarker.png");
    }
}