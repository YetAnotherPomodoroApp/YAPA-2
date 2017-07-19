using System.Windows.Controls;

namespace YAPA.Plugins.SoundSettings.SoundNotifications
{
    public partial class SoundNotificationSettingWindow : UserControl
    {
        public SoundNotificationSettingWindow(SoundNotificationsSettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();

            DataContext = settings;
        }

    }
}
