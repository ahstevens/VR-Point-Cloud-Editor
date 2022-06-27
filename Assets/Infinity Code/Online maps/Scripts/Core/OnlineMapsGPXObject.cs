/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class for working with GPX.
/// </summary>
public class OnlineMapsGPXObject
{
    /// <summary>
    /// GPX document version.
    /// </summary>
    public string version = "1.1";

    /// <summary>
    /// Name or URL of the software that created your GPX document.<br/>
    /// This allows others to inform the creator of a GPX instance document that fails to validate.
    /// </summary>
    public string creator = "OnlineMaps";

    /// <summary>
    /// Metadata about the gpx.
    /// </summary>
    public Meta metadata;

    /// <summary>
    /// A list of waypoints.
    /// </summary>
    public List<Waypoint> waypoints;

    /// <summary>
    /// A list of routes.
    /// </summary>
    public List<Route> routes;

    /// <summary>
    /// A list of tracks.
    /// </summary>
    public List<Track> tracks;

    /// <summary>
    /// You can add extend GPX by adding your own elements from another schema here.
    /// </summary>
    public OnlineMapsXML extensions;

    private OnlineMapsGPXObject()
    {
        waypoints = new List<Waypoint>();
        routes = new List<Route>();
        tracks = new List<Track>();
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="creator">Name or URL of the software that created your GPX document.</param>
    /// <param name="version">GPX document version.</param>
    public OnlineMapsGPXObject(string creator, string version = "1.1"): this()
    {
        this.creator = creator;
        this.version = version;
    }

    /// <summary>
    /// Load GPX Object from string.
    /// </summary>
    /// <param name="content">A string containing GPX content.</param>
    /// <returns>Instance of GPX Object</returns>
    public static OnlineMapsGPXObject Load(string content)
    {
        OnlineMapsGPXObject instance = new OnlineMapsGPXObject();

        try
        {
            OnlineMapsXML xml = OnlineMapsXML.Load(content);

            instance.version = xml.A("version");
            instance.creator = xml.A("creator");

            foreach (OnlineMapsXML n in xml)
            {
                if (n.name == "wpt") instance.waypoints.Add(new Waypoint(n));
                else if (n.name == "rte") instance.routes.Add(new Route(n));
                else if (n.name == "trk") instance.tracks.Add(new Track(n));
                else if (n.name == "metadata") instance.metadata = new Meta(n);
                else if (n.name == "extensions") instance.extensions = n;
                else Debug.Log(n.name);
            }
        }
        catch (Exception exception)
        {
            Debug.Log(exception.Message + "\n" + exception.StackTrace);
        }

        return instance;
    }

    /// <summary>
    /// Returns OnlineMapsXML, contains full information about GPX Object.
    /// </summary>
    /// <returns>Instance of OnlineMapsXML.</returns>
    public OnlineMapsXML ToXML()
    {
        OnlineMapsXML xml = new OnlineMapsXML("gpx");
        xml.A("version", version);
        xml.A("creator", creator);

        if (metadata != null) metadata.AppendToNode(xml.Create("metadata"));
        if (waypoints != null) foreach (Waypoint i in waypoints) i.AppendToNode(xml.Create("wpt"));
        if (routes != null) foreach (Route i in routes) i.AppendToNode(xml.Create("rte"));
        if (tracks != null) foreach (Track i in tracks) i.AppendToNode(xml.Create("trk"));
        if (extensions != null) xml.AppendChild(extensions);

        return xml;
    }

    public override string ToString()
    {
        return ToXML().outerXml;
    }

    /// <summary>
    /// Information about the copyright holder and any license governing use of this file. 
    /// By linking to an appropriate license, you may place your data into the public domain or grant additional usage rights. 
    /// </summary>
    public class Copyright
    {
        /// <summary>
        /// Copyright holder
        /// </summary>
        public string author;

        /// <summary>
        /// Year of copyright.
        /// </summary>
        public int? year;

        /// <summary>
        /// Link to external file containing license text.
        /// </summary>
        public string license;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="author">Copyright holder</param>
        public Copyright(string author)
        {
            this.author = author;
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Copyright node</param>
        public Copyright(OnlineMapsXML node)
        {
            author = node.A("author");
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "year") year = n.Value<int>();
                else if (n.name == "license") license = n.Value();
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            node.A("author", author);
            if (year.HasValue) node.Create("year", year.Value);
            if (!string.IsNullOrEmpty(license)) node.Create("license", license);
        }
    }

    /// <summary>
    /// Two lat/lon pairs defining the extent of an element. 
    /// </summary>
    public class Bounds
    {
        /// <summary>
        /// The minimum latitude.
        /// </summary>
        public double minlat
        {
            get { return _minlat; }
            set { _minlat = OnlineMapsUtils.Clip(value, -90, 90); }
        }

        /// <summary>
        /// The minimum longitude.
        /// </summary>
        public double minlon
        {
            get { return _minlon; }
            set { _minlon = OnlineMapsUtils.Repeat(value, -180, 180); }
        }

        /// <summary>
        /// The maximum latitude.
        /// </summary>
        public double maxlat
        {
            get { return _maxlat; }
            set { _maxlat = OnlineMapsUtils.Clip(value, -90, 90); }
        }

        /// <summary>
        /// The maximum longitude.
        /// </summary>
        public double maxlon
        {
            get { return _maxlon; }
            set { _maxlon = OnlineMapsUtils.Repeat(value, -180, 180); }
        }

        private double _minlat;
        private double _minlon;
        private double _maxlat;
        private double _maxlon;

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Bounds node</param>
        public Bounds(OnlineMapsXML node)
        {
            minlat = node.A<double>("minlat");
            minlon = node.A<double>("minlon");
            maxlat = node.A<double>("maxlat");
            maxlon = node.A<double>("maxlon");
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="minlon">The minimum longitude.</param>
        /// <param name="minlat">The minimum latitude.</param>
        /// <param name="maxlon">The maximum longitude.</param>
        /// <param name="maxlat">The maximum latitude.</param>
        public Bounds(double minlon, double minlat, double maxlon, double maxlat)
        {
            this.minlat = minlat;
            this.minlon = minlon;
            this.maxlat = maxlat;
            this.maxlon = maxlon;
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            node.A("minlat", minlat);
            node.A("minlon", minlon);
            node.A("maxlat", maxlat);
            node.A("maxlon", maxlon);
        }
    }

    /// <summary>
    /// An email address. Broken into two parts (id and domain) to help prevent email harvesting. 
    /// </summary>
    public class EMail
    {
        /// <summary>
        /// ID half of email address
        /// </summary>
        public string id;

        /// <summary>
        /// Domain half of email address
        /// </summary>
        public string domain;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">ID half of email address</param>
        /// <param name="domain">Domain half of email address</param>
        public EMail(string id, string domain)
        {
            this.id = id;
            this.domain = domain;
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">EMail node</param>
        public EMail(OnlineMapsXML node)
        {
            id = node.A("id");
            domain = node.A("domain");
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            node.A("id", id);
            node.A("domain", domain);
        }
    }

    /// <summary>
    /// A link to an external resource (Web page, digital photo, video clip, etc) with additional information. 
    /// </summary>
    public class Link
    {
        /// <summary>
        /// URL of hyperlink.
        /// </summary>
        public string href;

        /// <summary>
        /// Text of hyperlink.
        /// </summary>
        public string text;

        /// <summary>
        /// Mime type of content (image/jpeg)
        /// </summary>
        public string type;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="href">URL of hyperlink.</param>
        public Link(string href)
        {
            this.href = href;
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Link node</param>
        public Link(OnlineMapsXML node)
        {
            href = node.A("href");
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "text") text = n.Value();
                else if (n.name == "type") type = n.Value();
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            node.A("href", href);
            if (!string.IsNullOrEmpty(text)) node.Create("text", text);
            if (!string.IsNullOrEmpty(type)) node.Create("type", type);
        }
    }

    /// <summary>
    /// Information about the GPX file, author, and copyright restrictions goes in the metadata section.<br/>
    /// Providing rich, meaningful information about your GPX files allows others to search for and use your GPS data. 
    /// </summary>
    public class Meta
    {
        /// <summary>
        /// The name of the GPX file.
        /// </summary>
        public string name;

        /// <summary>
        /// A description of the contents of the GPX file.
        /// </summary>
        public string description;

        /// <summary>
        /// The person or organization who created the GPX file.
        /// </summary>
        public Person author;

        /// <summary>
        /// Copyright and license information governing use of the file.
        /// </summary>
        public Copyright copyright;

        /// <summary>
        /// URLs associated with the location described in the file.
        /// </summary>
        public List<Link> links;

        /// <summary>
        /// The creation date of the file.
        /// </summary>
        public DateTime? time;

        /// <summary>
        /// Keywords associated with the file. Search engines or databases can use this information to classify the data.
        /// </summary>
        public string keywords;

        /// <summary>
        /// Minimum and maximum coordinates which describe the extent of the coordinates in the file.
        /// </summary>
        public Bounds bounds;

        /// <summary>
        /// You can add extend GPX by adding your own elements from another schema here.
        /// </summary>
        public OnlineMapsXML extensions;

        /// <summary>
        /// Constructor
        /// </summary>
        public Meta()
        {
            links = new List<Link>();
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Meta node</param>
        public Meta(OnlineMapsXML node):this()
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "desc") description = n.Value();
                else if (n.name == "author") author = new Person(n);
                else if (n.name == "copyright") copyright = new Copyright(n);
                else if (n.name == "link") links.Add(new Link(n));
                else if (n.name == "time") time = DateTime.Parse(n.Value());
                else if (n.name == "keywords") keywords = n.Value();
                else if (n.name == "bounds") bounds = new Bounds(n);
                else if (n.name == "extensions") extensions = n;
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            if (!string.IsNullOrEmpty(name)) node.Create("name", name);
            if (!string.IsNullOrEmpty(description)) node.Create("desc", description);
            if (author != null) author.AppendToNode(node);
            if (copyright != null) copyright.AppendToNode(node.Create("copyright"));
            if (links != null && links.Count > 0) foreach (Link l in links) l.AppendToNode(node.Create("link"));
            if (time.HasValue) node.Create("time", time.Value.ToUniversalTime().ToString("s") + "Z");
            if (!string.IsNullOrEmpty(keywords)) node.Create("keywords", keywords);
            if (bounds != null) bounds.AppendToNode(node.Create("bounds"));
            if (extensions != null) node.AppendChild(extensions);
        }
    }

    /// <summary>
    /// A person or organization. 
    /// </summary>
    public class Person
    {
        /// <summary>
        /// Name of person or organization.
        /// </summary>
        public string name;

        /// <summary>
        /// Email address.
        /// </summary>
        public EMail email;

        /// <summary>
        /// Link to Web site or other external information about person.
        /// </summary>
        public Link link;

        /// <summary>
        /// Constructor
        /// </summary>
        public Person()
        {
            
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Person node</param>
        public Person(OnlineMapsXML node)
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "email") email = new EMail(n);
                else if (n.name == "link") link = new Link(n);
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            if (!string.IsNullOrEmpty(name)) node.Create("name", name);
            if (email != null) email.AppendToNode(node.Create("email"));
            if (link != null) link.AppendToNode(node.Create("link"));
        }
    }

    /// <summary>
    /// Route - an ordered list of waypoints representing a series of turn points leading to a destination. 
    /// </summary>
    public class Route
    {
        /// <summary>
        /// GPS name of route.
        /// </summary>
        public string name;

        /// <summary>
        /// GPS comment for route.
        /// </summary>
        public string comment;

        /// <summary>
        /// Text description of route for user. Not sent to GPS.
        /// </summary>
        public string description;

        /// <summary>
        /// Source of data. Included to give user some idea of reliability and accuracy of data.
        /// </summary>
        public string source;

        /// <summary>
        /// Links to external information about the route.
        /// </summary>
        public List<Link> links;

        /// <summary>
        /// GPS route number.
        /// </summary>
        public uint? number;

        /// <summary>
        /// Type (classification) of route.
        /// </summary>
        public string type;

        /// <summary>
        /// A list of route points.
        /// </summary>
        public List<Waypoint> points;

        /// <summary>
        /// You can add extend GPX by adding your own elements from another schema here.
        /// </summary>
        public OnlineMapsXML extensions;

        /// <summary>
        /// Constructor
        /// </summary>
        public Route()
        {
            links = new List<Link>();
            points = new List<Waypoint>();
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Route node</param>
        public Route(OnlineMapsXML node) : this()
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "cmt") comment = n.Value();
                else if (n.name == "desc") description = n.Value();
                else if (n.name == "src") source = n.Value();
                else if (n.name == "link") links.Add(new Link(n));
                else if (n.name == "number") number = n.Value<uint>();
                else if (n.name == "type") type = n.Value();
                else if (n.name == "rtept") points.Add(new Waypoint(n));
                else if (n.name == "extensions") extensions = n;
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            if (!string.IsNullOrEmpty(name)) node.Create("name", name);
            if (!string.IsNullOrEmpty(comment)) node.Create("cmt", comment);
            if (!string.IsNullOrEmpty(description)) node.Create("desc", description);
            if (!string.IsNullOrEmpty(source)) node.Create("src", source);
            if (links != null) foreach (Link l in links) l.AppendToNode(node.Create("link"));
            if (number.HasValue) node.Create("number", number.Value);
            if (!string.IsNullOrEmpty(type)) node.Create("type", type);
            foreach (Waypoint p in points) p.AppendToNode(node.Create("rtept"));
            if (extensions != null) node.AppendChild(extensions);
        }
    }

