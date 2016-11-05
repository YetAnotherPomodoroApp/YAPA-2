using System;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.SoundSettings
{
    [BuiltInPlugin]
    public class SoundSettingsPlugin : IPluginMeta
    {
        public string Title => "Sound";

        public Type Plugin => null;

        public Type Settings => null;

        public Type SettingEditWindow => typeof(SoundSettingWindow);
    }
}
