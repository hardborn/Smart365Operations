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
using Microsoft.Practices.ServiceLocation;
using Smart365Operation.Modules.VideoMonitoring.ViewModels;
using Smart365Operations.Common.Infrastructure.Interfaces;

namespace Smart365Operation.Modules.VideoMonitoring
{
    /// <summary>
    /// VideoMonitoringView.xaml 的交互逻辑
    /// </summary>
    public partial class VideoMonitoringView : UserControl
    {
        public VideoMonitoringView(VideoMonitoringViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}
