/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// The Elevation API provides elevation data for all locations on the surface of the earth, including depth locations on the ocean floor (which return negative values).<br/>
/// In those cases where Google does not possess exact elevation measurements at the precise location you request, the service will interpolate and return an averaged value using the four nearest locations.<br/>
/// With the Elevation API, you can develop hiking and biking applications, mobile positioning applications, or low resolution surveying applications. <br/>
/// https://developers.google.com/maps/documentation/elevation/
/// </summary>
public class OnlineMapsGoogleElevation: OnlineMapsTextWebService
{
    protected OnlineMapsGoogleElevation()
    {

    }

    protected OnlineMapsGoogleElevation(Vector2 location, string key, string client, string signature)
    {
        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder("https://maps.googleapis.com/maps/api/elevation/xml?sensor=false&locations=")
            .Append(location.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
            .Append(location.x.ToString(OnlineMapsUtils.numberFormat));
        Download(url, key, client, signature);
    }

    protected OnlineMapsGoogleElevation(Vector2[] locations, string key, string client, string signature)
    {
        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder("https://maps.googleapis.com/maps/api/elevation/xml?sensor=false&locations=");

        for (int i = 0; i < locations.Length; i++)
        {
            url.Append(locations[i].y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(locations[i].x.ToString(OnlineMapsUtils.numberFormat));
            if (i < locations.Length - 1) url.Append("|");
        }

        Download(url, key, client, signature);
    }

    private OnlineMapsGoogleElevation(Vector2[] path, int samples, string key, string client, string signature)
    {
        _status = OnlineMapsQueryStatus.downloading;
        StringBuilder url = new StringBuilder();
        url.Append("https://maps.googleapis.com/maps/api/elevation/xml?sensor=false&path=");

        for (int i = 0; i < path.Length; i++)
        {
            url.Append(path[i].y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(path[i].x.ToString(OnlineMapsUtils.numberFormat));
            if (i < path.Length - 1) url.Append("|");
        }

        url.Append("&samples=").Append(samples);

        Download(url, key, client, signature);
    }

    private void Download(StringBuilder url, string key, string client, string signature)
    {
        if (!string.IsNullOrEmpty(key)) url.Append("&key=").Append(key);
        else if (OnlineMapsKeyManager.hasGoogleMaps) url.Append("&key=").Append(OnlineMapsKeyManager.GoogleMaps());

        if (!string.IsNullOrEmpty(client)) url.Append("&client=").Append(client);
        if (!string.IsNullOrEmpty(signature)) url.Append("&signature=").Append(signature);
        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Get elevation value for single location.
    /// </summary>
    /// <param name="location">
    /// Location on the earth from which to return elevation data.
    /// </param>
    /// <param name="key">
    /// Your application's API key. <br/>
    /// This key identifies your application for purposes of quota management.
    /// </param>
    /// <param name="client">
    /// Client ID identifies you as a Maps API for Work customer and enables support and purchased quota for your application.<br/>
    /// Requests made without a client ID are not eligible for Maps API for Work benefits.
    /// </param>
    /// <param name="signature">
    /// Your welcome letter includes a cryptographic signing key, which you must use to generate unique signatures for your web service requests.
    /// </param>
    /// <returns>Query instance to the Google API.</returns>
    public static OnlineMapsGoogleElevation Find(Vector2 location, string key = null, string client = null, string signature = null)
    {
        return new OnlineMapsGoogleElevation(location, key, client, signature);
    }

    /// <summary>
    /// Get elevation values for several locations.
    /// </summary>
    /// <param name="locations">
    /// Locations on the earth from which to return elevation data.
    /// </param>
    /// <param name="key">
    /// Your application's API key.<br/>
    /// This key identifies your application for purposes of quota management.
    /// </param>
    /// <param name="client">
    /// Client ID identifies you as a Maps API for Work customer and enables support and purchased quota for your application.<br/>
    /// Requests made without a client ID are not eligible for Maps API for Work benefits.
    /// </param>
    /// <param name="signature">
    /// Your welcome letter includes a cryptographic signing key, which you must use to generate unique signatures for your web service requests.
    /// </param>
    /// <returns>Query instance to the Google API.</returns>
    public static OnlineMapsGoogleElevation Find(Vector2[] locations, string key = null, string client = null, string signature = null)
    {
        return new OnlineMapsGoogleElevation(locations, key, client, signature);
    }

    /// <summary>
    /// Get elevation values for path.
    /// </summary>
    /// <param name="path">Path on the earth for which to return elevation data. </param>
    /// <param name="samples">
    /// Specifies the number of sample points along a path for which to return elevation data.<br/>
    /// The samples parameter divides the given path into an ordered set of equidistant points along the path.
    /// </param>
    /// <param name="key">
    /// Your application's API key.<br/>
    /// This key identifies your application for purposes of quota management.
    /// </param>
    /// <param name="client">
    /// Client ID identifies you as a Maps API for Work customer and enables support and purchased quota for your application.<br/>
    /// Requests made without a client ID are not eligible for Maps API for Work benefits.
    /// </param>
    /// <param name="signature">
    /// Your welcome letter includes a cryptographic signing key, which you must use to generate unique signatures for your web service requests.
    /// </param>
    /// <returns>Query instance to the Google API.</returns>
    public static OnlineMapsGoogleElevation Find(Vector2[] path, int samples, string key = null, string client = null, string signature = null)
    {
        return new OnlineMapsGoogleElevation(path, samples, key, client, signature);
    }

    /// <summary>
    /// Converts response into an array of results.
    /// </summary>
    /// <param name="response">Response of Google API.</param>
    /// <returns>Array of result.</returns>
    public static OnlineMapsGoogleElevationResult[] GetResults(string response)
    {
        OnlineMapsXML xml = OnlineMapsXML.Load(response);
        if (xml.isNull || xml.Get<string>("status") != "OK") return null;

        List<OnlineMapsGoogleElevationResult> rList = new List<OnlineMapsGoogleElevationResult>();
        foreach (OnlineMapsXML node in xml.FindAll("result")) rList.Add(new OnlineMapsGoogleElevationResult(node));

        return rList.ToArray();
    }
}