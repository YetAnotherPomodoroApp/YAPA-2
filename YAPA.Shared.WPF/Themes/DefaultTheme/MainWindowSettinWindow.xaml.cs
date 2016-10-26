using System.Windows.Controls;

namespace YAPA.WPF.Themes.DefaultTheme
{
    public partial class MainWindowSettinWindow : UserControl
    {
        public MainWindowSettinWindow(DefaultThemeSettings settings)
        {
            DataContext = settings;
            InitializeComponent();
        }
    }
}
