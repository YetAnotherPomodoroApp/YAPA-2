using System.Collections.Generic;

namespace YAPA.Contracts
{
    public interface IPluginManager
    {
        IEnumerable<IPluginMeta> Plugins { get; }
        IEnumerable<IPluginMeta> ActivePlugins { get; }
        object ResolveSettingWindow(IPluginMeta plugin);
        void InitPlugins();
    }
}
