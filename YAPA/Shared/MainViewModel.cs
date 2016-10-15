using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class MainViewModel : IMainViewModel
    {
        private readonly ISettings _settings;
        public IPomodoroEngine Engine { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand StartCommand { get; set; }

        public ICommand ResetCommand { get; set; }

        public ICommand ShowSettingsCommand { get; set; }

        public MainViewModel(IPomodoroEngine engine, ISettings settings)
        {
            _settings = settings;
            Engine = engine;
            StopCommand = new StopCommand(Engine);
            StartCommand = new StartCommand(Engine);
            ResetCommand = new ResetCommand(Engine);

            ShowSettingsCommand = new ShowSettingsCommand(_settings);

        }
    }
}
