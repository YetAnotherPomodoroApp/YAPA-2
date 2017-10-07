using YAPA.Plugins.SoundSettings.MusicPlayer;
using YAPA.Plugins.SoundSettings.SoundNotifications;
using YAPA.Shared.Common;

namespace YAPA.Plugins.SoundSettings
{
    public partial class SoundSettingWindow
    {
        public SoundSettingWindow(SoundNotificationSettingWindow soundNotification, MusicPlayerPluginSettingWindow musicPlayer, PomodoroEngineSettings engineSettings)
        {
            InitializeComponent();
            Container.Children.Add(soundNotification);
            Container.Children.Add(musicPlayer);

            engineSettings.DeferChanges();

            DataContext = engineSettings;
        }
    }
}
