/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

/// <summary>
/// This class is used to search for a route by coordinates using Open Route Service.<br/>
/// You can create a new instance using OnlineMapsOpenRouteService.Find.<br/>
/// http://wiki.openstreetmap.org/wiki/OpenRouteService
/// </summary>
public class OnlineMapsOpenRouteService: OnlineMapsTextWebService
{
    private const string endpoint = "https://api.openrouteservice.org/";

    private OnlineMapsOpenRouteService(StringBuilder url, Params p)
    {
        _status = OnlineMapsQueryStatus.downloading;
        p.Append(url);

        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Returns a route between two or more locations for a selected profile and its settings as GeoJSON response.
    /// </summary>
    /// <param name="p">Parameters of request</param>
    /// <returns>Instance of the query</returns>
    [Obsolete("Use OnlineMapsOpenRouteServiceDirections.Find instead")]
    public static OnlineMapsOpenRouteService Directions(DirectionParams p)
    {
        return new OnlineMapsOpenRouteService(new StringBuilder(endpoint).Append("directions?"), p);
    }

    /// <summary>
    /// Resolve input coordinates to addresses and vice versa
    /// </summary>
    /// <param name="p">Parameters of the request</param>
    /// <returns>Instance of the query</returns>
    [Obsolete("Use OnlineMapsOpenRouteServiceGeocode.Search instead")]
    public static OnlineMapsOpenRouteService Geocoding(GeocodingParams p)
    {
        return new OnlineMapsOpenRouteService(new StringBuilder(endpoint).Append("geocoding?"), p);
    }

    /// <summary>
    /// Converts the response string from Open Route Service Directions to result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    [Obsolete("Use OnlineMapsOpenRouteServiceDirections.GetResults instead")]
    public static OnlineMapsOpenRouteServiceDirectionResult GetDirectionResults(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsOpenRouteServiceDirectionResult>(response);
    }

    /// <summary>
    /// Converts the response string from Open Route Service Geocoding to result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    [Obsolete("Use OnlineMapsOpenRouteServiceGeocode.GetResults instead")]
    public static OnlineMapsOpenRouteServiceGeocodingResult GetGeocodingResults(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsOpenRouteServiceGeocodingResult>(response);
    }

    /// <summary>
    /// Base class of parameters of the request to Open Route Service.
    /// </summary>
    public abstract class Params
    {
        /// <summary>
        /// Open Route Service API key
        /// </summary>
        protected string key;

        /// <summary>
        /// Arbitrary identification string of the request reflected in the meta information.
        /// </summary>
        public string id;

        public virtual void Append(StringBuilder builder)
        {
            if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.OpenRouteService();

            builder.Append("api_key=").Append(key);
            if (!string.IsNullOrEmpty(id)) builder.Append("&id=").Append(id);
        }
    }

    /// <summary>
    /// Parameters of the directions request
    /// </summary>
    [Obsolete]
    public class DirectionParams: Params
    {
        private OnlineMapsVector2d[] coordinates;

        /// <summary>
        /// The route profile.
        /// </summary>
        public Profile profile = Profile.drivingCar;

        /// <summary>
        /// The route preference.
        /// </summary>
        public Preference? preference;

        /// <summary>
        /// The distance unit.
        /// </summary>
        public Units? units;

        /// <summary>
        /// Language for the route instructions.
        /// </summary>
        public string language;

        /// <summary>
        /// Specifies whether to return geometry.
        /// </summary>
        public bool? geometry;

        /// <summary>
        /// Sets the format of the returned geometry.
        /// </summary>
        public GeometryFormat? geometry_format;

        /// <summary>
        /// Specifies whether to simplify the geometry. true will automatically be set to false if extra_info parameter is specified.
        /// </summary>
        public bool? geometry_simplify;

        /// <summary>
        /// Specifies whether to return instructions.
        /// </summary>
        public bool? instructions;

        /// <summary>
        /// Format of instructions. Select html for more verbose instructions.
        /// </summary>
        public InstructionsFormat? instructions_format;

        /// <summary>
        /// Specifies whether to return elevation values for points.
        /// </summary>
        public bool? elevation;

        /// <summary>
        /// List of additional information.
        /// </summary>
        public string[] extra_info;

        /// <summary>
        /// For advanced options formatted as json object.
        /// </summary>
        public string options;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">Open Route Service API key</param>
        /// <param name="coordinates">List of coordinates visited in order.</param>
        public DirectionParams(string key, OnlineMapsVector2d[] coordinates)
        {
            this.key = key;

            if (coordinates.Length < 2) throw new Exception("You must specify at least two coordinates.");
            this.coordinates = coordinates;
        }

        public override void Append(StringBuilder builder)
        {
            base.Append(builder);

            builder.Append("&coordinates=");
            for (int i = 0; i < coordinates.Length; i++)
            {
                if (i > 0) builder.Append("|");
                OnlineMapsVector2d c = coordinates[i];
                builder.Append(c.x.ToString(OnlineMapsUtils.numberFormat)).Append(",").Append(c.y.ToString(OnlineMapsUtils.numberFormat));
            }

            builder.Append("&profile=").Append(OnlineMapsReflectionHelper.GetEnumDescription(profile));
            if (preference.HasValue) builder.Append("&preference=").Append(preference);
            if (units.HasValue) builder.Append("&units=").Append(units);
            if (!string.IsNullOrEmpty(language)) builder.Append("&language=").Append(language);
            if (geometry.HasValue) builder.Append("&geometry=").Append(geometry.Value);
            if (geometry_format.HasValue) builder.Append("&geometry_format=").Append(geometry_format.Value);
            if (geometry_simplify.HasValue) builder.Append("&geometry_simplify=").Append(geometry_simplify.Value);
            if (instructions.HasValue) builder.Append("&instructions=").Append(instructions.Value);
            if (instructions_format.HasValue) builder.Append("&instructions_format=").Append(instructions_format.Value);
            if (elevation.HasValue) builder.Append("&elevation=").Append(elevation.Value);

            if (extra_info != null)
            {
                builder.Append("&extra_info=");
                for (int i = 0; i < extra_info.Length; i++)
                {
                    if (i > 0) builder.Append("|");
                    builder.Append(extra_info[i]);
                }
            }

            if (!string.IsNullOrEmpty(options)) builder.Append("&options=").Append(options);
        }

        public enum Profile
        {
            [Description("driving-car")]
            drivingCar,
            [Description("driving-hgv")]
            drivingHgv,
            [Description("driving-regular")]
            cyclingRegular,
            [Description("cycling-road")]
            cyclingRoad,
            [Description("cycling-save")]
            cyclingSave,
            [Description("cycling-mountain")]
            cyclingMountain,
            [Description("cycling-tour")]
            cyclingTour,
            [Description("cycling-electric")]
            cyclingElectric,
            [Description("foot-walking")]
            footWalking,
            [Description("foot-hiking")]
            footHiking,
            [Description("wheelchair")]
            wheelchair
        }

        public enum Preference
        {
            fastest,
            shortest,
            recommended
        }

        public enum Units
        {
            m,
            km,
            mi
        }

        public enum GeometryFormat
        {
            encodedpolyline,
            geojson,
            polyline
        }

        public enum InstructionsFormat
        {
            text,
            html
        }
    }

    /// <summary>
    /// Parameters of the geocoding request
    /// </summary>
    [Obsolete]
    public class GeocodingParams : Params
    {
        /// <summary>
        /// Name of location, street address or postal code.
        /// </summary>
        private string query;

        /// <summary>
        /// Coordinate to be inquired.
        /// </summary>
        private OnlineMapsVector2d? location;

        /// <summary>
        /// Sets the language of the response.
        /// </summary>
        public string lang;

        /// <summary>
        /// Specifies the type of spatial search restriction. rect for a rectangle and circle
        /// </summary>
        public BoundaryType? boundary_type;

        /// <summary>
        /// For boundary_type=rect only! Sets the restriction rectangle.
        /// </summary>
        public OnlineMapsGeoRect rect;

        /// <summary>
        /// For boundary_type=circle only! Sets the restriction circle with a Centerpoint and a Radius in meters.
        /// </summary>
        public Circle circle;

        /// <summary>
        /// Specifies the maximum number of responses. Not needed for reverse.
        /// </summary>
        public int limit = 20;

        /// <summary>
        /// Gets the coordinates of the specified address.
        /// </summary>
        /// <param name="key">Open Route Service API key</param>
        /// <param name="query">Name of location, street address or postal code.</param>
        public GeocodingParams(string key, string query)
        {
            this.key = key;
            this.query = query;
        }

        /// <summary>
        /// Converts coordinates to a location address.
        /// </summary>
        /// <param name="key">Open Route Service API key</param>
        /// <param name="location">Coordinate to be inquired.</param>
        public GeocodingParams(string key, OnlineMapsVector2d location)
        {
            this.key = key;
            this.location = location;
        }

        /// <summary>
        /// Converts coordinates to a location address.
        /// </summary>
        /// <param name="key">Open Route Service API key</param>
        /// <param name="lng">Longitude to be inquired.</param>
        /// <param name="lat">Latitude to be inquired.</param>
        public GeocodingParams(string key, double lng, double lat)
        {
            this.key = key;
            location = new OnlineMapsVector2d(lng, lat);
        }

        public override void Append(StringBuilder builder)
        {
            base.Append(builder);

            if (!string.IsNullOrEmpty(query)) builder.Append("&query=").Append(query);
            if (location.HasValue) builder.Append("&location=")
                .Append(location.Value.x.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(location.Value.y.ToString(OnlineMapsUtils.numberFormat));
            if (!string.IsNullOrEmpty(lang)) builder.Append("&lang=").Append(lang);
            if (boundary_type.HasValue) builder.Append("&boundary_type=").Append(boundary_type.Value);
            if (rect != null)
            {
                builder.Append("&rect=")
                    .Append(rect.left.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(rect.top.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(rect.right.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(rect.bottom.ToString(OnlineMapsUtils.numberFormat));
            }

            if (circle != null)
            {
                builder.Append("&circle=")
                    .Append(circle.lng.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(circle.lat.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(circle.radius.ToString(OnlineMapsUtils.numberFormat));
            }
            builder.Append("&limit=").Append(limit);

            Debug.Log(builder.ToString());
        }

        public enum BoundaryType
        {
            rect,
            circle
        }

        /// <summary>
        /// For boundary_type=circle only! Sets the restriction circle with a Centerpoint and a Radius in meters.
        /// </summary>
        public class Circle
        {
            /// <summary>
            /// Coordinates of the centerpoint
            /// </summary>
            public OnlineMapsVector2d location;

            /// <summary>
            /// Radius in meters
            /// </summary>
            public float radius;

            /// <summary>
            /// Longitude of the centerpoint
            /// </summary>
            public double lng
            {
                get { return location.x; }
            }

            /// <summary>
            /// Latitude of the centerpoint
            /// </summary>
            public double lat
            {
                get { return location.y; }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="location">Coordinates of the centerpoint</param>
            /// <param name="radius">Radius in meters</param>
            public Circle(OnlineMapsVector2d location, float radius)
            {
                this.location = location;
                this.radius = radius;
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="lng">Longitude of the centerpoint</param>
            /// <param name="lat">Latitude of the centerpoint</param>
            /// <param name="radius">Radius in meters</param>
            public Circle(double lng, double lat, float radius)
            {
                location = new OnlineMapsVector2d(lng, lat);
                this.radius = radius;
            }
        }
    }

    /// <summary>
    /// The preference of the routing.
    /// </summary>
    public enum OnlineMapsOpenRouteServicePref
    {
        Fastest,
        Shortest,
        Pedestrian,
        Bicycle
    }

    public class DescriptionAttribute : Attribute
    {
        private string name;

        public string Description
        {
            get { return name; }
        }

        public DescriptionAttribute(string name)
        {
            this.name = name;
        }
    }
}