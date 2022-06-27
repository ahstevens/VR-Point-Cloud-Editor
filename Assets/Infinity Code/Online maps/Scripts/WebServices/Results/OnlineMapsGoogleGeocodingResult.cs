/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of Google Maps Geocode query.
/// </summary>
public class OnlineMapsGoogleGeocodingResult
{
    /// <summary>
    /// Array containing the separate address components.
    /// </summary>
    public AddressComponent[] address_components;

    /// <summary>
    /// Array indicates the type of the returned result. <br/>
    /// This array contains a set of zero or more tags identifying the type of feature returned in the result. <br/>
    /// For example, a geocode of "Chicago" returns "locality" which indicates that "Chicago" is a city, and also returns "political" which indicates it is a political entity.
    /// </summary>
    public string[] types;

    /// <summary>
    /// String containing the human-readable address of this location. <br/>
    /// Often this address is equivalent to the "postal address," which sometimes differs from country to country. <br/>
    /// (Note that some countries, such as the United Kingdom, do not allow distribution of true postal addresses due to licensing restrictions.) <br/>
    /// This address is generally composed of one or more address components. <br/>
    /// For example, the address "111 8th Avenue, New York, NY" contains separate address components for "111" (the street number, "8th Avenue" (the route), "New York" (the city) and "NY" (the US state).
    /// </summary>
    public string formatted_address;

    /// <summary>
    /// Array denoting all the localities contained in a postal code. <br/>
    /// This is only present when the result is a postal code that contains multiple localities. 
    /// </summary>
    public string[] postcode_localities;

    /// <summary>
    /// Geocoded latitude,longitude value.
    /// </summary>
    public Vector2 geometry_location;

    /// <summary>
    /// Additional data about the specified location.
    /// </summary>
    public string geometry_location_type;

    /// <summary>
    /// Recommended viewport for displaying the returned result, specified as latitude,longitude values defining the northeast corner of the viewport bounding box. <br/>
    /// Generally the viewport is used to frame a result when displaying it to a user.
    /// </summary>
    public Vector2 geometry_viewport_northeast;

    /// <summary>
    /// Recommended viewport for displaying the returned result, specified as latitude,longitude values defining the southwest corner of the viewport bounding box. <br/>
    /// Generally the viewport is used to frame a result when displaying it to a user.
    /// </summary>
    public Vector2 geometry_viewport_southwest;

    /// <summary>
    /// Unique identifier that can be used with other Google APIs.
    /// </summary>
    public string place_id;

    /// <summary>
    /// (optionally returned)<br/>
    /// Stores latitude,longitude values defining the northeast corner the bounding box which can fully contain the returned result. <br/>
    /// Note that these bounds may not match the recommended viewport.
    /// </summary>
    public Vector2 geometry_bounds_northeast;

    /// <summary>
    /// (optionally returned)<br/>
    /// Stores latitude,longitude values defining the southwest corner the bounding box which can fully contain the returned result. <br/>
    /// Note that these bounds may not match the recommended viewport.
    /// </summary>
    public Vector2 geometry_bounds_southwest;

    /// <summary>
    /// Indicates that the geocoder did not return an exact match for the original request, though it was able to match part of the requested address.<br/>
    /// You may wish to examine the original request for misspellings and/or an incomplete address.
    /// </summary>
    public bool partial_match;

    public OnlineMapsGoogleGeocodingResult()
    {
        
    }

    /// <summary>
    /// Constructor of OnlineMapsGoogleGeocodingResult.
    /// </summary>
    /// <param name="node">Location node from response</param>
    public OnlineMapsGoogleGeocodingResult(OnlineMapsXML node)
    {
        List<AddressComponent> address_components = new List<AddressComponent>();
        List<string> types = new List<string>();
        List<string> postcode_localities = new List<string>();

        foreach (OnlineMapsXML n in node)
        {
            if (n.name == "type") types.Add(n.Value());
            else if (n.name == "place_id") place_id = n.Value();
            else if (n.name == "formatted_address") formatted_address = n.Value();
            else if (n.name == "address_component") address_components.Add(new AddressComponent(n));
            else if (n.name == "geometry")
            {
                foreach (OnlineMapsXML gn in n)
                {
                    if (gn.name == "location") geometry_location = OnlineMapsXML.GetVector2FromNode(gn);
                    else if (gn.name == "location_type") geometry_location_type = gn.Value();
                    else if (gn.name == "viewport")
                    {
                        geometry_viewport_northeast = OnlineMapsXML.GetVector2FromNode(gn["northeast"]);
                        geometry_viewport_southwest = OnlineMapsXML.GetVector2FromNode(gn["southwest"]);
                    }
                    else if (gn.name == "bounds")
                    {
                        geometry_bounds_northeast = OnlineMapsXML.GetVector2FromNode(gn["northeast"]);
                        geometry_bounds_southwest = OnlineMapsXML.GetVector2FromNode(gn["southwest"]);
                    }
                    else Debug.Log(n.name);
                }
            }
            else if (n.name == "partial_match") partial_match = n.Value() == "true";
            else Debug.Log(n.name);
        }

        this.address_components = address_components.ToArray();
        this.types = types.ToArray();
        this.postcode_localities = postcode_localities.ToArray();
    }

    /// <summary>
    /// Address Component of Google Geocoder response.
    /// </summary>
    public class AddressComponent
    {
        /// <summary>
        /// Array indicating the type of the address component.
        /// </summary>
        public string[] types;

        /// <summary>
        /// Full text description or name of the address component as returned by the Geocoder.
        /// </summary>
        public string long_name;

        /// <summary>
        /// Abbreviated textual name for the address component, if available. <br/>
        /// For example, an address component for the state of Alaska may have a long_name of "Alaska" and a short_name of "AK" using the 2-letter postal abbreviation.
        /// </summary>
        public string short_name;

        public AddressComponent()
        {
            
        }

        public AddressComponent(OnlineMapsXML node)
        {
            List<string> types = new List<string>();

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "long_name") long_name = n.Value();
                else if (n.name == "short_name") short_name = n.Value();
                else if (n.name == "type") types.Add(n.Value());
                else Debug.Log(n.name);
            }

            this.types = types.ToArray();
        }

        public override string ToString()
        {
            return "OnlineMapsGoogleGeocodingResult.AddressComponent. Types: {" + string.Join(",", types) + "}, Long name: {" + long_name + "}, Short name: {" + short_name + "}";
        }
    }
}