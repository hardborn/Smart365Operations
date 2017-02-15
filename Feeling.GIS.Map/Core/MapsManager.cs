
namespace Feeling.GIS.Map.Core
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml.Serialization;
    using System.Data.Common;
    using System.Xml;


    using System.Data.SQLite;

    /// <summary>
    /// maps manager
    /// </summary>
    public class MapsManager : Singleton<MapsManager>
    {

        public string CurrentMapName = "西安市地图";

        public string MapServer = "hostlocal";


        // Google version strings
        public string VersionGoogleMap = "m@142";
        public string VersionGoogleSatellite = "76";
        public string VersionGoogleLabels = "h@142";
        public string VersionGoogleTerrain = "t@126,r@142";
        public string SecGoogleWord = "Galileo";

        // Google (China) version strings
        public string VersionGoogleMapChina = "m@142";
        public string VersionGoogleSatelliteChina = "s@76";
        public string VersionGoogleLabelsChina = "h@142";
        public string VersionGoogleTerrainChina = "t@126,r@142";

        /// <summary>
        /// Google Maps API generated using http://greatmaps.codeplex.com/
        /// from http://code.google.com/intl/en-us/apis/maps/signup.html
        /// </summary>
        public string GoogleMapsAPIKey = @"ABQIAAAAWaQgWiEBF3lW97ifKnAczhRAzBk5Igf8Z5n2W3hNnMT0j2TikxTLtVIGU7hCLLHMAuAMt-BO5UrEWA";

        // Yahoo version strings
        public string VersionYahooMap = "4.3";
        public string VersionYahooSatellite = "1.9";
        public string VersionYahooLabels = "4.3";

        // BingMaps
        public string VersionBingMaps = "631";

        /// <summary>
        /// Bing Maps Customer Identification, more info here
        /// http://msdn.microsoft.com/en-us/library/bb924353.aspx
        /// </summary>
        public string BingMapsClientToken = null;

        readonly string[] levelsForSigPacSpainMap = {"0", "1", "2", "3", "4", 
                          "MTNSIGPAC", 
                          "MTN2000", "MTN2000", "MTN2000", "MTN2000", "MTN2000", 
                          "MTN200", "MTN200", "MTN200", 
                          "MTN25", "MTN25",
                          "ORTOFOTOS","ORTOFOTOS","ORTOFOTOS","ORTOFOTOS"};

        public string Server_PergoTurkeyMap = "map{0}.pergo.com.tr";

        public string UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7";

        public int Timeout = 30 * 1000;

        public IWebProxy Proxy;

        public AccessMode Mode = AccessMode.ServerAndCache;

        internal string LanguageStr;
        LanguageType language = LanguageType.ChineseSimplified;

        public LanguageType Language
        {
            get
            {
                return language;
            }
            set
            {
                language = value;
                LanguageStr = Stuff.EnumToString(Language);
            }
        }

        public bool UseRouteCache = true;

        public bool UseGeocoderCache = true;

        public bool UsePlacemarkCache = true;

        /// <summary>
        /// 是否使用内存缓存
        /// </summary>
        public bool UseMemoryCache = true;

        /// <summary>
        /// 地图最大缩放级别
        /// </summary>
        public readonly int MaxZoom = 17;

        public double EarthRadiusKm = 6378.137; // WGS-84

        public PureImageCache ImageCacheLocal
        {
            get
            {
                return Cache.Instance.ImageCache;
            }
            set
            {
                Cache.Instance.ImageCache = value;
            }
        }

        public PureImageCache ImageCacheSecond
        {
            get
            {
                return Cache.Instance.ImageCacheSecond;
            }
            set
            {
                Cache.Instance.ImageCacheSecond = value;
            }
        }

        public PureImageProxy ImageProxy;

        /// <summary>
        /// 是否随机顺序装载地图
        /// </summary>
        public bool ShuffleTilesOnLoad = true;

        /// <summary>
        /// 瓦片缓存队列
        /// </summary>
        readonly Queue<CacheItemQueue> tileCacheQueue = new Queue<CacheItemQueue>();

        /// <summary>
        ///内存中的瓦片
        /// </summary>
        internal readonly KiberTileCache TilesInMemory = new KiberTileCache();

        internal readonly FastReaderWriterLock kiberCacheLock = new FastReaderWriterLock();

        /// <summary>
        /// 内存缓存大小。默认: 22MB
        /// </summary>
        public int MemoryCacheCapacity
        {
            get
            {
                kiberCacheLock.AcquireReaderLock();
                try
                {
                    return TilesInMemory.MemoryCacheCapacity;
                }
                finally
                {
                    kiberCacheLock.ReleaseReaderLock();
                }
            }
            set
            {
                kiberCacheLock.AcquireWriterLock();
                try
                {
                    TilesInMemory.MemoryCacheCapacity = value;
                }
                finally
                {
                    kiberCacheLock.ReleaseWriterLock();
                }
            }
        }

        public double MemoryCacheSize
        {
            get
            {
                kiberCacheLock.AcquireReaderLock();
                try
                {
                    return TilesInMemory.MemoryCacheSize;
                }
                finally
                {
                    kiberCacheLock.ReleaseReaderLock();
                }
            }
        }

        bool? isRunningOnMono;

        public bool IsRunningOnMono
        {
            get
            {
                if (!isRunningOnMono.HasValue)
                {
                    try
                    {
                        isRunningOnMono = (Type.GetType("Mono.Runtime") != null);
                        return isRunningOnMono.Value;
                    }
                    catch
                    {
                    }
                }
                else
                {
                    return isRunningOnMono.Value;
                }
                return false;
            }
        }

        bool IsCorrectedGoogleVersions = false;

        bool IsCorrectedBingVersions = false;

        /// <summary>
        /// 缓存工作线程
        /// </summary>
        Thread CacheEngine;
        AutoResetEvent WaitForCache = new AutoResetEvent(false);

        public MapsManager()
        {
            #region 唯一实例检查
            if (Instance != null)
            {
                throw (new Exception("试图创建已存在的新的唯一实例类。请使用 \"class.Instance\" 取代 \"new class()\""));
            }
            #endregion

            Language = LanguageType.ChineseSimplified;
            ServicePointManager.DefaultConnectionLimit = 444;

            Proxy = WebRequest.DefaultWebProxy;

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object obj)
               {
                   TryCorrectGoogleVersions();
                   TryCorrectBingVersions();
               }));
        }

        #region -- Stuff --

        MemoryStream GetTileFromMemoryCache(RawTile tile)
        {
            kiberCacheLock.AcquireReaderLock();
            try
            {
                MemoryStream ret = null;
                if (TilesInMemory.TryGetValue(tile, out ret))
                {
                    return ret;
                }
            }
            finally
            {
                kiberCacheLock.ReleaseReaderLock();
            }
            return null;
        }

        void AddTileToMemoryCache(RawTile tile, MemoryStream data)
        {
            kiberCacheLock.AcquireWriterLock();
            try
            {
                if (!TilesInMemory.ContainsKey(tile))
                {
                    TilesInMemory.Add(tile, Stuff.CopyStream(data, true));
                }
            }
            finally
            {
                kiberCacheLock.ReleaseWriterLock();
            }
        }

        public MapType[] GetAllLayersOfType(MapType type)
        {
            MapType[] types = null;
            {
                switch (type)
                {
                    case MapType.GoogleHybrid:
                        {
                            types = new MapType[2];
                            types[0] = MapType.GoogleSatellite;
                            types[1] = MapType.GoogleLabels;
                        }
                        break;

                    case MapType.GoogleHybridChina:
                        {
                            types = new MapType[2];
                            types[0] = MapType.GoogleSatelliteChina;
                            types[1] = MapType.GoogleLabelsChina;
                        }
                        break;


                    case MapType.YahooHybrid:
                        {
                            types = new MapType[2];
                            types[0] = MapType.YahooSatellite;
                            types[1] = MapType.YahooLabels;
                        }
                        break;


                    case MapType.OpenSeaMapHybrid:
                        {
                            types = new MapType[2];
                            types[0] = MapType.OpenStreetMap;
                            types[1] = MapType.OpenSeaMapLabels;
                        }
                        break;
                    default:
                        {
                            types = new MapType[1];
                            types[0] = type;
                        }
                        break;
                }
            }

            return types;
        }

        public void AdjustProjection(MapType type, ref PureProjection Projection, out int maxZoom)
        {
            maxZoom = MaxZoom;

            switch (type)
            {

                case MapType.ArcGIS_World_Physical_Map:
                    {
                        if (false == (Projection is MercatorProjection))
                        {
                            Projection = new MercatorProjection();
                        }
                        maxZoom = 8;
                    }
                    break;

                case MapType.ArcGIS_World_Shaded_Relief:
                case MapType.ArcGIS_World_Terrain_Base:
                    {
                        if (false == (Projection is MercatorProjection))
                        {
                            Projection = new MercatorProjection();
                        }
                        maxZoom = 13;
                    }
                    break;

                case MapType.OpenStreetMapSurfer:
                case MapType.OpenStreetMapSurferTerrain:
                case MapType.ArcGIS_World_Topo_Map:
                    {
                        if (false == (Projection is MercatorProjection))
                        {
                            Projection = new MercatorProjection();
                        }
                        maxZoom = 19;
                    }
                    break;

                default:
                    {
                        if (false == (Projection is MercatorProjection))
                        {
                            Projection = new MercatorProjection();
                            maxZoom = MapsManager.Instance.MaxZoom;
                        }
                    }
                    break;
            }
        }

        public double GetDistance(PointLatLng p1, PointLatLng p2)
        {
            double dLat1InRad = p1.Lat * (Math.PI / 180);
            double dLong1InRad = p1.Lng * (Math.PI / 180);
            double dLat2InRad = p2.Lat * (Math.PI / 180);
            double dLong2InRad = p2.Lng * (Math.PI / 180);
            double dLongitude = dLong2InRad - dLong1InRad;
            double dLatitude = dLat2InRad - dLat1InRad;
            double a = Math.Pow(Math.Sin(dLatitude / 2), 2) + Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) * Math.Pow(Math.Sin(dLongitude / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double dDistance = EarthRadiusKm * c;
            return dDistance;
        }

        public double GetBearing(PointLatLng p1, PointLatLng p2)
        {
            var latitude1 = ToRadian(p1.Lat);
            var latitude2 = ToRadian(p2.Lat);
            var longitudeDifference = ToRadian(p2.Lng - p1.Lng);

            var y = Math.Sin(longitudeDifference) * Math.Cos(latitude2);
            var x = Math.Cos(latitude1) * Math.Sin(latitude2) - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitudeDifference);

            return (ToDegree(Math.Atan2(y, x)) + 360) % 360;
        }

        public static Double ToRadian(Double degree)
        {
            return (degree * Math.PI / 180.0);
        }

        public static Double ToDegree(Double radian)
        {
            return (radian / Math.PI * 180.0);
        }

        /// <summary>
        /// 获取两点的最短路径
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="avoidHighways"></param>
        /// <param name="Zoom"></param>
        /// <returns></returns>
        public MapRoute GetRouteBetweenPoints(PointLatLng start, PointLatLng end, bool avoidHighways, int Zoom)
        {
            string tooltip;
            int numLevels;
            int zoomFactor;
            MapRoute ret = null;
            List<PointLatLng> points = GetRouteBetweenPointsUrl(MakeRouteUrl(start, end, LanguageStr, avoidHighways), Zoom, UseRouteCache, out tooltip, out numLevels, out zoomFactor);
            if (points != null)
            {
                ret = new MapRoute(points, tooltip);
            }
            return ret;
        }

        /// <summary>
        /// 获取两点的最短路径
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="avoidHighways"></param>
        /// <param name="Zoom"></param>
        /// <returns></returns>
        public MapRoute GetRouteBetweenPoints(string start, string end, bool avoidHighways, int Zoom)
        {
            string tooltip;
            int numLevels;
            int zoomFactor;
            MapRoute ret = null;
            List<PointLatLng> points = GetRouteBetweenPointsUrl(MakeRouteUrl(start, end, LanguageStr, avoidHighways), Zoom, UseRouteCache, out tooltip, out numLevels, out zoomFactor);
            if (points != null)
            {
                ret = new MapRoute(points, tooltip);
            }
            return ret;
        }

      
        public MapRoute GetWalkingRouteBetweenPoints(PointLatLng start, PointLatLng end, int Zoom)
        {
            string tooltip;
            int numLevels;
            int zoomFactor;
            MapRoute ret = null;
            List<PointLatLng> points = GetRouteBetweenPointsUrl(MakeWalkingRouteUrl(start, end, LanguageStr), Zoom, UseRouteCache, out tooltip, out numLevels, out zoomFactor);
            if (points != null)
            {
                ret = new MapRoute(points, tooltip);
            }
            return ret;
        }

     
        public MapRoute GetWalkingRouteBetweenPoints(string start, string end, int Zoom)
        {
            string tooltip;
            int numLevels;
            int zoomFactor;
            MapRoute ret = null;
            List<PointLatLng> points = GetRouteBetweenPointsUrl(MakeWalkingRouteUrl(start, end, LanguageStr), Zoom, UseRouteCache, out tooltip, out numLevels, out zoomFactor);
            if (points != null)
            {
                ret = new MapRoute(points, tooltip);
            }
            return ret;
        }

 
        public PointLatLng? GetLatLngFromGeocoder(string keywords, out GeoCoderStatusCode status)
        {
            return GetLatLngFromGeocoderUrl(MakeGeocoderUrl(keywords, LanguageStr), UseGeocoderCache, out status);
        }

        public Placemark GetPlacemarkFromGeocoder(PointLatLng location)
        {
            return GetPlacemarkFromReverseGeocoderUrl(MakeReverseGeocoderUrl(location, LanguageStr), UsePlacemarkCache);
        }


        public bool ExportToGMDB(string file)
        {
#if SQLite
            if (Cache.Instance.ImageCache is SQLitePureImageCache)
            {
                StringBuilder db = new StringBuilder((Cache.Instance.ImageCache as SQLitePureImageCache).GtileCache);
                db.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}Data.gmdb", MapsManager.Instance.LanguageStr, Path.DirectorySeparatorChar);

                return SQLitePureImageCache.ExportMapDataToDB(db.ToString(), file);
            }
