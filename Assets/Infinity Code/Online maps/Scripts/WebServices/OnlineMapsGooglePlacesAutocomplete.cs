/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Retrieves information from Google Maps Place Autocomplete API.<br/>
/// Place Autocomplete service is a web service that returns place predictions.<br/>
/// The request specifies a textual search string and optional geographic bounds.<br/>
/// The service can be used to provide autocomplete functionality for text-based geographic searches, by returning places such as businesses, addresses and points of interest as a user types.<br/>
/// <strong>Requires Google Maps API key.</strong><br/>
/// https://developers.google.com/places/documentation/autocomplete
/// </summary>
public class OnlineMapsGooglePlacesAutocomplete: OnlineMapsTextWebService
{
    protected OnlineMapsGooglePlacesAutocomplete()
    {

    }

    protected OnlineMapsGooglePlacesAutocomplete(string input, Params p)
    {
        _status = OnlineMapsQueryStatus.downloading;

        if (string.IsNullOrEmpty(p.key)) p.key = OnlineMapsKeyManager.GoogleMaps();

        StringBuilder url = new StringBuilder("https://maps.googleapis.com/maps/api/place/autocomplete/xml?sensor=false");
        url.Append("&input=").Append(OnlineMapsWWW.EscapeURL(input));
        url.Append("&key=").Append(p.key);

        if (!string.IsNullOrEmpty(p.sessionToken)) url.Append("&sessiontoken=").Append(p.sessionToken);
        if (p.longitude.HasValue || p.latitude.HasValue) url.AppendFormat(OnlineMapsUtils.numberFormat, "&location={0},{1}", p.latitude.GetValueOrDefault(), p.longitude.GetValueOrDefault());
        if (p.radius != -1) url.Append("&radius=").Append(p.radius);
        if (p.offset != -1) url.Append("&offset=").Append(p.offset);
        if (!string.IsNullOrEmpty(p.types)) url.Append("&types=").Append(p.types);
        if (!string.IsNullOrEmpty(p.components)) url.Append("&components=").Append(p.components);
        if (!string.IsNullOrEmpty(p.language)) url.Append("&language=").Append(p.language);

        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Creates a new request to the Google Maps Place Autocomplete API.
    /// </summary>
    /// <param name="input">
    /// The text string on which to search. <br/>
    /// The Place Autocomplete service will return candidate matches based on this string and order results based on their perceived relevance.
    /// </param>
    /// <param name="key">
    /// Your application's API key. This key identifies your application for purposes of quota management. <br/>
    /// Visit the Google APIs Console to select an API Project and obtain your key. 
    /// </param>
    /// <param name="types">The types of place results to return.</param>
    /// <param name="offset">
    /// The position, in the input term, of the last character that the service uses to match predictions. <br/>
    /// For example, if the input is 'Google' and the offset is 3, the service will match on 'Goo'. <br/>
    /// The string determined by the offset is matched against the first word in the input term only. <br/>
    /// For example, if the input term is 'Google abc' and the offset is 3, the service will attempt to match against 'Goo abc'. <br/>
    /// If no offset is supplied, the service will use the whole term. <br/>
    /// The offset should generally be set to the position of the text caret.
    /// </param>
    /// <param name="lnglat">The point around which you wish to retrieve place information.</param>
    /// <param name="radius">
    /// The distance (in meters) within which to return place results. <br/>
    /// Note that setting a radius biases results to the indicated area, but may not fully restrict results to the specified area.
    /// </param>
    /// <param name="language">The language in which to return results.</param>
    /// <param name="components">
    /// A grouping of places to which you would like to restrict your results. <br/>
    /// Currently, you can use components to filter by country. <br/>
    /// The country must be passed as a two character, ISO 3166-1 Alpha-2 compatible country code. <br/>
    /// For example: components=country:fr would restrict your results to places within France.
    /// </param>
    /// <param name="sessionToken">A random string which identifies an autocomplete session for billing purposes. If this parameter is omitted from an autocomplete request, the request is billed independently.</param>
    /// <returns>Query instance to the Google API.</returns>
    public static OnlineMapsGooglePlacesAutocomplete Find(string input, string key = null, string types = null, int offset = -1, Vector2 lnglat = default(Vector2), int radius = -1, string language = null, string components = null, string sessionToken = null)
    {
        return new OnlineMapsGooglePlacesAutocomplete(
            input,
            new Params
            {
                key = key,
                sessionToken = sessionToken,
                types = types,
                offset = offset,
                location = lnglat,
                radius = radius,
                language = language,
                components = components
            });
    }

    public static OnlineMapsGooglePlacesAutocomplete Find(string input, Params p)
    {
        return new OnlineMapsGooglePlacesAutocomplete(input, p);
    }

    /// <summary>
    /// Converts response into an array of results.
    /// </summary>
    /// <param name="response">Response of Google API.</param>
    /// <returns>Array of result.</returns>
    public static OnlineMapsGooglePlacesAutocompleteResult[] GetResults(string response)
    {
        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response);
            string status = xml.Find<string>("//status");
            if (status != "OK") return null;

            List<OnlineMapsGooglePlacesAutocompleteResult> results = new List<OnlineMapsGooglePlacesAutocompleteResult>();

            OnlineMapsXMLList resNodes = xml.FindAll("//prediction");

            foreach (OnlineMapsXML node in resNodes)
            {
                results.Add(new OnlineMapsGooglePlacesAutocompleteResult(node));
            }

            return results.ToArray();
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
        }

