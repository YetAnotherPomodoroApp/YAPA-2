using System.Windows.Controls;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.ThemeManager
{
    public partial class ThemeManagerSettingWindow
    {
        private readonly ThemeManagerSettings _settings;
        private readonly ISettingManager _manager;
        private readonly ISettings _globalSettings;

        public ThemeManagerSettingWindow(IThemeManager themeManager, ThemeManagerSettings settings, ISettingManager manager, ISettings globalSettings)
        {
            var themes = themeManager;
            _settings = settings;
            _manager = manager;
            _globalSettings = globalSettings;

            _globalSettings.PropertyChanged += GlobalSettings_PropertyChanged;

            _settings.DeferChanges();

            InitializeComponent();

            foreach (var themeMeta in themes.Themes)
            {
                var selectedTheme = settings.SelectedTheme == themeMeta.Title;
                var cb = new ComboBoxItem
                {
                    IsSelected = selectedTheme,
                    Content = themeMeta.Title
                };

                ThemeList.Items.Add(cb);
            }

            if (themes.ActiveTheme.SettingEditWindow != null)
            {
                SettingPage.Children.Clear();
                SettingPage.Children.Add((UserControl)themes.ResolveSettingWindow(themes.ActiveTheme));
            }

            ThemeList.SelectionChanged += ThemeList_SelectionChanged;

            Unloaded += ThemeManagerSettingWindow_Unloaded;
        }

        private void ThemeManagerSettingWindow_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _globalSettings.PropertyChanged -= GlobalSettings_PropertyChanged;
        }

        private void GlobalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == $"{nameof(ThemeManager)}.{nameof(_settings.SelectedTheme)}")
            {
                _manager.RestartNeeded = true;
            }
        }

        private void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _settings.SelectedTheme = (string)((ComboBoxItem)ThemeList.SelectedItem).Content;
        }

    }
}
