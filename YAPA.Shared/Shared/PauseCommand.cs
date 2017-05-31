using System;
using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA.Shared.Shared
{
    public class PauseCommand : ICommand
    {
        private readonly IPomodoroEngine _engine;
        public PauseCommand(IPomodoroEngine engine)
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
            return _engine.Phase == PomodoroPhase.Work;
        }

        public void Execute(object parameter)
        {
            _engine.Pause();
        }

        public event EventHandler CanExecuteChanged;
    }
}
