using System.Windows.Controls;
using YAPA.Contracts;
using YAPA.Shared;

namespace YAPA.WPF
{
    public partial class ThemeManagerSettingWindow : UserControl
    {
        private readonly IThemeManager _themes;
        private readonly ThemeManagerSettings _settings;

        public ThemeManagerSettingWindow(IThemeManager themes, ThemeManagerSettings settings)
        {

            _themes = themes;
            _settings = settings;
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

        private void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _settings.SelectedTheme = (string)((ComboBoxItem)ThemeList.SelectedItem).Content;
        }
    }
}
