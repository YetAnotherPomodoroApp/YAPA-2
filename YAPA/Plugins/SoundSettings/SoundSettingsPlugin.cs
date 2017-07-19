using System;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.SoundSettings
{
    [BuiltInPlugin]
    public class SoundSettingsPlugin : IPluginMeta
    {
        public string Title => "Sound";
        public string Id => "SoundSettingsPlugin";

        public Type Plugin => null;

        public Type Settings => null;

        public Type SettingEditWindow => typeof(SoundSettingWindow);
    }
}
