/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LPoints = System.Collections.Generic.List<OnlineMapsVectorTile.LPoint>;

/// <summary>
/// Vector tile. This class is not used in the current version and was created for future versions.
/// </summary>
public abstract class OnlineMapsVectorTile : OnlineMapsTile
{
    private Dictionary<string, byte[]> layers = new Dictionary<string, byte[]>();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="x">Tile X</param>
    /// <param name="y">Tile Y</param>
    /// <param name="zoom">Tile zoom</param>
    /// <param name="map">Reference to the map</param>
    /// <param name="isMapTile">Will the tile be displayed on the map.</param>
    public OnlineMapsVectorTile(int x, int y, int zoom, OnlineMaps map, bool isMapTile = true) : base(x, y, zoom, map, isMapTile)
    {
    }

    private static List<LPoints> ClipPoints(List<LPoints> geoms, long extent, uint bufferSize)
    {
        List<LPoints> retVal = new List<LPoints>();

        foreach (var geomPart in geoms)
        {
            LPoints outGeom = new LPoints();
            foreach (var geom in geomPart)
            {
                if (geom.x < 0L - bufferSize || geom.y < 0L - bufferSize ||
                    geom.x > extent + bufferSize || geom.y > extent + bufferSize)
                {
                    continue;
                }
                outGeom.Add(geom);
            }

            if (outGeom.Count > 0) retVal.Add(outGeom);
        }

        return retVal;
    }

    private static List<LPoints> ClipGeometries(List<LPoints> geoms, GeomType geomType, long extent, uint bufferSize)
    {
        bool closed = geomType != GeomType.LINESTRING;

        List<LPoints> clip = new List<LPoints>
        {
            new LPoints
            {
                new LPoint(0L - bufferSize, 0L - bufferSize),
                new LPoint(extent + bufferSize, 0L - bufferSize),
                new LPoint(extent + bufferSize, extent + bufferSize),
                new LPoint(0L - bufferSize, extent + bufferSize)
            }
        };

        List<LPoints> subjects = geoms.Select(g => new LPoints(g)).ToList();

        OnlineMapsMapboxClipper.Clipper c = new OnlineMapsMapboxClipper.Clipper();
        c.AddPaths(subjects, OnlineMapsMapboxClipper.PolyType.ptSubject, closed);
        c.AddPaths(clip, OnlineMapsMapboxClipper.PolyType.ptClip, true);

        List<LPoints> solution = new List<LPoints>();

        bool succeeded;
        if (geomType == GeomType.LINESTRING)
        {
            OnlineMapsMapboxClipper.PolyTree lineSolution = new OnlineMapsMapboxClipper.PolyTree();
            succeeded = c.Execute(OnlineMapsMapboxClipper.ClipType.ctIntersection, lineSolution, OnlineMapsMapboxClipper.PolyFillType.pftNonZero, OnlineMapsMapboxClipper.PolyFillType.pftNonZero);
            if (succeeded) solution = OnlineMapsMapboxClipper.Clipper.PolyTreeToPaths(lineSolution);
        }
        else
        {
            succeeded = c.Execute(OnlineMapsMapboxClipper.ClipType.ctIntersection, solution, OnlineMapsMapboxClipper.PolyFillType.pftNonZero, OnlineMapsMapboxClipper.PolyFillType.pftNonZero);
        }

        if (!succeeded) return geoms;

        List<LPoints> retVal = new List<LPoints>();
        foreach (LPoints part in solution)
        {
            LPoints p = new LPoints(part);
            if (geomType == GeomType.POLYGON && p[0] == p[p.Count - 1]) p.Insert(0, p[p.Count - 1]);
            retVal.Add(p);
        }

        return retVal;
    }

