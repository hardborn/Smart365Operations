using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Smart365Operation.Modules.VideoMonitoring.Models;
using Smart365Operation.Modules.VideoMonitoring.Services;
using Smart365Operation.Modules.VideoMonitoring.Utility;

namespace Smart365Operation.Modules.VideoMonitoring.ViewModels
{
    public class VideoSurveillanceViewModel : BindableBase, IVideoService
    {

        public VideoSurveillanceViewModel()
        {
            MakeRegions(Rows, Columns);
        }

        private void MakeRegions(int row, int columns)
        {
            int index = 0;
            Regions.Clear();
            for (int rowIndex = 0; rowIndex < row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < columns; columnIndex++)
                {
                    Regions.Add(new RegionInfo() { ColumnIndex = columnIndex, RowIndex = rowIndex, Index = index++ });
                }
            }
        }

        private int _rows = 1;
        public int Rows
        {
            get { return _rows; }
            set { SetProperty(ref _rows, value); }
        }

        private int _columns = 1;
        public int Columns
        {
            get { return _columns; }
            set { SetProperty(ref _columns, value); }
        }

        private ObservableCollection<RegionInfo> _regions = new ObservableCollection<RegionInfo>();
        public ObservableCollection<RegionInfo> Regions
        {
            get { return _regions; }
            set { SetProperty(ref _regions, value); }
        }

        private DisplayMode _selectedDisplayMode = DisplayMode.One;
        public DisplayMode SelectedDisplayMode
        {
            get { return _selectedDisplayMode; }
            set
            {
                if (value != _selectedDisplayMode)
                {
                    UpdateDisplayRegions(value);
                }
                SetProperty(ref _selectedDisplayMode, value);
            }
        }

        private int _selectedIndex = 0;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set { SetProperty(ref _selectedIndex, value); }
        }

        private void UpdateDisplayRegions(DisplayMode displayMode)
        {
            switch (displayMode)
            {
                case DisplayMode.One:
                    Rows = 1;
                    Columns = 1;
                    break;
                case DisplayMode.Two:
                    Rows = 2;
                    Columns = 1;
                    break;
                case DisplayMode.Four:
                    Rows = 2;
                    Columns = 2;
                    break;
                case DisplayMode.Six:
                    Rows = 2;
                    Columns = 3;
                    break;
                case DisplayMode.Nine:
                    Rows = 3;
                    Columns = 3;
                    break;
                default:
                    break;
            }
            MakeRegions(Rows, Columns);
        }


        #region VideoPlay Impl
        public void Play(string cameraId)
        {
            var region = GetCurrentDisplayRegion();
            if (region.IsDisplaying && region.SessionId != IntPtr.Zero)
            {
                HkAction.Stop(region.SessionId);
            }
            region.SessionId = HkAction.AllocSession();
            if (region.SessionId != null && !string.IsNullOrEmpty(cameraId))
            {
                var playStatus = HkAction.Play(region.DisplayHandler, cameraId, region.SessionId);
                if (playStatus)
                {
                    region.IsDisplaying = true;
                    SelectedIndex = SelectedIndex >= Regions.Count - 1 ? 0 : (SelectedIndex + 1);
                }
            }
        }

        private RegionInfo GetCurrentDisplayRegion()
        {
            return Regions.FirstOrDefault(r => r.Index == SelectedIndex);
        }

        #endregion
    }
}
