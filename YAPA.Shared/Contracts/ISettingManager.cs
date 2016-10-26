using System.Collections.Generic;
using System.ComponentModel;

namespace YAPA.Contracts
{
    public interface ISettingManager : INotifyPropertyChanged
    {
        object GetPageFor(string name);
        IEnumerable<string> GetPlugins();
        IEnumerable<string> GetRootSettings();

        bool RestartNeeded { get; set; }
    }
}
