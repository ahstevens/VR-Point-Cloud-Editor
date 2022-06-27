/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Class of range.
/// </summary>
[System.Serializable]
public class OnlineMapsRange
{
    /// <summary>
    /// Maximum value.
    /// </summary>
    public float max = float.MaxValue;

    /// <summary>
    /// Minimum value.
    /// </summary>
    public float min = float.MinValue;

    /// <summary>
    /// Maximum limit.<br/>
    /// Uses in inspector.
    /// </summary>
    public float maxLimit = OnlineMaps.MAXZOOM_EXT;

    /// <summary>
    /// Minimum limit.<br/>
    /// Uses in inspector.
    /// </summary>
    public float minLimit = OnlineMaps.MINZOOM;

    public OnlineMapsRange()
    {
        
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <param name="minLimit">Minimum limit.</param>
    /// <param name="maxLimit">Maximum limit.</param>
    public OnlineMapsRange(float min = float.MinValue, float max = float.MaxValue, float minLimit = OnlineMaps.MINZOOM, float maxLimit = OnlineMaps.MAXZOOM_EXT)
    {
        this.min = min;
        this.max = max;
        this.maxLimit = maxLimit;
        this.minLimit = minLimit;
    }

    /// <summary>
    /// Checks and limits value.
    /// </summary>
    /// <param name="value">Value</param>
    /// <returns>Value corresponding to the specified range.</returns>
    public float CheckAndFix(float value)
    {
        if (value < min) value = min;
        if (value > max) value = max;
        return value;
    }

    /// <summary>
    /// Checks whether the number in the range.
    /// </summary>
    /// <param name="value">Value</param>
    /// <returns>True - if the number is in the range, false - if not.</returns>
    public bool InRange(float value)
    {
        return value >= min && value <= max;
    }

    /// <summary>
    /// Converts a range to string.
    /// </summary>
    /// <returns>String</returns>
    public override string ToString()
    {
        return string.Format("Min: {0}, Max: {1}", min, max);
    }

    /// <summary>
    /// Updates the minimum and maximum values​​.
    /// </summary>
    /// <param name="newMin">Minimum value.</param>
    /// <param name="newMax">Maximum value.</param>
    /// <returns>True - if the range is changed, false - if not changed.</returns>
    public bool Update(float newMin, float newMax)
    {
        bool changed = false;
        if (Math.Abs(newMin - min) > float.Epsilon)
        {
            min = newMin;
            changed = true;
        }
        if (Math.Abs(newMax - max) > float.Epsilon)
        {
            max = newMax;
            changed = true;
        }
        return changed;
    }
}