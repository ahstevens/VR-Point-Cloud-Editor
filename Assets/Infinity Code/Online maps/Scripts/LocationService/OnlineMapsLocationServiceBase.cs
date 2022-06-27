/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Controls map using Location Service (GPS and compass).
/// </summary>
[OnlineMapsPlugin("Location Service", typeof(OnlineMapsControlBase))]
public abstract class OnlineMapsLocationServiceBase : MonoBehaviour, IOnlineMapsSavableComponent
{
    private static OnlineMapsLocationServiceBase _baseInstance;

    public delegate void OnGetLocationDelegate(out float longitude, out float latitude);

    /// <summary>
    /// This event is called when the user rotates the device
    /// </summary>
    public Action<float> OnCompassChanged;

    /// <summary>
    /// This event allows you to intercept receiving a GPS location
    /// </summary>
    public OnGetLocationDelegate OnGetLocation;

    /// <summary>
    /// This event is called when the IP location are found
    /// </summary>
    public Action OnFindLocationByIPComplete;

    /// <summary>
    /// This event is called when changed your GPS location
    /// </summary>
    public Action<Vector2> OnLocationChanged;

    /// <summary>
    /// This event is called when the GPS is initialized (the first value is received) or location by IP is found
    /// </summary>
    public Action OnLocationInited;

    /// <summary>
    /// This event called after map position restored when timeout "Restore After" expires
    /// </summary>
    public Action OnPositionRestored;

    /// <summary>
    /// Update stop position when user input
    /// </summary>
    public bool autoStopUpdateOnInput = true;

    /// <summary>
    /// Threshold of compass
    /// </summary>
    public float compassThreshold = 8;

    /// <summary>
    /// Specifies the need to create a marker that indicates the current GPS coordinates
    /// </summary>
    public bool createMarkerInUserPosition = false;

    /// <summary>
    /// Indicates whether to disable the emulator when used on the device
    /// </summary>
    public bool disableEmulatorInPublish = true;

    /// <summary>
    /// Emulated compass trueHeading. Do not use this field. Use OnlineMapsLocationService.trueHeading instead.
    /// </summary>
    public float emulatorCompass;

    /// <summary>
    /// Emulated GPS position. Do not use this field. Use OnlineMapsLocationService.position instead.
    /// </summary>
    public Vector2 emulatorPosition;

    /// <summary>
    /// Specifies whether to search for a location by IP
    /// </summary>
    public bool findLocationByIP = false;

    /// <summary>
    /// Smooth rotation by compass. This helps to bypass the jitter.
    /// </summary>
    public bool lerpCompassValue = true;

    /// <summary>
    /// Scale of the marker
    /// </summary>
    public float markerScale = 1;

    /// <summary>
    /// Tooltip of the marker
    /// </summary>
    public string markerTooltip;

    /// <summary>
    /// Type of the marker.
    /// </summary>
    public OnlineMapsLocationServiceMarkerType markerType = OnlineMapsLocationServiceMarkerType.twoD;

    /// <summary>
    /// Align of the 2D marker
    /// </summary>
    public OnlineMapsAlign marker2DAlign = OnlineMapsAlign.Center;

    /// <summary>
    /// Texture of 2D marker
    /// </summary>
    public Texture2D marker2DTexture;

    /// <summary>
    /// Prefab of 3D marker.
    /// </summary>
    public GameObject marker3DPrefab;

    /// <summary>
    /// Marker size type.
    /// </summary>
    public OnlineMapsMarker3D.SizeType marker3DSizeType = OnlineMapsMarker3D.SizeType.scene;

    /// <summary>
    /// The maximum number of stored positions.<br/>
    /// It is used to calculate the speed.
    /// </summary>
    public int maxPositionCount = 3;

    /// <summary>
    /// Current GPS coordinates.<br/>
    /// <strong>Important: position not available Start, because GPS is not already initialized.<br/>
    /// Use OnLocationInited event, to determine the initialization of GPS.</strong>
    /// </summary>
    public Vector2 position = Vector2.zero;

    /// <summary>
    /// Use the GPS coordinates after seconds of inactivity.
    /// </summary>
    public int restoreAfter = 10;

    /// <summary>
    /// Rotates the camera through a compass. Requires OnlineMapsCameraOrbit component.
    /// </summary>
    public bool rotateCameraByCompass = false;

    /// <summary>
    /// The heading in degrees relative to the geographic North Pole.<br/>
    /// <strong>Important: position not available Start, because compass is not already initialized.<br/>
    /// Use OnCompassChanged event, to determine the initialization of compass.</strong>
    /// </summary>
    public float trueHeading = 0;

    /// <summary>
    /// Specifies whether the script will automatically update the location
    /// </summary>
    public bool updatePosition = true;

