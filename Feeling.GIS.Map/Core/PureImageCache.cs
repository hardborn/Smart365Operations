
namespace Feeling.GIS.Map.Core
{
   using System.IO;
   using System;

   public interface PureImageCache
   {
     
      bool PutImageToCache(MemoryStream tile, MapType type, MapPoint pos, int zoom);

      PureImage GetImageFromCache(MapType type, MapPoint pos, int zoom);

      int DeleteOlderThan(DateTime date);
   }
}
