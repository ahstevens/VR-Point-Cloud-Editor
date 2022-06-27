/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// The class contains the coordinates of the area boundaries.
/// </summary>
public class OnlineMapsGeoRect
{
    /// <summary>
    /// Left longitude
    /// </summary>
    public double left;

    /// <summary>
    /// Right longitude
    /// </summary>
    public double right;

    /// <summary>
    /// Top latitude
    /// </summary>
    public double top;

    /// <summary>
    /// Bottom latitude
    /// </summary>
    public double bottom;

    /// <summary>
    /// Constructor
    /// </summary>
    public OnlineMapsGeoRect()
    {
        
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="left">Left longitude</param>
    /// <param name="top">Top latitude</param>
    /// <param name="right">Right longitude</param>
    /// <param name="bottom">Bottom latitude</param>
    public OnlineMapsGeoRect(double left, double top, double right, double bottom)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
    }
}