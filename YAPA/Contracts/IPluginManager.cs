using System.Collections.Generic;

namespace YAPA.Contracts
{
    public interface IPluginManager
    {
        IEnumerable<IPluginMeta> Plugins { get; }
        object ResolveSettingWindow(IPluginMeta plugin);
        void InitPlugins();
    }
}
