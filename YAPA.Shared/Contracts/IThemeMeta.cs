using System;

namespace YAPA.Contracts
{
    public interface IThemeMeta
    {
        string Title { get; }
        Type Theme { get; }
        Type Settings { get; }
        Type SettingEditWindow { get; }
    }
}
