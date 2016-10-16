using System.Windows.Controls;
using YAPA.Shared;

namespace YAPA.WPF
{
    /// <summary>
    /// Interaction logic for SoundNotificationSettingWindow.xaml
    /// </summary>
    public partial class SoundNotificationSettingWindow : UserControl
    {
        public SoundNotificationSettingWindow(SoundNotificationsSettings settings)
        {
            DataContext = settings;
            InitializeComponent();
        }
    }
}
