/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Dynamically creates texture for 2D Controls.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Dynamic Texture")]
[OnlineMapsPlugin("Dynamic Texture", typeof(OnlineMapsControlBase2D))]
public class OnlineMapsDynamicTexture: MonoBehaviour
{
    /// <summary>
    /// Width of the texture. Must be 256 * N.
    /// </summary>
    public int width = 512;

    /// <summary>
    /// Height of the texture. Must be 256 * N.
    /// </summary>
    public int height = 512;

    public void Start()
    {
        if (GetComponent<OnlineMapsAdjustToScreen>() != null) return;

        if (width < 256) throw new Exception("Width must be greater than or equal to 256.");
        if (height < 256) throw new Exception("Height must be greater than or equal to 256.");

        width = Mathf.ClosestPowerOfTwo(width);
        height = Mathf.ClosestPowerOfTwo(height);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.name = "Dynamic Map Texture";
        GetComponent<OnlineMaps>().SetTexture(texture);
    }
}