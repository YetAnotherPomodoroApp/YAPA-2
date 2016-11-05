using System;
using YAPA.Contracts;

namespace YAPA
{
    public class ShowSettingsCommand : IShowSettingsCommand
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;
        private readonly IPluginManager _pluginManager;
        private readonly IDependencyInjector _container;

        public ShowSettingsCommand(ISettings settings, ISettingManager mananger, IPluginManager pluginManager, IDependencyInjector container)
        {
            _settings = settings;
            _mananger = mananger;
            _pluginManager = pluginManager;
            _container = container;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var settingWindow = new Settings(_settings, _mananger, _pluginManager, _container);

            settingWindow.ShowDialog();
        }

        public event EventHandler CanExecuteChanged;
    }
}
