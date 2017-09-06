using System.Windows.Input;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Common
{
    public class MainViewModel : IMainViewModel
    {
        public IPomodoroEngine Engine { get; set; }

        public ICommand StopCommand { get; set; }
        public ICommand StartCommand { get; set; }
        public ICommand ResetCommand { get; set; }
        public ICommand PauseCommand { get; set; }

        public ICommand ShowSettingsCommand { get; set; }

        public MainViewModel(IPomodoroEngine engine, IShowSettingsCommand showSettings)
        {
            Engine = engine;
            StopCommand = new StopCommand(Engine);
            StartCommand = new StartCommand(Engine);
            ResetCommand = new ResetCommand(Engine);
            PauseCommand= new PauseCommand(Engine);

            ShowSettingsCommand = showSettings;
        }
    }
}
