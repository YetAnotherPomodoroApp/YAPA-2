using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using System.Linq;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class PomodoroEngineSettingWindow
    {
        private readonly ISettingManager _settingManager;
        private readonly IFontService _fontService;

        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings, IPomodoroEngine engine, PomodoroProfileSettings profileSettings, ISettings globalSettings, ISettingManager settingManager, IFontService fontService)
        {
            settings.DeferChanges();
            InitializeComponent();

            _settingManager = settingManager;
            _fontService = fontService;
            var counterValues = new List<CounterListItem>
            {
                new CounterListItem{ Item = CounterEnum.CompletedThisSession, Title = "Completed this session"},
                new CounterListItem{ Item = CounterEnum.CompletedToday, Title = "Competed today"},
                new CounterListItem{ Item = CounterEnum.PomodoroIndex, Title = "Pomodoro index"},
            };

            CounterList.ItemsSource = counterValues;

            Settings = settings;
            Engine = engine;
            DataContext = this;

            ProfileSetting.Children.Clear();
            ProfileSetting.Children.Add(profileSettings);

            FontSelector.ItemsSource = _fontService.GetAllFonts();
        }

        public PomodoroEngineSettings Settings { get; }
        public IPomodoroEngine Engine { get; }
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
            if (!int.TryParse(value?.ToString(), out var seconds))
            {
                seconds = 1;
            }

            return seconds * 60;
        }
    }

    public class InverseBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && !(bool)value;
        }
    }

    public class StringToFontFamily : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fontName = value?.ToString();
            if (!string.IsNullOrEmpty(fontName))
            {
                var font = new FontFamily(fontName);
                return font;
            }
            return new FontFamily();

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }

}
