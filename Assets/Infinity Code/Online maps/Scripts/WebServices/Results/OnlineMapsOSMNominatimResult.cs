/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of Nominatim query.
/// </summary>
public class OnlineMapsOSMNominatimResult
{
    /// <summary>
    /// XML node
    /// </summary>
    public OnlineMapsXML node;

    /// <summary>
    /// Place ID
    /// </summary>
    public long place_id;

    /// <summary>
    /// OSM Type
    /// </summary>
    public string osm_type;

    /// <summary>
    /// OSM ID
    /// </summary>
    public long osm_id;

    /// <summary>
    /// Place rank
    /// </summary>
    public int place_rank;

    /// <summary>
    /// Bounding box
    /// </summary>
    public Rect boundingbox;

    /// <summary>
    /// Latitude
    /// </summary>
    public double latitude;

    /// <summary>
    /// Longitude
    /// </summary>
    public double longitude;

    /// <summary>
    /// Ñoordinates of location (X - longitude, Y - latitude).
    /// </summary>
    public Vector2 location;

    /// <summary>
    /// Display name
    /// </summary>
    public string display_name;

    /// <summary>
    /// Type of object
    /// </summary>
    public string type;

    /// <summary>
    /// Importance
    /// </summary>
    public double importance;

    /// <summary>
    /// Dictionary of address details
    /// </summary>
    public Dictionary<string, string> addressdetails;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">XML Node</param>
    /// <param name="isReverse">Indicates reverse geocoding result.</param>
    public OnlineMapsOSMNominatimResult(OnlineMapsXML node, bool isReverse)
    {
        this.node = node;

        place_id = node.A<long>("place_id");
        osm_type = node.A("osm_type");
        osm_id = node.A<long>("osm_id");
        place_rank = node.A<int>("place_rank");
        latitude = node.A<double>("lat");
        longitude = node.A<double>("lon");
        location = new Vector2((float)longitude, (float)latitude);
        display_name = isReverse? node.Value(): node.A("display_name");
        type = node.A("type");
        importance = node.A<double>("importance");

        string bb = node.A("boundingbox");
        if (!string.IsNullOrEmpty(bb))
        {
            string[] bbParts = bb.Split(',');
            double w = Double.Parse(bbParts[0], OnlineMapsUtils.numberFormat);
            double e = Double.Parse(bbParts[1], OnlineMapsUtils.numberFormat);
            double s = Double.Parse(bbParts[2], OnlineMapsUtils.numberFormat);
            double n = Double.Parse(bbParts[3], OnlineMapsUtils.numberFormat);
            boundingbox = new Rect((float)w, (float)n, (float)(e - w), (float)(s - n));
        }
        
        addressdetails = new Dictionary<string, string>();
    }

    /// <summary>
    /// Load address details.
    /// </summary>
    /// <param name="adNode">Address details XML node.</param>
    public void LoadAddressDetails(OnlineMapsXML adNode)
    {
        foreach (OnlineMapsXML n in adNode)
        {
            if (!n.isNull) addressdetails.Add(n.name, n.Value());
        }
    }
}