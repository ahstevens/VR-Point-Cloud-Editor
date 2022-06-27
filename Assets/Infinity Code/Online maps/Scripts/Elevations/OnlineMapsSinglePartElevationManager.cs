/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using UnityEngine;

/// <summary>
/// Implements elevation managers, which loads elevation data in one piece.
/// </summary>
/// <typeparam name="T">Type of elevation manager</typeparam>
public abstract class OnlineMapsSinglePartElevationManager<T> : OnlineMapsElevationManager<T>
    where T : OnlineMapsSinglePartElevationManager<T>
{
    public bool tweenUpdateValues = false;
    public float tweenDuration = 0.5f;

    protected float elevationRequestX1;
    protected float elevationRequestY1;
    protected float elevationRequestX2;
    protected float elevationRequestY2;
    protected float elevationRequestW;
    protected float elevationRequestH;
    protected short[,] elevationData;
    protected float elevationX1;
    protected float elevationY1;
    protected float elevationW;
    protected float elevationH;
    protected int elevationDataWidth;
    protected int elevationDataHeight;
    protected bool waitSetElevationData;

    protected bool tweenStarted = false;
    protected float tweenProgress = 0;
    protected float prevX1;
    protected float prevY1;
    protected float prevW;
    protected float prevH;
    protected short[,] prevData;

    public override bool hasData
    {
        get { return elevationData != null; }
    }

    public override float GetElevationValue(double x, double z, float yScale, double tlx, double tly, double brx, double bry)
    {
        if (elevationData == null) return 0;

        float v = GetUnscaledElevationValue(x, z, tlx, tly, brx, bry);
        if (bottomMode == OnlineMapsElevationBottomMode.minValue) v -= minValue;
        return v * yScale * scale;
    }

    public override float GetUnscaledElevationValue(double x, double z, double tlx, double tly, double brx, double bry)
    {
        if (elevationData == null) return 0;
        if (elevationDataWidth == 0 || elevationDataHeight == 0 || elevationW == 0 || elevationH == 0) return 0;

        x = x / -_sizeInScene.x;
        z = z / _sizeInScene.y;

        int ew = elevationDataWidth - 1;
        int eh = elevationDataHeight - 1;

        if (x < 0) x = 0;
        else if (x > 1) x = 1;

        if (z < 0) z = 0;
        else if (z > 1) z = 1;

        double cx = (brx - tlx) * x + tlx;
        double cz = (bry - tly) * z + tly;

        float rx = (float)((cx - elevationX1) / elevationW * ew);
        float ry = (float)((cz - elevationY1) / elevationH * eh);

        if (rx < 0) rx = 0;
        else if (rx > ew) rx = ew;

        if (ry < 0) ry = 0;
        else if (ry > eh) ry = eh;

        int x1 = (int)rx;
        int x2 = x1 + 1;
        int y1 = (int)ry;
        int y2 = y1 + 1;
        if (x2 > ew) x2 = ew;
        if (y2 > eh) y2 = eh;

        float p1 = (elevationData[x2, eh - y1] - elevationData[x1, eh - y1]) * (rx - x1) + elevationData[x1, eh - y1];
        float p2 = (elevationData[x2, eh - y2] - elevationData[x1, eh - y2]) * (rx - x1) + elevationData[x1, eh - y2];

        float v = (p2 - p1) * (ry - y1) + p1;
        if (!tweenStarted || prevData == null) return v;

        float pv = GetPrevUnscaledElevation(x, z, tlx, tly, brx, bry);
        return pv > float.MinValue? Mathf.Lerp(pv, v, tweenProgress): v;
    }

    private float GetPrevUnscaledElevation(double x, double z, double tlx, double tly, double brx, double bry)
    {
        int ew = elevationDataWidth - 1;
        int eh = elevationDataHeight - 1;

        double cx = (brx - tlx) * x + tlx;
        double cz = (bry - tly) * z + tly;

        float rx = (float)((cx - prevX1) / prevW * ew);
        float ry = (float)((cz - prevY1) / prevH * eh);

        if (rx < 0) rx = 0;
        else if (rx > ew) rx = ew;

        if (ry < 0) ry = 0;
        else if (ry > eh) ry = eh;

        int x1 = (int)rx;
        int x2 = x1 + 1;
        int y1 = (int)ry;
        int y2 = y1 + 1;
        if (x2 > ew) x2 = ew;
        if (y2 > eh) y2 = eh;

        float p1 = (prevData[x2, eh - y1] - prevData[x1, eh - y1]) * (rx - x1) + prevData[x1, eh - y1];
        float p2 = (prevData[x2, eh - y2] - prevData[x1, eh - y2]) * (rx - x1) + prevData[x1, eh - y2];

        return (p2 - p1) * (ry - y1) + p1;
    }

    protected void SavePrevValues()
    {
        if (!tweenUpdateValues) return;

        prevX1 = elevationX1;
        prevY1 = elevationY1;
        prevW = elevationW;
        prevH = elevationH;
        tweenProgress = 0;
        if (elevationData != null)
        {
            if (prevData == null || prevData.GetLength(0) != elevationData.GetLength(0) || prevData.GetLength(1) != elevationData.GetLength(1)) prevData = new short[elevationData.GetLength(0),elevationData.GetLength(1)];
            for (int i = 0; i < prevData.GetLength(0); i++)
            {
                for (int j = 0; j < prevData.GetLength(1); j++)
                {
                    prevData[i, j] = elevationData[i, j];
                }
            }
            tweenStarted = true;
        }
    }

    public override void SetElevationData(short[,] data)
    {
        SavePrevValues();

        elevationData = data;
        elevationX1 = elevationRequestX1;
        elevationY1 = elevationRequestY1;
        elevationW = elevationRequestW;
        elevationH = elevationRequestH;
        elevationDataWidth = data.GetLength(0);
        elevationDataHeight = data.GetLength(1);

        UpdateMinMax();

        waitSetElevationData = false;

        if (OnElevationUpdated != null) OnElevationUpdated();
        map.Redraw();
    }

    protected override void Update()
    {
        if (tweenStarted)
        {
            tweenProgress += Time.deltaTime / tweenDuration;
            if (tweenProgress >= 1)
            {
                tweenProgress = 1;
                tweenStarted = false;
                if (OnElevationUpdated != null) OnElevationUpdated();
            }

            map.Redraw();
        }

        if (!zoomRange.InRange(map.buffer.lastState.floatZoom)) return;
        if (elevationBufferPosition == bufferPosition) return;

        RequestNewElevationData();
    }

    protected override void UpdateMinMax()
    {
        minValue = short.MaxValue;
        maxValue = short.MinValue;

        if (!hasData) return;

        int s1 = elevationData.GetLength(0);
        int s2 = elevationData.GetLength(1);

        for (int i = 0; i < s1; i++)
        {
            for (int j = 0; j < s2; j++)
            {
                short v = elevationData[i, j];
                if (v < minValue) minValue = v;
                if (v > maxValue) maxValue = v;
            }
        }
    }
}