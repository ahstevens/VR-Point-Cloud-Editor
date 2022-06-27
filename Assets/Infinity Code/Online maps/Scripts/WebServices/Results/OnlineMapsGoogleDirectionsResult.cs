/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Result of Google Direction API.
/// </summary>
public class OnlineMapsGoogleDirectionsResult
{
    /// <summary>
    /// Metadata on the request.
    /// </summary>
    public string status;

    /// <summary>
    /// When the status code is other than OK, there may be an additional error_message field within the Directions response object. <br/>
    /// This field contains more detailed information about the reasons behind the given status code.
    /// </summary>
    public string error_message;

    /// <summary>
    /// Array of routes from the origin to the destination.
    /// </summary>
    public Route[] routes;

    /// <summary>
    /// Array with details about the geocoding of origin, destination and waypoints.
    /// </summary>
    public GeocodedWaypoint[] geocoded_waypoints;

    public OnlineMapsGoogleDirectionsResult(){}

    public OnlineMapsGoogleDirectionsResult(OnlineMapsXML xml)
    {
        List<Route> routes = new List<Route>();
        List<GeocodedWaypoint> geocodedWaypoints = new List<GeocodedWaypoint>();

        foreach (OnlineMapsXML node in xml)
        {
            if (node.name == "status") status = node.Value();
            else if (node.name == "error_message") error_message = node.Value();
            else if (node.name == "route") routes.Add(new Route(node));
            else if (node.name == "geocoded_waypoint") geocodedWaypoints.Add(new GeocodedWaypoint(node));
            else Debug.Log("Result: " + node.name + "\n" + node.outerXml);
        }

        this.routes = routes.ToArray();
        geocoded_waypoints = geocodedWaypoints.ToArray();
    }

    /// <summary>
    /// Total fare on route. 
    /// </summary>
    public class Fare
    {
        /// <summary>
        /// An ISO 4217 currency code indicating the currency that the amount is expressed in.
        /// </summary>
        public string currency;

        /// <summary>
        /// The total fare amount, in the currency specified above.
        /// </summary>
        public double value;

        /// <summary>
        /// The total fare amount, formatted in the requested language.
        /// </summary>
        public string text;

        public Fare()
        {
            
        }

        public Fare(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "currency") currency = n.Value();
                else if (n.name == "value") value = n.Value<double>();
                else if (n.name == "text") text = n.Value();
                else Debug.Log("Fare: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    /// <summary>
    /// Details about the geocoding of origin, destination and waypoints
    /// </summary>
    public class GeocodedWaypoint
    {
        /// <summary>
        /// Status code resulting from the geocoding operation.
        /// </summary>
        public string geocoder_status;

        /// <summary>
        /// Address type of the geocoding result used for calculating directions.
        /// </summary>
        public string[] types;

        /// <summary>
        /// Unique identifier that can be used with other Google APIs.
        /// </summary>
        public string place_id;

        /// <summary>
        /// Indicates that the geocoder did not return an exact match for the original request, though it was able to match part of the requested address. <br/>
        /// You may wish to examine the original request for misspellings and/or an incomplete address.
        /// </summary>
        public string partial_match;

        public GeocodedWaypoint()
        {
            
        }

        public GeocodedWaypoint(OnlineMapsXML node)
        {
            List<string> types = new List<string>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "geocoder_status") geocoder_status = n.Value();
                else if (n.name == "type") types.Add(n.Value());
                else if (n.name == "place_id") place_id = n.Value();
                else if (n.name == "partial_match") partial_match = n.Value();
                else Debug.Log("GeocodedWaypoint: " + n.name + "\n" + n.outerXml);
            }

            this.types = types.ToArray();
        }
    }

    /// <summary>
    /// Single leg of the journey from the origin to the destination in the calculated route.
    /// </summary>
    public class Leg
    {
        /// <summary>
        /// Array of steps denoting information about each separate step of the leg of the journey.
        /// </summary>
        public Step[] steps;

        /// <summary>
        /// Total duration of this leg.
        /// </summary>
        public TextValue<int> duration;

