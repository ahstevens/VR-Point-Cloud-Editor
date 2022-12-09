/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Text;
using UnityEngine;

/// <summary>
/// The what3words API gives you programmatic access to convert a 3 word address to coordinates (forward geocoding), to convert coordinates to a 3 word address (reverse geocoding), to obtain suggestions based on a full or partial 3 word address (AutoSuggest and StandardBlend), to obtain a geographically bounded section of the 3m x 3m what3words grid and to determine the languages that the API currently supports. 
/// https://docs.what3words.com/api/v3/
/// </summary>
public class OnlineMapsWhat3Words:OnlineMapsTextWebService
{
    private const string endpoint = "https://api.what3words.com/v3/";

    private OnlineMapsWhat3Words(StringBuilder url)
    {
        _status = OnlineMapsQueryStatus.downloading;
        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Returns a list of 3 word addresses based on user input and other parameters.
    /// </summary>
    /// <param name="key">A valid API key</param>
    /// <param name="addr">The full or partial 3 word address to obtain suggestions for. At minimum this must be the first two complete words plus at least one character from the third word.</param>
    /// <param name="multilingual">AutoSuggest is provided via 2 variant resources; single language and multilingual.</param>
    /// <param name="lang">For single language the lang parameter is required; for multilingual, the lang parameter is optional. If specified, this parameter must be a supported 3 word address language as an ISO 639-1 2 letter code.</param>
    /// <param name="focus">A location, used to refine the results. If specified, the results will be weighted to give preference to those near the specified location in addition to considering similarity to the addr string. If omitted the default behaviour is to weight results for similarity to the addr string only.</param>
    /// <param name="clip">Restricts results to those within a geographical area.</param>
    /// <param name="count">The number of AutoSuggest results to return. A maximum of 100 results can be specified, if a number greater than this is requested, this will be truncated to the maximum. The default is 3.</param>
    /// <param name="display">Display type.</param>
    /// <returns>Query instance.</returns>
    public static OnlineMapsWhat3Words AutoSuggest(string key, string addr, bool multilingual = false, string lang = "en", OnlineMapsVector2d? focus = null, Clip clip = null, int? count = null, Display display = Display.full)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.What3Words();

        StringBuilder url = new StringBuilder(endpoint).Append("autosuggest");
        //if (multilingual) url.Append("-ml");
        url.Append("?format=json&key=").Append(key);
        url.Append("&input=").Append(addr);
        if (!string.IsNullOrEmpty(lang)) url.Append("&language=").Append(lang);
        if (focus.HasValue)
        {
            url.Append("&focus=").Append(focus.Value.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(focus.Value.x.ToString(OnlineMapsUtils.numberFormat));
        }
        if (clip != null) clip.AppendURL(url);
        if (count.HasValue) url.Append("&count=").Append(count.Value);
        if (display != Display.full)
        {
            if (display == Display.minimal) throw new Exception("AutoSuggest does not support Display.minimal.");
            url.Append("&display=").Append(display);
        }
        return new OnlineMapsWhat3Words(url);
    }

    /// <summary>
    /// Forward geocodes a 3 word address to a position, expressed as coordinates of latitude and longitude.
    /// </summary>
    /// <param name="key">A valid API key</param>
    /// <param name="addr">A 3 word address as a string</param>
    /// <param name="lang">A supported 3 word address language as an ISO 639-1 2 letter code. </param>
    /// <param name="display">Display type</param>
    /// <returns>Query instance.</returns>
    public static OnlineMapsWhat3Words Forward(string key, string addr, string lang = null, Display display = Display.full)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.What3Words();

        StringBuilder url = new StringBuilder(endpoint).Append("convert-to-coordinates");
        url.Append("?format=json&key=").Append(key);
        url.Append("&words=").Append(addr);
        if (!string.IsNullOrEmpty(lang)) url.Append("&lang=").Append(lang);
        if (display != Display.full) url.Append("&display=").Append(display);
        return new OnlineMapsWhat3Words(url);
    }

