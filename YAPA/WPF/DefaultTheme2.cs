using System;
using YAPA.Contracts;

namespace YAPA.WPF
{
    public class DefaultTheme2 : IThemeMeta
    {
        public string Title => "Default theme (2)";

        public Type Theme => typeof(MainWindow2);

        public Type Settings => null;

        public Type SettingEditWindow => null;
    }
}
