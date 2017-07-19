using System.Windows.Controls;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.ThemeManager
{
    public partial class ThemeManagerSettingWindow : UserControl
    {
        private readonly IThemeManager _themes;
        private readonly ThemeManagerSettings _settings;
        private readonly ISettingManager _manager;
        private readonly ISettings _globalSettings;

        public ThemeManagerSettingWindow(IThemeManager themes, ThemeManagerSettings settings, ISettingManager manager, ISettings globalSettings)
        {
            _themes = themes;
            _settings = settings;
            _manager = manager;
            _globalSettings = globalSettings;

            _globalSettings.PropertyChanged += _globalSettings_PropertyChanged;

            _settings.DeferChanges();

            InitializeComponent();
            foreach (var themeMeta in _themes.Themes)
            {
                var selectedTheme = settings.SelectedTheme == themeMeta.Title;
                var cb = new ComboBoxItem
                {
                    IsSelected = selectedTheme,
                    Content = themeMeta.Title
                };

                ThemeList.Items.Add(cb);
            }

            if (_themes.ActiveTheme.SettingEditWindow != null)
            {
                SettingPage.Children.Clear();
                SettingPage.Children.Add((UserControl)_themes.ResolveSettingWindow(_themes.ActiveTheme));
            }

            ThemeList.SelectionChanged += ThemeList_SelectionChanged;
        }

        private void _globalSettings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
