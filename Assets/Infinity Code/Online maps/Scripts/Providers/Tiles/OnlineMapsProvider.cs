/*         INFINITY CODE         */
/*   https://infinity-code.com   */

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Provider of tiles.
/// </summary>
public class OnlineMapsProvider
{
    private const string SATELLITE = "Satellite";
    private const string RELIEF = "Relief";
    private const string TERRAIN = "Terrain";
    private const string MAP = "Map";

    private static OnlineMapsProvider[] providers;

    private Func<MapType, string, string> OnReplaceToken;

    /// <summary>
    /// ID of provider
    /// </summary>
    public readonly string id;

    /// <summary>
    /// Human-readable provider title.
    /// </summary>
    public readonly string title;

    /// <summary>
    /// Indicates that the provider supports multilanguage.
    /// </summary>
    public bool? hasLanguage;

    /// <summary>
    /// Indicates that the provider supports a map with labels.
    /// </summary>
    public bool? hasLabels;

    /// <summary>
    /// Indicates that the label is always enabled.
    /// </summary>
    public bool? labelsEnabled;

    /// <summary>
    /// Map projection.
    /// </summary>
    public OnlineMapsProjection projection;

    /// <summary>
    /// Indicates that the provider uses HTTP.
    /// </summary>
    public bool? useHTTP;

    /// <summary>
    /// Index of current provider.
    /// </summary>
    public int index;

    /// <summary>
    /// Extension. Token {ext}, that is being replaced in the URL.
    /// </summary>
    public string ext;

    /// <summary>
    /// Property. Token {prop}, that is being replaced in the URL.
    /// </summary>
    public string prop;

    /// <summary>
    /// Property. Token {prop2}, that is being replaced in the URL.
    /// </summary>
    public string prop2;

    /// <summary>
    /// Indicates that the provider uses two letter language code.
    /// </summary>
    public bool twoLetterLanguage = true;

    public bool logUrl = false;
    public IExtraField[] extraFields;

    private string _url;
    private MapType[] _types;

    /// <summary>
    /// Array of map types available for the current provider.
    /// </summary>
    public MapType[] types
    {
        get { return _types; }
    }

    /// <summary>
    /// Gets / sets the URL pattern of tiles.
    /// </summary>
    public string url
    {
        get { return _url; }
        set
        {
            _url = value;
            if (!value.StartsWith("https")) useHTTP = true;
        }
    }

    private OnlineMapsProvider(string title) : this(title.ToLower(), title)
    {
        
    }

    private OnlineMapsProvider(string id, string title)
    {
        this.id = id.ToLower();
        this.title = title;
        projection = new OnlineMapsProjectionSphericalMercator();
    }

    /// <summary>
    /// Appends map types to the provider.
    /// </summary>
    /// <param name="newTypes">Map types</param>
    public void AppendTypes(params MapType[] newTypes)
    {
        int l = _types.Length;
        Array.Resize(ref _types, l + newTypes.Length);
        for (int i = 0; i < newTypes.Length; i++)
        {
            MapType type = _types[l + i] = newTypes[i];
            type.provider = this;
            type.fullID = id + "." + type.id;
        }
    }

    /// <summary>
    /// Creates a new provider, with the specified title.
    /// </summary>
    /// <param name="title">Provider title. Provider id = title.ToLower().</param>
    /// <returns>Instance of provider.</returns>
    public static OnlineMapsProvider Create(string title)
    {
        OnlineMapsProvider provider = new OnlineMapsProvider(title);
        provider._types = new MapType[0];
        if (providers == null) InitProviders();
        Array.Resize(ref providers, providers.Length + 1);
        providers[providers.Length - 1] = provider;
        return provider;
    }

    /// <summary>
    /// Gets an instance of a map type by ID.<br/>
    /// ID - providerID or providerID(dot)typeID.<br/>
    /// If the typeID is not specified returns the first map type of provider.<br/>
    /// If the provider ID is not found, returns the first map type of the first provider.<br/>
    /// Example: nokia or google.satellite
    /// </summary>
    /// <param name="mapTypeID">ID of map type</param>
    /// <returns>Instance of map type</returns>
    public static MapType FindMapType(string mapTypeID)
    {
        if (providers == null) InitProviders();

        if (string.IsNullOrEmpty(mapTypeID)) return providers[0].types[0];

        string[] parts = mapTypeID.Split('.');

        foreach (OnlineMapsProvider provider in providers)
        {
            if (provider.id == parts[0])
            {
                if (parts.Length == 1) return provider.types[0];
                foreach (MapType type in provider.types)
                {
                    if (type.id == parts[1]) return type;
                }
                return provider.types[0];
            }
        }
        return providers[0].types[0];
    }

    /// <summary>
    /// Gets map type by index.
    /// </summary>
    /// <param name="index">Index of map type.</param>
    /// <param name="repeat">TRUE - Repeat index value, FALSE - Clamp index value.</param>
    /// <returns>Instance of map type.</returns>
    public MapType GetByIndex(int index, bool repeat = false)
    {
        if (repeat) index = Mathf.RoundToInt(Mathf.Repeat(index, _types.Length - 1));
        else index = Mathf.Clamp(index, 0, _types.Length);
        return _types[index];
    }