    public static Feature GetFeature(Layer layer, int index)
    {
        byte[] data = layer.featuresData[index];
        OnlineMapsPBFReader reader = new OnlineMapsPBFReader(data);
        Feature feature = new Feature(layer);
        while (reader.NextByte())
        {
            switch ((FeatureType)reader.tag)
            {
                case FeatureType.Id:
                    feature.id = (ulong)reader.Varint();
                    break;
                case FeatureType.Tags:
                    List<int> tags = reader.GetPackedUnit32().Select(t => (int)t).ToList();
                    feature.tags = tags;
                    break;
                case FeatureType.Type:
                    int geomType = (int)reader.Varint();
                    feature.geometryType = (GeomType)geomType;
                    break;
                case FeatureType.Geometry:
                    feature.geometryCommands = reader.GetPackedUnit32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return feature;
    }

    public static List<LPoints> GetGeometry(GeomType geomType, List<uint> geometryCommands)
    {
        List<LPoints> geomOut = new List<LPoints>();
        LPoints geomTmp = new LPoints();
        long cursorX = 0;
        long cursorY = 0;

        for (int i = 0; i < geometryCommands.Count; i++)
        {
            uint g = geometryCommands[i];
            Commands cmd = (Commands)(g & 0x7);
            uint cmdCount = g >> 3;

            if (cmd == Commands.LineTo)
            {
                for (int j = 0; j < cmdCount; j++)
                {
                    long x = geometryCommands[i + 1];
                    long y = geometryCommands[i + 2];
                    cursorX += (x >> 1) ^ -(x & 1);
                    cursorY += (y >> 1) ^ -(y & 1);
                    i += 2;

                    if (cmd == Commands.MoveTo && geomTmp.Count > 0)
                    {
                        geomOut.Add(geomTmp.ToList());
                        geomTmp.Clear();
                    }

                    geomTmp.Add(new LPoint(cursorX, cursorY));
                }
            }
            else if (cmd == Commands.MoveTo)
            {
                long x = geometryCommands[i + 1];
                long y = geometryCommands[i + 2];
                cursorX += (x >> 1) ^ -(x & 1);
                cursorY += (y >> 1) ^ -(y & 1);
                i += 2;

                if (geomTmp.Count > 0)
                {
                    geomOut.Add(geomTmp.ToList());
                    geomTmp.Clear();
                }

                geomTmp.Add(new LPoint(cursorX, cursorY));
            }
            else if (cmd == Commands.ClosePath && geomType == GeomType.POLYGON && geomTmp.Count > 0) geomTmp.Add(geomTmp[0]);
        }

        if (geomTmp.Count > 0) geomOut.Add(geomTmp.ToList());
        return geomOut;
    }

    public Layer GetLayer(string name)
    {
        byte[] data;
        if (layers.TryGetValue(name, out data)) return GetLayer(data);
        return null;
    }

    private static Layer GetLayer(byte[] data)
    {
        Layer layer = new Layer();
        OnlineMapsPBFReader layerReader = new OnlineMapsPBFReader(data);
        while (layerReader.NextByte())
        {
            switch ((LayerType)layerReader.tag)
            {
                case LayerType.Version:
                    layer.version = (ulong)layerReader.Varint();
                    break;
                case LayerType.Name:
                    layer.name = layerReader.GetString((ulong)layerReader.Varint());
                    break;
                case LayerType.Extent:
                    layer.extent = (ulong)layerReader.Varint();
                    break;
                case LayerType.Keys:
                    byte[] keyBuffer = layerReader.View();
                    string key = Encoding.UTF8.GetString(keyBuffer, 0, keyBuffer.Length);
                    layer.keys.Add(key);
                    break;
                case LayerType.Values:
                    byte[] valueBuffer = layerReader.View();
                    OnlineMapsPBFReader valReader = new OnlineMapsPBFReader(valueBuffer);
                    while (valReader.NextByte())
                    {
                        switch ((ValueType)valReader.tag)
                        {
                            case ValueType.String:
                                byte[] stringBuffer = valReader.View();
                                string value = Encoding.UTF8.GetString(stringBuffer, 0, stringBuffer.Length);
                                layer.values.Add(value);
                                break;
                            case ValueType.Float:
                                layer.values.Add(valReader.GetFloat());
                                break;
                            case ValueType.Double:
                                layer.values.Add(valReader.GetDouble());
                                break;
                            case ValueType.Int:
                                layer.values.Add(valReader.Varint());
                                break;
                            case ValueType.UInt:
                            case ValueType.SInt:
                                layer.values.Add(valReader.Varint());
                                break;
                            case ValueType.Bool:
                                layer.values.Add(valReader.Varint() == 1);
                                break;
                        }
                    }
                    break;
                case LayerType.Features:
                    layer.featuresData.Add(layerReader.View());
                    break;
                default:
                    layerReader.Skip();
                    break;
            }
        }

        return layer;
    }

    public List<string> GetLayerNames()
    {
        return layers.Keys.ToList();
    }

    public void Read(byte[] data)
    {
        if (data == null) throw new Exception("Tile data cannot be null");
        if (data.Length < 2) throw new Exception("Tile data cannot be empty");
        if (data[0] == 0x1f && data[1] == 0x8b) return;

        OnlineMapsPBFReader reader = new OnlineMapsPBFReader(data);
        while (reader.NextByte())
        {
            if (reader.tag != (int) TileType.Layers)
            {
                reader.Skip();
                continue;
            }

            string name = null;
            byte[] layerMessage = reader.View();
            OnlineMapsPBFReader layerView = new OnlineMapsPBFReader(layerMessage);
            while (layerView.NextByte())
            {
                if (layerView.tag == (int) LayerType.Name) name = layerView.GetString((ulong) layerView.Varint());
                else layerView.Skip();
            }

            layers.Add(name, layerMessage);
        }
    }

    public class Layer
    {
        public ulong extent;
        public List<byte[]> featuresData;

        public List<string> keys;
        public string name;

        public List<object> values;
        public ulong version;

        public int featureCount
        {
            get { return featuresData.Count; }
        }

        public Layer()
        {
            featuresData = new List<byte[]>();
            keys = new List<string>();
            values = new List<object>();
        }

        public Feature GetFeature(int index)
        {
            return OnlineMapsVectorTile.GetFeature(this, index);
        }
    }

    public class Feature
    {
        public List<uint> geometryCommands;

        public GeomType geometryType;
        public ulong id;
        public List<int> tags;

        private Layer layer;

        public Feature(Layer layer)
        {
            this.layer = layer;
            tags = new List<int>();
        }

        public List<LPoints> Geometry(uint? clipBuffer = null)
        {
            List<LPoints> geom = GetGeometry(geometryType, geometryCommands);
            if (clipBuffer.HasValue)
            {
                if (geometryType == GeomType.POINT) geom = ClipPoints(geom, (long)layer.extent, clipBuffer.Value);
                else geom = ClipGeometries(geom, geometryType, (long)layer.extent, clipBuffer.Value);
            }
            return geom;
        }

        public Dictionary<string, object> GetProperties()
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            for (int i = 0; i < tags.Count; i += 2)
            {
                properties.Add(layer.keys[tags[i]], layer.values[tags[i + 1]]);
            }
            return properties;
        }
    }

    public struct LPoint
    {
        public long x;
        public long y;

        public LPoint(long x, long y)
        {
            this.x = x;
            this.y = y;
        }

        public LPoint(LPoint p)
        {
            x = p.x;
            y = p.y;
        }

        public static bool operator ==(LPoint a, LPoint b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(LPoint a, LPoint b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static LPoint operator +(LPoint a, LPoint b)
        {
            return new LPoint(a.x + b.x, a.y + b.y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is LPoint)
            {
                LPoint a = (LPoint)obj;
                return x == a.x && y == a.y;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return x + ", " + y;
        }
    }

    private enum Commands
    {
        MoveTo = 1,
        LineTo = 2,
        ClosePath = 7
    }

    private enum TileType
    {
        Layers = 3
    }

    private enum LayerType
    {
        Version = 15,
        Name = 1,
        Features = 2,
        Keys = 3,
        Values = 4,
        Extent = 5
    }

    public enum GeomType
    {
        UNKNOWN = 0,
        POINT = 1,
        LINESTRING = 2,
        POLYGON = 3
    }

    private enum FeatureType
    {
        Id = 1,
        Tags = 2,
        Type = 3,
        Geometry = 4,
        Raster = 5
    }

    private enum ValueType
    {
        String = 1,
        Float = 2,
        Double = 3,
        Int = 4,
        UInt = 5,
        SInt = 6,
        Bool = 7
    }
}