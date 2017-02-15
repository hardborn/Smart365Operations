
namespace Feeling.GIS.Map.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.IO;


   /// <summary>
   /// internal map control core
   /// </summary>
   public class MapCore
   {
      public PointLatLng currentPosition;
      public MapPoint currentPositionPixel;

      public MapPoint renderOffset;
      public MapPoint centerTileXYLocation;
      public MapPoint centerTileXYLocationLast;
      public MapPoint dragPoint;

      public MapPoint mouseDown;
      public MapPoint mouseCurrent;
      public MapPoint mouseLastZoom;

      public MouseWheelZoomType MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;

      public PointLatLng? LastLocationInBounds = null;
      public bool VirtualSizeEnabled = false;

      public MapSize sizeOfMapArea;
      public MapSize minOfTiles;
      public MapSize maxOfTiles;

      public MapRect tileRect;
      public MapRect tileRectBearing;
      public MapRect currentRegion;
      public float bearing = 0;
      public bool IsRotated = false;

      public readonly TileMatrix Matrix = new TileMatrix();

      public readonly List<MapPoint> tileDrawingList = new List<MapPoint>();
      public readonly FastReaderWriterLock tileDrawingListLock = new FastReaderWriterLock();

      //readonly ManualResetEvent waitForTileLoad = new ManualResetEvent(false);
      public readonly Queue<LoadTask> tileLoadQueue = new Queue<LoadTask>();

      public static readonly string googleCopyright = string.Format("©{0} Google - Map data ©{0} Tele Atlas, Imagery ©{0} TerraMetrics", DateTime.Today.Year);
      public static readonly string openStreetMapCopyright = string.Format("© OpenStreetMap - Map data ©{0} OpenStreetMap", DateTime.Today.Year);
      public static readonly string yahooMapCopyright = string.Format("© Yahoo! Inc. - Map data & Imagery ©{0} NAVTEQ", DateTime.Today.Year);
      public static readonly string virtualEarthCopyright = string.Format("©{0} Microsoft Corporation, ©{0} NAVTEQ, ©{0} Image courtesy of NASA", DateTime.Today.Year);
      public static readonly string arcGisCopyright = string.Format("©{0} ESRI - Map data ©{0} ArcGIS", DateTime.Today.Year);
      public static readonly string hnitCopyright = string.Format("©{0} Hnit-Baltic - Map data ©{0} ESRI", DateTime.Today.Year);
      public static readonly string pergoCopyright = string.Format("©{0} Pergo - Map data ©{0} Fideltus Advanced Technology", DateTime.Today.Year);

      static readonly int GThreadPoolSize = 3;


      DateTime LastInvalidation = DateTime.Now;
      DateTime LastTileLoadStart = DateTime.Now;
      DateTime LastTileLoadEnd = DateTime.Now;
      internal bool IsStarted = false;
      int zoom;
      internal int maxZoom = 2;
      internal int minZoom = 2;
      internal int Width;
      internal int Height;

      internal int pxRes100m;  // 100 meters
      internal int pxRes1000m;  // 1km  
      internal int pxRes10km; // 10km
      internal int pxRes100km; // 100km
      internal int pxRes1000km; // 1000km
      internal int pxRes5000km; // 5000km

      /// <summary>
      /// current peojection
      /// </summary>
      public PureProjection Projection;

      /// <summary>
      /// is user dragging map
      /// </summary>
      public bool IsDragging = false;

      public MapCore()
      {
         MapType = MapType.None;
      }

      /// <summary>
      /// map zoom
      /// </summary>
      public int Zoom
      {
         get
         {
            return zoom;
         }
         set
         {
            if(zoom != value && !IsDragging)
            {
               zoom = value;

               minOfTiles = Projection.GetTileMatrixMinXY(value);
               maxOfTiles = Projection.GetTileMatrixMaxXY(value);

               CurrentPositionGPixel = Projection.FromLatLngToPixel(CurrentPosition, value);

               if(IsStarted)
               {
                  lock(tileLoadQueue)
                  {
                     tileLoadQueue.Clear();
                  }

                  Matrix.ClearLevelsBelove(zoom - LevelsKeepInMemmory);
                  Matrix.ClearLevelsAbove(zoom + LevelsKeepInMemmory);

                  lock(FailedLoads)
                  {
                     FailedLoads.Clear();
                     RaiseEmptyTileError = true;
                  }

                  GoToCurrentPositionOnZoom();
                  UpdateBounds();

                  if(OnMapZoomChanged != null)
                  {
                     OnMapZoomChanged();
                  }
               }
            }
         }
      }

      /// <summary>
      /// current marker position in pixel coordinates
      /// </summary>
      public MapPoint CurrentPositionGPixel
      {
         get
         {
            return currentPositionPixel;
         }
         internal set
         {
            currentPositionPixel = value;
         }
      }

      /// <summary>
      /// current marker position
      /// </summary>
      public PointLatLng CurrentPosition
      {
         get
         {

            return currentPosition;
         }
         set
         {
            if(!IsDragging)
            {
               currentPosition = value;
               CurrentPositionGPixel = Projection.FromLatLngToPixel(value, Zoom);

               if(IsStarted)
               {
                  GoToCurrentPosition();
               }
            }
            else
            {
               currentPosition = value;
               CurrentPositionGPixel = Projection.FromLatLngToPixel(value, Zoom);
            }

            if(IsStarted)
            {
               if(OnCurrentPositionChanged != null)
                  OnCurrentPositionChanged(currentPosition);
            }
         }
      }

      internal bool zoomToArea = true;

      MapType mapType;
      public MapType MapType
      {
         get
         {
            return mapType;
         }
         set
         {
            if(value != MapType || value == MapType.None)
            {
               mapType = value;

               MapsManager.Instance.AdjustProjection(mapType, ref Projection, out maxZoom);

               tileRect = new MapRect(new MapPoint(0, 0), Projection.TileSize);
               tileRectBearing = tileRect;
               if(IsRotated)
               {
                  tileRectBearing.Inflate(1, 1);
               }

               minOfTiles = Projection.GetTileMatrixMinXY(Zoom);
               maxOfTiles = Projection.GetTileMatrixMaxXY(Zoom);
               CurrentPositionGPixel = Projection.FromLatLngToPixel(CurrentPosition, Zoom);

               if(IsStarted)
               {
                  CancelAsyncTasks();
                  OnMapSizeChanged(Width, Height);
                  ReloadMap();

                  if(OnMapTypeChanged != null)
                  {
                     OnMapTypeChanged(value);
                  }

                  switch(mapType)
                  {                     
                     default:
                     {
                        zoomToArea = true;
                     }
                     break;
                  }
               }
            }
         }
      }

    
      public bool SetZoomToFitRect(RectLatLng rect)
      {
         int mmaxZoom = GetMaxZoomToFitRect(rect);
         if(mmaxZoom > 0)
         {
            PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
            CurrentPosition = center;

            if(mmaxZoom > maxZoom)
            {
               mmaxZoom = maxZoom;
            }

            if((int)Zoom != mmaxZoom)
            {
               Zoom = mmaxZoom;
            }

            return true;
         }
         return false;
      }


      public bool PolygonsEnabled = true;

      public bool RoutesEnabled = true;

      public bool MarkersEnabled = true;

      public bool CanDragMap = true;


      public int RetryLoadTile = 0;

      public int LevelsKeepInMemmory = 7;


      public RenderMode RenderMode = RenderMode.WPF;

      public event CurrentPositionChanged OnCurrentPositionChanged;

      public event TileLoadComplete OnTileLoadComplete;

      public event TileLoadStart OnTileLoadStart;

      public event EmptyTileError OnEmptyTileError;

      public event NeedInvalidation OnNeedInvalidation;

      public event MapDrag OnMapDrag;

      public event MapZoomChanged OnMapZoomChanged;

      public event MapTypeChanged OnMapTypeChanged;

      readonly List<Thread> GThreadPool = new List<Thread>();


      internal string SystemType;
      internal static readonly Guid SessionIdGuid = Guid.NewGuid();
      internal static readonly Guid CompanyIdGuid = new Guid("3E35F098-CE43-4F82-9E9D-05C8B1046A45");
      internal static readonly Guid ApplicationIdGuid = new Guid("FF328040-77B0-4546-ACF3-7C6EC0827BBB");
      internal static volatile bool AnalyticsStartDone = false;
      internal static volatile bool AnalyticsStopDone = false;

      public void StartSystem()
      {
         if(!IsStarted)
         {
            IsStarted = true;
            GoToCurrentPosition();

         }
      }

      internal void ApplicationExit()
      {

      }


      public void UpdateCenterTileXYLocation()
      {
         PointLatLng center = FromLocalToLatLng(Width / 2, Height / 2);
         MapPoint centerPixel = Projection.FromLatLngToPixel(center, Zoom);
         centerTileXYLocation = Projection.FromPixelToTileXY(centerPixel);
      }

      public int vWidth = 800;
      public int vHeight = 400;

      public void OnMapSizeChanged(int width, int height)
      {
         this.Width = width;
         this.Height = height;

         if(IsRotated)
         {
            int diag = (int)Math.Round(Math.Sqrt(Width * Width + Height * Height) / Projection.TileSize.Width, MidpointRounding.AwayFromZero);
            sizeOfMapArea.Width = 1 + (diag / 2);
            sizeOfMapArea.Height = 1 + (diag / 2);
         }
         else
         {
            sizeOfMapArea.Width = 1 + (Width / Projection.TileSize.Width) / 2;
            sizeOfMapArea.Height = 1 + (Height / Projection.TileSize.Height) / 2;
         }

         UpdateCenterTileXYLocation();

         if(IsStarted)
         {
            UpdateBounds();

            if(OnCurrentPositionChanged != null)
               OnCurrentPositionChanged(currentPosition);
         }
      }

      public void OnMapClose()
      {
         CancelAsyncTasks();
         IsStarted = false;

         Matrix.ClearAllLevels();

         lock(FailedLoads)
         {
            FailedLoads.Clear();
            RaiseEmptyTileError = false;
         }
      }

      public RectLatLng CurrentViewArea
      {
         get
         {
            if(Projection != null)
            {
               PointLatLng p = Projection.FromPixelToLatLng(-renderOffset.X, -renderOffset.Y, Zoom);
               double rlng = Projection.FromPixelToLatLng(-renderOffset.X + Width, -renderOffset.Y, Zoom).Lng;
               double blat = Projection.FromPixelToLatLng(-renderOffset.X, -renderOffset.Y + Height, Zoom).Lat;

               return RectLatLng.FromLTRB(p.Lng, p.Lat, rlng, blat);
            }
            return RectLatLng.Empty;
         }
      }

      public PointLatLng FromLocalToLatLng(int x, int y)
      {
         return Projection.FromPixelToLatLng(new MapPoint(x - renderOffset.X, y - renderOffset.Y), Zoom);
      }

      public MapPoint FromLatLngToLocal(PointLatLng latlng)
      {
         MapPoint pLocal = Projection.FromLatLngToPixel(latlng, Zoom);
         pLocal.Offset(renderOffset);
         return pLocal;
      }

      public int GetMaxZoomToFitRect(RectLatLng rect)
      {
         int zoom = minZoom;

         for(int i = zoom; i <= maxZoom; i++)
         {
            MapPoint p1 = Projection.FromLatLngToPixel(rect.LocationTopLeft, i);
            MapPoint p2 = Projection.FromLatLngToPixel(rect.LocationRightBottom, i);

            if(((p2.X - p1.X) <= Width + 10) && (p2.Y - p1.Y) <= Height + 10)
            {
               zoom = i;
            }
            else
            {
               break;
            }
         }

         return zoom;
      }

      public void BeginDrag(MapPoint pt)
      {
         dragPoint.X = pt.X - renderOffset.X;
         dragPoint.Y = pt.Y - renderOffset.Y;
         IsDragging = true;
      }

      public void EndDrag()
      {
         IsDragging = false;
         mouseDown = MapPoint.Empty;

         if(OnNeedInvalidation != null)
         {
            OnNeedInvalidation();
         }
      }

      public void ReloadMap()
      {
         if(IsStarted)
         {
            Debug.WriteLine("------------------");

            lock(tileLoadQueue)
            {
               tileLoadQueue.Clear();
            }

            Matrix.ClearAllLevels();

            lock(FailedLoads)
            {
               FailedLoads.Clear();
               RaiseEmptyTileError = true;
            }

            if(OnNeedInvalidation != null)
            {
               OnNeedInvalidation();
            }

            UpdateBounds();
         }
         else
         {
            throw new Exception("不要在窗体装载之前调用ReloadMap()!");
         }
      }

   
      public void GoToCurrentPosition()
      {
         renderOffset = MapPoint.Empty;
         centerTileXYLocationLast = MapPoint.Empty;
         dragPoint = MapPoint.Empty;

         this.Drag(new MapPoint(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2)));
      }

      public bool MouseWheelZooming = false;

     
      internal void GoToCurrentPositionOnZoom()
      {
         renderOffset = MapPoint.Empty;
         centerTileXYLocationLast = MapPoint.Empty;
         dragPoint = MapPoint.Empty;

         if(MouseWheelZooming)
         {
            if(MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
            {
               MapPoint pt = new MapPoint(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2));
               renderOffset.X = pt.X - dragPoint.X;
               renderOffset.Y = pt.Y - dragPoint.Y;
            }
            else
            {
               renderOffset.X = -CurrentPositionGPixel.X - dragPoint.X;
               renderOffset.Y = -CurrentPositionGPixel.Y - dragPoint.Y;
               renderOffset.Offset(mouseLastZoom);
            }
         }
         else 
         {
            mouseLastZoom = MapPoint.Empty;

            MapPoint pt = new MapPoint(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2));
            renderOffset.X = pt.X - dragPoint.X;
            renderOffset.Y = pt.Y - dragPoint.Y;
         }

         UpdateCenterTileXYLocation();
      }

     
      public void DragOffset(MapPoint offset)
      {
         renderOffset.Offset(offset);

         UpdateCenterTileXYLocation();

         if(centerTileXYLocation != centerTileXYLocationLast)
         {
            centerTileXYLocationLast = centerTileXYLocation;
            UpdateBounds();
         }

         {
            LastLocationInBounds = CurrentPosition;
            CurrentPosition = FromLocalToLatLng((int)Width / 2, (int)Height / 2);
         }

         if(OnMapDrag != null)
         {
            OnMapDrag();
         }
      }

      public void Drag(MapPoint pt)
      {
         renderOffset.X = pt.X - dragPoint.X;
         renderOffset.Y = pt.Y - dragPoint.Y;

         UpdateCenterTileXYLocation();

         if(centerTileXYLocation != centerTileXYLocationLast)
         {
            centerTileXYLocationLast = centerTileXYLocation;
            UpdateBounds();
         }

         if(IsDragging)
         {
            LastLocationInBounds = CurrentPosition;
            CurrentPosition = FromLocalToLatLng((int)Width / 2, (int)Height / 2);

            if(OnMapDrag != null)
            {
               OnMapDrag();
            }
         }
      }

      public void CancelAsyncTasks()
      {
         if(IsStarted)
         {
            lock(tileLoadQueue)
            {
               tileLoadQueue.Clear();
            }
         }
      }

      bool RaiseEmptyTileError = false;
      internal readonly Dictionary<LoadTask, Exception> FailedLoads = new Dictionary<LoadTask, Exception>();

      internal static readonly int WaitForTileLoadThreadTimeout = 5 * 1000 * 60; // 5 min.

      long loadWaitCount = 0;
      readonly object LastInvalidationLock = new object();
      readonly object LastTileLoadStartEndLock = new object();

      void ProcessLoadTask()
      {
         bool invalidate = false;
         LoadTask? task = null;
         long lastTileLoadTimeMs;
         bool stop = false;

         Thread ct = Thread.CurrentThread;
         string ctid = "Thread[" + ct.ManagedThreadId + "]";

         while(!stop)
         {
            invalidate = false;
            task = null;

            lock(tileLoadQueue)
            {
               while(tileLoadQueue.Count == 0)
               {
                  Debug.WriteLine(ctid + " - Wait " + loadWaitCount + " - " + DateTime.Now.TimeOfDay);

                  if(++loadWaitCount >= GThreadPoolSize)
                  {
                     loadWaitCount = 0;

                     lock(LastInvalidationLock)
                     {
                        LastInvalidation = DateTime.Now;
                     }

                     if(OnNeedInvalidation != null)
                     {
                        OnNeedInvalidation();
                     }

                     lock(LastTileLoadStartEndLock)
                     {
                        LastTileLoadEnd = DateTime.Now;
                        lastTileLoadTimeMs = (long)(LastTileLoadEnd - LastTileLoadStart).TotalMilliseconds;
                     }

                     #region -- clear stuff--
                     {
                        MapsManager.Instance.kiberCacheLock.AcquireWriterLock();
                        try
                        {
                           MapsManager.Instance.TilesInMemory.RemoveMemoryOverload();
                        }
                        finally
                        {
                           MapsManager.Instance.kiberCacheLock.ReleaseWriterLock();
                        }

                        tileDrawingListLock.AcquireReaderLock();
                        try
                        {
                           Matrix.ClearLevelAndPointsNotIn(Zoom, tileDrawingList);
                        }
                        finally
                        {
                           tileDrawingListLock.ReleaseReaderLock();
                        }
                     }
                     #endregion

                     UpdateGroundResolution();

                     Debug.WriteLine(ctid + " - OnTileLoadComplete: " + lastTileLoadTimeMs + "ms, MemoryCacheSize: " + MapsManager.Instance.MemoryCacheSize + "MB");

                     if(OnTileLoadComplete != null)
                     {
                        OnTileLoadComplete(lastTileLoadTimeMs);
                     }
                  }

                  if(false == Monitor.Wait(tileLoadQueue, WaitForTileLoadThreadTimeout, false))
                  {
                     stop = true;
                     break;
                  }
               }

               if(!stop || tileLoadQueue.Count > 0)
               {
                  task = tileLoadQueue.Dequeue();
               }
            }

            if(task.HasValue)
            {
               try
               {
                  var m = Matrix.GetTileWithReadLock(task.Value.Zoom, task.Value.Pos);

                  if(m == null || m.Overlays.Count == 0)
                  {
                     Debug.WriteLine(ctid + " - Fill empty TileMatrix: " + task);

                     Tile t = new Tile(task.Value.Zoom, task.Value.Pos);
                     var layers = MapsManager.Instance.GetAllLayersOfType(MapType);

                     foreach(MapType tl in layers)
                     {
                        int retry = 0;
                        do
                        {
                           PureImage img;
                           Exception ex;

                           
                              img = MapsManager.Instance.GetImageFrom(tl, task.Value.Pos, task.Value.Zoom, out ex);

                           if(img != null)
                           {
                              lock(t.Overlays)
                              {
                                 t.Overlays.Add(img);
                              }
                              break;
                           }
                           else
                           {
                              if(ex != null)
                              {
                                 lock(FailedLoads)
                                 {
                                    if(!FailedLoads.ContainsKey(task.Value))
                                    {
                                       FailedLoads.Add(task.Value, ex);

                                       if(OnEmptyTileError != null)
                                       {
                                          if(!RaiseEmptyTileError)
                                          {
                                             RaiseEmptyTileError = true;
                                             OnEmptyTileError(task.Value.Zoom, task.Value.Pos);
                                          }
                                       }
                                    }
                                 }
                              }

                              if(RetryLoadTile > 0)
                              {
                                 Debug.WriteLine(ctid + " - ProcessLoadTask: " + task + " -> empty tile, retry " + retry);
                                 {
                                    Thread.Sleep(1111);
                                 }
                              }
                           }
                        }
                        while(++retry < RetryLoadTile);
                     }

                     if(t.Overlays.Count > 0)
                     {
                        Matrix.SetTile(t);
                     }
                     else
                     {
                        t.Clear();
                        t = null;
                     }

                     layers = null;
                  }
               }
               catch(Exception ex)
               {
                  Debug.WriteLine(ctid + " - ProcessLoadTask: " + ex.ToString());
               }
               finally
               {
                  lock(LastInvalidationLock)
                  {
                     invalidate = ((DateTime.Now - LastInvalidation).TotalMilliseconds > 111);
                     if(invalidate)
                     {
                        LastInvalidation = DateTime.Now;
                     }
                  }

                  if(invalidate)
                  {
                     if(OnNeedInvalidation != null)
                     {
                        OnNeedInvalidation();
                     }
                  }
               }
            }
         }

         lock(tileLoadQueue)
         {
            Debug.WriteLine("Quit - " + ct.Name);
            GThreadPool.Remove(ct);
         }
      }

     
      void UpdateBounds()
      {
         if(MapType == MapType.None)
         {
            return;
         }

         lock(tileLoadQueue)
         {
            tileDrawingListLock.AcquireWriterLock();
            try
            {
               tileDrawingList.Clear();

               for(int i = -sizeOfMapArea.Width; i <= sizeOfMapArea.Width; i++)
               {
                  for(int j = -sizeOfMapArea.Height; j <= sizeOfMapArea.Height; j++)
                  {
                     MapPoint p = centerTileXYLocation;
                     p.X += i;
                     p.Y += j;

                     if(p.X >= minOfTiles.Width && p.Y >= minOfTiles.Height && p.X <= maxOfTiles.Width && p.Y <= maxOfTiles.Height)
                     {
                        if(!tileDrawingList.Contains(p))
                        {
                           tileDrawingList.Add(p);
                        }
                     }
                  }
               }

               if(MapsManager.Instance.ShuffleTilesOnLoad)
               {
                  Stuff.Shuffle<MapPoint>(tileDrawingList);
               }

               foreach(MapPoint p in tileDrawingList)
               {
                  LoadTask task = new LoadTask(p, Zoom);
                  {
                     if(!tileLoadQueue.Contains(task))
                     {
                        tileLoadQueue.Enqueue(task);
                     }
                  }
               }
               EnsureLoaderThreads();
            }
            finally
            {
               tileDrawingListLock.ReleaseWriterLock();
            }

            //EnsureLoaderThreads();


            lock(LastTileLoadStartEndLock)
            {
               LastTileLoadStart = DateTime.Now;
               Debug.WriteLine("OnTileLoadStart - at zoom " + Zoom + ", time: " + LastTileLoadStart.TimeOfDay);
            }

            loadWaitCount = 0;

            Monitor.PulseAll(tileLoadQueue);
         }

         if(OnTileLoadStart != null)
         {
            OnTileLoadStart();
         }
      }

      private void EnsureLoaderThreads()
      {
          while (GThreadPool.Count < GThreadPoolSize)

          {
              Thread t = new Thread(new ThreadStart(ProcessLoadTask));
              {
                  t.Name = "TileLoader: " + GThreadPool.Count;
                  t.IsBackground = true;
                  t.Priority = ThreadPriority.BelowNormal;
              }
              GThreadPool.Add(t);

              Debug.WriteLine("add " + t.Name + " to GThreadPool");

              t.Start();
          }
      }

      void UpdateGroundResolution()
      {
         double rez = Projection.GetGroundResolution(Zoom, CurrentPosition.Lat);
         pxRes100m = (int)(100.0 / rez); // 100 meters
         pxRes1000m = (int)(1000.0 / rez); // 1km  
         pxRes10km = (int)(10000.0 / rez); // 10km
         pxRes100km = (int)(100000.0 / rez); // 100km
         pxRes1000km = (int)(1000000.0 / rez); // 1000km
         pxRes5000km = (int)(5000000.0 / rez); // 5000km
      }
   }
}
