/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

public abstract class OnlineMapsMarkerBasePropertyDrawer : PropertyDrawer
{
    public static bool isRemoved = false;
    public static bool? isEnabledChanged;

    protected virtual int countFields
    {
        get { return 0; }
    }

    protected void DrawCenterButton(Rect rect, SerializedProperty pLng, SerializedProperty pLat)
    {
        rect.y += 18;
        if (OnlineMaps.isPlaying && GUI.Button(rect, "Center"))
        {
            OnlineMaps.instance.SetPosition(pLng.doubleValue, pLat.doubleValue);
        }
    }

    protected bool DrawHeader(GUIContent label, Rect rect, SerializedProperty property)
    {
        SerializedProperty pExpand = property.FindPropertyRelative("expand");
        pExpand.boolValue = EditorGUI.Toggle(new Rect(rect.x, rect.y, 16, rect.height), string.Empty, pExpand.boolValue, EditorStyles.foldout);

        SerializedProperty pEnabled = property.FindPropertyRelative("_enabled");

        EditorGUI.BeginChangeCheck();
        bool newEnabled = EditorGUI.ToggleLeft(new Rect(rect.x + 16, rect.y, rect.width - 36, rect.height), label, pEnabled.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            if (OnlineMaps.isPlaying) isEnabledChanged = newEnabled;
            else pEnabled.boolValue = newEnabled;
        }

        if (GUI.Button(new Rect(rect.x + rect.width - 20, rect.y, 20, rect.height), "X")) isRemoved = true;

        return pExpand.boolValue;
    }

    protected SerializedProperty DrawProperty(SerializedProperty property, string name, ref Rect rect, GUIContent label = null)
    {
        rect.y += 18;
        SerializedProperty prop = property.FindPropertyRelative(name);
        EditorGUI.PropertyField(rect, prop, label);
        return prop;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.FindPropertyRelative("expand").boolValue) return 18;
        return (countFields + 1) * 18;
    }
}