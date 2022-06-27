/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// Use the Elevations API to get elevation information for a set of locations, polyline or area on the Earth.<br/>
/// https://msdn.microsoft.com/en-us/library/jj158961.aspx
/// </summary>
public class OnlineMapsBingMapsElevation:OnlineMapsTextWebService
{
    private OnlineMapsBingMapsElevation(Params p)
    {
        StringBuilder builder = new StringBuilder();
        p.GenerateURL(builder);
        www = new OnlineMapsWWW(builder);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Get the elevations for each set of coordinates.
    /// </summary>
    /// <param name="key">A Bing Maps Key.</param>
    /// <param name="points">
    /// A set of coordinates on the Earth to use in elevation calculations.<br/>
    /// IEnumerable values can be float, double or Vector2.
    /// </param>
    /// <param name="heights">Specifies which sea level model to use to calculate elevation.</param>
    /// <param name="output">Output format: JSON or XML</param>
    /// <returns>Instance of request.</returns>
    public static OnlineMapsBingMapsElevation GetElevationByPoints(string key, IEnumerable points, Heights heights = Heights.sealevel, Output output = Output.json)
    {
        return new OnlineMapsBingMapsElevation(new PointsParams(key, heights, output, points));
    }

    /// <summary>
    /// A polyline path is computed from the coordinates, and then elevation values at both endpoints and equally-spaced locations along the polyline are returned.
    /// </summary>
    /// <param name="key">A Bing Maps Key.</param>
    /// <param name="points">
    /// A set of coordinates on the Earth to use in elevation calculations.<br/>
    /// IEnumerable values can be float, double or Vector2.
    /// </param>
    /// <param name="samples">Specifies the number of equally-spaced elevation values to provide along a polyline path.</param>
    /// <param name="heights">Specifies which sea level model to use to calculate elevation.</param>
    /// <param name="output">Output format: JSON or XML</param>
    /// <returns>Instance of request.</returns>
    public static OnlineMapsBingMapsElevation GetElevationByPolyline(string key, IEnumerable points, int samples, Heights heights = Heights.sealevel, Output output = Output.json)
    {
        return new OnlineMapsBingMapsElevation(new PolylineParams(key, heights, output, points, samples));
    }

    /// <summary>
    /// The rectangular area defined by the four bounding box coordinates is divided into rows and columns.<br/>
    /// The edges of the bounding box account for two of the rows and two of the columns. <br/>
    /// Elevations are returned for the vertices of the grid created by the rows and columns.
    /// </summary>
    /// <param name="key">A Bing Maps Key.</param>
    /// <param name="leftLongitude">Left longitude</param>
    /// <param name="topLatitude">Top latitude</param>
    /// <param name="rightLongitude">Right longitude</param>
    /// <param name="bottomLatitude">Bottom latitude</param>
    /// <param name="rows">Number of rows to use to divide the bounding box area into a grid.</param>
    /// <param name="cols">Number of columns to use to divide the bounding box area into a grid.</param>
    /// <param name="heights">Specifies which sea level model to use to calculate elevation.</param>
    /// <param name="output">Output format: JSON or XML</param>
    /// <returns>Instance of request.</returns>
    public static OnlineMapsBingMapsElevation GetElevationByBounds(string key, double leftLongitude, double topLatitude, double rightLongitude, double bottomLatitude, int rows, int cols, Heights heights = Heights.sealevel, Output output = Output.json)
    {
        return new OnlineMapsBingMapsElevation(new BoundsParams(key, heights, output, leftLongitude, topLatitude, rightLongitude, bottomLatitude, rows, cols));
    }

    /// <summary>
    /// This request returns the offset in meters of the geoid model (heights=sealevel) from the ellipsoid model (heights=ellipsoid) at each location (difference = geoid_sealevel - ellipsoid_sealevel).
    /// </summary>
    /// <param name="key">A Bing Maps Key.</param>
    /// <param name="points">
    /// A set of coordinates on the Earth to use in elevation calculations.<br/>
    /// IEnumerable values can be float, double or Vector2.
    /// </param>
    /// <param name="output">Output format: JSON or XML</param>
    /// <returns>Instance of request.</returns>
    public static OnlineMapsBingMapsElevation GetSeaLevel(string key, IEnumerable points, Output output = Output.json)
    {
        return new OnlineMapsBingMapsElevation(new SeaLevelParams(key, Heights.sealevel, output, points));
    }

    /// <summary>
    /// Converts the response string to response object.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <param name="outputType">Format of response string (JSON or XML)</param>
    /// <returns>Response object</returns>
    public static OnlineMapsBingMapsElevationResult GetResult(string response, Output outputType)
    {
        try
        {
            if (outputType == Output.json) return OnlineMapsJSON.Deserialize<OnlineMapsBingMapsElevationResult>(response);
            OnlineMapsXML xml = OnlineMapsXML.Load(response);
            if (!xml.isNull)
            {
                OnlineMapsBingMapsElevationResult result = new OnlineMapsBingMapsElevationResult(xml);
                return result;
            }
        }
        catch {}
        return null;
    }

    private static int IndexOf(string str, params string[] parts)
    {
        int[] pi = new int[parts.Length];
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            for (int j = 0; j < parts.Length; j++)
            {
                if (parts[j][pi[j]] == c)
                {
                    pi[j]++;
                    if (pi[j] == parts[j].Length) return i + 1;
                }
                else pi[j] = 0;
            }
        }
        return -1;
    }

