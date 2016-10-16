using System.Windows.Controls;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class SoundNotificationSettingWindow : UserControl
    {
        public SoundNotificationSettingWindow(SoundNotificationsSettings settings)
        {
            InitializeComponent();

            DisableSoundNotifications.IsChecked = settings.DisabelSoundNotifications;

            DataContext = settings;
        }
    }
}
