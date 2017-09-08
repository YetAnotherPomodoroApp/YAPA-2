using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

               var pomodoros =
                   _dashboard.GetPomodoros()
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
