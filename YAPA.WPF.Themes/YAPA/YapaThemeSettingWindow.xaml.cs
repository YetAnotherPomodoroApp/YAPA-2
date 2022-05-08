using System.Windows.Controls;

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
}
