using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using YAPA.Contracts;

namespace YAPA.Shared
{

    public class ThemeManager : IThemeManager
    {
        public IEnumerable<IThemeMeta> Themes { get; private set; }

        public Type GetActiveTheme()
        {
            var theme = Themes.FirstOrDefault(x => x.Title == _settings.SelectedTheme);
            if (theme == null)
            {
                theme = Themes.First();
            }

            return theme.Theme;
        }

        private readonly IContainer _container;
        private readonly ThemeManagerSettings _settings;

        public ThemeManager(IContainer container, IEnumerable<IThemeMeta> metas, ThemeManagerSettings settings)
        {
            _container = container;
            _settings = settings;
            Themes = metas;
            RegisterThemes(container);
            RegisterThemeSettings(container);
            RegisterThemeSettingsWindows(container);
        }

        public object ResolveSettingWindow(IThemeMeta theme)
        {
            if (theme.SettingEditWindow == null)
            {
                return theme.SettingEditWindow;
            }
            return _container.Resolve(theme.SettingEditWindow);
        }

        private void RegisterThemes(IContainer container)
        {
            var updater = new ContainerBuilder();

            foreach (var theme in Themes.Where(x => x.Theme != null))
            {
                updater.RegisterType(theme.Theme).SingleInstance();
            }

            updater.Update(container);
        }

        private void RegisterThemeSettings(IContainer container)
        {
            var updater = new ContainerBuilder();

            foreach (var theme in Themes.Where(x => x.Settings != null))
            {
                updater.RegisterType(theme.Settings);
            }

            updater.Update(container);
        }

        private void RegisterThemeSettingsWindows(IContainer container)
        {
            var updater = new ContainerBuilder();

            foreach (var theme in Themes.Where(x => x.SettingEditWindow != null))
            {
                updater.RegisterType(theme.SettingEditWindow);
            }

            updater.Update(container);
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
