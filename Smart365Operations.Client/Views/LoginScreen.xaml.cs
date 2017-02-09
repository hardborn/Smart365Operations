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
using System.Windows.Shapes;
using Smart365Operations.Common.Infrastructure.Interfaces;

namespace Smart365Operations.Client.Views
{
    /// <summary>
    /// LoginScreen.xaml 的交互逻辑
    /// </summary>
    public partial class LoginScreen : Window, IView
    {
        public LoginScreen()
        {
            InitializeComponent();
        }

        public IViewModel ViewModel
        {
            get { return DataContext as IViewModel; }
            set { DataContext = value; }
        }
    }
}
