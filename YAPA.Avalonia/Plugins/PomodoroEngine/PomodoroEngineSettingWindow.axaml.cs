using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.PomodoroEngine
{
    [BuiltInPlugin(Order = 2)]
    public class PomodoroEnginePlugin : IPluginMeta
    {
        public string Title => "General";

        public string Id => "PomodoroEngine";

        public Type Plugin => null;

        public Type Settings => null;

        public Type SettingEditWindow => typeof(PomodoroEngineSettingWindow);
    }

    public partial class PomodoroEngineSettingWindow : UserControl
    {
        public PomodoroEngineSettings Settings { get; }
        public IPomodoroEngine Engine { get; }

        public PomodoroEngineSettingWindow()
        {
            InitializeComponent();
        }

        public PomodoroEngineSettingWindow(PomodoroEngineSettings settings, IPomodoroEngine engine, PomodoroProfileSettings profileSettings, IFontService fontService)
        {
            settings.DeferChanges();
            InitializeComponent();

            CounterList.ItemsSource = new List<CounterListItem>
            {
                new CounterListItem { Item = CounterEnum.CompletedThisSession, Title = "Completed this session" },
                new CounterListItem { Item = CounterEnum.CompletedToday, Title = "Completed today" },
                new CounterListItem { Item = CounterEnum.PomodoroIndex, Title = "Pomodoro index" },
            };

            Settings = settings;
            Engine = engine;

            ProfileSetting.Children.Clear();
            ProfileSetting.Children.Add(profileSettings);

            FontSelector.ItemsSource = fontService.GetAllFonts();
            DataContext = this;
        }
    }

    public class CounterListItem
    {
        public CounterEnum Item { get; set; }
        public string Title { get; set; }
    }

    public class StringToFontFamily : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fontName = value?.ToString();
            return string.IsNullOrEmpty(fontName) ? FontFamily.Default : new FontFamily(fontName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
