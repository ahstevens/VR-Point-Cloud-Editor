/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// Controls map using GPS.<br/>
/// Online Maps Location Service is a wrapper for Unity Location Service.<br/>
/// http://docs.unity3d.com/ScriptReference/LocationService.html
/// </summary>
[Serializable]
[AddComponentMenu("Infinity Code/Online Maps/Plugins/Location Service")]
public class OnlineMapsLocationService : OnlineMapsLocationServiceGenericBase<OnlineMapsLocationService>
{
    /// <summary>
    /// Desired service accuracy in meters. 
    /// </summary>
    public float desiredAccuracy = 10;

    /// <summary>
    /// Request permission on Android at runtime when isEnabledByUser = false
    /// </summary>
    public bool requestPermissionRuntime = true;

    /// <summary>
    ///  The minimum distance (measured in meters) a device must move laterally before location is updated.
    /// </summary>
    public float updateDistance = 10;

    private List<LastPositionItem> lastPositions;
    private double lastLocationInfoTimestamp;
    private bool isPermissionRequested = false;
    private double _distance;

    /// <summary>
    /// 
    /// </summary>
    public double distance
    {
        get { return _distance; }
    }

    protected override void GetLocationFromSensor(out float longitude, out float latitude)
    {
        LocationInfo data = Input.location.lastData;
        longitude = data.longitude;
        latitude = data.latitude;
    }

    public override bool IsLocationServiceRunning()
    {
        return Input.location.status == LocationServiceStatus.Running;
    }

    protected override OnlineMapsJSONItem SaveSettings()
    {
        return base.SaveSettings().AppendObject(new
        {
            desiredAccuracy,
            updateDistance
        });
    }

    /// <summary>
    /// Starts location service updates. Last location coordinates could be.
    /// </summary>
    /// <param name="desiredAccuracyInMeters">
    /// Desired service accuracy in meters. <br/>
    /// Using higher value like 500 usually does not require to turn GPS chip on and thus saves battery power.<br/>
    /// Values like 5-10 could be used for getting best accuracy. Default value is 10 meters.
    /// </param>
    /// <param name="updateDistanceInMeters">
    /// The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. <br/>
    /// Higher values like 500 imply less overhead.
    /// </param>
    public void StartLocationService(float? desiredAccuracyInMeters = null, float? updateDistanceInMeters = null)
    {
        if (!desiredAccuracyInMeters.HasValue) desiredAccuracyInMeters = desiredAccuracy;
        if (!updateDistanceInMeters.HasValue) updateDistanceInMeters = updateDistance;

        Input.location.Start(desiredAccuracyInMeters.Value, updateDistanceInMeters.Value);
    }

    public override void StopLocationService()
    {
        Input.location.Stop();
    }

    public override bool TryStartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            if (requestPermissionRuntime && !isPermissionRequested)
            {
#if PLATFORM_ANDROID
                if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    isPermissionRequested = true;
                    Permission.RequestUserPermission(Permission.FineLocation);
                    return Permission.HasUserAuthorizedPermission(Permission.FineLocation);
                }
#endif
            }
            return false;
        }
        else
        {
            StartLocationService();
            return true;
        }
    }

    public override void UpdateSpeed()
    {
        float longitude, latitude;
        double timestamp;
        if (!useGPSEmulator)
        {
            if (Input.location.status != LocationServiceStatus.Running) return;

            LocationInfo lastData = Input.location.lastData;
            timestamp = lastData.timestamp;
            if (Math.Abs(lastLocationInfoTimestamp - timestamp) < double.Epsilon) return;

            longitude = lastData.longitude;
            latitude = lastData.latitude;

            lastLocationInfoTimestamp = timestamp;
        }
        else
        {
            timestamp = Time.time;
            longitude = emulatorPosition.x;
            latitude = emulatorPosition.y;
        }

        if (OnGetLocation != null) OnGetLocation(out longitude, out latitude);

        if (lastPositions == null) lastPositions = new List<LastPositionItem>();

        lastPositions.Add(new LastPositionItem(longitude, latitude, timestamp));
        while (lastPositions.Count > maxPositionCount) lastPositions.RemoveAt(0);

        if (lastPositions.Count < 2)
        {
            _speed = 0;
            return;
        }

        LastPositionItem p1 = lastPositions[0];
        LastPositionItem p2 = lastPositions[lastPositions.Count - 1];

        double dx, dy;
        OnlineMapsUtils.DistanceBetweenPoints(p1.lng, p1.lat, p2.lng, p2.lat, out dx, out dy);
        _distance = Math.Sqrt(dx * dx + dy * dy);
        double time = (p2.timestamp - p1.timestamp) / 3600;
        _speed = Mathf.Abs((float) (_distance / time));
    }

    internal struct LastPositionItem
    {
        public float lat;
        public float lng;
        public double timestamp;

        public LastPositionItem(float longitude, float latitude, double timestamp)
        {
            lng = longitude;
            lat = latitude;
            this.timestamp = timestamp;
        }
    }
}