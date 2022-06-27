/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Text;

/// <summary>
/// A Place Details request returns more comprehensive information about the indicated place such as its complete address, phone number, user rating and reviews.<br/>
/// <strong>Requires Google Maps API key.</strong><br/>
/// https://developers.google.com/places/webservice/details
/// </summary>
public class OnlineMapsGooglePlaceDetails : OnlineMapsTextWebService
{
    protected OnlineMapsGooglePlaceDetails()
    {

    }

    protected OnlineMapsGooglePlaceDetails(Params p)
    {
        _status = OnlineMapsQueryStatus.downloading;

        if (string.IsNullOrEmpty(p.key)) p.key = OnlineMapsKeyManager.GoogleMaps();

        StringBuilder url = new StringBuilder("https://maps.googleapis.com/maps/api/place/details/xml?sensor=false&key=").Append(p.key);

        if (!string.IsNullOrEmpty(p.place_id)) url.Append("&placeid=").Append(p.place_id);
        if (!string.IsNullOrEmpty(p.language)) url.Append("&language=").Append(p.language);
        if (!string.IsNullOrEmpty(p.region)) url.Append("&region=").Append(p.region);
        if (!string.IsNullOrEmpty(p.sessiontoken)) url.Append("&sessiontoken=").Append(p.sessiontoken);
        if (!string.IsNullOrEmpty(p.fields)) url.Append("&fields=").Append(p.fields);

        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Gets details about the place by place_id.
    /// </summary>
    /// <param name="key">
    /// Your application's API key.<br/>
    /// This key identifies your application for purposes of quota management and so that places added from your application are made immediately available to your app.<br/>
    /// Visit the Google Developers Console to create an API Project and obtain your key.
    /// </param>
    /// <param name="place_id">A textual identifier that uniquely identifies a place, returned from a Place Search.</param>
    /// <param name="language">
    /// The language code, indicating in which language the results should be returned, if possible.<br/>
    /// Note that some fields may not be available in the requested language.
    /// </param>
    /// <returns>Query instance to the Google API.</returns>
    public static OnlineMapsGooglePlaceDetails FindByPlaceID(string key, string place_id, string language = null)
    {
        return new OnlineMapsGooglePlaceDetails(new Params(place_id)
        {
            key = key,
            language = language
        });
    }

    public static OnlineMapsGooglePlaceDetails Find(Params p)
    {
        return new OnlineMapsGooglePlaceDetails(p);
    }

    /// <summary>
    /// Converts response into an result object.
    /// Note: The object may not contain all the available fields.<br/>
    /// Other fields can be obtained from OnlineMapsGooglePlaceDetailsResult.node.
    /// </summary>
    /// <param name="response">Response of Google API.</param>
    /// <returns>Result object or null.</returns>
    public static OnlineMapsGooglePlaceDetailsResult GetResult(string response)
    {
        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response);
            string status = xml.Find<string>("//status");
            if (status != "OK") return null;

            return new OnlineMapsGooglePlaceDetailsResult(xml["result"]);
        }
        catch
        {
        }

        return null;
    }

    public class Params
    {
        /// <summary>
        /// Your application's API key. <br/>
        /// This key identifies your application for purposes of quota management and so that places added from your application are made immediately available to your app.<br/>
        /// Visit the Google Developers Console to create an API Project and obtain your key. <br/>
        /// If null, the value will be taken from the Key Manager.
        /// </summary>
        public string key;

        /// <summary>
        /// A textual identifier that uniquely identifies a place, returned from a Place Search.
        /// </summary>
        public string place_id;

        /// <summary>
        /// The language code, indicating in which language the results should be returned, if possible. <br/>
        /// Note that some fields may not be available in the requested language. See the list of supported languages and their codes. <br/>
        /// Note that we often update supported languages so this list may not be exhaustive.
        /// </summary>
        public string language;

        /// <summary>
        /// The region code, specified as a ccTLD (country code top-level domain) two-character value. <br/>
        /// Most ccTLD codes are identical to ISO 3166-1 codes, with some exceptions. <br/>
        /// This parameter will only influence, not fully restrict, results. <br/>
        /// If more relevant results exist outside of the specified region, they may be included. <br/>
        /// When this parameter is used, the country name is omitted from the resulting formatted_address for results in the specified region.
        /// </summary>
        public string region;

        /// <summary>
        /// A random string which identifies an autocomplete session for billing purposes. <br/>
        /// Use this for Place Details requests that are called following an autocomplete request in the same user session.
        /// </summary>
        public string sessiontoken;

        /// <summary>
        /// One or more fields, specifying the types of place data to return, separated by a comma.
        /// </summary>
        public string fields;

        public Params(string place_id)
        {
            this.place_id = place_id;
        }
    }
}