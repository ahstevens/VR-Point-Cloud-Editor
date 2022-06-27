/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Class control the map for the uGUI UI RawImage.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Controls/UI RawImage")]
public class OnlineMapsUIRawImageControl : OnlineMapsControlBaseUI<RawImage>
{
    /// <summary>
    /// Singleton instance of OnlineMapsUIRawImageControl control.
    /// </summary>
    public new static OnlineMapsUIRawImageControl instance
    {
        get { return _instance as OnlineMapsUIRawImageControl; }
    }

    public override void SetTexture(Texture2D texture)
    {
        base.SetTexture(texture);
        image.texture = texture;
    }
}