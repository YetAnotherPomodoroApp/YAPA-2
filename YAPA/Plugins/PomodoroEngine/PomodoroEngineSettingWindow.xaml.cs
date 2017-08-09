using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class PomodoroEngineSettingWindow : UserControl
    {

        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings)
        {
            settings.DeferChanges();
            InitializeComponent();

            var oneHour = Enumerable.Range(1, 60).ToList();
            WorkTimeSelect.ItemsSource = oneHour;
            BreakTimeSelect.ItemsSource = oneHour;
            LongBreakTimeSelect.ItemsSource = oneHour;

            var counterValues = new List<CounterListItem>
            {
                new CounterListItem{ Item = CounterEnum.CompletedThisSession, Title = "Completed this session"},
                new CounterListItem{ Item = CounterEnum.CompletedToday, Title = "Competed today"},
                new CounterListItem{ Item = CounterEnum.PomodoroIndex, Title = "Pomodoro index"},
            };

            CounterList.ItemsSource = counterValues;
            CounterList.SelectionChanged += CounterList_SelectionChanged;

            DataContext = settings;
        }

        private void CounterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }

    public class CounterListItem
    {
        public CounterEnum Item { get; set; }
        public string Title { get; set; }
    }

    public class SecondsToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var seconds = (int)(value ?? 60);
            return seconds / 60;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int seconds;

            if (!int.TryParse(value?.ToString(), out seconds))
            {
                seconds = 1;
            }

            return seconds * 60;
        }
    }
}
