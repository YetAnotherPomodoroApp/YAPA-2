using System;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.PomodoroEngine
{
    [BuiltInPlugin(Order = 2)]
    public class PomodoroEnginePluginMeta : IPluginMeta
    {
        public string Title => "General";

        public Type Plugin => null;

        public Type Settings => null;

        public Type SettingEditWindow => typeof(PomodoroEngineSettingWindow);
    }

}
