/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

/// <summary>
/// Base class for instance of marker. 
/// This class is used when for each marker create a separate GameObject.
/// </summary>
public abstract class OnlineMapsMarkerInstanceBase:MonoBehaviour 
{
    /// <summary>
    /// Reference to marker.
    /// </summary>
    public abstract OnlineMapsMarkerBase marker { get; set; }
}