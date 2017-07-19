using System;
using System.Windows;
using System.Windows.Input;
using YAPA.Shared.Contracts;

namespace YAPA.Commands
{
    public class CancelSettingsCommand : ICommand
    {
        private readonly Window _settingsWindow;
        private readonly ISettings _settings;

        public CancelSettingsCommand(Window settingWindow, ISettings settings)
        {
            _settingsWindow = settingWindow;
            _settings = settings;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (_settings.HasUnsavedChanges && MessageBox.Show("Do you want to cancel unsaved changes ?", "Cancel unsaved changes", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                return;
            }
            if (_settings.HasUnsavedChanges)
            {
                _settings.Load();
            }
            _settingsWindow.Close();
        }

        public event EventHandler CanExecuteChanged;
    }
}
