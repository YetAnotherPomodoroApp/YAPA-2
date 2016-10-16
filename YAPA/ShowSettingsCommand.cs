using System;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{
    public class ShowSettingsCommand : ICommand
    {
        private readonly ISettings _settings;
        private readonly IPluginManager _plugins;

        public ShowSettingsCommand(ISettings settings, IPluginManager plugins)
        {
            _settings = settings;
            _plugins = plugins;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var settingWindow = new Settings(_settings, _plugins);

            settingWindow.ShowDialog();
        }

        public event EventHandler CanExecuteChanged;
    }
}
