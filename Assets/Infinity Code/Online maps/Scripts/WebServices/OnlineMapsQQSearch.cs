/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Text;

/// <summary>
/// Class to work with QQ Search.<br/>
/// http://lbs.qq.com/webservice_v1/guide-search.html
/// </summary>
public class OnlineMapsQQSearch:OnlineMapsTextWebService
{
    private OnlineMapsQQSearch(string key, string keyword, Params p)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.QQ();

        StringBuilder builder = new StringBuilder("http://apis.map.qq.com/ws/place/v1/search?key=").Append(key).Append("&keyword=").Append(OnlineMapsWWW.EscapeURL(keyword));
        p.AppendParams(builder);
        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Make a new request to QQ Search
    /// </summary>
    /// <param name="key">QQ API key</param>
    /// <param name="keyword">POI Search keyword for full-text search fields</param>
    /// <param name="p">Parameters object</param>
    /// <returns>Instance of request</returns>
    public static OnlineMapsQQSearch Find(string key, string keyword, Params p)
    {
        return new OnlineMapsQQSearch(key, keyword, p);
    }

    /// <summary>
    /// Converts the response string to response object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Response Object</returns>
    public static OnlineMapsQQSearchResult GetResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsQQSearchResult>(response);
    }

    /// <summary>
    /// Parameters of QQ Search request
    /// </summary>
    public class Params
    {
        /// <summary>
        /// Filter criteria.<br/>
        /// http://lbs.qq.com/webservice_v1/guide-search.html#filter_detail
        /// </summary>
        public string filter;

        /// <summary>
        /// Sort by.<br/>
        /// http://lbs.qq.com/webservice_v1/guide-search.html#orderby_detail
        /// </summary>
        public string orderby;

        /// <summary>
        /// The maximum number of entries per page is 20.
        /// </summary>
        public int? page_size;

        /// <summary>
        /// Page x, default page 1
        /// </summary>
        public int? page_index;

        private SearchType type;
        private string region;
        private bool auto_extend;
        private double? lng1;
        private double? lat1;
        private double? lng2;
        private double? lat2;
        private int radius;

        /// <summary>
        /// Search for a specific city.
        /// </summary>
        /// <param name="region">Retrieves the region name, city name, such as Beijing.</param>
        /// <param name="auto_extend">
        /// TRUE: the current city search results, then automatically expand the scope; <br/>
        /// FALSE: Search only in the current city.
        /// </param>
        /// <param name="lng">Longitude of the center location.</param>
        /// <param name="lat">Latitude of the center location.</param>
        public Params(string region, bool auto_extend = false, double? lng = null, double? lat = null)
        {
            type = SearchType.region;
            this.region = region;
            this.auto_extend = auto_extend;
            lng1 = lng;
            lat1 = lat;
        }

        /// <summary>
        /// A Nearby Search lets you search for places within a specified area.
        /// </summary>
        /// <param name="lng">Longitude of the center location.</param>
        /// <param name="lat">Latitude of the center location.</param>
        /// <param name="radius">Radius (meters).</param>
        public Params(double lng, double lat, int radius)
        {
            type = SearchType.nearby;
            lng1 = lng;
            lat1 = lat;
            this.radius = radius;
        }

        /// <summary>
        /// Rectangle Search.
        /// </summary>
        /// <param name="lng1">Left longitude</param>
        /// <param name="lat1">Bottom latitude</param>
        /// <param name="lng2">Right longitude</param>
        /// <param name="lat2">Top latitude</param>
        public Params(double lng1, double lat1, double lng2, double lat2)
        {
            type = SearchType.rectangle;
            this.lng1 = lng1;
            this.lat1 = lat1;
            this.lng2 = lng2;
            this.lat2 = lat2;
        }

        public void AppendParams(StringBuilder builder)
        {
            if (type == SearchType.region)
            {
                builder.Append("&boundary=region(").Append(region);
                builder.Append(",").Append(auto_extend? "1": "0");
                if (lng1.HasValue && lat1.HasValue)
                {
                    builder.Append(",").Append(lat1.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                        .Append(lng1.Value.ToString(OnlineMapsUtils.numberFormat));
                }
            }
            else if (type == SearchType.nearby)
            {
                builder.Append("nearby(").Append(lat1.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(lng1.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",").Append(radius);
            }
            else if (type == SearchType.rectangle)
            {
                builder.Append("rectangle(")
                    .Append(lat1.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(lng1.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(lat2.Value.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(lng2.Value.ToString(OnlineMapsUtils.numberFormat));
            }
            builder.Append(")");
            if (!string.IsNullOrEmpty(filter)) builder.Append("&filter=").Append(filter);
            if (!string.IsNullOrEmpty(orderby)) builder.Append("&orderby=").Append(orderby);
            if (page_size.HasValue) builder.Append("&page_size=").Append(page_size.Value);
            if (page_index.HasValue) builder.Append("&page_index=").Append(page_index.Value);
        }
    }

    public enum SearchType
    {
        region,
        nearby,
        rectangle
    }
}