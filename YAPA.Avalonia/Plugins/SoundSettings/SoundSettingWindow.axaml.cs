using System;
using Avalonia.Controls;
using YAPA.Avalonia.Infrastructure;
using YAPA.Avalonia.Plugins.SoundSettings.MusicPlayer;
using YAPA.Avalonia.Plugins.SoundSettings.SoundNotifications;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.SoundSettings
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

    public partial class SoundSettingWindow : UserControl
    {
        public SoundSettingWindow()
        {
            InitializeComponent();
        }

        public SoundSettingWindow(SoundNotificationSettingWindow soundNotification, MusicPlayerPluginSettingWindow musicPlayer, PomodoroEngineSettings engineSettings)
        {
            InitializeComponent();
            Container.Children.Add(soundNotification);
            Container.Children.Add(musicPlayer);
            engineSettings.DeferChanges();
            DataContext = engineSettings;
        }
    }
}
