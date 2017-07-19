using System;
using System.Windows.Input;
using YAPA.Shared.Contracts;

namespace YAPA.Commands
{
    public class SaveSettingsCommand : ICommand
    {
        private readonly ISettings _settings;

        public SaveSettingsCommand(ISettings settings)
        {
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

        }

        public event EventHandler CanExecuteChanged;
    }
}
