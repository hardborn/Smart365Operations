
namespace Feeling.GIS.Map.Core
{
   using System.Collections.Generic;

   /// <summary>
   /// 地图瓦片
   /// </summary>
   public class Tile
   {
      MapPoint pos;
      int zoom;
      public readonly List<PureImage> Overlays = new List<PureImage>(1);

      public Tile(int zoom, MapPoint pos)
      {
         this.Zoom = zoom;
         this.Pos = pos;
      }

      public void Clear()
      {
         lock(Overlays)
         {
            foreach(PureImage i in Overlays)
            {
               i.Dispose();
            }

            Overlays.Clear();
         }
      }

      public int Zoom
      {
         get
         {
            return zoom;
         }
         private set
         {
            zoom = value;
         }
      }

      public MapPoint Pos
      {
         get
         {
            return pos;
         }
         private set
         {
            pos= value;
         }
      }
   }
}
