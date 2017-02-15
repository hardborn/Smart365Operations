
namespace Feeling.GIS.Map.Core
{
   /// <summary>
   /// 数据装载任务
   /// </summary>
   public struct LoadTask
   {
      public MapPoint Pos;
      public int Zoom;

      public LoadTask(MapPoint pos, int zoom)
      {
         Pos = pos;
         Zoom = zoom;
      }

      public override string ToString()
      {
         return Zoom + " - " + Pos.ToString();
      }
   }
}
