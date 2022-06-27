/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

/// <summary>
/// This component manages drawing elements.
/// </summary>
[AddComponentMenu("")]
public class OnlineMapsDrawingElementManager: OnlineMapsInteractiveElementManager<OnlineMapsDrawingElementManager, OnlineMapsDrawingElement>
{
    protected override void OnEnable()
    {
        base.OnEnable();

        _instance = this;
    }
}