using System.Windows.Controls;
using YAPA.Shared;

namespace YAPA.WPF
{
    /// <summary>
    /// Interaction logic for PomodoroEngineSettingWindow.xaml
    /// </summary>
    public partial class PomodoroEngineSettingWindow : UserControl
    {
        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings)
        {
            settings.DefereChanges();
            InitializeComponent();
            DataContext = settings;
        }
    }
}
