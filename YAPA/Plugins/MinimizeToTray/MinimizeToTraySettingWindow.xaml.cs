using System.Linq;
using System.Windows.Controls;

namespace YAPA.Plugins.MinimizeToTray
{
    public partial class MinimizeToTraySettingWindow : UserControl
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



