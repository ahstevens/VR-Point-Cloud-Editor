/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(OnlineMapsBuildingMaterial))]
public class OnlineMapsBuildingMaterialPropertyDrawer : PropertyDrawer
{
    public static bool isRemoved = false;

    private const int countFields = 3;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.LabelField(new Rect(position.x, position.y, position.width - 30, 16), label);
        if (GUI.Button(new Rect(position.xMax - 20, position.y, 20, 16), "X")) isRemoved = true;

        try
        {
            Rect rect = new Rect(position.x, position.y, position.width, 16);

            DrawProperty(property, "wall", ref rect, new GUIContent("Wall Material"));
            DrawProperty(property, "roof", ref rect, new GUIContent("Roof Material"));
            DrawProperty(property, "scale", ref rect);
        }
        catch
        {
        }


        EditorGUI.EndProperty();
    }

    private SerializedProperty DrawProperty(SerializedProperty property, string name, ref Rect rect, GUIContent label = null)
    {
        rect.y += 18;
        SerializedProperty prop = property.FindPropertyRelative(name);
        EditorGUI.PropertyField(rect, prop, label);
        return prop;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (countFields + 1) * 18;
    }
}