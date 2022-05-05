using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.Dashboard
{
    public partial class GithubDashboard : IPluginSettingWindow
    {
        private Shared.Common.Dashboard _dashboard;
        private readonly ISettings _globalSettings;

        public DashboardSettings settings { get; }

        public GithubDashboard(Shared.Common.Dashboard dashboard, DashboardSettings settings, ISettings globalSettings)
        {
            _dashboard = dashboard;
            this.settings = settings;
            _globalSettings = globalSettings;
            InitializeComponent();

            DataContext = this;

            CartesianChart.DataHover += Chart_DataHover;

            for (int i = 1; i < 13; i++)
            {
                NumberOfMonths.Items.Add(i);
            }

            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(Shared.Common.Dashboard)}.{nameof(DashboardSettings.NumberOfMonths)}")
            {
                Refresh(settings.NumberOfMonths);
            }
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
            Refresh(settings.NumberOfMonths);
        }

        public void Refresh(int numberOfMonths)
        {
            Task.Run(() =>
            {
                var pomodoros = GetPomodoros(numberOfMonths);
                if (pomodoros?.Any() == false)
                {
                    return;
                }

                UpdateCompletedSummary(pomodoros);

                UpdateGithubDashboard(pomodoros);

                UpdateCartesianChart(pomodoros);
            });
        }

        private void UpdateGithubDashboard(IEnumerable<PomodorosPerTimeModel> pomodoros)
        {
            var max = pomodoros.Max(_ => _.Pomodoro.Count);

            Dispatcher.Invoke(() =>
            {
                WeekStackPanel.Children.Clear();
            });

            foreach (var pomodoro in pomodoros.GroupBy(_ => new { _.Month, _.Year }))
            {
                var month = pomodoro.Select(_ => _.Pomodoro.ToPomodoroViewModel(_.Week, GetLevelFromCount(_.Pomodoro.Count, max)));
                Dispatcher.Invoke(() =>
                {
                    WeekStackPanel.Children.Add(new PomodoroMonth(month));
                });
            }
            Dispatcher.Invoke(() =>
            {
                var weekShift = DayOfWeek.Monday - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                MondayTextBlock.Margin = new Thickness(0, 12 * weekShift - 1, 0, 12);

                DayPanel.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Collapsed;
            });
        }

        private void UpdateCompletedSummary(IEnumerable<PomodorosPerTimeModel> pomodoros)
        {
            var count = pomodoros.Sum(_ => _.Pomodoro.Count);
            var totalTime = pomodoros.Sum(_ => _.Pomodoro.DurationMin);
            Dispatcher.Invoke(() =>
            {
                if (count > 0)
                {
                    var multiple = count > 1 ? "s" : "";
                    Summary.Text = $"Total time: {TimeSpan.FromMinutes(totalTime)}, Pomodoro{multiple}: {count}";
                }
            });
        }

        private IEnumerable<PomodorosPerTimeModel> GetPomodoros(int numberOfMonths)
        {
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi?.Calendar;

            if (cal == null)
            {
                return Enumerable.Empty<PomodorosPerTimeModel>();
            }

            var allPomodoros = _dashboard.GetPomodoros(numberOfMonths).ToList();
            var pomodoros = allPomodoros
                   .Select(
                       _ =>
                           new PomodorosPerTimeModel
                           {
                               Week = cal.GetWeekOfYear(_.DateTime, CalendarWeekRule.FirstFullWeek, dfi.FirstDayOfWeek),
                               Month = cal.GetMonth(_.DateTime),
                               Year = cal.GetYear(_.DateTime),
                               Pomodoro = _,
                           })
                   .ToList();

            return pomodoros;
        }

        private void UpdateCartesianChart(IEnumerable<PomodorosPerTimeModel> pomodoros)
        {
            const int daysToShow = 60;
            var seriesCollection = new SeriesCollection();

            var lastPomodoros = pomodoros.Skip(pomodoros.Count() - daysToShow).ToList();

            if (lastPomodoros.Any(_ => _.Pomodoro.DurationMin / 60.0 > 0.01))
            {
                Dispatcher.Invoke(() =>
                {
                    seriesCollection.Add(
                            new LineSeries
                            {
                                Title = "Total time",
                                Values = new ChartValues<double>(lastPomodoros.Select(_ => (double)_.Pomodoro.DurationMin))
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
        }
    }
}