        return null;
    }

    public class Params
    {
        /// <summary>
        /// Your application's API key. This key identifies your application for purposes of quota management. <br/>
        /// Visit the Google APIs Console to select an API Project and obtain your key. <br/>
        /// If empty, the key will be taken from the Key Manager.
        /// </summary>
        public string key;

        /// <summary>
        /// The text string on which to search.<br/>
        /// The Place Autocomplete service will return candidate matches based on this string and order results based on their perceived relevance.
        /// </summary>
        public string input;

        /// <summary>
        /// A random string which identifies an autocomplete session for billing purposes. If this parameter is omitted from an autocomplete request, the request is billed independently.
        /// </summary>
        public string sessionToken;

        /// <summary>
        /// The types of place results to return.
        /// </summary>
        public string types;

        /// <summary>
        /// The latitude of point around which you wish to retrieve place information.
        /// </summary>
        public double? latitude;

        /// <summary>
        /// The longitude of point around which you wish to retrieve place information.
        /// </summary>
        public double? longitude;

        /// <summary>
        /// The position, in the input term, of the last character that the service uses to match predictions.<br/>
        /// For example, if the input is 'Google' and the offset is 3, the service will match on 'Goo'.<br/>
        /// The string determined by the offset is matched against the first word in the input term only.<br/>
        /// For example, if the input term is 'Google abc' and the offset is 3, the service will attempt to match against 'Goo abc'.<br/>
        /// If no offset is supplied, the service will use the whole term.<br/>
        /// The offset should generally be set to the position of the text caret.
        /// </summary>
        public int offset;

        /// <summary>
        /// The distance (in meters) within which to return place results. <br/>
        /// Note that setting a radius biases results to the indicated area, but may not fully restrict results to the specified area.
        /// </summary>
        public int radius;

        /// <summary>
        /// The language in which to return results.
        /// </summary>
        public string language;

        /// <summary>
        /// A grouping of places to which you would like to restrict your results. <br/>
        /// Currently, you can use components to filter by country. <br/>
        /// The country must be passed as a two character, ISO 3166-1 Alpha-2 compatible country code. <br/>
        /// For example: components=country:fr would restrict your results to places within France.
        /// </summary>
        public string components;

        /// <summary>
        /// The point around which you wish to retrieve place information.
        /// </summary>
        public OnlineMapsVector2d location
        {
            get { return new OnlineMapsVector2d(longitude.GetValueOrDefault(), latitude.GetValueOrDefault()); }
            set
            {
                longitude = value.x;
                latitude = value.y;
            }
        }
    }
}