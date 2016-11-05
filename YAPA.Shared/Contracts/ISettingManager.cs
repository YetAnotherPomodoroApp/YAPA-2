using System.ComponentModel;

namespace YAPA.Contracts
{
    public interface ISettingManager : INotifyPropertyChanged
    {
        bool RestartNeeded { get; set; }
    }
}
