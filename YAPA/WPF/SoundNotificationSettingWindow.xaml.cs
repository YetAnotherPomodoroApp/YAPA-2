using System.Windows.Controls;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class SoundNotificationSettingWindow : UserControl
    {
        public SoundNotificationSettingWindow(SoundNotificationsSettings settings)
        {
            settings.DefereChanges();
            InitializeComponent();

            DataContext = settings;
        }
    }
}
