using System.Windows.Controls;

namespace Motivational
{
    public partial class MotivationalThemeSettingsWindow : UserControl
    {
        public MotivationalThemeSettingsWindow(MotivationalThemeSettings settings)
        {
            InitializeComponent();
            DataContext = settings;
        }
    }
}
