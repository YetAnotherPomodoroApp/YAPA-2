using System;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{
    public class ShowSettingsCommand : ICommand
    {
        private readonly ISettings _settings;
        private readonly ISettingManager _mananger;

        public ShowSettingsCommand(ISettings settings, ISettingManager mananger)
        {
            _settings = settings;
            _mananger = mananger;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var settingWindow = new Settings(_settings, _mananger);

            settingWindow.ShowDialog();
        }

        public event EventHandler CanExecuteChanged;
    }
}
