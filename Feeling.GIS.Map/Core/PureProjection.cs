
namespace Feeling.GIS.Map.Core
{
   using System;
   using System.Collections.Generic;

   /// <summary>
   ///投影系
   /// </summary>
   public abstract class PureProjection
   {
      readonly List<Dictionary<PointLatLng, MapPoint>> FromLatLngToPixelCache = new List<Dictionary<PointLatLng, MapPoint>>(33);
      readonly List<Dictionary<MapPoint, PointLatLng>> FromPixelToLatLngCache = new List<Dictionary<MapPoint, PointLatLng>>(33);

      public PureProjection()
      {
         for(int i = 0; i < FromLatLngToPixelCache.Capacity; i++)
         {
            FromLatLngToPixelCache.Add(new Dictionary<PointLatLng, MapPoint>(4444));
            FromPixelToLatLngCache.Add(new Dictionary<MapPoint, PointLatLng>(4444));
         }
      }

      /// <summary>
      /// 瓦片大小
      /// </summary>
      public abstract MapSize TileSize
      {
         get;
      }

      /// <summary>
      /// 椭圆主半轴
      /// </summary>
      public abstract double Axis
      {
         get;
      }

      /// <summary>
      /// Flattening of ellipsoid
      /// </summary>
      public abstract double Flattening
      {
         get;
      }

      /// <summary>
      /// get pixel coordinates from lat/lng
      /// </summary>
      /// <param name="lat"></param>
      /// <param name="lng"></param>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public abstract MapPoint FromLatLngToPixel(double lat, double lng, int zoom);

      /// <summary>
      /// gets lat/lng coordinates from pixel coordinates
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public abstract PointLatLng FromPixelToLatLng(int x, int y, int zoom);

      /// <summary>
      /// get pixel coordinates from lat/lng
      /// </summary>
      /// <param name="p"></param>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public MapPoint FromLatLngToPixel(PointLatLng p, int zoom)
      {
         MapPoint ret = MapPoint.Empty;
         if(!FromLatLngToPixelCache[zoom].TryGetValue(p, out ret))
         {
            ret = FromLatLngToPixel(p.Lat, p.Lng, zoom);
            FromLatLngToPixelCache[zoom].Add(p, ret);
         }
         return ret;
      }

      /// <summary>
      /// gets lat/lng coordinates from pixel coordinates
      /// </summary>
      /// <param name="p"></param>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public PointLatLng FromPixelToLatLng(MapPoint p, int zoom)
      {
         PointLatLng ret = PointLatLng.Zero;
         if(!FromPixelToLatLngCache[zoom].TryGetValue(p, out ret))
         {
            ret = FromPixelToLatLng(p.X, p.Y, zoom);
            FromPixelToLatLngCache[zoom].Add(p, ret);
         }
         return ret;
      }

      /// <summary>
      /// gets tile coorddinate from pixel coordinates
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public virtual MapPoint FromPixelToTileXY(MapPoint p)
      {
         return new MapPoint((int) (p.X / TileSize.Width), (int) (p.Y / TileSize.Height));
      }

      /// <summary>
      /// gets pixel coordinate from tile coordinate
      /// </summary>
      /// <param name="p"></param>
      /// <returns></returns>
      public virtual MapPoint FromTileXYToPixel(MapPoint p)
      {
         return new MapPoint((p.X * TileSize.Width), (p.Y * TileSize.Height));
      }

      /// <summary>
      /// min. tile in tiles at custom zoom level
      /// </summary>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public abstract MapSize GetTileMatrixMinXY(int zoom);

      /// <summary>
      /// max. tile in tiles at custom zoom level
      /// </summary>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public abstract MapSize GetTileMatrixMaxXY(int zoom);

      /// <summary>
      /// gets matrix size in tiles
      /// </summary>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public virtual MapSize GetTileMatrixSizeXY(int zoom)
      {
         MapSize sMin = GetTileMatrixMinXY(zoom);
         MapSize sMax = GetTileMatrixMaxXY(zoom);

         return new MapSize(sMax.Width - sMin.Width + 1, sMax.Height - sMin.Height + 1);
      }

      /// <summary>
      /// tile matrix size in pixels at custom zoom level
      /// </summary>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public int GetTileMatrixItemCount(int zoom)
      {
         MapSize s = GetTileMatrixSizeXY(zoom);
         return (s.Width * s.Height);
      }

      /// <summary>
      /// gets matrix size in pixels
      /// </summary>
      /// <param name="zoom"></param>
      /// <returns></returns>
      public virtual MapSize GetTileMatrixSizePixel(int zoom)
      {
         MapSize s = GetTileMatrixSizeXY(zoom);
         return new MapSize(s.Width * TileSize.Width, s.Height * TileSize.Height);
      }

      /// <summary>
      /// gets all tiles in rect at specific zoom
      /// </summary>
      public List<MapPoint> GetAreaTileList(RectLatLng rect, int zoom, int padding)
      {
         List<MapPoint> ret = new List<MapPoint>();

         MapPoint topLeft = FromPixelToTileXY(FromLatLngToPixel(rect.LocationTopLeft, zoom));
         MapPoint rightBottom = FromPixelToTileXY(FromLatLngToPixel(rect.LocationRightBottom, zoom));

         for(int x = (topLeft.X - padding); x <= (rightBottom.X + padding); x++)
         {
            for(int y = (topLeft.Y - padding); y <= (rightBottom.Y + padding); y++)
            {
               MapPoint p = new MapPoint(x, y);
               if(!ret.Contains(p) && p.X >= 0 && p.Y >= 0)
               {
                  ret.Add(p);
               }
            }
         }
         ret.TrimExcess();

         return ret;
      }

