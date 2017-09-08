using System;
using System.Windows;
using YAPA.Shared.Contracts;

namespace YAPA.WPF.Specifics
{
    public class ShowSettingsCommand : IShowSettingsCommand
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        private readonly IPluginManager _pluginManager;
        private readonly IDependencyInjector _container;
        private readonly IEnvironment _environment;

        public ShowSettingsCommand(ISettings settings, ISettingManager mananger, IPluginManager pluginManager, IDependencyInjector container, IEnvironment environment)
        {
            _settings = settings;
            _mananger = mananger;
            _pluginManager = pluginManager;
            _container = container;
            _environment = environment;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var settingWindow = new SettingsMananger.Settings(_settings, _mananger, _pluginManager, _container, _environment);
            settingWindow.Owner = (Window)_container.Resolve(typeof(IApplication)); 
            settingWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            settingWindow.ShowDialog();
        }

        public event EventHandler CanExecuteChanged;
    }
}
