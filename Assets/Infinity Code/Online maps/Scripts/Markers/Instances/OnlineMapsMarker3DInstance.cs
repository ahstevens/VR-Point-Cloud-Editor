/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// 3D marker instance class.
/// </summary>
[AddComponentMenu("")]
public class OnlineMapsMarker3DInstance : OnlineMapsMarkerInstanceBase
{
    private double _longitude;
    private double _latitude;
    private float _scale;
    private float lastZoom;

    private OnlineMapsMarker3D _marker;

    public override OnlineMapsMarkerBase marker
    {
        get { return _marker; }
        set { _marker = value as OnlineMapsMarker3D; }
    }

    private void Awake()
    {
        Collider cl = GetComponent<Collider>();
        if (cl == null) cl  = gameObject.AddComponent<BoxCollider>();
        cl.isTrigger = true;
    }

    private void LateUpdate()
    {
        if (marker as OnlineMapsMarker3D == null) 
        {
            OnlineMapsUtils.Destroy(gameObject);
            return;
        }

        UpdateBaseProps();
    }

    private void Start()
    {
        marker.GetPosition(out _longitude, out _latitude);
        _scale = marker.scale;
        OnlineMapsMarker3D marker3D = marker as OnlineMapsMarker3D;
        transform.localRotation = marker3D.rotation;

        UpdateScale(marker3D);
    }

    private void UpdateBaseProps()
    {
        double mx, my;
        marker.GetPosition(out mx, out my);
        OnlineMapsMarker3D marker3D = marker as OnlineMapsMarker3D;

        if (Math.Abs(_longitude - mx) > double.Epsilon || Math.Abs(_latitude - my) > double.Epsilon)
        {
            _longitude = mx;
            _latitude = my;

            marker.Update();
        }

        if (marker3D.sizeType == OnlineMapsMarker3D.SizeType.realWorld && Math.Abs(lastZoom - _marker.manager.map.floatZoom) > float.Epsilon) UpdateScale(marker3D);

        if (Math.Abs(_scale - marker.scale) > float.Epsilon)
        {
            _scale = marker.scale;
            UpdateScale(marker3D);
        }
    }

    private void UpdateScale(OnlineMapsMarker3D marker3D)
    {
        if (marker3D.sizeType == OnlineMapsMarker3D.SizeType.scene) transform.localScale = new Vector3(_scale, _scale, _scale);
        else if (marker3D.sizeType == OnlineMapsMarker3D.SizeType.realWorld)
        {
            float coof = (1 << (OnlineMaps.MAXZOOM - _marker.manager.map.zoom)) * _marker.manager.map.zoomCoof;
            float s = _scale / coof;
            transform.localScale = new Vector3(s, s, s);
        }

        lastZoom = _marker.manager.map.floatZoom;
    }
}