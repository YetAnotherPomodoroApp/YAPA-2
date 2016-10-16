using System.Windows.Input;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class MainViewModel : IMainViewModel
    {
        private readonly ISettings _settings;
        private readonly IPluginManager _plugins;
        public IPomodoroEngine Engine { get; set; }

        public ICommand StopCommand { get; set; }

        public ICommand StartCommand { get; set; }

        public ICommand ResetCommand { get; set; }

        public ICommand ShowSettingsCommand { get; set; }

        public MainViewModel(IPomodoroEngine engine, ISettings settings, IPluginManager plugins)
        {
            _settings = settings;
            _plugins = plugins;
            Engine = engine;
            StopCommand = new StopCommand(Engine);
            StartCommand = new StartCommand(Engine);
            ResetCommand = new ResetCommand(Engine);

            ShowSettingsCommand = new ShowSettingsCommand(_settings, plugins);

        }
    }
}
