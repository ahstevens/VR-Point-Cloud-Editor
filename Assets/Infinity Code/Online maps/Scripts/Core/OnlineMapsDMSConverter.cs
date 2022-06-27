/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Class for converting numeric degrees into deg / min / sec, and vice versa.
/// </summary>
public static class OnlineMapsDMSConverter
{
    /// <summary>
    /// Separator character to be used to separate degrees, minutes, seconds, and cardinal directions.
    /// </summary>
    public static string DMS_SEPARATOR = "";

    /// <summary>
    /// Converts numeric degrees to deg/min/sec (latitude: 2-digit degrees, suffixed with N/S; longitude: 3-digit degrees, suffixed with E/W).
    /// </summary>
    /// <param name="lng">Longitude degrees to be formatted as specified</param>
    /// <param name="lat">Latitude degrees to be formatted as specified</param>
    /// <param name="format">Return value as 'd', 'dm', 'dms' for deg, deg+min, deg+min+sec</param>
    /// <param name="dp">Number of decimal places to use – default 0 for dms, 2 for dm, 4 for d</param>
    /// <param name="separator">Latitude and longitude separator</param>
    /// <returns>Latitude and longitude degrees formatted as deg/min/secs according to specified format</returns>
    public static string CoordinatesToDMS(double lng, double lat, string format = "dms", int dp = -1, string separator = ", ")
    {
        return LatToDMS(lat, format, dp) + separator + LngToDMS(lng, format, dp);
    }

    /// <summary>
    /// Converts numeric degrees to deg/min/sec latitude (2-digit degrees, suffixed with N/S).
    /// </summary>
    /// <param name="lat">Degrees to be formatted as specified</param>
    /// <param name="format">Return value as 'd', 'dm', 'dms' for deg, deg+min, deg+min+sec</param>
    /// <param name="dp">Number of decimal places to use – default 0 for dms, 2 for dm, 4 for d</param>
    /// <returns>Degrees formatted as deg/min/secs according to specified format</returns>
    public static string LatToDMS(double lat, string format = "dms", int dp = -1)
    {
        string slat = ToDMS(lat, format, dp);
        return slat == null ? "–" : slat.Substring(1) + DMS_SEPARATOR + (lat < 0 ? "S" : "N");
    }

    /// <summary>
    /// Convert numeric degrees to deg/min/sec longitude (3-digit degrees, suffixed with E/W).
    /// </summary>
    /// <param name="lng">Degrees to be formatted as specified</param>
    /// <param name="format">Return value as 'd', 'dm', 'dms' for deg, deg+min, deg+min+sec</param>
    /// <param name="dp">Number of decimal places to use – default 0 for dms, 2 for dm, 4 for d</param>
    /// <returns>Degrees formatted as deg/min/secs according to specified format</returns>
    public static string LngToDMS(double lng, string format = "dms", int dp = -1)
    {
        string slng = ToDMS(lng, format, dp);
        return slng == null ? "–" : slng + DMS_SEPARATOR + (lng < 0 ? "W" : "E");
    }

    /// <summary>
    /// Parses string representing degrees/minutes/seconds into numeric degrees.
    /// </summary>
    /// <param name="dmsStr">Degrees or deg/min/sec in variety of formats</param>
    /// <param name="value">Degrees as decimal number</param>
    /// <returns>True - success, False - otherwise</returns>
    public static bool ParseDMS(string dmsStr, out double value)
    {
        if (double.TryParse(dmsStr, NumberStyles.AllowDecimalPoint, OnlineMapsUtils.numberFormat, out value)) return true;

        string dms = dmsStr.Trim();
        dms = Regex.Replace(dms, "^ -", "");
        dms = Regex.Replace(dms, "[NSEW]$", "", RegexOptions.IgnoreCase);

        if (string.IsNullOrEmpty(dms))
        {
            value = double.NaN;
            return false;
        }

        string[] dmsArr = dms.Split(new []{'°', '′', '″', ' '}, StringSplitOptions.RemoveEmptyEntries);
        if (dmsArr[dmsArr.Length - 1] == "") Array.Resize(ref dmsArr, dmsArr.Length - 1);

        switch (dmsArr.Length)
        {
            case 3:
                value = double.Parse(dmsArr[0], OnlineMapsUtils.numberFormat) + double.Parse(dmsArr[1], OnlineMapsUtils.numberFormat) / 60 +  double.Parse(dmsArr[2], OnlineMapsUtils.numberFormat) / 3600;
                break;
            case 2:
                value = double.Parse(dmsArr[0], OnlineMapsUtils.numberFormat) + double.Parse(dmsArr[1], OnlineMapsUtils.numberFormat) / 60;
                break;
            case 1:
                value = double.Parse(dmsArr[0], OnlineMapsUtils.numberFormat);
                break;
            default:
                value = double.NaN;
                return false;
        }
        if (Regex.IsMatch(dmsStr.Trim(), "^ -|[WS]$", RegexOptions.IgnoreCase)) value = -value;
        return true;
    }

