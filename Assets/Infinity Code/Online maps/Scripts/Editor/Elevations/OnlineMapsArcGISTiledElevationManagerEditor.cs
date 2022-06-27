/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsArcGISTiledElevationManager), true)]
public class OnlineMapsArcGISTiledElevationManagerEditor : OnlineMapsTiledElevationManagerEditor
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