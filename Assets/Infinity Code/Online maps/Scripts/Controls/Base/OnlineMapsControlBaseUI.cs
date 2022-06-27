/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if CURVEDUI
using CurvedUI;
#endif

/// <summary>
/// The base class for uGUI controls.
/// </summary>
/// <typeparam name="T">Type of display source.</typeparam>
public abstract class OnlineMapsControlBaseUI<T> : OnlineMapsControlBase2D where T: MaskableGraphic
{
    protected T image;

#if CURVEDUI
    private CurvedUISettings curvedUI;
#endif

    protected Camera worldCamera
    {
        get
        {
            if (image.canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return image.canvas.worldCamera;
        }
    }

    protected override void BeforeUpdate()
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        int touchCount = Input.GetMouseButton(0) ? 1 : 0;
        if (touchCount != lastTouchCount)
        {
            if (touchCount == 1) OnMapBasePress();
            else OnMapBaseRelease();
        }
        lastTouchCount = touchCount;
#else
        if (Input.touchCount != lastTouchCount)
        {
            if (Input.touchCount == 1) OnMapBasePress();
            else if (Input.touchCount == 0) OnMapBaseRelease();
        }
        lastTouchCount = Input.touchCount;
#endif
    }

    public override bool GetCoords(Vector2 position, out double lng, out double lat)
    {
        lng = lat = 0;
        double tx, ty;
        if (!GetTile(position, out tx, out ty)) return false;
        map.projection.TileToCoordinates(tx, ty, map.zoom, out lng, out lat);
        return true;
    }

    public override Rect GetRect()
    {
        RectTransform rectTransform = (RectTransform)transform;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        float xMin = float.PositiveInfinity, xMax = float.NegativeInfinity, yMin = float.PositiveInfinity, yMax = float.NegativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            Vector3 screenCoord = RectTransformUtility.WorldToScreenPoint(worldCamera, corners[i]);
            if (screenCoord.x < xMin) xMin = screenCoord.x;
            if (screenCoord.x > xMax) xMax = screenCoord.x;
            if (screenCoord.y < yMin) yMin = screenCoord.y;
            if (screenCoord.y > yMax) yMax = screenCoord.y;
        }
        Rect result = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        return result;
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        double mx, my;
        GetPosition(lng, lat, out mx, out my);
        OnlineMapsBuffer.StateProps lastState = map.buffer.lastState;
        mx /= lastState.width;
        my /= lastState.height;
        Rect mapRect = image.GetPixelAdjustedRect();
        mx = mapRect.x + mapRect.width * mx;
        my = mapRect.y + mapRect.height - mapRect.height * my;

        Vector3 worldPoint = new Vector3((float)mx, (float)my, 0);

        Matrix4x4 matrix = transform.localToWorldMatrix;
        worldPoint = matrix.MultiplyPoint(worldPoint);

        return RectTransformUtility.WorldToScreenPoint(worldCamera, worldPoint);
    }

    public override bool GetTile(Vector2 position, out double tx, out double ty)
    {
        tx = ty = 0;

        Vector2 point;

#if CURVEDUI
        if (curvedUI != null)
        {
            Camera activeCamera = image.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? Camera.main : image.canvas.worldCamera;

            if (!curvedUI.RaycastToCanvasSpace(activeCamera.ScreenPointToRay(position), out point)) return false;
            Vector3 worldPoint = image.canvas.transform.localToWorldMatrix.MultiplyPoint(point);
            point = image.rectTransform.worldToLocalMatrix.MultiplyPoint(worldPoint);
        }
        else
        {
#endif
        //if (!RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, position, worldCamera)) return false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, position, worldCamera, out point);
        if (point == Vector2.zero) return false;
#if CURVEDUI
        }
#endif

        Rect rect = image.GetPixelAdjustedRect();

        Vector2 size = rect.max - point;
        size.x = size.x / rect.size.x;
        size.y = size.y / rect.size.y;

        Vector2 r = new Vector2(size.x - .5f, size.y - .5f);

        int countX = map.width / OnlineMapsUtils.tileSize;
        int countY = map.height / OnlineMapsUtils.tileSize;

        map.GetTilePosition(out tx, out ty);

        float zoomCoof = map.zoomCoof;
        tx -= countX * r.x * zoomCoof;
        ty += countY * r.y * zoomCoof;

        return true;
    }

    protected override bool HitTest(Vector2 position)
    {
#if CURVEDUI
        if (curvedUI != null)
        {
            Camera activeCamera = image.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? Camera.main : image.canvas.worldCamera;
            return curvedUI.RaycastToCanvasSpace(activeCamera.ScreenPointToRay(position), out position);
        }
        
#endif
        if (EventSystem.current == null) return false;

        PointerEventData pe = new PointerEventData(EventSystem.current);
        pe.position = position;
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pe, hits);

        if (hits.Count > 0 && hits[0].gameObject != gameObject) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, position, worldCamera);
    }

    protected override void OnEnableLate()
    {
        image = GetComponent<T>();
        if (image == null)
        {
            Debug.LogError("Can not find " + typeof(T));
            OnlineMapsUtils.Destroy(this);
        }

#if CURVEDUI
        curvedUI = image.canvas.GetComponent<CurvedUISettings>();
#endif
    }
}