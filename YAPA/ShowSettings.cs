using System;
using System.Windows.Input;

namespace YAPA
{
    /// <summary>
    /// Command used to handle 'gear' clicks
    /// </summary>
    public class ShowSettings : ICommand
    {
        private IMainViewModel _host;

        /// <summary>
        /// Creates a new ShowSettings instance using the specified host.
        /// </summary>
        public ShowSettings(IMainViewModel host)
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
            // show settings window
            var settingsWindow = new Settings(_host, _host.ClockOpacity, _host.TextBrush, _host.WorkTime, _host.BreakTime, _host.BreakLongTime, _host.SoundEffects, _host.ShadowOpacity);
            settingsWindow.ShowDialog();
        }
    }
}
