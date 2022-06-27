/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections;
using UnityEngine;

/// <summary>
/// Class control the map for the NGUI.
/// </summary>
[System.Serializable]
[AddComponentMenu("Infinity Code/Online Maps/Controls/NGUI Texture")]
public class OnlineMapsNGUITextureControl : OnlineMapsControlBase2D
{
#if NGUI
    private UITexture uiTexture;
    private UIWidget uiWidget;

    /// <summary>
    /// Singleton instance of OnlineMapsNGUITextureControl control.
    /// </summary>
    public new static OnlineMapsNGUITextureControl instance
    {
        get { return OnlineMapsControlBase.instance as OnlineMapsNGUITextureControl; }
    }

    protected override bool allowTouchZoom
    {
        get { return false; }
    }

    public override Rect uvRect
    {
        get { return uiTexture.uvRect; }
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
        int w = Screen.width / 2;
        int h = Screen.height / 2;

        Bounds b = NGUIMath.CalculateAbsoluteWidgetBounds(uiTexture.transform);

        int rx = Mathf.RoundToInt(b.min.x * h + w);
        int ry = Mathf.RoundToInt((b.min.y + 1) * h);
        int rz = Mathf.RoundToInt(b.size.x * h);
        int rw = Mathf.RoundToInt(b.size.y * h);

        return new Rect(rx, ry, rz, rw);
    }

    public override Vector2 GetScreenPosition(double lng, double lat)
    {
        if (UICamera.currentCamera == null) return Vector2.zero;

        double px, py;
        GetPosition(lng, lat, out px, out py);
        px = (px / map.width - 0.5f) * uiWidget.localSize.x;
        py = (0.5f - py / map.height) * uiWidget.localSize.y;
        Vector3 worldPos = transform.TransformPoint(new Vector3((float)px, (float)py, 0));
        Vector3 screenPosition = UICamera.currentCamera.WorldToScreenPoint(worldPos);
        return screenPosition;
    }

    public override bool GetTile(Vector2 position, out double tx, out double ty)
    {
        tx = ty = 0;
        if (UICamera.currentCamera == null) return false;

        Vector3 worldPos = UICamera.currentCamera.ScreenToWorldPoint(position);
        Vector3 localPos = transform.worldToLocalMatrix.MultiplyPoint3x4(worldPos);

        localPos.x = localPos.x / uiWidget.localSize.x;
        localPos.y = localPos.y / uiWidget.localSize.y;

        map.GetTilePosition(out tx, out ty);

        int countX = map.texture.width / OnlineMapsUtils.tileSize;
        int countY = map.texture.height / OnlineMapsUtils.tileSize;

        float zoomCoof = map.zoomCoof;
        tx += countX * localPos.x * zoomCoof;
        ty -= countY * localPos.y * zoomCoof;

        return true;
    }

    protected override bool HitTest(Vector2 position)
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        return UICamera.currentTouch != null && UICamera.currentTouch.current == gameObject;
#else
        return UICamera.hoveredObject == gameObject;
#endif
    }


    protected override void OnEnableLate()
    {
        uiWidget = GetComponent<UIWidget>();
        uiTexture = GetComponent<UITexture>();
        if (uiTexture == null)
        {
            Debug.LogError("Can not find UITexture.");
            OnlineMapsUtils.Destroy(this);
        }
    }

    private void OnPress(bool state)
    {
        if (state) OnMapBasePress();
        else OnMapBaseRelease();
    }

    public override void SetTexture(Texture2D texture)
    {
        base.SetTexture(texture);
        StartCoroutine(OnFrameEnd(texture));
    }

    public IEnumerator OnFrameEnd(Texture2D texture)
    {
        yield return new WaitForEndOfFrame();
        uiTexture.mainTexture = texture;
    }
#else
    public override bool GetCoords(Vector2 position, out double lng, out double lat)
    {
        lng = lat = 0;
        return false;
    }

    public override bool GetTile(Vector2 position, out double tx, out double ty)
    {
        tx = ty = 0;
        return false;
    }
#endif
}