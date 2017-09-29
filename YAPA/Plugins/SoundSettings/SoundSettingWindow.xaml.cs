using YAPA.Plugins.SoundSettings.SoundNotifications;

namespace YAPA.Plugins.SoundSettings
{
    public partial class SoundSettingWindow
    {
        public SoundSettingWindow(SoundNotificationSettingWindow soundNotification, MusicPlayer.MusicPlayerPluginSettingWindow musicPlayer)
        {
            InitializeComponent();
            Container.Children.Add(soundNotification);
            Container.Children.Add(musicPlayer);
        }
    }
}
