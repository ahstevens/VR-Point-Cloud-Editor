/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// The Google Maps Roads API identifies the roads a vehicle was traveling along and provides additional metadata about those roads, such as speed limits.<br/>
/// https://developers.google.com/maps/documentation/roads/intro?hl=en
/// </summary>
public class OnlineMapsGoogleRoads:OnlineMapsTextWebService
{
    private OnlineMapsGoogleRoads(string key, IEnumerable path, bool interpolate)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.GoogleMaps();

        StringBuilder builder = new StringBuilder("https://roads.googleapis.com/v1/snapToRoads?key=").Append(key);
        if (interpolate) builder.Append("&intepolate=true");
        builder.Append("&path=");

        int type = -1;
        int i = -1;
        double longitude = 0;
        bool isFirst = true;

        foreach (object p in path)
        {
            i++;
            if (type == -1)
            {
                if (p is double) type = 0;
                else if (p is float) type = 1;
                else if (p is Vector2) type = 2;
                else throw new Exception("Unknown type of points. Must be IEnumerable<double>, IEnumerable<float> or IEnumerable<Vector2>.");
            }

            double latitude;
            if (type == 0 || type == 1)
            {
                if (i % 2 == 1)
                {
                    if (type == 0) latitude = (double)p;
                    else latitude = (float)p;
                }
                else
                {
                    if (type == 0) longitude = (double)p;
                    else longitude = (float)p;
                    continue;
                }
            }
            else
            {
                Vector2 v = (Vector2)p;
                longitude = v.x;
                latitude = v.y;
            }

            if (!isFirst) builder.Append("|");
            isFirst = false;
            builder.Append(latitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(longitude.ToString(OnlineMapsUtils.numberFormat));
        }
        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    private OnlineMapsGoogleRoads(string key, IEnumerable path, Units units = Units.KPH)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.GoogleMaps();

        StringBuilder builder = new StringBuilder("https://roads.googleapis.com/v1/speedLimits?key=").Append(key);
        if (units != Units.KPH) builder.Append("&units=MPH");

        int type = -1;
        int i = -1;
        double latitude = 0;
        double longitude = 0;
        bool isFirst = true;

        foreach (object p in path)
        {
            i++;
            if (type == -1)
            {
                if (p is double) type = 0;
                else if (p is float) type = 1;
                else if (p is Vector2) type = 2;
                else if (p is string) type = 3;
                else throw new Exception("Unknown type of points. Must be IEnumerable<double>, IEnumerable<float>, IEnumerable<Vector2> or IEnumerable<string>.");
            }

            if (type == 0 || type == 1)
            {
                if (i % 2 == 1)
                {
                    if (type == 0) latitude = (double)p;
                    else latitude = (float)p;
                }
                else
                {
                    if (type == 0) longitude = (double)p;
                    else longitude = (float)p;
                    continue;
                }
            }
            else if (type == 2)
            {
                Vector2 v = (Vector2)p;
                longitude = v.x;
                latitude = v.y;
            }

            if (type < 3)
            {
                if (isFirst)
                {
                    builder.Append("&path=");
                    isFirst = false;
                }
                else builder.Append("|");

                builder.Append(latitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(longitude.ToString(OnlineMapsUtils.numberFormat));
            }
            else builder.Append("&placeId=").Append((string) p);
        }

        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Convert response string into an array of SnapToRoadResult.
    /// </summary>
    /// <param name="response">Response string from Google Roads API</param>
    /// <returns>Array of SnapToRoadResult</returns>
    public static SnapToRoadResult[] GetSnapToRoadResults(string response)
    {
        OnlineMapsJSONObject json = OnlineMapsJSONObject.ParseObject(response);
        return GetSnapToRoadResults(json);
    }

    /// <summary>
    /// Convert OnlineMapsJSONObject into an array of SnapToRoadResult.
    /// </summary>
    /// <param name="json">OnlineMapsJSONObject from Google Roads API</param>
    /// <returns>Array of SnapToRoadResult</returns>
    private static SnapToRoadResult[] GetSnapToRoadResults(OnlineMapsJSONObject json)
    {
        return json["snappedPoints"].Deserialize<SnapToRoadResult[]>();
    }

    /// <summary>
    /// Convert response string into an array of SpeedLimitResult.
    /// </summary>
    /// <param name="response">Response string from Google Roads API</param>
    /// <returns>Array of SpeedLimitResult</returns>
    public static SpeedLimitResult[] GetSpeedLimitResults(string response)
    {
        OnlineMapsJSONObject json = OnlineMapsJSONObject.ParseObject(response);
        return GetSpeedLimitResults(json);
    }

    /// <summary>
    /// Convert OnlineMapsJSONObject into an array of SpeedLimitResult.
    /// </summary>
    /// <param name="json">OnlineMapsJSONObject from Google Roads API</param>
    /// <returns>Array of SpeedLimitResult</returns>
    private static SpeedLimitResult[] GetSpeedLimitResults(OnlineMapsJSONObject json)
    {
        return json["speedLimits"].Deserialize<SpeedLimitResult[]>();
    }

    /// <summary>
    /// This service returns the best-fit road geometry for a given set of GPS coordinates. <br/>
    /// This service takes up to 100 GPS points collected along a route, and returns a similar set of data with the points snapped to the most likely roads the vehicle was traveling along. <br/>
    /// Optionally, you can request that the points be interpolated, resulting in a path that smoothly follows the geometry of the road.<br/>
    /// https://developers.google.com/maps/documentation/roads/snap
    /// </summary>
    /// <param name="key">Your application's API key. Your application must identify itself every time it sends a request to the Google Maps Roads API by including an API key with each request.</param>
    /// <param name="path">
    /// The path to be snapped.<br/>
    /// IEnumerable values can be float, double or Vector2.
    /// </param>
    /// <param name="interpolate">
    /// Whether to interpolate a path to include all points forming the full road-geometry. <br/>
    /// When true, additional interpolated points will also be returned, resulting in a path that smoothly follows the geometry of the road, even around corners and through tunnels. <br/>
    /// Interpolated paths will most likely contain more points than the original path.
    /// </param>
    /// <returns>Instance of request.</returns>
    public static OnlineMapsGoogleRoads SnapToRoads(string key, IEnumerable path, bool interpolate = false)
    {
        return new OnlineMapsGoogleRoads(key, path, interpolate);
    }

    /// <summary>
    /// This service returns the posted speed limit for a road segment. <br/>
    /// The Speed Limit service is only available to Google Maps APIs Premium Plan customers.<br/>
    /// https://developers.google.com/maps/documentation/roads/speed-limits?hl=en
    /// </summary>
    /// <param name="key">Your application's API key. Your application must identify itself every time it sends a request to the Google Maps Roads API by including an API key with each request.</param>
    /// <param name="path">
    /// IEnumerable values can be float, double, Vector2 or string.<br/>
    /// If values is string, it must contain placeId.<br/>
    /// If values is float, double or Vector2, it must contain coordinates.
    /// </param>
    /// <param name="units">Whether to return speed limits in kilometers or miles per hour. This can be set to either KPH or MPH.</param>
    /// <returns>Instance of request.</returns>
    public static OnlineMapsGoogleRoads SpeedLimits(string key, IEnumerable path, Units units = Units.KPH)
    {
        return new OnlineMapsGoogleRoads(key, path, units);
    }

    /// <summary>
    /// Speed limits in kilometers or miles per hour.
    /// </summary>
    public enum Units
    {
        /// <summary>
        /// Kilometers per hour.
        /// </summary>
        KPH,

        /// <summary>
        /// Miles per hour.
        /// </summary>
        MPH
    }

    /// <summary>
    /// Snap to road result
    /// </summary>
    public class SnapToRoadResult
    {
        /// <summary>
        /// Contains a latitude and longitude value.
        /// </summary>
        public Location location;

        /// <summary>
        /// An integer that indicates the corresponding value in the original request. <br/>
        /// Each value in the request should map to a snapped value in the response. <br/>
        /// However, if you've set interpolate=true, then it's possible that the response will contain more coordinates than the request. <br/>
        /// Interpolated values will not have an originalIndex. <br/>
        /// These values are indexed from 0, so a point with an originalIndex of 4 will be the snapped value of the 5th latitude/longitude passed to the path parameter.
        /// </summary>
        public int originalIndex;

        /// <summary>
        /// A unique identifier for a place. All place IDs returned by the Google Maps Roads API correspond to road segments.
        /// </summary>
        public string placeId;
    }

    /// <summary>
    /// Speed limit result
    /// </summary>
    public class SpeedLimitResult
    {
        /// <summary>
        /// A unique identifier for a place. All place IDs returned by the Google Maps Roads API correspond to road segments.
        /// </summary>
        public string placeId;

        /// <summary>
        /// The speed limit for that road segment.
        /// </summary>
        public int speedLimit;

        /// <summary>
        /// Returns either KPH or MPH.
        /// </summary>
        public string units;
    }

    /// <summary>
    /// Latitude and longitude value
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Longitude
        /// </summary>
        public double longitude;

        /// <summary>
        /// Latitude
        /// </summary>
        public double latitude;
    }
}