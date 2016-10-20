using Autofac;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using YAPA.Contracts;
using YAPA.WPF;
using IContainer = Autofac.IContainer;

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

        private bool _restartNeeded;

        public bool RestartNeeded
        {
            get { return _restartNeeded; }
            set
            {
                _restartNeeded = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
