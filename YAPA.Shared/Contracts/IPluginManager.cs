using System.Collections.Generic;

namespace YAPA.Shared.Contracts
{
    public interface IPluginManager
    {
        IEnumerable<IPluginMeta> Plugins { get; }
        IEnumerable<IPluginMeta> BuiltInPlugins { get; }
        IEnumerable<IPluginMeta> CustomPlugins { get; }
        IEnumerable<IPluginMeta> ActivePlugins { get; }
        object ResolveSettingWindow(IPluginMeta plugin);
        void InitPlugins();
    }
}
