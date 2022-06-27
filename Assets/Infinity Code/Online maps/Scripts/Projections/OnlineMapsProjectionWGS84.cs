/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Implementation of WGS84 Ellipsoid Mercator.
/// </summary>
public class OnlineMapsProjectionWGS84: OnlineMapsProjection
{
    /// <summary>
    /// PI / 4
    /// </summary>
    public const double PID4 = Math.PI / 4;

    public override void CoordinatesToTile(double lng, double lat, int zoom, out double tx, out double ty)
    {
        lat = OnlineMapsUtils.Clip(lat, -85, 85);
        lng = OnlineMapsUtils.Repeat(lng, -180, 180);

        double rLon = lng * DEG2RAD;
        double rLat = lat * DEG2RAD;

        const double a = 6378137;
        const double d = 53.5865938 / 256;
        const double k = 0.0818191908426;

        double z = Math.Tan(PID4 + rLat / 2) / Math.Pow(Math.Tan(PID4 + Math.Asin(k * Math.Sin(rLat)) / 2), k);
        double z1 = Math.Pow(2, 23 - zoom);

        
        tx = (20037508.342789 + a * rLon) * d / z1;
        ty = (20037508.342789 - a * Math.Log(z)) * d / z1;
    }

    public override void TileToCoordinates(double tx, double ty, int zoom, out double lng, out double lat)
    {
        const double a = 6378137;
        const double c1 = 0.00335655146887969;
        const double c2 = 0.00000657187271079536;
        const double c3 = 0.00000001764564338702;
        const double c4 = 0.00000000005328478445;
        const double d = 256 / 53.5865938;
        double z1 = 23 - zoom;
        double mercX = tx * Math.Pow(2, z1) * d - 20037508.342789;
        double mercY = 20037508.342789 - ty * Math.Pow(2, z1) * d;

        double g = Math.PI / 2 - 2 * Math.Atan(1 / Math.Exp(mercY / a));
        double z = g + c1 * Math.Sin(2 * g) + c2 * Math.Sin(4 * g) + c3 * Math.Sin(6 * g) + c4 * Math.Sin(8 * g);

        lat = z * RAD2DEG;
        lng = mercX / a * RAD2DEG;
    }
}