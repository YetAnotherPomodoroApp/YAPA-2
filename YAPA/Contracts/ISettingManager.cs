using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;

namespace YAPA.Contracts
{
    public interface ISettingManager : INotifyPropertyChanged
    {
        UserControl GetPageFor(string name);
        IEnumerable<string> GetPlugins();
        IEnumerable<string> GetRootSettings();

        bool RestartNeeded { get; set; }
    }
}
