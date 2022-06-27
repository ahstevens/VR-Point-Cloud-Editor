/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;

/// <summary>
/// Returns a JSON formatted list of objects corresponding to the search input from Open Route Service Geocode.<br/>
/// https://openrouteservice.org/dev/#/api-docs/geocode
/// </summary>
public class OnlineMapsOpenRouteServiceGeocode: OnlineMapsTextWebService
{
    private const string endpoint = "https://api.openrouteservice.org/geocode/";

    private OnlineMapsOpenRouteServiceGeocode(string text, Params p = null)
    {
        if (string.IsNullOrEmpty(text)) throw new Exception("Text cannot be null or empty.");

        StringBuilder builder = new StringBuilder(endpoint);
        builder.Append("search?");

        string key = OnlineMapsKeyManager.OpenRouteService();
        builder.Append("api_key=");
        if (p != null && !string.IsNullOrEmpty(p.apiKey)) builder.Append(p.apiKey);
        else builder.Append(key);

        builder.Append("&text=").Append(OnlineMapsWWW.EscapeURL(text));

        if (p != null) p.Append(builder);

        www = new OnlineMapsWWW(builder.ToString());
        www.OnComplete += OnRequestComplete;
    }

    private OnlineMapsOpenRouteServiceGeocode(OnlineMapsVector2d point, Params p = null)
    {
        StringBuilder builder = new StringBuilder(endpoint);
        builder.Append("reverse?");

        string key = OnlineMapsKeyManager.OpenRouteService();
        builder.Append("api_key=");
        if (p != null && !string.IsNullOrEmpty(p.apiKey)) builder.Append(p.apiKey);
        else builder.Append(key);

        builder.Append("&point.lon=").Append(point.x.ToString(OnlineMapsUtils.numberFormat));
        builder.Append("&point.lat=").Append(point.y.ToString(OnlineMapsUtils.numberFormat));

        if (p != null) p.Append(builder);

        www = new OnlineMapsWWW(builder.ToString());
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Converts the response string from Open Route Service Geocode to result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    public static OnlineMapsOpenRouteServiceGeocodingResult GetResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsOpenRouteServiceGeocodingResult>(response);
    }

    public static OnlineMapsOpenRouteServiceGeocode Reverse(OnlineMapsVector2d point, ReverseParams p = null)
    {
        OnlineMapsOpenRouteServiceGeocode r = new OnlineMapsOpenRouteServiceGeocode(point, p);
        return r;
    }

    /// <summary>
    /// Resolve input coordinates to addresses and vice versa
    /// </summary>
    /// <param name="text">Name of location, street address or postal code</param>
    /// <param name="p">Parameters of the request</param>
    /// <returns>Instance of the query</returns>
    public static OnlineMapsOpenRouteServiceGeocode Search(string text, GeocodeParams p = null)
    {
        OnlineMapsOpenRouteServiceGeocode r = new OnlineMapsOpenRouteServiceGeocode(text, p);
        return r;
    }

    public abstract class Params
    {
        /// <summary>
        /// Open Route Service API key. If empty, the value from the Key Manager will be used.
        /// </summary>
        public string apiKey;

        /// <summary>
        /// Restrict your search to specific sources. Searches all sources by default. You can either use the normal or short name.
        /// </summary>
        public string sources;

        /// <summary>
        /// Restrict search to layers (place type). By default all layers are searched.
        /// </summary>
        public string layers;

        /// <summary>
        /// ISO-3166 country code to narrow results.
        /// </summary>
        public string boundaryCountry;

        /// <summary>
        /// Restrict results to administrative boundary using a Pelias global id gid. gids for records can be found using either the Who’s on First Spelunker, a tool for searching Who’s on First data, or from the responses of other Pelias queries.
        /// </summary>
        public string boundaryGid;

        /// <summary>
        /// Number of returned results. By default, returns up to 10 results.
        /// </summary>
        public int? size;

        public virtual void Append(StringBuilder builder)
        {
            if (!string.IsNullOrEmpty(sources)) builder.Append("&sources=").Append(sources);
            if (!string.IsNullOrEmpty(layers)) builder.Append("&layers=").Append(layers);
            if (!string.IsNullOrEmpty(boundaryCountry)) builder.Append("&boundary.country=").Append(boundaryCountry);
            if (size.HasValue) builder.Append("&size=").Append(size.Value);
            if (!string.IsNullOrEmpty(boundaryGid)) builder.Append("&boundary.gid=").Append(boundaryGid);
        }
    }

    public class GeocodeParams : Params
    {
        /// <summary>
        /// Location of a focus point. Specify the focus point to order results by linear distance to this point. Works for up to 100 kilometers distance.
        /// </summary>
        public OnlineMapsVector2d? focusPoint;

        /// <summary>
        /// Top-left border of rectangular boundary to narrow results.
        /// </summary>
        public OnlineMapsVector2d? boundaryMin;

        /// <summary>
        /// Bottom-right border of rectangular boundary to narrow results.
        /// </summary>
        public OnlineMapsVector2d? boundaryMax;

        /// <summary>
        /// Center location of circular boundary to narrow results.
        /// </summary>
        public OnlineMapsVector2d? boundaryCircle;

        /// <summary>
        /// Radius of circular boundary to narrow results.
        /// </summary>
        public float boundaryCircleRadius = 50;

        public override void Append(StringBuilder builder)
        {
            base.Append(builder);

            if (focusPoint.HasValue)
            {
                builder.Append("&focus.point.lat=").Append(focusPoint.Value.y.ToString(OnlineMapsUtils.numberFormat))
                    .Append("&focus.point.lon=").Append(focusPoint.Value.x.ToString(OnlineMapsUtils.numberFormat));
            }

            if (boundaryMin.HasValue)
            {
                builder.Append("&boundary.rect.min_lat=").Append(boundaryMin.Value.y.ToString(OnlineMapsUtils.numberFormat))
                    .Append("&boundary.rect.min_lon=").Append(boundaryMin.Value.x.ToString(OnlineMapsUtils.numberFormat));
            }

            if (boundaryMax.HasValue)
            {
                builder.Append("&boundary.rect.max_lat=").Append(boundaryMax.Value.y.ToString(OnlineMapsUtils.numberFormat))
                    .Append("&boundary.rect.max_lon=").Append(boundaryMax.Value.x.ToString(OnlineMapsUtils.numberFormat));
            }

            if (boundaryCircle.HasValue)
            {
                builder.Append("&boundary.circle.lat=").Append(boundaryCircle.Value.y.ToString(OnlineMapsUtils.numberFormat))
                    .Append("&boundary.circle.lon=").Append(boundaryCircle.Value.x.ToString(OnlineMapsUtils.numberFormat))
                    .Append("&boundary.circle.radius=").Append(boundaryCircleRadius.ToString(OnlineMapsUtils.numberFormat));
            }
        }
    }

    public class ReverseParams : Params
    {
        /// <summary>
        /// Restrict search to circular region around point. Value in kilometers.
        /// </summary>
        public float? boundaryCircleRadius;

        public override void Append(StringBuilder builder)
        {
            base.Append(builder);

            if (boundaryCircleRadius.HasValue) builder.Append("&boundary.circle.radius=").Append(boundaryCircleRadius.Value.ToString(OnlineMapsUtils.numberFormat));
        }
    }
}