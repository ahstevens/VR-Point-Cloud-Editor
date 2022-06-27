/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if EASYTOUCH
using HedgehogTeam.EasyTouch;
#endif
using System;
using UnityEngine;

[AddComponentMenu("Infinity Code/Online Maps/Plugins/EasyTouch Connector")]
[OnlineMapsPlugin("EasyTouch Connector", typeof(OnlineMapsControlBase))]
public class OnlineMapsEasyTouchConnector:MonoBehaviour
{
#if EASYTOUCH
    public OnlineMapsRawImageTouchForwarder forwarder;

    private Vector2 speed = Vector2.one;

    private OnlineMapsControlBase control;
    private OnlineMapsCameraOrbit cameraOrbit;

    private void EasyTouchOnOnTwist(Gesture gesture)
    {
        control.isMapDrag = false;
        if (!cameraOrbit.lockPan) cameraOrbit.rotation.y += gesture.twistAngle * speed.y;
    }

    private void EasyTouchOnOnDrag2Fingers(Gesture gesture)
    {
        control.isMapDrag = false;
        if (!cameraOrbit.lockTilt) cameraOrbit.rotation.x += gesture.deltaPosition.y * speed.x * 0.5f;
    }

    private void EasyTouchOnOnPinch(Gesture gesture)
    {
        control.isMapDrag = false;
        float delta = gesture.deltaPinch / 100;
        if (control.zoomMode == OnlineMapsZoomMode.center) OnlineMaps.instance.floatZoom += delta;
        else
        {
            Vector2 pos = gesture.position;
            if (forwarder != null) pos = forwarder.ForwarderToMapSpace(pos);
            control.ZoomOnPoint(delta, pos);
        }
    }

    private void Start()
    {
        control = OnlineMapsControlBase.instance;
        control.allowZoom = false;

        EasyTouch.On_Pinch += EasyTouchOnOnPinch;

        cameraOrbit = GetComponent<OnlineMapsCameraOrbit>();
        if (cameraOrbit != null)
        {
            speed = cameraOrbit.speed;
            cameraOrbit.speed = Vector2.zero;
            EasyTouch.On_Drag2Fingers += EasyTouchOnOnDrag2Fingers;
            EasyTouch.On_Twist += EasyTouchOnOnTwist;
        }
    }

#endif
}