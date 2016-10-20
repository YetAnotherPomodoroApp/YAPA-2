using System.Windows.Controls;
using YAPA.Contracts;
using YAPA.Shared;

namespace YAPA.WPF
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
                var cb = new ComboBoxItem
                {
                    IsSelected = settings.SelectedTheme == themeMeta.Title,
                    Content = themeMeta.Title
                };
                ThemeList.Items.Add(cb);
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
