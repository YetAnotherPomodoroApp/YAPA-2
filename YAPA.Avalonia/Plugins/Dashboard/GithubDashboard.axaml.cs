using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.Dashboard
{
    [BuiltInPlugin(Order = 1)]
    public class DashboardPlugin : IPluginMeta
    {
        public string Title => "Dashboard";

        public string Id => "Dashboard";

        public Type Plugin => typeof(Shared.Common.Dashboard);

        public Type Settings => typeof(DashboardSettings);

        public Type SettingEditWindow => typeof(GithubDashboard);
    }

    public partial class GithubDashboard : UserControl, IPluginSettingWindow
    {
        private readonly Shared.Common.Dashboard _dashboard;
        private readonly ISettings _globalSettings;
        private const int MaxNumberOfMonths = 13;
        private bool _updatingLists;

        public DashboardSettings settings { get; }

        public GithubDashboard()
        {
            InitializeComponent();
        }

        public GithubDashboard(Shared.Common.Dashboard dashboard, DashboardSettings settings, ISettings globalSettings)
        {
            _dashboard = dashboard;
            this.settings = settings;
            _globalSettings = globalSettings;
            InitializeComponent();
            DataContext = this;

            NumberOfMonths.ItemsSource = Enumerable.Range(1, MaxNumberOfMonths - 1).ToList();
            NumberOfMonths.SelectedItem = settings.NumberOfMonths;
            NumberOfMonths.SelectionChanged += (s, e) =>
            {
                if (!_updatingLists && NumberOfMonths.SelectedItem is int months && months != settings.NumberOfMonths)
                {
                    settings.NumberOfMonths = months;
                }
            };

            FilteredProfiles.SelectionChanged += (s, e) =>
            {
                if (_updatingLists)
                {
                    return;
                }
                var profile = FilteredProfiles.SelectedItem as string;
                profile = profile == AllProfiles ? null : profile;
                if (profile != settings.ProfileFilter)
                {
                    settings.ProfileFilter = profile;
                }
            };

            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;
        }

        // WPF has a clear button on the profile box; here the first entry clears the filter
        private const string AllProfiles = "(all)";

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.StartsWith($"{nameof(Shared.Common.Dashboard)}.{nameof(DashboardSettings.NumberOfMonths)}"))
            {
                _updatingLists = true;
                FilteredProfiles.ItemsSource = null;
                _updatingLists = false;
                settings.ProfileFilter = null;
                Refresh();
            }

            if (e.PropertyName.StartsWith($"{nameof(Shared.Common.Dashboard)}.{nameof(DashboardSettings.ProfileFilter)}"))
            {
                Refresh();
            }
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
            if (level < 0.25)
            {
                return PomodoroLevelEnum.Level1;
            }
            if (level < 0.50)
            {
                return PomodoroLevelEnum.Level2;
            }
            if (level < 0.75)
            {
                return PomodoroLevelEnum.Level3;
            }
            return PomodoroLevelEnum.Level4;
        }

        public void Refresh()
        {
            Refresh(settings.NumberOfMonths, settings.ProfileFilter);
        }

        public void Refresh(int numberOfMonths, string profileFilter)
        {
            var pomodoros = GetPomodoros(numberOfMonths, profileFilter);
            if (pomodoros?.Any() != true)
            {
                return;
            }

            UpdateCompletedSummary(pomodoros);
            UpdateGithubDashboard(pomodoros);
            UpdateCartesianChart(pomodoros);
        }

        private void UpdateGithubDashboard(List<PomodorosPerTimeModel> pomodoros)
        {
            var max = pomodoros.Max(_ => _.Pomodoro.Count);
            WeekStackPanel.Children.Clear();

            foreach (var pomodoro in pomodoros.GroupBy(_ => new { _.Month, _.Year }))
            {
                var month = pomodoro.Select(_ => _.Pomodoro.ToPomodoroViewModel(_.Week, GetLevelFromCount(_.Pomodoro.Count, max))).ToList();
                WeekStackPanel.Children.Add(new PomodoroMonth(month));
            }

            var weekShift = DayOfWeek.Monday - CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            MondayTextBlock.Margin = new Thickness(0, 12 * weekShift - 1, 0, 12);
            DayPanel.IsVisible = true;
            LoadingPanel.IsVisible = false;
        }

        private void UpdateCompletedSummary(List<PomodorosPerTimeModel> pomodoros)
        {
            var count = pomodoros.Sum(_ => _.Pomodoro.Count);
            var totalTime = pomodoros.Sum(_ => _.Pomodoro.DurationMin);
            if (count > 0)
            {
                var multiple = count > 1 ? "s" : "";
                Summary.Text = $"Total time: {TimeSpan.FromMinutes(totalTime)}, Pomodoro{multiple}: {count}";
            }
            else
            {
                Summary.Text = string.Empty;
            }
        }

        private List<PomodorosPerTimeModel> GetPomodoros(int numberOfMonths, string profileFilter)
        {
            var dfi = DateTimeFormatInfo.CurrentInfo;
            var cal = dfi?.Calendar;
            if (cal == null)
            {
                return new List<PomodorosPerTimeModel>();
            }

            var allPomodoros = _dashboard.GetPomodoros(numberOfMonths, profileFilter);
            if (FilteredProfiles.ItemsSource == null)
            {
                _updatingLists = true;
                var profiles = new List<string> { AllProfiles };
                profiles.AddRange(allPomodoros.Profiles.Where(x => !string.IsNullOrEmpty(x)));
                FilteredProfiles.ItemsSource = profiles;
                FilteredProfiles.SelectedItem = string.IsNullOrEmpty(profileFilter) ? null : profileFilter;
                _updatingLists = false;
            }

            return allPomodoros.DashboardItems.ToList()
                .Select(_ => new PomodorosPerTimeModel
                {
                    Week = cal.GetWeekOfYear(_.DateTime, CalendarWeekRule.FirstFullWeek, dfi.FirstDayOfWeek),
                    Month = cal.GetMonth(_.DateTime),
                    Year = cal.GetYear(_.DateTime),
                    Pomodoro = _,
                })
                .ToList();
        }

        private void UpdateCartesianChart(List<PomodorosPerTimeModel> pomodoros)
        {
            const int daysToShow = 60;
            var lastPomodoros = pomodoros.Skip(pomodoros.Count - daysToShow).ToList();

            if (lastPomodoros.Any(_ => _.Pomodoro.DurationMin / 60.0 > 0.01))
            {
                CartesianContainer.IsVisible = true;
                CartesianChartTitle.Text = $"Last {daysToShow} days";
                CartesianChart.LabelFormatter = x => TimeSpan.FromMinutes(x).ToString("g", CultureInfo.CurrentCulture);
                CartesianChart.SetData(
                    lastPomodoros.Select(_ => (double)_.Pomodoro.DurationMin).ToList(),
                    lastPomodoros.Select(_ => _.Pomodoro.DateTime.ToShortDateString()).ToList());
            }
            else
            {
                CartesianContainer.IsVisible = false;
            }
        }
    }

    public class PomodorosPerTimeModel
    {
        public PomodoroGithubDashboardModel Pomodoro { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
    }

    public enum PomodoroLevelEnum
    {
        Level0 = 0,
        Level1 = 1,
        Level2 = 2,
        Level3 = 3,
        Level4 = 4,
    }

    public class PomodoroViewModel
    {
        public DateTime DateTime { get; set; }
        public int Count { get; set; }
        public PomodoroLevelEnum Level { get; set; }
        public int Week { get; set; }
    }

    public static class PomodoroExtensions
    {
        public static PomodoroViewModel ToPomodoroViewModel(this PomodoroGithubDashboardModel pomo, int week, PomodoroLevelEnum level = PomodoroLevelEnum.Level0)
        {
            return new PomodoroViewModel
            {
                Count = pomo.Count,
                DateTime = pomo.DateTime.TryToLocalTime(),
                Week = week,
                Level = level
            };
        }
    }
}
