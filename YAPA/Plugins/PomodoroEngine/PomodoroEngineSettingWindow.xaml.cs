using System.Linq;
using System.Windows.Controls;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class PomodoroEngineSettingWindow : UserControl
    {
        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();

            //HACK!
            Enumerable.Range(1,60).Select(x =>
            {
                WorkTimeSelect.Items.Add(x);
                BreakTimeSelect.Items.Add(x);
                LongBreakTimeSelect.Items.Add(x);

                return true;
            }).ToList();

            DataContext = settings;
        }
    }
}
