/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This class is used to request to Open Street Map Overpass API.<br/>
/// You can create a new instance using OnlineMapsOSMAPIQuery.Find.<br/>
/// Open Street Map Overpass API documentation: http://wiki.openstreetmap.org/wiki/Overpass_API/Language_Guide <br/>
/// You can test your queries using: http://overpass-turbo.eu/ 
/// </summary>
public class OnlineMapsOSMAPIQuery: OnlineMapsTextWebService
{
    private static string osmURL = "https://overpass-api.de/api/interpreter?data=";

    private OnlineMapsOSMAPIQuery(string data)
    {
        _status = OnlineMapsQueryStatus.downloading;
        string url = osmURL + OnlineMapsWWW.EscapeURL(data);
        www = new OnlineMapsWWW(url);
        www.OnComplete += OnRequestComplete;
    }

    /// <summary>
    /// Query the Open Street Map Overpass API.
    /// </summary>
    /// <param name="data">Overpass QL request</param>
    /// <returns>Instance of the query</returns>
    public static OnlineMapsOSMAPIQuery Find(string data)
    {
        return new OnlineMapsOSMAPIQuery(data);
    }

    public static void InitOSMServer(OnlineMapsOSMOverpassServer server)
    {
        if (server == OnlineMapsOSMOverpassServer.main) osmURL = "https://overpass-api.de/api/interpreter?data=";
        else if (server == OnlineMapsOSMOverpassServer.main2) osmURL = "https://z.overpass-api.de/api/interpreter?data=";
        else if (server == OnlineMapsOSMOverpassServer.french) osmURL = "https://overpass.openstreetmap.fr/api/interpreter?data=";
        else if (server == OnlineMapsOSMOverpassServer.taiwan) osmURL = "https://overpass.nchc.org.tw/api/interpreter?data=";
        else if (server == OnlineMapsOSMOverpassServer.kumiSystems) osmURL = "https://overpass.kumi.systems/api/interpreter?data=";
    }

    /// <summary>
    /// Get data from the response Open Street Map Overpass API.
    /// </summary>
    /// <param name="response">Response from Overpass API</param>
    /// <param name="nodes">List of nodes</param>
    /// <param name="ways">List of ways</param>
    /// <param name="relations">List of relations</param>
    public static void ParseOSMResponse(string response, out List<OnlineMapsOSMNode> nodes, out List<OnlineMapsOSMWay> ways, out List<OnlineMapsOSMRelation> relations)
    {
        List<OnlineMapsOSMArea> areas;
        ParseOSMResponse(response, out nodes, out ways, out relations, out areas);
    }

    /// <summary>
    /// Get data from the response Open Street Map Overpass API.
    /// </summary>
    /// <param name="response">Response from Overpass API</param>
    /// <param name="nodes">List of nodes</param>
    /// <param name="ways">List of ways</param>
    /// <param name="relations">List of relations</param>
    /// <param name="areas">List of areas</param>
    public static void ParseOSMResponse(string response, out List<OnlineMapsOSMNode> nodes, out List<OnlineMapsOSMWay> ways, out List<OnlineMapsOSMRelation> relations, out List<OnlineMapsOSMArea> areas)
    {
        nodes = new List<OnlineMapsOSMNode>();
        ways = new List<OnlineMapsOSMWay>();
        relations = new List<OnlineMapsOSMRelation>();
        areas = new List<OnlineMapsOSMArea>();

        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response);

