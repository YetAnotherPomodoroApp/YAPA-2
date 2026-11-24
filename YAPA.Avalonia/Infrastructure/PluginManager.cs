using System;
using System.Collections.Generic;
using System.Linq;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BuiltInPluginAttribute : Attribute
    {
        public int Order { get; set; } = 999;
        public bool Hide { get; set; }
    }

    public class PluginManager : IPluginManager
    {
        private readonly IDependencyInjector _container;
        private readonly PluginManagerSettings _settings;
        private IEnumerable<IPlugin> _pluginInstances;
        private readonly IEnumerable<string> _enabledPlugins;
        private bool _initialised;

        public PluginManager(IDependencyInjector container, IEnumerable<IPluginMeta> metas, PluginManagerSettings settings, ISettings gloabalSettings)
        {
            _container = container;
            _settings = settings;
            ApplyMigration(settings, gloabalSettings);
            _enabledPlugins = _settings.EnabledPlugins;
            Plugins = metas;
        }

        private static void ApplyMigration(PluginManagerSettings settings, ISettings gloabalSettings)
        {
            var migrations = new List<Tuple<string, string>> { Tuple.Create("Minimize to tray", "SystemTray") };
            var enabled = settings.EnabledPlugins;
            foreach (var migration in migrations)
            {
                if (enabled.Contains(migration.Item1))
                {
                    enabled.Remove(migration.Item1);
                    enabled.Add(migration.Item2);
                }
            }
            settings.EnabledPlugins = enabled;
            gloabalSettings.Save();
        }

        public IEnumerable<IPluginMeta> Plugins { get; }

        public IEnumerable<IPluginMeta> BuiltInPlugins =>
            Plugins.Where(x => Attribute(x) != null).OrderBy(x => Attribute(x).Order);

        public IEnumerable<IPluginMeta> CustomPlugins => Plugins.Where(x => Attribute(x) == null);

        public IEnumerable<IPluginMeta> ActivePlugins => CustomPlugins.Where(x => _enabledPlugins.Contains(x.Id));

        private static BuiltInPluginAttribute Attribute(IPluginMeta meta)
        {
            return (BuiltInPluginAttribute)meta.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).FirstOrDefault();
        }

        public object ResolveSettingWindow(IPluginMeta plugin)
        {
            return plugin.SettingEditWindow == null ? null : _container.Resolve(plugin.SettingEditWindow);
        }

        public void InitPlugins()
        {
            if (_initialised)
            {
                return;
            }
            foreach (var plugin in Plugins.Where(x => x.Settings != null))
            {
                _container.Register(plugin.Settings);
            }
            foreach (var plugin in Plugins.Where(x => x.SettingEditWindow != null))
            {
                _container.Register(plugin.SettingEditWindow);
            }
            foreach (var plugin in Plugins.Where(x => x.Plugin != null))
            {
                _container.Register(plugin.Plugin, true);
            }
            _pluginInstances = ActivePlugins.Union(BuiltInPlugins)
                .Where(x => x.Plugin != null)
                .Select(plugin => (IPlugin)_container.Resolve(plugin.Plugin))
                .ToList();
            _initialised = true;
        }
    }

    public static class PluginExtensions
    {
        public static bool IsHidden(this IPluginMeta plugin)
        {
            var attribute = (BuiltInPluginAttribute)plugin.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).FirstOrDefault();
            return attribute != null && attribute.Hide;
        }
    }

    public class PluginManagerSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public List<string> EnabledPlugins
        {
            get => _settings.Get(nameof(EnabledPlugins), new List<string>()) ?? new List<string>();
            set => _settings.Update(nameof(EnabledPlugins), value);
        }

        public PluginManagerSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(PluginManager));
        }

        public void DeferChanges() => _settings.DeferChanges();
    }

    public class ThemeManager : IThemeManager
    {
        private readonly IDependencyInjector _container;
        private readonly ThemeManagerSettings _settings;

        public IEnumerable<IThemeMeta> Themes { get; }
        public IThemeMeta ActiveTheme { get; }

        public ThemeManager(IDependencyInjector container, IEnumerable<IThemeMeta> metas, ThemeManagerSettings settings)
        {
            _container = container;
            _settings = settings;
            Themes = metas;
            foreach (var theme in Themes)
            {
                if (theme.Theme != null) container.Register(theme.Theme, true);
                if (theme.Settings != null) container.Register(theme.Settings);
                if (theme.SettingEditWindow != null) container.Register(theme.SettingEditWindow);
            }
            ActiveTheme = Themes.FirstOrDefault(x => x.Title == _settings.SelectedTheme) ?? Themes.First();
        }

        public object ResolveSettingWindow(IThemeMeta theme)
        {
            return theme.SettingEditWindow == null ? null : _container.Resolve(theme.SettingEditWindow);
        }
    }

    public class ThemeManagerSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string SelectedTheme
        {
            get => _settings.Get(nameof(SelectedTheme), "YAPA 1.0");
            set => _settings.Update(nameof(SelectedTheme), value);
        }

        public ThemeManagerSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent("ThemeManager");
        }

        public void DeferChanges() => _settings.DeferChanges();
    }
}
