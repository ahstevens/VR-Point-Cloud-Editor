/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OnlineMapsMapboxStyle
{
    public int version;
    public string name;
    public Metadata metadata;
    public double[] center;
    public double zoom;
    public double bearing;
    public double pitch;
    public object light;
    public object sources;
    public string sprite;
    public string glyphs;
    public object transition;
    public Layer[] layers;
    public string created;
    public string id;
    public string modified;
    public string owner;
    public string visibility;
    public bool draft;

    public Layer this[string layerName]
    {
        get
        {
            return layers.FirstOrDefault(l => l.id == layerName);
        }
    }

    public static Color32 HSL2RGB(float h, float s, float l)
    {
        float r = l;
        float g = l;
        float b = l;

        float v = l <= 0.5f ? l * (1.0f + s) : l + s - l * s;

        if (v > 0)
        {
            float m = l + l - v;
            float sv = (v - m) / v;
            h *= 6.0f;
            int sextant = (int)h;
            float fract = h - sextant;
            float vsf = v * sv * fract;
            float mid1 = m + vsf;
            float mid2 = v - vsf;

            switch (sextant)
            {
                case 0:
                    r = v;
                    g = mid1;
                    b = m;
                    break;
                case 1:
                    r = mid2;
                    g = v;
                    b = m;
                    break;
                case 2:
                    r = m;
                    g = v;
                    b = mid1;
                    break;
                case 3:
                    r = m;
                    g = mid2;
                    b = v;
                    break;
                case 4:
                    r = mid1;
                    g = m;
                    b = v;
                    break;
                case 5:
                    r = v;
                    g = m;
                    b = mid2;
                    break;
            }
        }

        return new Color(r, g, b, 1);
    }

    public Layer TryGetSubstyle(string layerName, int index)
    {
        Layer[] ls = layers.Where(l => l.id.Contains(layerName)).ToArray();
        if (ls.Length == 0) return null;

        if (index >= ls.Length) return ls[ls.Length - 1];
        return ls[index];
    }

    public class Metadata
    {
        [OnlineMapsJSON.Alias("mapbox:autocomposite")]
        public string autocomposite;

        [OnlineMapsJSON.Alias("mapbox:type")]
        public string type;

        [OnlineMapsJSON.Alias("mapbox:origin")]
        public string origin;

        [OnlineMapsJSON.Alias("mapbox:groups")]
        public object groups;
    }

    public class Layer
    {
        public string id;
        public string type;
        public object layout;
        public object metadata;
        public string source;
        public string[] filter;

        public object paint;

        [OnlineMapsJSON.Alias("source-layer")]
        public string sourceLayer;

        public int maxzoom;
        public int minzoom;

        public Paint GetPaint(float zoom)
        {
            if (type == "fill") return new Paint.Fill(paint, zoom);
            if (type == "background") return new Paint.Background(paint, zoom);
            Debug.Log("---------------------------------------> " + type);
            return null;
        }
    }

    public abstract class Paint
    {
        protected Color GetColor(object value, float zoom)
        {
            if (value is string) return GetColorFromString((string)value);
            if (value is IDictionary)
            {
                Stop<string> s1, s2;
                float t;
                int count = Stop<string>.GetActiveStops(value, zoom, out s1, out s2, out t);
                if (count == 1) return GetColorFromString(s1.value);
                return Color.Lerp(GetColorFromString(s1.value), GetColorFromString(s2.value), t);
            }

            Debug.Log("GetColor error");
            return Color.red;
        }

        private Color GetColorFromString(string value)
        {
            value = value.ToLower();
            if (value.StartsWith("hsl") || value.StartsWith("RGB"))
            {
                int v = 0;
                int compIndex = 0;
                int decimalV = 0;
                float[] comps = new float[4];
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    if (c == ',' || c == ')')
                    {
                        if (decimalV > 0) comps[compIndex] = v / (float)decimalV;
                        else comps[compIndex] = v;
                        v = decimalV = 0;
                        compIndex++;
                    }
                    else if (c == '.') decimalV = 1;
                    else if (c >= '0' && c <= '9')
                    {
                        v = v * 10 + c - '0';
                        decimalV *= 10;
                    }
                }
                Color color;
                if (value.StartsWith("hsl")) color = HSL2RGB(comps[0] / 360, comps[1] / 100, comps[2] / 100);
                else color = new Color(comps[0], comps[1], comps[2]);

                if (compIndex == 5) color.a = comps[3];

                return color;
            }

            Debug.Log("GetColorFromString error");
            return Color.red;
        }

        protected bool GetBool(object value, float zoom)
        {
            if (value is bool) return (bool)value;
            if (value is IDictionary)
            {
                Stop<bool> s1, s2;
                float t;
                int count = Stop<bool>.GetActiveStops(value, zoom, out s1, out s2, out t);
                if (count == 1) return s1.value;
                return s1.value;
            }
            return false;
        }

        protected float GetFloat(object value, float zoom)
        {
            if (value is double) return (float)(double)value;
            if (value is IDictionary)
            {
                Stop<float> s1, s2;
                float t;
                int count = Stop<float>.GetActiveStops(value, zoom, out s1, out s2, out t);
                if (count == 1) return s1.value;
                return Mathf.Lerp(s1.value, s2.value, t);
            }
            return 1;
        }

        private struct Stop<T>
        {
            public float zoom;
            public T value;

            private Stop(object obj)
            {
                List<object> v = obj as List<object>;
                if (v[0] is long) zoom = (long)v[0];
                else if (v[0] is double) zoom = (float) (double) v[0];
                else zoom = 0;
                
                Type type = v[1].GetType();
                if (type == typeof(string)) value = (T)Convert.ChangeType((string)v[1], typeof(T));
                else if (type == typeof(double)) value = (T)Convert.ChangeType((double)v[1], typeof(T));
                else if (type == typeof(long)) value = (T)Convert.ChangeType((long)v[1], typeof(T));
                else if (type == typeof(bool)) value = (T) Convert.ChangeType((bool) v[1], typeof(T));
                else value = default(T);
            }

            private static Stop<T>[] GetFromPaint(object obj)
            {
                List<object> stops = (obj as IDictionary)["stops"] as List<object>;
                Stop<T>[] result = new Stop<T>[stops.Count];
                for (int i = 0; i < stops.Count; i++) result[i] = new Stop<T>(stops[i]);
                return result;
            }

            public static int GetActiveStops(object obj, float zoom, out Stop<T> s1, out Stop<T> s2, out float t)
            {
                return GetActiveStops(GetFromPaint(obj), zoom, out s1, out s2, out t);
            }

            private static int GetActiveStops(Stop<T>[] stops, float zoom, out Stop<T> s1, out Stop<T> s2, out float t)
            {
                s2 = default(Stop<T>);
                t = 0;
                for (int i = 0; i < stops.Length; i++)
                {
                    if (zoom - stops[i].zoom < float.Epsilon)
                    {
                        if (i == 0)
                        {
                            s1 = stops[i];
                            return 1;
                        }
                        s1 = stops[i - 1];
                        s2 = stops[i];
                        float range = s2.zoom - s1.zoom;
                        t = (zoom - s1.zoom) / range;
                        return 2;
                    }
                }
                s1 = stops[stops.Length - 1];
                return 1;
            }
        }

        public class Fill : Paint
        {
            public Color? fillColor;
            public float? fillOpacity;
            public bool? fillAntialias;

            public Fill(object paint, float zoom)
            {
                Dictionary<string, object> dPaint = paint as Dictionary<string, object>;
                foreach (KeyValuePair<string, object> pair in dPaint)
                {
                    if (pair.Key == "fill-color") fillColor = GetColor(pair.Value, zoom);
                    else if (pair.Key == "fill-opacity") fillOpacity = GetFloat(pair.Value, zoom);
                    else if (pair.Key == "fill-antialias") fillAntialias = GetBool(pair.Value, zoom);
                    else if (pair.Key == "fill-outline-color")
                    {
                        // TODO Implement this
                    }
                    else Debug.Log("Paint.Fill: " + pair.Key);
                }
            }
        }

        public class Background : Paint
        {
            public Color? backgroundColor;

            public Background(object paint, float zoom)
            {
                Dictionary<string, object> dPaint = paint as Dictionary<string, object>;
                foreach (KeyValuePair<string, object> pair in dPaint)
                {
                    if (pair.Key == "background-color") backgroundColor = GetColor(pair.Value, zoom);
                    else Debug.Log("Paint.Background: " + pair.Key);
                }
            }
        }
    }
}