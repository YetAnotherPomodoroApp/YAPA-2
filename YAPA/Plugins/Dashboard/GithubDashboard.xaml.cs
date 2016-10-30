using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YAPA.Shared;

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

        private async void Dashboard_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await Task.Run(() =>
            {

                var dfi = DateTimeFormatInfo.CurrentInfo;
                var cal = dfi.Calendar;


                var pomodoros =
                    _dashboard.GetPomodoros()
                        .Select(
                            x =>
                                new
                                {
                                    week =
                                        cal.GetWeekOfYear(x.DateTime, CalendarWeekRule.FirstFullWeek, dfi.FirstDayOfWeek),
                                    x
                                })
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



                Dispatcher.Invoke(() =>
                {
                    var weekShift = DayOfWeek.Monday - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                    MondayTextBlock.Margin = new Thickness(0, 12 * weekShift - 1, 0, 12);

                    DayPanel.Visibility = Visibility.Visible;
                    LoadingPanel.Visibility = Visibility.Collapsed;
                });

            });
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
