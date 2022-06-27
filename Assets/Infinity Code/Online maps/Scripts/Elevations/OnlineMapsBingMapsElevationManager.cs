/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Implements the use of elevation data from Bing Maps
/// </summary>
[OnlineMapsPlugin("Bing Maps Elevations", typeof(OnlineMapsControlBaseDynamicMesh), "Elevations")]
[AddComponentMenu("Infinity Code/Online Maps/Elevations/Bing Maps")]
public class OnlineMapsBingMapsElevationManager : OnlineMapsSinglePartElevationManager<OnlineMapsBingMapsElevationManager>
{
    /// <summary>
    /// Bing Maps API key
    /// </summary>
    public string bingAPI = "";

    private OnlineMapsBingMapsElevation elevationRequest;

    public override void CancelCurrentElevationRequest() 
    {
        waitSetElevationData = false;
        
        if (elevationRequest != null)
        {
            elevationRequest.Destroy();
            elevationRequest = null;
        }
    }

    public override void RequestNewElevationData()
    {
        base.RequestNewElevationData();

        if (elevationRequest != null || waitSetElevationData) return;

        elevationBufferPosition = bufferPosition;

        const int s = OnlineMapsUtils.tileSize;
        int countX = map.width / s + 2;
        int countY = map.height / s + 2;

        double sx, sy, ex, ey;
        map.projection.TileToCoordinates(bufferPosition.x - 1, bufferPosition.y - 1, map.zoom, out sx, out sy);
        map.projection.TileToCoordinates(bufferPosition.x + countX + 1, bufferPosition.y + countY + 1, map.zoom, out ex, out ey);

        elevationRequestX1 = (float)sx;
        elevationRequestY1 = (float)sy;
        elevationRequestX2 = (float)ex;
        elevationRequestY2 = (float)ey;
        elevationRequestW = elevationRequestX2 - elevationRequestX1;
        elevationRequestH = elevationRequestY2 - elevationRequestY1;

        if (OnGetElevation == null)
        {
            StartDownloadElevation(sx, sy, ex, ey);
        }
        else
        {
            waitSetElevationData = true;
            OnGetElevation(sx, sy, ex, ey);
        }

        if (OnElevationRequested != null) OnElevationRequested();
    }

    private void OnElevationRequestComplete(string response)
    {
        const int elevationDataResolution = 32;

        try
        {
            SavePrevValues();

            bool isFirstResponse = false;
            if (elevationData == null)
            {
                elevationData = new short[elevationDataResolution, elevationDataResolution];
                isFirstResponse = true;
            }
            Array ed = elevationData;

            if (OnlineMapsBingMapsElevation.ParseElevationArray(response, OnlineMapsBingMapsElevation.Output.json, ref ed))
            {
                elevationX1 = elevationRequestX1;
                elevationY1 = elevationRequestY1;
                elevationW = elevationRequestW;
                elevationH = elevationRequestH;
                elevationDataWidth = elevationDataResolution;
                elevationDataHeight = elevationDataResolution;

                UpdateMinMax();
                if (OnElevationUpdated != null) OnElevationUpdated();

                control.UpdateControl();
            }
            else
            {
                if (isFirstResponse)
                {
                    elevationX1 = elevationRequestX1;
                    elevationY1 = elevationRequestY1;
                    elevationW = elevationRequestW;
                    elevationH = elevationRequestH;
                    elevationDataWidth = elevationDataResolution;
                    elevationDataHeight = elevationDataResolution;
                }
                Debug.LogWarning(response);
                if (OnElevationFails != null) OnElevationFails(response);
            }
            elevationRequest = null;
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
            if (OnElevationFails != null) OnElevationFails(exception.Message);
        }
        map.Redraw();
    }

    protected override void Start()
    {
        base.Start();

        if (string.IsNullOrEmpty(bingAPI) && !OnlineMapsKeyManager.hasBingMaps)
        {
            Debug.LogWarning("Missing Map / Key Manager / Bing Maps API key.");
        }
    }

    /// <summary>
    /// Starts downloading elevation data for an area
    /// </summary>
    /// <param name="sx">Left longitude</param>
    /// <param name="sy">Top latitude</param>
    /// <param name="ex">Right longitude</param>
    /// <param name="ey">Bottom latitude</param>
    public void StartDownloadElevation(double sx, double sy, double ex, double ey)
    {
        elevationRequest = OnlineMapsBingMapsElevation.GetElevationByBounds(bingAPI, sx, sy, ex, ey, 32, 32);
        elevationRequest.OnComplete += OnElevationRequestComplete;
    }
}