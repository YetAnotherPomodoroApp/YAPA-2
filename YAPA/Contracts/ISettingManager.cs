using System.Collections.Generic;
using System.Windows.Controls;

namespace YAPA.Contracts
{
    public interface ISettingManager
    {
        UserControl GetPageFor(string name);
        IEnumerable<string> GetPlugins();
        IEnumerable<string> GetRootSettings();

        bool RestartNeeded { get; set; }
    }
}
