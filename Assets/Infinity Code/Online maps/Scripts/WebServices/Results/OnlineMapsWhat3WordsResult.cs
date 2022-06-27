/*         INFINITY CODE         */
/*   https://infinity-code.com   */

/// <summary>
/// Base class for result objects of What 3 Words.
/// </summary>
public abstract class OnlineMapsWhat3WordsResultBase
{
    /// <summary>
    /// The response code.
    /// </summary>
    public int code = 200;

    /// <summary>
    /// The human-readable status of the request.
    /// </summary>
    public string message = "OK";

    public class Bounds : OnlineMapsGeoRect
    {
        public OnlineMapsVector2d southwest
        {
            get
            {
                return new OnlineMapsVector2d(left, bottom);
            }
            set
            {
                left = value.x;
                bottom = value.y;
            }
        }

        public OnlineMapsVector2d northeast
        {
            get
            {
                return new OnlineMapsVector2d(right, top);
            }
            set
            {
                right = value.x;
                top = value.y;
            }
        }
    }
}

/// <summary>
/// The resulting object for What 3 Words forward and reverse geocoding.
/// </summary>
public class OnlineMapsWhat3WordsFRResult : OnlineMapsWhat3WordsResultBase
{
    public Bounds bounds;
    public string words;
    public string map;
    public string language;
    public OnlineMapsVector2d geometry;
}

/// <summary>
/// The resulting object for What 3 Words AutoSuggest or StandardBlend.
/// </summary>
public class OnlineMapsWhat3WordsSBResult : OnlineMapsWhat3WordsResultBase
{
    [OnlineMapsJSON.Alias("suggestions", "blends")]
    public Item[] items;

    public class Item
    {
        public string country;
        public int distance;
        public string words;
        public int rank;
        public OnlineMapsVector2d geometry;
        public string place;
    }
}

/// <summary>
/// The resulting object for What 3 Words Grid.
/// </summary>
public class OnlineMapsWhat3WordsGridResult : OnlineMapsWhat3WordsResultBase
{
    public Line[] lines;

    public class Line
    {
        public OnlineMapsVector2d start;
        public OnlineMapsVector2d end;
    }
}

/// <summary>
///  The resulting object for What 3 Words Get Languages.
/// </summary>
public class OnlineMapsWhat3WordsLanguagesResult : OnlineMapsWhat3WordsResultBase
{
    public Language[] languages;

    public class Language
    {
        public string code;
        public string name;
        public string native_name;
    }
}