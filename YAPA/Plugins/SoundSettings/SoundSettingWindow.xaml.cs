using System.Windows.Controls;
using YAPA.WPF;

namespace YAPA.Plugins.SoundSettings
{
    /// <summary>
    /// Interaction logic for SoundSettingWindow.xaml
    /// </summary>
    public partial class SoundSettingWindow : UserControl
    {
        public SoundSettingWindow(SoundNotificationSettingWindow soundNotification, MusicPlayerPluginSettingWindow musicPlayer)
        {
            InitializeComponent();
            Container.Children.Add(soundNotification);
            Container.Children.Add(musicPlayer);
        }
    }
}
