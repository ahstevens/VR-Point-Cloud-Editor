/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// This class is used to search OSM data by name and address and to generate synthetic addresses of OSM points (reverse geocoding).<br/>
/// http://wiki.openstreetmap.org/wiki/Nominatim
///  </summary>
public class OnlineMapsOSMNominatim: OnlineMapsTextWebService
{
    private OnlineMapsOSMNominatim(string query, string acceptlanguage, int limit, bool addressdetails)
    {
        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder("https://nominatim.openstreetmap.org/search?format=xml&q=").Append(OnlineMapsWWW.EscapeURL(query));
        if (addressdetails) url.Append("&addressdetails=1");
        if (limit > 0) url.Append("&limit=").Append(limit);
        if (!string.IsNullOrEmpty(acceptlanguage)) url.Append("&accept-language=").Append(acceptlanguage);

        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    private OnlineMapsOSMNominatim(Vector2 location, string acceptlanguage, bool addressdetails)
    {
        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder("https://nominatim.openstreetmap.org/reverse?format=xml&lat=");
        url.Append(location.y.ToString(OnlineMapsUtils.numberFormat))
            .Append("&lon=").Append(location.x.ToString(OnlineMapsUtils.numberFormat));

        if (addressdetails) url.Append("&addressdetails=1");
        if (!string.IsNullOrEmpty(acceptlanguage)) url.Append("&accept-language=").Append(acceptlanguage);
        
        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Nominatim indexes named (or numbered) features with the OSM data set and a subset of other unnamed features (pubs, hotels, churches, etc).
    /// </summary>
    /// <param name="query">Query string to search for.</param>
    /// <param name="acceptlanguage">
    /// Preferred language order for showing search results, overrides the value specified in the "Accept-Language" HTTP header.<br/>
    /// Either uses standard rfc2616 accept-language string or a simple comma separated list of language codes.
    /// </param>
    /// <param name="limit">Limit the number of returned results.</param>
    /// <param name="addressdetails">Include a breakdown of the address into elements.</param>
    /// <returns>Instance of query</returns>
    public static OnlineMapsTextWebService Search(string query, string acceptlanguage = "en", int limit = 0, bool addressdetails = true)
    {
        return new OnlineMapsOSMNominatim(query, acceptlanguage, limit, addressdetails);
    }

    /// <summary>
    /// Reverse geocoding generates an address from a latitude and longitude. 
    /// </summary>
    /// <param name="location">The location to generate an address for.</param>
    /// <param name="acceptlanguage">
    /// Preferred language order for showing search results, overrides the value specified in the "Accept-Language" HTTP header.<br/>
    /// Either uses standard rfc2616 accept-language string or a simple comma separated list of language codes.
    /// </param>
    /// <param name="addressdetails">Include a breakdown of the address into elements.</param>
    /// <returns>Instance of query</returns>
    public static OnlineMapsTextWebService Reverse(Vector2 location, string acceptlanguage = "en", bool addressdetails = true)
    {
        return new OnlineMapsOSMNominatim(location, acceptlanguage, addressdetails);
    }

    /// <summary>
    /// Converts response into an array of results.
    /// </summary>
    /// <param name="response">Response of query.</param>
    /// <returns>Array of result.</returns>
    public static OnlineMapsOSMNominatimResult[] GetResults(string response)
    {
        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response);
            bool isReverse = xml.name == "reversegeocode";

            OnlineMapsXMLList resNodes = xml.FindAll(isReverse? "//result" : "//place");
            if (resNodes.count == 0) return null;

            List<OnlineMapsOSMNominatimResult> results = new List<OnlineMapsOSMNominatimResult>();
            foreach (OnlineMapsXML node in resNodes)
            {
                OnlineMapsOSMNominatimResult result = new OnlineMapsOSMNominatimResult(node, isReverse);

                OnlineMapsXML adNode = isReverse ? xml["addressparts"] : node;
                if (!adNode.isNull) result.LoadAddressDetails(adNode);
                results.Add(result);
            }
            return results.ToArray();
        }
        catch (Exception exception)
        {
            Debug.Log("Can not get a result.\n" + exception.Message + "\n" + exception.StackTrace);
        }

        return null;
    }
}