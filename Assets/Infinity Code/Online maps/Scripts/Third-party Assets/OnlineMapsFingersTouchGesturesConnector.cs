/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

#if FINGERS_TG
using DigitalRubyShared;
#endif

#if FINGERS_TG
[RequireComponent(typeof(FingersScript))]
#endif
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Fingers - Touch Gestures Connector")]
[OnlineMapsPlugin("Fingers - Touch Gestures Connector", typeof(OnlineMapsControlBase))]
public class OnlineMapsFingersTouchGesturesConnector : MonoBehaviour
{
#if FINGERS_TG
    public float scaleSpeed = 0.1f;
    public Vector2 speed = Vector2.one;

    private ScaleGestureRecognizer scaleGesture;
    private RotateGestureRecognizer rotateGesture;
    private OnlineMapsControlBase control;
    private OnlineMapsCameraOrbit cameraOrbit;

    private void Start()
    {
        control = OnlineMapsControlBase.instance;
        cameraOrbit = OnlineMapsCameraOrbit.instance;

        scaleGesture = new ScaleGestureRecognizer();
        scaleGesture.StateUpdated += ScaleGestureCallback;
        FingersScript.Instance.AddGesture(scaleGesture);

        if (cameraOrbit != null)
        {
            rotateGesture = new RotateGestureRecognizer();
            rotateGesture.StateUpdated += RotateGestureCallback;
            FingersScript.Instance.AddGesture(rotateGesture);
        }
    }

    private bool IsCursorOnUIElement()
    {
        Vector2 inputPosition = control.GetInputPosition();

        if (Input.touchSupported && Input.touchCount > 1)
        {
            Vector2 p1 = Input.GetTouch(0).position;
            Vector2 p2 = Input.GetTouch(1).position;

            inputPosition = Vector2.Lerp(p1, p2, 0.5f);
        }

        return control.IsCursorOnUIElement(inputPosition);
    }

    private void ScaleGestureCallback(GestureRecognizer gesture)
    {
        if (gesture.State == GestureRecognizerState.Executing && !IsCursorOnUIElement())
        {
            OnlineMaps.instance.floatZoom *= (scaleGesture.ScaleMultiplier - 1) * scaleSpeed + 1;
        }
    }

    private void RotateGestureCallback(GestureRecognizer gesture)
    {
        if (gesture.State == GestureRecognizerState.Executing && !IsCursorOnUIElement())
        {
            
            control.isMapDrag = false;
            if (!cameraOrbit.lockPan) cameraOrbit.rotation.y += rotateGesture.RotationDegreesDelta * speed.y;
        }
    }
#endif
}