    /// <summary>
    /// Specifies the need for marker rotation
    /// </summary>
    public bool useCompassForMarker = false;

    /// <summary>
    /// Specifies GPS emulator usage. Works only in Unity Editor.
    /// </summary>
    public bool useGPSEmulator = false;

    private OnlineMaps map;

    private bool _allowUpdatePosition = true;
    private float lastPositionChangedTime;
    private bool lockDisable;
    private bool isPositionInited = false;

    private OnlineMapsMarkerBase _marker;
    protected float _speed = 0;
    private bool started = false;
    private OnlineMapsSavableItem[] savableItems;

    /// <summary>
    /// Instance of LocationService base.
    /// </summary>
    public static OnlineMapsLocationServiceBase baseInstance
    {
        get { return _baseInstance; }
    }

    /// <summary>
    /// Instance of marker.
    /// </summary>
    public static OnlineMapsMarkerBase marker
    {
        get { return _baseInstance._marker; }
        set { _baseInstance._marker = value; }
    }

    /// <summary>
    /// Is it allowed to update the position.
    /// </summary>
    public bool allowUpdatePosition
    {
        get { return _allowUpdatePosition; }
        set
        {
            if (value == _allowUpdatePosition) return;
            _allowUpdatePosition = value;
            if (value) UpdatePosition();
        }
    }

    /// <summary>
    /// Speed km/h.
    /// Note: in Unity Editor will always be zero.
    /// </summary>
    public float speed
    {
        get { return _speed; }
    }

    /// <summary>
    /// Returns the current GPS location or emulator location.
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    public void GetLocation(out float longitude, out float latitude)
    {
        longitude = position.x;
        latitude = position.y;
    }

    /// <summary>
    /// Returns the current GPS location from sensor.
    /// </summary>
    /// <param name="longitude">Longitude</param>
    /// <param name="latitude">Latitude</param>
    protected abstract void GetLocationFromSensor(out float longitude, out float latitude);

    public OnlineMapsSavableItem[] GetSavableItems()
    {
        if (savableItems != null) return savableItems;

        savableItems = new[]
        {
            new OnlineMapsSavableItem("locationService", "Location Service", SaveSettings)
            {
                loadCallback = LoadSettings
            }
        };

        return savableItems;
    }

    /// <summary>
    /// Checks that the Location Service is running.
    /// </summary>
    /// <returns>True - location service is running, false - otherwise.</returns>
    public abstract bool IsLocationServiceRunning();

    public void LoadSettings(OnlineMapsJSONItem json)
    {
        // TODO: Implement
    }

    private void OnCameraOrbitChangedByInput()
    {
        if (lockDisable) return;

        lastPositionChangedTime = Time.realtimeSinceStartup;
        if (autoStopUpdateOnInput) _allowUpdatePosition = false;
    }

    private void OnChangePosition()
    {
        if (lockDisable) return;

        lastPositionChangedTime = Time.realtimeSinceStartup;
        if (autoStopUpdateOnInput) _allowUpdatePosition = false;
    }

    protected virtual void OnEnable()
    {
        _baseInstance = this;
        map = GetComponent<OnlineMaps>();
        if (map != null) map.OnChangePosition += OnChangePosition;
    }

    private void OnFindLocationComplete(OnlineMapsWWW www)
    {
        if (www.hasError) return;

        string response = www.text;
        if (string.IsNullOrEmpty(response)) return;

        int index = 0;
        string s = "\"loc\": \"";
        float lat = 0, lng = 0;
        bool finded = false;
        for (int i = 0; i < response.Length; i++)
        {
            if (response[i] == s[index])
            {
                index++;
                if (index >= s.Length)
                {
                    i++;
                    int startIndex = i;
                    while (true)
                    {
                        char c = response[i];
                        if (c == ',')
                        {
                            lat = float.Parse(response.Substring(startIndex, i - startIndex), OnlineMapsUtils.numberFormat);
                            i++;
                            startIndex = i;
                        }
                        else if (c == '"')
                        {
                            lng = float.Parse(response.Substring(startIndex, i - startIndex), OnlineMapsUtils.numberFormat);
                            finded = true;
                            break;
                        }
                        i++;
                    }
                    break;
                }
            }
            else index = 0;
        }

        if (finded)
        {
            if (useGPSEmulator) emulatorPosition = new Vector2(lng, lat);
            else if (position == Vector2.zero)
            {
                position = new Vector2(lng, lat);
                if (!isPositionInited && OnLocationInited != null)
                {
                    isPositionInited = true;
                    OnLocationInited();
                }
                if (OnLocationChanged != null) OnLocationChanged(position);
            }
            if (OnFindLocationByIPComplete != null) OnFindLocationByIPComplete();
        }
    }

