/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// The class allows you to change the map location using the keyboard and the joystick.
/// </summary>
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Keyboard Input")]
[OnlineMapsPlugin("Keyboard Input", typeof(OnlineMapsControlBase))]
public class OnlineMapsKeyboardInput : MonoBehaviour, IOnlineMapsSavableComponent
{
    /// <summary>
    /// Speed of moving the map.
    /// </summary>
    public float speed = 1;

    private OnlineMaps map;
    private double tileX;
    private double tileY;
    private bool ignoreChangePosition;
    private OnlineMapsSavableItem[] savableItems;
    private OnlineMapsCameraOrbit cameraOrbit;

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("keyboardInput", "Keyboard Input", SaveSettings)
            {
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    private void LoadSettings(OnlineMapsJSONObject json)
    {
        json.DeserializeObject(this);
    }

    private void OnChangePosition()
    {
        if (ignoreChangePosition) return;

        double lng, lat;
        map.GetPosition(out lng, out lat);
        map.projection.CoordinatesToTile(lng, lat, map.zoom, out tileX, out tileY);
    }

    private OnlineMapsJSONItem SaveSettings()
    {
        return OnlineMapsJSON.Serialize(new
        {
            speed
        });
    }

    private void Start()
    {
        map = GetComponent<OnlineMaps>();
        cameraOrbit = GetComponent<OnlineMapsCameraOrbit>();

        double lng, lat;
        map.GetPosition(out lng, out lat);
        map.projection.CoordinatesToTile(lng, lat, map.zoom, out tileX, out tileY);
        map.OnChangePosition += OnChangePosition;
    }

    private void Update()
    {
        float latitudeSpeed = Input.GetAxis("Vertical") * Time.deltaTime;
        float longitudeSpeed = Input.GetAxis("Horizontal") * Time.deltaTime;

        if (Math.Abs(latitudeSpeed) < float.Epsilon && Math.Abs(longitudeSpeed) < float.Epsilon) return;

        if (cameraOrbit != null)
        {
            Vector3 v = Quaternion.Euler(0, cameraOrbit.rotation.y, 0) * new Vector3(longitudeSpeed, 0, latitudeSpeed);
            longitudeSpeed = v.x;
            latitudeSpeed = v.z;
        }

        tileX += longitudeSpeed * speed;
        tileY -= latitudeSpeed * speed;

        double lng, lat;

        map.projection.TileToCoordinates(tileX, tileY, map.zoom, out lng, out lat);

        ignoreChangePosition = true;
        map.SetPosition(lng, lat);
        ignoreChangePosition = false;
    }
}