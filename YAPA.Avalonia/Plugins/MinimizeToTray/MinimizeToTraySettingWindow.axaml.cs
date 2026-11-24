using System.Linq;
using Avalonia.Controls;

namespace YAPA.Avalonia.Plugins.MinimizeToTray
{
    public partial class MinimizeToTraySettingWindow : UserControl
    {
        public MinimizeToTraySettingWindow()
        {
            InitializeComponent();
        }

        public MinimizeToTraySettingWindow(SystemTraySettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();
            // The list first: an editable combo box clears its text when the items change
            BalloonTipSelect.ItemsSource = Enumerable.Range(1, 60).Select(x => x.ToString()).ToList();
            DataContext = settings;
        }
    }
}