            foreach (OnlineMapsXML node in xml)
            {
                if (node.name == "node") nodes.Add(new OnlineMapsOSMNode(node));
                else if (node.name == "way") ways.Add(new OnlineMapsOSMWay(node));
                else if (node.name == "relation") relations.Add(new OnlineMapsOSMRelation(node));
                else if (node.name == "area") areas.Add(new OnlineMapsOSMArea(node));
            }
        }
        catch
        {
            Debug.Log(response);
        }
    }

    /// <summary>
    /// Get data from the response Open Street Map Overpass API.
    /// </summary>
    /// <param name="response">Response from Overpass API</param>
    /// <param name="nodes">Dictionary of nodes</param>
    /// <param name="ways">List of ways</param>
    /// <param name="relations">List of relations</param>
    public static void ParseOSMResponse(string response, out Dictionary<string, OnlineMapsOSMNode> nodes, out List<OnlineMapsOSMWay> ways, out List<OnlineMapsOSMRelation> relations)
    {
        List<OnlineMapsOSMArea> areas;
        ParseOSMResponse(response, out nodes, out ways, out relations, out areas);
    }

    /// <summary>
    /// Get data from the response Open Street Map Overpass API.
    /// </summary>
    /// <param name="response">Response from Overpass API</param>
    /// <param name="nodes">Dictionary of nodes</param>
    /// <param name="ways">List of ways</param>
    /// <param name="relations">List of relations</param>
    /// <param name="areas">List of areas</param>
    public static void ParseOSMResponse(string response, out Dictionary<string, OnlineMapsOSMNode> nodes, out List<OnlineMapsOSMWay> ways, out List<OnlineMapsOSMRelation> relations, out List<OnlineMapsOSMArea> areas)
    {
        nodes = new Dictionary<string, OnlineMapsOSMNode>();
        ways = new List<OnlineMapsOSMWay>();
        relations = new List<OnlineMapsOSMRelation>();
        areas = new List<OnlineMapsOSMArea>();

        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(response);

            foreach (OnlineMapsXML node in xml)
            {
                if (node.name == "node")
                {
                    OnlineMapsOSMNode osmNode = new OnlineMapsOSMNode(node);
                    nodes.Add(osmNode.id, osmNode);
                }
                else if (node.name == "way") ways.Add(new OnlineMapsOSMWay(node));
                else if (node.name == "relation") relations.Add(new OnlineMapsOSMRelation(node));
                else if (node.name == "area") areas.Add(new OnlineMapsOSMArea(node));
            }
        }
        catch
        {
            Debug.Log(response);
        }
    }

    /// <summary>
    /// Fast way to get data from the response Open Street Map Overpass API.
    /// </summary>
    /// <param name="response">Response from Overpass API</param>
    /// <param name="nodes">Dictionary of nodes</param>
    /// <param name="ways">List of ways</param>
    /// <param name="relations">List of relations</param>
    public static void ParseOSMResponseFast(string response, out Dictionary<string, OnlineMapsOSMNode> nodes, out Dictionary<string, OnlineMapsOSMWay> ways, out List<OnlineMapsOSMRelation> relations)
    {
        int i = 0;
        OSMXMLNode rootNode = new OSMXMLNode(response, ref i);

        if (rootNode.childs == null)
        {
            nodes = new Dictionary<string, OnlineMapsOSMNode>();
            ways = new Dictionary<string, OnlineMapsOSMWay>();
            relations = new List<OnlineMapsOSMRelation>();
            return;
        }

        int countNodes = 0;
        int countWays = 0;
        int countRelations = 0;

        for (int j = 0; j < rootNode.childs.Count; j++)
        {
            OSMXMLNode node = rootNode.childs[j];
            if (node.name == "node") countNodes++;
            else if (node.name == "way") countWays++;
            else if (node.name == "relation") countRelations++;
        }

        nodes = new Dictionary<string, OnlineMapsOSMNode>(countNodes);
        ways = new Dictionary<string, OnlineMapsOSMWay>(countWays);
        relations = new List<OnlineMapsOSMRelation>(countRelations);

        for (int j = 0; j < rootNode.childs.Count; j++)
        {
            OSMXMLNode node = rootNode.childs[j];
            if (node.name == "node") nodes.Add(node.GetAttribute("id"), new OnlineMapsOSMNode(node));
            else if (node.name == "way")
            {
                OnlineMapsOSMWay way = new OnlineMapsOSMWay(node);
                if (!ways.ContainsKey(way.id)) ways.Add(way.id, way);
            }
            else if (node.name == "relation") relations.Add(new OnlineMapsOSMRelation(node));

        }
    }

    /// <summary>
    /// Fast XML parser optimized for OSM response.<br/>
    /// It has very limited support for XML and is not recommended for parsing any data except OSM response.
    /// </summary>
    public class OSMXMLNode
    {
        /// <summary>
        /// Noda name
        /// </summary>
        public string name;

        /// <summary>
        /// List of child nodes
        /// </summary>
        public List<OSMXMLNode> childs;

        /// <summary>
        /// Array of attributes key
        /// </summary>
        public string[] attributeKeys;

        /// <summary>
        /// Array of attributes value
        /// </summary>
        public string[] attributeValues;

        /// <summary>
        /// Value of node
        /// </summary>
        public string value;

        private int l;
        private int attributeCapacity;
        private int attributeCount;

        /// <summary>
        /// Parse XML string.
        /// </summary>
        /// <param name="s">XML string</param>
        /// <param name="i">Index of current character</param>
        public OSMXMLNode(string s, ref int i)
        {
            l = s.Length;
            int it = 0;
            while (i < l)
            {
                if (it++ > 1000)
                {
                    Debug.Log("it > 1000");
                    return;
                }
                
                char c = s[i];
                if (c == '<')
                {
                    i++;
                    if (s[i] == '?')
                    {
                        i++;
                        int it2 = 0;
                        while (i < l)
                        {
                            if (it2++ > 100)
                            {
                                Debug.Log("it2 > 100");
                                return;
                            }
                            if (s[i] == '?' && s[i + 1] == '>') break;
                            i++;
                        }
                    }
                    else
                    {
                        char lastChar;
                        int sni, eni;
                        GetNameIndices(s, i, out sni, out eni, out lastChar);
                        name = s.Substring(sni, eni - sni);
                        i = eni + 1;
                        if (lastChar == ' ') ParseAttributes(s, ref i, out lastChar);
                        if (lastChar == ' ')
                        {
                            int it2 = 0;
                            while (i < l)
                            {
                                c = s[i];
                                if (it2++ > 100)
                                {
                                    Debug.Log("it2 > 100");
                                    return;
                                }
                                if (c == '/')
                                {
                                    lastChar = c;
                                    i += 2;
                                    break;
                                }
                                if (c == '>')
                                {
                                    lastChar = c;
                                    i++;
                                    break;
                                }
                            }
                        }
                        if (lastChar == '>')
                        {
                            if (s[i] == '>') i++;
                            ParseValue(s, ref i);
                        }
                        if (lastChar == '/') i += 2;
                        break;
                    }
                }
                i++;
            }
        }

        public string GetAttribute(string key)
        {
            for (int i = 0; i < attributeKeys.Length; i++)
            {
                if (key == attributeKeys[i]) return attributeValues[i];
            }
            return null;
        }

        private void GetAttributeValue(string s, int i, out int svi, out int evi)
        {
            svi = -1;
            evi = -1;
            int it = 0;
            while (i < l)
            {
                if (it++ > 1000)
                {
                    Debug.Log("it > 1000");
                    return;
                }

                if (s[i] == '"')
                {
                    if (svi == -1) svi = i + 1;
                    else
                    {
                        evi = i;
                        return;
                    }
                }
                i++;
            }
        }

        private void GetNameIndices(string s, int i, out int startIndex, out int endIndex, out char lastChar)
        {
            int it = 0;
            startIndex = -1;
            endIndex = -1;
            while (i < l)
            {
                if (it++ > 100)
                {
                    Debug.Log("it > 100");
                    lastChar = (char) 0;
                    return;
                }
                char c = s[i];
                if (c == ' ')
                {
                    if (startIndex != -1)
                    {
                        endIndex = i;
                        lastChar = c;
                        return;
                    }
                }
                else if (c == '/' || c == '>' || c == '=')
                {
                    endIndex = i;
                    lastChar = c;
                    return;
                }
                else if (startIndex == -1) startIndex = i;
                i++;
            }
            lastChar = (char)0;
        }

        private void ParseAttributes(string s, ref int i, out char lastChar)
        {
            int j = i;
            int countQuotes = 0;
            lastChar = (char) 0;
            bool ignoreEnds = false;
            while (j < l)
            {
                char c = s[j];
                if (c == '"')
                {
                    if (s[j - 1] != '\\')
                    {
                        ignoreEnds = !ignoreEnds;
                        countQuotes++;
                    }
                }
                else if (!ignoreEnds && c == '/' || c == '>')
                {
                    lastChar = c;
                    break;
                }
                j++;
            }

            if (countQuotes == 0)
            {
                i = j;
                return;
            }
            if (countQuotes % 2 != 0)
            {
                Debug.Log("Something wrong");
                i = j;
                return;
            }

            attributeCapacity = countQuotes / 2;
            attributeCount = 0;
            attributeKeys = new string[attributeCapacity];
            attributeValues = new string[attributeCapacity];

            int it = 0;
            while (true)
            {
                if (it++ > 100)
                {
                    Debug.Log("it > 100");
                    lastChar = (char)0;
                    return;
                }

                if (!ParseAttribute(s, ref i, out lastChar)) break;
                if (lastChar == '/' || lastChar == '>') break;
            }
        }

        private bool ParseAttribute(string s, ref int i, out char lastChar)
        {
            int si, ei;
            GetNameIndices(s, i, out si, out ei, out lastChar);
            if (ei != -1)
            {
                string key = s.Substring(si, ei - si);
                i = ei + 1;
                GetAttributeValue(s, i, out si, out ei);
                string value = s.Substring(si, ei - si);
                attributeKeys[attributeCount] = key;
                attributeValues[attributeCount] = value;
                attributeCount++;
                i = ei + 1;
                lastChar = s[i];
                return true;
            }
            return false;
        }

        private void ParseChild(string s, ref int i)
        {
            OSMXMLNode child = new OSMXMLNode(s, ref i);
            if (childs == null) childs = new List<OSMXMLNode>();
            childs.Add(child);
        }

        private void ParseValue(string s, ref int i)
        {
            int it = 0;
            while (i < l)
            {
                if (it++ > 1000000)
                {
                    Debug.Log("it > 1000000");
                    return;
                }
                char c = s[i];
                if (c == '<')
                {
                    if (s[i + 1] == '/')
                    {
                        int it2 = 0;
                        while (i < l)
                        {
                            if (it2++ > 1000)
                            {
                                Debug.Log("it2 > 1000");
                                return;
                            }

                            if (s[i] == '>')
                            {
                                i++;
                                return;
                            }
                            i++;
                        }
                    }
                    else ParseChild(s, ref i);
                }
                else if (c == ' ' || c == '\n' || c == '\t')
                {
                    // Ignore
                }
                else
                {
                    //Load string value
                    int si = i;
                    int ei = -1;
                    int it2 = 0;
                    while (i < l)
                    {
                        if (it2++ > 1000)
                        {
                            Debug.Log("it2 > 1000");
                            return;
                        }
                        if (s[i] == '<' && s[i + 1] == '/')
                        {
                            ei = i;
                            break;
                        }
                        i++;
                    }
                    value = s.Substring(si, ei - si);
                    it2 = 0;
                    while (i < l)
                    {
                        if (it2++ > 1000)
                        {
                            Debug.Log("it2 > 1000");
                            return;
                        }

                        if (s[i] == '>')
                        {
                            i++;
                            return;
                        }
                        i++;
                    }
                }
                i++;
            }
        }
    }
}

