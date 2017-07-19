using System;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.PomodoroEngine
{
    [BuiltInPlugin(Order = 2)]
    public class PomodoroEnginePlugin : IPluginMeta
    {
        public string Title => "General";
        public string Id => "PomodoroEngine";

        public Type Plugin => null;

        public Type Settings => null;

        public Type SettingEditWindow => typeof(PomodoroEngineSettingWindow);
    }

}
