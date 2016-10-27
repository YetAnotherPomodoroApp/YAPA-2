using System.Windows.Controls;

namespace YAPA.WPF.Themes.YAPA
{
    /// <summary>
    /// Interaction logic for YapaThemeSettingWindow.xaml
    /// </summary>
    public partial class YapaThemeSettingWindow : UserControl
    {
        public YapaThemeSettingWindow(YapaThemeSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }
}
