using System;
using System.IO;
using Avalonia.Controls;
using YAPA.Avalonia.Infrastructure;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;

namespace YAPA.Avalonia.Plugins.SoundSettings.SoundNotifications
{
    [BuiltInPlugin(Hide = true)]
    public class SoundNotificationsPlugin : IPluginMeta
    {
        public string Title => "Sound notifications";

        public string Id => "SoundNotifications";

        public Type Plugin => typeof(SoundNotifications);

        public Type Settings => typeof(SoundNotificationsSettings);

        public Type SettingEditWindow => typeof(SoundNotificationSettingWindow);
    }

    public class SoundNotifications : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly SoundNotificationsSettings _settings;
        private readonly IMusicPlayer _musicPlayer;
        private readonly PomodoroEngineSettings _engineSettings;

        public SoundNotifications(IPomodoroEngine engine, SoundNotificationsSettings settings, IMusicPlayer musicPlayer, PomodoroEngineSettings engineSettings)
        {
            _engine = engine;
            _settings = settings;
            _musicPlayer = musicPlayer;
            _engineSettings = engineSettings;

            _engine.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_engine.Phase))
                {
                    _musicPlayer.Stop();
                    Play();
                }
            };
        }

        private void PlaySound(string file)
        {
            _musicPlayer.Load(file);
            _musicPlayer.Play(volume: _engineSettings.Volume);
        }

        private void Play()
        {
            if (_engineSettings.DisableSoundNotifications || !_settings.PlayPeriodStartEndSounds)
            {
                return;
            }

            switch (_engine.Phase)
            {
                case PomodoroPhase.Work:
                case PomodoroPhase.Break:
                    PlaySound(_settings.PeriodStartSound);
                    break;
                case PomodoroPhase.BreakEnded:
                case PomodoroPhase.WorkEnded:
                    PlaySound(_settings.PeriodEndSound);
                    break;
            }
        }
    }

    public class SoundNotificationsSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string PeriodStartSound
        {
            get => _settings.Get(nameof(PeriodStartSound), Path.Combine(AppContext.BaseDirectory, "Resources", "tick.wav"));
            set => _settings.Update(nameof(PeriodStartSound), value);
        }

        public string PeriodEndSound
        {
            get => _settings.Get(nameof(PeriodEndSound), Path.Combine(AppContext.BaseDirectory, "Resources", "ding.wav"));
            set => _settings.Update(nameof(PeriodEndSound), value);
        }

        public bool PlayPeriodStartEndSounds
        {
            get => _settings.Get(nameof(PlayPeriodStartEndSounds), true);
            set => _settings.Update(nameof(PlayPeriodStartEndSounds), value);
        }

        public SoundNotificationsSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new SoundNotificationsPlugin().Id);
        }

        public void DeferChanges() => _settings.DeferChanges();
    }

    public class SoundNotificationSettingWindow : UserControl
    {
        public SoundNotificationSettingWindow(SoundNotificationsSettings settings)
        {
            settings.DeferChanges();
            DataContext = settings;
            // YAPA/Plugins/SoundSettings/SoundNotifications/SoundNotificationSettingWindow.xaml
            var checkBox = new CheckBox { Content = "Play period start\\end sounds" };
            checkBox.Bind(CheckBox.IsCheckedProperty, new global::Avalonia.Data.Binding(nameof(SoundNotificationsSettings.PlayPeriodStartEndSounds)) { Mode = global::Avalonia.Data.BindingMode.TwoWay });
            Content = new Grid { Children = { checkBox } };
        }
    }
}
