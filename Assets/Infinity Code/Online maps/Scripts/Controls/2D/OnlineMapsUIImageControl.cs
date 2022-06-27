/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class control the map for the uGUI UI Image.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Controls/UI Image")]
public class OnlineMapsUIImageControl : OnlineMapsControlBaseUI<Image>
{
    /// <summary>
    /// Singleton instance of OnlineMapsUIImageControl control.
    /// </summary>
    public new static OnlineMapsUIImageControl instance
    {
        get { return _instance as OnlineMapsUIImageControl; }
    }

    public override void SetTexture(Texture2D texture)
    {
        base.SetTexture(texture);
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
    }
}