        /// <summary>
        /// Total duration of this leg. This value is an estimate of the time in traffic based on current and historical traffic conditions.
        /// </summary>
        public TextValue<int> duration_in_traffic;

        /// <summary>
        /// Total distance covered by this leg.
        /// </summary>
        public TextValue<int> distance;

        /// <summary>
        /// Coordinates of the origin of this leg. Because the Directions API calculates directions between locations by using the nearest transportation option (usually a road) at the start and end points, start_location may be different than the provided origin of this leg if, for example, a road is not near the origin.
        /// </summary>
        public OnlineMapsVector2d start_location;

        /// <summary>
        /// Coordinates of the given destination of this leg. Because the Google Maps Directions API calculates directions between locations by using the nearest transportation option (usually a road) at the start and end points, end_location may be different than the provided destination of this leg if, for example, a road is not near the destination.
        /// </summary>
        public OnlineMapsVector2d end_location;

        /// <summary>
        /// Human-readable address (typically a street address) resulting from reverse geocoding the start_location of this leg.
        /// </summary>
        public string start_address;

        /// <summary>
        /// Human-readable address (typically a street address) from reverse geocoding the end_location of this leg.
        /// </summary>
        public string end_address;

        public ViaWaypoint via_waypoint;

        /// <summary>
        /// Estimated time of arrival for this leg. This property is only returned for transit directions.
        /// </summary>
        public TextValueZone<string> arrival_time;

        /// <summary>
        /// Estimated time of departure for this leg, specified as a Time object. The departure_time is only available for transit directions.
        /// </summary>
        public TextValueZone<string> departure_time;

        public Leg()
        {
            
        }

        public Leg(OnlineMapsXML node)
        {
            List<Step> steps = new List<Step>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "step") steps.Add(new Step(n));
                else if (n.name == "duration") duration = new TextValue<int>(n);
                else if (n.name == "duration_in_traffic") duration_in_traffic = new TextValue<int>(n);
                else if (n.name == "distance") distance = new TextValue<int>(n);
                else if (n.name == "start_location") start_location = OnlineMapsXML.GetVector2dFromNode(n);
                else if (n.name == "end_location") end_location = OnlineMapsXML.GetVector2dFromNode(n);
                else if (n.name == "start_address") start_address = n.Value();
                else if (n.name == "end_address") end_address = n.Value();
                else if (n.name == "via_waypoint") via_waypoint = new ViaWaypoint(n);
                else if (n.name == "arrival_time") arrival_time = new TextValueZone<string>(n);
                else if (n.name == "departure_time") departure_time = new TextValueZone<string>(n);
                else Debug.Log("Leg: " + n.name + "\n" + n.outerXml);
            }

