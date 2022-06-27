/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

/// <summary>
/// AMap Search provides many kinds of querying POI information, including keyword search, peripheral search, polygon search and ID query.<br/>
/// http://lbs.amap.com/api/webservice/guide/api/search/#introduce
/// </summary>
public class OnlineMapsAMapSearch: OnlineMapsTextWebService
{
    private OnlineMapsAMapSearch(Params p)
    {
        StringBuilder builder = new StringBuilder();
        p.GenerateURL(builder);
        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Make a new request to AMap Search
    /// </summary>
    /// <param name="p">Parameters of request</param>
    /// <returns>Instance of request</returns>
    public static OnlineMapsAMapSearch Find(Params p)
    {
        return new OnlineMapsAMapSearch(p);
    }

    /// <summary>
    /// Converts the response string to response object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Response object</returns>
    public static OnlineMapsAMapSearchResult GetResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsAMapSearchResult>(response);
    }

    /// <summary>
    /// Base class for parameters object
    /// </summary>
    public abstract class Params
    {
        protected string key;

        protected abstract string baseurl
        {
            get;
        }

        internal virtual void GenerateURL(StringBuilder builder)
        {
            if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.AMap();
            builder.Append(baseurl).Append("key=").Append(key).Append("&output=JSON");
        }
    }

    /// <summary>
    /// Keyword search parameters object
    /// </summary>
    public class TextParams : Params
    {
        /// <summary>
        /// Query keywords.<br/>
        /// Multiple keywords are separated by "|"
        /// </summary>
        public string keywords;

        /// <summary>
        /// Query POI type.<br/>
        /// Multiple types are separated by "|".<br/>
        /// http://a.amap.com/lbs/static/zip/AMap_API_Table.zip
        /// </summary>
        public string types;

        /// <summary>
        /// Check the city.<br/>
        /// Optional values: city Chinese, Chinese spelling, citycode, adcode.<br/>
        /// Such as: Beijing / beijing / 010/110000
        /// </summary>
        public string city;

        /// <summary>
        /// Returns only the specified city data.
        /// </summary>
        public bool citylimit = false;

        /// <summary>
        /// Whether the sub-POI data is displayed by hierarchy.
        /// </summary>
        public bool children = false;

        /// <summary>
        /// Each page records data.<br/>
        /// It is strongly recommended not to exceed 25, if more than 25 may cause access error.
        /// </summary>
        public int? offset;

        /// <summary>
        /// The current page count.
        /// </summary>
        public int? page;

        /// <summary>
        /// POI number of the building.<br/>
        /// After building POI is introduced, only in the building within the search.
        /// </summary>
        public string building;

        /// <summary>
        /// Search for floors.<br/>
        /// Returns the keyword search results for the current floor in the building if the building id + floor is passed in.<br/>
        /// If only the floor, the return parameters incomplete advice.<br/>
        /// If the building id + floor, the floor does not have the corresponding search results, will return to the contents of the building.
        /// </summary>
        public int? floor;

        /// <summary>
        /// Returns the result control.<br/>
        /// This item returns the basic address information by default; the value returns all address information, nearby POIs, roads, and road intersections.
        /// </summary>
        public string extensions;

        /// <summary>
        /// Digital signature
        /// </summary>
        public string sig;

