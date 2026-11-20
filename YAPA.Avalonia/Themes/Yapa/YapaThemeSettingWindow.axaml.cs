using Avalonia.Controls;

namespace YAPA.Avalonia.Themes.Yapa
{
    public partial class YapaThemeSettingWindow : UserControl
    {
        public YapaThemeSettingWindow()
        {
            InitializeComponent();
        }

        public YapaThemeSettingWindow(YapaThemeSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }
}
