using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;
using Smart365Operation.Modules.VideoMonitoring.Models;

namespace Smart365Operation.Modules.VideoMonitoring.Converters
{
    public class DisplayModeToPackIconConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DisplayMode mode = (DisplayMode) value;
            PackIconKind kind = PackIconKind.Numeric1BoxMultipleOutline;
            switch (mode)
            {
                case DisplayMode.One:
                    kind = PackIconKind.Numeric1BoxMultipleOutline;
                    break;
                case DisplayMode.Two:
                    kind = PackIconKind.Numeric2BoxMultipleOutline;
                    break;
                case DisplayMode.Four:
                    kind = PackIconKind.Numeric4BoxMultipleOutline;
                    break;
                case DisplayMode.Six:
                    kind = PackIconKind.Numeric6BoxMultipleOutline;
                    break;
                case DisplayMode.Nine:
                    kind = PackIconKind.Numeric9BoxMultipleOutline;
                    break;
                default:
                    break;
            }
            return kind;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
