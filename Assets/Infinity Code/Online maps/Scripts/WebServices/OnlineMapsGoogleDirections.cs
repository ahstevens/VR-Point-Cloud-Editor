/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// This class is used to search for a route by address or coordinates.<br/>
/// You can create a new instance using OnlineMapsGoogleDirections.Find.<br/>
/// https://developers.google.com/maps/documentation/directions/intro
/// </summary>
public class OnlineMapsGoogleDirections : OnlineMapsTextWebService
{
    /// <summary>
    /// Request parameters.
    /// </summary>
    public Params requestParams;

    protected OnlineMapsGoogleDirections()
    {

    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="key">Google API key</param>
    /// <param name="origin">The address (string), coordinates (Vector2, OnlineMapsVector2d), or place ID (string prefixed with place_id:) from which you wish to calculate directions.</param>
    /// <param name="destination">The address (string), coordinates (Vector2, OnlineMapsVector2d), or place ID (string prefixed with place_id:) to which you wish to calculate directions.</param>
    public OnlineMapsGoogleDirections(string key, object origin, object destination)
    {
        requestParams = new Params(origin, destination)
        {
            key = key
        };
    }

    private OnlineMapsGoogleDirections(Params p)
    {
        requestParams = p;
        Send();
    }

    /// <summary>
    /// Creates a new request for a route search.
    /// </summary>
    /// <param name="p">Parameters of request.</param>
    /// <returns>Query instance to the Google API.</returns>
    public static OnlineMapsGoogleDirections Find(Params p)
    {
        return new OnlineMapsGoogleDirections(p);
    }

    /// <summary>
    /// Converts the response string to a result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    public static OnlineMapsGoogleDirectionsResult GetResult(string response)
    {
        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response);
            return new OnlineMapsGoogleDirectionsResult(xml);
        }
        catch { }

