using System;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class ResetCommand : ICommand
    {
        private readonly IPomodoroEngine _engine;
        public ResetCommand(IPomodoroEngine engine)
        {
            _engine = engine;
            _engine.PropertyChanged += _engine_PropertyChanged;
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_engine.Phase))
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter)
        {
            //Can't reset if we haven't started any pomodoro
            return !(_engine.Index == 1 && _engine.Phase == PomodoroPhase.NotStarted);
        }

        public void Execute(object parameter)
        {
            _engine.Reset();
        }

        public event EventHandler CanExecuteChanged;
    }
}
