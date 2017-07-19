using System;
using YAPA.Shared.Contracts;
using YAPA.WPF.Themes.DefaultTheme;

namespace YAPA.WPF
{
    public class DefaultTheme : IThemeMeta
    {
        public string Title => "Default theme";

        public Type Theme => typeof(MainWindow);

        public Type Settings => typeof(DefaultThemeSettings);

        public Type SettingEditWindow => typeof(MainWindowSettinWindow);
    }

    public class DefaultThemeSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public int Width
        {
            get { return _settings.Get(nameof(Width), 250); }
            set { _settings.Update(nameof(Width), value); }
        }

        public DefaultThemeSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(DefaultTheme));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