        return null;
    }

    public void Send()
    {
        if (_status != OnlineMapsQueryStatus.idle) return;
        _status = OnlineMapsQueryStatus.downloading;

        Params p = requestParams;

        StringBuilder url = new StringBuilder();
        url.Append("https://maps.googleapis.com/maps/api/directions/xml?sensor=false");
        url.Append("&origin=");

        if (p.origin is string) url.Append(OnlineMapsWWW.EscapeURL(p.origin as string));
        else if (p.origin is Vector2)
        {
            Vector2 o = (Vector2)p.origin;
            url.Append(o.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(o.x.ToString(OnlineMapsUtils.numberFormat));
        }
        else if (p.origin is OnlineMapsVector2d)
        {
            OnlineMapsVector2d o = (OnlineMapsVector2d)p.origin;
            url.Append(o.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(o.x.ToString(OnlineMapsUtils.numberFormat));
        }
        else throw new Exception("Origin must be string, Vector2 or OnlineMapsVector2d.");

        url.Append("&destination=");

        if (p.destination is string) url.Append(OnlineMapsWWW.EscapeURL(p.destination as string));
        else if (p.destination is Vector2)
        {
            Vector2 d = (Vector2)p.destination;
            url.Append(d.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(d.x.ToString(OnlineMapsUtils.numberFormat));
        }
        else if (p.destination is OnlineMapsVector2d)
        {
            OnlineMapsVector2d d = (OnlineMapsVector2d)p.destination;
            url.Append(d.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(d.x.ToString(OnlineMapsUtils.numberFormat));
        }
        else throw new Exception("Destination must be string, Vector2 or OnlineMapsVector2d.");

        if (p.mode.HasValue && p.mode.Value != Mode.driving) url.Append("&mode=").Append(Enum.GetName(typeof(Mode), p.mode.Value));
        if (p.waypoints != null)
        {
            StringBuilder waypointStr = new StringBuilder();
            bool isFirst = true;
            int countWaypoints = 0;
            foreach (object w in p.waypoints)
            {
                if (countWaypoints >= 8)
                {
                    Debug.LogWarning("The maximum number of waypoints is 8.");
                    break;
                }

                if (!isFirst) waypointStr = waypointStr.Append("|");

                if (w is string) waypointStr.Append(OnlineMapsWWW.EscapeURL(w as string));
                else if (w is Vector2)
                {
                    Vector2 v = (Vector2)w;
                    waypointStr.Append(v.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                        .Append(v.x.ToString(OnlineMapsUtils.numberFormat));
                }
                else if (w is OnlineMapsVector2d)
                {
                    OnlineMapsVector2d v = (OnlineMapsVector2d)w;
                    waypointStr.Append(v.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                        .Append(v.x.ToString(OnlineMapsUtils.numberFormat));
                }
                else throw new Exception("Waypoints must be string, Vector2 or OnlineMapsVector2d.");

                countWaypoints++;

                isFirst = false;
            }

            if (countWaypoints > 0) url.Append("&waypoints=optimize:true|").Append(waypointStr);
        }
        if (p.alternatives) url.Append("&alternatives=true");
        if (p.avoid.HasValue && p.avoid.Value != Avoid.none) url.Append("&avoid=").Append(Enum.GetName(typeof(Avoid), p.avoid.Value));
        if (p.units.HasValue && p.units.Value != Units.metric) url.Append("&units=").Append(Enum.GetName(typeof(Units), p.units.Value));
        if (!string.IsNullOrEmpty(p.region)) url.Append("&region=").Append(p.region);
        if (p.departure_time != null) url.Append("&departure_time=").Append(p.departure_time);
        if (p.arrival_time.HasValue && p.arrival_time.Value > 0) url.Append("&arrival_time=").Append(p.arrival_time.Value);
        if (!string.IsNullOrEmpty(p.language)) url.Append("&language=").Append(p.language);
        if (!string.IsNullOrEmpty(p.key)) url.Append("&key=").Append(p.key);
        else if (OnlineMapsKeyManager.hasGoogleMaps) url.Append("&key=").Append(OnlineMapsKeyManager.GoogleMaps());

        if (p.traffic_model.HasValue && p.traffic_model.Value != TrafficModel.bestGuess) url.Append("&traffic_model=").Append(Enum.GetName(typeof(TrafficModel), p.traffic_model.Value));
        if (p.transit_mode.HasValue) OnlineMapsUtils.GetValuesFromEnum(url, "transit_mode", typeof(TransitMode), (int)p.transit_mode.Value);
        if (p.transit_routing_preference.HasValue) url.Append("&transit_routing_preference=").Append(Enum.GetName(typeof(TransitRoutingPreference), p.transit_routing_preference.Value));

        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Request parameters.
    /// </summary>
    public class Params
    {
        /// <summary>
        /// The address (string), coordinates (Vector2, OnlineMapsVector2d), or place ID (string prefixed with place_id:) from which you wish to calculate directions.
        /// </summary>
        public object origin;

        /// <summary>
        /// The address (string), coordinates (Vector2, OnlineMapsVector2d), or place ID (string prefixed with place_id:) to which you wish to calculate directions.
        /// </summary>
        public object destination;

        /// <summary>
        /// Specifies the mode of transport to use when calculating directions. Default: driving.
        /// </summary>
        public Mode? mode;

        /// <summary>
        /// Specifies an IEnumerate of waypoints. Waypoints alter a route by routing it through the specified location(s).<br/>
        /// The maximum number of waypoints is 8. <br/>
        /// Each waypoint can be specified as a coordinates (Vector2, OnlineMapsVector2d), an encoded polyline (string prefixed with enc:), a place ID (string prefixed with place_id:), or an address which will be geocoded. 
        /// </summary>
        public IEnumerable waypoints;

        /// <summary>
        /// If set to true, specifies that the Directions service may provide more than one route alternative in the response.<br/>
        /// Note that providing route alternatives may increase the response time from the server.
        /// </summary>
        public bool alternatives;

        /// <summary>
        /// Indicates that the calculated route(s) should avoid the indicated features.
        /// </summary>
        public Avoid? avoid;

        /// <summary>
        /// Specifies the unit system to use when displaying results.
        /// </summary>
        public Units? units;

        /// <summary>
        /// Specifies the region code, specified as a ccTLD ("top-level domain") two-character value. 
        /// </summary>
        public string region;

        /// <summary>
        /// Specifies the desired time of arrival for transit directions, in seconds since midnight, January 1, 1970 UTC.<br/>
        /// You can specify either departure_time or arrival_time, but not both. 
        /// </summary>
        public long? arrival_time;

        /// <summary>
        /// Specifies the language in which to return results.
        /// </summary>
        public string language;

        /// <summary>
        /// Your application's API key. This key identifies your application for purposes of quota management.
        /// </summary>
        public string key;

        /// <summary>
        /// Specifies the assumptions to use when calculating time in traffic.
        /// </summary>
        public TrafficModel? traffic_model;

        /// <summary>
        /// Specifies one or more preferred modes of transit. This parameter may only be specified for transit directions.
        /// </summary>
        public TransitMode? transit_mode;

        /// <summary>
        /// Specifies preferences for transit routes. Using this parameter, you can bias the options returned, rather than accepting the default best route chosen by the API.<br/>
        /// This parameter may only be specified for transit directions.
        /// </summary>
        public TransitRoutingPreference? transit_routing_preference;

        private object _departure_time;

        /// <summary>
        /// Specifies the desired time of departure. You can specify the time as an integer in seconds since midnight, January 1, 1970 UTC.<br/>
        /// Alternatively, you can specify a value of now, which sets the departure time to the current time.
        /// </summary>
        public object departure_time
        {
            get { return _departure_time; }
            set { _departure_time = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="origin">The address (string), coordinates (Vector2, OnlineMapsVector2d), or place ID (string prefixed with place_id:) from which you wish to calculate directions.</param>
        /// <param name="destination">The address (string), coordinates (Vector2, OnlineMapsVector2d), or place ID (string prefixed with place_id:) to which you wish to calculate directions.</param>
        public Params(object origin, object destination)
        {
            if (!(origin is string || origin is Vector2 || origin is OnlineMapsVector2d)) throw new Exception("Origin must be string, Vector2 or OnlineMapsVector2d.");
            if (!(destination is string || destination is Vector2 || destination is OnlineMapsVector2d)) throw new Exception("Destination must be string, Vector2 or OnlineMapsVector2d.");

            this.origin = origin;
            this.destination = destination;
        }
    }

    /// <summary>
    /// Indicates that the calculated route(s) should avoid the indicated features.
    /// </summary>
    public enum Avoid
    {
        /// <summary>
        /// None avoid.
        /// </summary>
        none,

        /// <summary>
        /// Indicates that the calculated route should avoid toll roads/bridges.
        /// </summary>
        tolls,

        /// <summary>
        /// Indicates that the calculated route should avoid highways.
        /// </summary>
        highways,

        /// <summary>
        /// Indicates that the calculated route should avoid ferries.
        /// </summary>
        ferries
    }

    /// <summary>
    /// Mode of transport to use when calculating directions.
    /// </summary>
    public enum Mode
    {
        /// <summary>
        /// Indicates standard driving directions using the road network.
        /// </summary>
        driving,

        /// <summary>
        /// Requests walking directions via pedestrian paths & sidewalks (where available).
        /// </summary>
        walking,

        /// <summary>
        /// Requests bicycling directions via bicycle paths & preferred streets (where available).
        /// </summary>
        bicycling,

        /// <summary>
        /// Requests directions via public transit routes (where available).<br/>
        /// If you set the mode to transit, you can optionally specify either a departure_time or an arrival_time.<br/>
        /// If neither time is specified, the departure_time defaults to now (that is, the departure time defaults to the current time). 
        /// </summary>
        transit
    }

    /// <summary>
    /// Specifies the assumptions to use when calculating time in traffic.
    /// </summary>
    public enum TrafficModel
    {
        /// <summary>
        /// Indicates that the returned duration_in_traffic should be the best estimate of travel time given what is known about both historical traffic conditions and live traffic.
        /// </summary>
        bestGuess,

        /// <summary>
        /// Indicates that the returned duration_in_traffic should be longer than the actual travel time on most days, though occasional days with particularly bad traffic conditions may exceed this value. 
        /// </summary>
        pessimistic,

        /// <summary>
        /// Indicates that the returned duration_in_traffic should be shorter than the actual travel time on most days, though occasional days with particularly good traffic conditions may be faster than this value. 
        /// </summary>
        optimistic
    }

    /// <summary>
    /// Specifies one or more preferred modes of transit.
    /// </summary>
    [Flags]
    public enum TransitMode
    {
        /// <summary>
        /// Indicates that the calculated route should prefer travel by bus.
        /// </summary>
        bus = 1,

        /// <summary>
        /// Indicates that the calculated route should prefer travel by subway.
        /// </summary>
        subway = 2,

        /// <summary>
        /// Indicates that the calculated route should prefer travel by train.
        /// </summary>
        train = 4,

        /// <summary>
        /// Indicates that the calculated route should prefer travel by tram and light rail.
        /// </summary>
        tram = 8,

        /// <summary>
        /// Indicates that the calculated route should prefer travel by train, tram, light rail, and subway. This is equivalent to train|tram|subway.
        /// </summary>
        rail = 16
    }

    /// <summary>
    /// Specifies preferences for transit routes.
    /// </summary>
    public enum TransitRoutingPreference
    {
        /// <summary>
        /// Indicates that the calculated route should prefer limited amounts of walking.
        /// </summary>
        lessWalking,

        /// <summary>
        /// Indicates that the calculated route should prefer a limited number of transfers.
        /// </summary>
        fewerTransfers
    }

    /// <summary>
    /// Specifies the unit system to use when displaying results. 
    /// </summary>
    public enum Units
    {
        /// <summary>
        /// Specifies usage of the metric system. Textual distances are returned using kilometers and meters.
        /// </summary>
        metric,

        /// <summary>
        /// Specifies usage of the Imperial (English) system. Textual distances are returned using miles and feet.
        /// </summary>
        imperial
    }
}