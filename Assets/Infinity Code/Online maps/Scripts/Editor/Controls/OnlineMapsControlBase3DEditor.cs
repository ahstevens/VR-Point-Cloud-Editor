/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnlineMapsControlBase3D), true)]
public class OnlineMapsControlBase3DEditor<T> : OnlineMapsControlBaseEditor<T>
    where T: OnlineMapsControlBase3D
{
    protected SerializedProperty pMarker2DMode;
    protected SerializedProperty pMarker2DSize;
    protected SerializedProperty pActiveCamera;

    protected override void CacheSerializedFields()
    {
        base.CacheSerializedFields();

        pMarker2DMode = serializedObject.FindProperty("marker2DMode");
        pMarker2DSize = serializedObject.FindProperty("marker2DSize");
        pActiveCamera = serializedObject.FindProperty("activeCamera");
    }

    protected override void GenerateLayoutItems()
    {
        base.GenerateLayoutItems();

        rootLayoutItem.Create(pActiveCamera).content = new GUIContent("Camera");
        LayoutItem markerMode = rootLayoutItem.Create(pMarker2DMode);
        markerMode.OnChangedInPlaymode += () =>
        {
            if (pMarker2DMode.enumValueIndex == (int) OnlineMapsMarker2DMode.billboard) control.markerDrawer = new OnlineMapsMarkerBillboardDrawer(control as OnlineMapsControlBaseDynamicMesh);
            else control.markerDrawer = new OnlineMapsMarkerFlatDrawer(control as OnlineMapsControlBaseDynamicMesh);
            map.Redraw();
        };

        markerMode.Create(pMarker2DSize).OnValidateDraw += () => pMarker2DMode.enumValueIndex == (int)OnlineMapsMarker2DMode.billboard;
    }

    protected override void OnEnableBefore()
    {
        base.OnEnableBefore();

        if (control.GetComponent<OnlineMapsMarker3DManager>() == null) control.gameObject.AddComponent<OnlineMapsMarker3DManager>();
    }
}