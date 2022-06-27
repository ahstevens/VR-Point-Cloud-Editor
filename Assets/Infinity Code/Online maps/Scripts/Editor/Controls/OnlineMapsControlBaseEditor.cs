/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsControlBase), true)]
public abstract class OnlineMapsControlBaseEditor<T> : OnlineMapsFormattedEditor
    where T : OnlineMapsControlBase
{
    protected OnlineMaps map;
    protected T control;

    protected LayoutItem warningLayoutItem;

    protected SerializedProperty allowUserControl;
    protected SerializedProperty allowZoom;
    protected SerializedProperty invertTouchZoom;
    protected SerializedProperty zoomInOnDoubleClick;
    protected SerializedProperty zoomMode;
    protected SerializedProperty smoothZoom;
    protected SerializedProperty zoomSensitivity;

    protected override void CacheSerializedFields()
    {
        allowUserControl = serializedObject.FindProperty("allowUserControl");
        allowZoom = serializedObject.FindProperty("allowZoom");
        invertTouchZoom = serializedObject.FindProperty("invertTouchZoom");
        zoomInOnDoubleClick = serializedObject.FindProperty("zoomInOnDoubleClick");
        zoomMode = serializedObject.FindProperty("zoomMode");
        zoomMode = serializedObject.FindProperty("zoomMode");
        smoothZoom = serializedObject.FindProperty("smoothZoom");
        zoomSensitivity = serializedObject.FindProperty("zoomSensitivity");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        warningLayoutItem = rootLayoutItem.Create("WarningArea");
        rootLayoutItem.Create(allowUserControl);
        LayoutItem lZoom = rootLayoutItem.Create(allowZoom);
        lZoom.drawGroup = LayoutItem.Group.valueOn;
        lZoom.Create(zoomMode);
        lZoom.Create(zoomSensitivity);
        lZoom.Create(zoomInOnDoubleClick);
        lZoom.Create(invertTouchZoom);
        lZoom.Create(smoothZoom);
    }

    private static OnlineMaps GetOnlineMaps(OnlineMapsControlBase control)
    {
        OnlineMaps map = control.GetComponent<OnlineMaps>();

        if (map == null)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.HelpBox("Problem detected:\nCan not find OnlineMaps component.", MessageType.Error);

            if (GUILayout.Button("Add OnlineMaps Component"))
            {
                map = control.gameObject.AddComponent<OnlineMaps>();
                UnityEditorInternal.ComponentUtility.MoveComponentUp(map);
            }

            EditorGUILayout.EndVertical();
        }
        return map;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        map = null;
        control = null;
    }

    protected override void OnEnableBefore()
    {
        base.OnEnableBefore();

        control = (T)target;
        map = GetOnlineMaps(control);
        if (control.GetComponent<OnlineMapsMarkerManager>() == null) control.gameObject.AddComponent<OnlineMapsMarkerManager>();
    }

    protected override void OnSetDirty()
    {
        base.OnSetDirty();

        EditorUtility.SetDirty(map);
        EditorUtility.SetDirty(control);

        if (OnlineMaps.isPlaying) map.Redraw();
    }
}