    protected virtual OnlineMapsJSONItem SaveSettings()
    {
        OnlineMapsJSONObject json = OnlineMapsJSON.Serialize(new
        {
            autoStopUpdateOnInput,
            updatePosition,
            restoreAfter,
            createMarkerInUserPosition,
            useGPSEmulator
        }) as OnlineMapsJSONObject;

        if (createMarkerInUserPosition)
        {
            json.AppendObject(new
            {
                markerType,
                markerScale,
                markerTooltip,
                useCompassForMarker
            });

            if (markerType == OnlineMapsLocationServiceMarkerType.twoD)
            {
                json.AppendObject(new
                {
                    marker2DAlign,
                    marker2DTexture
                });
            }
            else
            {
                json.Add("marker3DPrefab", marker3DPrefab.GetInstanceID());
                json.Add("marker3DSizeType", marker3DSizeType);
            }
        }

        if (useGPSEmulator)
        {
            json.AppendObject(new
            {
                emulatorPosition,
                emulatorCompass
            });
        }

        return json;
    }

    public void SetStarted(bool value)
    {
        started = value;
    }

    private void Start()
    {
        map.OnChangePosition += OnChangePosition;

        if (OnlineMapsCameraOrbit.instance != null) OnlineMapsCameraOrbit.instance.OnChangedByInput += OnCameraOrbitChangedByInput;

        if (findLocationByIP)
        {
#if UNITY_EDITOR || !UNITY_WEBGL
            OnlineMapsWWW findByIPRequest = new OnlineMapsWWW("https://ipinfo.io/json");
#else
            OnlineMapsWWW findByIPRequest = new OnlineMapsWWW("https://service.infinity-code.com/getlocation.php");
#endif
            findByIPRequest.OnComplete += OnFindLocationComplete;
        }
    }

    /// <summary>
    /// Stops Location Service
    /// </summary>
    public abstract void StopLocationService();

    /// <summary>
    /// Try to start Location Service.
    /// </summary>
    /// <returns>True - success, false - otherwise.</returns>
    public abstract bool TryStartLocationService();

    private void Update()
    {
        if (map == null)
        {
            map = OnlineMaps.instance;
            if (map == null) return;
        }

        try
        {
            if (!started)
            {
#if !UNITY_EDITOR
                Input.compass.enabled = true;
                if(!TryStartLocationService()) return;
#endif
                started = true;
            }

#if !UNITY_EDITOR
            if (disableEmulatorInPublish) useGPSEmulator = false;
#endif
            bool positionChanged = false;

            if (createMarkerInUserPosition && _marker == null && (useGPSEmulator || position != Vector2.zero)) UpdateMarker();

            if (!useGPSEmulator && !IsLocationServiceRunning()) return;

            bool compassChanged = false;

            if (useGPSEmulator)
            {
                UpdateCompassFromEmulator(ref compassChanged);
                if (!isPositionInited) positionChanged = true;
            }
            else UpdateCompassFromInput(ref compassChanged);

            UpdateSpeed();

            if (useGPSEmulator) UpdatePositionFromEmulator(ref positionChanged);
            else UpdatePositionFromInput(ref positionChanged);

            if (createMarkerInUserPosition)
            {
                if (positionChanged || compassChanged) UpdateMarker();
                UpdateMarkerRotation();
            }

            if (rotateCameraByCompass)
            {
                UpdateCameraRotation();
            }

            if (positionChanged)
            {
                if (!isPositionInited)
                {
                    isPositionInited = true;
                    if (OnLocationInited != null) OnLocationInited();
                }
                if (OnLocationChanged != null) OnLocationChanged(position);
            }

            if (updatePosition)
            {
                if (_allowUpdatePosition)
                {
                    UpdatePosition();
                }
                else if (restoreAfter > 0 && Time.realtimeSinceStartup > lastPositionChangedTime + restoreAfter)
                {
                    _allowUpdatePosition = true;
                    UpdatePosition();
                    if (OnPositionRestored != null) OnPositionRestored();
                }
            }

            if (positionChanged/* || compassChanged*/) map.Redraw();
        }
        catch /*(Exception exception)*/
        {
            //errorMessage = exception.Message + "\n" + exception.StackTrace;
        }
    }

    private void UpdateCameraRotation()
    {
        OnlineMapsCameraOrbit co = OnlineMapsCameraOrbit.instance;
        if (co == null) return;

        float value = Mathf.Repeat(co.rotation.y, 360);
        float off = value - Mathf.Repeat(trueHeading, 360);

        if (off > 180) value -= 360;
        else if (off < -180) value += 360;

        if (!(Math.Abs(trueHeading - value) >= float.Epsilon)) return;

        if (!lerpCompassValue || Mathf.Abs(trueHeading - value) < 0.003f) value = trueHeading;
        else value = Mathf.Lerp(value, trueHeading, 0.02f);

        co.rotation = new Vector2(co.rotation.x, value);
    }

