using Autofac;
using System.Collections.Generic;
using System.Linq;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class PluginManager : IPluginManager
    {
        private readonly IContainer _container;
        private readonly PluginManagerSettings _settings;
        private IEnumerable<IPlugin> _pluginInstances;
        private bool _initialised = false;
        public PluginManager(IContainer container, IEnumerable<IPluginMeta> metas, PluginManagerSettings settings)
        {
            _container = container;
            _settings = settings;
            Plugins = metas;
        }

        public IEnumerable<IPluginMeta> Plugins { get; }

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

        private IEnumerable<IPlugin> RegisterPlugins(IContainer container)
        {
            var updater = new ContainerBuilder();

            foreach (var plugin in Plugins.Where(x => x.Plugin != null))
            {
                updater.RegisterType(plugin.Plugin).SingleInstance();
            }
            updater.Update(container);

            return Plugins.Where(x => x.Plugin != null).Select(plugin => (IPlugin)container.Resolve(plugin.Plugin)).ToList();
        }

        private void RegisterPluginSettings(IContainer container)
        {
            var updater = new ContainerBuilder();

            foreach (var plugin in Plugins)
            {
                updater.RegisterType(plugin.Settings);
            }

            updater.Update(container);
        }

        private void RegisterPluginSettingsWindows(IContainer container)
        {
            var updater = new ContainerBuilder();

            foreach (var plugin in Plugins.Where(x => x.SettingEditWindow != null))
            {
                updater.RegisterType(plugin.SettingEditWindow);
            }

            updater.Update(container);
        }
    }


    public class PluginManagerSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

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
