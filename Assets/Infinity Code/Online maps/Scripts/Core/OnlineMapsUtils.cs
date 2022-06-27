/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper class, which contains all the basic methods.
/// </summary>
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class OnlineMapsUtils
{
    public static string persistentDataPath;

    /// <summary>
    /// Arcseconds in meters.
    /// </summary>
    public const float angleSecond = 1 / 3600f;

    /// <summary>
    /// Maximal distance of raycast.
    /// </summary>
    public const int maxRaycastDistance = 100000;

    /// <summary>
    /// Earth radius.
    /// </summary>
    public const double R = 6371;

    /// <summary>
    /// Degrees-to-radians conversion constant.
    /// </summary>
    public const double Deg2Rad = Math.PI / 180;

    /// <summary>
    /// Radians-to-degrees conversion constant.
    /// </summary>
    public const double Rad2Deg = 180 / Math.PI;

    /// <summary>
    /// Bytes per megabyte.
    /// </summary>
    public const int mb = 1024 * 1024;

    /// <summary>
    /// PI * 4
    /// </summary>
    public const float pi4 = 4 * Mathf.PI;

    /// <summary>
    /// Size of the tile texture in pixels.
    /// </summary>
    public const short tileSize = 256;

    /// <summary>
    /// The second in ticks.
    /// </summary>
    public const long second = 10000000;

    /// <summary>
    /// tileSize squared, to accelerate the calculations.
    /// </summary>
    public const int sqrTileSize = tileSize * tileSize;

    public static CultureInfo cultureInfo
    {
        get { return CultureInfo.InvariantCulture; }
    }

    public static NumberFormatInfo numberFormat
    {
        get { return cultureInfo.NumberFormat; }
    }

    static OnlineMapsUtils()
    {
        persistentDataPath = Application.persistentDataPath;
    }

    /// <summary>
    /// The angle between the two points in degree.
    /// </summary>
    /// <param name="point1">Point 1</param>
    /// <param name="point2">Point 2</param>
    /// <returns>Angle in degree</returns>
    public static float Angle2D(Vector2 point1, Vector2 point2)
    {
        return Mathf.Atan2(point2.y - point1.y, point2.x - point1.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// The angle between the two points in degree.
    /// </summary>
    /// <param name="point1">Point 1</param>
    /// <param name="point2">Point 2</param>
    /// <returns>Angle in degree</returns>
    public static float Angle2D(Vector3 point1, Vector3 point2)
    {
        return Mathf.Atan2(point2.z - point1.z, point2.x - point1.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// The angle between the two points in degree.
    /// </summary>
    /// <param name="p1x">Point 1 X</param>
    /// <param name="p1y">Point 1 Y</param>
    /// <param name="p2x">Point 2 X</param>
    /// <param name="p2y">Point 2 Y</param>
    /// <returns>Angle in degree</returns>
    public static double Angle2D(double p1x, double p1y, double p2x, double p2y)
    {
        return Math.Atan2(p2y - p1y, p2x - p1x) * Rad2Deg;
    }

    /// <summary>
    /// The angle between the three points in degree.
    /// </summary>
    /// <param name="point1">Point 1</param>
    /// <param name="point2">Point 2</param>
    /// <param name="point3">Point 3</param>
    /// <param name="unsigned">Return a positive result.</param>
    /// <returns>Angle in degree</returns>
    public static float Angle2D(Vector3 point1, Vector3 point2, Vector3 point3, bool unsigned = true)
    {
        float angle1 = Angle2D(point1, point2);
        float angle2 = Angle2D(point2, point3);
        float angle = angle1 - angle2;
        if (angle > 180) angle -= 360;
        if (angle < -180) angle += 360;
        if (unsigned) angle = Mathf.Abs(angle);
        return angle;
    }

    /// <summary>
    /// The angle between the two points in radians.
    /// </summary>
    /// <param name="point1">Point 1</param>
    /// <param name="point2">Point 2</param>
    /// <param name="offset">Result offset in degrees.</param>
    /// <returns>Angle in radians</returns>
    public static float Angle2DRad(Vector3 point1, Vector3 point2, float offset = 0)
    {
        return Mathf.Atan2(point2.z - point1.z, point2.x - point1.x) + offset * Mathf.Deg2Rad;
    }

    /// <summary>
    /// The angle between the two points in radians.
    /// </summary>
    /// <param name="p1x">Point 1 X</param>
    /// <param name="p1z">Point 1 Z</param>
    /// <param name="p2x">Point 2 X</param>
    /// <param name="p2z">Point 2 Z</param>
    /// <param name="offset">Result offset in degrees.</param>
    /// <returns>Angle in radians</returns>
    public static float Angle2DRad(float p1x, float p1z, float p2x, float p2z, float offset = 0)
    {
        return Mathf.Atan2(p2z - p1z, p2x - p1x) + offset * Mathf.Deg2Rad;
    }

    public static float AngleOfTriangle(Vector2 A, Vector2 B, Vector2 C)
    {
        float a = (B - C).magnitude;
        float b = (A - C).magnitude;
        float c = (A - B).magnitude;

        return Mathf.Acos((a * a + b * b - c * c) / (2 * a * b));
    }

    /// <summary>
    /// Clamps a value between a minimum double and maximum double value.
    /// </summary>
    /// <param name="n">Value</param>
    /// <param name="minValue">Minimum</param>
    /// <param name="maxValue">Maximum</param>
    /// <returns>Value between a minimum and maximum.</returns>
    public static double Clip(double n, double minValue, double maxValue)
    {
        if (n < minValue) return minValue;
        if (n > maxValue) return maxValue;
        return n;
    }

    public static Vector2 Crossing(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        if (Math.Abs(p3.x - p4.x) < float.Epsilon)
        {
            float y = p1.y + (p2.y - p1.y) * (p3.x - p1.x) / (p2.x - p1.x);
            if (y > Mathf.Max(p3.y, p4.y) || y < Mathf.Min(p3.y, p4.y) || y > Mathf.Max(p1.y, p2.y) || y < Mathf.Min(p1.y, p2.y)) return Vector2.zero;
            return new Vector2(p3.x, y);
        }
        float x = p1.x + (p2.x - p1.x) * (p3.y - p1.y) / (p2.y - p1.y);
        if (x > Mathf.Max(p3.x, p4.x) || x < Mathf.Min(p3.x, p4.x) || x > Mathf.Max(p1.x, p2.x) || x < Mathf.Min(p1.x, p2.x)) return Vector2.zero;
        return new Vector2(x, p3.y);
    }

    public static T DeepCopy<T>(object obj)
    {
        return (T) DeepCopy(obj, typeof (T));
    }

    public static object DeepCopy(object obj, Type targetType)
    {
        if (obj == null) return null;
        Type type = obj.GetType();

        if (OnlineMapsReflectionHelper.IsValueType(type) || type == typeof(string)) return obj;
        if (type.IsArray)
        {
            Type elementType = Type.GetType(targetType.FullName.Replace("[]", string.Empty));
            Array array = obj as Array;
            Array copied = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++) copied.SetValue(DeepCopy(array.GetValue(i), elementType), i);
            return copied;
        }
        if (OnlineMapsReflectionHelper.IsClass(type))
        {
            object target = Activator.CreateInstance(targetType);
            IEnumerable<FieldInfo> fields = OnlineMapsReflectionHelper.GetFields(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(obj);
                if (fieldValue == null) continue;
                field.SetValue(target, DeepCopy(fieldValue, field.FieldType));
            }
            return target;
        }
        throw new ArgumentException("Unknown type");
    }

    /// <summary>
    /// Converts Polyline to point list.
    /// </summary>
    /// <param name="encodedPoints">
    /// The encoded polyline.
    /// </param>
    /// <returns>
    /// A List of Vector2 points;
    /// </returns>
    public static List<Vector2> DecodePolylinePoints(string encodedPoints)
    {
        if (string.IsNullOrEmpty(encodedPoints)) return null;

        List<Vector2> poly = new List<Vector2>();
        char[] polylinechars = encodedPoints.ToCharArray();
        int index = 0;

        int currentLat = 0;
        int currentLng = 0;
        int next5bits;

        try
        {
            while (index < polylinechars.Length)
            {
                int sum = 0;
                int shifter = 0;
                do
                {
                    next5bits = polylinechars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylinechars.Length);

                if (index >= polylinechars.Length)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : sum >> 1;

                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = polylinechars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylinechars.Length);

                if (index >= polylinechars.Length && next5bits >= 32) break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : sum >> 1;
                Vector2 p = new Vector2(Convert.ToSingle(currentLng) / 100000.0f, Convert.ToSingle(currentLat) / 100000.0f);
                poly.Add(p);
            }
        }
        catch { }
        return poly;
    }

    /// <summary>
    /// Converts Polyline to point list.
    /// </summary>
    /// <param name="encodedPoints">
    /// The encoded polyline.
    /// </param>
    /// <returns>
    /// A List of Vector2 points;
    /// </returns>
    public static List<OnlineMapsVector2d> DecodePolylinePointsD(string encodedPoints)
    {
        if (string.IsNullOrEmpty(encodedPoints)) return null;

        List<OnlineMapsVector2d> poly = new List<OnlineMapsVector2d>();
        char[] polylinechars = encodedPoints.ToCharArray();
        int index = 0;

        int currentLat = 0;
        int currentLng = 0;
        int next5bits;

        try
        {
            while (index < polylinechars.Length)
            {
                int sum = 0;
                int shifter = 0;
                do
                {
                    next5bits = polylinechars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylinechars.Length);

                if (index >= polylinechars.Length)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : sum >> 1;

                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = polylinechars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylinechars.Length);

                if (index >= polylinechars.Length && next5bits >= 32) break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : sum >> 1;
                OnlineMapsVector2d p = new OnlineMapsVector2d(Convert.ToDouble(currentLng) / 100000.0, Convert.ToDouble(currentLat) / 100000.0);
                poly.Add(p);
            }
        }
        catch { }
        return poly;
    }

    /// <summary>
    /// Removes a gameobject, component or asset.
    /// </summary>
    /// <param name="obj">The object to destroy.</param>
    public static void Destroy(Object obj)
    {
        if (obj == null) return;

#if UNITY_EDITOR
        if (OnlineMaps.isPlaying)
        {
            if (obj.GetInstanceID() < 0) Object.Destroy(obj);
        }
        else Object.DestroyImmediate(obj);
#else
        Object.Destroy(obj);
#endif
    }

    /// <summary>
    /// The distance between two geographical coordinates.
    /// </summary>
    /// <param name="point1">Coordinate (X - Lng, Y - Lat)</param>
    /// <param name="point2">Coordinate (X - Lng, Y - Lat)</param>
    /// <returns>Distance (km).</returns>
    public static Vector2 DistanceBetweenPoints(Vector2 point1, Vector2 point2)
    {
        double scfY = Math.Sin(point1.y * Deg2Rad);
        double sctY = Math.Sin(point2.y * Deg2Rad);
        double ccfY = Math.Cos(point1.y * Deg2Rad);
        double cctY = Math.Cos(point2.y * Deg2Rad);
        double cX = Math.Cos((point1.x - point2.x) * Deg2Rad);
        double sizeX1 = Math.Abs(R * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
        double sizeX2 = Math.Abs(R * Math.Acos(sctY * sctY + cctY * cctY * cX));
        float sizeX = (float)((sizeX1 + sizeX2) / 2.0);
        float sizeY = (float)(R * Math.Acos(scfY * sctY + ccfY * cctY));
        if (float.IsNaN(sizeX)) sizeX = 0;
        if (float.IsNaN(sizeY)) sizeY = 0;
        return new Vector2(sizeX, sizeY);
    }

    /// <summary>
    /// The distance between two geographical coordinates.
    /// </summary>
    /// <param name="x1">Longitude 1.</param>
    /// <param name="y1">Latitude 1.</param>
    /// <param name="x2">Longitude 2.</param>
    /// <param name="y2">Latitude 2.</param>
    /// <param name="dx">Distance longitude (km).</param>
    /// <param name="dy">Distance latitude (km).</param>
    public static void DistanceBetweenPoints(double x1, double y1, double x2, double y2, out double dx, out double dy)
    {
        double scfY = Math.Sin(y1 * Deg2Rad);
        double sctY = Math.Sin(y2 * Deg2Rad);
        double ccfY = Math.Cos(y1 * Deg2Rad);
        double cctY = Math.Cos(y2 * Deg2Rad);
        double cX = Math.Cos((x1 - x2) * Deg2Rad);
        double sizeX1 = Math.Abs(R * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
        double sizeX2 = Math.Abs(R * Math.Acos(sctY * sctY + cctY * cctY * cX));
        dx = (sizeX1 + sizeX2) / 2.0;
        dy = R * Math.Acos(scfY * sctY + ccfY * cctY);
        if (double.IsNaN(dx)) dx = 0;
        if (double.IsNaN(dy)) dy = 0;
    }

    /// <summary>
    /// The distance between two geographical coordinates with altitude.
    /// </summary>
    /// <param name="x1">Longitude 1</param>
    /// <param name="y1">Latitude 1</param>
    /// <param name="a1">Altitude 1 (km)</param>
    /// <param name="x2">Longitude 2</param>
    /// <param name="y2">Latitude 2</param>
    /// <param name="a2">Altitude 2 (km)</param>
    /// <returns>Distance (km).</returns>
    public static double DistanceBetweenPoints(double x1, double y1, double a1, double x2, double y2, double a2)
    {
        double r = R + Math.Min(a1, a2);
        double scfY = Math.Sin(y1 * Deg2Rad);
        double sctY = Math.Sin(y2 * Deg2Rad);
        double ccfY = Math.Cos(y1 * Deg2Rad);
        double cctY = Math.Cos(y2 * Deg2Rad);
        double cX = Math.Cos((x1 - x2) * Deg2Rad);
        double sizeX1 = Math.Abs(r * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
        double sizeX2 = Math.Abs(r * Math.Acos(sctY * sctY + cctY * cctY * cX));
        double dx = (sizeX1 + sizeX2) / 2.0;
        double dy = r * Math.Acos(scfY * sctY + ccfY * cctY);
        if (double.IsNaN(dx)) dx = 0;
        if (double.IsNaN(dy)) dy = 0;
        double d = Math.Sqrt(dx * dx + dy * dy);
        double hd = Math.Abs(a1 - a2);
        return Math.Sqrt(d * d + hd * hd);
    }

    /// <summary>
    /// The distance between geographical coordinates.
    /// </summary>
    /// <param name="points">IEnumerate of double, float, Vector2 or Vector3</param>
    /// <param name="dx">Distance longitude (km).</param>
    /// <param name="dy">Distance latitude (km).</param>
    /// <returns>Distance (km).</returns>
    public static double DistanceBetweenPoints(IEnumerable points, out double dx, out double dy)
    {
        dx = 0;
        dy = 0;

        object v1 = null;
        object pv1 = null;
        object pv2 = null;
        bool isV1 = true;
        bool isFirst = true;

        int type = -1; // 0 - double, 1 - float, 2 - Vector2, 3 - Vector3

        foreach (object p in points)
        {
            if (type == -1)
            {
                if (p is double) type = 0;
                else if (p is float) type = 1;
                else if (p is Vector2) type = 2;
                else if (p is Vector3) type = 3;
                else throw new Exception("Unknown type of points. Must be IEnumerable<double>, IEnumerable<float> or IEnumerable<Vector2>.");
            }

            if (type == 0 || type == 1)
            {
                if (isV1) v1 = p;
                else
                {
                    object v2 = p;
                    if (isFirst) isFirst = false;
                    else
                    {
                        double ox, oy;
                        if (type == 0) DistanceBetweenPoints((double) pv1, (double) pv2, (double) v1, (double) v2, out ox, out oy);
                        else DistanceBetweenPoints((float)pv1, (float)pv2, (float)v1, (float)v2, out ox, out oy);
                        dx += ox;
                        dy += oy;
                    }
                    pv1 = v1;
                    pv2 = v2;
                }

                isV1 = !isV1;
            }
            else
            {
                if (isFirst) isFirst = false;
                else
                {
                    Vector2 d;
                    if (type == 2) d = DistanceBetweenPoints((Vector2)pv1, (Vector2)p);
                    else d = DistanceBetweenPoints((Vector3)pv1, (Vector3)p);
                    dx += d.x;
                    dy += d.y;
                }
                pv1 = p;
            }
        }

        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// The distance between two geographical coordinates.
    /// </summary>
    /// <param name="point1">Coordinate (X - Lng, Y - Lat)</param>
    /// <param name="point2">Coordinate (X - Lng, Y - Lat)</param>
    /// <returns>Distance (km).</returns>
    public static double DistanceBetweenPointsD(Vector2 point1, Vector2 point2)
    {
        double scfY = Math.Sin(point1.y * Deg2Rad);
        double sctY = Math.Sin(point2.y * Deg2Rad);
        double ccfY = Math.Cos(point1.y * Deg2Rad);
        double cctY = Math.Cos(point2.y * Deg2Rad);
        double cX = Math.Cos((point1.x - point2.x) * Deg2Rad);
        double sizeX1 = Math.Abs(R * Math.Acos(scfY * scfY + ccfY * ccfY * cX));
        double sizeX2 = Math.Abs(R * Math.Acos(sctY * sctY + cctY * cctY * cX));
        double sizeX = (sizeX1 + sizeX2) / 2.0;
        double sizeY = R * Math.Acos(scfY * sctY + ccfY * cctY);
        if (double.IsNaN(sizeX)) sizeX = 0;
        if (double.IsNaN(sizeY)) sizeY = 0;
        return Math.Sqrt(sizeX * sizeX + sizeY * sizeY);
    }

    public static double Dot(double lx, double ly, double rx, double ry)
    {
        return lx * rx + ly * ry;
    }

    /// <summary>
    /// Fix geographic coordinates.
    /// </summary>
    /// <param name="v">Coordinates for fix.</param>
    /// <returns>Correct geographic coordinates.</returns>
    public static Vector2 FixAngle(Vector2 v)
    {
        float y = v.y;
        if (y < -90) y = -90;
        else if (y > 90) y = 90;
        return new Vector2(Mathf.Repeat(v.x + 180, 360) - 180, y);
    }

    /// <summary>
    /// Flip the negative dimensions of the rect.
    /// </summary>
    /// <param name="r">Rect.</param>
    public static void FlipNegative(ref Rect r)
    {
        if (r.width < 0) r.x -= r.width *= -1;
        if (r.height < 0) r.y -= r.height *= -1;
    }

    /// <summary>
    /// Get the center point and best zoom for the array of markers.
    /// </summary>
    /// <param name="markers">Array of markers.</param>
    /// <param name="center">Center point.</param>
    /// <param name="zoom">Best zoom.</param>
    /// <param name="inset">Inset for finding a cropped zoom level.</param>
    public static void GetCenterPointAndZoom(OnlineMapsMarkerBase[] markers, out Vector2 center, out int zoom, Vector2 inset = default(Vector2))
    {
        center = new Vector2();
        zoom = OnlineMaps.MINZOOM;
        if (markers == null || markers.Length == 0) return;

        OnlineMaps map = OnlineMaps.instance;
        OnlineMapsProjection projection = map.projection;

        double mx, my;
        markers[0].GetPosition(out mx, out my);
        double minX = mx;
        double minY = my;
        double maxX = mx;
        double maxY = my;

        for (int i = 1; i < markers.Length; i++)
        {
            OnlineMapsMarkerBase marker = markers[i];
            marker.GetPosition(out mx, out my);

            double rx = mx - minX;
            if (rx > 180) mx -= 360;
            else if (rx < -180) mx += 360;

            if (mx < minX) minX = mx;
            if (mx > maxX) maxX = mx;

            if (my < minY) minY = my;
            if (my > maxY) maxY = my;
        }

        double sx = maxX - minX;
        double sy = maxY - minY;

        center = new Vector2((float)(sx / 2 + minX), (float)(sy / 2 + minY));

        if (center.x < -180) center.x += 360;
        else if (center.x > 180) center.x -= 360;

        int width = map.width;
        int height = map.height;
        double xTileOffset = inset.x * width;
        double yTileOffset = inset.y * height;


        float countX = width / (float)tileSize / 2;
        float countY = height / (float)tileSize / 2;

        bool useZoomMin = false;

        for (int z = OnlineMaps.MAXZOOM; z > OnlineMaps.MINZOOM; z--)
        {
            bool success = true;

            double cx, cy;
            projection.CoordinatesToTile(center.x, center.y, z, out cx, out cy);
            int _mx = 1 << z;
            int hx = 1 << (z - 1);

            for (int i = 0; i < markers.Length; i++)
            {
                OnlineMapsMarkerBase marker = markers[i];
                double px, py;
                marker.GetPosition(out px, out py);
                projection.CoordinatesToTile(px, py, z, out px, out py);

                if (px - cx < -hx) px += _mx;
                else if (px - cx > hx) px -= _mx;

                px -= cx - countX;
                py -= cy - countY;
                px *= tileSize;
                py *= tileSize;

                if (marker is OnlineMapsMarker)
                {
                    useZoomMin = true;
                    OnlineMapsMarker m = marker as OnlineMapsMarker;
                    OnlineMapsVector2i ip = m.GetAlignedPosition((int)px, (int)py);
                    if (ip.x < xTileOffset || ip.y < yTileOffset || ip.x + m.width > width - xTileOffset || ip.y + m.height > height - yTileOffset)
                    {
                        success = false;
                        break;
                    }
                }
                else if (marker is OnlineMapsMarker3D)
                {
                    if (px < xTileOffset || py < yTileOffset || px > width - xTileOffset || py > height - yTileOffset)
                    {
                        success = false;
                        break;
                    }
                }
                else
                {
                    throw new Exception("Wrong marker type");
                }
            }
            if (success)
            {
                zoom = z;
                if (useZoomMin) zoom -= 1;
                return;
            }
        }

        zoom = OnlineMaps.MINZOOM;
    }

    /// <summary>
    /// Get the center point and best zoom for the array of coordinates.
    /// </summary>
    /// <param name="positions">Array of coordinates</param>
    /// <param name="center">Center coordinate</param>
    /// <param name="zoom">Best zoom</param>
    /// <param name="inset">Inset for finding a cropped zoom level.</param>
    public static void GetCenterPointAndZoom(Vector2[] positions, out Vector2 center, out int zoom, Vector2 inset = default(Vector2))
    {
        center = new Vector2();
        zoom = OnlineMaps.MINZOOM;
        if (positions == null || positions.Length == 0) return;

        OnlineMaps map = OnlineMaps.instance;
        OnlineMapsProjection projection = map.projection;

        Vector2 p = positions[0];
        float minX = p.x;
        float minY = p.y;
        float maxX = p.x;
        float maxY = p.y;

        for (int i = 1; i < positions.Length; i++)
        {
            p = positions[i];
            float px = p.x;

            float rx = px - minX;
            if (rx > 180) px -= 360;
            else if (rx < -180) px += 360;

            if (px < minX) minX = px;
            if (px > maxX) maxX = px;

            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        float sx = maxX - minX;
        float sy = maxY - minY;

        center = new Vector2(sx / 2 + minX, sy / 2 + minY);

        if (center.x < -180) center.x += 360;
        else if (center.x > 180) center.x -= 360;

        int width = map.width;
        int height = map.height;
        double xTileOffset = inset.x * width;
        double yTileOffset = inset.y * height;

        float countX = width / (float)tileSize / 2;
        float countY = height / (float)tileSize / 2;

        for (int z = OnlineMaps.MAXZOOM; z > OnlineMaps.MINZOOM; z--)
        {
            bool success = true;

            int mx = 1 << z;
            int hx = 1 << (z - 1);
            double cx, cy;
            projection.CoordinatesToTile(center.x, center.y, z, out cx, out cy);

            for (int i = 0; i < positions.Length; i++)
            {
                Vector2 pos = positions[i];
                double px, py;
                projection.CoordinatesToTile(pos.x, pos.y, z, out px, out py);

                if (px - cx < -hx) px += mx;
                else if (px - cx > hx) px -= mx;

                px -= cx - countX;
                py -= cy - countY;
                px *= tileSize;
                py *= tileSize;

                if (px < xTileOffset || py < yTileOffset || px > width - xTileOffset || py > height - yTileOffset)
                {
                    success = false;
                    break;
                }
            }
            if (success)
            {
                zoom = z;
                return;
            }
        }

        zoom = OnlineMaps.MINZOOM;
    }

    /// <summary>
    /// Given a start point, angle and distance, this will calculate the destination point travelling along a (shortest distance) great circle arc.
    /// </summary>
    /// <param name="lng">Longitude of start point</param>
    /// <param name="lat">Latitude of start point</param>
    /// <param name="distance">Distance (km)</param>
    /// <param name="angle">Angle, clockwise from north (degree)</param>
    /// <param name="nlng">Longitude of destination point</param>
    /// <param name="nlat">Latitude of destination point</param>
    public static void GetCoordinateInDistance(double lng, double lat, float distance, float angle, out double nlng, out double nlat)
    {
        double d = distance / R;
        double a = angle * Deg2Rad;

        double f1 = lat * Deg2Rad;
        double l1 = lng * Deg2Rad;

        double sinF1 = Math.Sin(f1);
        double cosF1 = Math.Cos(f1);
        double sinD = Math.Sin(d);
        double cosD = Math.Cos(d);
        double sinA = Math.Sin(a);
        double cosA = Math.Cos(a);

        double sinF2 = sinF1 * cosD + cosF1 * sinD * cosA;
        double f2 = Math.Asin(sinF2);
        double y = sinA * sinD * cosF1;
        double x = cosD - sinF1 * sinF2;
        double l2 = l1 + Math.Atan2(y, x);

        nlat = f2 * Rad2Deg;
        nlng = (l2 * Rad2Deg + 540) % 360 - 180;
    }

    public static Vector2 GetIntersectionPointOfTwoLines(Vector2 p11, Vector2 p12, Vector2 p21, Vector2 p22, out int state)
    {
        Vector2 result = new Vector2();
        float m = (p22.x - p21.x) * (p11.y - p21.y) - (p22.y - p21.y) * (p11.x - p21.x);
        float n = (p22.y - p21.y) * (p12.x - p11.x) - (p22.x - p21.x) * (p12.y - p11.y);

        float Ua = m / n;

        if (Math.Abs(n) < float.Epsilon && Math.Abs(m) > float.Epsilon) state = -1;
        else if (Math.Abs(m) < float.Epsilon && Math.Abs(n) < float.Epsilon) state = 0;
        else
        {
            result.x = p11.x + Ua * (p12.x - p11.x);
            result.y = p11.y + Ua * (p12.y - p11.y);
            state = 1;
        }
        return result;
    }

    public static int GetIntersectionPointOfTwoLines(float p11x, float p11y, float p12x, float p12y, float p21x, float p21y, float p22x, float p22y, out float resultx, out float resulty)
    {
        int state;
        resultx = 0;
        resulty = 0;
        
        float m = (p22x - p21x) * (p11y - p21y) - (p22y - p21y) * (p11x - p21x);
        float n = (p22y - p21y) * (p12x - p11x) - (p22x - p21x) * (p12y - p11y);

        float Ua = m / n;

        if (Math.Abs(n) < float.Epsilon && Math.Abs(m) > float.Epsilon) state = -1;
        else if (Math.Abs(m) < float.Epsilon && Math.Abs(n) < float.Epsilon) state = 0;
        else
        {
            resultx = p11x + Ua * (p12x - p11x);
            resulty = p11y + Ua * (p12y - p11y);
            state = 1;
        }
        return state;
    }

    public static Vector2 GetIntersectionPointOfTwoLines(Vector3 p11, Vector3 p12, Vector3 p21, Vector3 p22, out int state)
    {
        return GetIntersectionPointOfTwoLines(new Vector2(p11.x, p11.z), new Vector2(p12.x, p12.z), new Vector2(p21.x, p21.z), new Vector2(p22.x, p22.z), out state);
    }

    public static Object GetObject(int tid)
    {
#if UNITY_EDITOR
        if (tid == 0) return null;
        return EditorUtility.InstanceIDToObject(tid);
#else
        return null;
#endif
    }

    public static void GetValuesFromEnum(StringBuilder builder, string key, Type type, int value)
    {
        builder.Append("&").Append(key).Append("=");
        Array values = Enum.GetValues(type);

        bool addSeparator = false;
        for (int i = 0; i < values.Length; i++)
        {
            int v = (int)values.GetValue(i);
            if ((value & v) == v)
            {
                if (addSeparator) builder.Append(",");
                builder.Append(Enum.GetName(type, v));
                addSeparator = true;
            }
        }
    }

    /// <summary>
    /// Converts HEX string to color.
    /// </summary>
    /// <param name="hex">HEX string</param>
    /// <returns>Color</returns>
    public static Color HexToColor(string hex)
    {
        byte r = Byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte g = Byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = Byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    public static bool Intersect(Rect a, Rect b)
    {
        FlipNegative(ref a);
        FlipNegative(ref b);
        if (a.xMin >= b.xMax) return false;
        if (a.xMax <= b.xMin) return false;
        if (a.yMin >= b.yMax) return false;
        if (a.yMax <= b.yMin) return false;

        return true;
    }

    public static bool LineIntersection(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2, out Vector2 out_intersection)
    {
        out_intersection = Vector2.zero;

        Vector2 dir1 = end1 - start1;
        Vector2 dir2 = end2 - start2;

        float a1 = -dir1.y;
        float b1 = +dir1.x;
        float d1 = -(a1 * start1.x + b1 * start1.y);

        float a2 = -dir2.y;
        float b2 = +dir2.x;
        float d2 = -(a2 * start2.x + b2 * start2.y);

        float seg1_line2_start = a2 * start1.x + b2 * start1.y + d2;
        float seg1_line2_end = a2 * end1.x + b2 * end1.y + d2;

        float seg2_line1_start = a1 * start2.x + b1 * start2.y + d1;
        float seg2_line1_end = a1 * end2.x + b1 * end2.y + d1;

        if (seg1_line2_start * seg1_line2_end >= 0 || seg2_line1_start * seg2_line1_end >= 0) return false;

        float u = seg1_line2_start / (seg1_line2_start - seg1_line2_end);
        out_intersection = start1 + u * dir1;

        return true;
    }

    public static bool LineIntersection(float s1x, float s1y, float e1x, float e1y, float s2x, float s2y, float e2x, float e2y, out float intX, out float intY)
    {
        intX = 0;
        intY = 0;

        float dir1x = e1x - s1x;
        float dir1y = e1y - s1y;
        float dir2x = e2x - s2x;
        float dir2y = e2y - s2y;

        float a1 = -dir1y;
        float b1 = +dir1x;
        float d1 = -(a1 * s1x + b1 * s1y);

        float a2 = -dir2y;
        float b2 = +dir2x;
        float d2 = -(a2 * s2x + b2 * s2y);

        float seg1_line2_start = a2 * s1x + b2 * s1y + d2;
        float seg1_line2_end = a2 * e1x + b2 * e1y + d2;

        float seg2_line1_start = a1 * s2x + b1 * s2y + d1;
        float seg2_line1_end = a1 * e2x + b1 * e2y + d1;

        if (seg1_line2_start * seg1_line2_end >= 0 || seg2_line1_start * seg2_line1_end >= 0) return false;

        float u = seg1_line2_start / (seg1_line2_start - seg1_line2_end);
        intX = s1x + u * dir1x;
        intY = s1y + u * dir1y;

        return true;
    }

    public static Vector2 LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        double x1lo, x1hi, y1lo, y1hi;

        double Ax = p2.x - p1.x;
        double Bx = p3.x - p4.x;

        if (Ax < 0)
        {
            x1lo = p2.x; 
            x1hi = p1.x;
        }
        else
        {
            x1hi = p2.x; 
            x1lo = p1.x;
        }

        if (Bx > 0)
        {
            if (x1hi < p4.x || p3.x < x1lo) return Vector2.zero;
        }
        else
        {
            if (x1hi < p3.x || p4.x < x1lo) return Vector2.zero;
        }

        double Ay = p2.y - p1.y;
        double By = p3.y - p4.y;

        if (Ay < 0)
        {
            y1lo = p2.y; 
            y1hi = p1.y;
        }
        else
        {
            y1hi = p2.y; 
            y1lo = p1.y;
        }

        if (By > 0)
        {
            if (y1hi < p4.y || p3.y < y1lo) return Vector2.zero;
        }
        else
        {
            if (y1hi < p3.y || p4.y < y1lo) return Vector2.zero;
        }

        double Cx = p1.x - p3.x;
        double Cy = p1.y - p3.y;
        double d = By * Cx - Bx * Cy;
        double f = Ay * Bx - Ax * By;

        if (f > 0)
        {
            if (d < 0 || d > f) return Vector2.zero;
        }
        else
        {
            if (d > 0 || d < f) return Vector2.zero;
        }

        double e = Ax * Cy - Ay * Cx;

        if (f > 0)
        {
            if (e < 0 || e > f) return Vector2.zero;
        }
        else
        {
            if (e > 0 || e < f) return Vector2.zero;
        }

        if (Math.Abs(f) < double.Epsilon) return Vector2.zero;

        Vector2 intersection;

        double num = d * Ax;
        double offset = same_sign(num, f) ? f * 0.5 : -f * 0.5;
        intersection.x = (float)(p1.x + (num + offset) / f);

        num = d * Ay;
        offset = same_sign(num, f) ? f * 0.5 : -f * 0.5;
        intersection.y = (float)(p1.y + (num + offset) / f);

        return intersection;
    }

    private static bool same_sign(double a, double b)
    {
        return a * b >= 0f;
    }

    public static bool IsPointInPolygon(List<Vector2> poly, float x, float y)
    {
        int i, j;
        bool c = false;
        for (i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            if (((poly[i].y <= y && y < poly[j].y) || (poly[j].y <= y && y < poly[i].y)) && 
                x < (poly[j].x - poly[i].x) * (y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                c = !c;
        }
        return c;
    }

    public static bool IsPointInPolygon(IEnumerable poly, double x, double y)
    {
        //int i, j;
        bool c = false;
        int valueType = -1; // 0 - Vector2, 1 - float, 2 - double, 3 - OnlineMapsVector2d

        object firstValue = null;
        object secondValue = null;
        object v1 = null, v2 = null, v3 = null;

        int i = 0;

        foreach (object p in poly)
        {
            if (valueType == -1)
            {
                firstValue = p;
                if (p is Vector2) valueType = 0;
                else if (p is float) valueType = 1;
                else if (p is double) valueType = 2;
                else if (p is OnlineMapsVector2d) valueType = 3;
            }

            if (i == 1) secondValue = p;

            object v4 = v3;
            v3 = v2;
            v2 = v1;
            v1 = p;

            if (valueType == 0)
            {
                if (i > 0)
                {
                    Vector2 p1 = (Vector2) v2;
                    Vector2 p2 = (Vector2) v1;
                    if (((p2.y <= y && y < p1.y) || (p1.y <= y && y < p2.y)) && x < (p1.x - p2.x) * (y - p2.y) / (p1.y - p2.y) + p2.x) c = !c;
                }
            }
            else if (valueType == 3)
            {
                if (i > 0)
                {
                    OnlineMapsVector2d p1 = (OnlineMapsVector2d)v2;
                    OnlineMapsVector2d p2 = (OnlineMapsVector2d)v1;
                    if (((p2.y <= y && y < p1.y) || (p1.y <= y && y < p2.y)) && x < (p1.x - p2.x) * (y - p2.y) / (p1.y - p2.y) + p2.x) c = !c;
                }
            }
            else if (i > 2 && i % 2 == 1)
            {
                if (valueType == 1)
                {
                    float p1x = (float) v4;
                    float p1y = (float) v3;
                    float p2x = (float) v2;
                    float p2y = (float) v1;

                    if (((p2y <= y && y < p1y) || (p1y <= y && y < p2y)) && x < (p1x - p2x) * (y - p2y) / (p1y - p2y) + p2x) c = !c;
                }
                else if (valueType == 2)
                {
                    double p1x = (double)v4;
                    double p1y = (double)v3;
                    double p2x = (double)v2;
                    double p2y = (double)v1;

                    if (((p2y <= y && y < p1y) || (p1y <= y && y < p2y)) && x < (p1x - p2x) * (y - p2y) / (p1y - p2y) + p2x) c = !c;
                }
            }

            i++;
        }

        if (valueType == 0)
        {
            if (i > 0)
            {
                Vector2 p1 = (Vector2)v1;
                Vector2 p2 = (Vector2)firstValue;
                if (((p2.y <= y && y < p1.y) || (p1.y <= y && y < p2.y)) && x < (p1.x - p2.x) * (y - p2.y) / (p1.y - p2.y) + p2.x) c = !c;
            }
        }
        else if (valueType == 3)
        {
            if (i > 0)
            {
                OnlineMapsVector2d p1 = (OnlineMapsVector2d)v1;
                OnlineMapsVector2d p2 = (OnlineMapsVector2d)firstValue;
                if (((p2.y <= y && y < p1.y) || (p1.y <= y && y < p2.y)) && x < (p1.x - p2.x) * (y - p2.y) / (p1.y - p2.y) + p2.x) c = !c;
            }
        }
        else if (i > 2 && i % 2 == 1)
        {
            if (valueType == 1)
            {
                float p1x = (float)v2;
                float p1y = (float)v1;
                float p2x = (float)firstValue;
                float p2y = (float)secondValue;

                if (((p2y <= y && y < p1y) || (p1y <= y && y < p2y)) && x < (p1x - p2x) * (y - p2y) / (p1y - p2y) + p2x) c = !c;
            }
            else if (valueType == 2)
            {
                double p1x = (double)v2;
                double p1y = (double)v1;
                double p2x = (double)firstValue;
                double p2y = (double)secondValue;

                if (((p2y <= y && y < p1y) || (p1y <= y && y < p2y)) && x < (p1x - p2x) * (y - p2y) / (p1y - p2y) + p2x) c = !c;
            }
        }

        return c;
    }

    public static bool IsPointInPolygon(double[] poly, double x, double y)
    {
        int i, j;
        bool c = false;
        int l = poly.Length / 2;
        for (i = 0, j = l - 1; i < l; j = i++)
        {
            int i2 = i * 2;
            int j2 = j * 2;
            int i2p = i2 + 1;
            int j2p = j2 + 1;
            if (((poly[i2p] <= y && y < poly[j2p]) || (poly[j2p] <= y && y < poly[i2p])) && x < (poly[j2] - poly[i2]) * (y - poly[i2p]) / (poly[j2p] - poly[i2p]) + poly[i2]) c = !c;
        }
        return c;
    }

    /// <summary>
    /// Converts geographic coordinates to Mercator coordinates.
    /// </summary>
    /// <param name="x">Longitude</param>
    /// <param name="y">Latitude</param>
    /// <returns>Mercator coordinates</returns>
    public static Vector2 LatLongToMercat(float x, float y)
    {
        // TODO: Move to projection
        float sy = Mathf.Sin(y * Mathf.Deg2Rad);
        return new Vector2((x + 180) / 360, 0.5f - Mathf.Log((1 + sy) / (1 - sy)) / pi4);
    }

    /// <summary>
    /// Converts geographic coordinates to Mercator coordinates.
    /// </summary>
    /// <param name="x">Longitude</param>
    /// <param name="y">Latitude</param>
    public static void LatLongToMercat(ref float x, ref float y)
    {
        // TODO: Move to projection
        float sy = Mathf.Sin(y * Mathf.Deg2Rad);
        x = (x + 180) / 360;
        y = 0.5f - Mathf.Log((1 + sy) / (1 - sy)) / pi4;
    }

    /// <summary>
    /// Converts geographic coordinates to Mercator coordinates.
    /// </summary>
    /// <param name="x">Longitude</param>
    /// <param name="y">Latitude</param>
    public static void LatLongToMercat(ref double x, ref double y)
    {
        // TODO: Move to projection
        double sy = Math.Sin(y * Deg2Rad);
        x = (x + 180) / 360;
        y = 0.5 - Math.Log((1 + sy) / (1 - sy)) / (Math.PI * 4);
    }

    /// <summary>
    /// Returns the length of vector.
    /// </summary>
    /// <param name="p1x">Point 1 X</param>
    /// <param name="p1y">Point 1 Y</param>
    /// <param name="p2x">Point 2 X</param>
    /// <param name="p2y">Point 2 Y</param>
    /// <returns>Length of vector</returns>
    public static double Magnitude(double p1x, double p1y, double p2x, double p2y)
    {
        return Math.Sqrt((p2x - p1x) * (p2x - p1x) + (p2y - p1y) * (p2y - p1y));
    }

    public static Vector2 NearestPointStrict(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 fullDirection = lineEnd - lineStart;
        Vector2 lineDirection = fullDirection.normalized;
        float closestPoint = Vector2.Dot(point - lineStart, lineDirection) / Vector2.Dot(lineDirection, lineDirection);
        return lineStart + Mathf.Clamp(closestPoint, 0, fullDirection.magnitude) * lineDirection;
    }

    public static void NearestPointStrict(double pointX, double pointY, double lineStartX, double lineStartY, double lineEndX, double lineEndY, out double nearestPointX, out double nearestPointY)
    {
        double fdX = lineEndX - lineStartX;
        double fdY = lineEndY - lineStartY;
        double magnitude = Math.Sqrt(fdX * fdX + fdY * fdY);
        double ldX = fdX / magnitude;
        double ldY = fdY / magnitude;
        double lx = pointX - lineStartX;
        double ly = pointY - lineStartY;
        double closestPoint = (lx * ldX + ly * ldY) / (ldX * ldX + ldY * ldY);

        if (closestPoint < 0) closestPoint = 0;
        else if (closestPoint > magnitude) closestPoint = magnitude;

        nearestPointX = lineStartX + closestPoint * ldX;
        nearestPointY = lineStartY + closestPoint * ldY;
    }

    public static double Repeat(double n, double minValue, double maxValue)
    {
        if (double.IsInfinity(n) || double.IsInfinity(minValue) || double.IsInfinity(maxValue) || double.IsNaN(n) || double.IsNaN(minValue) || double.IsNaN(maxValue)) return n;

        double range = maxValue - minValue;
        while (n < minValue || n > maxValue)
        {
            if (n < minValue) n += range;
            else if (n > maxValue) n -= range;
        }
        return n;
    }

    public static double SqrMagnitude(double p1x, double p1y, double p2x, double p2y)
    {
        return (p2x - p1x) * (p2x - p1x) + (p2y - p1y) * (p2y - p1y);
    }

    public static string StrReplace(string str, string[] origin, string[] replace)
    {
        if (origin == null || replace == null) return str;

        for (int i = 0; i < Mathf.Min(origin.Length, replace.Length); i++) str = str.Replace(origin[i], replace[i]);
        return str;
    }

    public static void ThreadSleep(int millisecondsTimeout)
    {
#if !NETFX_CORE
        Thread.Sleep(millisecondsTimeout);
#else
        OnlineMapsThreadWINRT.Sleep(millisecondsTimeout);
#endif

    }

    /// <summary>
    /// Converts tile index to quadkey.
    /// What is the tiles and quadkey, and how it works, you can read here:
    /// http://msdn.microsoft.com/en-us/library/bb259689.aspx
    /// </summary>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <param name="zoom">Zoom</param>
    /// <returns>Quadkey</returns>
    public static string TileToQuadKey(int x, int y, int zoom)
    {
        StringBuilder quadKey = new StringBuilder();
        for (int i = zoom; i > 0; i--)
        {
            char digit = '0';
            int mask = 1 << (i - 1);
            if ((x & mask) != 0) digit++;
            if ((y & mask) != 0)
            {
                digit++;
                digit++;
            }
            quadKey.Append(digit);
        }
        return quadKey.ToString();
    }

    public static StringBuilder TileToQuadKey(int x, int y, int zoom, StringBuilder quadKey)
    {
        for (int i = zoom; i > 0; i--)
        {
            char digit = '0';
            int mask = 1 << (i - 1);
            if ((x & mask) != 0) digit++;
            if ((y & mask) != 0)
            {
                digit++;
                digit++;
            }
            quadKey.Append(digit);
        }
        return quadKey;
    }

    public static List<int> Triangulate(List<Vector2> points)
    {
        List<int> indices = new List<int>(18);

        int n = points.Count;
        if (n < 3) return indices;

        int[] V = new int[n];
        if (TriangulateArea(points) > 0)
        {
            for (int v = 0; v < n; v++) V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++) V[v] = n - 1 - v;
        }

        int nv = n;
        int count = 2 * nv;

        for (int v = nv - 1; nv > 2; )
        {
            if (count-- <= 0) return indices;

            int u = v;
            if (nv <= u) u = 0;
            v = u + 1;
            if (nv <= v) v = 0;
            int w = v + 1;
            if (nv <= w) w = 0;

            if (TriangulateSnip(points, u, v, w, nv, V))
            {
                int s, t;
                indices.Add(V[u]);
                indices.Add(V[v]);
                indices.Add(V[w]);
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices;
    }

    public static IEnumerable<int> Triangulate(float[] points, int countVertices, List<int> indices)
    {
        indices.Clear();

        int n = countVertices;
        if (n < 3) return indices;

        int[] V = new int[n];
        if (TriangulateArea(points, countVertices) > 0)
        {
            for (int v = 0; v < n; v++) V[v] = v;
        }
        else
        {
            for (int v = 0; v < n; v++) V[v] = n - 1 - v;
        }

        int nv = n;
        int count = 2 * nv;

        for (int v = nv - 1; nv > 2;)
        {
            if (count-- <= 0) return indices;

            int u = v;
            if (nv <= u) u = 0;
            v = u + 1;
            if (nv <= v) v = 0;
            int w = v + 1;
            if (nv <= w) w = 0;

            if (TriangulateSnip(points, u, v, w, nv, V))
            {
                int s, t;
                indices.Add(V[u]);
                indices.Add(V[v]);
                indices.Add(V[w]);
                for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                nv--;
                count = 2 * nv;
            }
        }

        indices.Reverse();
        return indices;
    }

    private static float TriangulateArea(List<Vector2> points)
    {
        int n = points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = points[p];
            Vector2 qval = points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return A * 0.5f;
    }

    private static float TriangulateArea(float[] points, int countVertices)
    {
        float A = 0.0f;
        int n = countVertices * 2;
        for (int p = n - 2, q = 0; q < n; p = q - 2)
        {
            float pvx = points[p];
            float pvy = points[p + 1];
            float qvx = points[q++];
            float qvy = points[q++];

            A += pvx * qvy - qvx * pvy;
        }
        return A * 0.5f;
    }

    private static bool TriangulateInsideTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        float bp = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
        float ap = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        float cp = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
        return bp >= 0.0f && cp >= 0.0f && ap >= 0.0f;
    }

    private static bool TriangulateInsideTriangle(float ax, float ay, float bx, float by, float cx, float cy, float px, float py)
    {
        float bp = (cx - bx) * (py - by) - (cy - by) * (px - bx);
        float ap = (bx - ax) * (py - ay) - (by - ay) * (px - ax);
        float cp = (ax - cx) * (py - cy) - (ay - cy) * (px - cx);
        return bp >= 0.0f && cp >= 0.0f && ap >= 0.0f;
    }

    private static bool TriangulateSnip(List<Vector2> points, int u, int v, int w, int n, int[] V)
    {
        Vector2 A = points[V[u]];
        Vector2 B = points[V[v]];
        Vector2 C = points[V[w]];
        if (Mathf.Epsilon > (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x)) return false;
        for (int p = 0; p < n; p++)
        {
            if (p == u || p == v || p == w) continue;
            if (TriangulateInsideTriangle(A, B, C, points[V[p]])) return false;
        }
        return true;
    }

    private static bool TriangulateSnip(float[] points, int u, int v, int w, int n, int[] V)
    {
        int iu = V[u] * 2;
        int iv = V[v] * 2;
        int iw = V[w] * 2;

        float ax = points[iu];
        float ay = points[iu + 1];
        float bx = points[iv];
        float by = points[iv + 1];
        float cx = points[iw];
        float cy = points[iw + 1];

        if (Mathf.Epsilon > (bx - ax) * (cy - ay) - (by - ay) * (cx - ax)) return false;

        for (int p = 0; p < n; p++)
        {
            if (p == u || p == v || p == w) continue;
            
            int ip = V[p] * 2;
            if (TriangulateInsideTriangle(ax, ay, bx, by, cx, cy, points[ip], points[ip + 1])) return false;
        }
        return true;
    }
}