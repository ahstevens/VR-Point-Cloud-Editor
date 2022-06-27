/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Text.RegularExpressions;

/// <summary>
/// Providers of the traffic tiles.
/// </summary>
public class OnlineMapsTrafficProvider
{
    private static OnlineMapsTrafficProvider[] _providers;

    /// <summary>
    /// Provider ID
    /// </summary>
    public string id;

    /// <summary>
    /// Provider name
    /// </summary>
    public string title;

    /// <summary>
    /// URL of tiles
    /// </summary>
    public string url;

    /// <summary>
    /// Indicates that this is an custom provider.
    /// </summary>
    public bool isCustom;

    /// <summary>
    ///  Gets an instance of a traffic provider by ID.
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <returns>Success: Instance of provider; FAILED - First provider</returns>
    public static OnlineMapsTrafficProvider GetByID(string id)
    {
        OnlineMapsTrafficProvider[] providers = GetProviders();
        foreach (OnlineMapsTrafficProvider p in providers) if (p.id == id) return p;
        return providers[0];
    }

    /// <summary>
    /// Gets array of traffic providers
    /// </summary>
    /// <returns>Array of traffic providers</returns>
    public static OnlineMapsTrafficProvider[] GetProviders()
    {
        if (_providers == null)
        {
            _providers = new[]
            {
                new OnlineMapsTrafficProvider
                {
                    id = "googlemaps",
                    title = "Google Maps",
                    url = "https://mts0.google.com/vt?pb=!1m4!1m3!1i{zoom}!2i{x}!3i{y}!2m3!1e0!2sm!3i301114286!2m6!1e2!2straffic!4m2!1soffset_polylines!2s0!5i1!2m12!1e2!2spsm!4m2!1sgid!2sl0t0vMkIqfb3hBb090479A!4m2!1ssp!2s1!5i1!8m2!13m1!14b1!3m25!2sru-RU!3sUS!5e18!12m1!1e50!12m3!1e37!2m1!1ssmartmaps!12m5!1e14!2m1!1ssolid!2m1!1soffset_polylines!12m4!1e52!2m2!1sentity_class!2s0S!12m4!1e26!2m2!1sstyles!2zcy5lOmx8cC52Om9mZixzLnQ6MXxwLnY6b2ZmLHMudDozfHAudjpvZmY!4e0"
                },
                new OnlineMapsTrafficProvider
                {
                    id = "nokia",
                    title = "Nokia Maps (here.com)",
                    url = "https://1.traffic.maps.api.here.com/maptile/2.1/flowtile/newest/terrain.day/{zoom}/{x}/{y}/256/png8?app_id=xWVIueSv6JL0aJ5xqTxb&app_code=djPZyynKsbTjIUDOBcHZ2g&lg=rus&ppi=72&pview=RUS&tprof=PrtlHere"
                },
                new OnlineMapsTrafficProvider
                {
                    id = "virtualearth",
                    title = "Virtual Earth (Bing Maps)",
                    url = "https://t0-traffic.tiles.virtualearth.net/comp/ch/{quad}?it=Z,TF&L&n=z"
                },
                new OnlineMapsTrafficProvider
                {
                    id = "custom",
                    title = "Custom",
                    isCustom = true
                }
            };
        }
        return _providers;
    }

    /// <summary>
    /// Gets the URL to download the traffic texture.
    /// </summary>
    /// <param name="tile">Instence of tile.</param>
    /// <returns>URL to texture</returns>
    public string GetURL(OnlineMapsTile tile)
    {
        return Regex.Replace(url, @"{\w+}", delegate (Match match)
        {
            string v = match.Value.ToLower().Trim('{', '}');

            if (OnlineMapsTile.OnReplaceTrafficURLToken != null)
            {
                string ret = OnlineMapsTile.OnReplaceTrafficURLToken(tile, v);
                if (ret != null) return ret;
            }

            if (v == "zoom") return tile.zoom.ToString();
            if (v == "z") return tile.zoom.ToString();
            if (v == "x") return tile.x.ToString();
            if (v == "y") return tile.y.ToString();
            if (v == "quad") return OnlineMapsUtils.TileToQuadKey(tile.x, tile.y, tile.zoom);
            return v;
        });
    }
}