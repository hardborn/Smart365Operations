
namespace Feeling.GIS.Map.Core
{
   /// <summary>
   /// 瓦片数据存取模式
   /// </summary>
   public enum AccessMode
   {
      /// <summary>
      /// 仅服务访问
      /// </summary>
      ServerOnly,

      /// <summary>
      /// 服务与本地缓存访问
      /// </summary>
      ServerAndCache,

      /// <summary>
      /// 仅本地缓存访问
      /// </summary>
      CacheOnly,
   }
}