#endif
            return false;
        }


        public bool ImportFromGMDB(string file)
        {
#if SQLite
            if (Cache.Instance.ImageCache is SQLitePureImageCache)
            {
                StringBuilder db = new StringBuilder((Cache.Instance.ImageCache as SQLitePureImageCache).GtileCache);
                db.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}Data.gmdb", MapsManager.Instance.LanguageStr, Path.DirectorySeparatorChar);

                return SQLitePureImageCache.ExportMapDataToDB(file, db.ToString());
            }
#endif
            return false;
        }

#if SQLite
        public bool OptimizeMapDb(string file)
        {
            if (Cache.Instance.ImageCache is SQLitePureImageCache)
            {
                if (string.IsNullOrEmpty(file))
                {
                    StringBuilder db = new StringBuilder((Cache.Instance.ImageCache as SQLitePureImageCache).GtileCache);
                    db.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}Data.gmdb", MapsManager.Instance.LanguageStr, Path.DirectorySeparatorChar);

                    return SQLitePureImageCache.VacuumDb(db.ToString());
                }
                else
                {
                    return SQLitePureImageCache.VacuumDb(file);
                }
            }

            return false;
        }
#endif

        void EnqueueCacheTask(CacheItemQueue task)
        {
            lock (tileCacheQueue)
            {
                if (!tileCacheQueue.Contains(task))
                {
                    Debug.WriteLine("EnqueueCacheTask: " + task.Pos.ToString());

                    tileCacheQueue.Enqueue(task);

                    if (CacheEngine != null && CacheEngine.IsAlive)
                    {
                        WaitForCache.Set();
                    }

                    else if (CacheEngine == null || CacheEngine.ThreadState == System.Threading.ThreadState.Stopped || CacheEngine.ThreadState == System.Threading.ThreadState.Unstarted)
                    {
                        CacheEngine = null;
                        CacheEngine = new Thread(new ThreadStart(CacheEngineLoop));
                        CacheEngine.Name = "MapTile CacheEngine";
                        CacheEngine.IsBackground = false;
                        CacheEngine.Priority = ThreadPriority.Lowest;
                        CacheEngine.Start();
                    }
                }
            }
        }

        void CacheEngineLoop()
        {
            Debug.WriteLine("CacheEngine: start");
            while (true)
            {
                try
                {
                    CacheItemQueue? task = null;

                    lock (tileCacheQueue)
                    {
                        if (tileCacheQueue.Count > 0)
                        {
                            task = tileCacheQueue.Dequeue();
                        }
                    }

                    if (task.HasValue)
                    {
                        if (task.Value.Img != null && task.Value.Img.CanRead)
                        {
                            if ((task.Value.CacheType & CacheUsage.First) == CacheUsage.First && ImageCacheLocal != null)
                            {
                                ImageCacheLocal.PutImageToCache(task.Value.Img, task.Value.Type, task.Value.Pos, task.Value.Zoom);
                            }

                            if ((task.Value.CacheType & CacheUsage.Second) == CacheUsage.Second && ImageCacheSecond != null)
                            {
                                ImageCacheSecond.PutImageToCache(task.Value.Img, task.Value.Type, task.Value.Pos, task.Value.Zoom);
                            }

                            Thread.Sleep(44);
                        }
                        else
                        {
                            Debug.WriteLine("CacheEngineLoop: -> " + task.Value);
                        }
                    }
                    else
                    {
                        if (!WaitForCache.WaitOne(4444, false))
                        {
                            break;
                        }
                    }
                }
                catch (AbandonedMutexException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CacheEngineLoop: " + ex.ToString());
                }
            }
            Debug.WriteLine("CacheEngine: stop");
        }

        class StringWriterExt : StringWriter
        {
            public StringWriterExt(IFormatProvider info)
                : base(info)
            {

            }

            public override Encoding Encoding
            {
                get
                {
                    return Encoding.UTF8;
                }
            }
        }


        #endregion

        #region -- URL generation --

        /// <summary>
        ///生成图片URL
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="zoom"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        internal string MakeImageUrl(MapType type, MapPoint pos, int zoom, string language)
        {
            switch (type)
            {
                #region -- Feeling --
                case MapType.FeelingMap:
                    {
                        string str6 = string.Format("gm_{0}_{1}_{2}.png", pos.X, pos.Y, zoom);
                        return string.Format("{0}/{1}/{2}/{3}", new object[] { this.MapServer, this.CurrentMapName, zoom, str6 });

                    }

                #endregion

                #region -- Google --
                case MapType.GoogleMap:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt1.google.com/vt/lyrs=m@130&hl=lt&x=18683&s=&y=10413&z=15&s=Galile

                        return string.Format("http://{0}{1}.google.com/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleMap, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleSatellite:
                    {
                        string server = "khm";
                        string request = "kh";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        return string.Format("http://{0}{1}.google.com/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleSatellite, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleLabels:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt1.google.com/vt/lyrs=h@107&hl=lt&x=583&y=325&z=10&s=Ga
                        // http://mt0.google.com/vt/lyrs=h@130&hl=lt&x=1166&y=652&z=11&s=Galile

                        return string.Format("http://{0}{1}.google.com/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleLabels, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleTerrain:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        return string.Format("http://{0}{1}.google.com/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleTerrain, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }
                #endregion

                #region -- Google (China) version --
                case MapType.GoogleMapChina:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt3.google.cn/vt/lyrs=m@123&hl=zh-CN&gl=cn&x=3419&y=1720&z=12&s=G

                        return string.Format("http://{0}{1}.google.cn/{2}/lyrs={3}&hl={4}&gl=cn&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleMapChina, "zh-CN", pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleSatelliteChina:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt1.google.cn/vt/lyrs=s@59&gl=cn&x=3417&y=1720&z=12&s=Gal

                        return string.Format("http://{0}{1}.google.cn/{2}/lyrs={3}&gl=cn&x={4}{5}&y={6}&z={7}&s={8}", server, GetServerNum(pos, 4), request, VersionGoogleSatelliteChina, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleLabelsChina:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt1.google.cn/vt/imgtp=png32&lyrs=h@123&hl=zh-CN&gl=cn&x=3417&y=1720&z=12&s=Gal

                        return string.Format("http://{0}{1}.google.cn/{2}/imgtp=png32&lyrs={3}&hl={4}&gl=cn&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleLabelsChina, "zh-CN", pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleTerrainChina:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt2.google.cn/vt/lyrs=t@108,r@123&hl=zh-CN&gl=cn&x=3418&y=1718&z=12&s=Gali

                        return string.Format("http://{0}{1}.google.com/{2}/lyrs={3}&hl={4}&gl=cn&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleTerrainChina, "zh-CN", pos.X, sec1, pos.Y, zoom, sec2);
                    }
                #endregion

                #region -- Yahoo --
                case MapType.YahooMap:
                    {
                        // http://maps1.yimg.com/hx/tl?b=1&v=4.3&.intl=en&x=12&y=7&z=7&r=1

                        return string.Format("http://maps{0}.yimg.com/hx/tl?v={1}&.intl={2}&x={3}&y={4}&z={5}&r=1", ((GetServerNum(pos, 2)) + 1), VersionYahooMap, language, pos.X, (((1 << zoom) >> 1) - 1 - pos.Y), (zoom + 1));
                    }

                case MapType.YahooSatellite:
                    {
                        // http://maps3.yimg.com/ae/ximg?v=1.9&t=a&s=256&.intl=en&x=15&y=7&z=7&r=1

                        return string.Format("http://maps{0}.yimg.com/ae/ximg?v={1}&t=a&s=256&.intl={2}&x={3}&y={4}&z={5}&r=1", 3, VersionYahooSatellite, language, pos.X, (((1 << zoom) >> 1) - 1 - pos.Y), (zoom + 1));
                    }

                case MapType.YahooLabels:
                    {
                        // http://maps1.yimg.com/hx/tl?b=1&v=4.3&t=h&.intl=en&x=14&y=5&z=7&r=1

                        return string.Format("http://maps{0}.yimg.com/hx/tl?v={1}&t=h&.intl={2}&x={3}&y={4}&z={5}&r=1", 1, VersionYahooLabels, language, pos.X, (((1 << zoom) >> 1) - 1 - pos.Y), (zoom + 1));
                    }
                #endregion

                #region -- OpenStreet --
                case MapType.OpenStreetMap:
                    {
                        char letter = "abc"[GetServerNum(pos, 3)];
                        return string.Format("http://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png", letter, zoom, pos.X, pos.Y);
                    }

                case MapType.OpenStreetOsm:
                    {
                        char letter = "abc"[GetServerNum(pos, 3)];
                        return string.Format("http://{0}.tah.openstreetmap.org/Tiles/tile/{1}/{2}/{3}.png", letter, zoom, pos.X, pos.Y);
                    }

                case MapType.OpenCycleMap:
                    {
                        //http://b.tile.opencyclemap.org/cycle/13/4428/2772.png

                        char letter = "abc"[GetServerNum(pos, 3)];
                        return string.Format("http://{0}.tile.opencyclemap.org/cycle/{1}/{2}/{3}.png", letter, zoom, pos.X, pos.Y);
                    }

                case MapType.OpenStreetMapSurfer:
                    {
                        // http://tiles1.mapsurfer.net/tms_r.ashx?x=37378&y=20826&z=16

                        return string.Format("http://tiles1.mapsurfer.net/tms_r.ashx?x={0}&y={1}&z={2}", pos.X, pos.Y, zoom);
                    }

                case MapType.OpenStreetMapSurferTerrain:
                    {
                        // http://tiles2.mapsurfer.net/tms_t.ashx?x=9346&y=5209&z=14

                        return string.Format("http://tiles2.mapsurfer.net/tms_t.ashx?x={0}&y={1}&z={2}", pos.X, pos.Y, zoom);
                    }

                case MapType.OpenSeaMapLabels:
                    {
                        // http://tiles.openseamap.org/seamark/15/17481/10495.png

                        return string.Format("http://tiles.openseamap.org/seamark/{0}/{1}/{2}.png", zoom, pos.X, pos.Y);
                    }
                #endregion

                #region -- Bing --
                case MapType.BingMap:
                    {
                        string key = TileXYToQuadKey(pos.X, pos.Y, zoom);
                        return string.Format("http://ecn.t{0}.tiles.virtualearth.net/tiles/r{1}.png?g={2}&mkt={3}{4}", GetServerNum(pos, 4), key, VersionBingMaps, language, (!string.IsNullOrEmpty(BingMapsClientToken) ? "&token=" + BingMapsClientToken : string.Empty));
                    }

                case MapType.BingMap_New:
                    {
                        // http://ecn.t3.tiles.virtualearth.net/tiles/r12030012020233?g=559&mkt=en-us&lbl=l1&stl=h&shading=hill&n=z

                        string key = TileXYToQuadKey(pos.X, pos.Y, zoom);
                        return string.Format("http://ecn.t{0}.tiles.virtualearth.net/tiles/r{1}.png?g={2}&mkt={3}{4}&lbl=l1&stl=h&shading=hill&n=z", GetServerNum(pos, 4), key, VersionBingMaps, language, (!string.IsNullOrEmpty(BingMapsClientToken) ? "&token=" + BingMapsClientToken : string.Empty));
                    }

                case MapType.BingSatellite:
                    {
                        string key = TileXYToQuadKey(pos.X, pos.Y, zoom);
                        return string.Format("http://ecn.t{0}.tiles.virtualearth.net/tiles/a{1}.jpeg?g={2}&mkt={3}{4}", GetServerNum(pos, 4), key, VersionBingMaps, language, (!string.IsNullOrEmpty(BingMapsClientToken) ? "&token=" + BingMapsClientToken : string.Empty));
                    }

                case MapType.BingHybrid:
                    {
                        string key = TileXYToQuadKey(pos.X, pos.Y, zoom);
                        return string.Format("http://ecn.t{0}.tiles.virtualearth.net/tiles/h{1}.jpeg?g={2}&mkt={3}{4}", GetServerNum(pos, 4), key, VersionBingMaps, language, (!string.IsNullOrEmpty(BingMapsClientToken) ? "&token=" + BingMapsClientToken : string.Empty));
                    }
                #endregion

                #region -- ArcGIS --
                case MapType.ArcGIS_StreetMap_World_2D:
                    {
                        // http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_StreetMap_World_2D/MapServer/tile/0/0/0.jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_StreetMap_World_2D/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_Imagery_World_2D:
                    {
                        // http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_Imagery_World_2D/MapServer/tile/1/0/1.jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_Imagery_World_2D/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_ShadedRelief_World_2D:
                    {
                        // http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_ShadedRelief_World_2D/MapServer/tile/1/0/1.jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_ShadedRelief_World_2D/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_Topo_US_2D:
                    {
                        // http://server.arcgisonline.com/ArcGIS/rest/services/NGS_Topo_US_2D/MapServer/tile/4/3/15

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/NGS_Topo_US_2D/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_World_Physical_Map:
                    {
                        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Physical_Map/MapServer/tile/2/0/2.jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/World_Physical_Map/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_World_Shaded_Relief:
                    {
                        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer/tile/0/0/0jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_World_Street_Map:
                    {
                        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/0/0/0jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_World_Terrain_Base:
                    {
                        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Terrain_Base/MapServer/tile/0/0/0jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/World_Terrain_Base/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

                case MapType.ArcGIS_World_Topo_Map:
                    {
                        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/0/0/0jpg

                        return string.Format("http://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
                    }

#if TESTpjbcoetzer
            case MapType.ArcGIS_TestPjbcoetzer:
            {
               // http://mapping.mapit.co.za/ArcGIS/rest/services/World/MapServer/tile/Zoom/X/Y

               return string.Format("http://mapping.mapit.co.za/ArcGIS/rest/services/World/MapServer/tile/{0}/{1}/{2}", zoom, pos.Y, pos.X);
            }
#endif
                #endregion              

            }

            return null;
        }

        MercatorProjection ProjectionForWMS = new MercatorProjection();

        internal void GetSecGoogleWords(MapPoint pos, out string sec1, out string sec2)
        {
            sec1 = ""; // after &x=...
            sec2 = ""; // after &zoom=...
            int seclen = ((pos.X * 3) + pos.Y) % 8;
            sec2 = SecGoogleWord.Substring(0, seclen);
            if (pos.Y >= 10000 && pos.Y < 100000)
            {
                sec1 = "&s=";
            }
        }

        internal int GetServerNum(MapPoint pos, int max)
        {
            return (pos.X + 2 * pos.Y) % max;
        }

        internal string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = levelOfDetail; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }
                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }

        internal string MakeGeocoderUrl(string keywords, string language)
        {
            string key = keywords.Replace(' ', '+');
            return string.Format("http://maps.google.com/maps/geo?q={0}&hl={1}&output=csv&key={2}", key, language, GoogleMapsAPIKey);
        }

        internal string MakeReverseGeocoderUrl(PointLatLng pt, string language)
        {
            return string.Format("http://maps.google.com/maps/geo?hl={0}&ll={1},{2}&output=xml&key={3}", language, pt.Lat.ToString(CultureInfo.InvariantCulture), pt.Lng.ToString(CultureInfo.InvariantCulture), GoogleMapsAPIKey);
        }

        internal string MakeRouteUrl(PointLatLng start, PointLatLng end, string language, bool avoidHighways)
        {
            string highway = avoidHighways ? "&mra=ls&dirflg=dh" : "&mra=ls&dirflg=d";

            return string.Format("http://maps.google.com/maps?f=q&output=dragdir&doflg=p&hl={0}{1}&q=&saddr=@{2},{3}&daddr=@{4},{5}", language, highway, start.Lat.ToString(CultureInfo.InvariantCulture), start.Lng.ToString(CultureInfo.InvariantCulture), end.Lat.ToString(CultureInfo.InvariantCulture), end.Lng.ToString(CultureInfo.InvariantCulture));
        }

        internal string MakeRouteUrl(string start, string end, string language, bool avoidHighways)
        {
            string highway = avoidHighways ? "&mra=ls&dirflg=dh" : "&mra=ls&dirflg=d";

            return string.Format("http://maps.google.com/maps?f=q&output=dragdir&doflg=p&hl={0}{1}&q=&saddr=@{2}&daddr=@{3}", language, highway, start.Replace(' ', '+'), end.Replace(' ', '+'));
        }

        internal string MakeRouteAndDirectionsKmlUrl(PointLatLng start, PointLatLng end, string language, bool avoidHighways)
        {
            string highway = avoidHighways ? "&mra=ls&dirflg=dh" : "&mra=ls&dirflg=d";

            return string.Format("http://maps.google.com/maps?f=q&output=kml&doflg=p&hl={0}{1}&q=&saddr=@{2},{3}&daddr=@{4},{5}", language, highway, start.Lat.ToString(CultureInfo.InvariantCulture), start.Lng.ToString(CultureInfo.InvariantCulture), end.Lat.ToString(CultureInfo.InvariantCulture), end.Lng.ToString(CultureInfo.InvariantCulture));
        }

        internal string MakeRouteAndDirectionsKmlUrl(string start, string end, string language, bool avoidHighways)
        {
            string highway = avoidHighways ? "&mra=ls&dirflg=dh" : "&mra=ls&dirflg=d";

            return string.Format("http://maps.google.com/maps?f=q&output=kml&doflg=p&hl={0}{1}&q=&saddr=@{2}&daddr=@{3}", language, highway, start.Replace(' ', '+'), end.Replace(' ', '+'));
        }

        internal string MakeWalkingRouteUrl(PointLatLng start, PointLatLng end, string language)
        {
            string directions = "&mra=ls&dirflg=w";

            return string.Format("http://maps.google.com/maps?f=q&output=dragdir&doflg=p&hl={0}{1}&q=&saddr=@{2},{3}&daddr=@{4},{5}", language, directions, start.Lat.ToString(CultureInfo.InvariantCulture), start.Lng.ToString(CultureInfo.InvariantCulture), end.Lat.ToString(CultureInfo.InvariantCulture), end.Lng.ToString(CultureInfo.InvariantCulture));
        }

        internal string MakeWalkingRouteUrl(string start, string end, string language)
        {
            string directions = "&mra=ls&dirflg=w";
            return string.Format("http://maps.google.com/maps?f=q&output=dragdir&doflg=p&hl={0}{1}&q=&saddr=@{2}&daddr=@{3}", language, directions, start.Replace(' ', '+'), end.Replace(' ', '+'));
        }

        #endregion

        #region -- 数据下载 --

        internal void TryCorrectGoogleVersions()
        {
            if (!IsCorrectedGoogleVersions)
            {
                string url = @"http://maps.google.com";
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    if (Proxy != null)
                    {
                        request.Proxy = Proxy;
                        request.PreAuthenticate = true;
                    }

                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader read = new StreamReader(responseStream))
                            {
                                string html = read.ReadToEnd();

                                Regex reg = new Regex("\"*http://mt0.google.com/vt/lyrs=m@(\\d*)", RegexOptions.IgnoreCase);
                                Match mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 0)
                                    {
                                        VersionGoogleMap = string.Format("m@{0}", gc[1].Value);
                                        VersionGoogleMapChina = VersionGoogleMap;
                                    }
                                }

                                reg = new Regex("\"*http://mt0.google.com/vt/lyrs=h@(\\d*)", RegexOptions.IgnoreCase);
                                mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 0)
                                    {
                                        VersionGoogleLabels = string.Format("h@{0}", gc[1].Value);
                                        VersionGoogleLabelsChina = VersionGoogleLabels;
                                    }
                                }

                                reg = new Regex("\"*http://khm0.google.com/kh/v=(\\d*)", RegexOptions.IgnoreCase);
                                mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 0)
                                    {
                                        VersionGoogleSatellite = gc[1].Value;
                                        VersionGoogleSatelliteChina = "s@" + VersionGoogleSatellite;
                                    }
                                }

                                reg = new Regex("\"*http://mt0.google.com/vt/lyrs=t@(\\d*),r@(\\d*)", RegexOptions.IgnoreCase);
                                mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 1)
                                    {
                                        VersionGoogleTerrain = string.Format("t@{0},r@{1}", gc[1].Value, gc[2].Value);
                                        VersionGoogleTerrainChina = VersionGoogleTerrain;
                                    }
                                }
                            }
                        }
                    }
                    IsCorrectedGoogleVersions = true; 
                }
                catch (Exception ex)
                {
                    IsCorrectedGoogleVersions = false;
                    Debug.WriteLine("TryCorrectGoogleVersions failed: " + ex.ToString());
                }
            }
        }

      
        internal void TryCorrectBingVersions()
        {
            if (!IsCorrectedBingVersions)
            {
                string url = @"http://www.bing.com/maps";
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    if (Proxy != null)
                    {
                        request.Proxy = Proxy;
                        request.PreAuthenticate = true;
                    }

                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader read = new StreamReader(responseStream))
                            {
                                string html = read.ReadToEnd();

                                Regex reg = new Regex("http://ecn.t(\\d*).tiles.virtualearth.net/tiles/r(\\d*)[?*]g=(\\d*)", RegexOptions.IgnoreCase);
                                Match mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 2)
                                    {
                                        VersionBingMaps = gc[3].Value;
                                    }
                                }

                            }
                        }
                    }
                    IsCorrectedBingVersions = true; // try it only once
                }
                catch (Exception ex)
                {
                    IsCorrectedBingVersions = false;
                }
            }
        }

        internal string GetRouteBetweenPointsKmlUrl(string url)
        {
            string ret = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.ServicePoint.ConnectionLimit = 50;
                if (Proxy != null)
                {
                    request.Proxy = Proxy;
                    request.PreAuthenticate = true;
                }

                request.UserAgent = UserAgent;
                request.Timeout = Timeout;
                request.ReadWriteTimeout = Timeout * 6;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader read = new StreamReader(responseStream))
                        {
                            string kmls = read.ReadToEnd();

                            //XmlSerializer serializer = new XmlSerializer(typeof(KmlType));
                            using (StringReader reader = new StringReader(kmls)) //Substring(kmls.IndexOf("<kml"))
                            {
                                //ret = (KmlType) serializer.Deserialize(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret = null;
            }
            return ret;
        }

        internal PointLatLng? GetLatLngFromGeocoderUrl(string url, bool useCache, out GeoCoderStatusCode status)
        {
            status = GeoCoderStatusCode.Unknow;
            PointLatLng? ret = null;
            try
            {
                string urlEnd = url.Substring(url.IndexOf("geo?q="));

                char[] ilg = Path.GetInvalidFileNameChars();

                foreach (char c in ilg)
                {
                    urlEnd = urlEnd.Replace(c, '_');
                }

                string geo = useCache ? Cache.Instance.GetGeocoderFromCache(urlEnd) : string.Empty;

                if (string.IsNullOrEmpty(geo))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    if (Proxy != null)
                    {
                        request.Proxy = Proxy;
                        request.PreAuthenticate = true;
                    }

                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;
                    request.KeepAlive = false;

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader read = new StreamReader(responseStream))
                            {
                                geo = read.ReadToEnd();
                            }
                        }
                    }

                    if (useCache && geo.StartsWith("200"))
                    {
                        Cache.Instance.CacheGeocoder(urlEnd, geo);
                    }
                }

                // true : 200,4,56.1451640,22.0681787
                // false: 602,0,0,0
                {
                    string[] values = geo.Split(',');
                    if (values.Length == 4)
                    {
                        status = (GeoCoderStatusCode)int.Parse(values[0]);
                        if (status == GeoCoderStatusCode.G_GEO_SUCCESS)
                        {
                            double lat = double.Parse(values[2], CultureInfo.InvariantCulture);
                            double lng = double.Parse(values[3], CultureInfo.InvariantCulture);

                            ret = new PointLatLng(lat, lng);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret = null;
                Debug.WriteLine("GetLatLngFromGeocoderUrl: " + ex.ToString());
            }

            return ret;
        }

        internal Placemark GetPlacemarkFromReverseGeocoderUrl(string url, bool useCache)
        {
            Placemark ret = null;

            try
            {
                string urlEnd = url.Substring(url.IndexOf("geo?hl="));

                char[] ilg = Path.GetInvalidFileNameChars();


                foreach (char c in ilg)
                {
                    urlEnd = urlEnd.Replace(c, '_');
                }

                string reverse = useCache ? Cache.Instance.GetPlacemarkFromCache(urlEnd) : string.Empty;

                if (string.IsNullOrEmpty(reverse))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    if (Proxy != null)
                    {
                        request.Proxy = Proxy;
                        request.PreAuthenticate = true;
                    }

                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;
                    request.KeepAlive = false;

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader read = new StreamReader(responseStream))
                            {
                                reverse = read.ReadToEnd();
                            }
                        }
                    }

                    if (useCache)
                    {
                        Cache.Instance.CachePlacemark(urlEnd, reverse);
                    }
                }

                {
                    if (reverse.StartsWith("200"))
                    {
                        string acc = reverse.Substring(0, reverse.IndexOf('\"'));
                        ret = new Placemark(reverse.Substring(reverse.IndexOf('\"')));
                        ret.Accuracy = int.Parse(acc.Split(',').GetValue(1) as string);
                    }
                    else if (reverse.StartsWith("<?xml")) // kml
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(reverse);

                        XmlNamespaceManager nsMgr = new XmlNamespaceManager(doc.NameTable);
                        nsMgr.AddNamespace("sm", "http://earth.google.com/kml/2.0");
                        nsMgr.AddNamespace("sn", "urn:oasis:names:tc:ciq:xsdschema:xAL:2.0");

                        XmlNodeList l = doc.SelectNodes("/sm:kml/sm:Response/sm:Placemark", nsMgr);
                        if (l != null)
                        {
                            foreach (XmlNode n in l)
                            {
                                XmlNode nn = n.SelectSingleNode("//sm:Placemark/sm:address", nsMgr);
                                if (nn != null)
                                {
                                    ret = new Placemark(nn.InnerText);
                                    ret.XmlData = n.OuterXml;

                                    nn = n.SelectSingleNode("//sm:Status/sm:code", nsMgr);
                                    if (nn != null)
                                    {
                                        ret.Status = (GeoCoderStatusCode)int.Parse(nn.InnerText);
                                    }

                                    nn = n.SelectSingleNode("//sm:Placemark/sn:AddressDetails/@Accuracy", nsMgr);
                                    if (nn != null)
                                    {
                                        ret.Accuracy = int.Parse(nn.InnerText);
                                    }

                                    nn = n.SelectSingleNode("//sm:Placemark/sn:AddressDetails/sn:Country/sn:CountryNameCode", nsMgr);
                                    if (nn != null)
                                    {
                                        ret.CountryNameCode = nn.InnerText;
                                    }

                                    nn = n.SelectSingleNode("//sm:Placemark/sn:AddressDetails/sn:Country/sn:CountryName", nsMgr);
                                    if (nn != null)
                                    {
                                        ret.CountryName = nn.InnerText;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ret = null;
                Debug.WriteLine("GetPlacemarkReverseGeocoderUrl: " + ex.ToString());
            }

            return ret;
        }

        internal List<PointLatLng> GetRouteBetweenPointsUrl(string url, int zoom, bool useCache, out string tooltipHtml, out int numLevel, out int zoomFactor)
        {
            List<PointLatLng> points = new List<PointLatLng>();
            tooltipHtml = string.Empty;
            numLevel = -1;
            zoomFactor = -1;
            try
            {
                string urlEnd = url.Substring(url.IndexOf("&hl="));

                char[] ilg = Path.GetInvalidFileNameChars();
                foreach (char c in ilg)
                {
                    urlEnd = urlEnd.Replace(c, '_');
                }

                string route = useCache ? Cache.Instance.GetRouteFromCache(urlEnd) : string.Empty;

                if (string.IsNullOrEmpty(route))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.ServicePoint.ConnectionLimit = 50;
                    if (Proxy != null)
                    {
                        request.Proxy = Proxy;
                        request.PreAuthenticate = true;
                    }

                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;
                    request.KeepAlive = false;

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader read = new StreamReader(responseStream))
                            {
                                route = read.ReadToEnd();
                            }
                        }
                    }

                    if (useCache)
                    {
                        Cache.Instance.CacheRoute(urlEnd, route);
                    }
                }

                // title              
                int tooltipEnd = 0;
                {
                    int x = route.IndexOf("tooltipHtml:") + 13;
                    if (x >= 13)
                    {
                        tooltipEnd = route.IndexOf("\"", x + 1);
                        if (tooltipEnd > 0)
                        {
                            int l = tooltipEnd - x;
                            if (l > 0)
                            {
                                tooltipHtml = route.Substring(x, l).Replace(@"\x26#160;", " ");
                            }
                        }
                    }
                }

                // points
                int pointsEnd = 0;
                {
                    int x = route.IndexOf("points:", tooltipEnd >= 0 ? tooltipEnd : 0) + 8;
                    if (x >= 8)
                    {
                        pointsEnd = route.IndexOf("\"", x + 1);
                        if (pointsEnd > 0)
                        {
                            int l = pointsEnd - x;
                            if (l > 0)
                            {
                                /*
                                while(l % 5 != 0)
                                {
                                   l--;
                                }
                                */

                                // http://code.google.com/apis/maps/documentation/polylinealgorithm.html
                                //
                                string encoded = route.Substring(x, l).Replace("\\\\", "\\");
                                {
                                    int len = encoded.Length;
                                    int index = 0;
                                    double dlat = 0;
                                    double dlng = 0;

                                    while (index < len)
                                    {
                                        int b;
                                        int shift = 0;
                                        int result = 0;

                                        do
                                        {
                                            b = encoded[index++] - 63;
                                            result |= (b & 0x1f) << shift;
                                            shift += 5;

                                        } while (b >= 0x20 && index < len);

                                        dlat += ((result & 1) == 1 ? ~(result >> 1) : (result >> 1));

                                        shift = 0;
                                        result = 0;

                                        if (index < len)
                                        {
                                            do
                                            {
                                                b = encoded[index++] - 63;
                                                result |= (b & 0x1f) << shift;
                                                shift += 5;
                                            }
                                            while (b >= 0x20 && index < len);

                                            dlng += ((result & 1) == 1 ? ~(result >> 1) : (result >> 1));

                                            points.Add(new PointLatLng(dlat * 1e-5, dlng * 1e-5));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // levels  
                string levels = string.Empty;
                int levelsEnd = 0;
                {
                    int x = route.IndexOf("levels:", pointsEnd >= 0 ? pointsEnd : 0) + 8;
                    if (x >= 8)
                    {
                        levelsEnd = route.IndexOf("\"", x + 1);
                        if (levelsEnd > 0)
                        {
                            int l = levelsEnd - x;
                            if (l > 0)
                            {
                                levels = route.Substring(x, l);
                            }
                        }
                    }
                }

                // numLevel             
                int numLevelsEnd = 0;
                {
                    int x = route.IndexOf("numLevels:", levelsEnd >= 0 ? levelsEnd : 0) + 10;
                    if (x >= 10)
                    {
                        numLevelsEnd = route.IndexOf(",", x);
                        if (numLevelsEnd > 0)
                        {
                            int l = numLevelsEnd - x;
                            if (l > 0)
                            {
                                numLevel = int.Parse(route.Substring(x, l));
                            }
                        }
                    }
                }

                // zoomFactor             
                {
                    int x = route.IndexOf("zoomFactor:", numLevelsEnd >= 0 ? numLevelsEnd : 0) + 11;
                    if (x >= 11)
                    {
                        int end = route.IndexOf("}", x);
                        if (end > 0)
                        {
                            int l = end - x;
                            if (l > 0)
                            {
                                zoomFactor = int.Parse(route.Substring(x, l));
                            }
                        }
                    }
                }

                // finnal
                if (numLevel > 0 && !string.IsNullOrEmpty(levels))
                {
                    if (points.Count - levels.Length > 0)
                    {
                        points.RemoveRange(levels.Length, points.Count - levels.Length);
                    }

                    //http://facstaff.unca.edu/mcmcclur/GoogleMaps/EncodePolyline/description.html
                    //
                    string allZlevels = "TSRPONMLKJIHGFEDCBA@?";
                    if (numLevel > allZlevels.Length)
                    {
                        numLevel = allZlevels.Length;
                    }

                    string pLevels = allZlevels.Substring(allZlevels.Length - numLevel);

                    {
                        List<PointLatLng> removedPoints = new List<PointLatLng>();

                        for (int i = 0; i < levels.Length; i++)
                        {
                            int zi = pLevels.IndexOf(levels[i]);
                            if (zi > 0)
                            {
                                if (zi * numLevel > zoom)
                                {
                                    removedPoints.Add(points[i]);
                                }
                            }
                        }

                        foreach (var v in removedPoints)
                        {
                            points.Remove(v);
                        }
                        removedPoints.Clear();
                        removedPoints = null;
                    }
                }

                points.TrimExcess();
            }
            catch (Exception ex)
            {
                points = null;
                Debug.WriteLine("GetRouteBetweenPointsUrl: " + ex.ToString());
            }
            return points;
            //tooltipHtml = null;
            numLevel = 0;
            zoomFactor = 0;

            return null;
        }

        /// <summary>
        /// gets image from tile server
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public PureImage GetImageFrom(MapType type, MapPoint pos, int zoom, out Exception result)
        {
            PureImage ret = null;
            result = null;

            try
            {
                if (UseMemoryCache)
                {
                    MemoryStream m = GetTileFromMemoryCache(new RawTile(type, pos, zoom));
                    if (m != null)
                    {
                        if (MapsManager.Instance.ImageProxy != null)
                        {
                            ret = MapsManager.Instance.ImageProxy.FromStream(m);
                            if (ret == null)
                            {
                                m.Dispose();
                            }
                        }
                    }
                }

                if (ret == null)
                {
                    if (Mode != AccessMode.ServerOnly)
                    {
                        if (Cache.Instance.ImageCache != null)
                        {
                            ret = Cache.Instance.ImageCache.GetImageFromCache(type, pos, zoom);
                            if (ret != null)
                            {
                                if (UseMemoryCache)
                                {
                                    AddTileToMemoryCache(new RawTile(type, pos, zoom), ret.Data);
                                }
                                return ret;
                            }
                        }

                        if (Cache.Instance.ImageCacheSecond != null)
                        {
                            ret = Cache.Instance.ImageCacheSecond.GetImageFromCache(type, pos, zoom);
                            if (ret != null)
                            {
                                if (UseMemoryCache)
                                {
                                    AddTileToMemoryCache(new RawTile(type, pos, zoom), ret.Data);
                                }
                                EnqueueCacheTask(new CacheItemQueue(type, pos, zoom, ret.Data, CacheUsage.First));
                                return ret;
                            }
                        }
                    }

                    if (Mode != AccessMode.CacheOnly)
                    {
                        string url = MakeImageUrl(type, pos, zoom, LanguageStr);

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        if (Proxy != null)
                        {
                            request.Proxy = Proxy;
                            request.PreAuthenticate = true;

                        }

                        request.UserAgent = UserAgent;
                        request.Timeout = Timeout;
                        request.ReadWriteTimeout = Timeout * 6;
                        request.Accept = "*/*";

                        switch (type)
                        {
                            case MapType.GoogleMap:
                            case MapType.GoogleSatellite:
                            case MapType.GoogleLabels:
                            case MapType.GoogleTerrain:
                            case MapType.GoogleHybrid:
                                {
                                    request.Referer = "http://maps.google.com/";
                                }
                                break;

                            case MapType.GoogleMapChina:
                            case MapType.GoogleSatelliteChina:
                            case MapType.GoogleLabelsChina:
                            case MapType.GoogleTerrainChina:
                            case MapType.GoogleHybridChina:
                                {
                                    request.Referer = "http://ditu.google.cn/";
                                }
                                break;

                            case MapType.BingHybrid:
                            case MapType.BingMap:
                            case MapType.BingMap_New:
                            case MapType.BingSatellite:
                                {
                                    request.Referer = "http://www.bing.com/maps/";
                                }
                                break;

                            case MapType.YahooHybrid:
                            case MapType.YahooLabels:
                            case MapType.YahooMap:
                            case MapType.YahooSatellite:
                                {
                                    request.Referer = "http://maps.yahoo.com/";
                                }
                                break;
                           
                            case MapType.OpenStreetMapSurfer:
                            case MapType.OpenStreetMapSurferTerrain:
                                {
                                    request.Referer = "http://www.mapsurfer.net/";
                                }
                                break;

                            case MapType.OpenStreetMap:
                            case MapType.OpenStreetOsm:
                                {
                                    request.Referer = "http://www.openstreetmap.org/";
                                }
                                break;

                            case MapType.OpenSeaMapLabels:
                                {
                                    request.Referer = "http://openseamap.org/";
                                }
                                break;

                            case MapType.OpenCycleMap:
                                {
                                    request.Referer = "http://www.opencyclemap.org/";
                                }
                                break;
                           
                        }

                        Debug.WriteLine("Starting GetResponse: " + pos);

                        using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                        {
                            Debug.WriteLine("GetResponse OK: " + pos);

                            Debug.WriteLine("Starting GetResponseStream: " + pos);
                            MemoryStream responseStream = Stuff.CopyStream(response.GetResponseStream(), false);
                            {
                                Debug.WriteLine("GetResponseStream OK: " + pos);

                                if (MapsManager.Instance.ImageProxy != null)
                                {
                                    ret = MapsManager.Instance.ImageProxy.FromStream(responseStream);

                                    // Enqueue Cache
                                    if (ret != null)
                                    {
                                        if (UseMemoryCache)
                                        {
                                            AddTileToMemoryCache(new RawTile(type, pos, zoom), responseStream);
                                        }

                                        if (Mode != AccessMode.ServerOnly)
                                        {
                                            EnqueueCacheTask(new CacheItemQueue(type, pos, zoom, responseStream, CacheUsage.Both));
                                        }
                                    }
                                }
                            }
                            response.Close();
                        }
                    }
                    else
                    {
                        result = new Exception("在本地瓦片数据缓存中没有数据...");
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex;
                ret = null;
            }

            return ret;
        }


        #endregion
    }
}
