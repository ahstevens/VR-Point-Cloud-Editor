/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;

/// <summary>
/// Implementation of WGS84 Spherical Mercator (Web Mercator).
/// </summary>
public class OnlineMapsProjectionSphericalMercator:OnlineMapsProjection
{
    public override void CoordinatesToTile(double lng, double lat, int zoom, out double tx, out double ty)
    {
        double sy = Math.Sin(lat * DEG2RAD);
        lng = (lng + 180) / 360;
        lat = 0.5 - Math.Log((1 + sy) / (1 - sy)) / PI4;
        long mapSize = (long)OnlineMapsUtils.tileSize << zoom;
        double px = lng * mapSize + 0.5;
        double py = lat * mapSize + 0.5;

        if (px < 0) px = 0;
        else if (px > mapSize - 1) px = mapSize - 1;
        if (py < 0) py = 0;
        else if (py > mapSize - 1) py = mapSize - 1;

        tx = px / OnlineMapsUtils.tileSize;
        ty = py / OnlineMapsUtils.tileSize;
    }

    public override void TileToCoordinates(double tx, double ty, int zoom, out double lng, out double lat)
    {
        double mapSize = (long)OnlineMapsUtils.tileSize << zoom;
        lng = 360 * (OnlineMapsUtils.Repeat(tx * OnlineMapsUtils.tileSize, 0, mapSize - 1) / mapSize - 0.5);
        lat = 90 - 360 * Math.Atan(Math.Exp((OnlineMapsUtils.Clip(ty * OnlineMapsUtils.tileSize, 0, mapSize - 1) / mapSize - 0.5) * PI2)) / Math.PI;
    }
}