using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;
using YAPA.WPF;

namespace YAPA.Plugins.Dashboard
{
    public partial class GithubDashboard
    {
        private readonly Shared.Common.Dashboard _dashboard;

        public GithubDashboard(Shared.Common.Dashboard dashboard)
        {
            _dashboard = dashboard;

            Loaded += Dashboard_Loaded;

            InitializeComponent();

            Chart.DataHover += Chart_DataHover;
        }

        private void Chart_DataHover(object sender, ChartPoint chartPoint)
        {
            //TODO: highlight grid dashboard cell
        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
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
               var max = pomodoros.Max(x => x.x.Count);

               var seriesCollection = new SeriesCollection();

               foreach (var pomodoro in pomodoros.GroupBy(x => x.month))
               {
                   var month = pomodoro.Select(x => x.x.ToPomodoroViewModel(x.week, GetLevelFromCount(x.x.Count, max)));
                   Dispatcher.Invoke(() =>
                   {
                       WeekStackPanel.Children.Add(new PomodoroMonth(month));
                   });
               }
               Dispatcher.Invoke(() =>
               {
                   var daysToShow = 60;

                   var lastPomodoros = pomodoros.Skip(pomodoros.Count - daysToShow).ToList();

                   if (lastPomodoros.Any(x => x.x.DurationMin / 60.0 > 0.01))
                   {
                       seriesCollection.Add(
                           new LineSeries
                           {
                               Title = "Hours",
                               Values = new ChartValues<double>(lastPomodoros.Select(x => Math.Round((double)x.x.DurationMin / 60, 2)))
                           });

                       Chart.Series = seriesCollection;
                       ChartLabels.Labels = Enumerable.Range(1, daysToShow).Reverse().Select(x => DateTime.Now.AddDays(-x + 1).ToShortDateString()).ToArray();
                   }
                   else
                   {
                       Chart.Visibility = Visibility.Collapsed;
                   }

               });

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
    }


}
