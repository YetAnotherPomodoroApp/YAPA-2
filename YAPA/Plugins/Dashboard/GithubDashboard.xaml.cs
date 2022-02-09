using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.Dashboard
{
    public partial class GithubDashboard : IPluginSettingWindow
    {
        private Shared.Common.Dashboard _dashboard;

        public GithubDashboard(Shared.Common.Dashboard dashboard)
        {
            _dashboard = dashboard;

            InitializeComponent();

            CartesianChart.DataHover += Chart_DataHover;
        }

        ~GithubDashboard()
        {
            _dashboard = null;
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            //TODO: highlight grid dashboard cell
        }

        private static PomodoroLevelEnum GetLevelFromCount(int count, int maxCount)
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

            var displayLevel = PomodoroLevelEnum.Level4;

            if (level < 0.25)
            {
                displayLevel = PomodoroLevelEnum.Level1;
            }
            else if (level < 0.50)
            {
                displayLevel = PomodoroLevelEnum.Level2;
            }
            else if (level < 0.75)
            {
                displayLevel = PomodoroLevelEnum.Level3;
            }

            return displayLevel;
        }

        public void Refresh()
        {
            Task.Run(() =>
            {
                var dfi = DateTimeFormatInfo.CurrentInfo;
                var cal = dfi?.Calendar;

                if (cal == null)
                {
                    return;
                }

                var allPomodoros = _dashboard.GetPomodoros().ToList();
                var pomodoros = allPomodoros
                       .Select(
                           x =>
                               new
                               {
                                   week = cal.GetWeekOfYear(x.DateTime, CalendarWeekRule.FirstFullWeek, dfi.FirstDayOfWeek),
                                   month = cal.GetMonth(x.DateTime),
                                   x
                               })
                       .ToList();

                var count = allPomodoros.Sum(_ => _.Count);
                var totalTime = allPomodoros.Sum(_ => _.DurationMin);
                Dispatcher.Invoke(() =>
                {
                    if (count > 0)
                    {
                        var multiple = count > 1 ? "s" : "";
                        Summary.Text = $"{count} Pomodoro{multiple}, {TimeSpan.FromMinutes(totalTime)} total time";
                    }
                });

                var max = pomodoros.Max(x => x.x.Count);

                var seriesCollection = new SeriesCollection();

                Dispatcher.Invoke(() =>
                {
                    WeekStackPanel.Children.Clear();
                });

                foreach (var pomodoro in pomodoros.GroupBy(x => x.month))
                {
                    var month = pomodoro.Select(x => x.x.ToPomodoroViewModel(x.week, GetLevelFromCount(x.x.Count, max)));
                    Dispatcher.Invoke(() =>
                    {
                        WeekStackPanel.Children.Add(new PomodoroMonth(month));
                    });
                }

                const int daysToShow = 60;

                var lastPomodoros = pomodoros.Skip(pomodoros.Count - daysToShow).ToList();

                if (lastPomodoros.Any(x => x.x.DurationMin / 60.0 > 0.01))
                {
                    Dispatcher.Invoke(() =>
                    {
                        seriesCollection.Add(
                                new LineSeries
                                {
                                    Title = "Total time",
                                    Values = new ChartValues<double>(lastPomodoros.Select(x => (double)x.x.DurationMin))
                                });

                        CartesianChart.Series = seriesCollection;
                        CartesianChartTitle.Text = $"Last {daysToShow} days";
                        AxisYLabels.LabelFormatter = x => TimeSpan.FromMinutes(x).ToString("g");
                        ChartLabels.Labels = Enumerable
                            .Range(1, daysToShow)
                            .Reverse()
                            .Select(x => DateTime.Now.AddDays(-x + 1).ToShortDateString())
                            .ToArray();
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        CartesianContainer.Visibility = Visibility.Collapsed;
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
    }
}
