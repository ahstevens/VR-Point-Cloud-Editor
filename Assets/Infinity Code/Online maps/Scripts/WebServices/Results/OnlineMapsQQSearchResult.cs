/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// QQ Search result object.
/// </summary>
public class OnlineMapsQQSearchResult
{
    /// <summary>
    /// Status code, 0 is normal,<br/>
    /// 310 request parameter information is wrong,<br/>
    /// 311key format error,<br/>
    /// 306 request has the support information please check the string,<br/>
    /// 110 The request source is not authorized
    /// </summary>
    public int status;

    /// <summary>
    /// State Description
    /// </summary>
    public string message;

    /// <summary>
    /// The total number of results for this search
    /// </summary>
    public int count;

    /// <summary>
    /// A unique identifier for the request.
    /// </summary>
    public string request_id;

    /// <summary>
    /// The search results POI array, each for a POI object
    /// </summary>
    public Data[] data;

    /// <summary>
    /// POI Object
    /// </summary>
    public class Data
    {
        /// <summary>
        /// POI Unique identification
        /// </summary>
        public string id;

        /// <summary>
        /// POI name
        /// </summary>
        public string title;

        /// <summary>
        /// Address
        /// </summary>
        public string address;

        /// <summary>
        /// Phone
        /// </summary>
        public string tel;

        /// <summary>
        /// POI classification
        /// </summary>
        public string category;

        /// <summary>
        /// POI type, value Description: 0: Ordinary POI / 1: Bus station / 2: Metro station / 3: Bus line / 4: Administrative division
        /// </summary>
        public int type;

        /// <summary>
        /// Coordinate
        /// </summary>
        public Location location;

        /// <summary>
        /// Administrative division information, currently only provides adcode
        /// </summary>
        public AdInfo ad_info;

        /// <summary>
        /// Contour, coordinate array, the larger the POI will have, such as residential quarters
        /// </summary>
        public string[] boundary;

        /// <summary>
        /// The POI's Street View is the best viewing scenario and perspective information
        /// </summary>
        public Pano pano;
    }

    /// <summary>
    /// Coordinate Object
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Latitude
        /// </summary>
        public double lat;

        /// <summary>
        /// Longitude
        /// </summary>
        public double lng;
    }

    /// <summary>
    /// Administrative division information, currently only provides adcode
    /// </summary>
    public class AdInfo
    {
        /// <summary>
        /// Administrative division code
        /// </summary>
        public string adcode;
    }

    /// <summary>
    /// The POI's Street View is the best viewing scenario and perspective information
    /// </summary>
    public class Pano
    {
        /// <summary>
        /// Street scene ID, if pano information, then the id must exist
        /// </summary>
        public string id;

        /// <summary>
        /// Best yaw angle
        /// </summary>
        public int heading;

        /// <summary>
        /// Pitch angle
        /// </summary>
        public int pitch;

        /// <summary>
        /// The zoom level
        /// </summary>
        public int zoom;
    }
}