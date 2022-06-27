/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// The base class of map projection.
/// </summary>
public abstract class OnlineMapsProjection
{
    /// <summary>
    /// Degrees-to-radians conversion constant.
    /// </summary>
    public const double DEG2RAD = Math.PI / 180;

    /// <summary>
    /// PI * 2
    /// </summary>
    public const double PI2 = Math.PI * 2;

    /// <summary>
    /// PI * 4
    /// </summary>
    public const double PI4 = Math.PI * 4;

    /// <summary>
    /// Radians-to-degrees conversion constant.
    /// </summary>
    public const double RAD2DEG = 180 / Math.PI;

    /// <summary>
    /// Converts geographic coordinates to tile coordinates.
    /// </summary>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    /// <param name="zoom">Zoom</param>
    /// <param name="tx">Tile X</param>
    /// <param name="ty">Tile Y</param>
    public abstract void CoordinatesToTile(double lng, double lat, int zoom, out double tx, out double ty);

    /// <summary>
    /// Converts tile coordinates to geographic coordinates.
    /// </summary>
    /// <param name="tx">Tile X</param>
    /// <param name="ty">Tile Y</param>
    /// <param name="zoom">Zoom</param>
    /// <param name="lng">Longitude</param>
    /// <param name="lat">Latitude</param>
    public abstract void TileToCoordinates(double tx, double ty, int zoom, out double lng, out double lat);
}