/// <summary>
/// The base class of Open Streen Map element.
/// </summary>
public abstract class OnlineMapsOSMBase
{
    /// <summary>
    /// Element ID
    /// </summary>
    public string id;

    /// <summary>
    /// Element tags
    /// </summary>
    public List<OnlineMapsOSMTag> tags;

    protected static double CreateDouble(string s)
    {
        long n = 0;
        bool hasDecimalPoint = false;
        bool neg = false;
        long decimalV = 1;
        for (int x = 0; x < s.Length; x++)
        {
            char c = s[x];
            if (c == '.') hasDecimalPoint = true;
            else if (c == '-') neg = true;
            else
            {
                n *= 10;
                n += c - '0';
                if (hasDecimalPoint) decimalV *= 10;
            }
        }

        if (neg) n = -n;

        return n / (double)decimalV;
    }

    public virtual void Dispose()
    {
        tags = null;
    }

    public bool Equals(OnlineMapsOSMBase other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(this, other)) return true;
        return id == other.id;
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }

    /// <summary>
    /// Get tag value for the key.
    /// </summary>
    /// <param name="key">Tag key</param>
    /// <returns>Tag value</returns>
    public string GetTagValue(string key)
    {
        if (tags == null) return null;
        for (int i = 0; i < tags.Count; i++)
        {
            OnlineMapsOSMTag tag = tags[i];
            if (tag.key == key) return tag.value;
        }
        return null;
    }

    /// <summary>
    /// Checks for the tag with the specified key and value.
    /// </summary>
    /// <param name="key">Tag key</param>
    /// <param name="value">Tag value</param>
    /// <returns>True - if successful, False - otherwise.</returns>
    public bool HasTag(string key, string value)
    {
        return tags.Any(t => t.key == key && t.value == value);
    }

    /// <summary>
    /// Checks for the tag with the specified keys.
    /// </summary>
    /// <param name="keys">Tag keys.</param>
    /// <returns>True - if successful, False - otherwise.</returns>
    public bool HasTagKey(params string[] keys)
    {
        int kl = keys.Length;
        for (int i = 0; i < tags.Count; i++)
        {
            OnlineMapsOSMTag tag = tags[i];
            for (int k = 0; k < kl; k++)
            {
                if (keys[k] == tag.key) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks for the tag with the specified values.
    /// </summary>
    /// <param name="values">Tag values</param>
    /// <returns>True - if successful, False - otherwise.</returns>
    public bool HasTagValue(params string[] values)
    {
        return values.Any(val => tags.Any(t => t.value == val));
    }

    /// <summary>
    /// Checks for the tag with the specified key and values.
    /// </summary>
    /// <param name="key">Tag key</param>
    /// <param name="values">Tag values</param>
    /// <returns>True - if successful, False - otherwise.</returns>
    public bool HasTags(string key, params string[] values)
    {
        return tags.Any(tag => tag.key == key && values.Any(v => v == tag.value));
    }
}

/// <summary>
/// Open Street Map node element class
/// </summary>
public class OnlineMapsOSMNode : OnlineMapsOSMBase
{
    /// <summary>
    /// Latitude
    /// </summary>
    public readonly float lat;

    /// <summary>
    /// Longitude
    /// </summary>
    public readonly float lon;

    public OnlineMapsOSMNode(OnlineMapsOSMAPIQuery.OSMXMLNode node)
    {
        id = node.GetAttribute("id");
        lat = (float) CreateDouble(node.GetAttribute("lat"));
        lon = (float) CreateDouble(node.GetAttribute("lon"));
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Node</param>
    public OnlineMapsOSMNode(OnlineMapsXML node)
    {
        id = node.A("id");
        lat = node.A<float>("lat");
        lon = node.A<float>("lon");

        tags = new List<OnlineMapsOSMTag>(node.count);

        foreach (OnlineMapsXML subNode in node) tags.Add(new OnlineMapsOSMTag(subNode));
    }

    public static implicit operator Vector2(OnlineMapsOSMNode val)
    {
        return new Vector2(val.lon, val.lat);
    }
}

/// <summary>
/// Open Street Map way element class
/// </summary>
public class OnlineMapsOSMWay : OnlineMapsOSMBase
{
    /// <summary>
    /// List of node id;
    /// </summary>
    public List<string> nodeRefs
    {
        get { return _nodeRefs; }
        set { _nodeRefs = value; }
    }

    private List<string> _nodeRefs;

    public OnlineMapsOSMWay()
    {
        
    }

    public OnlineMapsOSMWay(OnlineMapsOSMAPIQuery.OSMXMLNode node)
    {
        id = node.GetAttribute("id");

        int countNd = 0;
        int countTags = 0;

        for (int i = 0; i < node.childs.Count; i++)
        {
            OnlineMapsOSMAPIQuery.OSMXMLNode subNode = node.childs[i];
            if (subNode.name == "nd") countNd++;
            else if (subNode.name == "tag") countTags++;
        }

        _nodeRefs = new List<string>(countNd);
        tags = new List<OnlineMapsOSMTag>(countTags);

        for (int i = 0; i < node.childs.Count; i++)
        {
            OnlineMapsOSMAPIQuery.OSMXMLNode subNode = node.childs[i];
            if (subNode.name == "nd") _nodeRefs.Add(subNode.GetAttribute("ref"));
            else if (subNode.name == "tag") tags.Add(new OnlineMapsOSMTag(subNode));
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Node</param>
    public OnlineMapsOSMWay(OnlineMapsXML node)
    {
        id = node.A("id");
        _nodeRefs = new List<string>();
        tags = new List<OnlineMapsOSMTag>();

        foreach (OnlineMapsXML subNode in node)
        {
            if (subNode.name == "nd") _nodeRefs.Add(subNode.A("ref"));
            else if (subNode.name == "tag") tags.Add(new OnlineMapsOSMTag(subNode));
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _nodeRefs = null;
    }

    /// <summary>
    /// Returns a list of nodes related to that way.
    /// </summary>
    /// <param name="nodes">General list of nodes</param>
    /// <returns>List of nodes related to that way</returns>
    public List<OnlineMapsOSMNode> GetNodes(List<OnlineMapsOSMNode> nodes)
    {
        List<OnlineMapsOSMNode> _nodes = new List<OnlineMapsOSMNode>();
        foreach (string nRef in nodeRefs)
        {
            OnlineMapsOSMNode node = nodes.FirstOrDefault(n => n.id == nRef);
            if (node != null) _nodes.Add(node);
        }
        return _nodes;
    }

    /// <summary>
    /// Returns a list of nodes related to that way.
    /// </summary>
    /// <param name="nodes">General dictionary of nodes</param>
    /// <returns>List of nodes related to that way</returns>
    public List<OnlineMapsOSMNode> GetNodes(Dictionary<string, OnlineMapsOSMNode> nodes)
    {
        List<OnlineMapsOSMNode> _nodes = new List<OnlineMapsOSMNode>(10);
        foreach (string nRef in nodeRefs)
        {
            if (nodes.ContainsKey(nRef))
            {
                _nodes.Add(nodes[nRef]);
            }
        }
        return _nodes;
    }

    /// <summary>
    /// Gets a list of nodes related to that way.
    /// </summary>
    /// <param name="nodes">General dictionary of nodes</param>
    /// <param name="usedNodes">List of nodes related to that way</param>
    public void GetNodes(Dictionary<string, OnlineMapsOSMNode> nodes, List<OnlineMapsOSMNode> usedNodes)
    {
        usedNodes.Clear();
        int count = 0;
        for (int i = 0; i < _nodeRefs.Count; i++)
        {
            string nRef = _nodeRefs[i];
            OnlineMapsOSMNode node;
            if (nodes.TryGetValue(nRef, out node))
            {
                usedNodes.Add(node);
                count++;
            }
        }
    }
}

/// <summary>
/// Open Street Map relation element class
/// </summary>
public class OnlineMapsOSMRelation : OnlineMapsOSMBase
{
    /// <summary>
    /// List members of relation
    /// </summary>
    public List<OnlineMapsOSMRelationMember> members
    {
        get { return _members; }
    }

    private List<OnlineMapsOSMRelationMember> _members;

    public OnlineMapsOSMRelation(OnlineMapsOSMAPIQuery.OSMXMLNode node)
    {
        id = node.GetAttribute("id");
        _members = new List<OnlineMapsOSMRelationMember>(16);
        tags = new List<OnlineMapsOSMTag>(4);

        foreach (OnlineMapsOSMAPIQuery.OSMXMLNode subNode in node.childs)
        {
            if (subNode.name == "member") _members.Add(new OnlineMapsOSMRelationMember(subNode));
            else if (subNode.name == "tag") tags.Add(new OnlineMapsOSMTag(subNode));
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Node</param>
    public OnlineMapsOSMRelation(OnlineMapsXML node)
    {
        id = node.A("id");
        _members = new List<OnlineMapsOSMRelationMember>(16);
        tags = new List<OnlineMapsOSMTag>(4);

        foreach (OnlineMapsXML subNode in node)
        {
            if (subNode.name == "member") _members.Add(new OnlineMapsOSMRelationMember(subNode));
            else if (subNode.name == "tag") tags.Add(new OnlineMapsOSMTag(subNode));
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _members = null;
    }
}

/// <summary>
/// Open Street Map relation member class
/// </summary>
public class OnlineMapsOSMRelationMember
{
    /// <summary>
    /// ID of reference element
    /// </summary>
    public readonly string reference;

    /// <summary>
    /// Member role
    /// </summary>
    public readonly string role;

    /// <summary>
    /// Member type
    /// </summary>
    public readonly string type;

    public OnlineMapsOSMRelationMember(OnlineMapsOSMAPIQuery.OSMXMLNode node)
    {
        type = node.GetAttribute("type");
        reference = node.GetAttribute("ref");
        role = node.GetAttribute("role");
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Node</param>
    public OnlineMapsOSMRelationMember(OnlineMapsXML node)
    {
        type = node.A("type");
        reference = node.A("ref");
        role = node.A("role");
    }
}

/// <summary>
/// Open Street Map element tag class
/// </summary>
public class OnlineMapsOSMTag
{
    /// <summary>
    /// Tag key
    /// </summary>
    public readonly string key;

    /// <summary>
    /// Tag value
    /// </summary>
    public readonly string value;

    public OnlineMapsOSMTag(OnlineMapsOSMAPIQuery.OSMXMLNode node)
    {
        key = node.GetAttribute("k");
        value = node.GetAttribute("v");
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Node</param>
    public OnlineMapsOSMTag(OnlineMapsXML node)
    {
        key = node.A("k");
        value = node.A("v");
    }

    public override string ToString()
    {
        return key + ": " + value;
    }
}

/// <summary>
/// Open Street Map area element class
/// </summary>
public class OnlineMapsOSMArea : OnlineMapsOSMBase
{

    public OnlineMapsOSMArea(OnlineMapsOSMAPIQuery.OSMXMLNode node)
    {
        id = node.GetAttribute("id");
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="node">Node</param>
    public OnlineMapsOSMArea(OnlineMapsXML node)
    {
        id = node.A("id");

        tags = new List<OnlineMapsOSMTag>(node.count);

        foreach (OnlineMapsXML subNode in node) tags.Add(new OnlineMapsOSMTag(subNode));
    }

}