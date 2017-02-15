
namespace Feeling.GIS.Map.Core
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Diagnostics;
    using System.Windows.Shapes;
    using System;
    using Feeling.GIS.Map;

    //public interface IGMapMarker : INotifyPropertyChanged
    //{
    //   UIElement Shape
    //   {
    //      get;
    //      set;
    //   }

    //   int LocalPositionX
    //   {
    //      get;
    //      set;
    //   }

    //   int LocalPositionY
    //   {
    //      get;
    //      set;
    //   }

    //   PointLatLng Position
    //   {
    //      get;
    //      set;
    //   }

    //   System.Windows.Point Offset
    //   {
    //      get;
    //      set;
    //   }

    //   int ZIndex
    //   {
    //      get;
    //      set;
    //   }

    //   List<PointLatLng> Route
    //   {
    //      get;
    //   }
    //   List<PointLatLng> Polygon
    //   {
    //      get;
    //   }
    //}

    /// <summary>
    /// 地图标注
    /// </summary>
    public class MapMarker : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        void OnPropertyChanged(PropertyChangedEventArgs name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, name);
            }
        }

        UIElement shape;
        static readonly PropertyChangedEventArgs Shape_PropertyChangedEventArgs = new PropertyChangedEventArgs("Shape");

        private string id;
        public string ID
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("ID");
            }
        }

        public UIElement Shape
        {
            get
            {
                return shape;
            }
            set
            {
                if (shape != value)
                {
                    shape = value;
                    OnPropertyChanged(Shape_PropertyChangedEventArgs);

                    UpdateLocalPosition();
                }
            }
        }

        private PointLatLng position;

        public PointLatLng Position
        {
            get
            {
                return position;
            }
            set
            {
                if (position != value)
                {
                    position = value;
                    UpdateLocalPosition();
                }
            }
        }

        private MapControl map;

        public MapControl Map
        {
            get
            {
                if (Shape != null && map == null)
                {
                    DependencyObject visual = Shape;
                    while (visual != null && !(visual is MapControl))
                    {
                        visual = VisualTreeHelper.GetParent(visual);
                    }

                    map = visual as MapControl;
                }

                return map;
            }
        }

        public object Tag;

        System.Windows.Point offset;

        public System.Windows.Point Offset
        {
            get
            {
                return offset;
            }
            set
            {
                if (offset != value)
                {
                    offset = value;
                    UpdateLocalPosition();
                }
            }
        }

        int localPositionX;
        static readonly PropertyChangedEventArgs LocalPositionX_PropertyChangedEventArgs = new PropertyChangedEventArgs("LocalPositionX");

        public int LocalPositionX
        {
            get
            {
                return localPositionX;
            }
            internal set
            {
                if (localPositionX != value)
                {
                    localPositionX = value;
                    OnPropertyChanged(LocalPositionX_PropertyChangedEventArgs);
                }
            }
        }

        int localPositionY;
        static readonly PropertyChangedEventArgs LocalPositionY_PropertyChangedEventArgs = new PropertyChangedEventArgs("LocalPositionY");

        public int LocalPositionY
        {
            get
            {
                return localPositionY;
            }
            internal set
            {
                if (localPositionY != value)
                {
                    localPositionY = value;
                    OnPropertyChanged(LocalPositionY_PropertyChangedEventArgs);
                }
            }
        }

        int zIndex;
        static readonly PropertyChangedEventArgs ZIndex_PropertyChangedEventArgs = new PropertyChangedEventArgs("ZIndex");

        public int ZIndex
        {
            get
            {
                return zIndex;
            }
            set
            {
                if (zIndex != value)
                {
                    zIndex = value;
                    OnPropertyChanged(ZIndex_PropertyChangedEventArgs);
                }
            }
        }

        public readonly List<PointLatLng> Route = new List<PointLatLng>();

        public readonly List<PointLatLng> Polygon = new List<PointLatLng>();

        public MapMarker(PointLatLng pos)
        {
            Position = pos;
        }

        public MapMarker(string markerID,PointLatLng pos)
            : this(pos)
        {
            id = markerID;
        }

        public MapMarker()
        {
            Position = new PointLatLng();
        }

        public void Clear()
        {
            var s = (Shape as IDisposable);
            if (s != null)
            {
                s.Dispose();
                s = null;
            }
            Shape = null;

            Route.Clear();
            Polygon.Clear();
        }

        internal void UpdateLocalPosition()
        {
            if (Map != null)
            {
                MapPoint p = Map.FromLatLngToLocal(Position);

                LocalPositionX = p.X + (int)Offset.X;
                LocalPositionY = p.Y + (int)Offset.Y;
            }
        }

        internal void ForceUpdateLocalPosition(MapControl m)
        {
            if (m != null)
            {
                map = m;
            }
            UpdateLocalPosition();
        }

        public virtual void RegenerateRouteShape(MapControl map)
        {
            this.map = map;

            if (map != null && Route.Count > 1)
            {
                var localPath = new List<System.Windows.Point>();
                var offset = Map.FromLatLngToLocal(Route[0]);
                foreach (var i in Route)
                {
                    var p = Map.FromLatLngToLocal(new PointLatLng(i.Lat, i.Lng));
                    localPath.Add(new System.Windows.Point(p.X - offset.X, p.Y - offset.Y));
                }

                var shape = map.CreateRoutePath(localPath);

                if (this.Shape != null && this.Shape is Path)
                {
                    (this.Shape as Path).Data = shape.Data;
                }
                else
                {
                    this.Shape = shape;
                }
            }
        }

        public virtual void RegeneratePolygonShape(MapControl map)
        {
            this.map = map;

            if (map != null && Polygon.Count > 1)
            {
                var localPath = new List<System.Windows.Point>();
                var offset = Map.FromLatLngToLocal(Polygon[0]);
                foreach (var i in Polygon)
                {
                    var p = Map.FromLatLngToLocal(new PointLatLng(i.Lat, i.Lng));
                    localPath.Add(new System.Windows.Point(p.X - offset.X, p.Y - offset.Y));
                }

                var shape = map.CreatePolygonPath(localPath);

                if (this.Shape != null && this.Shape is Path)
                {
                    (this.Shape as Path).Data = shape.Data;
                }
                else
                {
                    this.Shape = shape;
                }
            }
        }
    }
}