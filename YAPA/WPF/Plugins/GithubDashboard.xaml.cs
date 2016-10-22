using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace YAPA.WPF.Plugins
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class GithubDashboard : UserControl
    {
        private readonly Dashboard _dashboard;

        public GithubDashboard(Dashboard dashboard)
        {
            _dashboard = dashboard;

            this.Loaded += Dashboard_Loaded;

            InitializeComponent();
        }

        private void Dashboard_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi.Calendar;


            var pomodoros =
                _dashboard.GetPomodoros()
                    .Select(x => new { week = cal.GetWeekOfYear(x.DateTime, CalendarWeekRule.FirstFullWeek, dfi.FirstDayOfWeek), x })
                    .ToList();
            var max = pomodoros.Max(x => x.x.Count);

            foreach (var pomodoro in pomodoros.GroupBy(x => x.week))
            {
                var week = pomodoro.Select(x => x.x.ToPomodoroViewModel(GetLevelFromCount(x.x.Count, max)));
                Dispatcher.Invoke(() =>
                {
                    WeekStackPanel.Children.Add(new PomodoroWeek(week));
                });
            }
        }

        private PomodoroLevelEnum GetLevelFromCount(int count, int maxCount)
        {
            if (count == 0)
            {
                return PomodoroLevelEnum.Level0;
            }
            if (maxCount <= 4)
            {
                return PomodoroLevelEnum.Level4;
            }
            var level = (double)count / maxCount;
            if (level < 0.25)
            {
                return PomodoroLevelEnum.Level1;
            }
            else if (level < 0.50)
            {
                return PomodoroLevelEnum.Level2;
            }
            else if (level < 0.75)
            {
                return PomodoroLevelEnum.Level3;
            }

            return PomodoroLevelEnum.Level4;
        }
    }


}
