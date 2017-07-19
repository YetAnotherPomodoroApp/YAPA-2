using System.Collections.Generic;

namespace YAPA.Shared.Contracts
{
    public interface IThemeManager
    {
        IEnumerable<IThemeMeta> Themes { get; }
        object ResolveSettingWindow(IThemeMeta theme);
        IThemeMeta ActiveTheme { get; }
    }
}
