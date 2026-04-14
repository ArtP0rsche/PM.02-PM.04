using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DiskSpaceMonitor
{
    public class FreeSpaceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double freePercent)
            {
                if (freePercent < 10)
                    return Brushes.Red;
                else if (freePercent <= 25)
                    return Brushes.Goldenrod;
                else
                    return Brushes.Lime;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
