using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Settings
{
    [BuiltInPlugin(Order = 4)]
    public class ThemeManagerPlugin : IPluginMeta
    {
        public string Title => "Themes";

        public string Id => "ThemeManager";

        public System.Type Plugin => null;

        public System.Type Settings => null;

        public System.Type SettingEditWindow => typeof(ThemeManagerSettingWindow);
    }

    public partial class ThemeManagerSettingWindow : UserControl
    {
        private readonly ThemeManagerSettings _settings;
        private readonly ISettingManager _manager;
        private readonly ISettings _globalSettings;

        public ThemeManagerSettingWindow()
        {
            InitializeComponent();
        }

        public ThemeManagerSettingWindow(IThemeManager themes, ThemeManagerSettings settings, ISettingManager manager, ISettings globalSettings)
        {
            _settings = settings;
            _manager = manager;
            _globalSettings = globalSettings;
            _globalSettings.PropertyChanged += GlobalSettings_PropertyChanged;
            _settings.DeferChanges();

            InitializeComponent();

            var titles = themes.Themes.Select(x => x.Title).ToList();
            ThemeList.ItemsSource = titles;
            ThemeList.SelectedItem = titles.FirstOrDefault(x => x == settings.SelectedTheme);

            if (themes.ActiveTheme.SettingEditWindow != null)
            {
                SettingPage.Children.Clear();
                SettingPage.Children.Add((Control)themes.ResolveSettingWindow(themes.ActiveTheme));
            }

            ThemeList.SelectionChanged += (s, e) =>
            {
                if (ThemeList.SelectedItem is string title)
                {
                    _settings.SelectedTheme = title;
                }
            };
            DetachedFromVisualTree += (s, e) => _globalSettings.PropertyChanged -= GlobalSettings_PropertyChanged;
            AttachedToVisualTree += (s, e) =>
            {
                _globalSettings.PropertyChanged -= GlobalSettings_PropertyChanged;
                _globalSettings.PropertyChanged += GlobalSettings_PropertyChanged;
            };
        }

        private void GlobalSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(ThemeManager)}.{nameof(_settings.SelectedTheme)}")
            {
                _manager.RestartNeeded = true;
            }
        }
    }
}
