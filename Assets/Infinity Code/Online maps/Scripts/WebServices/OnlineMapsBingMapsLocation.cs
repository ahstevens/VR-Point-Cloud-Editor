/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// This class is used to search for a location by address using Bing Maps Location API.<br/>
/// https://msdn.microsoft.com/en-us/library/ff701715.aspx
/// </summary>
public class OnlineMapsBingMapsLocation: OnlineMapsTextWebService
{
    private OnlineMapsBingMapsLocation(string query, string key, int maxResults, bool includeNeighborhood)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.BingMaps();

        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder();
        url.AppendFormat("https://dev.virtualearth.net/REST/v1/Locations/{0}?key={1}&o=xml", OnlineMapsWWW.EscapeURL(query), key);
        if (includeNeighborhood) url.Append("&inclnb=1");
        if (maxResults > 0 && maxResults != 5) url.Append("&maxRes=").Append(maxResults);
        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    private OnlineMapsBingMapsLocation(Vector2 point, string key, bool includeNeighborhood)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.BingMaps();

        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder();
        url.AppendFormat("https://dev.virtualearth.net/REST/v1/Locations/{0}?key={1}&o=xml", point.y + "," + point.x, key);
        if (includeNeighborhood) url.Append("&inclnb=1");
        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Get latitude and longitude coordinates that correspond to location information provided as a query string.
    /// </summary>
    /// <param name="query">A string that contains information about a location, such as an address or landmark name.</param>
    /// <param name="key">Bing Maps API Key</param>
    /// <param name="maxResults">Specifies the maximum number (1-20) of locations to return in the response.</param>
    /// <param name="includeNeighborhood">Specifies to include the neighborhood with the address information the response when it is available. </param>
    /// <returns>Instance of query</returns>
    public static OnlineMapsBingMapsLocation FindByQuery(string query, string key, int maxResults = 5, bool includeNeighborhood = false)
    {
        return new OnlineMapsBingMapsLocation(query, key, maxResults, includeNeighborhood);
    }

    /// <summary>
    /// Get the location information associated with latitude and longitude coordinates (reverse geocoding). 
    /// </summary>
    /// <param name="point">The coordinates of the location that you want to reverse geocode (X - Longitude, Y - Latitude).</param>
    /// <param name="key">Bing Maps API Key.</param>
    /// <param name="includeNeighborhood">Specifies to include the neighborhood in the response when it is available. </param>
    /// <returns>Instance of query</returns>
    public static OnlineMapsBingMapsLocation FindByPoint(Vector2 point, string key, bool includeNeighborhood = false)
    {
        return new OnlineMapsBingMapsLocation(point, key, includeNeighborhood);
    }

    /// <summary>
    /// Converts response into an array of results.
    /// </summary>
    /// <param name="response">Response of query.</param>
    /// <returns>Array of result.</returns>
    public static OnlineMapsBingMapsLocationResult[] GetResults(string response)
    {
        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response.Substring(1));
            OnlineMapsXMLNamespaceManager nsmgr = xml.GetNamespaceManager("x");
            string status = xml.Find<string>("//x:StatusDescription", nsmgr);

            if (status != "OK") return null;

            List<OnlineMapsBingMapsLocationResult> results = new List<OnlineMapsBingMapsLocationResult>();
            OnlineMapsXMLList resNodes = xml.FindAll("//x:Location", nsmgr);

            foreach (OnlineMapsXML node in resNodes)
            {
                results.Add(new OnlineMapsBingMapsLocationResult(node));
            }

            return results.ToArray();
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
        }

        return null;
    }
}