      /// <summary>
      /// The ground resolution indicates the distance (in meters) on the ground that’s represented by a single pixel in the map.
      /// For example, at a ground resolution of 10 meters/pixel, each pixel represents a ground distance of 10 meters.
      /// </summary>
      /// <param name="zoom"></param>
      /// <param name="latitude"></param>
      /// <returns></returns>
      public virtual double GetGroundResolution(int zoom, double latitude)
      {
         return (Math.Cos(latitude * (Math.PI / 180)) * 2 * Math.PI * Axis) / GetTileMatrixSizePixel(zoom).Width;
      }

      #region -- math functions --

     
      protected static readonly double PI = Math.PI;
            
      protected static readonly double HALF_PI = (PI * 0.5);

      protected static readonly double TWO_PI = (PI * 2.0);

      protected static readonly double EPSLoN = 1.0e-10;

      protected const double MAX_VAL = 4;

      protected static readonly double MAXLONG = 2147483647;

      protected static readonly double DBLLONG = 4.61168601e18;

      static readonly double R2D = 180 / Math.PI;
      static readonly double D2R = Math.PI / 180;

      public static double DegreesToRadians(double deg)
      {
         return (D2R * deg);
      }

      public static double RadiansToDegrees(double rad)
      {
         return (R2D * rad);
      }

      protected static double Sign(double x)
      {
         if(x < 0.0)
            return (-1);
         else
            return (1);
      }

      protected static double AdjustLongitude(double x)
      {
         long count = 0;
         while(true)
         {
            if(Math.Abs(x) <= PI)
               break;
            else
               if(((long) Math.Abs(x / Math.PI)) < 2)
                  x = x - (Sign(x) * TWO_PI);
               else
                  if(((long) Math.Abs(x / TWO_PI)) < MAXLONG)
                  {
                     x = x - (((long) (x / TWO_PI)) * TWO_PI);
                  }
                  else
                     if(((long) Math.Abs(x / (MAXLONG * TWO_PI))) < MAXLONG)
                     {
                        x = x - (((long) (x / (MAXLONG * TWO_PI))) * (TWO_PI * MAXLONG));
                     }
                     else
                        if(((long) Math.Abs(x / (DBLLONG * TWO_PI))) < MAXLONG)
                        {
                           x = x - (((long) (x / (DBLLONG * TWO_PI))) * (TWO_PI * DBLLONG));
                        }
                        else
                           x = x - (Sign(x) * TWO_PI);
            count++;
            if(count > MAX_VAL)
               break;
         }
         return (x);
      }

   
      protected static void SinCos(double val, out double sin, out double cos)
      {
         sin = Math.Sin(val);
         cos = Math.Cos(val);
      }

      protected static double e0fn(double x)
      {
         return (1.0 - 0.25 * x * (1.0 + x / 16.0 * (3.0 + 1.25 * x)));
      }

      protected static double e1fn(double x)
      {
         return (0.375 * x * (1.0 + 0.25 * x * (1.0 + 0.46875 * x)));
      }

      protected static double e2fn(double x)
      {
         return (0.05859375 * x * x * (1.0 + 0.75 * x));
      }

      protected static double e3fn(double x)
      {
         return (x * x * x * (35.0 / 3072.0));
      }

   
      protected static double mlfn(double e0, double e1, double e2, double e3, double phi)
      {
         return (e0 * phi - e1 * Math.Sin(2.0 * phi) + e2 * Math.Sin(4.0 * phi) - e3 * Math.Sin(6.0 * phi));
      }

    
      protected static long GetUTMzone(double lon)
      {
         return ((long) (((lon + 180.0) / 6.0) + 1.0));
      }

    
      protected static double Clip(double n, double minValue, double maxValue)
      {
         return Math.Min(Math.Max(n, minValue), maxValue);
      }

      #endregion


      public void FromGeodeticToCartesian(double Lat, double Lng, double Height, out double X, out double Y, out double Z)
      {
         Lat = (Math.PI / 180) * Lat;
         Lng = (Math.PI / 180) * Lng;

         double B = Axis * (1.0 - Flattening);
         double ee = 1.0 - (B / Axis) * (B / Axis);
         double N = (Axis / Math.Sqrt(1.0 - ee * Math.Sin(Lat) * Math.Sin(Lat)));

         X = (N + Height) * Math.Cos(Lat) * Math.Cos(Lng);
         Y = (N + Height) * Math.Cos(Lat) * Math.Sin(Lng);
         Z = (N * (B / Axis) * (B / Axis) + Height) * Math.Sin(Lat);
      }

      public void FromCartesianTGeodetic(double X, double Y, double Z, out double Lat, out double Lng)
      {
         double E = Flattening * (2.0 - Flattening);
         Lng = Math.Atan2(Y, X);

         double P = Math.Sqrt(X * X + Y * Y);
         double Theta = Math.Atan2(Z, (P * (1.0 - Flattening)));
         double st = Math.Sin(Theta);
         double ct = Math.Cos(Theta);
         Lat = Math.Atan2(Z + E / (1.0 - Flattening) * Axis * st * st * st, P - E * Axis * ct * ct * ct);

         Lat /= (Math.PI / 180);
         Lng /= (Math.PI / 180);
      }
   }
}
