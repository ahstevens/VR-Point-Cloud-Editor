/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of Google Maps Places query.
/// </summary>
public class OnlineMapsGooglePlacesResult
{
    /// <summary>
    /// Coordinates of the place.
    /// </summary>
    public Vector2 location;

    /// <summary>
    /// URL of a recommended icon which may be displayed to the user when indicating this result.
    /// </summary>
    public string icon;

    /// <summary>
    /// Unique stable identifier denoting this place. <br/>
    /// This identifier may not be used to retrieve information about this place, but is guaranteed to be valid across sessions. <br/>
    /// It can be used to consolidate data about this place, and to verify the identity of a place across separate searches. <br/>
    /// Note: The id is now deprecated in favor of place_id.
    /// </summary>
    public string id;

    /// <summary>
    /// Human-readable address of this place. <br/>
    /// Often this address is equivalent to the "postal address". <br/>
    /// The formatted_address property is only returned for a Text Search.
    /// </summary>
    public string formatted_address;

    /// <summary>
    /// Human-readable name for the returned result. <br/>
    /// For establishment results, this is usually the business name.
    /// </summary>
    public string name;

    /// <summary>
    /// Unique identifier for a place.
    /// </summary>
    public string place_id;

    /// <summary>
    /// Unique token that you can use to retrieve additional information about this place in a Place Details request. <br/>
    /// Although this token uniquely identifies the place, the converse is not true. <br/>
    /// A place may have many valid reference tokens. <br/>
    /// It's not guaranteed that the same token will be returned for any given place across different searches. <br/>
    /// Note: The reference is now deprecated in favor of place_id.
    /// </summary>
    public string reference;

    /// <summary>
    /// Array of feature types describing the given result. <br/>
    /// XML responses include multiple type elements if more than one type is assigned to the result.
    /// </summary>
    public string[] types;

    /// <summary>
    /// Feature name of a nearby location. <br/>
    /// Often this feature refers to a street or neighborhood within the given results. <br/>
    /// The vicinity property is only returned for a Nearby Search.
    /// </summary>
    public string vicinity;

    /// <summary>
    /// The price level of the place, on a scale of 0 to 4.  <br/>
    /// The exact amount indicated by a specific value will vary from region to region. <br/>
    /// Price levels are interpreted as follows: <br/>
    /// -1 - Unknown <br/>
    /// 0 - Free <br/>
    /// 1 - Inexpensive <br/>
    /// 2 - Moderate <br/>
    /// 3 - Expensive <br/>
    /// 4 - Very Expensive
    /// </summary>
    public int price_level = -1;

    /// <summary>
    /// Place's rating, from 1.0 to 5.0, based on aggregated user reviews.
    /// </summary>
    public float rating;

    /// <summary>
    /// Value indicating if the place is open at the current time.
    /// </summary>
    public bool open_now;

    /// <summary>
    /// Indicates the scope of the place_id. 
    /// </summary>
    public string scope;

    /// <summary>
    /// Undocumented in Google Maps Places API.
    /// </summary>
    public string[] weekday_text;

    /// <summary>
    /// Array of photo objects, each containing a reference to an image. <br/>
    /// A Place Search will return at most one photo object. <br/>
    /// Performing a Place Details request on the place may return up to ten photos.
    /// </summary>
    public Photo[] photos;

    public PlusCode plus_code;

    public OnlineMapsGooglePlacesResult()
    {
        
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Place node from response</param>
    public OnlineMapsGooglePlacesResult(OnlineMapsXML node)
    {
        List<Photo> photos = new List<Photo>();
        List<string> types = new List<string>();
        List<string> weekday_text = new List<string>();

        foreach (OnlineMapsXML n in node)
        {
            if (n.name == "name") name = n.Value();
            else if (n.name == "id") id = n.Value();
            else if (n.name == "vicinity") vicinity = n.Value();
            else if (n.name == "type") types.Add(n.Value());
            else if (n.name == "geometry") location = OnlineMapsXML.GetVector2FromNode(n[0]);
            else if (n.name == "rating") rating = n.Value<float>();
            else if (n.name == "icon") icon = n.Value();
            else if (n.name == "reference") reference = n.Value();
            else if (n.name == "place_id") place_id = n.Value();
            else if (n.name == "scope") scope = n.Value();
            else if (n.name == "price_level") price_level = n.Value<int>();
            else if (n.name == "formatted_address") formatted_address = n.Value();
            else if (n.name == "opening_hours")
            {
                open_now = n.Get<string>("open_now") == "true";
                foreach (OnlineMapsXML wdt in n.FindAll("weekday_text")) weekday_text.Add(wdt.Value());
            }
            else if (n.name == "photo") photos.Add(new Photo(n));
            else if (n.name == "plus_code") plus_code = new PlusCode(n);
            else Debug.Log(n.name);
        }

        this.photos = photos.ToArray();
        this.types = types.ToArray();
        this.weekday_text = weekday_text.ToArray();
    }

    /// <summary>
    /// Photo objects, contains a reference to an image.
    /// </summary>
    public class Photo
    {
        /// <summary>
        /// The maximum width of the image.
        /// </summary>
        public int width;

        /// <summary>
        /// The maximum height of the image.
        /// </summary>
        public int height;

        /// <summary>
        /// String used to identify the photo when you perform a Photo request.
        /// </summary>
        public string photo_reference;

        /// <summary>
        /// Contains any required attributions. This field will always be present, but may be empty.
        /// </summary>
        public string[] html_attributions;

        public Photo ()
        {
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="node">Photo node from response</param>
        public Photo(OnlineMapsXML node)
        {
            try
            {
                width = node.Get<int>("width");
                height = node.Get<int>("height");
                photo_reference = node["photo_reference"].Value();

                List<string> html_attributions = new List<string>();
                foreach (OnlineMapsXML ha in node.FindAll("html_attributions")) html_attributions.Add(ha.Value());
                this.html_attributions = html_attributions.ToArray();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Download photo from Google Places.
        /// </summary>
        /// <param name="key">Google Maps API Key.</param>
        /// <param name="maxWidth">
        /// Specifies the maximum desired width, in pixels, of the image returned by the Place Photos service. <br/>
        /// If the image is smaller than the values specified, the original image will be returned. <br/>
        /// If the image is larger in either dimension, it will be scaled to match the smaller of the two dimensions, restricted to its original aspect ratio. <br/>
        /// maxWidth accept an integer between 1 and 1600.
        /// </param>
        /// <param name="maxHeight">
        /// Specifies the maximum desired height, in pixels, of the image returned by the Place Photos service. <br/>
        /// If the image is smaller than the values specified, the original image will be returned. <br/>
        /// If the image is larger in either dimension, it will be scaled to match the smaller of the two dimensions, restricted to its original aspect ratio. <br/>
        /// maxHeight accept an integer between 1 and 1600.
        /// </param>
        /// <returns>Instance of a request</returns>
        public OnlineMapsGooglePlacePhoto Download(string key, int? maxWidth = null, int? maxHeight = null)
        {
            if (!maxWidth.HasValue) maxWidth = width;
            if (!maxHeight.HasValue) maxHeight = height;
            return OnlineMapsGooglePlacePhoto.Download(key, photo_reference, maxWidth, maxHeight);
        }
    }

    public class PlusCode
    {
        public string global_code;
        public string compound_code;

        public PlusCode()
        {

        }

        public PlusCode(OnlineMapsXML node)
        {
            try
            {
                global_code = node.Get<string>("global_code");
                compound_code = node.Get<string>("compound_code");
            }
            catch
            {
            }
        }
    }
}