    /// <summary>
    /// Track - an ordered list of points describing a path. 
    /// </summary>
    public class Track
    {
        /// <summary>
        /// GPS name of track.
        /// </summary>
        public string name;

        /// <summary>
        /// GPS comment for track.
        /// </summary>
        public string comment;

        /// <summary>
        /// User description of track.
        /// </summary>
        public string description;

        /// <summary>
        /// Source of data. Included to give user some idea of reliability and accuracy of data.
        /// </summary>
        public string source;

        /// <summary>
        /// Links to external information about track.
        /// </summary>
        public List<Link> links;

        /// <summary>
        /// GPS track number.
        /// </summary>
        public uint? number;

        /// <summary>
        /// Type (classification) of track.
        /// </summary>
        public string type;

        /// <summary>
        /// A Track Segment holds a list of Track Points which are logically connected in order.<br/>
        /// To represent a single GPS track where GPS reception was lost, or the GPS receiver was turned off, start a new Track Segment for each continuous span of track data.
        /// </summary>
        public List<TrackSegment> segments;

        /// <summary>
        /// You can add extend GPX by adding your own elements from another schema here.
        /// </summary>
        public OnlineMapsXML extensions;

        /// <summary>
        /// Constructor
        /// </summary>
        public Track()
        {
            links = new List<Link>();
            segments = new List<TrackSegment>();
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Track node</param>
        public Track(OnlineMapsXML node): this()
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "name") name = n.Value();
                else if (n.name == "cmt") comment = n.Value();
                else if (n.name == "desc") description = n.Value();
                else if (n.name == "src") source = n.Value();
                else if (n.name == "link") links.Add(new Link(n));
                else if (n.name == "number") number = n.Value<uint>();
                else if (n.name == "type") type = n.Value();
                else if (n.name == "trkseg") segments.Add(new TrackSegment(n));
                else if (n.name == "extensions") extensions = n;
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            if (!string.IsNullOrEmpty(name)) node.Create("name", name);
            if (!string.IsNullOrEmpty(comment)) node.Create("cmt", comment);
            if (!string.IsNullOrEmpty(description)) node.Create("desc", description);
            if (!string.IsNullOrEmpty(source)) node.Create("src", source);
            if (links != null) foreach (Link l in links) l.AppendToNode(node.Create("link"));
            if (number.HasValue) node.Create("number", number.Value);
            if (!string.IsNullOrEmpty(type)) node.Create("type", type);
            foreach (TrackSegment p in segments) p.AppendToNode(node.Create("trkseg"));
            if (extensions != null) node.AppendChild(extensions);
        }
    }

    /// <summary>
    /// A Track Segment holds a list of Track Points which are logically connected in order.<br/>
    /// To represent a single GPS track where GPS reception was lost, or the GPS receiver was turned off, start a new Track Segment for each continuous span of track data. 
    /// </summary>
    public class TrackSegment
    {
        /// <summary>
        /// A Track Point holds the coordinates, elevation, timestamp, and metadata for a single point in a track.
        /// </summary>
        public List<Waypoint> points;

        /// <summary>
        /// You can add extend GPX by adding your own elements from another schema here.
        /// </summary>
        public OnlineMapsXML extensions;

        /// <summary>
        /// Constructor
        /// </summary>
        public TrackSegment()
        {
            points = new List<Waypoint>();
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">TrackSegment node</param>
        public TrackSegment(OnlineMapsXML node): this()
        {
            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "trkpt") points.Add(new Waypoint(n));
                else if (n.name == "extensions") extensions = n;
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            foreach (Waypoint p in points) p.AppendToNode(node.Create("trkpt"));
            if (extensions != null) node.AppendChild(extensions);
        }
    }

    /// <summary>
    /// Waypoint, point of interest, or named feature on a map. 
    /// </summary>
    public class Waypoint
    {
        /// <summary>
        /// Elevation (in meters) of the point.
        /// </summary>
        public double? elevation;

        /// <summary>
        /// Creation/modification timestamp for element.<br/>
        /// Date and time in are in Univeral Coordinated Time (UTC), not local time!<br/>
        /// Conforms to ISO 8601 specification for date/time representation.<br/>
        /// Fractional seconds are allowed for millisecond timing in tracklogs.
        /// </summary>
        public DateTime? time;

        /// <summary>
        /// Height (in meters) of geoid (mean sea level) above WGS84 earth ellipsoid. As defined in NMEA GGA message.
        /// </summary>
        public double? geoidheight;

        /// <summary>
        /// The GPS name of the waypoint. This field will be transferred to and from the GPS.<br/>
        /// GPX does not place restrictions on the length of this field or the characters contained in it.<br/>
        /// It is up to the receiving application to validate the field before sending it to the GPS.
        /// </summary>
        public string name;

        /// <summary>
        /// GPS waypoint comment. Sent to GPS as comment.
        /// </summary>
        public string comment;

        /// <summary>
        /// A text description of the element. Holds additional information about the element intended for the user, not the GPS.
        /// </summary>
        public string description;

        /// <summary>
        /// Source of data. Included to give user some idea of reliability and accuracy of data. "Garmin eTrex", "USGS quad Boston North", e.g.
        /// </summary>
        public string source;

        /// <summary>
        /// Link to additional information about the waypoint.
        /// </summary>
        public List<Link> links;

        /// <summary>
        /// Text of GPS symbol name. For interchange with other programs, use the exact spelling of the symbol as displayed on the GPS. If the GPS abbreviates words, spell them out.
        /// </summary>
        public string symbol;

        /// <summary>
        /// Type (classification) of the waypoint.
        /// </summary>
        public string type;

        /// <summary>
        /// Type of GPX fix.
        /// </summary>
        public string fix;

        /// <summary>
        /// Number of satellites used to calculate the GPX fix.
        /// </summary>
        public uint? sat;

        /// <summary>
        /// Horizontal dilution of precision.
        /// </summary>
        public double? hdop;

        /// <summary>
        /// Vertical dilution of precision.
        /// </summary>
        public double? vdop;

        /// <summary>
        /// Position dilution of precision.
        /// </summary>
        public double? pdop;

        /// <summary>
        /// Number of seconds since last DGPS update.
        /// </summary>
        public double? ageofdgpsdata;

        /// <summary>
        /// You can add extend GPX by adding your own elements from another schema here.
        /// </summary>
        public OnlineMapsXML extensions;

        private double _lat;
        private double _lon;
        private double? _magvar;
        private short? _dgpsid;

        /// <summary>
        /// The latitude of the point. Decimal degrees, WGS84 datum.
        /// </summary>
        public double lat
        {
            get { return _lat; }
            set { _lat = OnlineMapsUtils.Clip(value, -90, 90); }
        }

        /// <summary>
        /// The longitude of the point. Decimal degrees, WGS84 datum.
        /// </summary>
        public double lon
        {
            get { return _lon; }
            set { _lon = OnlineMapsUtils.Repeat(value, -180, 180); }
        }

        /// <summary>
        /// Magnetic variation (in degrees) at the point
        /// </summary>
        public double? magvar
        {
            get { return _magvar; }
            set
            {
                if (value.HasValue) _magvar = OnlineMapsUtils.Clip(value.Value, 0, 360);
                else _magvar = null;
            }
        }

        /// <summary>
        /// ID of DGPS station used in differential correction.
        /// </summary>
        public short? dgpsid
        {
            get { return _dgpsid; }
            set
            {
                if (value.HasValue)
                {
                    if (value.Value < 0) _dgpsid = 0;
                    else if (value.Value > 1023) _dgpsid = 1023;
                    else _dgpsid = value.Value;
                }
                else _dgpsid = null;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lon">The longitude of the point.</param>
        /// <param name="lat">The latitude of the point.</param>
        public Waypoint(double lon, double lat)
        {
            links = new List<Link>();
            this.lat = lat;
            this.lon = lon;
        }

        /// <summary>
        /// Creates instance and loads the data from the node.
        /// </summary>
        /// <param name="node">Waypoint node</param>
        public Waypoint(OnlineMapsXML node)
        {
            links = new List<Link>();
            lat = node.A<double>("lat");
            lon = node.A<double>("lon");

            foreach (OnlineMapsXML n in node)
            {
                if (n.name == "ele") elevation = n.Value<double>();
                else if (n.name == "time") time = DateTime.Parse(n.Value());
                else if (n.name == "magvar") magvar = n.Value<double>();
                else if (n.name == "geoidheight") geoidheight = n.Value<double>();
                else if (n.name == "name") name = n.Value();
                else if (n.name == "cmt") comment = n.Value();
                else if (n.name == "desc") description = n.Value();
                else if (n.name == "src") source = n.Value();
                else if (n.name == "link") links.Add(new Link(n));
                else if (n.name == "sym") symbol = n.Value();
                else if (n.name == "type") type = n.Value();
                else if (n.name == "fix") fix = n.Value();
                else if (n.name == "sat") sat = n.Value<uint>();
                else if (n.name == "hdop") hdop = n.Value<double>();
                else if (n.name == "vdop") vdop = n.Value<double>();
                else if (n.name == "pdop") pdop = n.Value<double>();
                else if (n.name == "ageofdgpsdata") ageofdgpsdata = n.Value<double>();
                else if (n.name == "dgpsid") dgpsid = n.Value<short>();
                else if (n.name == "extensions") extensions = n;
                else Debug.Log(n.name);
            }
        }

        public void AppendToNode(OnlineMapsXML node)
        {
            node.A("lat", lat);
            node.A("lon", lon);

            if (elevation.HasValue) node.Create("ele", elevation.Value);
            if (time.HasValue) node.Create("time", time.Value.ToUniversalTime().ToString("s") + "Z");
            if (magvar.HasValue) node.Create("magvar", magvar.Value);
            if (geoidheight.HasValue) node.Create("geoidheight", geoidheight.Value);
            if (!string.IsNullOrEmpty(name)) node.Create("name", name);
            if (!string.IsNullOrEmpty(comment)) node.Create("cmt", comment);
            if (!string.IsNullOrEmpty(description)) node.Create("desc", description);
            if (!string.IsNullOrEmpty(source)) node.Create("src", source);
            if (links != null) foreach (Link l in links) l.AppendToNode(node.Create("link"));
            if (!string.IsNullOrEmpty(symbol)) node.Create("sym", symbol);
            if (!string.IsNullOrEmpty(type)) node.Create("type", type);
            if (!string.IsNullOrEmpty(fix)) node.Create("fix", fix);
            if (sat.HasValue) node.Create("sat", sat.Value);
            if (hdop.HasValue) node.Create("hdop", hdop.Value);
            if (vdop.HasValue) node.Create("vdop", vdop.Value);
            if (pdop.HasValue) node.Create("pdop", pdop.Value);
            if (ageofdgpsdata.HasValue) node.Create("ageofdgpsdata", ageofdgpsdata.Value);
            if (dgpsid.HasValue) node.Create("dgpsid", dgpsid.Value);
            if (extensions != null) node.AppendChild(extensions);
        }
    }
}