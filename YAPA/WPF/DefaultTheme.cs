using System;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class DefaultTheme : IThemeMeta
    {
        public string Title => "Default theme";

        public Type Theme => typeof(MainWindow);

        public Type Settings => null;

        public Type SettingEditWindow => null;
    }
}