    private void UpdateCompassFromEmulator(ref bool compassChanged)
    {
        if (Math.Abs(trueHeading - emulatorCompass) > float.Epsilon)
        {
            compassChanged = true;
            trueHeading = Mathf.Repeat(emulatorCompass, 360);
            if (OnCompassChanged != null) OnCompassChanged(trueHeading / 360);
        }
    }

    private void UpdateCompassFromInput(ref bool compassChanged)
    {
        float heading = Input.compass.trueHeading;
        float offset = trueHeading - heading;

        if (offset > 180) offset -= 360;
        else if (offset < -180) offset += 360;

        if (Mathf.Abs(offset) > compassThreshold)
        {
            compassChanged = true;
            trueHeading = heading;
            if (OnCompassChanged != null) OnCompassChanged(trueHeading / 360);
        }
    }

    private void UpdateMarker()
    {
        if (_marker == null)
        {
            if (markerType == OnlineMapsLocationServiceMarkerType.twoD)
            {
                OnlineMapsMarker m2d = OnlineMapsMarkerManager.CreateItem(position, marker2DTexture, markerTooltip);
                _marker = m2d;
                m2d.align = marker2DAlign;
                m2d.scale = markerScale;
                if (useCompassForMarker) m2d.rotationDegree = trueHeading;
            }
            else
            {
                OnlineMapsControlBase3D control = map.control as OnlineMapsControlBase3D;
                if (control == null)
                {
                    Debug.LogError("You must use the 3D control (Texture or Tileset).");
                    createMarkerInUserPosition = false;
                    return;
                }
                OnlineMapsMarker3D m3d = OnlineMapsMarker3DManager.CreateItem(position, marker3DPrefab);
                _marker = m3d;
                m3d.sizeType = marker3DSizeType;
                m3d.scale = markerScale;
                m3d.label = markerTooltip;
                if (useCompassForMarker) m3d.rotationY = trueHeading;
            }
        }
        else
        {
            _marker.position = position;
        }
    }

    private void UpdateMarkerRotation()
    {
        if (!useCompassForMarker || marker == null) return;

        float value;
        if (markerType == OnlineMapsLocationServiceMarkerType.twoD) value = (_marker as OnlineMapsMarker).rotationDegree;
        else value = (_marker as OnlineMapsMarker3D).rotationY;

        if (trueHeading - value > 180) value += 360;
        else if (trueHeading - value < -180) value -= 360;

        if (Math.Abs(trueHeading - value) >= float.Epsilon)
        {
            if (!lerpCompassValue || Mathf.Abs(trueHeading - value) < 0.003f) value = trueHeading;
            else value = Mathf.Lerp(value, trueHeading, 0.02f);

            if (markerType == OnlineMapsLocationServiceMarkerType.twoD) (_marker as OnlineMapsMarker).rotationDegree = value;
            else (_marker as OnlineMapsMarker3D).rotationY = value;

            map.Redraw();
        }
    }

    /// <summary>
    /// Sets map position using GPS coordinates.
    /// </summary>
    public void UpdatePosition()
    {
        if (!useGPSEmulator && position == Vector2.zero) return;
        if (map == null) return;

        lockDisable = true;

        Vector2 p = map.position;
        bool changed = false;

        if (Math.Abs(p.x - position.x) > float.Epsilon)
        {
            p.x = position.x;
            changed = true;
        }
        if (Math.Abs(p.y - position.y) > float.Epsilon)
        {
            p.y = position.y;
            changed = true;
        }

        if (changed)
        {
            map.position = p;
            map.Redraw();
        }

        lockDisable = false;
    }

    private void UpdatePositionFromEmulator(ref bool positionChanged)
    {
        if (Math.Abs(position.x - emulatorPosition.x) > float.Epsilon)
        {
            position.x = emulatorPosition.x;
            positionChanged = true;
        }
        if (Math.Abs(position.y - emulatorPosition.y) > float.Epsilon)
        {
            position.y = emulatorPosition.y;
            positionChanged = true;
        }
    }

    private void UpdatePositionFromInput(ref bool positionChanged)
    {
        float longitude;
        float latitude;

        if (OnGetLocation != null) OnGetLocation(out longitude, out latitude);
        else
        {
            GetLocationFromSensor(out longitude, out latitude);
        }

        if (Math.Abs(position.x - longitude) > float.Epsilon)
        {
            position.x = longitude;
            positionChanged = true;
        }
        if (Math.Abs(position.y - latitude) > float.Epsilon)
        {
            position.y = latitude;
            positionChanged = true;
        }
    }

    /// <summary>
    /// Updates the speed data.
    /// </summary>
    public abstract void UpdateSpeed();
}
