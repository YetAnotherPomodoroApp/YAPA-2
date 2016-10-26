using System;

namespace YAPA.Contracts
{
    public interface IPluginMeta
    {
        string Title { get; }
        Type Plugin { get; }
        Type Settings { get; }
        Type SettingEditWindow { get; }
    }
}
