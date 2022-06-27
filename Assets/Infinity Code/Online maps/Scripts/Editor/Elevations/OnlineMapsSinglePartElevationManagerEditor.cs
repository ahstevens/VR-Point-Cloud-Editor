/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;

[CustomEditor(typeof(OnlineMapsSinglePartElevationManager<>), true)]
public abstract class OnlineMapsSinglePartElevationManagerEditor : OnlineMapsElevationManagerBaseEditor
{
    protected SerializedProperty tweenUpdateValues;
    protected SerializedProperty tweenDuration;

    protected override void CacheSerializedFields()
    {
        base.CacheSerializedFields();

        tweenUpdateValues = serializedObject.FindProperty("tweenUpdateValues");
        tweenDuration = serializedObject.FindProperty("tweenDuration");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        rootLayoutItem.Create(tweenUpdateValues);
        rootLayoutItem.Create(tweenDuration).OnValidateDraw += () => tweenUpdateValues.boolValue;
    }
}