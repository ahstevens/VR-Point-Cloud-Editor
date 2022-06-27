/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System.Collections;
using System.Collections.Generic;

/// <summary>
/// The resulting object for Open Route Service Directions
/// </summary>
public class OnlineMapsOpenRouteServiceDirectionResult
{
    /// <summary>
    /// Array of the routes
    /// </summary>
    public Route[] routes;

    /// <summary>
    /// Coordinates of the bounding box
    /// </summary>
    public double[] bbox;

    /// <summary>
    /// Route object
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Contains total sums of duration, route distance and actual distance of the route.
        /// </summary>
        public Summary summary;

        /// <summary>
        /// Contains the defined geometry format.
        /// </summary>
        public string geometry_format;

        /// <summary>
        /// Contains the geometry in the defined geometry format.
        /// </summary>
        public object geometry;

        /// <summary>
        /// List containing the segments and its correspoding steps which make up the route.
        /// </summary>
        public Segment[] segments;

        /// <summary>
        /// List containing the indices of way points corresponding to the geometry.
        /// </summary>
        public int[] way_points;

        /// <summary>
        /// For every information item there is an associated block divided into summary and values.
        /// </summary>
        public Extra extras;
        public double[] bbox;

        /// <summary>
        /// Points of the route
        /// </summary>
        public List<OnlineMapsVector2d> points
        {
            get
            {
                if (geometry_format == "encodedpolyline" || geometry_format == null) return OnlineMapsUtils.DecodePolylinePointsD((string) geometry);
                if (geometry_format == "polyline" || geometry_format == "geojson")
                {
                    IEnumerable ps;
                    if (geometry_format == "polyline") ps = geometry as IEnumerable;
                    else
                    {
                        Dictionary<string, object> d = geometry as Dictionary<string, object>;
                        if (d == null) return null;
                        ps = d["coordinates"] as IEnumerable;
                    }

                    if (ps == null) return null;

                    List<OnlineMapsVector2d> p = new List<OnlineMapsVector2d>();
                    foreach (object v in ps)
                    {
                        IEnumerable v2 = v as IEnumerable;
                        double x = 0, y = 0;
                        bool isY = false;
                        foreach (object v3 in v2)
                        {
                            if (isY)
                            {
                                y = (double) v3;
                                break;
                            }
                            x = (double) v3;
                            isY = true;
                        }
                        p.Add(new OnlineMapsVector2d(x, y));
                    }
                    return p;
                }
                return null;
            }
        }
    }

    /// <summary>
    /// Contains total sums of duration, route distance and actual distance of the route.
    /// </summary>
    public class Summary
    {
        /// <summary>
        /// Total route distance in specified units.
        /// </summary>
        public float distance;

        /// <summary>
        /// Total duration in seconds.
        /// </summary>
        public float duration;

        /// <summary>
        /// Total ascent in meters.
        /// </summary>
        public float ascent;

        /// <summary>
        /// Total descent in meters.
        /// </summary>
        public float descent;
    }

    /// <summary>
    /// Segment and its correspoding steps which make up the route.
    /// </summary>
    public class Segment
    {
        /// <summary>
        /// Contains the distance of the segment in specified units.
        /// </summary>
        public float distance;

        /// <summary>
        /// Contains the duration of the segment in seconds.
        /// </summary>
        public float duration;

        /// <summary>
        /// Contains ascent of this segment in meters for elevation=true.
        /// </summary>
        public float ascent;

        /// <summary>
        /// Contains descent of this segment in meters for elevation=true.
        /// </summary>
        public float descent;

        /// <summary>
        /// List containing the specific steps the segment consists of.
        /// </summary>
        public Step[] steps;
    }

    /// <summary>
    /// Step of the segment
    /// </summary>
    public class Step
    {
        /// <summary>
        /// The distance for the step in meters.
        /// </summary>
        public float distance;

        /// <summary>
        /// The duration for the step in seconds.
        /// </summary>
        public float duration;

        /// <summary>
        /// The instruction action for symbolisation purposes. Refer to the tables on https://github.com/GIScience/openrouteservice-docs
        /// </summary>
        public int type;

        /// <summary>
        /// The routing instruction text for the step.
        /// </summary>
        public string instruction;

        /// <summary>
        /// List containing the indices of the steps start- and endpoint corresponding to the geometry.
        /// </summary>
        public int[] way_points;
    }

    /// <summary>
    /// For every information item there is an associated block divided into summary and values.
    /// </summary>
    public class Extra
    {
        public ExtraItem surface;
        public ExtraItem waytypes;
        public ExtraItem steepness;
        public ExtraItem suitability;
        public ExtraItem waycategory;
    }

    public class ExtraItem
    {
        /// <summary>
        /// Broken down by way_points.
        /// </summary>
        public int[][] values;

        /// <summary>
        /// Broken down by information category values.
        /// </summary>
        public ExtraItemSummary[] summary;
    }

    /// <summary>
    /// Broken down by information category values.
    /// </summary>
    public class ExtraItemSummary
    {
        /// <summary>
        /// Value of a info category. Refer to the tables on https://github.com/GIScience/openrouteservice-docs
        /// </summary>
        public int value;

        /// <summary>
        /// Cumulative distance of this value.
        /// </summary>
        public double distance;

        /// <summary>
        /// Category percentage of the entire route.
        /// </summary>
        public double amount;
    }
}