    /// <summary>
    /// Retrieves a list of the currently loaded and available 3 word address languages.
    /// </summary>
    /// <param name="key">A valid API key</param>
    /// <returns>Query instance.</returns>
    public static OnlineMapsWhat3Words GetLanguages(string key)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.What3Words();

        StringBuilder url = new StringBuilder(endpoint);
        url.Append("available-languages?format=json&key=").Append(key);
        return new OnlineMapsWhat3Words(url);
    }

    /// <summary>
    /// Converts the response string from Forward or Reverse geocoding to result object.
    /// </summary>
    /// <param name="response">Response string.</param>
    /// <returns>Result object</returns>
    public static OnlineMapsWhat3WordsFRResult GetForwardReverseResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsWhat3WordsFRResult>(response);
    }

    /// <summary>
    /// Converts the response string from AutoSuggest or StandardBlend to result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    public static OnlineMapsWhat3WordsSBResult GetSuggestionsBlendsResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsWhat3WordsSBResult>(response);
    }

    /// <summary>
    /// Converts the response string from Grid to result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    public static OnlineMapsWhat3WordsGridResult GetGridResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsWhat3WordsGridResult>(response);
    }

    /// <summary>
    /// Converts the response string from Get Languages to result object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <returns>Result object</returns>
    public static OnlineMapsWhat3WordsLanguagesResult GetLanguagesResult(string response)
    {
        return OnlineMapsJSON.Deserialize<OnlineMapsWhat3WordsLanguagesResult>(response);
    }

    /// <summary>
    /// Returns a section of the 3m x 3m what3words grid for a given area.
    /// </summary>
    /// <param name="key">A valid API key</param>
    /// <param name="bbox">Bounding box, for which the grid should be returned.</param>
    /// <returns>Query instance.</returns>
    public static OnlineMapsWhat3Words Grid(string key, OnlineMapsGeoRect bbox)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.What3Words();

        StringBuilder url = new StringBuilder(endpoint);
        url.Append("grid-section?format=json&key=").Append(key);
        url.Append("&bounding-box=")
            .Append(bbox.top.ToString(OnlineMapsUtils.numberFormat)).Append(",")
            .Append(bbox.right.ToString(OnlineMapsUtils.numberFormat)).Append(",")
            .Append(bbox.bottom.ToString(OnlineMapsUtils.numberFormat)).Append(",")
            .Append(bbox.left.ToString(OnlineMapsUtils.numberFormat));
        return new OnlineMapsWhat3Words(url);
    }

    /// <summary>
    /// Reverse geocodes coordinates, expressed as latitude and longitude to a 3 word address.
    /// </summary>
    /// <param name="key">A valid API key</param>
    /// <param name="coords">Coordinates</param>
    /// <param name="lang">A supported 3 word address language as an ISO 639-1 2 letter code.</param>
    /// <param name="display">Display type</param>
    /// <returns>Query instance.</returns>
    public static OnlineMapsWhat3Words Reverse(string key, OnlineMapsVector2d coords, string lang = null, Display display = Display.full)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.What3Words();

        StringBuilder url = new StringBuilder(endpoint).Append("convert-to-3wa");
        url.Append("?format=json&key=").Append(key);
        url.Append("&coordinates=")
            .Append(coords.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
            .Append(coords.x.ToString(OnlineMapsUtils.numberFormat));
        if (!string.IsNullOrEmpty(lang)) url.Append("&language=").Append(lang);
        if (display != Display.full) url.Append("&display=").Append(display);
        return new OnlineMapsWhat3Words(url);
    }

    /// <summary>
    /// Returns a blend of the three most relevant 3 word address candidates for a given location, based on a full or partial 3 word address. <br/>
    /// The specified 3 word address may either be a full 3 word address or a partial 3 word address containing the first 2 words in full and at least 1 character of the 3rd word.StandardBlend provides the search logic that powers the search box on map.what3words.com and in the what3words mobile apps.
    /// </summary>
    /// <param name="key">A valid API key</param>
    /// <param name="addr">The full or partial 3 word address to obtain blended candidates for. At minimum this must be the first two complete words plus at least one character from the third word</param>
    /// <param name="multilingual">
    /// StandardBlend is provided via 2 variant resources; single language and multilingual. <br/>
    /// The single language standardblend resource requires a language to be specified.The input full or partial 3 word address will be interpreted as being in the specified language and all results will be in this language. <br/>
    /// The multilingual standardblend-ml resource can accept an optional language. If specified, this will ensure that the standardblend-ml resource will look for results in this language, in addition to any other languages that yield relevant results.
    /// </param>
    /// <param name="lang">If specified, this parameter must be a supported 3 word address language as an ISO 639-1 2 letter code.</param>
    /// <param name="focus">A location, specified as a latitude,longitude used to refine the results. If specified, the results will be weighted to give preference to those near the specified location.</param>
    /// <returns>Query instance.</returns>
    public static OnlineMapsWhat3Words StandardBlend(string key, string addr, bool multilingual = false, string lang = "en", OnlineMapsVector2d? focus = null)
    {
        if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.What3Words();

        StringBuilder url = new StringBuilder(endpoint).Append("standardblend");
        if (multilingual) url.Append("-ml");
        url.Append("?format=json&key=").Append(key);
        url.Append("&addr=").Append(addr);
        if (!string.IsNullOrEmpty(lang)) url.Append("&lang=").Append(lang);
        if (focus.HasValue)
        {
            url.Append("&focus=")
                .Append(focus.Value.y.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(focus.Value.x.ToString(OnlineMapsUtils.numberFormat));
        }
        return new OnlineMapsWhat3Words(url);
    }

    /// <summary>
    /// Restricts results to those within a geographical area.
    /// </summary>
    public class Clip
    {
        private double v1;
        private double v2;
        private double v3;
        private double v4;
        private int type;

        /// <summary>
        /// Clips to a radius of km kilometers from the point.
        /// </summary>
        /// <param name="lng">Longitude of the point</param>
        /// <param name="lat">Latitude of the point</param>
        /// <param name="radius">Raduis (km)</param>
        public Clip(double lng, double lat, double radius)
        {
            v1 = lng;
            v2 = lat;
            v3 = radius;
            type = 0;
        }

        /// <summary>
        /// Clips to a radius of km kilometers from the point specified by the focus parameter.
        /// </summary>
        /// <param name="km">Radius (km)</param>
        public Clip(double km)
        {
            v1 = km;
            type = 1;
        }

        /// <summary>
        /// Clips to 3 word addresses that are fully contained inside a bounding box.
        /// </summary>
        /// <param name="leftLng">Left longitude</param>
        /// <param name="topLat">Top latitude</param>
        /// <param name="rightLng">Right longitude</param>
        /// <param name="bottomLat">Bottom latitude</param>
        public Clip(double leftLng, double topLat, double rightLng, double bottomLat)
        {
            v1 = leftLng;
            v2 = topLat;
            v3 = rightLng;
            v4 = bottomLat;
            type = 2;
        }

        public void AppendURL(StringBuilder url)
        {
            if (type == 0)
            {
                url.Append("&clip-to-circle=")
                    .Append(v2.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(v1.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(v3.ToString(OnlineMapsUtils.numberFormat));
            }
            /*else if (type == 1)
            {
                url.Append("focus(").Append(v1.ToString(OnlineMapsUtils.numberFormat)).Append(")");
            }*/
            else if (type == 2)
            {
                url.Append("&clip-to-bounding-box=")
                    .Append(v2.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(v3.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(v4.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                    .Append(v1.ToString(OnlineMapsUtils.numberFormat));
            }
        }
    }

    /// <summary>
    /// Display type
    /// </summary>
    public enum Display
    {
        /// <summary>
        /// Full output
        /// </summary>
        full,

        /// <summary>
        /// Less output
        /// </summary>
        terse,

        /// <summary>
        /// The bare minimum
        /// </summary>
        minimal
    }
}