            this.steps = steps.ToArray();
        }
    }

    /// <summary>
    /// information about the transit line used in this step.
    /// </summary>
    public class Line
    {
        /// <summary>
        /// Full name of this transit line. eg. "7 Avenue Express".
        /// </summary>
        public string name;

        /// <summary>
        /// Short name of this transit line. This will normally be a line number, such as "M7" or "355".
        /// </summary>
        public string short_name;

        /// <summary>
        /// Color commonly used in signage for this transit line. The color will be specified as a hex string such as: #FF0033.
        /// </summary>
        public string color;

        /// <summary>
        /// Array of TransitAgency objects that each provide information about the operator of the line
        /// </summary>
        public TransitAgency[] agencies;

        /// <summary>
        /// URL for this transit line as provided by the transit agency.
        /// </summary>
        public string url;

        /// <summary>
        /// URL for the icon associated with this line.
        /// </summary>
        public string icon;

        /// <summary>
        /// Color of text commonly used for signage of this line. The color will be specified as a hex string.
        /// </summary>
        public string text_color;

        /// <summary>
        /// Type of vehicle used on this line.
        /// </summary>
        public Vehicle vehicle;

        public Line()
        {
            
        }

        public Line(OnlineMapsXML node)
        {
            List<TransitAgency> agencies = new List<TransitAgency>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "short_name") short_name = n.Value();
                else if (n.name == "color") color = n.Value();
                else if (n.name == "agency") agencies.Add(new TransitAgency(n));
                else if (n.name == "url") url = n.Value();
                else if (n.name == "icon") icon = n.Value();
                else if (n.name == "text_color") text_color = n.Value();
                else if (n.name == "vehicle") vehicle = new Vehicle(n);
                else Debug.Log("Line: " + n.name + "\n" + n.outerXml);
            }

            this.agencies = agencies.ToArray();
        }
    }

    public class NameLocation
    {
        public string name;
        public OnlineMapsVector2d location;

        public NameLocation()
        {
            
        }

        public NameLocation(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "location") location = OnlineMapsXML.GetVector2dFromNode(n);
                else if (n.name == "name") name = n.Value();
                else Debug.Log("NameLocation: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    /// <summary>
    /// Route from the origin to the destination
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Short textual description for the route, suitable for naming and disambiguating the route from alternatives.
        /// </summary>
        public string summary;

        /// <summary>
        /// Array which contains information about a leg of the route, between two locations within the given route. <br/>
        /// A separate leg will be present for each waypoint or destination specified.
        /// </summary>
        public Leg[] legs;

        /// <summary>
        /// Copyrights text to be displayed for this route. You must handle and display this information yourself.
        /// </summary>
        public string copyrights;

        /// <summary>
        /// Array indicating the order of any waypoints in the calculated route.
        /// </summary>
        public int[] waypoint_order;

        /// <summary>
        /// Single points object that holds an encoded polyline representation of the route. <br/>
        /// This polyline is an approximate (smoothed) path of the resulting directions.
        /// </summary>
        public Vector2[] overview_polyline;

        /// <summary>
        /// Single points object that holds an encoded polyline representation of the route. <br/>
        /// This polyline is an approximate (smoothed) path of the resulting directions.
        /// </summary>
        public OnlineMapsVector2d[] overview_polylineD;

        /// <summary>
        /// Viewport bounding box of the overview_polyline.
        /// </summary>
        public OnlineMapsGPXObject.Bounds bounds;

        /// <summary>
        /// Array of warnings to be displayed when showing these directions. You must handle and display these warnings yourself.
        /// </summary>
        public string[] warnings;

        /// <summary>
        /// If present, contains the total fare (that is, the total ticket costs) on this route. <br/>
        /// This property is only returned for transit requests and only for routes where fare information is available for all transit legs.
        /// </summary>
        public Fare fare;

        public Route()
        {
            
        }

        public Route(OnlineMapsXML node)
        {
            List<Leg> legs = new List<Leg>();
            List<int> waypointOrder = new List<int>();
            List<string> warnings = new List<string>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "summary") summary = n.Value();
                else if (n.name == "leg") legs.Add(new Leg(n));
                else if (n.name == "copyrights") copyrights = n.Value();
                else if (n.name == "overview_polyline")
                {
                    overview_polylineD = OnlineMapsUtils.DecodePolylinePointsD(n["points"].Value()).ToArray();
                    overview_polyline = overview_polylineD.Select(p => (Vector2) p).ToArray();
                }
                else if (n.name == "waypoint_index") waypointOrder.Add(n.Value<int>());
                else if (n.name == "warning") warnings.Add(n.Value());
                else if (n.name == "fare") fare = new Fare(n);
                else if (n.name == "bounds")
                {
                    OnlineMapsXML sw = n["southwest"];
                    OnlineMapsXML ne = n["northeast"];
                    bounds = new OnlineMapsGPXObject.Bounds(sw.Get<double>("lng"), sw.Get<double>("lat"), ne.Get<double>("lng"), ne.Get<double>("lat"));
                }
                else Debug.Log("Route: " + n.name + "\n" + n.outerXml);
            }

            this.legs = legs.ToArray();
            waypoint_order = waypointOrder.ToArray();
            this.warnings = warnings.ToArray();
        }
    }

    /// <summary>
    /// Single step of the calculated directions.
    /// </summary>
    public class Step
    {
        public string travel_mode;

        /// <summary>
        /// Location of the starting point of this step
        /// </summary>
        public OnlineMapsVector2d start_location;

        /// <summary>
        /// Location of the last point of this step
        /// </summary>
        public OnlineMapsVector2d end_location;

        /// <summary>
        /// Array that holds an polyline representation of the step. This polyline is an approximate (smoothed) path of the step.
        /// </summary>
        public Vector2[] polyline;

        /// <summary>
        /// Array that holds an polyline representation of the step. This polyline is an approximate (smoothed) path of the step.
        /// </summary>
        public OnlineMapsVector2d[] polylineD;

        /// <summary>
        /// Typical time required to perform the step, until the next step.
        /// </summary>
        public TextValue<int> duration;

        /// <summary>
        /// Distance covered by this step until the next step.
        /// </summary>
        public TextValue<int> distance;

        /// <summary>
        /// Formatted instructions for this step, presented as an HTML text string.
        /// </summary>
        public string html_instructions;

        /// <summary>
        /// Formatted instructions for this step, presented as an text string without HTML tags.
        /// </summary>
        public string string_instructions;

        /// <summary>
        /// Maneuver the current step.
        /// </summary>
        public string maneuver;

        /// <summary>
        /// Transit specific information. This field is only returned with travel_mode is set to "transit".
        /// </summary>
        public TransitDetails transit_details;

        /// <summary>
        /// Detailed directions for walking or driving steps in transit directions. Substeps are only available when travel_mode is set to "transit".
        /// </summary>
        public Step[] steps;

        public Step()
        {
            
        }

        public Step(OnlineMapsXML node)
        {
            List<Step> steps = new List<Step>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "travel_mode") travel_mode = n.Value();
                else if (n.name == "start_location") start_location = OnlineMapsXML.GetVector2dFromNode(n);
                else if (n.name == "end_location") end_location = OnlineMapsXML.GetVector2dFromNode(n);
                else if (n.name == "polyline")
                {
                    polylineD = OnlineMapsUtils.DecodePolylinePointsD(n["points"].Value()).ToArray();
                    polyline = polylineD.Select(p => (Vector2)p).ToArray();
                }
                else if (n.name == "duration") duration = new TextValue<int>(n);
                else if (n.name == "distance") distance = new TextValue<int>(n);
                else if (n.name == "step") steps.Add(new Step(n));
                else if (n.name == "html_instructions")
                {
                    html_instructions = n.Value();
                    if (string.IsNullOrEmpty(html_instructions)) return;
                    string_instructions = OnlineMapsUtils.StrReplace(html_instructions,
                        new[] { "&lt;", "&gt;", "&nbsp;", "&amp;", "&amp;nbsp;" },
                        new[] { "<", ">", " ", "&", " " });
                    string_instructions = Regex.Replace(string_instructions, "<div.*?>", "\n");
                    string_instructions = Regex.Replace(string_instructions, "<.*?>", string.Empty);
                }
                else if (n.name == "maneuver") maneuver = n.Value();
                else if (n.name == "transit_details") transit_details = new TransitDetails(n);
                else Debug.Log("Step: " + n.name + "\n" + n.outerXml);
            }

            this.steps = steps.ToArray();
        }
    }

    public class TextValue<T>
    {
        /// <summary>
        /// Human-readable representation of value.
        /// </summary>
        public string text;

        /// <summary>
        /// Value
        /// </summary>
        public T value;

        public TextValue()
        {
            
        }

        public TextValue(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "text") text = n.Value();
                else if (n.name == "value") value = n.Value<T>();
                else Debug.Log("TextValue: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    public class TextValueZone<T>
    {
        /// <summary>
        /// Human-readable representation of value.
        /// </summary>
        public string text;

        /// <summary>
        /// Value
        /// </summary>
        public T value;

        /// <summary>
        /// Time zone of this station. The value is the name of the time zone as defined in the IANA Time Zone Database, e.g. "America/New_York".
        /// </summary>
        public string time_zone;

        public TextValueZone()
        {
            
        }

        public TextValueZone(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "text") text = n.Value();
                else if (n.name == "value") value = n.Value<T>();
                else if (n.name == "time_zone") time_zone = n.Value();
                else Debug.Log("TextValueZone: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    /// <summary>
    /// Information about the operator of the line
    /// </summary>
    public class TransitAgency
    {
        /// <summary>
        /// Name of the transit agency.
        /// </summary>
        public string name;

        /// <summary>
        /// URL for the transit agency.
        /// </summary>
        public string url;

        /// <summary>
        /// Phone number of the transit agency.
        /// </summary>
        public string phone;

        public TransitAgency()
        {
            
        }

        public TransitAgency(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "url") url = n.Value();
                else if (n.name == "phone") phone = n.Value();
                else Debug.Log("TransitAgency: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    /// <summary>
    /// Additional information that is not relevant for other modes of transportation.
    /// </summary>
    public class TransitDetails
    {
        /// <summary>
        /// Information about the stop/station for this part of the trip.
        /// </summary>
        public NameLocation arrival_stop;

        /// <summary>
        /// Information about the stop/station for this part of the trip.
        /// </summary>
        public NameLocation departure_stop;

        /// <summary>
        /// Arrival time for this leg of the journey
        /// </summary>
        public TextValueZone<string> arrival_time;

        /// <summary>
        /// Departure time for this leg of the journey
        /// </summary>
        public TextValueZone<string> departure_time;

        /// <summary>
        /// Direction in which to travel on this line, as it is marked on the vehicle or at the departure stop. This will often be the terminus station.
        /// </summary>
        public string headsign;

        /// <summary>
        /// Expected number of seconds between departures from the same stop at this time. <br/>
        /// For example, with a headway value of 600, you would expect a ten minute wait if you should miss your bus.
        /// </summary>
        public int headway;

        /// <summary>
        /// Number of stops in this step, counting the arrival stop, but not the departure stop. <br/>
        /// For example, if your directions involve leaving from Stop A, passing through stops B and C, and arriving at stop D, num_stops will return 3.
        /// </summary>
        public int num_stops;

        /// <summary>
        /// Information about the transit line used in this step.
        /// </summary>
        public Line line;

        public TransitDetails()
        {
            
        }

        public TransitDetails(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "arrival_stop") arrival_stop = new NameLocation(n);
                else if (n.name == "departure_stop") departure_stop = new NameLocation(n);
                else if (n.name == "arrival_time") arrival_time = new TextValueZone<string>(n);
                else if (n.name == "departure_time") departure_time = new TextValueZone<string>(n);
                else if (n.name == "headsign") headsign = n.Value();
                else if (n.name == "headway") headway = n.Value<int>();
                else if (n.name == "num_stops") num_stops = n.Value<int>();
                else if (n.name == "line") line = new Line(n);
                else Debug.Log("TransitDetails: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    public class ViaWaypoint
    {
        public OnlineMapsVector2d location;
        public int step_index;
        public double step_interpolation;

        public ViaWaypoint()
        {
            
        }

        public ViaWaypoint(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "location") location = OnlineMapsXML.GetVector2dFromNode(n);
                else if (n.name == "step_index") step_index = n.Value<int>();
                else if (n.name == "step_interpolation") step_interpolation = n.Value<double>();
                else Debug.Log("ViaWaypoint: " + n.name + "\n" + n.outerXml);
            }
        }
    }

    /// <summary>
    /// Type of vehicle.
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// Name of the vehicle on this line. eg. "Subway."
        /// </summary>
        public string name;

        /// <summary>
        /// Type of vehicle that runs on this line.
        /// </summary>
        public string type;

        /// <summary>
        /// URL for an icon associated with this vehicle type.
        /// </summary>
        public string icon;

        /// <summary>
        /// URL for the icon associated with this vehicle type, based on the local transport signage.
        /// </summary>
        public string local_icon;

        public Vehicle()
        {
            
        }

        public Vehicle(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "type") type = n.Value();
                else if (n.name == "icon") icon = n.Value();
                else if (n.name == "local_icon") local_icon = n.Value();
                else Debug.Log("Vehicle: " + n.name + "\n" + n.outerXml);
            }
        }
    }
}