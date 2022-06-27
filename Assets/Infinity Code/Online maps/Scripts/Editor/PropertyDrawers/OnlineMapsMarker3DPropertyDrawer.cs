/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(OnlineMapsMarker3D))]
public class OnlineMapsMarker3DPropertyDrawer : OnlineMapsMarkerBasePropertyDrawer
{
    public static float? isRotationChanged;
    protected override int countFields
    {
        get { return OnlineMaps.isPlaying? 9: 8; }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        try
        {
            Rect rect = new Rect(position.x, position.y, position.width, 16);

            if (!DrawHeader(label, rect, property))
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.BeginChangeCheck();
            SerializedProperty pLat = DrawProperty(property, "latitude", ref rect);
            if (EditorGUI.EndChangeCheck())
            {
                if (pLat.doubleValue < -90) pLat.doubleValue = -90;
                else if (pLat.doubleValue > 90) pLat.doubleValue = 90;
            }

            EditorGUI.BeginChangeCheck();
            SerializedProperty pLng = DrawProperty(property, "longitude", ref rect);
            if (EditorGUI.EndChangeCheck())
            {
                if (pLng.doubleValue < -180) pLng.doubleValue += 360;
                else if (pLng.doubleValue > 180) pLng.doubleValue -= 360;
            }

            DrawProperty(property, "range", ref rect, new GUIContent("Zooms"));

            DrawProperty(property, "_scale", ref rect);
            DrawProperty(property, "sizeType", ref rect);

            EditorGUI.BeginChangeCheck();
            DrawProperty(property, "_rotationY", ref rect);
            if (EditorGUI.EndChangeCheck() && OnlineMaps.isPlaying) isRotationChanged = property.FindPropertyRelative("_rotationY").floatValue;

            DrawProperty(property, "label", ref rect);
            DrawProperty(property, "prefab", ref rect);

            DrawCenterButton(rect, pLng, pLat);
        }
        catch
        {
        }


        EditorGUI.EndProperty();
    }
}