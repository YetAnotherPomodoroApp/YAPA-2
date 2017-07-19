using System.ComponentModel;

namespace YAPA.Shared.Contracts
{
    public interface ISettingManager : INotifyPropertyChanged
    {
        bool RestartNeeded { get; set; }
    }
}
