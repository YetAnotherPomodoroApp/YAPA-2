using System.Windows.Input;
using YAPA.Contracts;
using YAPA.Shared.Contracts;

namespace YAPA.Shared
{
    public class MainViewModel : IMainViewModel
    {
        public IPomodoroEngine Engine { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand StartCommand { get; set; }

        public ICommand ResetCommand { get; set; }

        public ICommand ShowSettingsCommand { get; set; }

        public MainViewModel(IPomodoroEngine engine, IShowSettingsCommand showSettings)
        {
            Engine = engine;
            StopCommand = new StopCommand(Engine);
            StartCommand = new StartCommand(Engine);
            ResetCommand = new ResetCommand(Engine);

            ShowSettingsCommand = showSettings;
        }
    }
}
