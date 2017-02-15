
namespace Feeling.GIS.Map.Core
{
   using System.IO;

  
   internal struct CacheItemQueue
   {
      public MapType Type;
      public MapPoint Pos;
      public int Zoom;
      public MemoryStream Img;
      public CacheUsage CacheType;

      public CacheItemQueue(MapType Type, MapPoint Pos, int Zoom, MemoryStream Img, CacheUsage cacheType)
      {
         this.Type = Type;
         this.Pos = Pos;
         this.Zoom = Zoom;
         this.Img = Img;
         this.CacheType = cacheType;
      }

      public override string ToString()
      {
         return Type + " at zoom " + Zoom + ", pos: " + Pos + ", CacheType:" + CacheType;
      }
   }

   internal enum CacheUsage
   {
      First=0,
      Second=1,
      Both=First | Second
   }
}
