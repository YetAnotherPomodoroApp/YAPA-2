using System;
using System.Windows;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA
{
    public class SaveSettingsCommand : ICommand
    {
        private readonly Window _settingWindow;
        private readonly ISettings _settings;

        public SaveSettingsCommand(Window settingWindow, ISettings settings)
        {
            _settingWindow = settingWindow;
            _settings = settings;
            _settings.PropertyChanged += _settings_PropertyChanged;
        }

        private void _settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_settings.HasUnsavedChanges))
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            return _settings.HasUnsavedChanges;
        }

        public void Execute(object parameter)
        {
            _settings.Save();
            _settingWindow.Close();
        }

        public event EventHandler CanExecuteChanged;
    }
}
