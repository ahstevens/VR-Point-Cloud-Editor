/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;

/// <summary>
/// Bing Maps Elevation API response object.<br/>
/// https://msdn.microsoft.com/en-us/library/jj158961.aspx
/// </summary>
public class OnlineMapsBingMapsElevationResult
{
    /// <summary>
    /// A status code that offers additional information about authentication success or failure.
    /// </summary>
    public string authenticationResultCode;

    /// <summary>
    /// A URL that references a brand image to support contractual branding requirements.
    /// </summary>
    public string brandLogoUri;

    /// <summary>
    /// A copyright notice.
    /// </summary>
    public string copyright;

    /// <summary>
    /// A collection of ResourceSet objects. A ResourceSet is a container of Resources returned by the request.
    /// </summary>
    public ResourceSet[] resourceSets;

    /// <summary>
    /// The HTTP Status code for the request.
    /// </summary>
    public int statusCode;

    /// <summary>
    /// A description of the HTTP status code.
    /// </summary>
    public string statusDescription;

    /// <summary>
    /// A unique identifier for the request.
    /// </summary>
    public string traceId;

    /// <summary>
    /// Constructor
    /// </summary>
    public OnlineMapsBingMapsElevationResult(){}

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="xml">Response XML</param>
    public OnlineMapsBingMapsElevationResult(OnlineMapsXML xml)
    {
        List<ResourceSet> rs = new List<ResourceSet>();

        foreach (OnlineMapsXML node in xml)
        {
            if (node.name == "Copyright") copyright = node.Value();
            else if (node.name == "BrandLogoUri") brandLogoUri = node.Value();
            else if (node.name == "StatusCode") statusCode = node.Value<int>();
            else if (node.name == "StatusDescription") statusDescription = node.Value();
            else if (node.name == "AuthenticationResultCode") authenticationResultCode = node.Value();
            else if (node.name == "TraceId") traceId = node.Value();
            else if (node.name == "ResourceSets") foreach (OnlineMapsXML rsNode in node) rs.Add(new ResourceSet(rsNode));
        }

        resourceSets = rs.ToArray();
    }

    /// <summary>
    /// A collection of Resource objects.
    /// </summary>
    public class ResourceSet
    {
        /// <summary>
        /// An estimate of the total number of resources in the ResourceSet.
        /// </summary>
        public int estimatedTotal;

        /// <summary>
        /// A collection of one or more resources. The resources that are returned depend on the request.
        /// </summary>
        public Resource[] resources;

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceSet(){}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xml">ResourceSet XML node</param>
        public ResourceSet(OnlineMapsXML xml)
        {
            List<Resource> rs = new List<Resource>();

            foreach (OnlineMapsXML node in xml)
            {
                if (node.name == "EstimatedTotal") estimatedTotal = node.Value<int>();
                else if (node.name == "Resources") foreach (OnlineMapsXML rsNode in node) rs.Add(new Resource(rsNode));
            }

            resources = rs.ToArray();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Resource
    {
        /// <summary>
        /// Array of offsets in meters of the geoid model (heights=sealevel) from the ellipsoid model (heights=ellipsoid) at each location (difference = geoid_sealevel - ellipsoid_sealevel).
        /// </summary>
        public int[] offsets;

        /// <summary>
        /// Array of elevations and the associated zoom level is returned in the responses that request elevation values.
        /// </summary>
        public int[] elevations;

        /// <summary>
        /// Zoom level.
        /// </summary>
        public int zoomLevel;

        /// <summary>
        /// Constructor
        /// </summary>
        public Resource(){}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xml">Resource XML node</param>
        public Resource(OnlineMapsXML xml)
        {
            List<int> el = new List<int>();

            foreach (OnlineMapsXML node in xml[0])
            {
                if (node.name == "ZoomLevel") zoomLevel = node.Value<int>();
                else if (node.name == "Elevations") foreach (OnlineMapsXML rsNode in node) el.Add(rsNode.Value<int>());
                else if (node.name == "Offsets") foreach (OnlineMapsXML rsNode in node) el.Add(rsNode.Value<int>());
            }

            elevations = el.ToArray();
        }
    }
}