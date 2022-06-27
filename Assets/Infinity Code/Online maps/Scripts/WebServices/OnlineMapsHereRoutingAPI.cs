/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
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
    private OnlineMapsHereRoutingAPI(string app_id, string app_code, string apiKey, Waypoint[] waypoints, RoutingMode mode, Params p)
    {
        if (string.IsNullOrEmpty(app_code)) app_code = OnlineMapsKeyManager.HereAppCode();
        if (string.IsNullOrEmpty(app_id)) app_id = OnlineMapsKeyManager.HereAppID();
        if (string.IsNullOrEmpty(apiKey)) apiKey = OnlineMapsKeyManager.HereApiKey();

        if (string.IsNullOrEmpty(apiKey) && 
           (string.IsNullOrEmpty(app_id) || string.IsNullOrEmpty(app_code)))
        {
            throw new Exception("HERE requires App ID + App Code or Api Key.");
        }

        if (waypoints == null || waypoints.Length < 2) throw new Exception("Requires 2 or more waypoints.");

        StringBuilder builder = new StringBuilder("https://route.ls.hereapi.com/routing/7.2/calculateroute.xml?");
        if (!string.IsNullOrEmpty(apiKey))
        {
            builder.Append("apiKey=").Append(apiKey);
        }
        else
        {
            builder.Append("app_code=").Append(app_code);
            builder.Append("&app_id=").Append(app_id);
        }


        for (int i = 0; i < waypoints.Length; i++)
        {
            builder.Append("&waypoint").Append(i).Append("=");
            waypoints[i].GetURLKey(builder);
        }

        mode.GetURLKey(builder);

        if (p != null) p.GetURLKey(builder);

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
        Waypoint[] waypoints,
        RoutingMode mode = null,
        Params p = null
    )
    {
        if (mode == null) mode = new RoutingMode();
        return new OnlineMapsHereRoutingAPI(null,null, apiKey, waypoints, mode, p);
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
        Waypoint[] waypoints,
        RoutingMode mode = null,
        Params p = null
        )
    {
        if (mode == null) mode = new RoutingMode();
        return new OnlineMapsHereRoutingAPI(app_id, app_code, null, waypoints, mode, p);
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
        string app_id,
        string app_code,
        string apiKey,
        Waypoint[] waypoints,
        RoutingMode mode = null,
        Params p = null
    )
    {
        if (mode == null) mode = new RoutingMode();
        return new OnlineMapsHereRoutingAPI(app_id, app_code, apiKey, waypoints, mode, p);
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
            OnlineMapsXML xml = OnlineMapsXML.Load(response);
            OnlineMapsXML rNode = xml["Response"];
            if (rNode.isNull) return null;
            return new OnlineMapsHereRoutingAPIResult(rNode);
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
        }
        return null;
    }

    /// <summary>
    /// The class contains the coordinates of the area boundaries.
    /// </summary>
    public class GeoRect: OnlineMapsGeoRect
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node">XMLNode from response of HERE Routing API</param>
        public GeoRect(OnlineMapsXML node)
        {
            OnlineMapsHereRoutingAPIResult.Route.GeoCoordinate tl = new OnlineMapsHereRoutingAPIResult.Route.GeoCoordinate(node["TopLeft"]);
            OnlineMapsHereRoutingAPIResult.Route.GeoCoordinate br = new OnlineMapsHereRoutingAPIResult.Route.GeoCoordinate(node["BottomRight"]);
            left = tl.longitude;
            right = br.longitude;
            top = tl.latitude;
            bottom = br.latitude;
        }
    }

    /// <summary>
    /// GeoWaypoint defines a waypoint by latitude and longitude coordinates, and an optional radius.
    /// </summary>
    public class GeoWaypoint: Waypoint
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
        /// Altitude in meters.
        /// </summary>
        public double? altitude;

        /// <summary>
        /// Matching Links are selected within the specified TransitRadius, in meters. <br/>
        /// For example to drive past a city without necessarily going into the city center you can specify the coordinates of the center and a TransitRadius of 5000m.
        /// </summary>
        public int? transitRadius;

        /// <summary>
        /// Custom label identifying this waypoint.
        /// </summary>
        public string userLabel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="longitude">Longitude</param>
        /// <param name="latitude">Latitude</param>
        /// <param name="altitude">Altitude</param>
        public GeoWaypoint(double longitude, double latitude, double? altitude = null)
        {
            this.latitude = latitude;
            this.longitude = longitude;
            if (altitude.HasValue) this.altitude = altitude.Value;
        }

        public override void GetURLKey(StringBuilder builder)
        {
            if (type.HasValue) builder.Append(type).Append("!");
            builder.Append(latitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(longitude.ToString(OnlineMapsUtils.numberFormat));
            if (altitude.HasValue) builder.Append(",").Append(altitude.Value.ToString(OnlineMapsUtils.numberFormat));
            if (transitRadius.HasValue) builder.Append(";").Append(transitRadius);
            if (!string.IsNullOrEmpty(userLabel))
            {
                if (!transitRadius.HasValue) builder.Append(";");
                builder.Append(";").Append(userLabel);
            }
        }
    }

    /// <summary>
    /// LinkWaypoint defines a waypoint by LinkId and optional Spot value. <br/>
    /// Spot is defined as the fractional distance from the link's reference-node to the non-reference node, with a value between 0 and 1. <br/>
    /// When no Spot value nor DisplayPosition is given in request then default value 0.5 is assumed<br/>
    /// The optional display position of the waypoint defines where the location is displayed on a map.<br/>
    /// It denotes the center of the location and is not navigable, i.e.it is not located on a link in the routing network in contrast to the navigation positions of a location. <br/>
    /// The display position allows the routing engine to decide whether the waypoint is located on the left or on the right-hand side of the route.<br/>
    /// </summary>
    public class LinkWaypoint : Waypoint
    {
        /// <summary>
        /// Latitude WGS-84 degrees between -90 and 90. 
        /// </summary>
        public double? displayLatitude;

        /// <summary>
        /// Longitude WGS-84 degrees between -180 and 180. 
        /// </summary>
        public double? displayLongitude;

        /// <summary>
        /// Altitude in meters. 
        /// </summary>
        public double? displayAltitude;

        /// <summary>
        /// Custom label identifying this waypoint.
        /// </summary>
        public string userLabel;

        /// <summary>
        /// Id of the link position with mandatory direction prefix (+,-,*).
        /// </summary>
        public string linkId;

        /// <summary>
        /// Optional relative position of the location along the link with a value between 0 and 1. <br/>
        /// When no spot value nor display position is given in the request then default value 0.5 is assumed.
        /// </summary>
        public string spot;

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="linkId">Link ID</param>
        public LinkWaypoint(string linkId)
        {
            this.linkId = linkId;
        }

        public override void GetURLKey(StringBuilder builder)
        {
            builder.Append("link!");
            if (type.HasValue) builder.Append(type).Append("!");

            if (displayLatitude.HasValue && displayLongitude.HasValue)
            {
                builder.Append(displayLatitude.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(displayLongitude.Value.ToString(OnlineMapsUtils.numberFormat));
                if (displayAltitude.HasValue) builder.Append(",").Append(displayAltitude.Value.ToString(OnlineMapsUtils.numberFormat));
            }

            if (!string.IsNullOrEmpty(userLabel)) builder.Append(";").Append(userLabel);

            builder.Append("!").Append(linkId);
            if (!string.IsNullOrEmpty(spot)) builder.Append(",").Append(spot);
        }
    }

    /// <summary>
    /// Optional request parameters.
    /// </summary>
    public class Params
    {
        /// <summary>
        /// Clients may pass in an arbitrary string to trace request processing through the system. 
        /// The RequestId is mirrored in the MetaInfo element of the response structure. 
        /// </summary>
        public string requestId;

        /// <summary>
        /// Areas which the route must not cross. 
        /// </summary>
        public GeoRect[] avoidAreas;

        /// <summary>
        /// Links which the route must not cross.
        /// </summary>
        public string[] avoidLinks;

        /// <summary>
        /// The optional avoid seasonal closures boolean flag can be specified to avoid usage of seasonally closed links.
        /// </summary>
        public bool avoidSeasonalClosures;

        /// <summary>
        /// Time when travel is expected to start. <br/>
        /// Traffic speed and incidents are taken into account when calculating the route. <br/>
        /// You can use now to specify the current time. Specify either departure or arrival, not both.
        /// </summary>
        public string departure;

        /// <summary>
        /// Time when travel is expected to end. <br/>
        /// Specify either departure or arrival, not both.
        /// </summary>
        public string arrival;

        /// <summary>
        /// Maximum number of alternative routes that will be calculated and returned. <br/>
        /// Alternative routes can be unavailable, thus they are not guaranteed to be returned. <br/>
        /// If at least one via point is used in a route request, returning alternative routes is not supported. <br/>
        /// 0 stands for "no alternative routes", i.e. only best route is returned.
        /// </summary>
        public int? alternatives;

        /// <summary>
        /// Defines the measurement system used in instruction text. <br/>
        /// When imperial is selected, units used are based on the language specified in the request. <br/>
        /// Defaults to metric when not specified.
        /// </summary>
        public MetricSystem? metricSystem;

        /// <summary>
        /// If the view bounds are given in the request then only route shape points which fit into these bounds will be returned. <br/>
        /// The route shape beyond the view bounds is reduced to the points which are referenced by links, legs or maneuvers. <br/>
        /// A common use case for this is the drag and drop scenario where the client is only interested in a rough visual update of the route in the currently visible bounds.
        /// </summary>
        public GeoRect viewBounds;

        /// <summary>
        /// Specifies the resolution of the view and a possible snap resolution in meters per pixel in the response. <br/>
        /// You must specify a whole, positive integer.<br/>
        /// If you specify only one value, then this value defines the view resolution only.<br/>
        /// You can use snap resolution to adjust waypoint links to the resolution of the client display.
        /// </summary>
        public string resolution;

        /// <summary>
        /// Defines the representation format of the maneuver's instruction text.
        /// </summary>
        public InstructionFormat? instructionFormat;

        /// <summary>
        /// A list of languages for all textual information, the first supported language is used. <br/>
        /// If there are no matching supported languages the response is an error. <br/>
        /// Defaults to en-us.<br/>
        /// See <see href="https://developer.here.com/rest-apis/documentation/routing/topics/resource-param-type-languages.html#languages">Languages</see> for a list of supported languages. 
        /// </summary>
        public string language;

        /// <summary>
        /// Define which elements are included in the response as part of the data representation of the route.
        /// </summary>
        public Representation? representation;

        /// <summary>
        /// Define which attributes are included in the response as part of the data representation of the route. <br/>
        /// Defaults to waypoints, summary, legs and additionally lines if publicTransport or publicTransportTimeTable mode is used. 
        /// </summary>
        public RouteAttributes? routeAttributes;

        /// <summary>
        /// Define which attributes are included in the response as part of the data representation of the route legs. <br/>
        /// Defaults to maneuvers, waypoint, length, travelTime.
        /// </summary>
        public LegAttributes? legAttributes;

        /// <summary>
        /// Define which attributes are included in the response as part of the data representation of the route maneuvers. <br/>
        /// Defaults to position, length, travelTime.
        /// </summary>
        public ManeuverAttributes? maneuverAttributes;

        /// <summary>
        /// Define which attributes are included in the response as part of the data representation of the route links. <br/>
        /// Defaults to shape, speedLimit.
        /// </summary>
        public LinkAttributes? linkAttributes;

        /// <summary>
        /// Sequence of attribute keys of the fields that are included in public transport line elements. <br/>
        /// If not specified, defaults to lineForeground, lineBackground.
        /// </summary>
        public LineAttributes? lineAttributes;

        /// <summary>
        /// Specifies the desired tolerances for generalizations of the base route geometry. <br/>
        /// Tolerances are given in degrees of longitude or latitude on a spherical approximation of the Earth. <br/>
        /// One meter is approximately equal to 0:00001 degrees at typical latitudes.
        /// </summary>
        public double[] generalizationtolerances;

        /// <summary>
        /// Specifies type of vehicle engine and average fuel consumption, which can be used to estimate CO2 emission for the route summary.
        /// </summary>
        public VehicleType vehicleType;

        /// <summary>
        /// Restricts number of changes in a public transport route to a given value. <br/>
        /// The parameter does not filter resulting alternatives. <br/>
        /// Instead, it affects route calculation so that only routes containing at most the given number of changes are considered. <br/>
        /// The provided value must be between 0 and 10.
        /// </summary>
        public int? maxNumberOfChanges;

        /// <summary>
        /// Public transport types that shall not be included in the response route.
        /// </summary>
        public PublicTransportType? avoidTransportTypes;

        /// <summary>
        /// Allows to prefer or avoid public transport routes with longer walking distances. <br/>
        /// A value > 1.0 means a slower walking speed and will prefer routes with less walking distance. <br/>
        /// The provided value must be between 0.01 and 100.
        /// </summary>
        public double? walkTimeMultiplier;

        /// <summary>
        /// Specifies speed which will be used by a service as a walking speed for pedestrian routing (meters per second). <br/>
        /// This parameter affects pedestrian, publicTransport and publicTransportTimetable modes. <br/>
        /// The provided value must be between 0.5 and 2.
        /// </summary>
        public double? walkSpeed;

        /// <summary>
        /// Allows the user to specify a maximum distance to the start and end stations of a public transit route. <br/>
        /// Only valid for publicTransport and publicTransportTimetable routes. <br/>
        /// The provided value must be between 0 and 6000. 
        /// </summary>
        public int? walkRadius;

        /// <summary>
        /// Enables the change maneuver in the route response, which indicates a public transit line change. <br/>
        /// In the absence of this maneuver, each line change is represented with a pair of subsequent enter and leave maneuvers. <br/>
        /// We recommend enabling combineChange behavior wherever possible, to simplify client-side development.
        /// </summary>
        public bool combineChange;

        /// <summary>
        /// Truck routing only, specifies the vehicle type. Defaults to truck. 
        /// </summary>
        public TruckType? truckType;

        /// <summary>
        /// Truck routing only, specifies number of trailers pulled by a vehicle. <br/>
        /// The provided value must be between 0 and 4. Defaults to 0. 
        /// </summary>
        public int? trailersCount;

        /// <summary>
        /// Truck routing only, list of hazardous materials in the vehicle.
        /// </summary>
        public HazardousGoodType? shippedHazardousGoods;

        /// <summary>
        /// Truck routing only, vehicle weight including trailers and shipped goods, in tons. <br/>
        /// The provided value must be between 0 and 1000.
        /// </summary>
        public int? limitedWeight;

        /// <summary>
        /// Truck routing only, vehicle weight per axle in tons. <br/>
        /// The provided value must be between 0 and 1000.
        /// </summary>
        public int? weightPerAxle;

        /// <summary>
        /// Truck routing only, vehicle height in meters. <br/>
        /// The provided value must be between 0 and 50.
        /// </summary>
        public int? height;

        /// <summary>
        /// Truck routing only, vehicle width in meters. <br/>
        /// The provided value must be between 0 and 50.
        /// </summary>
        public int? width;

        /// <summary>
        /// Truck routing only, vehicle length in meters. <br/>
        /// The provided value must be between 0 and 300.
        /// </summary>
        public int? length;

        /// <summary>
        /// Specifies the tunnel category to restrict certain route links. <br/>
        /// The route will pass only through tunnels of a less strict category.
        /// </summary>
        public string tunnelCategory;

        /// <summary>
        /// If set to true, all shapes inside routing response will consist of 3 values instead of 2. <br/>
        /// Third value will be elevation. If there are no elevation data available for given shape point, elevation will be interpolated from surrounding points. <br/>
        /// In case there is no elevation data available for any of the shape points, elevation will be 0.0.
        /// </summary>
        public bool returnelevation;

        public void GetURLKey(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(requestId)) builder.Append("&requestId=").Append(requestId);
            if (avoidAreas != null)
            {
                builder.Append("&avoidAreas=");
                for (int i = 0; i < avoidAreas.Length; i++)
                {
                    GeoRect area = avoidAreas[i];
                    builder.Append(area.top.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                        .Append(area.left.ToString(OnlineMapsUtils.numberFormat)).Append(";")
                        .Append(area.bottom.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                        .Append(area.right.ToString(OnlineMapsUtils.numberFormat));
                    if (i != avoidAreas.Length - 1) builder.Append("!");
                }
            }
            if (avoidLinks != null)
            {
                builder.Append("&avoidLinks=");
                for (int i = 0; i < avoidLinks.Length; i++)
                {
                    builder.Append(avoidLinks[i]);
                    if (i < avoidLinks.Length) builder.Append(",");
                }
            }
            if (avoidSeasonalClosures) builder.Append("&avoidSeasonalClosures=true");
            if (!string.IsNullOrEmpty(departure)) builder.Append("&departure=").Append(departure);
            if (!string.IsNullOrEmpty(arrival)) builder.Append("&arrival=").Append(arrival);
            if (alternatives.HasValue)
            {
                if (alternatives.Value < 0) throw new Exception("alternatives must be greater than or equal to 0.");
                builder.Append("&alternatives=").Append(alternatives.Value);
            }
            if (metricSystem.HasValue) builder.Append("&metricSystem=").Append(metricSystem.Value);
            if (viewBounds != null)
            {
                builder.Append("&viewBounds=")
                    .Append(viewBounds.top.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(viewBounds.left.ToString(OnlineMapsUtils.numberFormat)).Append(";")
                    .Append(viewBounds.bottom.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(viewBounds.right.ToString(OnlineMapsUtils.numberFormat));
            }
            if (!string.IsNullOrEmpty(resolution)) builder.Append("&resolution=").Append(resolution);
            if (instructionFormat.HasValue) builder.Append("&instructionFormat=").Append(instructionFormat.Value);
            if (!string.IsNullOrEmpty(language)) builder.Append("&language=").Append(language);
            if (representation.HasValue) builder.Append("&representation=").Append(representation.Value);
            if (routeAttributes.HasValue) GetShortNamesFromEnum(builder, "routeAttributes", typeof(RouteAttributes), (int)routeAttributes.Value);
            if (legAttributes.HasValue) GetShortNamesFromEnum(builder, "legAttributes", typeof(LegAttributes), (int)legAttributes.Value);
            if (maneuverAttributes.HasValue) GetShortNamesFromEnum(builder, "maneuverAttributes", typeof(ManeuverAttributes), (int)maneuverAttributes.Value);
            if (linkAttributes.HasValue) GetShortNamesFromEnum(builder, "linkAttributes", typeof(LinkAttributes), (int)linkAttributes.Value);
            if (lineAttributes.HasValue) GetShortNamesFromEnum(builder, "lineAttributes", typeof(LineAttributes), (int)lineAttributes.Value);
            if (generalizationtolerances != null)
            {
                if (generalizationtolerances.Length != 2) throw new Exception("generalizationtolerances must have two values. 0 - longitude tolerance, 1 - latitude tolerance.");
                builder.Append("&generalizationtolerances=")
                    .Append(generalizationtolerances[1].ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(generalizationtolerances[0].ToString(OnlineMapsUtils.numberFormat));
            }
            if (vehicleType != null) vehicleType.GetURLKey(builder);
            if (maxNumberOfChanges.HasValue) builder.Append("&maxNumberOfChanges=").Append(maxNumberOfChanges.Value);
            if (avoidTransportTypes.HasValue) OnlineMapsUtils.GetValuesFromEnum(builder, "avoidTransportTypes", typeof(PublicTransportType), (int)avoidTransportTypes.Value);
            if (walkTimeMultiplier.HasValue)
            {
                builder.Append("&walkTimeMultiplier=").Append(walkTimeMultiplier.Value.ToString(OnlineMapsUtils.numberFormat));
            }

            if (walkSpeed.HasValue)
            {
                builder.Append("&walkSpeed=").Append(walkSpeed.Value.ToString(OnlineMapsUtils.numberFormat));
            }
            if (walkRadius.HasValue) builder.Append("&walkRadius=").Append(walkRadius.Value);
            if (combineChange) builder.Append("&combineChange=true");
            if (truckType.HasValue) builder.Append("&truckType=").Append(truckType.Value);
            if (trailersCount.HasValue) builder.Append("&trailersCount=").Append(trailersCount.Value);
            if (shippedHazardousGoods.HasValue) OnlineMapsUtils.GetValuesFromEnum(builder, "shippedHazardousGoods", typeof(HazardousGoodType), (int)shippedHazardousGoods.Value);
            if (limitedWeight.HasValue) builder.Append("&limitedWeight=").Append(limitedWeight.Value);
            if (weightPerAxle.HasValue) builder.Append("&weightPerAxle=").Append(weightPerAxle.Value);
            if (height.HasValue) builder.Append("&height=").Append(height.Value);
            if (width.HasValue) builder.Append("&width=").Append(width.Value);
            if (length.HasValue) builder.Append("&length=").Append(length.Value);
            if (!string.IsNullOrEmpty(tunnelCategory)) builder.Append("&tunnelCategory=").Append(tunnelCategory);
            if (returnelevation) builder.Append("&returnelevation=true");
        }

        private void GetShortNamesFromEnum(StringBuilder builder, string key, Type type, int value)
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
                    MemberInfo firstInfo = OnlineMapsReflectionHelper.GetMember(type, Enum.GetName(type, v));
                    object[] attributes = firstInfo.GetCustomAttributes(typeof(ShortNameAttribute), false).ToArray();
                    builder.Append(((ShortNameAttribute)attributes[0]).shortName);
                    addSeparator = true;
                }
            }
        }
    }

    /// <summary>
    /// The RoutingMode specifies how the route is calculated.
    /// </summary>
    public class RoutingMode
    {
        /// <summary>
        /// RoutingType relevant to calculation.
        /// </summary>
        public Type type = Type.fastest;

        /// <summary>
        /// Specify which mode of transport to calculate the route for.
        /// </summary>
        public TransportModes transportMode = TransportModes.car;

        /// <summary>
        /// Specify whether to optimize a route for traffic.
        /// </summary>
        public TrafficMode? trafficMode;

        /// <summary>
        /// Route feature weightings to be applied when calculating the route.
        /// </summary>
        public Feature feature;

        /// <summary>
        /// RoutingType provides identifiers for different optimizations which can be applied during the route calculation. <br/>
        /// Selecting the routing type affects which constraints, speed attributes and weights are taken into account during route calculation.
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Route calculation from start to destination optimizing based on the travel time. <br/>
            /// Depending on the provided traffic mode of the request, this travel time can be determined with or without traffic information.
            /// </summary>
            fastest,

            /// <summary>
            /// Route calculation from start to destination disregarding any traffic conditions. <br/>
            /// In this mode, the distance of the route is minimized. 
            /// </summary>
            shortest
        }

        /// <summary>
        /// Depending on the transport mode special constraints, speed attributes and weights are taken into account during route calculation. 
        /// </summary>
        public enum TransportModes
        {
            /// <summary>
            /// Route calculation for cars. 
            /// </summary>
            car,

            /// <summary>
            /// Route calculation for a pedestrian. <br/>
            /// As one effect, maneuvers will be optimized for walking, i.e. segments will consider actions relevant for pedestrians and maneuver instructions will contain texts suitable for a walking person. <br/>
            /// This mode disregards any traffic information. 
            /// </summary>
            pedestrian,

            /// <summary>
            /// Route calculation for HOV (high-occupancy vehicle) cars. 
            /// </summary>
            carHOV,

            /// <summary>
            /// Route calculation using public transport lines and walking parts to get to stations. <br/>
            /// It is based on static map data, so the results are not aligned with officially published timetable. 
            /// </summary>
            publicTransport,

            /// <summary>
            /// Route calculation using public transport lines and walking parts to get to stations. <br/>
            /// This mode uses additional officially published timetable information to provide most precise routes and times. <br/>
            /// In case the timetable data is unavailable, the service will use estimated results based on static map data (same as from publicTransport mode). 
            /// </summary>
            publicTransportTimeTable,

            /// <summary>
            /// Route calculation for trucks. <br/>
            /// This mode considers truck limitations on links and uses different speed assumptions when calculating the route. 
            /// </summary>
            truck,

            /// <summary>
            /// Route calculation for bicycles. <br/>
            /// This mode uses the bicycle speed on links dedicated for both cars and pedestrians and the pedestrian speed for the roads that are only for pedestrians. 
            /// </summary>
            bicycle
        }

        public enum TrafficMode
        {
            /// <summary>
            /// No departure time provided: This behavior is deprecated and will return error in the future. <br/>
            /// * Static time based restrictions: Ignored <br/>
            /// * Real-time traffic closures: Valid for entire length of route. <br/>
            /// * Real-time traffic flow events: Speed at calculation time used for entire length of route. <br/>
            /// * Historical and predictive traffic speed: Ignored <br/>
            /// Departure time provided: <br/>
            /// * Static time based restrictions: Avoided if road would be traversed within validity period of the restriction. <br/>
            /// * Real-time traffic closures: Avoided if road would be traversed within validity period of the incident. <br/>
            /// * Real-time traffic flow events: Speed used if road would be traversed within validity period of the flow event. <br/>
            /// * Historical and predictive traffic: Used.
            /// </summary>
            enabled,

            /// <summary>
            /// No departure time provided: <br/>
            /// * Static time based restrictions: Ignored <br/>
            /// * Real-time traffic closures: Ignored. <br/>
            /// * Real-time traffic flow speed: Ignored. <br/>
            /// * Historical and predictive traffic: Ignored <br/>
            /// Departure time provided: <br/>
            /// * Static time based restrictions: Avoided if road would be traversed within validity period of the restriction. <br/>
            /// * Real-time traffic closures: Valid for entire length of route. <br/>
            /// * Real-time traffic flow speed: Ignored. <br/>
            /// * Historical and predictive traffic: Ignored <br/>
            /// Note: Difference between traffic disabled and enabled affects only the calculation of the route.  <br/>
            /// Traffic time of the route will still be calculated for all routes using the same rules as for traffic:enabled.
            /// </summary>
            disabled,

            /// <summary>
            /// Let the service automatically apply traffic related constraints that are suitable for the selected routing type, transport mode, and departure time. <br/>
            /// Also user entitlements will be taken into consideration. 
            /// </summary>
            defaults
        }

        /// <summary>
        /// The routing features can be used to define special conditions on the calculated route.
        /// </summary>
        public class Feature
        {
            public Weight tollroad = Weight.normal;
            public Weight motorway = Weight.normal;
            public Weight boatFerry = Weight.normal;
            public Weight railFerry = Weight.normal;
            public Weight tunnel = Weight.normal;
            public Weight dirtRoad = Weight.normal;
            public Weight park = Weight.normal;

            /// <summary>
            /// Route feature weights are used to define weighted conditions on special route features like tollroad, motorways, etc. 
            /// </summary>
            public enum Weight
            {
                /// <summary>
                /// The routing engine guarantees that the route does not contain strictly excluded features. <br/>
                /// If the condition cannot be fulfilled no route is returned. 
                /// </summary>
                strictExclude = -3,

                /// <summary>
                /// The routing engine does not consider links containing the corresponding feature. <br/>
                /// If no route can be found because of these limitations the condition is weakened. 
                /// </summary>
                softExclude = -2,

                /// <summary>
                /// The routing engine assigns penalties for links containing the corresponding feature. 
                /// </summary>
                avoid = -1,

                /// <summary>
                /// The routing engine does not alter the ranking of links containing the corresponding feature. 
                /// </summary>
                normal = 0
            }

            public void GetURLKey(StringBuilder builder)
            {
                if (tollroad != Weight.normal) builder.Append(";tollroad:").Append((int) tollroad);
                if (motorway != Weight.normal) builder.Append(";motorway:").Append((int) motorway);
                if (boatFerry != Weight.normal) builder.Append(";boatFerry:").Append((int) boatFerry);
                if (railFerry != Weight.normal) builder.Append(";railFerry:").Append((int) railFerry);
                if (tunnel != Weight.normal) builder.Append(";tunnel:").Append((int) tunnel);
                if (dirtRoad != Weight.normal) builder.Append(";dirtRoad:").Append((int) dirtRoad);
                if (park != Weight.normal) builder.Append(";park:").Append((int) park);
            }
        }

        public void GetURLKey(StringBuilder builder)
        {
            builder.Append("&mode=").Append(type).Append(";").Append(transportMode);
            if (trafficMode.HasValue) builder.Append(";traffic:").Append(trafficMode.Value != TrafficMode.defaults? trafficMode.Value.ToString(): "default");
            if (feature != null) feature.GetURLKey(builder);
        }

        public static RoutingMode Parse(OnlineMapsXML node)
        {
            RoutingMode mode = new RoutingMode();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "Type") mode.type = (Type) Enum.Parse(typeof (Type), n.Value());
                else if (n.name == "TransportModes") mode.transportMode = (TransportModes) Enum.Parse(typeof (TransportModes), n.Value());
                else if (n.name == "TrafficMode") mode.trafficMode = (TrafficMode) Enum.Parse(typeof (TrafficMode), n.Value());
                else Debug.Log(n.name + "\n" + n.outerXml);
            }

            return mode;
        }
    }

    /// <summary>
    /// StreetWaypoint defines a waypoint by street position and name. <br/>
    /// The street name helps select the right road in complex intersection scenarios such as a bridge crossing another road. <br/>
    /// A common use case for this scenario is when the user specifies a waypoint by selecting a place or a location after a search.<br/>
    /// The optional display position of the waypoint defines where the location is displayed on a map. <br/>
    /// It denotes the center of the location and is not navigable, i.e.it is not located on a link in the routing network in contrast to the navigation positions of a location. <br/>
    /// The display position allows the routing engine to decide whether the waypoint is located on the left or on the right-hand side of the route.
    /// </summary>
    public class StreetWaypoint : Waypoint
    {
        /// <summary>
        /// Latitude WGS-84 degrees between -90 and 90. 
        /// </summary>
        public double? displayLatitude;

        /// <summary>
        /// Longitude WGS-84 degrees between -180 and 180. 
        /// </summary>
        public double? displayLongitude;

        /// <summary>
        /// Altitude in meters. 
        /// </summary>
        public double? displayAltitude;

        /// <summary>
        /// Custom label identifying this waypoint.
        /// </summary>
        public string userLabel;

        /// <summary>
        /// Latitude WGS-84 degrees between -90 and 90.
        /// </summary>
        public double streetLatitude;

        /// <summary>
        /// Longitude WGS-84 degrees between -180 and 180. 
        /// </summary>
        public double streetLongitude;

        /// <summary>
        /// Altitude in meters. 
        /// </summary>
        public double? streetAltitude;

        public string streetName;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="streetLongitude">Longitude</param>
        /// <param name="streetLatitude">Latitude</param>
        public StreetWaypoint(double streetLongitude, double streetLatitude)
        {
            this.streetLongitude = streetLongitude;
            this.streetLatitude = streetLatitude;
        }

        public override void GetURLKey(StringBuilder builder)
        {
            builder.Append("street!");
            if (type.HasValue) builder.Append(type).Append("!");

            if (displayLatitude.HasValue && displayLongitude.HasValue)
            {
                builder.Append(displayLatitude).Append(",").Append(displayLongitude);
                if (displayAltitude.HasValue) builder.Append(",").Append(displayAltitude.Value.ToString(OnlineMapsUtils.numberFormat));
            }
            
            if (!string.IsNullOrEmpty(userLabel)) builder.Append(";").Append(userLabel);

            builder.Append("!").Append(streetLatitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(streetLongitude.ToString(OnlineMapsUtils.numberFormat));
            if (streetAltitude.HasValue) builder.Append(",").Append(streetAltitude.Value.ToString(OnlineMapsUtils.numberFormat));
            if (!string.IsNullOrEmpty(streetName)) builder.Append(";").Append(streetName);
        }
    }

    /// <summary>
    /// Contains vehicle specific information, which can be used to estimate CO2 emission. 
    /// </summary>
    public class VehicleType
    {
        /// <summary>
        /// Vehicle engine type. 
        /// </summary>
        public EngineType engineType = EngineType.gasoline;

        /// <summary>
        /// The average fuel consumption, measured in liters per 100km. <br/>
        /// Affects CO2 emission only in case of combustion engines (diesel and gasoline). 
        /// </summary>
        public double? averageConsumption;

        public enum EngineType
        {
            /// <summary>
            /// Diesel engine. Emits 2.64 kg of CO2 from each combusted liter of fuel. 
            /// </summary>
            diesel,

            /// <summary>
            /// Gasoline engine. Emits 2.392 kg of CO2 from each combusted liter of fuel. 
            /// </summary>
            gasoline,

            /// <summary>
            /// Electric engine. Does not emit CO2. 
            /// </summary>
            electric
        }

        public void GetURLKey(StringBuilder builder)
        {
            builder.Append("&vehicletype=").Append(engineType);
            if (averageConsumption.HasValue) builder.Append(",").Append(averageConsumption.Value.ToString(OnlineMapsUtils.numberFormat));
        }
    }

    /// <summary>
    /// The base class for Waypoints.
    /// </summary>
    public abstract class Waypoint
    {
        /// <summary>
        /// 180 degree turns are allowed for stopOver but not for passThrough. <br/>
        /// Waypoints defined through a drag-n-drop action should be marked as pass-through. <br/>
        /// PassThrough waypoints will not appear in the list of maneuvers. 
        /// </summary>
        public Type? type;

        public enum Type
        {
            stopOver,
            passThrough
        }

        public abstract void GetURLKey(StringBuilder builder);
    }


    [Flags]
    public enum HazardousGoodType
    {
        explosive = 1,
        gas = 2,
        flammable = 4,
        combustible = 8,
        organic = 16,
        poison = 32,
        radioActive = 64,
        corrosive = 128,
        poisonousInhalation = 256,
        harmfulToWater = 512,
        other = 1024,
        allhazardousGoods = 2048
    }

    /// <summary>
    /// Representation formats for instruction texts.
    /// </summary>
    public enum InstructionFormat
    {
        /// <summary>
        /// Returns the instruction as a plain text string.
        /// </summary>
        text,

        /// <summary>
        /// html instruction format is based on span tags with different CSS classes to assign semantics to the tagged part of the instruction.
        /// </summary>
        html
    }

    [Flags]
    public enum LegAttributes
    {
        /// <summary>
        /// Indicates whether the waypoint shall be included in the route leg.
        /// </summary>
        [ShortName("wp")]
        waypoint = 1,

        /// <summary>
        /// Indicates whether the maneuvers of the route leg shall be provided.
        /// </summary>
        [ShortName("mn")]
        maneuvers = 2,

        /// <summary>
        /// Indicates whether the links along the route leg shall be provided.
        /// </summary>
        [ShortName("li")]
        links = 4,

        /// <summary>
        /// Indicates whether the route leg should include its length 
        /// </summary>
        [ShortName("le")]
        length = 8,

        /// <summary>
        /// Indicates whether the route leg should include its duration 
        /// </summary>
        [ShortName("tt")]
        travelTime = 16,

        /// <summary>
        /// Indicates whether the shape of the segment to the next maneuver should be included in the maneuvers.
        /// </summary>
        [ShortName("sh")]
        shape = 32,

        /// <summary>
        /// Indicates whether shape index information (FirstPoint, LastPoint) should be included in the maneuvers instead of copying shape points to the maneuver.
        /// </summary>
        [ShortName("ix")]
        indices = 64,

        /// <summary>
        /// Indicates whether the bounding box of the maneuver shall be provided.
        /// </summary>
        [ShortName("bb")]
        boundingBox = 128,

        /// <summary>
        /// Indicates whether the BaseTime information should be provided in RouteLegs.
        /// </summary>
        [ShortName("bt")]
        baseTime = 256,

        /// <summary>
        /// Indicates whether the TrafficTime information should be included in RouteLegs.
        /// </summary>
        [ShortName("tm")]
        trafficTime = 512,

        /// <summary>
        /// Indicates whether distance and time summary information should be included in RouteLegs.
        /// </summary>
        [ShortName("sm")]
        summary = 1024
    }

    [Flags]
    public enum LineAttributes
    {
        /// <summary>
        /// Indicates whether the foreground color shall be included in the line.
        /// </summary>
        [ShortName("fr")]
        foreground = 1,

        /// <summary>
        /// Indicates whether the background color of the line shall be provided.
        /// </summary>
        [ShortName("bg")]
        background = 2,

        /// <summary>
        /// Indicates whether the line style of the public transport line shall be provided.
        /// </summary>
        [ShortName("ls")]
        lineStyle = 4,

        /// <summary>
        /// Indicates whether the company short name should be included in the public transport line.
        /// </summary>
        [ShortName("cs")]
        companyShortName = 8,

        /// <summary>
        /// Indicates whether the company logo should be included in the public transport line.
        /// </summary>
        [ShortName("cl")]
        companyLogo = 16,

        /// <summary>
        /// Indicates whether the flags should be included in the public transport line.
        /// </summary>
        [ShortName("fl")]
        flags = 32,

        /// <summary>
        /// Indicates whether the type name should be included in the public transport line.
        /// </summary>
        [ShortName("tn")]
        typeName = 64,

        /// <summary>
        /// Indicates whether the line Id should be included in the public transport line.
        /// </summary>
        [ShortName("li")]
        lineId = 128,

        /// <summary>
        /// Indicates whether the company Id should be included in the public transport line.
        /// </summary>
        [ShortName("ci")]
        companyId = 256,

        /// <summary>
        /// Indicates whether the system Id should be included in the public transport line.
        /// </summary>
        [ShortName("si")]
        systemId = 512,

        /// <summary>
        /// Indicates whether stops should be included in the public transport line.
        /// </summary>
        [ShortName("st")]
        stops = 1024
    }

    [Flags]
    public enum LinkAttributes
    {
        /// <summary>
        /// Indicates whether the link should include its geometry
        /// </summary>
        [ShortName("sh")]
        shape = 1,

        /// <summary>
        /// Indicates whether the link should include its length
        /// </summary>
        [ShortName("le")]
        length = 2,

        /// <summary>
        /// Indicates whether the link should include SpeedLimit
        /// </summary>
        [ShortName("sl")]
        speedLimit = 4,

        /// <summary>
        /// Indicates whether the link should include dynamic speed information
        /// </summary>
        [ShortName("ds")]
        dynamicSpeedInfo = 8,

        /// <summary>
        /// Indicates whether the link should include truck restrictions 
        /// </summary>
        [ShortName("tr")]
        truckRestrictions = 16,

        /// <summary>
        /// Indicates whether the link should include link flags
        /// </summary>
        [ShortName("fl")]
        flags = 32,

        /// <summary>
        /// Indicates whether the link should include the links road number 
        /// </summary>
        [ShortName("rn")]
        roadNumber = 64,

        /// <summary>
        /// Indicates whether the link should include the links road name 
        /// </summary>
        [ShortName("ro")]
        roadName = 128,

        /// <summary>
        /// Indicates whether the link should include the timezone. <br/>
        /// Note: Requesting timezone information is known to slowndown response.
        /// </summary>
        [ShortName("tz")]
        timezone = 256,

        /// <summary>
        /// Indicates whether the link should include the link which will be next when following the route
        /// </summary>
        [ShortName("nl")]
        nextLink = 512,

        /// <summary>
        /// Indicates whether the link should include information about the public transport line.
        /// </summary>
        [ShortName("pt")]
        publicTransportLine = 1024,

        /// <summary>
        /// Indicates whether the link should include information about the remaining time until the destination is reached.
        /// </summary>
        [ShortName("rt")]
        remainTime = 2048,

        /// <summary>
        /// Indicates whether the link should include information about the remaining distance until the destination is reached.
        /// </summary>
        [ShortName("rd")]
        remainDistance = 4096,

        /// <summary>
        /// Indicates whether the link should include information about the associated maneuver.
        /// </summary>
        [ShortName("ma")]
        maneuver = 8192,

        /// <summary>
        /// Indicates whether the link should include information about the functional class.
        /// </summary>
        [ShortName("fc")]
        functionalClass = 16384,

        /// <summary>
        /// Indicates whether the link should include information about the next stop.
        /// </summary>
        [ShortName("ns")]
        nextStopName = 32768,

        /// <summary>
        /// Indicates whether shape index information (FirstPoint, LastPoint) should be included in links instead of copying shape points.
        /// </summary>
        [ShortName("ix")]
        indices = 65536
    }

    [Flags]
    public enum ManeuverAttributes
    {
        /// <summary>
        /// Indicates whether the position should be included in the maneuvers.
        /// </summary>
        [ShortName("po")]
        position = 1,

        /// <summary>
        /// Indicates whether the shape of the segment to the next maneuver should be included in the maneuvers.
        /// </summary>
        [ShortName("sh")]
        shape = 2,

        /// <summary>
        /// Indicates whether the time needed to the next maneuver should be included in the maneuvers.
        /// </summary>
        [ShortName("tt")]
        travelTime = 4,

        /// <summary>
        /// Indicates whether the distance to the next maneuver should be included in the maneuvers.
        /// </summary>
        [ShortName("le")]
        length = 8,

        /// <summary>
        /// Indicates whether the point in time when the maneuver will take place should be included in the maneuvers.
        /// </summary>
        [ShortName("ti")]
        time = 16,

        /// <summary>
        /// Indicates whether the link where the maneuver takes place shall be included in the maneuver.
        /// </summary>
        [ShortName("li")]
        link = 32,

        /// <summary>
        /// Indicates whether the information about the public transport line should be included in the maneuvers.
        /// </summary>
        [ShortName("pt")]
        publicTransportLine = 64,

        /// <summary>
        /// Indicates whether the platform information for a public transport line should be included in the maneuvers.
        /// </summary>
        [ShortName("pl")]
        platform = 128,

        /// <summary>
        /// Indicates whether the road name should be included in the maneuvers.
        /// </summary>
        [ShortName("rn")]
        roadName = 256,

        /// <summary>
        /// Indicates whether the name of the next road shall be included in the maneuvers.
        /// </summary>
        [ShortName("nr")]
        nextRoadName = 512,

        /// <summary>
        /// Indicates whether the road number should be included in the maneuvers.
        /// </summary>
        [ShortName("ru")]
        roadNumber = 1024,

        /// <summary>
        /// Indicates whether the number of the next road should be included in the maneuvers.
        /// </summary>
        [ShortName("nu")]
        nextRoadNumber = 2048,

        /// <summary>
        /// Indicates whether the sign post information should be included in the maneuvers.
        /// </summary>
        [ShortName("sp")]
        signPost = 4096,

        /// <summary>
        /// Indicates whether additional notes should be included in the maneuvers.
        /// </summary>
        [ShortName("no")]
        notes = 8192,

        /// <summary>
        /// Indicates whether actions should be included in the maneuvers. 
        /// </summary>
        [ShortName("ac")]
        action = 16384,

        /// <summary>
        /// Indicates whether directions should be included in the maneuvers.
        /// </summary>
        [ShortName("di")]
        direction = 32768,

        /// <summary>
        /// Indicates whether the freeway exit should be included in the maneuvers.
        /// </summary>
        [ShortName("fe")]
        freewayExit = 65536,

        /// <summary>
        /// Indicates whether the freeway junction should be included in the maneuvers.
        /// </summary>
        [ShortName("fj")]
        freewayJunction = 131072,

        /// <summary>
        /// Indicates whether shape index information (FirstPoint, LastPoint) should be included in the maneuvers instead of copying shape points to the maneuver.
        /// </summary>
        [ShortName("ux")]
        indices = 262144,

        /// <summary>
        /// Indicates whether the BaseTime information should be included in the maneuvers. By default, BaseTime information is not included in the maneuvers.
        /// </summary>
        [ShortName("bt")]
        baseTime = 524288,

        /// <summary>
        /// Indicates whether the TrafficTime information should be included in the maneuvers. By default, TrafficTime information is not included in the maneuvers.
        /// </summary>
        [ShortName("tm")]
        trafficTime = 1048576,

        /// <summary>
        /// Indicates whether wait time information should be included in public transport maneuvers.
        /// </summary>
        [ShortName("wt")]
        waitTime = 2097152,

        /// <summary>
        /// Indicates whether the bounding box of the route shall be provided for the route.
        /// </summary>
        [ShortName("bb")]
        boundingBox = 4194304,

        /// <summary>
        /// Indicates whether road shield information should be included in the maneuvers.
        /// </summary>
        [ShortName("rs")]
        roadShield = 8388608,

        /// <summary>
        /// Indicates whether information about shape quality should be included in maneuvers.
        /// </summary>
        [ShortName("sq")]
        shapeQuality = 16777216,

        /// <summary>
        /// Indicates whether a reference to the next maneuver should be included in the maneuvers.
        /// </summary>
        [ShortName("nm")]
        nextManeuver = 33554432,

        /// <summary>
        /// Indicates whether the information about the public transport tickets should be included in the maneuvers.
        /// </summary>
        [ShortName("tx")]
        publicTransportTickets = 67108864,

        /// <summary>
        /// Indicates whether start angle information should be included in the maneuvers.
        /// </summary>
        [ShortName("sa")]
        startAngle = 134217728
    }

    public enum MetricSystem
    {
        imperial,
        metric
    }

    [Flags]
    public enum PublicTransportType
    {
        busPublic = 1,
        busTouristic = 2,
        busIntercity = 4,
        busExpress = 8,
        railMetro = 16,
        railMetroRegional = 32,
        railLight = 64,
        railRegional = 128,
        trainRegional = 256,
        trainIntercity = 512,
        trainHighSpeed = 1024,
        monoRail = 2048,
        aerial = 4096,
        inclined = 8192,
        water = 16384,
        privateService = 32768
    }

    public enum Representation
    {
        /// <summary>
        /// Overview mode only returns the Route and the RouteSummary object 
        /// </summary>
        overview,

        /// <summary>
        /// Display mode that allows to show the route with all maneuvers. Links won't be included in the response
        /// </summary>
        display,

        /// <summary>
        /// Drag and Drop mode to be used during drag and drop (re-routing) actions. <br/>
        /// The response will only contain the shape of the route restricted to the view bounds provided in the representation options.
        /// </summary>
        dragNDrop,

        /// <summary>
        /// Navigation mode to provide all information necessary to support a navigation device. <br/>
        /// This mode activates the most extensive data response as all link information will be included in the response to allow a detailed display while navigating.<br/>
        /// RouteId will not be calculated in this mode however, unless it is additionally requested.
        /// </summary>
        navigation,

        /// <summary>
        /// Paging mode that will be used when incrementally loading links while navigating along the route. <br/>
        /// The response will be limited to link information.
        /// </summary>
        linkPaging,

        /// <summary>
        /// Turn by turn mode to provide all information necessary to support turn by turn. <br/>
        /// This mode activates all data needed for navigation excluding any redundancies. <br/>
        /// RouteId will not be calculated in this mode however, unless it is additionally requested.
        /// </summary>
        turnByTurn
    }

    [Flags]
    public enum RouteAttributes
    {
        /// <summary>
        /// Indicates whether via points shall be included in the route. 
        /// </summary>
        [ShortName("wp")]
        waypoints = 1,

        /// <summary>
        /// Indicates whether a route summary shall be provided for the route.
        /// </summary>
        [ShortName("sm")]
        summary = 2,

        /// <summary>
        /// Indicates whether a country-based route summary shall be provided for the route.
        /// </summary>
        [ShortName("sc")]
        summaryByCountry = 4,

        /// <summary>
        /// Indicates whether the shape of the route shall be provided for the route.
        /// </summary>
        [ShortName("sh")]
        shape = 8,

        /// <summary>
        /// Indicates whether the bounding box of the route shall be provided for the route.
        /// </summary>
        [ShortName("bb")]
        boundingBox = 16,

        /// <summary>
        /// Indicates whether the legs of the route shall be provided for the route.
        /// </summary>
        [ShortName("lg")]
        legs = 32,

        /// <summary>
        /// Indicates whether additional notes shall be provided for the route.
        /// </summary>
        [ShortName("no")]
        notes = 64,

        /// <summary>
        /// Indicates whether PublicTransportLines shall be provided for the route.
        /// </summary>
        [ShortName("li")]
        lines = 128,

        /// <summary>
        /// Indicates whether PublicTransportTickets shall be provided for the route.
        /// </summary>
        [ShortName("ri")]
        routeId = 256,

        /// <summary>
        /// Indicates whether Maneuver Groups should be included in the route. Maneuver Groups are useful for multi modal routes. 
        /// </summary>
        [ShortName("gr")]
        groups = 512,

        /// <summary>
        /// Indicates whether PublicTransportTickets shall be provided for the route.
        /// </summary>
        [ShortName("tx")]
        tickets = 1024,

        /// <summary>
        /// Indicates whether Incidents on the route shall be provided for the route.
        /// </summary>
        [ShortName("ic")]
        incidents = 2048,

        /// <summary>
        /// Indicates whether Labels shall be provided for the route. Labels are useful to distinguish between alternative routes. 
        /// </summary>
        [ShortName("la")]
        labels = 4096
    }

    public class ShortNameAttribute : Attribute
    {
        public readonly string shortName;

        public ShortNameAttribute(string shortName)
        {
            this.shortName = shortName;
        }
    }

    public enum TruckType
    {
        truck,
        tractorTruck
    }
}