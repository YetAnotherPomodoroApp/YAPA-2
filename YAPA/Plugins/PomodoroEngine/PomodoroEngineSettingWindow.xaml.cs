using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Plugins.PomodoroEngine
{
    public partial class PomodoroEngineSettingWindow
    {
        private readonly ISettingManager _settingManager;

        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings, IPomodoroEngine engine, PomodoroProfileSettings profileSettings, ISettings globalSettings, ISettingManager settingManager)
        {
            settings.DeferChanges();
            InitializeComponent();

            globalSettings.PropertyChanged += _globalSettings_PropertyChanged;
            _settingManager = settingManager;


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

            var fonts = new List<string>
            {
                "Novar.ttf#Novar",
                "Zoika.ttf#Zoika font",
                "Segoe UI Light.ttf#Segoe UI Light",
                "Cascadia.ttf#Cascadia Code",
                "ANDALEMO.TTF#Andale Mono",
                "cinecavD type.ttf#CinecavD Type",
                "CutiveMono-Regular.ttf#Cutive Mono",
                "Elronmonospace.ttf#ElroNet Monospace",
                "LibertinusMono-Regular.otf#Libertinus Mono",
                "MajorMonoDisplay-Regular.ttf#Major Mono Display",
                "XanhMono-Regular.ttf#Xanh Mono",
                "Nitti-Normal.ttf#Nitti",
                "Ra-Mono.otf#Ra Mono",
                "SabirMonoRegular.otf#Sabir Mono",
                "Sanchezregular.otf#Sanchez Regular",
                "Solid-Mono.ttf#Solid Mono",
                "Torrance-lgZl0.ttf#Torrance"
            };

            FontSelector.ItemsSource = fonts;
        }

        private void _globalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(PomodoroEngine)}.{nameof(Settings.FontFamily)}")
            {
                _settingManager.RestartNeeded = true;
            }
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
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                      "Resources",
                      "Fonts",
                      value.ToString());
                var font = new FontFamily(path);
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
