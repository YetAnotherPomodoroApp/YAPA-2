using System;
using System.IO;
using YAPA.Shared.Common;
using YAPA.Shared.Contracts;
using YAPA.WPF;

namespace YAPA.Plugins.SoundSettings.SoundNotifications
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

            _engine.PropertyChanged += _engine_PropertyChanged;
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_engine.Phase))
            {
                _musicPlayer.Stop();
                Play();
            }
        }

        private void PlayPeriodStart()
        {
            _musicPlayer.Load(_settings.PeriodStartSound);
            _musicPlayer.Play(volume: _engineSettings.Volume);
        }

        private void PlayPeriodEnd()
        {
            _musicPlayer.Load(_settings.PeriodEndSound);
            _musicPlayer.Play(volume: _engineSettings.Volume);
        }

        private void Play()
        {
            if (_engineSettings.DisableSoundNotifications)
            {
                return;
            }

            switch (_engine.Phase)
            {
                case PomodoroPhase.Work:
                    if (_settings.PlayPeriodStartEndSounds) PlayPeriodStart();
                    break;
                case PomodoroPhase.Break:
                    if (_settings.PlayPeriodStartEndSounds) PlayPeriodStart();
                    break;
                case PomodoroPhase.BreakEnded:
                case PomodoroPhase.WorkEnded:
                    if (_settings.PlayPeriodStartEndSounds) PlayPeriodEnd();
                    break;
            }
        }
    }

    public class SoundNotificationsSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

        public string PeriodStartSound
        {
            get { return _settings.Get(nameof(PeriodStartSound), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\tick.wav")); }
            set { _settings.Update(nameof(PeriodStartSound), value); }
        }

        public string PeriodEndSound
        {
            get { return _settings.Get(nameof(PeriodEndSound), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\ding.wav")); }
            set { _settings.Update(nameof(PeriodEndSound), value); }
        }

        public bool PlayPeriodStartEndSounds
        {
            get { return _settings.Get(nameof(PlayPeriodStartEndSounds), true); }
            set { _settings.Update(nameof(PlayPeriodStartEndSounds), value); }
        }

        public SoundNotificationsSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(new SoundNotificationsPlugin().Id);
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }

}
