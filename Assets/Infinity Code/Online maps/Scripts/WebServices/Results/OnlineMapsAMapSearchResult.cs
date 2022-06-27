/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// AMap search response object.<br/>
/// Note: Descriptions of the fields are translated from Chinese using Google Translate and can be translated incorrectly. <br/>
/// If you confused in the description of field, please read the official AMap documentation.<br/>
/// http://lbs.amap.com/api/webservice/guide/api/search/#introduce
/// </summary>
public class OnlineMapsAMapSearchResult
{
    /// <summary>
    /// The resulting status value. 0: Request failed, 1: The request was successful.
    /// </summary>
    public int status;

    /// <summary>
    /// Returns the status description.<br/>
    /// When status is 0, info returns the cause of the error, otherwise it returns "OK".<br/>
    /// http://lbs.amap.com/api/webservice/info/
    /// </summary>
    public string info;

    public string infocode;  // Undocumented

    /// <summary>
    /// Number of search items (maximum 1000).
    /// </summary>
    public int count;

    /// <summary>
    /// Search POI information array.
    /// </summary>
    public POI[] pois;

    public Suggestion suggestion; // Undocumented

    /// <summary>
    /// POI information.
    /// </summary>
    public class POI
    {
        /// <summary>
        /// The unique ID.
        /// </summary>
        public string id; 

        /// <summary>
        /// Name.
        /// </summary>
        public string name; 

        public string tag; // Undocumented

        /// <summary>
        /// Points of Interest.
        /// </summary>
        public string type;

        /// <summary>
        /// Points of interest type encoding.
        /// </summary>
        public string typecode;

        /// <summary>
        /// Career type.
        /// </summary>
        public string biz_type;

        /// <summary>
        /// Address.
        /// </summary>
        public string address;

        /// <summary>
        /// Latitude and longitude
        /// </summary>
        public string location;

        /// <summary>
        /// Phone
        /// </summary>
        public string tel; 

        public string postcode; // Undocumented
        public string website; // Undocumented

        /// <summary>
        /// The province of POI the code.<br/>
        /// The following data is a list of poi details, extensions = all to return; extensions = base does not return.
        /// </summary>
        public string pcode;

        /// <summary>
        /// The name of POI province.
        /// </summary>
        public string pname;

        /// <summary>
        /// City code.
        /// </summary>
        public string citycode;

        /// <summary>
        /// City name.
        /// </summary>
        public string cityname;

        /// <summary>
        /// Area encoding.
        /// </summary>
        public string adcode;

        /// <summary>
        /// Area name.
        /// </summary>
        public string adname; 

        public string importance; // Undocumented
        public string shopid; // Undocumented
        public string poiweight; // Undocumented

        /// <summary>
        /// Geography.
        /// </summary>
        public string gridcode;

        /// <summary>
        /// The distance from the center point, in meters.
        /// </summary>
        public string distance;

        /// <summary>
        /// The map number.
        /// </summary>
        public string navi_poiid;

        /// <summary>
        /// Entrance latitude and longitude.
        /// </summary>
        public string entr_location;

        /// <summary>
        /// The business district.
        /// </summary>
        public string business_area;

        /// <summary>
        /// Exit latitude and longitude.
        /// </summary>
        public string exit_location; 

        public string match; // Undocumented
        public string recommend; // Undocumented
        public string timestamp; // Undocumented

        /// <summary>
        /// Type of parking. Show parking types, including: underground, ground, roadside.
        /// </summary>
        public string parking_type;

        /// <summary>
        /// Alias.
        /// </summary>
        public string alias;

        /// <summary>
        /// Are there indoor map signs.
        /// </summary>
        public string indoor_map;

        /// <summary>
        /// Indoor map of the relevant data.
        /// </summary>
        public IndoorData indoor_data; 

        public string groupbuy_num; // Undocumented
        public string discount_num; // Undocumented
        public BizExt biz_ext; // Undocumented
        public string @event; // Undocumented
        public Children[] children; // Undocumented
        public Photo[] photos; // Undocumented

        /// <summary>
        /// Gets the location coordinates
        /// </summary>
        /// <param name="lng">Longitude</param>
        /// <param name="lat">Latitude</param>
        public void GetLocation(out double lng, out double lat)
        {
            lng = 0;
            lat = 0;
            if (string.IsNullOrEmpty(location)) return;

            string[] parts = location.Split(',');
            lat = double.Parse(parts[1], OnlineMapsUtils.numberFormat);
            lng = double.Parse(parts[0], OnlineMapsUtils.numberFormat);
        }
    }

    public class Suggestion
    {
        public string keywords;
        public string cities;
    }

    /// <summary>
    /// Indoor map of the relevant data
    /// </summary>
    public class IndoorData
    {
        /// <summary>
        /// The parent POI of the current POI
        /// </summary>
        public string cpid;

        /// <summary>
        /// Floor directory
        /// </summary>
        public string floor;

        /// <summary>
        /// On the floor
        /// </summary>
        public string truefloor;

        public string cmsid; // Undocumented
    }

    // Undocumented
    public class BizExt
    {
        public string rating;
        public string cost;
    }

    // Undocumented
    public class Children
    {
        public string id;
        public string name;
        public string sname;
        public string location;
        public string address;
        public string distance;
        public string subtype;
    }

    // Undocumented
    public class Photo
    {
        public string title;
        public string url;
    }
}