        protected override string baseurl
        {
            get { return "http://restapi.amap.com/v3/place/text?"; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">AMap API key</param>
        public TextParams(string key)
        {
            this.key = key;
        }

        internal override void GenerateURL(StringBuilder builder)
        {
            base.GenerateURL(builder);

            bool hasKeywords = false, hasTypes = false;
            if (!string.IsNullOrEmpty(keywords))
            {
                builder.Append("&keywords=").Append(keywords);
                hasKeywords = true;
            }
            if (!string.IsNullOrEmpty(types))
            {
                builder.Append("&types=").Append(types);
                hasTypes = true;
            }

            if (!hasKeywords && !hasTypes)
            {
                throw new Exception("You must specify the keywords or types.");
            }

            if (!string.IsNullOrEmpty(city)) builder.Append("&city=").Append(city);
            if (citylimit) builder.Append("&citylimit=true");
            if (children) builder.Append("&children=true");
            if (offset.HasValue) builder.Append("&offset=").Append(offset.Value);
            if (page.HasValue) builder.Append("&page=").Append(page.Value);
            if (!string.IsNullOrEmpty(building)) builder.Append("&building=").Append(building);
            if (floor.HasValue) builder.Append("&floor=").Append(floor.Value);
            if (!string.IsNullOrEmpty(extensions)) builder.Append("&extensions=").Append(extensions);
            if (!string.IsNullOrEmpty(sig)) builder.Append("&sig=").Append(sig);
        }
    }

    /// <summary>
    /// Peripheral search parameters object
    /// </summary>
    public class AroundParams : Params
    {
        /// <summary>
        /// Query keywords.<br/>
        /// Multiple keywords are separated by "|".
        /// </summary>
        public string keywords;

        /// <summary>
        /// Query the POI type.<br/>
        /// Multiple keywords are separated by "|".<br/>
        /// http://a.amap.com/lbs/static/zip/AMap_API_Table.zip
        /// </summary>
        public string types;

        /// <summary>
        /// Check the city.<br/>
        /// Optional values: city Chinese, Chinese spelling, citycode, adcode<br/>
        /// Such as: Beijing / beijing / 010/110000
        /// </summary>
        public string city;

        /// <summary>
        ///  The radius of the query.<br/>
        /// The value ranges from 0 to 50000, in meters.
        /// </summary>
        public int? raduis;

        /// <summary>
        /// Collation.
        /// </summary>
        public string sortrule;

        /// <summary>
        /// Each page records data.<br/>
        /// The maximum number of records per page is 25. Out of range The maximum value is returned.
        /// </summary>
        public int? offset;

        /// <summary>
        /// The current page count.
        /// </summary>
        public int? page;

        /// <summary>
        /// Returns the result control.<br/>
        /// This item returns the basic address information by default; the value returns all address information, nearby POIs, roads, and road intersections.
        /// </summary>
        public string extensions;

        /// <summary>
        /// Digital signature
        /// </summary>
        public string sig;

        private double longitude;
        private double latitude;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">AMap API Key</param>
        /// <param name="longitude">Center point longitude</param>
        /// <param name="latitude">Center point latitude</param>
        public AroundParams(string key, double longitude, double latitude)
        {
            this.key = key;
            this.longitude = longitude;
            this.latitude = latitude;
        }

        protected override string baseurl
        {
            get { return "http://restapi.amap.com/v3/place/around?"; }
        }

        internal override void GenerateURL(StringBuilder builder)
        {
            base.GenerateURL(builder);

            builder.Append("&location=")
                .Append(latitude.ToString("F6", OnlineMapsUtils.numberFormat)).Append(",")
                .Append(longitude.ToString("F6", OnlineMapsUtils.numberFormat));

            if (!string.IsNullOrEmpty(keywords)) builder.Append("&keywords=").Append(keywords);
            if (!string.IsNullOrEmpty(types)) builder.Append("&types=").Append(types);
            if (raduis.HasValue) builder.Append("&raduis=").Append(raduis.Value);
            if (!string.IsNullOrEmpty(sortrule)) builder.Append("&sortrule=").Append(sortrule);
            if (!string.IsNullOrEmpty(city)) builder.Append("&city=").Append(city);
            if (offset.HasValue) builder.Append("&offset=").Append(offset.Value);
            if (page.HasValue) builder.Append("&page=").Append(page.Value);
            if (!string.IsNullOrEmpty(extensions)) builder.Append("&extensions=").Append(extensions);
            if (!string.IsNullOrEmpty(sig)) builder.Append("&sig=").Append(sig);

            Debug.Log(builder);
        }
    }

    /// <summary>
    /// Polygon search parameters object
    /// </summary>
    public class PolygonParams : Params
    {
        /// <summary>
        /// Query keywords.<br/>
        /// Multiple keywords are separated by "|".
        /// </summary>
        public string keywords;

        /// <summary>
        /// Query POI type.<br/>
        /// Multiple types are separated by "|".
        /// </summary>
        public string types;

        /// <summary>
        /// Each page records data.<br/>
        /// The maximum number of records per page is 25. Out of range Return to the maximum value.
        /// </summary>
        public int? offset;

        /// <summary>
        /// The current page count.
        /// </summary>
        public int? page;

        /// <summary>
        /// Returns the result control.<br/>
        /// This basic default return address information; the value of all the return address information, nearby POI, road and road intersection information.
        /// </summary>
        public string extensions;

        /// <summary>
        /// Digital signature.
        /// </summary>
        public string sig;

        private double[] polygon;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">AMap API key</param>
        /// <param name="polygon">Longitude and latitude coordinate pairs.</param>
        public PolygonParams(string key, double[] polygon)
        {
            this.key = key;
            this.polygon = polygon;
        }

        protected override string baseurl
        {
            get { return "http://restapi.amap.com/v3/place/around?"; }
        }

        internal override void GenerateURL(StringBuilder builder)
        {
            base.GenerateURL(builder);

            builder.Append("&location=");
            for (int i = 0; i < polygon.Length; i++)
            {
                if (i != 0) builder.Append(",");
                builder.Append(polygon[i].ToString("F6", OnlineMapsUtils.numberFormat));
            }

            if (!string.IsNullOrEmpty(keywords)) builder.Append("&keywords=").Append(keywords);
            if (!string.IsNullOrEmpty(types)) builder.Append("&types=").Append(types);
            if (offset.HasValue) builder.Append("&offset=").Append(offset.Value);
            if (page.HasValue) builder.Append("&page=").Append(page.Value);
            if (!string.IsNullOrEmpty(extensions)) builder.Append("&extensions=").Append(extensions);
            if (!string.IsNullOrEmpty(sig)) builder.Append("&sig=").Append(sig);
        }
    }
}