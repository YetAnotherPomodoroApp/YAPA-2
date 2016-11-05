using System.Collections.Generic;
using System.Linq;
using YAPA.Contracts;

namespace YAPA.Shared
{

    public class ThemeManager : IThemeManager
    {
        public IEnumerable<IThemeMeta> Themes { get; private set; }

        public IThemeMeta ActiveTheme { get; private set; }

        private IThemeMeta GetActiveTheme()
        {
            var theme = Themes.FirstOrDefault(x => x.Title == _settings.SelectedTheme) ?? Themes.First();

            return theme;
        }

        private readonly IDependencyInjector _container;
        private readonly ThemeManagerSettings _settings;

        public ThemeManager(IDependencyInjector container, IEnumerable<IThemeMeta> metas, ThemeManagerSettings settings)
        {
            _container = container;
            _settings = settings;
            Themes = metas;
            RegisterThemes(container);
            RegisterThemeSettings(container);
            RegisterThemeSettingsWindows(container);
            ActiveTheme = GetActiveTheme();
        }

        public object ResolveSettingWindow(IThemeMeta theme)
        {
            if (theme.SettingEditWindow == null)
            {
                return theme.SettingEditWindow;
            }
            return _container.Resolve(theme.SettingEditWindow);
        }

        private void RegisterThemes(IDependencyInjector container)
        {
            foreach (var theme in Themes.Where(x => x.Theme != null))
            {
                container.Register(theme.Theme, true);
            }
        }

        private void RegisterThemeSettings(IDependencyInjector container)
        {
            foreach (var theme in Themes.Where(x => x.Settings != null))
            {
                container.Register(theme.Settings);
            }
        }

        private void RegisterThemeSettingsWindows(IDependencyInjector container)
        {

            foreach (var theme in Themes.Where(x => x.SettingEditWindow != null))
            {
                container.Register(theme.SettingEditWindow);
            }
        }
    }

    public class ThemeManagerSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string SelectedTheme
        {
            get { return _settings.Get<string>(nameof(SelectedTheme), "Default theme"); }
            set { _settings.Update(nameof(SelectedTheme), value); }
        }

        public ThemeManagerSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(ThemeManager));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
