
namespace Feeling.GIS.Map.Core
{
   using System.Globalization;

   public struct MapPoint
   {
      public static readonly MapPoint Empty = new MapPoint();

      private int x;
      private int y;

      public MapPoint(int x, int y)
      {
         this.x = x;
         this.y = y;
      }

      public MapPoint(MapSize sz)
      {
         this.x = sz.Width;
         this.y = sz.Height;
      }

      public MapPoint(int dw)
      {
         this.x = (short) LOWORD(dw);
         this.y = (short) HIWORD(dw);
      }

      public bool IsEmpty
      {
         get
         {
            return x == 0 && y == 0;
         }
      }

      public int X
      {
         get
         {
            return x;
         }
         set
         {
            x = value;
         }
      }

      public int Y
      {
         get
         {
            return y;
         }
         set
         {
            y = value;
         }
      }

      public static explicit operator MapSize(MapPoint p)
      {
         return new MapSize(p.X, p.Y);
      }

      public static MapPoint operator+(MapPoint pt, MapSize sz)
      {
         return Add(pt, sz);
      }

      public static MapPoint operator-(MapPoint pt, MapSize sz)
      {
         return Subtract(pt, sz);
      }

      public static bool operator==(MapPoint left, MapPoint right)
      {
         return left.X == right.X && left.Y == right.Y;
      }

      public static bool operator!=(MapPoint left, MapPoint right)
      {
         return !(left == right);
      }

      public static MapPoint Add(MapPoint pt, MapSize sz)
      {
         return new MapPoint(pt.X + sz.Width, pt.Y + sz.Height);
      }

      public static MapPoint Subtract(MapPoint pt, MapSize sz)
      {
         return new MapPoint(pt.X - sz.Width, pt.Y - sz.Height);
      }

      public override bool Equals(object obj)
      {
         if(!(obj is MapPoint))
            return false;
         MapPoint comp = (MapPoint) obj;
         return comp.X == this.X && comp.Y == this.Y;
      }

      public override int GetHashCode()
      {
         return x ^ y;
      }

      public void Offset(int dx, int dy)
      {
         X += dx;
         Y += dy;
      }

      public void Offset(MapPoint p)
      {
         Offset(p.X, p.Y);
      }

      public override string ToString()
      {
         return "{X=" + X.ToString(CultureInfo.CurrentCulture) + ",Y=" + Y.ToString(CultureInfo.CurrentCulture) + "}";
      }

      private static int HIWORD(int n)
      {
         return (n >> 16) & 0xffff;
      }

      private static int LOWORD(int n)
      {
         return n & 0xffff;
      }
   }
}
