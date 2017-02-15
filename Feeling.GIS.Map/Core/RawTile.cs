namespace Feeling.GIS.Map.Core
{
   using System.IO;

   /// <summary>
   /// 瓦片数据结构
   /// </summary>
   internal struct RawTile
   {
      public MapType Type;
      public MapPoint Pos;
      public int Zoom;

      public RawTile(MapType Type, MapPoint Pos, int Zoom)
      {
         this.Type = Type;
         this.Pos = Pos;
         this.Zoom = Zoom;
      }

      public override string ToString()
      {
         return Type + " at zoom " + Zoom + ", pos: " + Pos;
      }
   }
}
