/*         INFINITY CODE         */
/*   https://infinity-code.com   */

#if UCONTEXT
using InfinityCode.uContext;
using InfinityCode.uContext.Actions;
using InfinityCode.uContext.Windows;
using UnityEditor;
using UnityEngine;

public class OnlineMapsUContextActions : ActionItem, IValidatableLayoutItem
{
    private Vector2 lastPosition;

    protected override bool closeOnSelect
    {
        get { return false; }
    }

    protected override void Init()
    {
        lastPosition = Input.mousePosition;
        Texture2D icon = OnlineMapsEditorUtils.LoadAsset<Texture2D>("Icons/Online-Maps-uContext.png", true);
        _guiContent = new GUIContent(icon, "Online Maps");
    }

    public override void Invoke()
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Find Location By Name"), false, Geocode);
        menu.AddItem(new GUIContent("Find Location Name Under Cursor"), false, ReverseGeocode);
        menu.ShowAsContext();
    }

    private void ReverseGeocode()
    {
        uContextMenu.Close();

        double lng, lat;

        OnlineMapsControlBase.instance.GetCoords(lastPosition, out lng, out lat);

        new OnlineMapsWWW(
            "https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/reverseGeocode?f=pjson&location=" + lng.ToString(OnlineMapsUtils.numberFormat) + "," + lat.ToString(OnlineMapsUtils.numberFormat)
        ).OnComplete += OnReverseGeocodeComplete;
    }

    private void OnReverseGeocodeComplete(OnlineMapsWWW www)
    {
        if (www.hasError)
        {
            Debug.Log(www.error);
            return;
        }

        OnlineMapsJSONItem json = OnlineMapsJSON.Parse(www.text);
        Debug.Log(json["address/LongLabel"].V<string>());
    }

    public bool Validate()
    {
        return OnlineMaps.instance != null && EditorApplication.isPlaying;
    }

    private void Geocode()
    {
        uContextMenu.Close();
        InputDialog.Show("Input Location Name", "Location Name", OnInputLocationName);
    }

    private void OnInputLocationName(string locationName)
    {
        new OnlineMapsWWW(
            "https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/findAddressCandidates?f=pjson&address=" + OnlineMapsWWW.EscapeURL(locationName)
        ).OnComplete += OnGeocodeComplete;
    }

    private void OnGeocodeComplete(OnlineMapsWWW www)
    {
        if (www.hasError)
        {
            Debug.Log(www.error);
            return;
        }

        OnlineMapsJSONItem json = OnlineMapsJSON.Parse(www.text);
        OnlineMapsJSONItem firstItem = json["candidates/0"];
        if (firstItem == null) return;

        OnlineMapsVector2d center = firstItem["location"].Deserialize<OnlineMapsVector2d>();

        OnlineMapsJSONItem extent = firstItem["extent"];
        double xmin = extent.V<double>("xmin"), 
               ymin = extent.V<double>("ymin"), 
               xmax = extent.V<double>("xmax"), 
               ymax = extent.V<double>("ymax");

        Vector2[] points = 
        {
            new Vector2((float)xmin, (float)ymin), 
            new Vector2((float)xmax, (float)ymax), 
        };

        Vector2 c;
        int zoom;
        OnlineMapsUtils.GetCenterPointAndZoom(points, out c, out zoom);

        OnlineMaps.instance.SetPositionAndZoom(center.x, center.y, zoom);
    }
}
#endif