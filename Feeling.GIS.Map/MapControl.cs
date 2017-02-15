
namespace Feeling.GIS.Map
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Effects;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Windows.Threading;
    using Feeling.GIS.Map.Core;
    using System.Diagnostics;
    //using MapMarker;

    /// <summary>
    /// WPF地图控件
    /// </summary>
    public partial class MapControl : ItemsControl, InterfaceMap
    {
        /// <summary>
        /// background of selected area
        /// </summary>
        private Brush SelectedAreaFill = new SolidColorBrush(Color.FromArgb(33, Colors.RoyalBlue.R, Colors.RoyalBlue.G, Colors.RoyalBlue.B));
        /// <summary>
        /// use circle for selection
        /// </summary>
        public bool SelectionUseCircle = false;

        /// <summary>
        /// current selected area in map
        /// </summary>
        private RectLatLng selectedArea;

        [Browsable(false)]
        public RectLatLng SelectedArea
        {
            get
            {
                return selectedArea;
            }
            set
            {
                selectedArea = value;
                InvalidateVisual();
            }
        }

        bool TouchEnabled = true;


        #region DependencyProperties and stuff



        public AccessMode MapAccessMode
        {
            get { return (AccessMode)GetValue(MapAccessModeProperty); }
            set { SetValue(MapAccessModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MapAccessMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MapAccessModeProperty =
            DependencyProperty.Register("MapAccessMode", typeof(AccessMode), typeof(MapControl), new UIPropertyMetadata(AccessMode.ServerAndCache, new PropertyChangedCallback(OnMapAccessModePropertyChanged)));

        private static void OnMapAccessModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MapControl mapControl = sender as MapControl;
            if (mapControl != null && e.NewValue != null && e.NewValue != e.OldValue)
            {
                mapControl.Manager.Mode = (AccessMode)e.NewValue;
            }
        }

        
      

        public static readonly DependencyProperty ZoomProperty
            = DependencyProperty.Register(
            "Zoom",
            typeof(double),
            typeof(MapControl),
            new UIPropertyMetadata(0.0, new PropertyChangedCallback(ZoomPropertyChanged), new CoerceValueCallback(OnCoerceZoom)));

        /// <summary>
        /// 地图缩放级别
        /// </summary>
        [Category("Map")]
        public double Zoom
        {
            get
            {
                return (double)(GetValue(ZoomProperty));
            }
            set
            {
                SetValue(ZoomProperty, value);
            }
        }

        private static object OnCoerceZoom(DependencyObject o, object value)
        {
            MapControl map = o as MapControl;
            if (map != null)
            {
                double result = (double)value;
                if (result > map.MaxZoom)
                {
                    result = map.MaxZoom;
                }
                if (result < map.MinZoom)
                {
                    result = map.MinZoom;
                }

                return result;
            }
            else
            {
                return value;
            }
        }

        private static void ZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MapControl map = (MapControl)d;
            if (map != null && map.Projection != null)
            {
                double value = (double)e.NewValue;

                Debug.WriteLine("Zoom: " + e.OldValue + " -> " + value);

                double remainder = value % 1;
                if (remainder != 0 && map.ActualWidth > 0)
                {
                    double scaleValue = remainder + 1;
                    {
                        map.MapRenderTransform = new ScaleTransform(scaleValue, scaleValue, map.ActualWidth / 2, map.ActualHeight / 2);
                    }

                    map.core.Zoom = Convert.ToInt32(value - remainder);

                    map.Core_OnMapZoomChanged();

                    map.InvalidateVisual();
                }
                else
                {
                    map.MapRenderTransform = null;
                    map.core.Zoom = Convert.ToInt32(value);
                    map.InvalidateVisual();
                }
            }
        }

        #endregion

        public readonly MapCore core = new MapCore();
        private MapRect region;
        private delegate void MethodInvoker();
        private PointLatLng selectionStart;
        private PointLatLng selectionEnd;
        private bool showTileGridLines = false;
        private MethodInvoker invalidator;

        
        /// <summary>
        /// 地图服务内容-城市
        /// </summary>
        public string MapServerContent
        {
            get
            {
                return this.Manager.CurrentMapName;
            }
            set
            {
                this.Manager.CurrentMapName = value;
            }
        }
        /// <summary>
        /// 地图数据服务器URL
        /// </summary>
        [Category("地图控件"), Description("地图数据服务器URL"), DisplayName("地图服务器")]
        public string MapServerURL
        {
            get
            {
                return this.Manager.MapServer;
            }
            set
            {
                if (this.Manager.MapServer != value)
                {
                    this.Manager.MapServer = value;
                }
            }
        }
        


        /// <summary>
        /// 地图最大缩放级别
        /// </summary>         
        [Category("Map")]
        [Description("地图最大缩放级别")]
        public int MaxZoom
        {
            get { return (int)GetValue(MaxZoomProperty); }
            set { SetValue(MaxZoomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxZoom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register("MaxZoom", typeof(int), typeof(MapControl), new UIPropertyMetadata(10));

        


        /// <summary>
        /// 地图最小缩放级别
        /// </summary>      
        [Category("Map")]
        [Description("地图最小缩放级别")]
        public int MinZoom
        {
            get { return (int)GetValue(MinZoomProperty); }
            set { SetValue(MinZoomProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MinZoom.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register("MinZoom", typeof(int), typeof(MapControl), new UIPropertyMetadata(1));

        



        /// <summary>
        /// 空瓦片背景
        /// </summary>
        private Brush EmptytileBrush = Brushes.Linen;

        /// <summary>
        /// 空瓦片信息
        /// </summary>
        private FormattedText EmptyTileText
            = new FormattedText(
                "在此缩放级别的区域中\n缺少地图瓦片",
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.Blue);

        /// <summary>
        /// 鼠标滑轮的缩放类型
        /// </summary>
        [Category("Map")]
        [Description("map zooming type for mouse wheel")]
        public MouseWheelZoomType MouseWheelZoomType
        {
            get
            {
                return core.MouseWheelZoomType;
            }
            set
            {
                core.MouseWheelZoomType = value;
            }
        }

        /// <summary>
        /// 地图Pan的鼠标按键
        /// </summary>
        [Category("Map")]
        public MouseButton DragButton = MouseButton.Left;


        /// <summary>
        /// 显示瓦片网格线
        /// </summary>
        [Category("Map")]
        public bool ShowTileGridLines
        {
            get
            {
                return showTileGridLines;
            }
            set
            {
                showTileGridLines = value;
                InvalidateVisual();
            }
        }


        /// <summary>
        /// 内存存储多少层的瓦片
        /// </summary>
        [Browsable(false)]
        public int LevelsKeepInMemmory
        {
            get
            {
                return core.LevelsKeepInMemmory;
            }

            set
            {
                core.LevelsKeepInMemmory = value;
            }
        }





        /// <summary>
        /// 地图边界
        /// </summary>
        private RectLatLng? BoundsOfMap = null;

        /// <summary>
        /// 地图标记集合
        /// </summary>
        private ObservableCollection<MapMarker> markers = new ObservableCollection<MapMarker>();

        public ObservableCollection<MapMarker> Markers
        {
            get { return markers; }
            set { markers = value; }
        }

        /// <summary>
        /// 当前地图二维变换
        /// </summary>
        internal Transform MapRenderTransform;

        /// <summary>
        /// current markers overlay offset
        /// </summary>
        internal readonly TranslateTransform MapTranslateTransform = new TranslateTransform();

        protected bool DesignModeInConstruct
        {
            get
            {
                return System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            }
        }

        //internal Canvas mapCanvas = null;
        internal Canvas mapCanvas = null;
        /// <summary>
        /// 标记容器Canvas
        /// </summary>
        internal Canvas MapCanvas
        {
            get
            {
                if (mapCanvas == null)
                {
                    if (this.VisualChildrenCount > 0)
                    {
                        Border border = VisualTreeHelper.GetChild(this, 0) as Border;
                        ItemsPresenter items = border.Child as ItemsPresenter;
                        DependencyObject target = VisualTreeHelper.GetChild(items, 0);
                        mapCanvas = target as Canvas;

                        mapCanvas.RenderTransform = MapTranslateTransform;
                    }
                }

                return mapCanvas;
            }
        }

        //internal ZoomableCanvas MapCanvas
        //{
        //    get
        //    {
        //        if (mapCanvas == null)
        //        {
        //            if (this.VisualChildrenCount > 0)
        //            {
        //                Border border = VisualTreeHelper.GetChild(this, 0) as Border;
        //                ItemsPresenter items = border.Child as ItemsPresenter;
        //                DependencyObject target = VisualTreeHelper.GetChild(items, 0);
        //                mapCanvas = target as ZoomableCanvas;

        //                mapCanvas.RenderTransform = MapTranslateTransform;
        //            }
        //        }

        //        return mapCanvas;
        //    }
        //}

        public MapsManager Manager
        {
            get
            {
                return MapsManager.Instance;
            }
        }


        public MapControl()
        {
            if (!DesignModeInConstruct)
            {
                #region -- 控件数据模板 --

                #region -- XAML --
                //  <ItemsControl Name="figures">
                //    <ItemsControl.ItemTemplate>
                //        <DataTemplate>
                //            <ContentPresenter Content="{Binding Path=Shape}" />
                //        </DataTemplate>
                //    </ItemsControl.ItemTemplate>
                //    <ItemsControl.ItemsPanel>
                //        <ItemsPanelTemplate>
                //            <Canvas />
                //        </ItemsPanelTemplate>
                //    </ItemsControl.ItemsPanel>
                //    <ItemsControl.ItemContainerStyle>
                //        <Style>
                //            <Setter Property="Canvas.Left" Value="{Binding Path=LocalPositionX}"/>
                //            <Setter Property="Canvas.Top" Value="{Binding Path=LocalPositionY}"/>
                //        </Style>
                //    </ItemsControl.ItemContainerStyle>
                //</ItemsControl> 
                #endregion

                DataTemplate dt = new DataTemplate(typeof(MapMarker));
                {
                    FrameworkElementFactory fef = new FrameworkElementFactory(typeof(ContentPresenter));
                    fef.SetBinding(ContentPresenter.ContentProperty, new Binding("Shape"));
                    dt.VisualTree = fef;
                }
                ItemTemplate = dt;

                FrameworkElementFactory factoryPanel = new FrameworkElementFactory(typeof(Canvas));
                {
                    factoryPanel.SetValue(Canvas.IsItemsHostProperty, true);

                    ItemsPanelTemplate template = new ItemsPanelTemplate();
                    template.VisualTree = factoryPanel;
                    ItemsPanel = template;
                }

                Style st = new Style();
                {
                    st.Setters.Add(new Setter(Canvas.LeftProperty, new Binding("LocalPositionX")));
                    st.Setters.Add(new Setter(Canvas.TopProperty, new Binding("LocalPositionY")));
                    st.Setters.Add(new Setter(Canvas.ZIndexProperty, new Binding("ZIndex")));
                }
                ItemContainerStyle = st;
                #endregion

                invalidator = new MethodInvoker(InvalidateVisual);

                ClipToBounds = true;
                SnapsToDevicePixels = true;

                Manager.ImageProxy = new WindowsPresentationImageProxy();

                core.RenderMode = RenderMode.WPF;
                core.OnNeedInvalidation += new NeedInvalidation(Core_OnNeedInvalidation);
                core.OnMapZoomChanged += new MapZoomChanged(Core_OnMapZoomChanged);
                Loaded += new RoutedEventHandler(GMapControl_Loaded);
                Unloaded += new RoutedEventHandler(GMapControl_Unloaded);
                SizeChanged += new SizeChangedEventHandler(GMapControl_SizeChanged);


                if (ItemsSource == null)
                {
                    ItemsSource = markers;
                }

                core.Zoom = (int)((double)ZoomProperty.DefaultMetadata.DefaultValue);

            }
        }


        void Current_Exit(object sender, ExitEventArgs e)
        {
            core.ApplicationExit();
        }

        protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (MapMarker marker in e.NewItems)
                {
                    marker.ForceUpdateLocalPosition(this);
                }
            }

            base.OnItemsChanged(e);
        }

        void GMapControl_Loaded(object sender, RoutedEventArgs e)
        {
            core.StartSystem();
            Core_OnMapZoomChanged();

            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle,
                   new Action(delegate()
                   {
                       if (Application.Current != null)
                       {
                           Application.Current.Exit += new ExitEventHandler(Current_Exit);
                       }
                   }
                   ));
            }
        }

        void GMapControl_Unloaded(object sender, RoutedEventArgs e)
        {
            core.OnMapClose();
        }

        void GMapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            System.Windows.Size constraint = e.NewSize;

            region = new MapRect(-50, -50, (int)constraint.Width + 100, (int)constraint.Height + 100);

            core.OnMapSizeChanged((int)constraint.Width, (int)constraint.Height);

            if (IsLoaded)
            {
                core.GoToCurrentPosition();

                if (IsRotated)
                {
                    UpdateRotationMatrix();
                    Core_OnMapZoomChanged();
                }
                else
                {
                    UpdateMarkersOffset();
                }
            }
        }

        void Core_OnMapZoomChanged()
        {
            UpdateMarkersOffset();

            foreach (MapMarker i in ItemsSource)
            {
                if (i != null)
                {
                    i.ForceUpdateLocalPosition(this);

                    if (i.Route.Count > 0)
                    {
                        i.RegenerateRouteShape(this);
                    }

                    if (i.Polygon.Count > 0)
                    {
                        i.RegeneratePolygonShape(this);
                    }
                }
            }
        }


        void Core_OnNeedInvalidation()
        {
            try
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Render, invalidator);
            }
            catch
            {
            }
        }

        public void UpdateMarkersOffset()
        {
            if (MapCanvas != null)
            {
                if (MapRenderTransform != null)
                {
                    var tp = MapRenderTransform.Transform(new System.Windows.Point(core.renderOffset.X, core.renderOffset.Y));
                    MapTranslateTransform.X = tp.X;
                    MapTranslateTransform.Y = tp.Y;
                }
                else
                {
                    MapTranslateTransform.X = core.renderOffset.X;
                    MapTranslateTransform.Y = core.renderOffset.Y;
                }
            }
        }

        public Brush EmptyMapBackground = Brushes.WhiteSmoke;

        //private List<Elevator> elevatorList = new List<Elevator>();

        //private Dictionary<PointLatLng, List<Elevator>> MarkersList = new Dictionary<PointLatLng, List<Elevator>>();

        //public void GenerateMapMarker(string[] elevatorInfos)
        //{
        //    foreach (string str in elevatorInfos)
        //    {
        //        if (string.IsNullOrEmpty(str))
        //        {
        //            continue;
        //        }
        //        string[] strArray = str.Split(new char[] { ';' });
        //        if (strArray.Length == 11)
        //        {
        //            Elevator item = new Elevator();
        //            item.Name = strArray[0];
        //            item.User = strArray[1];
        //            item.UserPhone = strArray[2];
        //            item.MaintenancePerson = strArray[3];
        //            item.MaintenancePersonPhone = strArray[4];
        //            item.Manager = strArray[5];
        //            item.ManagerPhone = strArray[6];
        //            item.FaultType = strArray[7];
        //            item.FaultTime = strArray[8];
        //            item.Position = strArray[9];
        //            item.ID = strArray[10];
        //            this.elevatorList.Add(item);
        //        }
        //    }
        //    using (List<Elevator>.Enumerator enumerator = this.elevatorList.GetEnumerator())
        //    {
        //        Predicate<Elevator> match = null;
        //        Elevator ele;
        //        while (enumerator.MoveNext())
        //        {
        //            ele = enumerator.Current;
        //            List<Elevator> list = new List<Elevator>();
        //            PointLatLng empty = new PointLatLng();
        //            if (match == null)
        //            {
        //                match = delegate(Elevator T)
        //                {
        //                    return T.Position.Equals(ele.Position);
        //                };
        //            }
        //            list = this.elevatorList.FindAll(match);
        //            string[] strArray2 = ele.Position.Split(new char[] { ',' });
        //            if (strArray2.Length == 2)
        //            {
        //                empty = new PointLatLng(Convert.ToDouble(strArray2[0]), Convert.ToDouble(strArray2[1]));
        //            }
        //            if (!this.MarkersList.ContainsKey(empty))
        //            {
        //                this.MarkersList.Add(empty, list);
        //            }
        //        }
        //    }
        //    foreach (PointLatLng lng2 in this.MarkersList.Keys)
        //    {
        //        MapMarker marker = new MapMarker(lng2);
        //        ElevatorControl control = new ElevatorControl();
        //        control.IsHitTestVisible = true;
        //        foreach (Elevator elevator2 in this.MarkersList[lng2])
        //        {
        //            control.Elevators.Add(elevator2);
        //            if (!string.IsNullOrEmpty(elevator2.FaultType))
        //            {
        //                control.ElevatorStateColor = Brushes.Red;
        //            }
        //        }
        //        marker.Shape = control;
        //        marker.Offset = new Point(-control.ActualWidth / 2.0, -control.ActualHeight);
        //        marker.ZIndex = 0x7ffffffd;
        //        this.markers.Add(marker);
        //    }
        //}

        //public void UpdataElevatorInfo(string[] elevatorInfos)
        //{
        //    foreach (string str in elevatorInfos)
        //    {
        //        if (string.IsNullOrEmpty(str))
        //        {
        //            continue;
        //        }

        //        string[] strArray = str.Split(new char[] { ';' });
        //        if (strArray.Length == 11)
        //        {
        //            string[] positionStr = strArray[9].Split(new char[] { ',' });
        //            PointLatLng position;
        //            if (positionStr.Length == 2)
        //            {
        //                bool stateFlag = false;

        //                position = new PointLatLng(Convert.ToDouble(positionStr[0]), Convert.ToDouble(positionStr[1]));

        //                var query1 = from pairs in MarkersList
        //                             where pairs.Key.Equals(position)
        //                             select pairs;
        //                foreach (KeyValuePair<PointLatLng, List<Elevator>> pair in query1)
        //                {
        //                    Elevator elevator = pair.Value.First(T => T.ID.Equals(strArray[10]));
        //                    elevator.Name = strArray[0];
        //                    elevator.User = strArray[1];
        //                    elevator.UserPhone = strArray[2];
        //                    elevator.MaintenancePerson = strArray[3];
        //                    elevator.MaintenancePersonPhone = strArray[4];
        //                    elevator.Manager = strArray[5];
        //                    elevator.ManagerPhone = strArray[6];
        //                    elevator.FaultType = strArray[7];
        //                    elevator.FaultTime = strArray[8];

        //                    if (!string.IsNullOrEmpty(elevator.FaultType))
        //                        stateFlag = true;
        //                }

        //                MapMarker marker = markers.First(T => T.Position.Equals(position));
        //                if (stateFlag)
        //                    (marker.Shape as ElevatorControl).ElevatorStateColor = Brushes.Red;
        //                else
        //                    (marker.Shape as ElevatorControl).ElevatorStateColor = Brushes.Green;


        //            }
        //        }
        //    }


        //}


        public void MoveMap(int offsetX, int offsetY)
        {
            MapPoint point = new MapPoint();
            point = this.FromLatLngToLocal(this.Position);
            this.core.mouseDown.X = point.X;
            this.core.mouseDown.Y = point.Y;
            base.Cursor = Cursors.Hand;
            this.core.BeginDrag(this.core.mouseDown);
            this.core.mouseCurrent.X = point.X + offsetX;
            this.core.mouseCurrent.Y = point.Y + offsetY;
            this.core.Drag(this.core.mouseCurrent);
            this.UpdateMarkersOffset();
            base.InvalidateVisual();
            this.core.EndDrag();
        }

        #region --- 弃用 ---
        //public void GenerateSubStationMarker(string name, string id, Point position, bool? status)
        //{
        //    MapMarker marker = new MapMarker(new PointLatLng(position.X, position.Y));
        //    SubStationMarker control = new SubStationMarker(name, id);
        //    control.OnSubStationMarkerMouseDoubleClick += new SubStationMarker.SubStationMarkerMouseDoubleClick(control_OnSubStationMarkerMouseDoubleClick);
        //    control.IsHitTestVisible = true;
        //    if (status == true)
        //        control.StateColor = Brushes.Red;
        //    else
        //        control.StateColor = Brushes.Green;
        //    marker.Shape = control;
        //    marker.Offset = new Point(-control.ActualWidth / 2.0, -control.ActualHeight);
        //    marker.ZIndex = 0x7ffffffd;
        //    this.markers.Add(marker);
        //}

        //void control_OnSubStationMarkerMouseDoubleClick(string id)
        //{
        //    if (OnSubStationTriggerInfo != null)
        //    {
        //        OnSubStationTriggerInfo(id);
        //    }
        //}

        //public void GenerateTechnologyStationMarker(string name, string id, Point position, bool? status)
        //{
        //    MapMarker marker = new MapMarker(new PointLatLng(position.X, position.Y));
        //    TechnologyStationMarker control = new TechnologyStationMarker(name, id);
        //    control.OnTechnologyStationMarkerMouseDoubleClick += new TechnologyStationMarker.TechnologyStationMarkerMouseDoubleClick(control_OnTechnologyStationMarkerMouseDoubleClick);
        //    control.IsHitTestVisible = true;
        //    if (status == true)
        //        control.StateColor = Brushes.Red;
        //    else
        //        control.StateColor = Brushes.Green;
        //    marker.Shape = control;
        //    marker.Offset = new Point(-control.Width / 2.0, -control.Height);
        //    marker.ZIndex = 0x7ffffffd;
        //    this.markers.Add(marker);
        //}

        //void control_OnTechnologyStationMarkerMouseDoubleClick(string id)
        //{
        //    if (OnTechnologyStationTriggerInfo != null)
        //    {
        //        OnTechnologyStationTriggerInfo(id);
        //    }
        //}

        //public void UpdataDeviceMarker(string id, bool? status)
        //{
        //    //if (markers.Count == 0)
        //    //    return;
        //    //foreach (MapMarker marker in this.markers)
        //    //{
        //    //    DeviceMarkerControl deviceControl = marker.Shape as DeviceMarkerControl;
        //    //    if (deviceControl != null && deviceControl.DeviceID.Equals(id))
        //    //    {
        //    //        if (status == true)
        //    //            deviceControl.StateColor = Brushes.Red;
        //    //        else
        //    //            deviceControl.StateColor = Brushes.Green;
        //    //    }
        //    //}


        //}

        //public delegate void SubStationTriggerInfo(string id);

        //public event SubStationTriggerInfo OnSubStationTriggerInfo;

        //public delegate void TechnologyStationTriggerInfo(string id);

        //public event TechnologyStationTriggerInfo OnTechnologyStationTriggerInfo;
        #endregion

        public void ClearAllMarkers()
        {
            this.markers.Clear();
        }

        void DrawMapWPF(DrawingContext g)
        {
            if (MapTileType == MapType.None)
            {
                return;
            }

            core.Matrix.EnterReadLock();
            core.tileDrawingListLock.AcquireReaderLock();
            try
            {
                foreach (var tilePoint in core.tileDrawingList)
                {
                    core.tileRect.X = tilePoint.X * core.tileRect.Width;
                    core.tileRect.Y = tilePoint.Y * core.tileRect.Height;
                    core.tileRect.Offset(core.renderOffset);

                    if (region.IntersectsWith(core.tileRect) || IsRotated)
                    {
                        bool found = false;

                        Tile t = core.Matrix.GetTileWithNoLock(core.Zoom, tilePoint);
                        if (t != null)
                        {
                            lock (t.Overlays)
                            {
                                foreach (WindowsPresentationImage img in t.Overlays)
                                {
                                    if (img != null && img.Img != null)
                                    {
                                        if (!found)
                                            found = true;

                                        g.DrawImage(img.Img, new Rect(core.tileRect.X + 0.6, core.tileRect.Y + 0.6, core.tileRect.Width + 0.6, core.tileRect.Height + 0.6));
                                    }
                                }
                            }
                        }

                        else if (Projection is MercatorProjection)
                        {
                            #region -- fill empty tiles --
                            int zoomOffset = 1;
                            Tile parentTile = null;
                            int Ix = 0;

                            while (parentTile == null && zoomOffset < core.Zoom && zoomOffset <= LevelsKeepInMemmory)
                            {
                                Ix = (int)Math.Pow(2, zoomOffset);
                                parentTile = core.Matrix.GetTileWithNoLock(core.Zoom - zoomOffset++, new MapPoint((int)(tilePoint.X / Ix), (int)(tilePoint.Y / Ix)));
                            }

                            if (parentTile != null)
                            {
                                int Xoff = Math.Abs(tilePoint.X - (parentTile.Pos.X * Ix));
                                int Yoff = Math.Abs(tilePoint.Y - (parentTile.Pos.Y * Ix));

                                var geometry = new RectangleGeometry(new Rect(core.tileRect.X + 0.6, core.tileRect.Y + 0.6, core.tileRect.Width + 0.6, core.tileRect.Height + 0.6));
                                var parentImgRect = new Rect(core.tileRect.X - core.tileRect.Width * Xoff + 0.6, core.tileRect.Y - core.tileRect.Height * Yoff + 0.6, core.tileRect.Width * Ix + 0.6, core.tileRect.Height * Ix + 0.6);

                                // render tile 
                                lock (parentTile.Overlays)
                                {
                                    foreach (WindowsPresentationImage img in parentTile.Overlays)
                                    {
                                        if (img != null && img.Img != null)
                                        {
                                            if (!found)
                                                found = true;

                                            g.PushClip(geometry);
                                            g.DrawImage(img.Img, parentImgRect);
                                            g.DrawRectangle(SelectedAreaFill, null, geometry.Bounds);
                                            g.Pop();
                                        }
                                    }
                                }

                                geometry = null;
                            }
                            #endregion
                        }
                        //else 
                        //{
                        //   int ZoomOffset = 0;
                        //   Tile ParentTile = null;
                        //   int Ix = 0;

                        //   while(ParentTile == null && (MapCore.Zoom - ZoomOffset) >= 1 && ZoomOffset <= LevelsKeepInMemmory)
                        //   {
                        //      Ix = (int) Math.Pow(2, ++ZoomOffset);
                        //      ParentTile = MapCore.Matrix.GetTileWithNoLock(MapCore.Zoom - ZoomOffset, new Map.Point((int) (tilePoint.X / Ix), (int) (tilePoint.Y / Ix)));
                        //   }

                        //   if(ParentTile != null)
                        //   {
                        //      int Xoff = Math.Abs(tilePoint.X - (ParentTile.Pos.X * Ix));
                        //      int Yoff = Math.Abs(tilePoint.Y - (ParentTile.Pos.Y * Ix));

                        //      // render tile 
                        //      lock(ParentTile.Overlays)
                        //      {
                        //         foreach(WindowsPresentationImage img in ParentTile.Overlays)
                        //         {
                        //            if(img != null && img.Img != null)
                        //            {
                        //               if(!found)
                        //                  found = true;

                        //               //System.Drawing.RectangleF srcRect = new System.Drawing.RectangleF((float) (Xoff * (img.Img.Width / Ix)), (float) (Yoff * (img.Img.Height / Ix)), (img.Img.Width / Ix), (img.Img.Height / Ix));
                        //               //System.Drawing.Rectangle dst = new System.Drawing.Rectangle(MapCore.tileRect.X, MapCore.tileRect.Y, MapCore.tileRect.Width, MapCore.tileRect.Height);

                        //               //g.DrawImage(img.Img, new Rect(

                        //               //g.DrawImage(img.Img, dst, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, TileFlipXYAttributes);
                        //               //g.FillRectangle(SelectedAreaFill, dst);
                        //            }
                        //         }
                        //      }
                        //   }
                        //}

                        if (!found)
                        {
                            lock (core.FailedLoads)
                            {
                                var lt = new LoadTask(tilePoint, core.Zoom);

                                if (core.FailedLoads.ContainsKey(lt))
                                {
                                    g.DrawRectangle(EmptytileBrush, new Pen(Brushes.White, 1.0), new Rect(core.tileRect.X, core.tileRect.Y, core.tileRect.Width, core.tileRect.Height));

                                    var ex = core.FailedLoads[lt];
                                    FormattedText TileText = new FormattedText("地图数据装载错误: " + ex.Message, new CultureInfo("zh-CN"), FlowDirection.LeftToRight, new Typeface("Arial"), 14, Brushes.Red);

                                    TileText.MaxTextWidth = core.tileRect.Width - 11;

                                    g.DrawText(TileText, new System.Windows.Point(core.tileRect.X + 11, core.tileRect.Y + 11));

                                    g.DrawText(EmptyTileText, new System.Windows.Point(core.tileRect.X + core.tileRect.Width / 2 - EmptyTileText.Width / 2, core.tileRect.Y + core.tileRect.Height / 2 - EmptyTileText.Height / 2));
                                }
                            }
                        }

                        if (ShowTileGridLines)
                        {
                            g.DrawRectangle(null, new Pen(Brushes.White, 1.0), new Rect(core.tileRect.X, core.tileRect.Y, core.tileRect.Width, core.tileRect.Height));

                            if (tilePoint == core.centerTileXYLocation)
                            {
                                FormattedText TileText = new FormattedText("地图中心瓦片:" + tilePoint.ToString(), System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Arial"), 16, Brushes.Red);
                                g.DrawText(TileText, new System.Windows.Point(core.tileRect.X + core.tileRect.Width / 2 - EmptyTileText.Width / 2, core.tileRect.Y + core.tileRect.Height / 2 - TileText.Height / 2));
                            }
                            else
                            {
                                FormattedText TileText = new FormattedText("地图瓦片: " + tilePoint.ToString(), System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Arial"), 16, Brushes.Red);
                                g.DrawText(TileText, new System.Windows.Point(core.tileRect.X + core.tileRect.Width / 2 - EmptyTileText.Width / 2, core.tileRect.Y + core.tileRect.Height / 2 - TileText.Height / 2));
                            }
                        }
                    }
                }
            }
            finally
            {
                core.tileDrawingListLock.ReleaseReaderLock();
                core.Matrix.LeaveReadLock();
            }
        }

        public ImageSource ToImageSource()
        {
            FrameworkElement obj = this;

            Transform transform = obj.LayoutTransform;
            obj.LayoutTransform = null;

            Thickness margin = obj.Margin;
            obj.Margin = new Thickness(0, 0,
            margin.Right - margin.Left, margin.Bottom - margin.Top);

            System.Windows.Size size = new System.Windows.Size(obj.ActualWidth, obj.ActualHeight);

            // force control to Update
            obj.Measure(size);
            obj.Arrange(new Rect(size));

            RenderTargetBitmap bmp = new RenderTargetBitmap(
            (int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(obj);

            if (bmp.CanFreeze)
            {
                bmp.Freeze();
            }

            obj.LayoutTransform = transform;
            obj.Margin = margin;

            return bmp;
        }

        public virtual Path CreateRoutePath(List<System.Windows.Point> localPath)
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(localPath[0], false, false);

                ctx.PolyLineTo(localPath, true, true);
            }

            geometry.Freeze();

            Path myPath = new Path();
            {
                myPath.Data = geometry;

                BlurEffect ef = new BlurEffect();
                {
                    ef.KernelType = KernelType.Gaussian;
                    ef.Radius = 3.0;
                    ef.RenderingBias = RenderingBias.Quality;
                }

                myPath.Effect = ef;

                myPath.Stroke = Brushes.Navy;
                myPath.StrokeThickness = 5;
                myPath.StrokeLineJoin = PenLineJoin.Round;
                myPath.StrokeStartLineCap = PenLineCap.Triangle;
                myPath.StrokeEndLineCap = PenLineCap.Square;

                myPath.Opacity = 0.6;
                myPath.IsHitTestVisible = false;
            }
            return myPath;
        }

        public virtual Path CreatePolygonPath(List<System.Windows.Point> localPath)
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(localPath[0], true, true);
                ctx.PolyLineTo(localPath, true, true);
            }

            geometry.Freeze();

            Path myPath = new Path();
            {
                myPath.Data = geometry;

                BlurEffect ef = new BlurEffect();
                {
                    ef.KernelType = KernelType.Gaussian;
                    ef.Radius = 3.0;
                    ef.RenderingBias = RenderingBias.Quality;
                }

                myPath.Effect = ef;

                myPath.Stroke = Brushes.MidnightBlue;
                myPath.StrokeThickness = 5;
                myPath.StrokeLineJoin = PenLineJoin.Round;
                myPath.StrokeStartLineCap = PenLineCap.Triangle;
                myPath.StrokeEndLineCap = PenLineCap.Square;

                myPath.Fill = Brushes.AliceBlue;

                myPath.Opacity = 0.6;
                myPath.IsHitTestVisible = false;
            }
            return myPath;
        }

        public bool SetZoomToFitRect(RectLatLng rect)
        {
            int maxZoom = core.GetMaxZoomToFitRect(rect);
            if (maxZoom > 0)
            {
                PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
                Position = center;

                if (maxZoom > MaxZoom)
                {
                    maxZoom = MaxZoom;
                }

                if (core.Zoom != maxZoom)
                {
                    Zoom = maxZoom;
                }

                return true;
            }
            return false;
        }

        public bool ZoomAndCenterMarkers(int? ZIndex)
        {
            RectLatLng? rect = GetRectOfAllMarkers(ZIndex);
            if (rect.HasValue)
            {
                return SetZoomToFitRect(rect.Value);
            }

            return false;
        }

        public RectLatLng? GetRectOfAllMarkers(int? ZIndex)
        {
            RectLatLng? ret = null;

            double left = double.MaxValue;
            double top = double.MinValue;
            double right = double.MinValue;
            double bottom = double.MaxValue;
            IEnumerable<MapMarker> Overlays;

            if (ZIndex.HasValue)
            {
                Overlays = ItemsSource.Cast<MapMarker>().Where(p => p != null && p.ZIndex == ZIndex);
            }
            else
            {
                Overlays = ItemsSource.Cast<MapMarker>();
            }

            if (Overlays != null)
            {
                foreach (var m in Overlays)
                {
                    if (m.Shape != null)//&& m.Shape.IsVisible
                    {
                        // left
                        if (m.Position.Lng < left)
                        {
                            left = m.Position.Lng;
                        }

                        // top
                        if (m.Position.Lat > top)
                        {
                            top = m.Position.Lat;
                        }

                        // right
                        if (m.Position.Lng > right)
                        {
                            right = m.Position.Lng;
                        }

                        // bottom
                        if (m.Position.Lat < bottom)
                        {
                            bottom = m.Position.Lat;
                        }
                    }
                }
            }

            if (left != double.MaxValue && right != double.MinValue && top != double.MinValue && bottom != double.MaxValue)
            {
                ret = RectLatLng.FromLTRB(left, top, right, bottom);
            }

            return ret;
        }

        public void Offset(int x, int y)
        {
            if (IsLoaded)
            {
                core.DragOffset(new MapPoint(x, y));
                UpdateMarkersOffset();
            }
        }

        readonly RotateTransform rotationMatrix = new RotateTransform();
        GeneralTransform rotationMatrixInvert = new RotateTransform();

        void UpdateRotationMatrix()
        {
            System.Windows.Point center = new System.Windows.Point(ActualWidth / 2.0, ActualHeight / 2.0);

            rotationMatrix.Angle = -Bearing;
            rotationMatrix.CenterY = center.Y;
            rotationMatrix.CenterX = center.X;

            rotationMatrixInvert = rotationMatrix.Inverse;
        }

        public bool IsRotated
        {
            get
            {
                return core.IsRotated;
            }
        }

        [Category("Map")]
        public float Bearing
        {
            get
            {
                return core.bearing;
            }
            set
            {
                if (core.bearing != value)
                {
                    bool resize = core.bearing == 0;
                    core.bearing = value;

                    UpdateRotationMatrix();

                    if (value != 0 && value % 360 != 0)
                    {
                        core.IsRotated = true;

                        if (core.tileRectBearing.Size == core.tileRect.Size)
                        {
                            core.tileRectBearing = core.tileRect;
                            core.tileRectBearing.Inflate(1, 1);
                        }
                    }
                    else
                    {
                        core.IsRotated = false;
                        core.tileRectBearing = core.tileRect;
                    }

                    if (resize)
                    {
                        core.OnMapSizeChanged((int)ActualWidth, (int)ActualHeight);
                    }

                    Core_OnMapZoomChanged();

                    InvalidateVisual();
                }
            }
        }

        System.Windows.Point ApplyRotation(double x, double y)
        {
            System.Windows.Point ret = new System.Windows.Point(x, y);

            if (IsRotated)
            {
                ret = rotationMatrix.Transform(ret);
            }

            return ret;
        }

        System.Windows.Point ApplyRotationInversion(double x, double y)
        {
            System.Windows.Point ret = new System.Windows.Point(x, y);

            if (IsRotated)
            {
                ret = rotationMatrixInvert.Transform(ret);
            }

            return ret;
        }

        #region UserControl Events
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!core.IsStarted)
                return;

            drawingContext.DrawRectangle(EmptyMapBackground, null, new Rect(RenderSize));

            if (IsRotated)
            {
                drawingContext.PushTransform(rotationMatrix);

                if (MapRenderTransform != null)
                {
                    drawingContext.PushTransform(MapRenderTransform);
                    {
                        DrawMapWPF(drawingContext);
                    }
                    drawingContext.Pop();
                }
                else
                {
                    DrawMapWPF(drawingContext);
                }

                drawingContext.Pop();
            }
            else
            {
                if (MapRenderTransform != null)
                {
                    drawingContext.PushTransform(MapRenderTransform);
                    {
                        DrawMapWPF(drawingContext);
                    }
                    drawingContext.Pop();
                }
                else
                {
                    DrawMapWPF(drawingContext);
                }
            }

            if (!SelectedArea.IsEmpty)
            {
                MapPoint p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
                MapPoint p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

                p1.Offset((int)MapTranslateTransform.X, (int)MapTranslateTransform.Y);
                p2.Offset((int)MapTranslateTransform.X, (int)MapTranslateTransform.Y);

                int x1 = p1.X;
                int y1 = p1.Y;
                int x2 = p2.X;
                int y2 = p2.Y;

                if (SelectionUseCircle)
                {
                    drawingContext.DrawEllipse(SelectedAreaFill, new Pen(Brushes.Blue, 2.0), new System.Windows.Point(x1 + (x2 - x1) / 2, y1 + (y2 - y1) / 2), (x2 - x1) / 2, (y2 - y1) / 2);
                }
                else
                {
                    drawingContext.DrawRoundedRectangle(SelectedAreaFill, new Pen(Brushes.Blue, 2.0), new Rect(x1, y1, x2 - x1, y2 - y1), 5, 5);
                }
            }

            if (ShowCenter)
            {
                drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point((ActualWidth / 2) - 10, ActualHeight / 2), new System.Windows.Point((ActualWidth / 2) + 10, ActualHeight / 2));
                drawingContext.DrawLine(CenterCrossPen, new System.Windows.Point(ActualWidth / 2, (ActualHeight / 2) - 10), new System.Windows.Point(ActualWidth / 2, (ActualHeight / 2) + 10));
            }

            base.OnRender(drawingContext);
        }

        public Pen CenterCrossPen = new Pen(Brushes.Red, 1);
        public bool ShowCenter = true;

        public bool InvertedMouseWheelZooming = false;

        public bool IgnoreMarkerOnMouseWheel = false;

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if ((IsMouseDirectlyOver || IgnoreMarkerOnMouseWheel) && !core.IsDragging)
            {
                System.Windows.Point p = e.GetPosition(this);
                //p = ApplyRotationInversion(p.X, p.Y);

                if (core.mouseLastZoom.X != (int)p.X && core.mouseLastZoom.Y != (int)p.Y)
                {
                    if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
                    {
                        core.currentPosition = FromLocalToLatLng((int)p.X, (int)p.Y);
                    }
                    else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
                    {
                        core.currentPosition = FromLocalToLatLng((int)ActualWidth / 2, (int)ActualHeight / 2);
                    }
                    else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
                    {
                        core.currentPosition = FromLocalToLatLng((int)p.X, (int)p.Y);
                    }

                    core.mouseLastZoom.X = (int)p.X;
                    core.mouseLastZoom.Y = (int)p.Y;
                }

                if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
                {
                    System.Windows.Point ps = PointToScreen(new System.Windows.Point(ActualWidth / 2, ActualHeight / 2));
                    Stuff.SetCursorPos((int)ps.X, (int)ps.Y);
                }

                core.MouseWheelZooming = true;

                if (e.Delta > 0)
                {
                    if (!InvertedMouseWheelZooming)
                    {
                        Zoom = ((int)Zoom) + 1;
                    }
                    else
                    {
                        Zoom = ((int)(Zoom + 0.99)) - 1;
                    }
                }
                else
                {
                    if (InvertedMouseWheelZooming)
                    {
                        Zoom = ((int)Zoom) + 1;
                    }
                    else
                    {
                        Zoom = ((int)(Zoom + 0.99)) - 1;
                    }
                }

                core.MouseWheelZooming = false;
            }

            base.OnMouseWheel(e);
        }

        bool isSelected = false;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (CanDragMap && e.ChangedButton == DragButton && e.ButtonState == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(this);

                if (MapRenderTransform != null)
                {
                    p = MapRenderTransform.Inverse.Transform(p);
                }

                p = ApplyRotationInversion(p.X, p.Y);

                core.mouseDown.X = (int)p.X;
                core.mouseDown.Y = (int)p.Y;

                InvalidateVisual();
            }
            else
            {
                if (!isSelected)
                {
                    Point p = e.GetPosition(this);
                    isSelected = true;
                    SelectedArea = RectLatLng.Empty;
                    selectionEnd = PointLatLng.Zero;
                    selectionStart = FromLocalToLatLng((int)p.X, (int)p.Y);
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (isSelected)
            {
                isSelected = false;
            }

            if (core.IsDragging)
            {
                if (isDragging)
                {
                    Mouse.Capture(null);

                    isDragging = false;
                    Debug.WriteLine("IsDragging = " + isDragging);
                    Cursor = cursorBefore;
                }
                core.EndDrag();

                if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                {
                    if (core.LastLocationInBounds.HasValue)
                    {
                        Position = core.LastLocationInBounds.Value;
                    }
                }
            }
            else
            {
                if (!selectionEnd.IsZero && !selectionStart.IsZero)
                {
                    if (!SelectedArea.IsEmpty && Keyboard.Modifiers == ModifierKeys.Shift)
                    {
                        SetZoomToFitRect(SelectedArea);
                    }
                }
                else
                {
                    if (e.ChangedButton == DragButton)
                    {
                        core.mouseDown = MapPoint.Empty;
                    }
                    InvalidateVisual();
                }
            }

            base.OnMouseUp(e);
        }

        Cursor cursorBefore = Cursors.Arrow;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!core.IsDragging && !core.mouseDown.IsEmpty)
            {
                Point p = e.GetPosition(this);

                if (MapRenderTransform != null)
                {
                    p = MapRenderTransform.Inverse.Transform(p);
                }

                p = ApplyRotationInversion(p.X, p.Y);

                if (Math.Abs(p.X - core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(p.Y - core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
                {
                    core.BeginDrag(core.mouseDown);
                }
            }

            if (core.IsDragging)
            {
                if (!isDragging)
                {
                    Mouse.Capture(this);

                    isDragging = true;
                    Debug.WriteLine("IsDragging = " + isDragging);

                    cursorBefore = Cursor;
                    Cursor = Cursors.SizeAll;
                }

                if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                {
                    // ...
                }
                else
                {
                    Point p = e.GetPosition(this);

                    if (MapRenderTransform != null)
                    {
                        p = MapRenderTransform.Inverse.Transform(p);
                    }

                    p = ApplyRotationInversion(p.X, p.Y);

                    core.mouseCurrent.X = (int)p.X;
                    core.mouseCurrent.Y = (int)p.Y;
                    {
                        core.Drag(core.mouseCurrent);
                    }

                    if (IsRotated)
                    {
                        Core_OnMapZoomChanged();
                    }
                    else
                    {
                        UpdateMarkersOffset();
                    }
                }
                InvalidateVisual();
            }
            else
            {
                if (isSelected && !selectionStart.IsZero && (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Alt))
                {
                    System.Windows.Point p = e.GetPosition(this);
                    selectionEnd = FromLocalToLatLng((int)p.X, (int)p.Y);
                    {
                        PointLatLng p1 = selectionStart;
                        PointLatLng p2 = selectionEnd;

                        double x1 = Math.Min(p1.Lng, p2.Lng);
                        double y1 = Math.Max(p1.Lat, p2.Lat);
                        double x2 = Math.Max(p1.Lng, p2.Lng);
                        double y2 = Math.Min(p1.Lat, p2.Lat);

                        SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
                    }
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnStylusDown(StylusDownEventArgs e)
        {
            if (TouchEnabled && CanDragMap && !e.InAir)
            {
                Point p = e.GetPosition(this);

                if (MapRenderTransform != null)
                {
                    p = MapRenderTransform.Inverse.Transform(p);
                }

                p = ApplyRotationInversion(p.X, p.Y);

                core.mouseDown.X = (int)p.X;
                core.mouseDown.Y = (int)p.Y;

                InvalidateVisual();
            }

            base.OnStylusDown(e);
        }

        protected override void OnStylusUp(StylusEventArgs e)
        {
            if (TouchEnabled)
            {
                if (isSelected)
                {
                    isSelected = false;
                }

                if (core.IsDragging)
                {
                    if (isDragging)
                    {
                        Mouse.Capture(null);

                        isDragging = false;
                        Debug.WriteLine("IsDragging = " + isDragging);
                        Cursor = cursorBefore;
                    }
                    core.EndDrag();

                    if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                    {
                        if (core.LastLocationInBounds.HasValue)
                        {
                            Position = core.LastLocationInBounds.Value;
                        }
                    }
                }
                else
                {
                    core.mouseDown = MapPoint.Empty;
                    InvalidateVisual();
                }
            }
            base.OnStylusUp(e);
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            if (TouchEnabled)
            {
                if (!core.IsDragging && !core.mouseDown.IsEmpty)
                {
                    Point p = e.GetPosition(this);

                    if (MapRenderTransform != null)
                    {
                        p = MapRenderTransform.Inverse.Transform(p);
                    }

                    p = ApplyRotationInversion(p.X, p.Y);

                    if (Math.Abs(p.X - core.mouseDown.X) * 2 >= SystemParameters.MinimumHorizontalDragDistance || Math.Abs(p.Y - core.mouseDown.Y) * 2 >= SystemParameters.MinimumVerticalDragDistance)
                    {
                        core.BeginDrag(core.mouseDown);
                    }
                }

                if (core.IsDragging)
                {
                    if (!isDragging)
                    {
                        Mouse.Capture(this);

                        isDragging = true;
                        Debug.WriteLine("IsDragging = " + isDragging);

                        cursorBefore = Cursor;
                        Cursor = Cursors.SizeAll;
                    }

                    if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                    {
                        // ...
                    }
                    else
                    {
                        Point p = e.GetPosition(this);

                        if (MapRenderTransform != null)
                        {
                            p = MapRenderTransform.Inverse.Transform(p);
                        }

                        p = ApplyRotationInversion(p.X, p.Y);

                        core.mouseCurrent.X = (int)p.X;
                        core.mouseCurrent.Y = (int)p.Y;
                        {
                            core.Drag(core.mouseCurrent);
                        }

                        if (IsRotated)
                        {
                            Core_OnMapZoomChanged();
                        }
                        else
                        {
                            UpdateMarkersOffset();
                        }
                    }
                    InvalidateVisual();
                }
            }

            base.OnStylusMove(e);
        }

        #endregion

        #region IGControl Members

        public void ReloadMap()
        {
            core.ReloadMap();
        }

        public GeoCoderStatusCode SetCurrentPositionByKeywords(string keys)
        {
            GeoCoderStatusCode status = GeoCoderStatusCode.Unknow;
            PointLatLng? pos = Manager.GetLatLngFromGeocoder(keys, out status);
            if (pos.HasValue && status == GeoCoderStatusCode.G_GEO_SUCCESS)
            {
                Position = pos.Value;
            }

            return status;
        }

        public PointLatLng FromLocalToLatLng(int x, int y)
        {
            if (MapRenderTransform != null)
            {
                var tp = MapRenderTransform.Inverse.Transform(new System.Windows.Point(x, y));
                x = (int)tp.X;
                y = (int)tp.Y;
            }

            if (IsRotated)
            {
                var f = rotationMatrixInvert.Transform(new System.Windows.Point(x, y));

                x = (int)f.X;
                y = (int)f.Y;
            }

            return core.FromLocalToLatLng(x, y);
        }

        public MapPoint FromLatLngToLocal(PointLatLng point)
        {
            MapPoint ret = core.FromLatLngToLocal(point);

            if (MapRenderTransform != null)
            {
                var tp = MapRenderTransform.Transform(new System.Windows.Point(ret.X, ret.Y));
                ret.X = (int)tp.X;
                ret.Y = (int)tp.Y;
            }

            if (IsRotated)
            {
                var f = rotationMatrix.Transform(new System.Windows.Point(ret.X, ret.Y));

                ret.X = (int)f.X;
                ret.Y = (int)f.Y;
            }

            ret.Offset(-(int)MapTranslateTransform.X, -(int)MapTranslateTransform.Y);

            return ret;
        }

        public bool ShowExportDialog()
        {
#if SQLite
            if (Cache.Instance.ImageCache is SQLitePureImageCache)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                {
                    dlg.CheckPathExists = true;
                    dlg.CheckFileExists = false;
                    dlg.AddExtension = true;
                    dlg.DefaultExt = "gmdb";
                    dlg.ValidateNames = true;
                    dlg.FileName = "DataExp";
                    dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    dlg.Filter = "Map DB files (*.gmdb)|*.gmdb";
                    dlg.FilterIndex = 1;
                    dlg.RestoreDirectory = true;

                    if (dlg.ShowDialog() == true)
                    {
                        bool ok = MapsManager.Instance.ExportToGMDB(dlg.FileName);
                        if (ok)
                        {
                            MessageBox.Show("导出成功!", "Map", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("导出失败!", "Map", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        return ok;
                    }
                }
            }
            else
            {
                MessageBox.Show("失败!仅支持SQLite!", "Map", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
#endif
            return false;
        }

        public bool ShowImportDialog()
        {
#if SQLite
            if (Cache.Instance.ImageCache is SQLitePureImageCache)
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                {
                    dlg.CheckPathExists = true;
                    dlg.CheckFileExists = false;
                    dlg.AddExtension = true;
                    dlg.DefaultExt = "gmdb";
                    dlg.ValidateNames = true;
                    dlg.FileName = "DataImport";
                    dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    dlg.Filter = "Map DB files (*.gmdb)|*.gmdb";
                    dlg.FilterIndex = 1;
                    dlg.RestoreDirectory = true;

                    if (dlg.ShowDialog() == true)
                    {
                        Cursor = Cursors.Wait;

                        bool ok = MapsManager.Instance.ImportFromGMDB(dlg.FileName);
                        if (ok)
                        {
                            MessageBox.Show("导入成功!", "Map", MessageBoxButton.OK, MessageBoxImage.Information);
                            ReloadMap();
                        }
                        else
                        {
                            MessageBox.Show("导入失败!", "Map", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        Cursor = Cursors.Arrow;

                        return ok;
                    }
                }
            }
            else
            {
                MessageBox.Show("失败!仅支持SQLite!", "Map", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
#endif
            return false;
        }

        //[Browsable(false)]
        //public PointLatLng Position
        //{
        //    get
        //    {
        //        return Core.CurrentPosition;
        //    }
        //    set
        //    {
        //        Core.CurrentPosition = value;
        //        UpdateMarkersOffset();
        //    }
        //}



        public PointLatLng Position
        {
            get { return (PointLatLng)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Position.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.Register("Position", typeof(PointLatLng), typeof(MapControl), new UIPropertyMetadata(new PropertyChangedCallback(OnPositionPropertyChanged)));

        private static void OnPositionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MapControl mapControl = sender as MapControl;
            if (mapControl != null && e.NewValue != null && e.NewValue != e.OldValue)
            {
                mapControl.core.currentPosition = (PointLatLng)e.NewValue;
                mapControl.UpdateMarkersOffset();
            }
        }



        public MapType MapTileType
        {
            get { return (MapType)GetValue(MapTileTypeProperty); }
            set { SetValue(MapTileTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MapTileTypeProperty =
            DependencyProperty.Register("MapTileType", typeof(MapType), typeof(MapControl), new UIPropertyMetadata(MapType.None, new PropertyChangedCallback(OnMapTileTypePropertyChanged)));

        private static void OnMapTileTypePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            MapControl map = (MapControl)sender;
            if (map != null)
            {
                Debug.WriteLine("MapType: " + e.OldValue + " -> " + e.NewValue);

                RectLatLng viewarea = map.SelectedArea;
                if (viewarea != RectLatLng.Empty)
                {
                    map.Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat / 2, viewarea.Lng + viewarea.WidthLng / 2);
                }
                else
                {
                    viewarea = map.CurrentViewArea;
                }

                map.core.MapType = (MapType)e.NewValue;

                if (map.core.IsStarted && map.core.zoomToArea)
                {
                    if (viewarea != RectLatLng.Empty && viewarea != map.CurrentViewArea)
                    {
                        int bestZoom = map.core.GetMaxZoomToFitRect(viewarea);
                        if (bestZoom > 0 && map.Zoom != bestZoom)
                        {
                            map.Zoom = bestZoom;
                        }
                    }
                }
            }
        }

        [Browsable(false)]
        public MapPoint CurrentPositionGPixel
        {
            get
            {
                return core.CurrentPositionGPixel;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public string CacheLocation
        {
            get
            {
                return Cache.Instance.CacheLocation;
            }
            set
            {
                Cache.Instance.CacheLocation = value;
            }
        }

        bool isDragging = false;

        //[Browsable(false)]
        public bool IsDragging
        {
            get
            {
                return core.IsDragging;
            }
            set
            {
                core.IsDragging = value;
            }
        }

        [Browsable(false)]
        public RectLatLng CurrentViewArea
        {
            get
            {
                return core.CurrentViewArea;
            }
        }

        [Browsable(false)]
        public PureProjection Projection
        {
            get
            {
                return core.Projection;
            }
        }

        [Category("Map")]
        public bool CanDragMap
        {
            get
            {
                return core.CanDragMap;
            }
            set
            {
                core.CanDragMap = value;
            }
        }

        public RenderMode RenderMode
        {
            get
            {
                return RenderMode.WPF;
            }
        }

        #endregion

        #region IGControl event Members

        public event CurrentPositionChanged OnCurrentPositionChanged
        {
            add
            {
                core.OnCurrentPositionChanged += value;
            }
            remove
            {
                core.OnCurrentPositionChanged -= value;
            }
        }

        public event TileLoadComplete OnTileLoadComplete
        {
            add
            {
                core.OnTileLoadComplete += value;
            }
            remove
            {
                core.OnTileLoadComplete -= value;
            }
        }

        public event TileLoadStart OnTileLoadStart
        {
            add
            {
                core.OnTileLoadStart += value;
            }
            remove
            {
                core.OnTileLoadStart -= value;
            }
        }

        public event MapDrag OnMapDrag
        {
            add
            {
                core.OnMapDrag += value;
            }
            remove
            {
                core.OnMapDrag -= value;
            }
        }

        public event MapZoomChanged OnMapZoomChanged
        {
            add
            {
                core.OnMapZoomChanged += value;
            }
            remove
            {
                core.OnMapZoomChanged -= value;
            }
        }

        public event MapTypeChanged OnMapTypeChanged
        {
            add
            {
                core.OnMapTypeChanged += value;
            }
            remove
            {
                core.OnMapTypeChanged -= value;
            }
        }

        public event EmptyTileError OnEmptyTileError
        {
            add
            {
                core.OnEmptyTileError += value;
            }
            remove
            {
                core.OnEmptyTileError -= value;
            }
        }
        #endregion
    }
}
