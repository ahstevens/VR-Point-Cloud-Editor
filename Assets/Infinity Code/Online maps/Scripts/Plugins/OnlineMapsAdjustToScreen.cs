/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adjusts map size to fit screen.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Adjust to Screen")]
[OnlineMapsPlugin("Adjust to Screen", typeof(OnlineMapsControlBase))]
public class OnlineMapsAdjustToScreen : MonoBehaviour
{
    [Header("Recommended for 2D Controls")]
    public bool halfSize = false;

    [Header("To not see the edges when rotating the map")]
    public bool useMaxSide = false;

    [Header("Optional")]
    public OnlineMapsRawImageTouchForwarder forwarder;

    public Camera mapCamera;

    private int screenWidth;
    private int screenHeight;
    private OnlineMaps map;
    private OnlineMapsControlBase control;
    private OnlineMapsCameraOrbit cameraOrbit;

    private void GetScreenSize(out int width, out int height)
    {
        if (forwarder == null)
        {
            width = Screen.width;
            height = Screen.height;
        }
        else
        {
            Vector3[] fourCorners = new Vector3[4];
            forwarder.image.rectTransform.GetWorldCorners(fourCorners);
            Vector3 size = fourCorners[2] - fourCorners[0];
            width = Mathf.RoundToInt(size.x);
            height = Mathf.RoundToInt(size.y);
        }
    }

    private void ResizeMap(int newWidth, int newHeight)
    {
        screenWidth = newWidth;
        screenHeight = newHeight;

        int width = newWidth / 256 * 256;
        int height = newHeight / 256 * 256;

        int zoom = map.zoom;
        
        if (halfSize)
        {
            width = width / 512 * 256;
            height = height / 512 * 256;
        }

        if (newWidth % 256 != 0) width += 256;
        if (newHeight % 256 != 0) height += 256;

        if (useMaxSide) width = height = Mathf.Max(width, height);

        if (height > (1 << zoom) * OnlineMapsUtils.tileSize)
        {
            zoom = Mathf.CeilToInt(Mathf.Log(height, 2) - 8);
        }

        if (width > (1 << zoom) * OnlineMapsUtils.tileSize)
        {
            zoom = Mathf.CeilToInt(Mathf.Log(width, 2) - 8);
        }

        int viewWidth = width;
        int viewHeight = height;

        if (halfSize)
        {
            viewWidth *= 2;
            viewHeight *= 2;
        }

        if (map.zoom != zoom) map.zoom = zoom;

        if (control.resultIsTexture)
        {
            OnlineMapsUtils.Destroy(control.activeTexture);
            if (control is OnlineMapsUIImageControl)
            {
                OnlineMapsUtils.Destroy(GetComponent<Image>().sprite);
            }
            else if (control is OnlineMapsSpriteRendererControl)
            {
                OnlineMapsUtils.Destroy(GetComponent<SpriteRenderer>().sprite);
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            map.SetTexture(texture);

            if (control is OnlineMapsUIRawImageControl)
            {
                RectTransform rt = transform as RectTransform;
                rt.sizeDelta = new Vector2(viewWidth, viewHeight);
            }
            else if (control is OnlineMapsUIImageControl)
            {
                RectTransform rt = transform as RectTransform;
                rt.sizeDelta = new Vector2(viewWidth, viewHeight);
            }
            else if (control is OnlineMapsSpriteRendererControl)
            {
                GetComponent<BoxCollider>().size = new Vector3(viewWidth / 100f, viewHeight / 100f, 0.2f);
            }

            map.RedrawImmediately();
        }
        else if (control is OnlineMapsTileSetControl)
        {
            OnlineMapsTileSetControl ts = control as OnlineMapsTileSetControl;

            ts.Resize(width, height, viewWidth, viewHeight);
            if (ts.currentCamera.orthographic) ts.currentCamera.orthographicSize = newHeight / 2f;
            else if (cameraOrbit != null) cameraOrbit.distance = newHeight * 0.8f;

            if (forwarder != null)
            {
                RenderTexture targetTexture = forwarder.targetTexture;
                if (targetTexture.width != newWidth || targetTexture.height != newHeight)
                {
                    targetTexture.Release();
                    targetTexture = new RenderTexture(newWidth, newHeight, 32);
                    forwarder.targetTexture = targetTexture;
                    forwarder.image.texture = targetTexture;
                    if (mapCamera != null) mapCamera.targetTexture = targetTexture;
                }
            }
        }
    }

    private void Start()
    {
        map = GetComponent<OnlineMaps>();
        control = map.control;
        cameraOrbit = GetComponent<OnlineMapsCameraOrbit>();

        int width, height;
        GetScreenSize(out width, out height);
        ResizeMap(width, height);
    }

    private void Update()
    {
        int width, height;
        GetScreenSize(out width, out height);
        if (screenWidth == width && screenHeight == height) return;

        ResizeMap(width, height);
    }
}