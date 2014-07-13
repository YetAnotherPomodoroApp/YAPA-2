using System;
using System.Windows.Input;

namespace YAPA
{
    /// <summary>
    /// Command used to support saving the new settings.
    /// </summary>
    public class SaveSettings : ICommand
    {
        private ISettingsViewModel _host;

        /// <summary>
        /// Creates a new instance of this command.
        /// </summary>
        /// <param name="host">The hosting Window control</param>
        public SaveSettings(ISettingsViewModel host)
        {
            _host = host;
        }

        /// <summary>
        /// Returns true if the command can execute; otherwise false.
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return true;
        }

        /// <summary>
        /// Raised when the CanExecute value changes.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Executes the command.
        /// </summary>
        public void Execute(object parameter)
        {
            YAPA.Properties.Settings.Default.Save();
            _host.CloseSettings();
        }
    }
}
