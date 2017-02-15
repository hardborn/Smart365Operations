
namespace Feeling.GIS.Map.Core
{
   public interface InterfaceMap
   {
      PointLatLng Position
      {
         get;
         set;
      }

      MapPoint CurrentPositionGPixel
      {
         get;
      }

      string CacheLocation
      {
         get;
         set;
      }

      bool IsDragging
      {
         get;
      }

      RectLatLng CurrentViewArea
      {
         get;
      }

      MapType MapTileType
      {
         get;
         set;
      }

      PureProjection Projection
      {
         get;
      }

      bool CanDragMap
      {
         get;
         set;
      }

      RenderMode RenderMode
      {
         get;
      }

      // events
      event CurrentPositionChanged OnCurrentPositionChanged;
      event TileLoadComplete OnTileLoadComplete;
      event TileLoadStart OnTileLoadStart;
      event MapDrag OnMapDrag;
      event MapZoomChanged OnMapZoomChanged;
      event MapTypeChanged OnMapTypeChanged;

      void ReloadMap();

      PointLatLng FromLocalToLatLng(int x, int y);
      MapPoint FromLatLngToLocal(PointLatLng point);

#if SQLite
      bool ShowExportDialog();
      bool ShowImportDialog();
#endif

   }
}
