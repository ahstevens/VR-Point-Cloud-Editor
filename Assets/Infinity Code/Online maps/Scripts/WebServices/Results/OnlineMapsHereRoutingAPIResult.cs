/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Result of HERE Routing API query.
/// </summary>
public class OnlineMapsHereRoutingAPIResult
{
    /// <summary>
    /// Provides details about the request itself, such as the time at which it was processed, a request id, or the map version on which the calculation was based. 
    /// </summary>
    public MetaInfo metaInfo;

    /// <summary>
    /// Contains the calculated path across a navigable link network, as specified in the request. <br/>
    /// Routes contain navigation instructions for a single trip as: <br/>
    /// waypoints (fixed locations) and route legs (sections of the route between waypoints). <br/>
    /// Each response may also include information about the route itself, such as its overall shape, map location, or a summary description. 
    /// </summary>
    public Route[] routes;

    /// <summary>
    /// Indicates the language used for all textual information related to the route. 
    /// </summary>
    public string language;

    /// <summary>
    /// Contains copyright information intended for the end user when route uses data provided by outside companies. <br/>
    /// Source attribution must be displayed together with a route according to terms and conditions of the API. 
    /// </summary>
    public SourceAttribution sourceAttribution;

    public OnlineMapsHereRoutingAPIResult(OnlineMapsXML node)
    {
        List<Route> rs = new List<Route>();
        foreach (OnlineMapsXML n in node)
        {
            if (n.name == "MetaInfo") metaInfo = new MetaInfo(n);
            else if (n.name == "Route") rs.Add(new Route(n));
            else if (n.name == "Language") language = n.Value();
            else if (n.name == "SourceAttribution") sourceAttribution = new SourceAttribution(n);
            else Debug.Log(n.name + "\n" + n.outerXml);
        }
        routes = rs.ToArray();
    }

    /// <summary>
    /// Meta information which is not directly related to the route calculation is wrapped within a separate element. 
    /// </summary>
    public class MetaInfo
    {
        /// <summary>
        /// Mirrored RequestId value from the request structure. Used to trace requests.
        /// </summary>
        public string requestId;

        /// <summary>
        /// Time at which the search was performed. 
        /// </summary>
        public string timestamp;

        /// <summary>
        /// Gives the version of the underlying map, upon which the route calculations are based.
        /// </summary>
        public string mapVersion;

        /// <summary>
        /// Gives the version of the module that performed the route calculations.
        /// </summary>
        public string moduleVersion;

        /// <summary>
        /// Required. Gives the version of the schema definition to enable formats other than XML to identify elements without using namespaces. 
        /// </summary>
        public string interfaceVersion;

