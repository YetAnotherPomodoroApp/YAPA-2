using System.Windows.Controls;

namespace YAPA.WPF
{
    public partial class MinimizeToTraySettingWindow : UserControl
    {
        public MinimizeToTraySettingWindow(SystemTraySettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();
            DataContext = settings;
        }
    }
}
