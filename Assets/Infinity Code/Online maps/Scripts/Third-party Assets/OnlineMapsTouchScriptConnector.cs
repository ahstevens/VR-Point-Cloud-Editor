/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

#if TOUCHSCRIPT
using TouchScript.Gestures.TransformGestures;
#endif

[OnlineMapsPlugin("TouchScript Connector", typeof(OnlineMapsControlBase))]
[AddComponentMenu("Infinity Code/Online Maps/Plugins/TouchScript Connector")]
public class OnlineMapsTouchScriptConnector : MonoBehaviour
{
#if TOUCHSCRIPT
    public ScreenTransformGesture gesture;
    public RotationMode rotationMode = RotationMode.camera;

    private OnlineMapsControlBase control;
    private OnlineMapsCameraOrbit cameraOrbit;
    private Vector2 speed;
    private OnlineMapsControlBaseDynamicMesh dmControl;

    private void Start()
    {
        if (gesture == null)
        {
            Debug.LogWarning("Online Maps TouchScript Connector / Gesture cannot be null");
            Destroy(this);
            return;
        }

        control = OnlineMapsControlBase.instance;
        control.allowZoom = false;

        dmControl = control as OnlineMapsControlBaseDynamicMesh;

        cameraOrbit = GetComponent<OnlineMapsCameraOrbit>();

        if (cameraOrbit != null)
        {
            speed = cameraOrbit.speed;
            cameraOrbit.speed = Vector2.zero;
        }

        gesture.Transformed += GestureOnTransformed;
    }

    private void GestureOnTransformed(object sender, EventArgs eventArgs)
    {
        if (gesture.NumPointers != 2) return;
        control.isMapDrag = false;
        float deltaScale = gesture.DeltaScale - 1;

        if (control.zoomMode == OnlineMapsZoomMode.center) control.map.floatZoom += deltaScale * control.zoomSensitivity;
        else control.ZoomOnPoint(deltaScale * control.zoomSensitivity, gesture.ScreenPosition);

        if (rotationMode == RotationMode.camera)
        {
            if (cameraOrbit != null)
            {
                if (!cameraOrbit.lockTilt) cameraOrbit.rotation.x += gesture.DeltaPosition.y * speed.x;
                if (!cameraOrbit.lockPan) cameraOrbit.rotation.y += gesture.DeltaRotation * speed.y;
            }
        }
        else if (dmControl != null) RotateMap(gesture);
    }

    private void RotateMap(ScreenTransformGesture gesture)
    {
        double lng1, lat1, lng2, lat2;
        control.GetCoords(gesture.ScreenPosition, out lng1, out lat1);

        Vector3 p = dmControl.center;
        p = control.transform.localToWorldMatrix.MultiplyPoint(p);
        Vector3 pos = control.transform.position - p;
        pos = Quaternion.Euler(0, -gesture.DeltaRotation * speed.y, 0) * pos + p;
        control.transform.position = pos;
        control.transform.Rotate(0, -gesture.DeltaRotation * speed.y, 0);

        control.GetCoords(gesture.ScreenPosition, out lng2, out lat2);

        double ox = lng2 - lng1;
        double oy = lat2 - lat1;

        control.map.GetPosition(out lng1, out lat1);
        control.map.SetPosition(lng1 - ox, lat1 - oy);
    }

    public enum RotationMode
    {
        camera,
        map
    }
#endif
}