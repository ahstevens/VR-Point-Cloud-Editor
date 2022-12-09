/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

/// <summary>
/// Stores keys to all supported services and automatically uses them in requests.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Key Manager")]
[OnlineMapsPlugin("Key Manager", typeof(OnlineMapsControlBase), true)]
public class OnlineMapsKeyManager: MonoBehaviour
{
    private static OnlineMapsKeyManager instance;

    /// <summary>
    /// AMap key
    /// </summary>
    public string amap;

    /// <summary>
    /// ArcGIS key
    /// </summary>
    public string arcgisKey;

    /// <summary>
    /// Bing Maps key
    /// </summary>
    public string bingMaps;

    /// <summary>
    /// Google Maps key
    /// </summary>
    public string googleMaps;

    /// <summary>
    /// HERE API key
    /// </summary>
    public string hereApiKey;

    /// <summary>
    /// Here App Code
    /// </summary>
    public string hereAppCode;

    /// <summary>
    /// Here App ID
    /// </summary>
    public string hereAppID;

    /// <summary>
    /// Mapbox Access Token
    /// </summary>
    public string mapboxAccessToken;

    /// <summary>
    /// Open Route Service key
    /// </summary>
    public string openRouteService;

    /// <summary>
    /// QQ key
    /// </summary>
    public string qq;

    /// <summary>
    /// What 3 Words key
    /// </summary>
    public string what3Words;

    /// <summary>
    /// Is there a key for AMap
    /// </summary>
    public static bool hasAMap
    {
        get { return !string.IsNullOrEmpty(AMap()); }
    }

     /// <summary>
    /// Is there a key for ArcGIS
    /// </summary>
    public static bool hasArcGISKey
    {
        get { return !string.IsNullOrEmpty(ArcGIS()); }
    }

    /// <summary>
    /// Is there a key for Bing Maps
    /// </summary>
    public static bool hasBingMaps
    {
        get { return !string.IsNullOrEmpty(BingMaps()); }
    }

    /// <summary>
    /// Is there a key for Google Maps
    /// </summary>
    public static bool hasGoogleMaps
    {
        get { return !string.IsNullOrEmpty(GoogleMaps()); }
    }

    /// <summary>
    /// Is there a app id and app code for Here
    /// </summary>
    public static bool hasHere
    {
        get { return !string.IsNullOrEmpty(HereAppCode()) && !string.IsNullOrEmpty(HereAppID()); }
    }

    /// <summary>
    /// Is there a key for Here
    /// </summary>
    public static bool hasHereKey
    {
        get { return !string.IsNullOrEmpty(HereApiKey()); }
    }

    /// <summary>
    /// Is there an access token for Mapbox
    /// </summary>
    public static bool hasMapbox
    {
        get { return !string.IsNullOrEmpty(Mapbox()); }
    }

    /// <summary>
    /// Is there a key for Open Route Service
    /// </summary>
    public static bool hasOpenRouteService
    {
        get { return !string.IsNullOrEmpty(OpenRouteService()); }
    }

    /// <summary>
    /// Is there a key for QQ
    /// </summary>
    public static bool hasQQ
    {
        get { return !string.IsNullOrEmpty(QQ()); }
    }

    /// <summary>
    /// Is there a key for What 3 Words
    /// </summary>
    public static bool hasWhat3Words
    {
        get { return !string.IsNullOrEmpty(What3Words()); }
    }

    private void OnEnable()
    {
        instance = this;
    }

    /// <summary>
    /// Returns the key for AMap if present
    /// </summary>
    /// <returns>Key for AMap or null</returns>
    public static string AMap()
    {
        if (instance != null) return instance.amap;
        return null;
    }

    /// <summary>
    /// Returns the key for ArcGIS if present
    /// </summary>
    /// <returns>Key for ArcGIS or null</returns>
    public static string ArcGIS()
    {
        if (instance != null) return instance.arcgisKey;
        return null;
    }

    /// <summary>
    /// Returns the key for Bing Maps if present
    /// </summary>
    /// <returns>Key for Bing Maps or null</returns>
    public static string BingMaps()
    {
        if (instance != null) return instance.bingMaps;
        return null;
    }

    /// <summary>
    /// Returns the key for Google Maps if present
    /// </summary>
    /// <returns>Key for Google Maps or null</returns>
    public static string GoogleMaps()
    {
        if (instance != null) return instance.googleMaps;
        return null;
    }

    /// <summary>
    /// Returns Here Api Key if present
    /// </summary>
    /// <returns>Here Api Key or null</returns>
    public static string HereApiKey()
    {
        if (instance != null) return instance.hereApiKey;
        return null;
    }

    /// <summary>
    /// Returns Here App Code if present
    /// </summary>
    /// <returns>Here App Code or null</returns>
    public static string HereAppCode()
    {
        if (instance != null) return instance.hereAppCode;
        return null;
    }

    /// <summary>
    /// Returns Here App ID if present
    /// </summary>
    /// <returns>Here App ID or null</returns>
    public static string HereAppID()
    {
        if (instance != null) return instance.hereAppID;
        return null;
    }

    /// <summary>
    /// Returns Mapbox Access Token if present
    /// </summary>
    /// <returns>Mapbox Access Token or null</returns>
    public static string Mapbox()
    {
        if (instance != null) return instance.mapboxAccessToken;
        return null;
    }

    /// <summary>
    /// Returns the key for Open Route Service if present
    /// </summary>
    /// <returns>Key for Open Route Service or null</returns>
    public static string OpenRouteService()
    {
        if (instance != null) return instance.openRouteService;
        return null;
    }

    /// <summary>
    /// Returns the key for QQ if present
    /// </summary>
    /// <returns>Key for QQ or null</returns>
    public static string QQ()
    {
        if (instance != null) return instance.qq;
        return null;
    }

    /// <summary>
    /// Returns the key for What 3 Words if present
    /// </summary>
    /// <returns>Key for What 3 Words or null</returns>
    public static string What3Words()
    {
        if (instance != null) return instance.what3Words;
        return null;
    }
}