        public MetaInfo(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "RequestId") requestId = n.Value();
                else if (n.name == "Timestamp") timestamp = n.Value();
                else if (n.name == "MapVersion") mapVersion = n.Value();
                else if (n.name == "ModuleVersion") moduleVersion = n.Value();
                else if (n.name == "InterfaceVersion") interfaceVersion = n.Value();
                else Debug.Log(n.name + "\n" + n.outerXml);
            }
        }
    }

    /// <summary>
    /// A Route describes a distinct path through the navigable road network between two or more waypoints. <br/>
    /// It consists of route legs, each of which is a path between two waypoints. 
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Permanent unique identifier of the route, generated based on route links. <br/>
        /// Can be used to reproduce any previously calculated route. <br/>
        /// If a RouteId is requested, but fails to be calculated for any reason(e.g. public transport enabled), then the RouteId element is not available in the response.The rest of the route is intact.
        /// </summary>
        public string routeId;

        /// <summary>
        /// List of waypoints that have been defined when requesting for a route calculation. <br/>
        /// The first waypoint is defined as the start of the route; the last waypoint marks the destination. <br/>
        /// Any points in between the two are considered via points. 
        /// </summary>
        public Waypoint[] waypoints;

        /// <summary>
        /// Settings for route calculation. One mode can be specified for each route. 
        /// </summary>
        public OnlineMapsHereRoutingAPI.RoutingMode mode;

        /// <summary>
        /// Shape of the route as a polyline. <br/>
        /// The accuracy might depend on the resolution specified in mpp (meters per pixel) when requesting the route. <br/>
        /// In some use cases (like web portals), only the route's shape is required without the nested structure of a route and detailed knowledge of the links and LinkIds. <br/>
        /// In this case, the shape does not need to be acquired by traversing the route's links, but can be represented using this attribute at route level. 
        /// </summary>
        public GeoCoordinate[] shape;

        /// <summary>
        /// Bounding Box of the route. 
        /// </summary>
        public OnlineMapsHereRoutingAPI.GeoRect boundingBox;

        /// <summary>
        /// Partition of the route into legs between the different waypoints. 
        /// </summary>
        public Leg[] legs;

        /// <summary>
        /// List of all public transport lines which are used by public transport links and maneuvers of the route. 
        /// </summary>
        public PublicTransportLine[] publicTransportLines;

        /// <summary>
        /// For public transport routes, a list of ticketing options for the provided route. <br/>
        /// Each option is a list of tickets covering those parts of the route for which ticketing information is supported. 
        /// </summary>
        public PublicTransportTickets[] publicTransportTickets;

        /// <summary>
        /// Notes that are either related to the calculation (violated routing options) or that refer the route as a whole. <br/>
        /// In addition to these notes additional notes can be attached to maneuvers. <br/>
        /// The maneuver notes are usually related to the route segment following the maneuver and would be of interest when passing this segment. 
        /// </summary>
        public Note[] notes;

        /// <summary>
        /// Overall route distance and time summary. 
        /// </summary>
        public Summary summary;

        /// <summary>
        /// Route distance and time summary per traversed country. 
        /// </summary>
        public SummaryByCountry[] summaryByCountries;

        /// <summary>
        /// A simplified base polyline with a given tolerance parameter used to reduce the number of points. <br/>
        /// The points in the base polyline are implicitly referenced by index. 
        /// </summary>
        public Generalization[] generalizations;

        /// <summary>
        /// Maneuvers organized into sections based on TransportModeType. <br/>
        /// It provides the user grouped itinerary summary and brief route instructions. 
        /// </summary>
        public ManueverGroup[] manueverGroups;

        /// <summary>
        /// An incident describes a temporary event on a route. <br/>
        /// It typically refers to a real world incident (accident, road construction, etc.) spanning on one or several subsequent links. 
        /// </summary>
        public Incident[] incidents;

        /// <summary>
        /// Unique names within a route used to distinguish between alternative routes. <br/>
        /// They can be city names, road names or something else that makes the distinction possible. 
        /// </summary>
        public string label;

        public Route(OnlineMapsXML node)
        {
            List<Waypoint> ws = new List<Waypoint>();
            List<PublicTransportLine> ptls = new List<PublicTransportLine>();
            List<Leg> ls = new List<Leg>();
            List<Note> ns = new List<Note>();
            List<PublicTransportTickets> ptts = new List<PublicTransportTickets>();
            List<ManueverGroup> ms = new List<ManueverGroup>();
            List<Generalization> gs = new List<Generalization>();
            List<SummaryByCountry> ss = new List<SummaryByCountry>();
            List<Incident> ics = new List<Incident>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "RouteId") routeId = n.Value();
                else if (n.name == "Waypoint") ws.Add(new Waypoint(n));
                else if (n.name == "Mode") mode = OnlineMapsHereRoutingAPI.RoutingMode.Parse(n);
                else if (n.name == "Shape") shape = GeoCoordinate.ParseArray(n.Value());
                else if (n.name == "BoundingBox") boundingBox = new OnlineMapsHereRoutingAPI.GeoRect(n);
                else if (n.name == "Leg") ls.Add(new Leg(n));
                else if (n.name == "PublicTransportLine") ptls.Add(new PublicTransportLine(n));
                else if (n.name == "PublicTransportTickets") ptts.Add(new PublicTransportTickets(n));
                else if (n.name == "Note") ns.Add(new Note(n));
                else if (n.name == "Summary") summary = new Summary(n);
                else if (n.name == "SummaryByCountry") ss.Add(new SummaryByCountry(n));
                else if (n.name == "Generalization") gs.Add(new Generalization(n));
                else if (n.name == "ManueverGroup") ms.Add(new ManueverGroup(n));
                else if (n.name == "Incident") ics.Add(new Incident(n));
                else if (n.name == "Label") label = n.Value();
                else Debug.Log(n.name + "\n" + n.outerXml);
            }
            waypoints = ws.ToArray();
            publicTransportLines = ptls.ToArray();
            legs = ls.ToArray();
            notes = ns.ToArray();
            publicTransportTickets = ptts.ToArray();
            manueverGroups = ms.ToArray();
            generalizations = gs.ToArray();
            summaryByCountries = ss.ToArray();
            incidents = ics.ToArray();
        }

        /// <summary>
        /// The service returns GeneralizationType in route responses for requests carrying the generalizationTolerances parameter. <br/>
        /// The default is to not return any GeneralizationType. 
        /// </summary>
        public class Generalization
        {
            /// <summary>
            /// Contains the tolerance level used in the request. 
            /// </summary>
            public double tolerance;

            /// <summary>
            /// Specifies the offset into the global shape array to be used for a given generalization tolerance. 
            /// </summary>
            public int index;

            public Generalization(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Tolerance") tolerance = n.Value<double>();
                    else if (n.name == "Index") index = n.Value<int>();
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }
        }

        /// <summary>
        /// Contains latitude, longitude, altitude
        /// </summary>
        public class GeoCoordinate
        {
            /// <summary>
            /// Latitude
            /// </summary>
            public double latitude;

            /// <summary>
            /// Longitude
            /// </summary>
            public double longitude;

            /// <summary>
            /// Altitude
            /// </summary>
            public double? altitude;

            public GeoCoordinate(double longitude, double latitude, double? altitude = null)
            {
                this.latitude = latitude;
                this.longitude = longitude;
                if (altitude.HasValue) this.altitude = altitude.Value;
            }

            public GeoCoordinate(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Latitude") latitude = n.Value<double>();
                    else if (n.name == "Longitude") longitude = n.Value<double>();
                    else if (n.name == "Altitude") altitude = n.Value<double>();
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }

            public static GeoCoordinate[] ParseArray(string value)
            {
                List<GeoCoordinate> cs = new List<GeoCoordinate>();
                StringBuilder lat = new StringBuilder(), lng = new StringBuilder();
                bool isLat = true;
                bool isNegateLng = false;
                bool isNegateLat = false;
                foreach (char c in value)
                {
                    if (c > 47 && c < 58 || c == '.')
                    {
                        if (isLat) lat.Append(c);
                        else lng.Append(c);
                    }
                    else if (c == '-')
                    {
                        if (isLat) isNegateLat = true;
                        else isNegateLng = true;
                    }
                    else if (c == ',') isLat = false;
                    else if (c == ' ')
                    {
                        if (isNegateLat) lat.Insert(0, '-');
                        if (isNegateLng) lng.Insert(0, '-');
                        cs.Add(new GeoCoordinate(double.Parse(lng.ToString(), OnlineMapsUtils.numberFormat), double.Parse(lat.ToString(), OnlineMapsUtils.numberFormat)));
                        lng.Length = 0;
                        lat.Length = 0;
                        isLat = true;
                        isNegateLat = false;
                        isNegateLng = false;
                    }
                }
                return cs.ToArray();
            }
        }

        /// <summary>
        /// An incident describes a temporary event on a route. <br/>
        /// It typically refers to a real world incident (accident, road construction, etc.) spanning on one or several subsequent links. 
        /// </summary>
        public class Incident
        {
            /// <summary>
            /// Time period when the incident is relevant
            /// </summary>
            public string validityPeriod;

            /// <summary>
            /// A textual description of the event
            /// </summary>
            public string text;

            /// <summary>
            /// Type of the incident.
            /// </summary>
            public string type;

            /// <summary>
            /// Criticality on an integer scale (0 = critical, 1 = major, 2 = minor, 3 = low impact).
            /// </summary>
            public string criticality;

            public Incident(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "ValidityPeriod") validityPeriod = n.Value();
                    else if (n.name == "Text") text = n.Value();
                    else if (n.name == "Type") type = n.Value();
                    else if (n.name == "Criticality") criticality = n.Value();
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }
        }

        /// <summary>
        /// The service defines a route leg as the portion of a route between one waypoint and the next. <br/>
        /// RouteLegType contains information about a route leg, such as the time required to traverse it, its shape, start and end point, as well as information about any sublegs contained in the leg due to the presence of passthrough waypoints. <br/>
        /// Note: passThrough waypoints do not create explicit route legs, but instead create sublegs. The service provides subleg information within this type if any are present. 
        /// </summary>
        public class Leg
        {
            /// <summary>
            /// Route waypoint that is located at the start of this route leg. <br/>
            /// This waypoint matches one of the waypoints in the Route. 
            /// </summary>
            public Waypoint start;

            /// <summary>
            /// Route waypoint that is located at the end of this route leg. <br/>
            /// This waypoint matches one of the waypoints in the Route. 
            /// </summary>
            public Waypoint end;

            /// <summary>
            /// Length of the leg. 
            /// </summary>
            public double length;

            /// <summary>
            /// The time in seconds needed to travel along this route leg. <br/>
            /// Considers any available traffic information, if enabled and the authorized for the user. 
            /// </summary>
            public double travelTime;

            /// <summary>
            /// List of all maneuvers which are included in this portion of the route. 
            /// </summary>
            public Maneuver[] maneuvers;

            /// <summary>
            /// List of all links which are included in this portion of the route. 
            /// </summary>
            public string[] links;

            /// <summary>
            /// Bounding Box of the leg. 
            /// </summary>
            public OnlineMapsHereRoutingAPI.GeoRect boundingBox;

            /// <summary>
            /// Shape of route leg. 
            /// </summary>
            public GeoCoordinate[] shape;

            /// <summary>
            /// Index into the global geometry array, pointing to the first point of the shape subsegment associated with this leg.
            /// </summary>
            public int firstPoint;

            /// <summary>
            /// Index into the global geometry array, pointing to the last point of the shape subsegment associated with this leg.
            /// </summary>
            public int lastPoint;

            /// <summary>
            /// Time in seconds required for this route leg, taking available traffic information into account. 
            /// </summary>
            public double trafficTime;

            /// <summary>
            /// Estimated time in seconds spent on this leg, without considering traffic conditions. <br/>
            /// The service may also account for additional time penalties, therefore this may be greater than the leg length divided by the base speed. 
            /// </summary>
            public double baseTime;

            /// <summary>
            /// Distance and time summary information for the route leg. 
            /// </summary>
            public Summary summary;

            /// <summary>
            /// Distance and time summary information for any sub legs of this route leg. <br/>
            /// The service defines sub legs where passThrough waypoints are used, so the list may be empty if no such waypoints exist within this route leg. 
            /// </summary>
            public Summary[] subLegSummary;

            public Leg(OnlineMapsXML node)
            {
                List<Maneuver> ms = new List<Maneuver>();
                List<string> ls = new List<string>();
                List<Summary> ss = new List<Summary>();

                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Start") start = new Waypoint(n);
                    else if (n.name == "End") end = new Waypoint(n);
                    else if (n.name == "Length") length = n.Value<double>();
                    else if (n.name == "TravelTime") travelTime = n.Value<double>();
                    else if (n.name == "Maneuver") ms.Add(new Maneuver(n));
                    else if (n.name == "Link") ls.Add(n.Value());
                    else if (n.name == "BoundingBox") boundingBox = new OnlineMapsHereRoutingAPI.GeoRect(n);
                    else if (n.name == "Shape") shape = GeoCoordinate.ParseArray(n.Value());
                    else if (n.name == "FirstPoint") firstPoint = n.Value<int>();
                    else if (n.name == "LastPoint") lastPoint = n.Value<int>();
                    else if (n.name == "TrafficTime") trafficTime = n.Value<double>();
                    else if (n.name == "BaseTime") baseTime = n.Value<double>();
                    else if (n.name == "Summary") summary = new Summary(n);
                    else if (n.name == "SubLegSummary") ss.Add(new Summary(n));
                    else Debug.Log(n.name + "\n" + n.Value());
                }

                maneuvers = ms.ToArray();
                links = ls.ToArray();
                subLegSummary = ss.ToArray();
            }
        }

        /// <summary>
        /// A maneuver describes the action needed to leave one street segment and enter the following street segment to progress along the route. 
        /// </summary>
        public class Maneuver
        {
            /// <summary>
            /// Key that identifies this element uniquely within the response. 
            /// </summary>
            public string id;

            /// <summary>
            /// Position where the maneuver starts.
            /// </summary>
            public GeoCoordinate position;

            /// <summary>
            /// Description of the required maneuver.
            /// </summary>
            public string instruction;

            /// <summary>
            /// Describes the amount of time in seconds for a single maneuver, traffic considered if this has been enabled. 
            /// </summary>
            public double? travelTime;

            /// <summary>
            /// Length (in meters) for the leg between this maneuver and the next. 
            /// </summary>
            public double? length;

            /// <summary>
            /// Shape of the leg between this maneuver and the next.
            /// </summary>
            public GeoCoordinate[] shape;

            /// <summary>
            /// Index into the global geometry array, pointing to the first point of the shape subsegment associated with this Maneuver.
            /// </summary>
            public int? firstPoint;

            /// <summary>
            /// Index into the global geometry array, pointing to the last point of the shape subsegment associated with this Maneuver.
            /// </summary>
            public int? lastPoint;

            /// <summary>
            /// Estimated point in time when the maneuver should occur, based on the selected transport mode. Options include: <br/>
            /// • Public transport: time at which the user is expected to begin this maneuver, see Public Transport Routing Mode for informations on how to compute the departure time of the line.<br/>
            /// • Private transport: calculated departure time for the maneuver. <br/>
            /// In both cases the time is given in the timezone of the maneuver's starting position.
            /// </summary>
            public double? time;

            /// <summary>
            /// Additional information about the route segment following the maneuver, such as "sharp curve ahead", "accessing toll road", etc. 
            /// </summary>
            public Note[] notes;

            /// <summary>
            /// Reference to the next maneuver on the recommended route. 
            /// </summary>
            public string nextManeuver;

            /// <summary>
            /// The key of the next outgoing link. 
            /// </summary>
            public string toLink;

            /// <summary>
            /// Coordinates defining the bounding box of the entire maneuver. 
            /// </summary>
            public OnlineMapsHereRoutingAPI.GeoRect boundingBox;

            /// <summary>
            /// Shape quality between current maneuver and the next one. Shape quality may vary depending on the transport mode chosen.
            /// </summary>
            public string shapeQuality;

            /// <summary>
            /// Code that identifies the action for this maneuver. Does not always indicate a direction. 
            /// </summary>
            public string action;

            /// <summary>
            /// Name of the next road in the route that the maneuver is heading toward. 
            /// </summary>
            public string nextRoadName;

            /// <summary>
            /// Maneuver direction hint. Can be used to display the appropriate arrow icon for the maneuver. 
            /// </summary>
            public string direction;

            /// <summary>
            /// Name of the road on which the maneuver begins. 
            /// </summary>
            public string roadName;

            /// <summary>
            /// Sign text indicating the direction a driver should follow.
            /// </summary>
            public string signPost;

            /// <summary>
            /// Number of the road where the maneuver starts (for example, A5, B49). 
            /// </summary>
            public string roadNumber;

            /// <summary>
            /// Number of the road (such as A5, B49, etc.) towards which the maneuver is heading. 
            /// </summary>
            public string nextRoadNumber;

            /// <summary>
            /// Name of the freeway exit to be taken at the maneuver. 
            /// </summary>
            public string freewayExit;

            /// <summary>
            /// Name of the freeway junction for the current maneuver. 
            /// </summary>
            public string freewayJunction;

            /// <summary>
            /// Traffic-enabled time. Estimated time in seconds spent on the segment following this maneuver, based on the TrafficSpeed. <br/>
            /// The service may also account for additional time penalties, therefore this may be greater than the link length divided by the traffic speed. 
            /// </summary>
            public double trafficTime;

            /// <summary>
            /// Estimated time in seconds spent on the segment following this maneuver, without considering traffic conditions, as it is based on the BaseSpeed. <br/>
            /// The service may also account for additional time penalties, therefore this may be greater than the link length divided by the base speed. 
            /// </summary>
            public double baseTime;

            /// <summary>
            /// Information that can be used to look up a visual representation of the road shield associated with this maneuver. 
            /// </summary>
            public RoadShield roadShield;

            /// <summary>
            /// Start angle information for the given maneuver, measured in degrees from 0 to 359. A value of 0 represents north, while a value of 90 represents east. Angles increase clockwise. 
            /// </summary>
            public int startAngle;

            /// <summary>
            /// Name of the stop where the user has to leave (action == "Leave"), change (action == "Change") or enter (action == "Enter") the transport line. 
            /// </summary>
            public string stopName;

            /// <summary>
            /// Platform name where the transport line arrives at a station. Applicable for "Leave" and "Change" maneuvers. 
            /// </summary>
            public string arrivalPlatform;

            /// <summary>
            /// Platform name where the transport line departs from a station. Applicable for "Enter" and "Change" maneuvers. 
            /// </summary>
            public string departurePlatform;

            /// <summary>
            /// Reference key to the PublicTransportLine object. To reduce data volume, the PublicTransport element is not directly embedded in the ManeuverType object, but is swapped out into the Route element. 
            /// </summary>
            public string line;

            /// <summary>
            /// Reference key to the PublicTransportLine object for the target line. <br/>
            /// This element is only provided in case of a "change" Maneuver (action == "Change" and if returning of "change" maneuvers has been requested using the "CombineChange" flag in PublicTransportProfile. <br/>
            /// To reduce data volume, the PublicTransport element is not directly embedded in the ManeuverType object, but is swapped out into the Route element. 
            /// </summary>
            public string toLine;

            /// <summary>
            /// Name of the access point where the user has to enter or leave the public transport station. <br/>
            /// Presence of this attribute depends on data availability.
            /// </summary>
            public string accessPointName;

            /// <summary>
            /// Waiting time in seconds applicable to the current maneuver. <br/>
            /// Represents time between start time of maneuver (attribute time) and actual transit departure time.
            /// </summary>
            public double waitTime;

            /// <summary>
            /// When a public transport leg contains estimated times, this value contains the maximum deviation possible. <br/>
            /// If any maneuver contains a value for this field, then the flag containsTimeEstimate will be listed in the publicTransportFlags list of the route summary.
            /// </summary>
            public double timeEstimatePrecision;

            /// <summary>
            /// A list of reference keys to all PublicTransportTicket objects that correspond to the public transport journey starting at this maneuver, i.e. "Enter" and "Change" maneuvers when some ticket covers this maneuver.
            /// </summary>
            public string ticket;

            /// <summary>
            /// Departure delay in seconds applicable to the current maneuver. <br/>
            /// Represents the difference between the actual transit departure time taken from the real time information and the scheduled departure time. <br/>
            /// The delay can have negative value, meaning that the public transport departed earlier than scheduled. <br/>
            /// If the real time information is not available or the delay is zero, the DepartureDelay element is missing.
            /// </summary>
            public double departureDelay;

            /// <summary>
            /// Arrival delay in seconds applicable to the current maneuver. <br/>
            /// Represents the difference between the actual transit arrival time taken from the real time information and the scheduled arrival time. <br/>
            /// The delay can have negative value, meaning that the public transport arrived earlier than scheduled. <br/>
            /// If the real time information is not available or the delay is zero, the ArrivalDelay element is missing.
            /// </summary>
            public double arrivalDelay;

            public Maneuver(OnlineMapsXML node)
            {
                id = node.A("id");

                List<Note> ns = new List<Note>();

                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Position") position = new GeoCoordinate(n);
                    else if (n.name == "Instruction") instruction = n.Value();
                    else if (n.name == "TravelTime") travelTime = n.Value<double>();
                    else if (n.name == "Length") length = n.Value<double>();
                    else if (n.name == "Shape") shape = GeoCoordinate.ParseArray(n.Value());
                    else if (n.name == "FirstPoint") firstPoint = n.Value<int>();
                    else if (n.name == "LastPoint") lastPoint = n.Value<int>();
                    else if (n.name == "Time") time = n.Value<double>();
                    else if (n.name == "Note") ns.Add(new Note(n));
                    else if (n.name == "NextManeuver") nextManeuver = n.Value();
                    else if (n.name == "ToLink") toLink = n.Value();
                    else if (n.name == "BoundingBox") boundingBox = new OnlineMapsHereRoutingAPI.GeoRect(n);
                    else if (n.name == "ShapeQuality") shapeQuality = n.Value();

                    else if (n.name == "Action") action = n.Value();
                    else if (n.name == "NextRoadName") nextRoadName = n.Value();

                    else if (n.name == "Direction") direction = n.Value();
                    else if (n.name == "RoadName") roadName = n.Value();
                    else if (n.name == "SignPost") signPost = n.Value();
                    else if (n.name == "RoadNumber") roadNumber = n.Value();
                    else if (n.name == "NextRoadNumber") nextRoadNumber = n.Value();
                    else if (n.name == "FreewayExit") freewayExit = n.Value();
                    else if (n.name == "FreewayJunction") freewayJunction = n.Value();
                    else if (n.name == "TrafficTime") trafficTime = n.Value<double>();
                    else if (n.name == "BaseTime") baseTime = n.Value<double>();
                    else if (n.name == "RoadShield") roadShield = new RoadShield(n);
                    else if (n.name == "StartAngle") startAngle = n.Value<int>();

                    else if (n.name == "StopName") stopName = n.Value();
                    else if (n.name == "ArrivalPlatform") arrivalPlatform = n.Value();
                    else if (n.name == "DeparturePlatform") departurePlatform = n.Value();
                    else if (n.name == "Line") line = n.Value();
                    else if (n.name == "ToLine") toLine = n.Value();
                    else if (n.name == "AccessPointName") accessPointName = n.Value();
                    else if (n.name == "WaitTime") waitTime = n.Value<double>();
                    else if (n.name == "TimeEstimatePrecision") timeEstimatePrecision = n.Value<double>();
                    else if (n.name == "Ticket") ticket = n.Value();
                    else if (n.name == "DepartureDelay") departureDelay = n.Value<double>();
                    else if (n.name == "ArrivalDelay") arrivalDelay = n.Value<double>();

                    else Debug.Log(n.name + "\n" + n.outerXml);
                }

                notes = ns.ToArray();
            }

            /// <summary>
            /// Contains information used to look up road shield imagery. 
            /// </summary>
            public class RoadShield
            {
                /// <summary>
                /// A string identifying the region where this road shield is used. <br/>
                /// This may be used to differentiate roadshield images by country. 
                /// </summary>
                public string region;

                /// <summary>
                /// A string identifying the category of the road shield, such as highways. 
                /// </summary>
                public string category;

                /// <summary>
                /// A Label identfying the inscription on the road shield, such as containing the road number. 
                /// </summary>
                public string label;

                public RoadShield(OnlineMapsXML node)
                {
                    foreach (OnlineMapsXML n in node)
                    {
                        if (n.name == "Region") region = n.Value();
                        else if (n.name == "Category") category = n.Value();
                        else if (n.name == "Label") label = n.Value();
                        else Debug.Log(n.name + "\n" + n.outerXml);
                    }
                }
            }
        }

        /// <summary>
        /// Maneuver groups organize maneuvers into sections based on TransportModeType to better provide the user with an itinerary summary and brief route instructions. 
        /// </summary>
        public class ManueverGroup
        {
            /// <summary>
            /// ID of the first maneuver in a group. 
            /// </summary>
            public string firstmaneuver;

            /// <summary>
            /// ID of the last maneuver in a group. 
            /// </summary>
            public string lastmaneuver;

            /// <summary>
            /// Settings for route calculation. One mode can be specified for each route. 
            /// </summary>
            public OnlineMapsHereRoutingAPI.RoutingMode mode;

            /// <summary>
            /// Describes summary of a maneuver group. 
            /// </summary>
            public string summaryDescription;

            /// <summary>
            /// Describes arrival instructions of a maneuver group. 
            /// </summary>
            public string arrivalDescription;

            /// <summary>
            /// Describes wait instructions of a maneuver group. 
            /// </summary>
            public string waitDescription;

            /// <summary>
            /// Type of public transport used in a group of maneuvers. 
            /// </summary>
            public string publicTransportType;

            public ManueverGroup(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "firstmaneuver") firstmaneuver = n.Value();
                    else if (n.name == "lastmaneuver") lastmaneuver = n.Value();
                    else if (n.name == "mode") mode = OnlineMapsHereRoutingAPI.RoutingMode.Parse(n);
                    else if (n.name == "summaryDescription") summaryDescription = n.Value();
                    else if (n.name == "arrivalDescription") arrivalDescription = n.Value();
                    else if (n.name == "waitDescription") waitDescription = n.Value();
                    else if (n.name == "publicTransportType") publicTransportType = n.Value();
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }
        }

        /// <summary>
        /// Route notes are used to store additional information about the route. <br/>
        /// These notes can either be related to the calculation itself (like violated routing options) or to the characteristics of the route (like entering a toll road, passing a border, etc.). 
        /// </summary>
        public class Note
        {
            /// <summary>
            /// Type of the note.
            /// </summary>
            public string type;

            /// <summary>
            /// A code that uniquely identifies the note. This code can be used to decide how to display the note (such as with a warning icon). 
            /// </summary>
            public string code;

            /// <summary>
            /// A short text describing the note. Please note that this attribute is not subject to internationalization and should therefore not be used in user displays. 
            /// </summary>
            public string text;

            /// <summary>
            /// Container for additional data to be stored along with the note. 
            /// </summary>
            public Dictionary<string, string> additionalData;

            public Note(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Type") type = n.Value();
                    else if (n.name == "Code") code = n.Value();
                    else if (n.name == "Text") text = n.Value();
                    else if (n.name == "AdditionalData") additionalData.Add(n.A("key"), n.Value());
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }
        }

        /// <summary>
        /// Rather than providing the entire route summary as a single summary, you can use this type to return summary information for each country in the route. 
        /// </summary>
        public class SummaryByCountry: Summary
        {
            /// <summary>
            /// Country code of the associated route summary, using the ISO 3166-1-alpha-3 format. 
            /// </summary>
            public string country;

            /// <summary>
            /// Total distance of toll roads on the route within the country, in meters.
            /// </summary>
            public double tollRoadDistance;

            public SummaryByCountry(OnlineMapsXML node) : base(node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Country") country = n.Value();
                    else if (n.name == "TollRoadDistance") tollRoadDistance = n.Value<double>();
                }
            }
        }

        /// <summary>
        /// Defines the information available for a public transport line. 
        /// </summary>
        public class PublicTransportLine
        {
            public string id;

            /// <summary>
            /// Name of the line
            /// </summary>
            public string lineName;

            /// <summary>
            /// Color that is to be used as foreground color when drawing the line.
            /// </summary>
            public string lineForeground;

            /// <summary>
            /// Color that is to be used as background color when drawing the line. 
            /// </summary>
            public string lineBackground;

            /// <summary>
            /// Style that is to be used when drawing the line. 
            /// </summary>
            public string lineStyle;

            /// <summary>
            /// Name of the transit lines company 
            /// </summary>
            public string companyName;

            /// <summary>
            /// Short name of the transit lines company
            /// </summary>
            public string companyShortName;

            /// <summary>
            /// Logo of the transit lines company
            /// </summary>
            public ExternalResource companyLogo;

            /// <summary>
            /// Final destination of the transport line 
            /// </summary>
            public string destination;

            /// <summary>
            /// Additional attributes classifying the transport line 
            /// </summary>
            public string flags;

            /// <summary>
            /// Type of the transport line
            /// </summary>
            public string type;

            /// <summary>
            /// Name of the transport line's type (such as "ICE", "TGV", etc.). <br/>
            /// Note: This type is not officially supported.
            /// </summary>
            public string typeName;

            /// <summary>
            /// List of all stops on this public transport line. 
            /// </summary>
            public Stop[] stops;

            public PublicTransportLine(OnlineMapsXML node)
            {
                List<Stop> sts = new List<Stop>();
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "LineName") lineName = n.Value();
                    else if (n.name == "LineForeground") lineForeground = n.Value();
                    else if (n.name == "LineBackground") lineBackground = n.Value();
                    else if (n.name == "LineStyle") lineStyle = n.Value();
                    else if (n.name == "CompanyName") companyName = n.Value();
                    else if (n.name == "CompanyShortName") companyShortName = n.Value();
                    else if (n.name == "CompanyLogo") companyLogo = new ExternalResource(n);
                    else if (n.name == "Destination") destination = n.Value();
                    else if (n.name == "Flags") flags = n.Value();
                    else if (n.name == "Type") type = n.Value();
                    else if (n.name == "TypeName") typeName = n.Value();
                    else if (n.name == "id") id = n.Value();
                    else if (n.name == "Stop") sts.Add(new Stop(n));
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
                stops = sts.ToArray();
            }

            /// <summary>
            /// Reference to an external resource (for example, a bitmap). <br/>
            /// The client is responsible for retrieving the referenced resource. 
            /// </summary>
            public class ExternalResource
            {
                /// <summary>
                /// The semantics of the resource.
                /// </summary>
                public string resourceType;

                /// <summary>
                /// Filename of the resource
                /// </summary>
                public string filename;

                public ExternalResource(OnlineMapsXML node)
                {
                    foreach (OnlineMapsXML n in node)
                    {
                        if (n.name == "ResourceType") resourceType = n.Value();
                        else if (n.name == "filename") filename = n.Value();
                        else Debug.Log(n.name + "\n" + n.outerXml);
                    }
                }
            }

            /// <summary>
            /// Represent stops on a public transport line. 
            /// Stops are different from stations as they are bound to a specific public transport line and a direction of travel, indicated by the line's destination. 
            /// </summary>
            public class Stop
            {
                /// <summary>
                /// Key that identifies this element uniquely within the response. 
                /// </summary>
                public string id;

                /// <summary>
                /// The position of this stop. 
                /// </summary>
                public GeoCoordinate position;

                /// <summary>
                /// Reference key to the PublicTransportLine object 
                /// </summary>
                public string line;

                /// <summary>
                /// The name of this stop. 
                /// </summary>
                public string stopName;

                /// <summary>
                /// The time in seconds required to travel from this stop to the next one using the public transport line specified in the Line element. <br/>
                /// Note that this value can be 0. 
                /// </summary>
                public double travelTime;

                public Stop(OnlineMapsXML node)
                {
                    foreach (OnlineMapsXML n in node)
                    {
                        if (n.name == "id") id = n.Value();
                        else if (n.name == "Position") position = new GeoCoordinate(n);
                        else if (n.name == "Line") line = n.Value();
                        else if (n.name == "StopName") stopName = n.Value();
                        else if (n.name == "TravelTime") travelTime = n.Value<double>();
                        else Debug.Log(n.name + "\n" + n.outerXml);
                    }
                }
            }
        }

        public class PublicTransportTickets
        {
            /// <summary>
            /// Sequential ID of the ticket set -- T1, T2, etc.
            /// </summary>
            public string id;

            /// <summary>
            /// Information about a single ticket in this set of tickets
            /// </summary>
            public Ticket[] tickets;

            public PublicTransportTickets(OnlineMapsXML node)
            {
                List<Ticket> ts = new List<Ticket>();
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "id") id = n.Value();
                    else if (n.name == "PublicTransportTicket") ts.Add(new Ticket(n));
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
                tickets = ts.ToArray();
            }

            /// <summary>
            /// Information about a single ticket
            /// </summary>
            public class Ticket
            {
                /// <summary>
                /// Sequential ID of the ticket based on the parent ticket set. Set T1 contains T1.1, T1.2, ...; T2 contains T2.1, T2.2, ..., etc.
                /// </summary>
                public string id;

                /// <summary>
                /// The name of the ticket in the local transport system.
                /// </summary>
                public string ticketName;

                /// <summary>
                /// The ISO-4217 code of the currency the ticket price is listed in.
                /// </summary>
                public string currency;

                /// <summary>
                /// The price of the ticket as a floating point number.
                /// </summary>
                public double price;

                public Ticket(OnlineMapsXML node)
                {
                    foreach (OnlineMapsXML n in node)
                    {
                        if (n.name == "TicketName") ticketName = n.Value();
                        else if (n.name == "Currency") currency = n.Value();
                        else if (n.name == "Price") price = n.Value<double>();
                        else if (n.name == "id") id = n.Value();
                        else Debug.Log(n.name + "\n" + n.outerXml);
                    }
                }
            }
        }

        /// <summary>
        /// This type provides summary information for the entire route. <br/>
        /// This type of information includes travel time, distance, and descriptions of the overall route path. 
        /// </summary>
        public class Summary
        {
            /// <summary>
            /// Indicates total travel distance for the route, in meters. 
            /// </summary>
            public double distance;

            /// <summary>
            /// Contains the travel time estimate in seconds for this element, considering traffic and transport mode. <br/>
            /// Based on the TrafficSpeed. The service may also account for additional time penalties, so this may be greater than the element length divided by the TrafficSpeed. 
            /// </summary>
            public double trafficTime;

            /// <summary>
            /// Contains the travel time estimate in seconds for this element, considering transport mode but not traffic conditions. <br/>
            /// Based on the BaseSpeed. The service may also account for additional time penalties, therefore this may be greater than the element length divided by the BaseSpeed. 
            /// </summary>
            public double baseTime;

            /// <summary>
            /// Special link characteristics (like ferry or motorway usage) which are covered by the route. 
            /// </summary>
            public string[] flags;

            /// <summary>
            /// Total travel time in seconds optionally considering traffic depending on the request parameters. 
            /// </summary>
            public double travelTime;

            /// <summary>
            /// Textual description of route summary. 
            /// </summary>
            public string text;

            /// <summary>
            /// Matrix Routing relevant only. 
            /// </summary>
            public int costFactor;

            /// <summary>
            /// Estimation of the carbon dioxyde emmision for the given Route. <br/>
            /// The value depends on the VehicleType request parameter which specifies the average fuel consumption per 100 km and the type of combustion engine (diesel or gasoline). <br/>
            /// Unit is kilograms with precision to three decimal places. 
            /// </summary>
            public double co2Emission;
            public string departure; // Undocumented in here.com

            public Summary(OnlineMapsXML node)
            {
                List<string> fs = new List<string>();
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Distance") distance = n.Value<double>();
                    else if (n.name == "TrafficTime") trafficTime = n.Value<double>();
                    else if (n.name == "BaseTime") baseTime = n.Value<double>();
                    else if (n.name == "Flags") fs.Add(n.Value());
                    else if (n.name == "TravelTime") travelTime = n.Value<double>();
                    else if (n.name == "Text") text = n.Value();
                    else if (n.name == "CostFactor") costFactor = n.Value<int>();
                    else if (n.name == "Co2Emission") co2Emission = n.Value<double>();
                    else if (n.name == "Departure") departure = n.Value();
                    else Debug.Log(n.name + "\n" + n.Value());
                }
                flags = fs.ToArray();
            }
        }

        /// <summary>
        /// Waypoints are points (including start and end points) along the route, based on input specified in the route request. <br/>
        /// They can also be defined as passThrough, such as a case where the road changes names and no stopover action is required. <br/> 
        /// If the the request does not pass a coordinate when specifying the waypoint, the originalPosition attribute will not be filled.
        /// </summary>
        public class Waypoint
        {
            /// <summary>
            /// ID of the link on the navigable network associated with the waypoint. 
            /// </summary>
            public string linkId;

            /// <summary>
            /// If this waypoint is a start point, this will be mapped to the beginning of the link. <br/>
            /// If used as destination point or via point, it will be mapped to the end of the link. 
            /// </summary>
            public GeoCoordinate mappedPosition;

            /// <summary>
            /// Original position as it was specified in the corresponding request. <br/>
            /// The value will depend on request construction: <br/>
            /// If using a NavigationWaypointParameterType, the service will set OriginalPosition as the display position (if specified) or as the navigation position selected by the routing engine(if not specified in the request). <br/>
            /// If using a GeoWaypointParameterType, the service will set the OriginalPosition as the position specified in the request.
            /// </summary>
            public GeoCoordinate originalPosition;

            /// <summary>
            /// Defines the type of the waypoint, either stopOver or passThrough. 
            /// </summary>
            public string type;

            /// <summary>
            /// Contains the relative position of the mapped location along the link, as the fractional distance between the link's reference node and the non-reference node. <br/>
            /// Ranges in value from 0 to 1. When no spot value nor display position is given in the request then default value 0.5 is assumed. 
            /// </summary>
            public double? spot;

            /// <summary>
            /// Indicates whether the waypoint is on the left or right side of the link, when heading from the reference node to the non-reference node. 
            /// </summary>
            public string sideOfStreet;

            /// <summary>
            /// Displays the name of the street to which the request waypoint was mapped. 
            /// </summary>
            public string mappedRoadName;

            /// <summary>
            /// A label identifying this waypoint, generated by the routing service. <br/>
            /// Label is either a street name or a public transport stop, depending on the transport mode of the request. 
            /// </summary>
            public string label;

            /// <summary>
            /// Used to identify a waypoint point with a custom name. Copied verbatim as specified in the request. 
            /// </summary>
            public string userLabel;

            /// <summary>
            /// Specifies the index of this waypoint, based on the global shape array that is provided at the route level. 
            /// </summary>
            public int? shapeIndex;

            public Waypoint(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "LinkId") linkId = n.Value();
                    else if (n.name == "MappedPosition") mappedPosition = new GeoCoordinate(n);
                    else if (n.name == "OriginalPosition") originalPosition = new GeoCoordinate(n);
                    else if (n.name == "Type") type = n.Value();
                    else if (n.name == "Spot") spot = n.Value<double>();
                    else if (n.name == "SideOfStreet") sideOfStreet = n.Value();
                    else if (n.name == "MappedRoadName") mappedRoadName = n.Value();
                    else if (n.name == "Label") label = n.Value();
                    else if (n.name == "UserLabel") userLabel = n.Value();
                    else if (n.name == "ShapeIndex") shapeIndex = n.Value<int>();
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }
        }
    }

    /// <summary>
    /// Source attribution contains information that must be displayed in the user interface of applications using Routing API. <br/>
    /// Source attributions are produced, for example, when route is using information available through certain public transportation data providers. <br/>
    /// Attribution contains localized ready-to-display string with HTML markup as well as structured information that could be used to get data without parsing attribution string.
    /// </summary>
    public class SourceAttribution
    {
        /// <summary>
        /// Ready-to-display attribution string with HTML markup
        /// </summary>
        public string attribution;

        /// <summary>
        /// Structured information about source supplier. 
        /// </summary>
        public Supplier supplier;

        public SourceAttribution(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "Attribution") attribution = n.Value();
                else if (n.name == "Supplier") supplier = new Supplier(n);
                else Debug.Log(n.name + "\n" + n.outerXml);
            }
        }

        /// <summary>
        /// Contains structured information about source data supplier. 
        /// </summary>
        public class Supplier
        {
            /// <summary>
            /// Source data supplier title.
            /// </summary>
            public string title;

            /// <summary>
            /// Link to source data supplier's website.
            /// </summary>
            public string href;

            /// <summary>
            /// Notes giving additional information about source data supplier. 
            /// </summary>
            public Note note;

            public Supplier(OnlineMapsXML node)
            {
                foreach (OnlineMapsXML n in node)
                {
                    if (n.name == "Title") title = n.Value();
                    else if (n.name == "Href") href = n.Value();
                    else if (n.name == "Note") note = new Note(n);
                    else Debug.Log(n.name + "\n" + n.outerXml);
                }
            }

            /// <summary>
            /// Contains information related specifically to data providers. <br/>
            /// Informaiton consists of ready-to-display text and structured information if any is available.
            /// </summary>
            public class Note
            {
                /// <summary>
                /// Type of note.
                /// </summary>
                public string type;

                /// <summary>
                /// Ready-to-display note text. The text may contain HTML text and markup, including hyperlinks.
                /// </summary>
                public string text;

                /// <summary>
                /// URL, to which note is referring, if any.
                /// </summary>
                public string href;

                /// <summary>
                /// Text, displayed with URL, to which note is referring, if any.
                /// </summary>
                public string hrefText;

                public Note(OnlineMapsXML node)
                {
                    foreach (OnlineMapsXML n in node)
                    {
                        if (n.name == "Type") type = n.Value();
                        else if (n.name == "Text") text = n.Value();
                        else if (n.name == "Href") href = n.Value();
                        else if (n.name == "HrefText") hrefText = n.Value();
                        else Debug.Log(n.name + "\n" + n.outerXml);
                    }
                }
            }
        }
    }
}