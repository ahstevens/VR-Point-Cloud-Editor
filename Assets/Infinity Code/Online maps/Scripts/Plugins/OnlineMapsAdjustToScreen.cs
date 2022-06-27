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

    private int screenWidth;
    private int screenHeight;
    private OnlineMaps map;
    private OnlineMapsControlBase control;
    private OnlineMapsCameraOrbit cameraOrbit;

    private void ResizeMap()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        int width = screenWidth / 256 * 256;
        int height = screenHeight / 256 * 256;

        int zoom = map.zoom;
        
        if (halfSize)
        {
            width = width / 512 * 256;
            height = height / 512 * 256;
        }

        if (screenWidth % 256 != 0) width += 256;
        if (screenHeight % 256 != 0) height += 256;

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
            if (ts.activeCamera.orthographic) ts.activeCamera.orthographicSize = screenHeight / 2f;
            else if (cameraOrbit != null) cameraOrbit.distance = screenHeight * 0.8f;
        }
    }

    private void Start()
    {
        map = GetComponent<OnlineMaps>();
        control = map.control;
        cameraOrbit = GetComponent<OnlineMapsCameraOrbit>();

        ResizeMap();
    }

    private void Update()
    {
        if (screenWidth != Screen.width || screenHeight != Screen.height) ResizeMap();
    }
}