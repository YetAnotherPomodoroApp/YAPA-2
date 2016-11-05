using System;
using System.IO;
using YAPA.Contracts;
using YAPA.Plugins;

namespace YAPA.Shared
{
    public class MusicPlayerPluginMetas : IPluginMeta
    {
        public string Title => "Music player";

        public Type Plugin => typeof(MusicPlayerPlugin);

        public Type Settings => typeof(MusicPlayerPluginSettings);

        public Type SettingEditWindow => typeof(MusicPlayerPluginSettingWindow);
    }

    public class MusicPlayerPlugin : IPlugin
    {
        private readonly IPomodoroEngine _engine;
        private readonly MusicPlayerPluginSettings _settings;
        private readonly IMusicPlayer _musicPlayer;
        private readonly PomodoroEngineSettings _engineSettings;

        public MusicPlayerPlugin(IPomodoroEngine engine, MusicPlayerPluginSettings settings, IMusicPlayer musicPlayer, PomodoroEngineSettings engineSettings)
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

        private void Play()
        {
            if (_engineSettings.DisableSoundNotifications)
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
                case PomodoroPhase.BreakEnded:
                case PomodoroPhase.WorkEnded:
                    break;
            }

            if (File.Exists(songToPlay))
            {
                _musicPlayer.Load(songToPlay);
                _musicPlayer.Play(repeat);
            }
        }
    }

    public class MusicPlayerPluginSettings : IPluginSettings
    {
        private readonly ISettingsForComponent _settings;

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

        public MusicPlayerPluginSettings(ISettings settings)
        {
            _settings = settings.GetSettingsForComponent(nameof(MusicPlayerPlugin));
        }

        public void DeferChanges()
        {
            _settings.DeferChanges();
        }
    }

}
