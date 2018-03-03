using System;
using System.Windows;
using YAPA.Shared.Contracts;
using YAPA.WPF.SettingsMananger;

namespace YAPA.WPF.Specifics
{
    public class ShowSettingsCommand : IShowSettingsCommand
    {
        private readonly SettingsWindow _settings;
        private readonly IDependencyInjector _container;

        public ShowSettingsCommand(SettingsWindow settings,  IDependencyInjector container)
        {
            _settings = settings;
            _container = container;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _settings.Owner = (Window)_container.Resolve(typeof(IApplication));
            _settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _settings.ShowDialog();
        }

        public event EventHandler CanExecuteChanged;
    }
}
