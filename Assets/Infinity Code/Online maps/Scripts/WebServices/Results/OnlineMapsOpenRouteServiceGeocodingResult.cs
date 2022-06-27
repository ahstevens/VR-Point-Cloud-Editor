/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// The resulting object for Open Route Service Geocoding
/// </summary>
public class OnlineMapsOpenRouteServiceGeocodingResult
{
    /// <summary>
    /// Type of features
    /// </summary>
    public string type;

    /// <summary>
    /// Array of the features
    /// </summary>
    public Feature[] features;

    /// <summary>
    /// Coordinates of the bounding box
    /// </summary>
    public double[] bbox;

    /// <summary>
    /// Feature object
    /// </summary>
    public class Feature
    {
        /// <summary>
        /// Type of the feature
        /// </summary>
        public string type;

        /// <summary>
        /// Geometry
        /// </summary>
        public Geometry geometry;

        /// <summary>
        /// Contains the OSM tag information of the point and the confidence. For reverse request with distance
        /// </summary>
        public Properties properties;
    }

    /// <summary>
    /// Geometry
    /// </summary>
    public class Geometry
    {
        /// <summary>
        /// Type of geometry
        /// </summary>
        public string type;

        /// <summary>
        /// Contains the longitude and latitude
        /// </summary>
        public double[] coordinates;
    }


    /// <summary>
    /// Contains the OSM tag information of the point and the confidence. For reverse request with distance
    /// </summary>
    public class Properties
    {
        /// <summary>
        /// Country
        /// </summary>
        public string country;

        /// <summary>
        /// County
        /// </summary>
        public string county;

        /// <summary>
        /// State
        /// </summary>
        public string state;

        /// <summary>
        /// City
        /// </summary>
        public string city;

        /// <summary>
        /// Name
        /// </summary>
        public string name;

        /// <summary>
        /// Postal code
        /// </summary>
        public string postal_code;

        /// <summary>
        /// Street
        /// </summary>
        public string street;

        /// <summary>
        /// House number
        /// </summary>
        public string house_number;

        /// <summary>
        /// Distance between the input location and the result point.
        /// </summary>
        public string distance;

        /// <summary>
        /// Value range: 0-1 For reverse geocoding: Based on the distance. <br/>
        /// The closer a result is to the queried point, the higher the confidence. <br/>
        /// For normal geocoding: Based on the comparison of the query and the result. <br/> 
        /// The closer a result is to the query, the higher the confidence.
        /// </summary>
        public double confidence;
    }
}