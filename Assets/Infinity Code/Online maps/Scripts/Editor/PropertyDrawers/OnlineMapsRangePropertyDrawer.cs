/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(OnlineMapsRange))]
public class OnlineMapsRangePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty pMin = property.FindPropertyRelative("min");
        SerializedProperty pMax = property.FindPropertyRelative("max");
        SerializedProperty pMinLimit = property.FindPropertyRelative("minLimit");
        SerializedProperty pMaxLimit = property.FindPropertyRelative("maxLimit");

        label = new GUIContent(label);
        label.text = string.Format("{0} ({1:F1}-{2:F1})", label.text, pMin.floatValue, pMax.floatValue);
        position = EditorGUI.PrefixLabel(position, label);

        float min = pMin.floatValue;
        float max = pMax.floatValue;

        EditorGUI.BeginChangeCheck();
        EditorGUI.MinMaxSlider(position, ref min, ref max, pMinLimit.floatValue, pMaxLimit.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            if (min > max) min = max;
            pMin.floatValue = min;
            pMax.floatValue = max;
        }
    }
}