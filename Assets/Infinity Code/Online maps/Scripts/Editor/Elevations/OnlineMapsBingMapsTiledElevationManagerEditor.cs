/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;

[CustomEditor(typeof(OnlineMapsBingMapsTiledElevationManager), true)]
public class OnlineMapsBingMapsTiledElevationManagerEditor : OnlineMapsTiledElevationManagerEditor
{
    private OnlineMapsKeyManager keyManager;

    private void CheckKey()
    {
        if (keyManager == null)
        {
            EditorGUILayout.HelpBox("Potential problem detected:\nCannot find Online Maps Key Manager component.", MessageType.Warning);
        }
        else if (string.IsNullOrEmpty(keyManager.bingMaps))
        {
            EditorGUILayout.HelpBox("Potential problem detected:\nOnline Maps Key Manager / Bing Maps is empty.", MessageType.Warning);
        }
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        rootLayoutItem.Create("keyWarning", CheckKey);
    }

    protected override void OnEnableLate()
    {
        base.OnEnableLate();

        keyManager = (target as OnlineMapsBingMapsTiledElevationManager).GetComponent<OnlineMapsKeyManager>();
        if (keyManager == null) keyManager = FindObjectOfType<OnlineMapsKeyManager>();
    }
}