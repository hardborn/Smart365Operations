
namespace Feeling.GIS.Map.Core
{
    /// <summary>
    /// 地图数据类型
    /// </summary>
    public enum MapType
    {
        None = 0,

        FeelingMap = 1,

        GoogleMap = 22,
        GoogleSatellite = 4,
        GoogleLabels = 8,
        GoogleTerrain = 16,
        GoogleHybrid = 20,

        GoogleMapChina = 2,
        GoogleSatelliteChina = 24,
        GoogleLabelsChina = 26,
        GoogleTerrainChina = 28,
        GoogleHybridChina = 29,

        OpenStreetMap = 32,
        OpenStreetOsm = 33,
        OpenStreetMapSurfer = 34,
        OpenStreetMapSurferTerrain = 35,
        OpenSeaMapLabels = 36,
        OpenSeaMapHybrid = 37,
        OpenCycleMap = 38,

        YahooMap = 64,
        YahooSatellite = 128,
        YahooLabels = 256,
        YahooHybrid = 333,

        BingMap = 444,
        BingMap_New = 455,
        BingSatellite = 555,
        BingHybrid = 666,

        ArcGIS_StreetMap_World_2D = 777,
        ArcGIS_Imagery_World_2D = 788,
        ArcGIS_ShadedRelief_World_2D = 799,
        ArcGIS_Topo_US_2D = 811,

        ArcGIS_World_Physical_Map = 822,
        ArcGIS_World_Shaded_Relief = 833,
        ArcGIS_World_Street_Map = 844,
        ArcGIS_World_Terrain_Base = 855,
        ArcGIS_World_Topo_Map = 866,
    }
}
