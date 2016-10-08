using System;
using System.IO;
using YAPA.Contracts;

namespace YAPA.Shared
{
    public class SoundNotifications : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly SoundNotificationsSettings _settings;
        private readonly IMusicPlayer _musicPlayer;

        public SoundNotifications(IPomodoroEngine engine, SoundNotificationsSettings settings, IMusicPlayer musicPlayer)
        {
            _engine = engine;
            _settings = settings;
            _musicPlayer = musicPlayer;

            _engine.PropertyChanged += _engine_PropertyChanged;
        }

        private void _engine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_engine.Phase))
            {
                _musicPlayer.Stop();
                if (_engine.Phase == PomodoroPhase.Work || _engine.Phase == PomodoroPhase.Break)
                {
                    Play();
                }
            }
        }

        private void Play()
        {
            if (_settings.DisabelSoundNotifications)
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
                    break;
                case PomodoroPhase.Break:
                    songToPlay = _settings.BreakSong;
                    repeat = _settings.RepeatBreakSong;
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
        private readonly ISettings _settings;

        public string WorkSong
        {
            get { return _settings.Get<string>("WorkSong", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\tick.wav")); }
            set { _settings.Update("WorkSong", value); }
        }

        public bool RepeatWorkSong
        {
            get { return _settings.Get("RepeatWorkSong", false); }
            set { _settings.Update("RepeatWorkSong", value); }
        }

        public string BreakSong
        {
            get { return _settings.Get<string>("BreakSong", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources\ding.wav")); }
            set { _settings.Update("BreakSong", value); }
        }

        public bool RepeatBreakSong
        {
            get { return _settings.Get("RepeatBreakSong", false); }
            set { _settings.Update("RepeatBreakSong", value); }
        }

        public bool DisabelSoundNotifications
        {
            get { return _settings.Get("DisabelSoundNotifications", false); }
            set { _settings.Update("DisabelSoundNotifications", value); }
        }

        public SoundNotificationsSettings(ISettings settings)
        {
            _settings = settings;
        }
    }
}
