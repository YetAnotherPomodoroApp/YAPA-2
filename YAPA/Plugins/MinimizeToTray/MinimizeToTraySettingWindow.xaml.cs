using System.Linq;

namespace YAPA.Plugins.MinimizeToTray
{
    public partial class MinimizeToTraySettingWindow
    {
        public MinimizeToTraySettingWindow(SystemTraySettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();
            DataContext = settings;

            BalloonTipSelect.ItemsSource = Enumerable.Range(1, 60);
        }
    }
}



