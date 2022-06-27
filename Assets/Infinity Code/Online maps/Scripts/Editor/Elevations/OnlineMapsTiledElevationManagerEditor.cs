/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;

[CustomEditor(typeof(OnlineMapsTiledElevationManager<>), true)]
public class OnlineMapsTiledElevationManagerEditor : OnlineMapsElevationManagerBaseEditor
{
    public SerializedProperty cacheElevations;
    public SerializedProperty zoomOffset;

    protected override void CacheSerializedFields()
    {
        base.CacheSerializedFields();

        cacheElevations = serializedObject.FindProperty("cacheElevations");
        zoomOffset = serializedObject.FindProperty("zoomOffset");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        rootLayoutItem.Create(cacheElevations);
        rootLayoutItem.Create(zoomOffset);
    }
}