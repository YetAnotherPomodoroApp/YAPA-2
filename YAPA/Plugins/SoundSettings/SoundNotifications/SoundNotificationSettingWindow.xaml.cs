using System.Windows.Controls;
using YAPA.Shared;

namespace YAPA.WPF
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
