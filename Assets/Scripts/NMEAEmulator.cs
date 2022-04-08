using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class NMEAEmulator : MonoBehaviour
{
    UdpClient client;

    public int port = 9900;

    public bool sendObjectPosition;
    public bool sendObjectHeading;

    public int latDegrees = 29;
    public int latMinutes = 56;
    public int latSeconds = 20;
    public int lonDegrees = 90;
    public int lonMinutes = 03;
    public int lonSeconds = 27;
    public int positionUpdateRateHz = 50;

    public float heading = 0f;
    public int headingUpdateRateHz = 50;

    float positionUpdateDelta = 0f;
    float headingUpdateDelta = 0f;

    GEOReference georef;

    // Start is called before the first frame update
    void Start()
    {
        client = new UdpClient();

        client.Connect(new IPEndPoint(IPAddress.Parse("192.168.8.255"), port));
    }

    // Update is called once per frame
    void Update()
    {
        if (georef == null)
            georef = (GEOReference)FindObjectOfType(typeof(GEOReference));

        // POSITION
        float positionUpdateTime = 1f / (float)positionUpdateRateHz;

        positionUpdateDelta += Time.deltaTime;

        if (positionUpdateDelta >= positionUpdateTime)
        {
            if (georef != null && sendObjectPosition)
            {
                double lat, lon;
                ToLatLon(georef.realWorldX + (double)this.transform.position.x, georef.realWorldZ + (double)this.transform.position.z, "15N", out lat, out lon);
                lon = Math.Abs(lon);
                
                SendPosition(lat, lon);
            }
            else
            {
                SendPosition(latDegrees, latMinutes, latSeconds, lonDegrees, lonMinutes, lonSeconds);
            }

            positionUpdateDelta = positionUpdateDelta % positionUpdateTime;
        }

        // HEADING
        float headingUpdateTime = 1f / (float)headingUpdateRateHz;

        headingUpdateDelta += Time.deltaTime;

        if (headingUpdateDelta >= headingUpdateTime)
        {
            if (sendObjectHeading)
                SendHeading(this.transform.eulerAngles.y);
            else
                SendHeading(heading);

            headingUpdateDelta = headingUpdateDelta % headingUpdateTime;
        }
    }

    void SendPosition(int latDeg, int latMin, float latSec, int lonDeg, int lonMin, float lonSec)
    {
        latDeg = Mathf.Clamp(latDeg, 0, 90);
        latMin = Mathf.Clamp(latMin, 0, 59);
        var latMinuteDec = Mathf.Clamp(latSec, 0, 59.999f) / 60f;

        lonDeg = Mathf.Clamp(lonDeg, 0, 180);
        lonMin = Mathf.Clamp(lonMin, 0, 59);
        var lonMinuteDec = Mathf.Clamp(lonSec, 0, 59.999f) / 60f;

        char[] decFilter = { '0', '.' };

        //var lat = "2956.3333";
        var lat = latDeg.ToString("D2") + latMin.ToString("D2") + "." + latMinuteDec.ToString("F5").TrimStart(decFilter);

        //var lon = "09003.45";
        var lon = lonDeg.ToString("D3") + lonMin.ToString("D2") + "." + lonMinuteDec.ToString("F5").TrimStart(decFilter);

        var sentence = "GPGGA,092750.000," + lat + ",N," + lon + ",W,1,8,1.03,61.7,M,55.2,M,,";

        PrepareAndSendCommand(sentence);
    }

    void SendPosition(double lat, double lon)
    {
        SendPosition(
            (int)lat,
            (int)((lat - (int)lat) * 60),
            (float)((((lat - (int)lat) * 60) - (int)((lat - (int)lat) * 60)) * 60.0),
            (int)lon,
            (int)((lon - (int)lon) * 60),
            (float)((((lon - (int)lon) * 60) - (int)((lon - (int)lon) * 60)) * 60.0)
            );
    }

    void SendHeading(float h)
    {
        heading = Mathf.Clamp(heading, 0, 359.9f);

        var sentence = "GPHDT," + h.ToString("000.0") + ",T";

        PrepareAndSendCommand(sentence);
    }

    void PrepareAndSendCommand(string sentence)
    {
        sentence = "$" + sentence + GetChecksumString(sentence);

        Debug.Log(sentence);

        Byte[] sendBytes = Encoding.ASCII.GetBytes(sentence);

        client.Send(sendBytes, sendBytes.Length);
    }

    // 
    string GetChecksumString(string sentence)
    {
        Byte[] s = Encoding.ASCII.GetBytes(sentence);

        Byte c = 0;

        foreach (Byte b in s)
        {
            c ^= b;
        }
        
        return "*" + c.ToString("X2");
    }

    public static void ToLatLon(double utmX, double utmY, string utmZone, out double latitude, out double longitude)
    {
        bool isNorthHemisphere = utmZone.EndsWith("N");

        var diflat = -0.00066286966871111111111111111111111111;
        var diflon = -0.0003868060578;

        var zone = int.Parse(utmZone.Remove(utmZone.Length - 1));
        var c_sa = 6378137.000000;
        var c_sb = 6356752.314245;
        var e2 = Math.Pow((Math.Pow(c_sa, 2) - Math.Pow(c_sb, 2)), 0.5) / c_sb;
        var e2cuadrada = Math.Pow(e2, 2);
        var c = Math.Pow(c_sa, 2) / c_sb;
        var x = utmX - 500000;
        var y = isNorthHemisphere ? utmY : utmY - 10000000;

        var s = ((zone * 6.0) - 183.0);
        var lat = y / (c_sa * 0.9996);
        var v = (c / Math.Pow(1 + (e2cuadrada * Math.Pow(Math.Cos(lat), 2)), 0.5)) * 0.9996;
        var a = x / v;
        var a1 = Math.Sin(2 * lat);
        var a2 = a1 * Math.Pow((Math.Cos(lat)), 2);
        var j2 = lat + (a1 / 2.0);
        var j4 = ((3 * j2) + a2) / 4.0;
        var j6 = ((5 * j4) + Math.Pow(a2 * (Math.Cos(lat)), 2)) / 3.0;
        var alfa = (3.0 / 4.0) * e2cuadrada;
        var beta = (5.0 / 3.0) * Math.Pow(alfa, 2);
        var gama = (35.0 / 27.0) * Math.Pow(alfa, 3);
        var bm = 0.9996 * c * (lat - alfa * j2 + beta * j4 - gama * j6);
        var b = (y - bm) / v;
        var epsi = ((e2cuadrada * Math.Pow(a, 2)) / 2.0) * Math.Pow((Math.Cos(lat)), 2);
        var eps = a * (1 - (epsi / 3.0));
        var nab = (b * (1 - epsi)) + lat;
        var senoheps = (Math.Exp(eps) - Math.Exp(-eps)) / 2.0;
        var delt = Math.Atan(senoheps / (Math.Cos(nab)));
        var tao = Math.Atan(Math.Cos(delt) * Math.Tan(nab));

        longitude = ((delt * (180.0 / Math.PI)) + s) + diflon;
        latitude = ((lat + (1 + e2cuadrada * Math.Pow(Math.Cos(lat), 2) - (3.0 / 2.0) * e2cuadrada * Math.Sin(lat) * Math.Cos(lat) * (tao - lat)) * (tao - lat)) * (180.0 / Math.PI)) + diflat;
    }
}
