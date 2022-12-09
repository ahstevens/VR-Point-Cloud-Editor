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

        LayoutItem item = new LayoutItem("WARNING");
        item.action += () => EditorGUILayout.HelpBox("ArcGIS has closed its elevations services. This component will not work and is present for compatibility. Replace it to some other Elevation Manager.", MessageType.Error);
        rootLayoutItem.Insert(0, item);

        rootLayoutItem.Create(resolution).OnChanged += () => { resolution.intValue = Mathf.Clamp(resolution.intValue, 16, 100); };
    }
}