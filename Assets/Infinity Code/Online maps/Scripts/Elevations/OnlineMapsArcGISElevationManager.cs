/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using UnityEngine;

/// <summary>
/// Implements the use of elevation data from ArcGIS
/// </summary>
[OnlineMapsPlugin("ArcGIS Elevations", typeof(OnlineMapsControlBaseDynamicMesh), "Elevations")]
[AddComponentMenu("Infinity Code/Online Maps/Elevations/ArcGIS")]
public class OnlineMapsArcGISElevationManager : OnlineMapsSinglePartElevationManager<OnlineMapsArcGISElevationManager>
{
    public int resolution = 32;

    private OnlineMapsWWW elevationRequest;
    private int elevationDataResolution;

    public override void CancelCurrentElevationRequest()
    {
        waitSetElevationData = false;
        elevationRequest = null;
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

    private void OnElevationRequestComplete(OnlineMapsWWW www)
    {
        elevationRequest = null;
        if (www.hasError)
        {
            Debug.Log(www.error);
            return;
        }

        string response = www.text;

        try
        {
            bool isFirstResponse = false;

            SavePrevValues();

            if (elevationData == null)
            {
                elevationData = new short[elevationDataResolution, elevationDataResolution];
                isFirstResponse = true;
            }

            int dataIndex = response.IndexOf("\"data\":[");
            if (dataIndex == -1)
            {
                if (isFirstResponse)
                {
                    elevationX1 = elevationRequestX1;
                    elevationY1 = elevationRequestY1;
                    elevationW = elevationRequestW;
                    elevationH = elevationRequestH;
                    SavePrevValues();
                    elevationDataWidth = elevationDataResolution;
                    elevationDataHeight = elevationDataResolution;
                }
                Debug.LogWarning(response);
                if (OnElevationFails != null) OnElevationFails(response);

                return;
            }
            dataIndex += 8;

            int index = 0;
            int v = 0;
            bool isNegative = false;

            for (int i = dataIndex; i < response.Length; i++)
            {
                char c = response[i];
                if (c == ',')
                {
                    int x = index % elevationDataResolution;
                    int y = elevationDataResolution - index / elevationDataResolution - 1;
                    if (isNegative) v = -v;
                    elevationData[x, y] = (short)v;
                    v = 0;
                    isNegative = false;
                    index++;
                }
                else if (c == '-') isNegative = true;
                else if (c > 47 && c < 58) v = v * 10 + (c - 48);
                else break;
            }

            if (isNegative) v = -v;
            elevationData[elevationDataResolution - 1, 0] = (short) v;

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
        catch (Exception exception)
        {
            Debug.Log(exception.Message + exception.StackTrace);
            if (OnElevationFails != null) OnElevationFails(exception.Message);
        }
        map.Redraw();
    }

    /// <summary>
    /// Starts downloading elevation data for the specified area.
    /// </summary>
    /// <param name="sx">Left longitude</param>
    /// <param name="sy">Top latitude</param>
    /// <param name="ex">Right longitude</param>
    /// <param name="ey">Bottom latitude</param>
    public void StartDownloadElevation(double sx, double sy, double ex, double ey)
    {
        if (resolution < 16) resolution = 16;
        else if (resolution > 100) resolution = 100;

        string url = "https://sampleserver4.arcgisonline.com/ArcGIS/rest/services/Elevation/ESRI_Elevation_World/MapServer/exts/ElevationsSOE/ElevationLayers/1/GetElevationData?f=json&Rows=" + resolution + "&Columns=" + resolution + "&Extent=";
        url += OnlineMapsWWW.EscapeURL("{\"spatialReference\":{\"wkid\":4326},\"ymin\":" + ey.ToString(OnlineMapsUtils.numberFormat) +
                                       ",\"ymax\":" + sy.ToString(OnlineMapsUtils.numberFormat) +
                                       ",\"xmin\":" + sx.ToString(OnlineMapsUtils.numberFormat) +
                                       ",\"xmax\":" + ex.ToString(OnlineMapsUtils.numberFormat) + "}");
        elevationRequest = new OnlineMapsWWW(url);
        elevationRequest.OnComplete += OnElevationRequestComplete;
        elevationDataResolution = resolution;
    }
}