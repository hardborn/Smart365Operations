using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Feeling.GIS.Map.Core;

namespace Smart365Operation.Modules.Dashboard.Views
{
    /// <summary>
    /// OverviewMapView.xaml 的交互逻辑
    /// </summary>
    public partial class OverviewMapView : UserControl
    {
        public OverviewMapView()
        {
            InitializeComponent();
            ConfigMap();
        }

        private void ConfigMap()
        {
            Map.Manager.Mode = AccessMode.ServerAndCache;
            Map.Position = new PointLatLng(34.210170, 108.869360);
            Map.Zoom = 1;
            Map.MaxZoom = 16;
            Map.Zoom = 12;
            Map.MapTileType = MapType.GoogleMapChina;
            //comboBoxMapType.ItemsSource = Enum.GetValues(typeof(MapType));
            //currentMarker = new MapMarker(Map.Position);
            //{
            //    currentMarker.Shape = new PositionMarker(this, currentMarker, "position marker");
            //    currentMarker.Offset = new System.Windows.Point(-15, -15);
            //    currentMarker.ZIndex = int.MaxValue;
            //    Map.Markers.Add(currentMarker);
            //}

        }
    }
}
