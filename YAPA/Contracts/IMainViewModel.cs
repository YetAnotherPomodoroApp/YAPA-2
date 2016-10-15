using System.Windows.Input;

namespace YAPA.Contracts
{
    public interface IMainViewModel
    {
        IPomodoroEngine Engine { get; set; }
        ICommand StopCommand { get; set; }
        ICommand StartCommand { get; set; }
        ICommand ResetCommand { get; set; }
        ICommand ShowSettingsCommand { get; set; }
    }
}
