using System.Windows.Controls;

namespace YAPA.WPF
{
    public partial class MinimizeToTraySettingWindow : UserControl
    {
        public MinimizeToTraySettingWindow(MinimizeToTraySettings settings)
        {
            settings.DefereChanges();
            InitializeComponent();
            DataContext = settings;
        }
    }
}
