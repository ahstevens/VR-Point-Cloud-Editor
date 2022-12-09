/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// HERE Routing API is a web service API that offers easy and fast routing for several regions in the world. <br/>
/// https://developer.here.com/rest-apis/documentation/routing/topics/resource-calculate-route.html
/// </summary>
public class OnlineMapsHereRoutingAPI: OnlineMapsTextWebService
{
    private OnlineMapsHereRoutingAPI(string app_id, string app_code, string apiKey, Waypoint origin, Waypoint destination, Waypoint[] via, Dictionary<string, string> extra)
    {
        if (string.IsNullOrEmpty(app_code)) app_code = OnlineMapsKeyManager.HereAppCode();
        if (string.IsNullOrEmpty(app_id)) app_id = OnlineMapsKeyManager.HereAppID();
        if (string.IsNullOrEmpty(apiKey)) apiKey = OnlineMapsKeyManager.HereApiKey();

        if (string.IsNullOrEmpty(apiKey) && 
           (string.IsNullOrEmpty(app_id) || string.IsNullOrEmpty(app_code)))
        {
            throw new Exception("HERE requires App ID + App Code or Api Key.");
        }

        if (origin == null) throw new Exception("Origin cannot be null.");
        if (destination == null) throw new Exception("Destination cannot be null.");

        StringBuilder builder = new StringBuilder("https://router.hereapi.com/v8/routes?");
        if (!string.IsNullOrEmpty(apiKey))
        {
            builder.Append("apiKey=").Append(apiKey);
        }
        else
        {
            builder.Append("app_code=").Append(app_code);
            builder.Append("&app_id=").Append(app_id);
        }

        builder.Append("&origin=");
        origin.GetURLKey(builder);
        
        builder.Append("&destination=");
        destination.GetURLKey(builder);

        if (via != null)
        {
            builder.Append("&via=");
            for (int i = 0; i < via.Length; i++)
            {
                if (i > 0) builder.Append(",");
                via[i].GetURLKey(builder);
            }
        }

        bool hasTransportMode = false;

        if (extra != null)
        {
            
            foreach (KeyValuePair<string, string> pair in extra)
            {
                builder.Append("&").Append(pair.Key).Append("=").Append(pair.Value);
                if (pair.Key == "transportMode") hasTransportMode = true;
            }
        }

        if (!hasTransportMode) builder.Append("&transportMode=car");

        Debug.Log(builder);
        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Creates a new request for a route search.
    /// </summary>
    /// <param name="apiKey">A 43-byte Base64 URL-safe encoded string used for the authentication of the client application.</param>
    /// <param name="waypoints">
    /// List of waypoints that define a route. The first element marks the start, the last the end point.<br/>
    /// Waypoints in between are interpreted as via points.
    /// </param>
    /// <param name="mode">The routing mode determines how the route is calculated.</param>
    /// <param name="p">Optional request parameters.</param>
    /// <returns>Query instance to HERE Routing API.</returns>

    public static OnlineMapsTextWebService Find(
        string apiKey,
        Waypoint origin,
        Waypoint destination,
        Waypoint[] via = null,
        Dictionary<string, string> extra = null
    )
    {
        return new OnlineMapsHereRoutingAPI(null,null, apiKey, origin, destination, via, extra);
    }

    /// <summary>
    /// Creates a new request for a route search.
    /// </summary>
    /// <param name="app_id">A 20 bytes Base64 URL-safe encoded string used for the authentication of the client application.</param>
    /// <param name="app_code">A 20 bytes Base64 URL-safe encoded string used for the authentication of the client application. </param>
    /// <param name="waypoints">
    /// List of waypoints that define a route. The first element marks the start, the last the end point.<br/>
    /// Waypoints in between are interpreted as via points.
    /// </param>
    /// <param name="mode">The routing mode determines how the route is calculated.</param>
    /// <param name="p">Optional request parameters.</param>
    /// <returns>Query instance to HERE Routing API.</returns>
    public static OnlineMapsTextWebService Find(
        string app_id,
        string app_code,
        Waypoint origin,
        Waypoint destination,
        Waypoint[] via = null,
        Dictionary<string, string> extra = null
        )
    {
        return new OnlineMapsHereRoutingAPI(app_id, app_code, null, origin, destination, via, extra);
    }

    /// <summary>
    /// Creates a new request for a route search.
    /// </summary>
    /// <param name="app_id">A 20 bytes Base64 URL-safe encoded string used for the authentication of the client application.</param>
    /// <param name="app_code">A 20 bytes Base64 URL-safe encoded string used for the authentication of the client application. </param>
    /// <param name="apiKey">A 43-byte Base64 URL-safe encoded string used for the authentication of the client application.</param>
    /// <param name="waypoints">
    /// List of waypoints that define a route. The first element marks the start, the last the end point.<br/>
    /// Waypoints in between are interpreted as via points.
    /// </param>
    /// <param name="mode">The routing mode determines how the route is calculated.</param>
    /// <param name="p">Optional request parameters.</param>
    /// <returns>Query instance to HERE Routing API.</returns>
    public static OnlineMapsTextWebService Find(
        Waypoint origin,
        Waypoint destination,
        Waypoint[] via,
        Dictionary<string, string> extra = null
    )
    {
        return new OnlineMapsHereRoutingAPI(null, null, null, origin, destination, via, extra);
    }

    public static OnlineMapsTextWebService Find(
        Waypoint origin,
        Waypoint destination,
        Dictionary<string, string> extra = null
    )
    {
        return new OnlineMapsHereRoutingAPI(null, null, null, origin, destination, null, extra);
    }

    /// <summary>
    /// Converts response string into an result object.
    /// </summary>
    /// <param name="response">Response of HERE Routing API.</param>
    /// <returns>Result object</returns>
    public static OnlineMapsHereRoutingAPIResult GetResult(string response)
    {
        try
        {
            OnlineMapsJSONItem json = OnlineMapsJSON.Parse(response);
            OnlineMapsHereRoutingAPIResult result = json.Deserialize<OnlineMapsHereRoutingAPIResult>();
            result.json = json;
            return result;
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
        }
        return null;
    }

    /// <summary>
    /// GeoWaypoint defines a waypoint by latitude and longitude coordinates, and an optional radius.
    /// </summary>
    public class Waypoint
    {
        /// <summary>
        /// Latitude WGS-84 degrees between -90 and 90.
        /// </summary>
        public double latitude;

        /// <summary>
        /// Longitude WGS-84 degrees between -180 and 180. 
        /// </summary>
        public double longitude;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="longitude">Longitude</param>
        /// <param name="latitude">Latitude</param>
        public Waypoint(double longitude, double latitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public void GetURLKey(StringBuilder builder)
        {
            builder.Append(latitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(longitude.ToString(OnlineMapsUtils.numberFormat));
        }
    }
}