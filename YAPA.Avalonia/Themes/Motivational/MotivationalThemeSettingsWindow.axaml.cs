using Avalonia.Controls;

namespace YAPA.Avalonia.Themes.Motivational
{
    public partial class MotivationalThemeSettingsWindow : UserControl
    {
        public MotivationalThemeSettingsWindow()
        {
            InitializeComponent();
        }

        public MotivationalThemeSettingsWindow(MotivationalThemeSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }
}
