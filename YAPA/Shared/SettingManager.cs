using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA.Shared
{
    public class SettingManager : ISettingManager
    {
        private readonly IContainer _container;
        private readonly IPluginManager _plugins;
        private Dictionary<string, Type> _rootSettings;

        public SettingManager(IContainer container, IPluginManager plugins)
        {
            _container = container;
            _plugins = plugins;

            _rootSettings = new Dictionary<string, Type>
            {
                ["Themes"] = typeof(ThemeManagerSettingWindow),
                ["Plugins"] = null //TODO
            };
        }

        public UserControl GetPageFor(string name)
        {
            Type type = _plugins.Plugins.Where(x => x.SettingEditWindow != null && x.Title == name).Select(x => x.SettingEditWindow).FirstOrDefault();

            if (type == null)
            {
                type = _rootSettings[name];
            }

            return (UserControl)_container.Resolve(type);
        }

        public IEnumerable<string> GetPlugins()
        {
            return _plugins.Plugins.Where(x => x.SettingEditWindow != null).Select(x => x.Title);
        }

        public IEnumerable<string> GetRootSettings()
        {
            return _rootSettings.Select(x => x.Key);
        }

        public bool RestartNeeded { get; set; }
    }
}
