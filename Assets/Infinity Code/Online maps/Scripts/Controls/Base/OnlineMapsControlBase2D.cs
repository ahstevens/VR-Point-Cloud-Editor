/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Class implements the basic functionality control of the 2D map.
/// </summary>
[Serializable]
public abstract class OnlineMapsControlBase2D : OnlineMapsControlBase
{
    /// <summary>
    /// Singleton instance of OnlineMapsControlBase2D control.
    /// </summary>
    public new static OnlineMapsControlBase2D instance
    {
        get { return _instance as OnlineMapsControlBase2D; }
    }

    /// <summary>
    /// Indicates whether it is possible to get the screen coordinates store. True - for 2D map.
    /// </summary>
    public override bool allowMarkerScreenRect
    {
        get { return true; } 
    }
}