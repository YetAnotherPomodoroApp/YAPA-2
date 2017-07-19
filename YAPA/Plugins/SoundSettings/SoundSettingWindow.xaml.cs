using System.Windows.Controls;
using SoundNotificationSettingWindow = YAPA.Plugins.SoundSettings.SoundNotifications.SoundNotificationSettingWindow;

namespace YAPA.Plugins.SoundSettings
{
    /// <summary>
    /// Interaction logic for SoundSettingWindow.xaml
    /// </summary>
    public partial class SoundSettingWindow : UserControl
    {
        public SoundSettingWindow(SoundNotificationSettingWindow soundNotification, MusicPlayer.MusicPlayerPluginSettingWindow musicPlayer)
        {
            InitializeComponent();
            Container.Children.Add(soundNotification);
            Container.Children.Add(musicPlayer);
        }
    }
}