    /// <summary>
    /// Parses string representing latitude and longitude degrees/minutes/seconds into numeric degrees.
    /// </summary>
    /// <param name="dmsStr">Degrees or deg/min/sec in variety of formats</param>
    /// <param name="lng">Longitude degrees as decimal number</param>
    /// <param name="lat">Latitude degrees as decimal number</param>
    /// <param name="separator">Latitude and longitude separator</param>
    /// <returns>True - success, False - otherwise</returns>
    public static bool ParseDMS(string dmsStr, out double lng, out double lat, string separator = ", ")
    {
        string[] dmsArr = dmsStr.Split(new [] {separator}, StringSplitOptions.RemoveEmptyEntries);
        lng = 0;
        lat = 0;
        if (dmsArr.Length != 2) return false;

        if (!ParseDMS(dmsArr[0], out lat)) return false;
        return ParseDMS(dmsArr[1], out lng);
    }

    private static string ToDMS(double deg, string format = null, int dp = -1)
    {
        if (double.IsNaN(deg)) return null;

        if (format == null) format = "dms";

        if (dp == -1)
        {
            switch (format)
            {
                case "d": case "deg": dp = 4; break;
                case "dm": case "deg+min": dp = 2; break;
                case "dms": case "deg+min+sec": dp = 0; break;
                default: format = "dms"; dp = 0; break;
            }
        }

        deg = Math.Abs(deg);

        string dms, sd, sm;
        double d, m;
        switch (format)
        {
            default:
                d = Math.Round(deg, dp);
                sd = d.ToString(OnlineMapsUtils.numberFormat);
                if (d < 100) sd = "0" + sd;
                if (d < 10) sd = "0" + sd;
                dms = sd + "°";
                break;
            case "dm":
            case "deg+min":
                double min = Math.Round(deg * 60, dp);
                d = Math.Floor(min / 60);
                sd = d.ToString(OnlineMapsUtils.numberFormat);
                m = Math.Round(min % 60, dp);
                sm = m.ToString(OnlineMapsUtils.numberFormat);
                if (d < 100) sd = "0" + sd;
                if (d < 10) sd = "0" + sd;
                if (m < 10) sm = "0" + sm;
                dms = sd + "°" + DMS_SEPARATOR + sm + "′";
                break;
            case "dms":
            case "deg+min+sec":
                var sec = Math.Round(deg * 3600,  dp);
                d = Math.Floor(sec / 3600);
                sd = d.ToString(OnlineMapsUtils.numberFormat);
                m = Math.Floor(sec / 60) % 60;
                sm = m.ToString(OnlineMapsUtils.numberFormat);
                double s = Math.Round(sec % 60, dp);
                string ss = s.ToString(OnlineMapsUtils.numberFormat);
                if (d < 100) sd = "0" + sd;
                if (d < 10) sd = "0" + sd;
                if (m < 10) sm = "0" + sm;
                if (s < 10) ss = "0" + ss;
                dms = sd + "°" + DMS_SEPARATOR + sm + "′" + DMS_SEPARATOR + ss + "″";
                break;
        }

        return dms;
    }
}