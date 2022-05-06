using MaterialDesignThemes.Wpf;
using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace YAPA.WPF.Themes.YAPA
{
    public partial class YapaThemeSettingWindow : UserControl
    {
        public YapaThemeSettingWindow(YapaThemeSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }

    public class ThemeColorSelectConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var integer = (int)value;
            return integer == (int)Enum.Parse(typeof(ThemeColors), parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter;
        }
    }
}
