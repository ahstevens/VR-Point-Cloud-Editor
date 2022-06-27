/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsArcGISElevationManager), true)]
public class OnlineMapsArcGISElevationManagerEditor : OnlineMapsSinglePartElevationManagerEditor
{
    private SerializedProperty resolution;

    protected override void CacheSerializedFields()
    {
        base.CacheSerializedFields();

        resolution = serializedObject.FindProperty("resolution");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        rootLayoutItem.Create(resolution).OnChanged += () => { resolution.intValue = Mathf.Clamp(resolution.intValue, 16, 100); };
    }
}