    /// <summary>
    /// Fast way get the elevation values without parsing JSON or XML.
    /// </summary>
    /// <param name="response">Response string</param>
    /// <param name="outputType">Format of response string (JSON or XML)</param>
    /// <param name="array">
    /// Reference to an array where the values will be stored.<br/>
    /// Supports one-dimensional and two-dimensional arrays.
    /// </param>
    /// <returns>TRUE - success, FALSE - failed.</returns>
    public static bool ParseElevationArray(string response, Output outputType, ref Array array)
    {
        if (array == null) throw new Exception("Array can not be null.");

        int rank = array.Rank;
        if (rank > 2) throw new Exception("Supports only one-dimensional and two-dimensional arrays.");

        int l1 = array.GetLength(0);
        int l2 = 1;
        if (rank == 2) l2 = array.GetLength(1);

        Type t = array.GetType();
        Type t2 = t.GetElementType();

        try
        {
            if (outputType == Output.json) return ParseJSONElevations(response, array, l1, l2, rank, t2);
            return ParseXMLElevations(response, array, l1, l2, rank, t2);
        }
        catch
        {
            return false;
        }
    }

    private static bool ParseJSONElevations(string response, Array array, int l1, int l2, int rank, Type t2)
    {
        int startIndex = IndexOf(response, "\"elevations\":[", "\"offsets\":[");
        if (startIndex == -1) return false;

        int index = 0;
        int v = 0;
        bool isNegative = false;
        bool smallArray = false;

        int x, y;

        for (int i = startIndex; i < response.Length; i++)
        {
            char c = response[i];
            if (c == ',')
            {
                x = index % l1;
                y = index / l2;
                if (isNegative) v = -v;

                if (rank == 1)
                {
                    if (y < l1) array.SetValue(Convert.ChangeType(v, t2), y);
                    else smallArray = true;
                }
                else
                {
                    if (x < l1 && y < l2) array.SetValue(Convert.ChangeType(v, t2), x, y);
                    else smallArray = true;
                }

                isNegative = false;
                v = 0;
                index++;
            }
            else if (c == '-') isNegative = true;
            else if (c > 47 && c < 58) v = v * 10 + (c - 48);
            else break;
        }

        x = index % l1;
        y = index / l2;

        if (isNegative) v = -v;

        if (rank == 1)
        {
            if (y < l1) array.SetValue(Convert.ChangeType(v, t2), y);
            else smallArray = true;
        }
        else
        {
            if (x < l1 && y < l2) array.SetValue(Convert.ChangeType(v, t2), x, y);
            else smallArray = true;
        }

        if (smallArray)
        {
            Debug.LogWarning("Invalid array. The response contains " + (index + 1) +" elements.");
            return false;
        }
        return true;
    }

    private static bool ParseXMLElevations(string response, Array array, int l1, int l2, int rank, Type t2)
    {
        int startIndex = IndexOf(response, "Elevations>", "Offsets>");
        if (startIndex == -1) return false;

        int index = 0;
        int v = 0;
        bool isNegative = false;

        int x, y;

        for (int i = startIndex; i < response.Length; i++)
        {
            char c = response[i];
            if (c == '/')
            {
                x = index % l1;
                y = index / l2;
                if (isNegative) v = -v;

                if (rank == 1)
                {
                    if (y < l2) array.SetValue(Convert.ChangeType(v, t2), y);
                }
                else
                {
                    if (x < l1 && y < l2) array.SetValue(Convert.ChangeType(v, t2), x, y);
                }

                isNegative = false;
                v = 0;
                index++;
            }
            else if (c == '-') isNegative = true;
            else if (c > 47 && c < 58) v = v * 10 + (c - 48);
            else if (c == 'E')
            {
                Debug.Log(response.Substring(startIndex, i - startIndex));
                //break;
                return false;
            }
        }

        if (index - 1 > l1 * l2)
        {
            Debug.LogWarning("Invalid array. The response contains " + (index - 1) + " elements.");
            return false;
        }
        return true;
    }

    private abstract class Params
    {
        protected string key;
        protected Heights heights;
        protected Output output;

        protected abstract string urlToken
        {
            get;
        }

        public Params(string key, Heights heights, Output output)
        {
            this.key = key;
            this.heights = heights;
            this.output = output;
        }

