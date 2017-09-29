namespace YAPA.Plugins.SoundSettings.SoundNotifications
{
    public partial class SoundNotificationSettingWindow
    {
        public SoundNotificationSettingWindow(SoundNotificationsSettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();

            DataContext = settings;
        }
    }
}
