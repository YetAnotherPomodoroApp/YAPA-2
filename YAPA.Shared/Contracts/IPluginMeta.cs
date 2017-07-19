using System;

namespace YAPA.Shared.Contracts
{
    public interface IPluginMeta
    {
        string Title { get; }
        string Id { get; }
        Type Plugin { get; }
        Type Settings { get; }
        Type SettingEditWindow { get; }
    }
}
