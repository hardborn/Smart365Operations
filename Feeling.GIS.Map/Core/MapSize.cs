
namespace Feeling.GIS.Map.Core
{
   using System.Globalization;


   public struct MapSize
   {
      public static readonly MapSize Empty = new MapSize();

      private int width;
      private int height;

      public MapSize(MapPoint pt)
      {
         width = pt.X;
         height = pt.Y;
      }

      public MapSize(int width, int height)
      {
         this.width = width;
         this.height = height;
      }

      public static MapSize operator+(MapSize sz1, MapSize sz2)
      {
         return Add(sz1, sz2);
      }

      public static MapSize operator-(MapSize sz1, MapSize sz2)
      {
         return Subtract(sz1, sz2);
      }

      public static bool operator==(MapSize sz1, MapSize sz2)
      {
         return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
      }

      public static bool operator!=(MapSize sz1, MapSize sz2)
      {
         return !(sz1 == sz2);
      }

      public static explicit operator MapPoint(MapSize size)
      {
         return new MapPoint(size.Width, size.Height);
      }

      public bool IsEmpty
      {
         get
         {
            return width == 0 && height == 0;
         }
      }

      public int Width
      {
         get
         {
            return width;
         }
         set
         {
            width = value;
         }
      }

      public int Height
      {
         get
         {
            return height;
         }
         set
         {
            height = value;
         }
      }

      public static MapSize Add(MapSize sz1, MapSize sz2)
      {
         return new MapSize(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
      }

      public static MapSize Subtract(MapSize sz1, MapSize sz2)
      {
         return new MapSize(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
      }

      public override bool Equals(object obj)
      {
         if(!(obj is MapSize))
            return false;

         MapSize comp = (MapSize) obj;
         return (comp.width == this.width) &&
                   (comp.height == this.height);
      }

      public override int GetHashCode()
      {
         if(this.IsEmpty)
         {
            return 0;
         }
         return (Width.GetHashCode() ^ Height.GetHashCode());
      }

      public override string ToString()
      {
         return "{Width=" + width.ToString(CultureInfo.CurrentCulture) + ", Height=" + height.ToString(CultureInfo.CurrentCulture) + "}";
      }
   }
}
