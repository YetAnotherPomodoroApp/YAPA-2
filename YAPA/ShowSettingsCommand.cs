using System;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{
    public class ShowSettingsCommand : ICommand
    {
        private readonly ISettings _settings;
        public ShowSettingsCommand(ISettings settings)
        {
            _settings = settings;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var settingWindow = new Settings(_settings);

            settingWindow.ShowDialog();
        }

        public event EventHandler CanExecuteChanged;
    }
}