        public virtual void GenerateURL(StringBuilder builder)
        {
            if (string.IsNullOrEmpty(key)) key = OnlineMapsKeyManager.BingMaps();
            builder.Append("https://dev.virtualearth.net/REST/v1/Elevation/").Append(urlToken).Append("?key=").Append(key);
            if (heights == Heights.ellipsoid) builder.Append("&hts=ellipsoid");
            if (output == Output.xml) builder.Append("&output=xml");
        }
    }

    private class PointsParams : Params
    {
        protected IEnumerable points;

        protected override string urlToken
        {
            get { return "List"; }
        }

        public PointsParams(string key, Heights heights, Output output, IEnumerable points) : base(key, heights, output)
        {
            this.points = points;
        }

        public string EncodePoints()
        {
            double longitude = 0;

            long prevLatitude = 0;
            long prevLongitude = 0;

            StringBuilder result = new StringBuilder();
            int type = -1;
            int i = -1;

            foreach (object p in points)
            {
                i++;
                if (type == -1)
                {
                    if (p is double) type = 0;
                    else if (p is float) type = 1;
                    else if (p is Vector2) type = 2;
                    else throw new Exception("Unknown type of points. Must be IEnumerable<double>, IEnumerable<float> or IEnumerable<Vector2>.");
                }

                double latitude;
                if (type == 0 || type == 1)
                {
                    if (i % 2 == 1)
                    {
                        if (type == 0) latitude = (double) p;
                        else latitude = (float) p;
                    }
                    else
                    {
                        if (type == 0) longitude = (double)p;
                        else longitude = (float)p;
                        continue;
                    }
                }
                else
                {
                    Vector2 v = (Vector2) p;
                    longitude = v.x;
                    latitude = v.y;
                }

                long newLatitude = (long)Math.Round(latitude * 100000);
                long newLongitude = (long)Math.Round(longitude * 100000);

                long dy = newLatitude - prevLatitude;
                long dx = newLongitude - prevLongitude;
                prevLatitude = newLatitude;
                prevLongitude = newLongitude;

                dy = (dy << 1) ^ (dy >> 31);
                dx = (dx << 1) ^ (dx >> 31);

                long index = (dy + dx) * (dy + dx + 1) / 2 + dy;

                while (index > 0)
                {
                    long rem = index & 31;
                    index = (index - rem) / 32;
                    if (index > 0) rem += 32;
                    result.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-"[(int)rem]);
                }
            }

            return result.ToString();
        }

        public override void GenerateURL(StringBuilder builder)
        {
            base.GenerateURL(builder);

            builder.Append("&pts=").Append(EncodePoints());
        }
    }

    private class PolylineParams : PointsParams
    {
        protected int samples;

        protected override string urlToken
        {
            get { return "Polyline"; }
        }

        public PolylineParams(string key, Heights heights, Output output, IEnumerable points, int samples) : base(key, heights, output, points)
        {
            this.samples = samples;
        }

        public override void GenerateURL(StringBuilder builder)
        {
            base.GenerateURL(builder);

            builder.Append("&samp=").Append(samples);
        }
    }

    private class BoundsParams : Params
    {
        private double leftLongitude;
        private double topLatitude;
        private double rightLongitude;
        private double bottomLatitude;
        private int rows;
        private int cols;

        protected override string urlToken
        {
            get { return "Bounds"; }
        }

        public BoundsParams(string key, Heights heights, Output output, double leftLongitude, double topLatitude, double rightLongitude, double bottomLatitude, int rows, int cols) : base(key, heights, output)
        {
            this.leftLongitude = leftLongitude;
            this.topLatitude = topLatitude;
            this.rightLongitude = rightLongitude;
            this.bottomLatitude = bottomLatitude;
            this.rows = rows;
            this.cols = cols;

            if (rows < 2) throw new Exception("Rows must be >= 2.");
            if (cols < 2) throw new Exception("Cols must be >= 2.");
            if (rows * cols > 1024) throw new Exception("The number of rows and columns can define a maximum of 1024 locations (rows * cols <= 1024).");
        }

        public override void GenerateURL(StringBuilder builder)
        {
            base.GenerateURL(builder);

            builder.Append("&bounds=")
                .Append(bottomLatitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(leftLongitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(topLatitude.ToString(OnlineMapsUtils.numberFormat)).Append(",")
                .Append(rightLongitude.ToString(OnlineMapsUtils.numberFormat));
            builder.Append("&rows=").Append(rows).Append("&cols=").Append(cols);
        }
    }

    private class SeaLevelParams : PointsParams
    {
        protected override string urlToken
        {
            get { return "SeaLevel"; }
        }

        public SeaLevelParams(string key, Heights heights, Output output, IEnumerable points) : base(key, heights, output, points)
        {

        }
    }

    public enum Heights
    {
        sealevel,
        ellipsoid
    }

    public enum Output
    {
        xml,
        json
    }
}