    /// <summary>
    /// Gets array of providers
    /// </summary>
    /// <returns>Array of providers</returns>
    public static OnlineMapsProvider[] GetProviders()
    {
        if (providers == null) InitProviders();
        return providers;
    }

    /// <summary>
    /// Gets array of provider titles.
    /// </summary>
    /// <returns>Array of provider titles</returns>
    public static string[] GetProvidersTitle()
    {
        if (providers == null) InitProviders();
        return providers.Select(p => p.title).ToArray();
    }

    private static void InitProviders()
    {
        providers = new []
        {
            new OnlineMapsProvider("arcgis", "ArcGIS (Esri)")
            {
                url = "https://server.arcgisonline.com/ArcGIS/rest/services/{variant}/MapServer/tile/{zoom}/{y}/{x}",
                _types = new []
                {
                    new MapType("WorldImagery") { variantWithoutLabels = "World_Imagery" },
                    new MapType("WorldTopoMap") { variantWithLabels = "World_Topo_Map" },
                    new MapType("WorldStreetMap") { variantWithLabels = "World_Street_Map"},
                    new MapType("DeLorme") { variantWithLabels = "Specialty/DeLorme_World_Base_Map"},
                    new MapType("WorldTerrain") { variantWithoutLabels = "World_Terrain_Base"},
                    new MapType("WorldShadedRelief") { variantWithoutLabels = "World_Shaded_Relief"},
                    new MapType("WorldPhysical") { variantWithoutLabels = "World_Physical_Map"},
                    new MapType("OceanBasemap") { variantWithLabels = "Ocean_Basemap"},
                    new MapType("NatGeoWorldMap") { variantWithLabels = "NatGeo_World_Map"},
                    new MapType("WorldGrayCanvas") { variantWithLabels = "Canvas/World_Light_Gray_Base"},
                }
            },
            new OnlineMapsProvider("CartoDB")
            {
                url = "https://cartodb-basemaps-d.global.ssl.fastly.net/{variant}/{z}/{x}/{y}.png",
                _types = new []
                {
                    new MapType("Positron")
                    {
                        variantWithLabels = "light_all",
                        variantWithoutLabels = "light_nolabels"
                    },
                    new MapType("DarkMatter")
                    {
                        variantWithLabels = "dark_all",
                        variantWithoutLabels = "dark_nolabels"
                    },
                }
            },
            new OnlineMapsProvider("DigitalGlobe")
            {
                url = "https://a.tiles.mapbox.com/v4/digitalglobe.{variant}/{zoom}/{x}/{y}.jpg?access_token={accesstoken}",
                _types = new []
                {
                    new MapType("Satellite")
                    {
                        variantWithoutLabels = "nal0g75k"
                    },
                    new MapType("Street")
                    {
                        variantWithLabels = "nako6329",
                    },
                    new MapType("Terrain")
                    {
                        variantWithLabels = "nako1fhg",
                    },
                },
                extraFields = new []
                {
                    new ExtraField("Access Token", "accesstoken"),
                }
            },
            new OnlineMapsProvider("google", "Google Maps")
            {
                hasLanguage = true,
                _types = new[]
                {
                    new MapType(SATELLITE)
                    {
                        urlWithLabels = "https://mt{rnd0-3}.googleapis.com/vt/lyrs=y&hl={lng}&x={x}&y={y}&z={zoom}",
                        urlWithoutLabels = "https://khm{rnd0-3}.googleapis.com/kh?v={version}&hl={lng}&x={x}&y={y}&z={zoom}",
                        extraFields = new []
                        {
                            new ExtraField("Tile version", "version", "902")
                        }
                    },
                    new MapType(RELIEF)
                    {
                        urlWithLabels = "https://mts{rnd0-3}.google.com/vt/lyrs=t@131,r@216000000&src=app&hl={lng}&x={x}&y={y}&z={zoom}&s="
                    },
                    new MapType(TERRAIN)
                    {
                        urlWithLabels = "https://mt{rnd0-3}.googleapis.com/vt?pb=!1m4!1m3!1i{zoom}!2i{x}!3i{y}!2m3!1e0!2sm!3i295124088!3m9!2s{lng}!3sUS!5e18!12m1!1e47!12m3!1e37!2m1!1ssmartmaps!4e0"
                    }
                }
            },
            new OnlineMapsProvider("Hydda")
            {
                url = "https://{s}.tile.openstreetmap.se/hydda/{variant}/{z}/{x}/{y}.png",
                _types = new []
                {
                    new MapType("Full") { variantWithLabels = "full" },
                    new MapType("Base") { variantWithLabels = "base" },
                    new MapType("RoadsAndLabels") { variantWithLabels = "roads_and_labels" },
                }
            },
            new OnlineMapsProvider("Mapbox")
            {
                labelsEnabled = false,

                _types = new []
                {
                    new MapType("Map")
                    {
                        urlWithLabels = "https://api.mapbox.com/styles/v1/{userid}/{mapid}/tiles/256/{z}/{x}/{y}?events=true&access_token={accesstoken}",
                        extraFields = new []
                        {
                            new ExtraField("User ID", "userid"),
                            new ExtraField("Map ID", "mapid"),
                        }
                    },
                    new MapType("Satellite")
                    {
                        urlWithoutLabels = "https://api.mapbox.com/v4/mapbox.satellite/{z}/{x}/{y}.png?events=true&access_token={accesstoken}"
                    }
                },

                extraFields = new []
                {
                    new ExtraField("Access Token", "accesstoken"),
                }
            },
            new OnlineMapsProvider("Mapbox classic")
            {
                url = "https://b.tiles.mapbox.com/v4/{mapid}/{zoom}/{x}/{y}.png?events=true&access_token={accesstoken}",
                labelsEnabled = true,

                _types = new []
                {
                    new MapType("Map"), 
                },

                extraFields = new []
                {
                    new ExtraField("Map ID", "mapid"), 
                    new ExtraField("Access Token", "accesstoken"), 
                }
            }, 
            new OnlineMapsProvider("MapQuest")
            {
                url = "https://a.tiles.mapbox.com/v4/{variant}/{zoom}/{x}/{y}.png?access_token={accesstoken}",
                _types = new []
                {
                    new MapType(SATELLITE) { variantWithoutLabels = "mapquest.satellite" },
                    new MapType("Streets") { variantWithLabels = "mapquest.streets" },
                },
                extraFields = new []
                {
                    new ToggleExtraGroup("Anonymous", true, new []
                    {
                        new ExtraField("Access Token", "accesstoken", "pk.eyJ1IjoibWFwcXVlc3QiLCJhIjoiY2Q2N2RlMmNhY2NiZTRkMzlmZjJmZDk0NWU0ZGJlNTMifQ.mPRiEubbajc6a5y9ISgydg")
                    })
                },
            },
            new OnlineMapsProvider("mapy", "Mapy.CZ")
            {
                url = "https://m{rnd0-4}.mapserver.mapy.cz/{variant}/{zoom}-{x}-{y}",
                _types = new []
                {
                    new MapType(SATELLITE) { variantWithoutLabels = "ophoto-m" },
                    new MapType("Travel") { variantWithLabels = "wturist-m" }, 
                    new MapType("Winter") { variantWithLabels = "wturist_winter-m" }, 
                    new MapType("Geographic") { variantWithLabels = "zemepis-m" }, 
                    new MapType("Summer") { variantWithLabels = "turist_aquatic-m" }, 
                    new MapType("19century", "19th century") { variantWithLabels = "army2-m" }, 
                }
            }, 
            new OnlineMapsProvider("nationalmap", "National Map")
            {
                url = "https://basemap.nationalmap.gov/arcgis/rest/services/{variant}/MapServer/tile/{z}/{y}/{x}",
                //hasLabels = true,
                _types = new []
                {
                    new MapType("USGSHydroCached")
                    {
                        variantWithLabels = "USGSHydroCached"
                    }, 
                    new MapType("USGSImagery")
                    {
                        variantWithoutLabels = "USGSImageryOnly",
                        variantWithLabels = "USGSImageryTopo"
                    }, 
                    new MapType("USGSShadedReliefOnly")
                    {
                        variantWithoutLabels = "USGSShadedReliefOnly"
                    }, 
                    new MapType("USGSTopo")
                    {
                        variantWithLabels = "USGSTopo"
                    }
                }
            },
            new OnlineMapsProvider("nokia", "Nokia Maps (here.com)")
            {
                url = "https://{rnd1-4}.{prop2}.maps.ls.hereapi.com/maptile/2.1/{prop}/newest/{variant}/{zoom}/{x}/{y}/256/png8?lg={lng}&{auth}",
                twoLetterLanguage = false,
                hasLanguage = true,
                labelsEnabled = true,
                prop = "maptile",
                prop2 = "base",
                OnReplaceToken = delegate(MapType type, string token)
                {
                    if (token != "auth") return null;

                    string api = "apikey";
                    if (type.TryUseExtraFields(ref api) && !string.IsNullOrEmpty(api)) return "apiKey=" + api;
                    
                    string id = "appid", code = "appcode";
                    type.TryUseExtraFields(ref id);
                    type.TryUseExtraFields(ref code);
                    return "app_id=" + id + "&app_code=" + code;
                },

                _types = new []
                {
                    new MapType(SATELLITE)
                    {
                        variantWithLabels = "hybrid.day",
                        variantWithoutLabels = "satellite.day",
                        prop2 = "aerial",
                    },
                    new MapType(TERRAIN)
                    {
                        variant = "terrain.day",
                        propWithoutLabels = "basetile",
                        prop2 = "aerial",
                    },
                    new MapType(MAP)
                    {
                        variant = "normal.day",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalDayCustom")
                    {
                        variant = "normal.day.custom",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalDayGrey")
                    {
                        variant = "normal.day.grey",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalDayMobile")
                    {
                        variant = "normal.day.mobile",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalDayGreyMobile")
                    {
                        variant = "normal.day.grey.mobile",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalDayTransit")
                    {
                        variant = "normal.day.transit",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalDayTransitMobile")
                    {
                        variant = "normal.day.transit.mobile",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalNight")
                    {
                        variant = "normal.night",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalNightMobile")
                    {
                        variant = "normal.night.mobile",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalNightGrey")
                    {
                        variant = "normal.night.grey",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("normalNightGreyMobile")
                    {
                        variant = "normal.night.grey.mobile",
                        propWithoutLabels = "basetile",
                    },
                    new MapType("pedestrianDay")
                    {
                        variantWithLabels = "pedestrian.day"
                    },
                    new MapType("pedestrianNight")
                    {
                        variantWithLabels = "pedestrian.night"
                    },
                },

                extraFields = new []
                {
                    new ToggleExtraGroup("Anonymous", true, new []
                    {
                        new ExtraField("App ID", "appid", "xWVIueSv6JL0aJ5xqTxb"),
                        new ExtraField("App Code", "appcode", "djPZyynKsbTjIUDOBcHZ2g"),
                    })
                }
            },
            new OnlineMapsProvider("OpenMapSurfer")
            {
                url = "http://korona.geog.uni-heidelberg.de/tiles/{variant}/x={x}&y={y}&z={z}",
                _types = new []
                {
                    new MapType("Roads") { variantWithLabels = "roads" },
                    new MapType("AdminBounds") { variantWithLabels = "adminb" },
                    new MapType("Grayscale") { variantWithLabels = "roadsg" },
                }
            },
            new OnlineMapsProvider("osm", "OpenStreetMap")
            {
                _types = new []
                {
                    new MapType("Mapnik") { urlWithLabels = "https://a.tile.openstreetmap.org/{zoom}/{x}/{y}.png" },
                    new MapType("BlackAndWhite") { urlWithLabels = "https://a.tiles.wmflabs.org/bw-mapnik/{zoom}/{x}/{y}.png" },
                    new MapType("DE") { urlWithLabels = "https://a.tile.openstreetmap.de/tiles/osmde/{zoom}/{x}/{y}.png" },
                    new MapType("France") { urlWithLabels = "https://a.tile.openstreetmap.fr/osmfr/{zoom}/{x}/{y}.png" },
                    new MapType("HOT") { urlWithLabels = "https://a.tile.openstreetmap.fr/hot/{zoom}/{x}/{y}.png" },
                }
            },
            new OnlineMapsProvider("OpenTopoMap")
            {
                _types = new []
                {
                    new MapType("OpenTopoMap") { urlWithLabels = "https://a.tile.opentopomap.org/{z}/{x}/{y}.png" },
                }
            },
            new OnlineMapsProvider("OpenWeatherMap")
            {
                url = "https://tile.openweathermap.org/map/{variant}/{z}/{x}/{y}.png?appid={apikey}",
                _types = new []
                {
                    new MapType("Clouds") { variantWithoutLabels = "clouds" },
                    new MapType("CloudsClassic") { variantWithoutLabels = "clouds_cls" },
                    new MapType("Precipitation") { variantWithoutLabels = "precipitation" },
                    new MapType("PrecipitationClassic") { variantWithoutLabels = "precipitation_cls" },
                    new MapType("Rain") { variantWithoutLabels = "rain" },
                    new MapType("RainClassic") { variantWithoutLabels = "rain_cls" },
                    new MapType("Pressure") { variantWithoutLabels = "pressure" },
                    new MapType("PressureContour") { variantWithoutLabels = "pressure_cntr" },
                    new MapType("Wind") { variantWithoutLabels = "wind" },
                    new MapType("Temperature") { variantWithoutLabels = "temp" },
                    new MapType("Snow") { variantWithoutLabels = "snow" },
                },
                extraFields = new []
                {
                    new ExtraField("API key", "apikey"),
                }
            },
            new OnlineMapsProvider("Stamen")
            {
                url = "https://stamen-tiles-a.a.ssl.fastly.net/{variant}/{z}/{x}/{y}.png",
                _types = new []
                {
                    new MapType("Toner") { variantWithLabels = "toner" },
                    new MapType("TonerBackground") { variantWithoutLabels = "toner-background" },
                    new MapType("TonerHybrid") { variantWithLabels = "toner-hybrid" },
                    new MapType("TonerLines") { variantWithLabels = "toner-lines" },
                    new MapType("TonerLabels") { variantWithLabels = "toner-labels" },
                    new MapType("TonerLite") { variantWithLabels = "toner-lite" },
                    new MapType("Watercolor") { variantWithoutLabels = "watercolor" },
                }
            },
            new OnlineMapsProvider("Thunderforest")
            {
                url = "https://a.tile.thunderforest.com/{variant}/{z}/{x}/{y}.png?apikey={apikey}",
                _types = new []
                {
                    new MapType("OpenCycleMap") { variantWithLabels = "cycle" },
                    new MapType("Transport") { variantWithLabels = "transport" },
                    new MapType("TransportDark") { variantWithLabels = "transport-dark" },
                    new MapType("SpinalMap") { variantWithLabels = "spinal-map" },
                    new MapType("Landscape") { variantWithLabels = "landscape" },
                    new MapType("Outdoors") { variantWithLabels = "outdoors" },
                    new MapType("Pioneer") { variantWithLabels = "pioneer" },
                },

                extraFields = new []
                {
                    new ToggleExtraGroup("Anonymous", true, new []
                    {
                        new ExtraField("API key", "apikey", "6170aad10dfd42a38d4d8c709a536f38")
                    })
                }
            },
            new OnlineMapsProvider("TianDiTu")
            {
                _types = new []
                {
                    new MapType("Normal")
                    {
                        urlWithoutLabels = "https://t{rnd0-7}.tianditu.gov.cn/DataServer?T=vec_w&x={x}&y={y}&l={z}&tk={apikey}"
                    },
                    new MapType(SATELLITE)
                    {
                        urlWithoutLabels = "https://t{rnd0-7}.tianditu.gov.cn/DataServer?T=img_w&x={x}&y={y}&l={z}&tk={apikey}"
                    },
                    new MapType(TERRAIN)
                    {
                        urlWithoutLabels = "https://t{rnd0-7}.tianditu.gov.cn/DataServer?T=ter_w&x={x}&y={y}&l={z}&tk={apikey}"
                    },
                },

                extraFields = new []
                {
                new ToggleExtraGroup("Anonymous", true, new []
                {
                    new ExtraField("API key", "apikey", "2ce94f67e58faa24beb7cb8a09780552")
                })
            }
            },
            new OnlineMapsProvider("virtualearth", "Virtual Earth (Bing Maps)")
            {
                hasLanguage = true,
                _types = new []
                {
                    new MapType("Aerial")
                    {
                        urlWithoutLabels = "https://t{rnd0-4}.ssl.ak.tiles.virtualearth.net/tiles/a{quad}.jpeg?mkt={lng}&g=1457&n=z",
                        urlWithLabels = "https://t{rnd0-4}.ssl.ak.dynamic.tiles.virtualearth.net/comp/ch/{quad}?mkt={lng}&it=A,G,L,LA&og=30&n=z"
                    },
                    new MapType("Road")
                    {
                        urlWithLabels = "https://t{rnd0-4}.ssl.ak.dynamic.tiles.virtualearth.net/comp/ch/{quad}?mkt={lng}&it=G,VE,BX,L,LA&og=30&n=z"
                    }
                }
            },
            new OnlineMapsProvider("yandex", "Yandex Maps")
            {
                projection = new OnlineMapsProjectionWGS84(),
                _types = new []
                {
                    new MapType(MAP)
                    {
                        hasLanguage = true,
                        urlWithLabels = "https://vec0{rnd1-4}.maps.yandex.net/tiles?l=map&v=4.65.1&x={x}&y={y}&z={zoom}&scale=1&lang={lng}"
                    }, 
                    new MapType(SATELLITE)
                    {
                        urlWithoutLabels = "https://sat0{rnd1-4}.maps.yandex.net/tiles?l=sat&v=3.261.0&x={x}&y={y}&z={zoom}"
                    }, 
                }
            }, 
            new OnlineMapsProvider("Other")
            {
                _types = new []
                {
                    new MapType("AMap Satellite") { urlWithoutLabels = "https://webst02.is.autonavi.com/appmaptile?style=6&x={x}&y={y}&z={zoom}" },
                    new MapType("AMap Terrain") { urlWithLabels = "https://webrd03.is.autonavi.com/appmaptile?lang=zh_cn&size=1&scale=1&style=8&x={x}&y={y}&z={zoom}" },
                    new MapType("MtbMap") { urlWithLabels = "http://tile.mtbmap.cz/mtbmap_tiles/{z}/{x}/{y}.png" },
                    new MapType("HikeBike") { urlWithLabels = "https://a.tiles.wmflabs.org/hikebike/{z}/{x}/{y}.png" },
                    new MapType("Waze") { urlWithLabels = "https://worldtiles{rnd1-4}.waze.com/tiles/{z}/{x}/{y}.png" },
                }
            }, 
            new OnlineMapsProvider("Custom")
            {
                _types = new [] 
                {
                    new MapType("Custom") { isCustom = true }
                }
            }
        };

        for (int i = 0; i < providers.Length; i++)
        {
            OnlineMapsProvider provider = providers[i];
            provider.index = i;
            for (int j = 0; j < provider._types.Length; j++)
            {
                MapType type = provider._types[j];
                type.provider = provider;
                type.fullID = provider.id + "." + type.id;
                type.index = j;
            }
        }
    }

    /// <summary>
    /// Class of map type
    /// </summary>
    public class MapType
    {
        /// <summary>
        /// ID of map type
        /// </summary>
        public readonly string id;

        public IExtraField[] extraFields;

        public string fullID;

        /// <summary>
        /// Human-readable map type title.
        /// </summary>
        public readonly string title;

        /// <summary>
        /// Reference to provider instance.
        /// </summary>
        public OnlineMapsProvider provider;

        /// <summary>
        /// Index of map type
        /// </summary>
        public int index;

        /// <summary>
        /// Indicates that this is an custom provider.
        /// </summary>
        public bool isCustom;

        private bool hasWithoutLabels = false;
        private bool hasWithLabels = false;

        private string _ext;
        private bool? _hasLanguage;
        private bool? _hasLabels;
        private bool? _labelsEnabled;
        private string _urlWithLabels;
        private string _urlWithoutLabels;
        private bool? _useHTTP;
        private string _variantWithLabels;
        private string _variantWithoutLabels;
        private string _propWithLabels;
        private string _propWithoutLabels;
        private string _prop2;
        private bool? _logUrl;

        /// <summary>
        /// Extension. Token {ext}, that is being replaced in the URL.
        /// </summary>
        public string ext
        {
            get
            {
                if (!string.IsNullOrEmpty(_ext)) return _ext;
                if (!string.IsNullOrEmpty(provider.ext)) return provider.ext;
                return string.Empty;
            }
            set { _ext = value; }
        }

        /// <summary>
        /// Indicates that the map type supports multilanguage.
        /// </summary>
        public bool hasLanguage
        {
            get
            {
                if (_hasLanguage.HasValue) return _hasLanguage.Value;
                if (provider.hasLanguage.HasValue) return provider.hasLanguage.Value;
                return false;
            }
            set { _hasLanguage = value; }
        }

        /// <summary>
        /// Indicates that the provider supports a map with labels.
        /// </summary>
        public bool hasLabels
        {
            get
            {
                if (_hasLabels.HasValue) return _hasLabels.Value;
                if (provider.hasLabels.HasValue) return provider.hasLabels.Value;
                return false;
            }
            set { _hasLabels = value; }
        }

        /// <summary>
        /// Indicates that the label is always enabled.
        /// </summary>
        public bool labelsEnabled
        {
            get
            {
                if (_labelsEnabled.HasValue) return _labelsEnabled.Value;
                if (provider.labelsEnabled.HasValue) return provider.labelsEnabled.Value;
                return false;
            }
            set { _labelsEnabled = value; }
        }

        public bool logUrl
        {
            get
            {
                if (_logUrl.HasValue) return _logUrl.Value;
                return provider.logUrl;
            }
            set { _logUrl = value; }
        }

        /// <summary>
        /// Property. Token {prop} when label enabled, that is being replaced in the URL.
        /// </summary>
        public string propWithLabels
        {
            get
            {
                if (!string.IsNullOrEmpty(_propWithLabels)) return _propWithLabels;
                return provider.prop;
            }
            set
            {
                _propWithLabels = value;
                labelsEnabled = true;
                hasWithLabels = true;
                if (hasWithoutLabels) hasLabels = true;
            }
        }

        /// <summary>
        /// Property. Token {prop} when label disabled, that is being replaced in the URL.
        /// </summary>
        public string propWithoutLabels
        {
            get
            {
                if (!string.IsNullOrEmpty(_propWithoutLabels)) return _propWithoutLabels;
                return provider.prop;
            }
            set
            {
                _propWithoutLabels = value;
                hasWithoutLabels = true;
                if (hasWithLabels) hasLabels = true;
            }
        }

        /// <summary>
        /// Property. Token {prop2}, that is being replaced in the URL.
        /// </summary>
        public string prop2
        {
            get { return string.IsNullOrEmpty(_prop2) ? provider.prop2 : _prop2; }
            set { _prop2 = value; }
        }

        /// <summary>
        /// Variant. Token {variant}, that is being replaced in the URL.
        /// </summary>
        public string variant
        {
            set
            {
                _variantWithoutLabels = value;
                _variantWithLabels = value;
                hasLabels = true;
                hasWithLabels = true;
                hasWithoutLabels = true;
                labelsEnabled = true;
            }
        }

        /// <summary>
        /// Variant. Token {variant} when label enabled, that is being replaced in the URL.
        /// </summary>
        public string variantWithLabels
        {
            get { return _variantWithLabels; }
            set
            {
                _variantWithLabels = value;
                labelsEnabled = true;
                hasWithLabels = true;
                if (hasWithoutLabels) hasLabels = true;
            }
        }

        /// <summary>
        /// Variant. Token {variant} when label disabled, that is being replaced in the URL.
        /// </summary>
        public string variantWithoutLabels
        {
            get { return _variantWithoutLabels; }
            set
            {
                _variantWithoutLabels = value;
                hasWithoutLabels = true;
                if (hasWithLabels) hasLabels = true;
            }
        }

        /// <summary>
        /// Gets / sets the URL pattern of tiles with labels.
        /// </summary>
        public string urlWithLabels
        {
            get { return _urlWithLabels; }
            set
            {
                _urlWithLabels = value;
                labelsEnabled = true;
                hasWithLabels = true;
                if (hasWithoutLabels) hasLabels = true;
                if (!value.StartsWith("https")) _useHTTP = true;
            }
        }

        /// <summary>
        /// Gets / sets the URL pattern of tiles without labels.
        /// </summary>
        public string urlWithoutLabels
        {
            get { return _urlWithoutLabels; }
            set
            {
                _urlWithoutLabels = value;
                hasWithoutLabels = true;
                if (hasWithLabels) hasLabels = true;
                if (!value.StartsWith("https")) _useHTTP = true;
            }
        }

        /// <summary>
        /// Indicates that the map type uses HTTP.
        /// </summary>
        public bool useHTTP
        {
            get
            {
                if (_useHTTP.HasValue) return _useHTTP.Value;
                if (provider.useHTTP.HasValue) return provider.useHTTP.Value;
                return false;
            }
            set { _useHTTP = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">Human-readable map type title.</param>
        public MapType(string title):this(title.ToLower(), title)
        {
            
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id">ID of map type.</param>
        /// <param name="title">Human-readable map type title.</param>
        public MapType(string id, string title)
        {
            this.id = id;
            this.title = title;
        }

        public string GetSettings()
        {
            if (provider.extraFields == null && extraFields == null) return null;

            StringBuilder builder = new StringBuilder();
            if (extraFields != null) foreach (IExtraField field in extraFields) field.SaveSettings(builder);
            if (provider.extraFields != null) foreach (IExtraField field in provider.extraFields) field.SaveSettings(builder);
            return builder.ToString();
        }

        /// <summary>
        /// Gets the URL to download the tile texture
        /// </summary>
        /// <param name="tile">Instence of tile</param>
        /// <returns>URL to tile texture</returns>
        public string GetURL(OnlineMapsTile tile)
        {
            OnlineMapsRasterTile rTile = tile as OnlineMapsRasterTile;
            bool useLabels = hasLabels ? rTile.labels : labelsEnabled;
            if (useLabels)
            {
                if (!string.IsNullOrEmpty(_urlWithLabels)) return GetURL(tile, _urlWithLabels, true);
                if (!string.IsNullOrEmpty(provider.url)) return GetURL(tile, provider.url, true);
                return GetURL(tile, _urlWithoutLabels, false);
            }

            if (!string.IsNullOrEmpty(_urlWithoutLabels)) return GetURL(tile, _urlWithoutLabels, false);
            if (!string.IsNullOrEmpty(provider.url)) return GetURL(tile, provider.url, false);
            return GetURL(tile, _urlWithLabels, true);
        }

        private string GetURL(OnlineMapsTile tile, string url, bool labels)
        {
            url = Regex.Replace(url, @"{\w+}", delegate(Match match)
            {
                string v = match.Value.ToLower().Trim('{', '}');

                if (OnlineMapsTile.OnReplaceURLToken != null)
                {
                    string ret = OnlineMapsTile.OnReplaceURLToken(tile, v);
                    if (ret != null) return ret;
                }

                if (v == "zoom") return tile.zoom.ToString();
                if (v == "z") return tile.zoom.ToString();
                if (v == "x") return tile.x.ToString();
                if (v == "y") return tile.y.ToString();
                if (v == "quad") return OnlineMapsUtils.TileToQuadKey(tile.x, tile.y, tile.zoom);
                if (v == "lng") return (tile as OnlineMapsRasterTile).language;
                if (v == "ext") return ext;
                if (v == "prop") return labels ? propWithLabels : propWithoutLabels;
                if (v == "prop2") return prop2;
                if (v == "variant") return labels ? variantWithLabels : variantWithoutLabels;
                if (TryUseExtraFields(ref v)) return v;
                return v;
            });
            url = Regex.Replace(url, @"{rnd(\d+)-(\d+)}", delegate(Match match)
            {
                int v1 = int.Parse(match.Groups[1].Value);
                int v2 = int.Parse(match.Groups[2].Value);
                return Random.Range(v1, v2 + 1).ToString();
            });
            if (logUrl) Debug.Log(url);
            return url;
        }

        public void LoadSettings(string settings)
        {
            if (string.IsNullOrEmpty(settings)) return;

            TryLoadExtraFields(settings, extraFields);
            TryLoadExtraFields(settings, provider.extraFields);
        }

        public override string ToString()
        {
            return fullID;
        }

        private void TryLoadExtraFields(string settings, IExtraField[] fields)
        {
            if (fields == null) return;

            int i = 0;
            while (i < settings.Length)
            {
                int titleLength = int.Parse(settings.Substring(i, 2));
                i += 2;
                string title = settings.Substring(i, titleLength);
                i += titleLength;

                int contentLengthSize = int.Parse(settings.Substring(i, 1));
                i++;
                int contentSize = int.Parse(settings.Substring(i, contentLengthSize));
                i += contentLengthSize;

                foreach (IExtraField field in fields) if (field.TryLoadSettings(title, settings, i, contentSize)) break;
                i += contentSize;
            }
        }

        public bool TryUseExtraFields(ref string token)
        {
            if (extraFields != null)
            {
                foreach (IExtraField field in extraFields)
                {
                    string value;
                    if (field.GetTokenValue(token, false, out value))
                    {
                        token = value;
                        return true;
                    }
                }
            }
            if (provider.extraFields != null)
            {
                foreach (IExtraField field in provider.extraFields)
                {
                    string value;
                    if (field.GetTokenValue(token, false, out value))
                    {
                        token = value;
                        return true;
                    }
                }
            }

            if (provider.OnReplaceToken != null)
            {
                string value = provider.OnReplaceToken(this, token);
                if (value != null)
                {
                    token = value;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Interface for extra fields tile provider
    /// </summary>
    public interface IExtraField
    {
        bool GetTokenValue(string token, bool useDefaultValue, out string value);
        void SaveSettings(StringBuilder builder);
        bool TryLoadSettings(string title, string settings, int index, int contentSize);
    }

    /// <summary>
    /// Class for extra field
    /// </summary>
    public class ExtraField: IExtraField
    {
        /// <summary>
        /// Title
        /// </summary>
        public string title;

        /// <summary>
        /// Value
        /// </summary>
        public string value;

        /// <summary>
        /// Default value
        /// </summary>
        public string defaultValue;

        /// <summary>
        /// Token (ID)
        /// </summary>
        public string token;

        public ExtraField(string title, string token)
        {
            this.title = title;
            this.token = token;
        }

        public ExtraField(string title, string token, string defaultValue):this(title, token)
        {
            value = this.defaultValue = defaultValue;
        }

        public bool GetTokenValue(string token, bool useDefaultValue, out string value)
        {
            value = null;

            if (this.token == token)
            {
                value = useDefaultValue? defaultValue: this.value;
                return true;
            }
            return false;
        }

        public void SaveSettings(StringBuilder builder)
        {
            int titleLength = title.Length;
            if (titleLength < 10) builder.Append("0");
            builder.Append(titleLength);
            builder.Append(title);

            if (string.IsNullOrEmpty(value)) builder.Append(1).Append(1).Append(0);
            else
            {
                StringBuilder dataBuilder = new StringBuilder();
                int valueLength = value.Length;
                dataBuilder.Append(valueLength.ToString().Length);
                dataBuilder.Append(valueLength);
                dataBuilder.Append(value);
                builder.Append(dataBuilder.Length.ToString().Length);
                builder.Append(dataBuilder.Length);
                builder.Append(dataBuilder);
            }
        }

        public bool TryLoadSettings(string title, string settings, int index, int contentSize)
        {
            if (this.title != title) return false;

            int lengthSize = int.Parse(settings.Substring(index, 1));
            if (lengthSize == 0) value = "";
            else
            {
                index++;
                int length = int.Parse(settings.Substring(index, lengthSize));
                index += lengthSize;
                value = settings.Substring(index, length);
            }

            return true;
        }
    }

    /// <summary>
    /// Group of toggle extra fields
    /// </summary>
    public class ToggleExtraGroup: IExtraField
    {
        /// <summary>
        /// Array of extra fields
        /// </summary>
        public IExtraField[] fields;

        /// <summary>
        /// Group title
        /// </summary>
        public string title;

        /// <summary>
        /// Group value
        /// </summary>
        public bool value = false;

        /// <summary>
        /// Group ID
        /// </summary>
        public string id;

        public ToggleExtraGroup(string title, bool value = false)
        {
            this.title = title;
            this.value = value;
        }

        public ToggleExtraGroup(string title, bool value, IExtraField[] fields): this(title, value)
        {
            this.fields = fields;
        }

        public bool GetTokenValue(string token, bool useDefaultValue, out string value)
        {
            value = null;
            if (fields == null) return false;

            foreach (IExtraField field in fields)
            {
                if (field.GetTokenValue(token, this.value || useDefaultValue, out value)) return true;
            }
            return false;
        }

        public void SaveSettings(StringBuilder builder)
        {
            int titleLength = title.Length;
            if (titleLength < 10) builder.Append("0");
            builder.Append(titleLength);
            builder.Append(title);

            StringBuilder dataBuilder = new StringBuilder();
            dataBuilder.Append(value ? 1 : 0);

            if (fields != null) foreach (IExtraField field in fields) field.SaveSettings(dataBuilder);

            builder.Append(dataBuilder.Length.ToString().Length);
            builder.Append(dataBuilder.Length);
            builder.Append(dataBuilder);
        }

        public bool TryLoadSettings(string title, string settings, int index, int contentSize)
        {
            if (this.title != title) return false;

            value = settings.Substring(index, 1) == "1";

            int i = index + 1;
            while (i < index + contentSize)
            {
                int titleLength = int.Parse(settings.Substring(i, 2));
                i += 2;
                string fieldTitle = settings.Substring(i, titleLength);
                i += titleLength;

                int contentLengthSize = int.Parse(settings.Substring(i, 1));
                i++;
                int contentLength = int.Parse(settings.Substring(i, contentLengthSize));
                i += contentLengthSize;

                foreach (IExtraField field in fields) if (field.TryLoadSettings(fieldTitle, settings, i, contentLength)) break;

                i += contentLength;
            }

            return true;
        }
    }
}