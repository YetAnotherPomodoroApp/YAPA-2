using System;
using System.Collections.Generic;
using System.Linq;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.PluginManager
{
    public class PluginManager : IPluginManager
    {
        private readonly IDependencyInjector _container;
        private readonly PluginManagerSettings _settings;
        private IEnumerable<IPlugin> _pluginInstances;
        private IEnumerable<string> _enabledPlugins;
        private bool _initialised = false;

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

        public IEnumerable<IPluginMeta> BuiltInPlugins
        {
            get
            {
                return Plugins
                .Where(x =>
                {
                    var attribute = x.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).FirstOrDefault();
                    return attribute != null;
                })
                .OrderBy(x => ((BuiltInPluginAttribute)x.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).FirstOrDefault()).Order);
            }
        }

        public IEnumerable<IPluginMeta> CustomPlugins
        {
            get
            {
                return Plugins.Where(_ => _.GetType().GetCustomAttributes(false).FirstOrDefault(y => y.GetType() == typeof(BuiltInPluginAttribute)) == null);
            }
        }

        public IEnumerable<IPluginMeta> ActivePlugins
        {
            get { return CustomPlugins.Where(x => _enabledPlugins.Contains(x.Id)); }
        }

        public object ResolveSettingWindow(IPluginMeta plugin)
        {
            if (plugin.SettingEditWindow == null)
            {
                return plugin.SettingEditWindow;
            }
            return _container.Resolve(plugin.SettingEditWindow);
        }

        public void InitPlugins()
        {
            if (_initialised)
            {
                return;
            }
            RegisterPluginSettings(_container);
            RegisterPluginSettingsWindows(_container);
            _pluginInstances = RegisterPlugins(_container);
            _initialised = true;
        }

        private IEnumerable<IPlugin> RegisterPlugins(IDependencyInjector container)
        {
            foreach (var plugin in Plugins.Union(BuiltInPlugins).Where(x => x.Plugin != null))
            {
                container.Register(plugin.Plugin, true);
            }

            return ActivePlugins.Union(BuiltInPlugins).Where(x => x.Plugin != null).Select(plugin => (IPlugin)container.Resolve(plugin.Plugin)).ToList();
        }

        private void RegisterPluginSettings(IDependencyInjector container)
        {
            foreach (var plugin in Plugins.Union(BuiltInPlugins).Where(x => x.Settings != null))
            {
                container.Register(plugin.Settings);
            }
        }

        private void RegisterPluginSettingsWindows(IDependencyInjector container)
        {
            foreach (var plugin in Plugins.Union(BuiltInPlugins).Where(x => x.SettingEditWindow != null))
            {
                container.Register(plugin.SettingEditWindow);
            }
        }
    }

    public static class PluginExtensions
    {
        public static bool IsHidden(this IPluginMeta plugin)
        {
            var attribute = plugin.GetType().GetCustomAttributes(typeof(BuiltInPluginAttribute), false).FirstOrDefault();
            return attribute != null && ((BuiltInPluginAttribute)attribute).Hide;
        }
    }

    public class PluginManagerSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public List<string> EnabledPlugins
        {
            get { return _settings.Get(nameof(EnabledPlugins), new List<string>()); }
            set { _settings.Update(nameof(EnabledPlugins), value); }
        }

        public PluginManagerSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(PluginManager));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }
}
