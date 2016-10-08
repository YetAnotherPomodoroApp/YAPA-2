using System;
using System.Globalization;
using System.Windows.Data;

namespace YAPA.Shared
{
    public class SecToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var seconds = value as int?;
            if (seconds == null)
            {
                return 0;
            }

            var min = seconds / 60;
            var sec = seconds % 60;
            return $"{min:00}:{sec:00}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
