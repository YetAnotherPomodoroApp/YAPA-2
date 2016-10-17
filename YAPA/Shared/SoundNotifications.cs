using System;
using System.IO;
using YAPA.Contracts;
using YAPA.WPF;

namespace YAPA.Shared
{
    public class SoundNotificationsPlugin : IPluginMeta
    {
        public string Title => "Sound notifications";

        public Type Plugin => typeof(SoundNotifications);

        public Type Settings => typeof(SoundNotificationsSettings);

        public Type SettingEditWindow => typeof(SoundNotificationSettingWindow);
    }

    public class SoundNotifications : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly SoundNotificationsSettings _settings;
        private readonly IMusicPlayer _musicPlayer;
        private readonly IMusicPlayer _musicPlayer2;

        public SoundNotifications(IPomodoroEngine engine, SoundNotificationsSettings settings, IMusicPlayer musicPlayer, IMusicPlayer musicPlayer2)
        {
            _engine = engine;
            _settings = settings;
            _musicPlayer = musicPlayer;
            _musicPlayer2 = musicPlayer2;

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
            _musicPlayer.Play();
        }


        private void PlayPeriodEnd()
        {
            _musicPlayer.Load(_settings.PeriodEndSound);
            _musicPlayer.Play();
        }

        private void Play()
        {
            if (_settings.DisableSoundNotifications)
            {
                return;
            }

            var songToPlay = string.Empty;
            var repeat = false;

            switch (_engine.Phase)
            {
                case PomodoroPhase.Work:
                    songToPlay = _settings.WorkSong;
                    repeat = _settings.RepeatWorkSong;
                    PlayPeriodStart();
                    break;
                case PomodoroPhase.Break:
                    songToPlay = _settings.BreakSong;
                    repeat = _settings.RepeatBreakSong;
                    PlayPeriodStart();
                    break;
                case PomodoroPhase.BreakEnded:
                case PomodoroPhase.WorkEnded:
                    PlayPeriodEnd();
                    break;
            }

            if (File.Exists(songToPlay))
            {
                _musicPlayer.Load(songToPlay);
                _musicPlayer.Play(repeat);
            }
        }
    }

    public class SoundNotificationsSettings : IPluginSettings
    {
        private readonly ISettingsForPlugin _settings;

        public string PeriodStartSound
        {
            get { return _settings.Get<string>(nameof(WorkSong), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\tick.wav")); }
            set { _settings.Update(nameof(WorkSong), value); }
        }

        public string PeriodEndSound
        {
            get { return _settings.Get<string>(nameof(WorkSong), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\tick.wav")); }
            set { _settings.Update(nameof(WorkSong), value); }
        }

        public string WorkSong
        {
            get { return _settings.Get<string>(nameof(WorkSong), null); }
            set { _settings.Update(nameof(WorkSong), value); }
        }

        public bool RepeatWorkSong
        {
            get { return _settings.Get(nameof(RepeatWorkSong), false); }
            set { _settings.Update(nameof(RepeatWorkSong), value); }
        }

        public string BreakSong
        {
            get { return _settings.Get<string>(nameof(BreakSong), null); }
            set { _settings.Update(nameof(BreakSong), value); }
        }

        public bool RepeatBreakSong
        {
            get { return _settings.Get(nameof(RepeatBreakSong), false); }
            set { _settings.Update(nameof(RepeatBreakSong), value); }
        }

        public bool DisableSoundNotifications
        {
            get { return _settings.Get(nameof(DisableSoundNotifications), false); }
            set { _settings.Update(nameof(DisableSoundNotifications), value); }
        }

        public SoundNotificationsSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForPlugin(nameof(SoundNotifications));
        }

        public void DefereChanges()
        {
            _settings.DeferChanges();
        }
    }

}
