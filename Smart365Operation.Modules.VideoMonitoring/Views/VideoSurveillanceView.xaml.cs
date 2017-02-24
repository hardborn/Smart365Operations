using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Smart365Operation.Modules.VideoMonitoring.ViewModels;
using UserControl = System.Windows.Controls.UserControl;

namespace Smart365Operation.Modules.VideoMonitoring.Views
{
    /// <summary>
    /// VideoSurveillanceView.xaml 的交互逻辑
    /// </summary>
    public partial class VideoSurveillanceView : UserControl
    {
        public VideoSurveillanceView()
        {
            InitializeComponent();
        }


        private void WindowsFormsHost_Loaded(object sender, RoutedEventArgs e)
        {
            var host = sender as WindowsFormsHost;
            int index = (int)host.Tag;
            var viewModel = this.DataContext as VideoSurveillanceViewModel;
            var region = viewModel.Regions.FirstOrDefault(r => r.Index == index);
            region.DisplayHandler = (host.Child as PictureBox).Handle;
        }
    }
}
